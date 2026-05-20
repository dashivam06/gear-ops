using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Infrastructure.Data;

namespace gearOps.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(AppDbContext context, ILogger<ReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReviewResponseDto> CreateReviewAsync(int userId, CreateReviewDto dto)
    {
        try
        {
            // Verify appointment belongs to user
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == dto.AppointmentId && a.CustomerId == userId)
                ?? throw new NotFoundException($"Appointment with ID {dto.AppointmentId} not found.");

            // Check if review already exists
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.AppointmentId == dto.AppointmentId && !r.IsDeleted);
            if (existingReview != null)
                throw new ConflictException("A review already exists for this appointment.");

            if (dto.Rating < 1 || dto.Rating > 5)
                throw new BadRequestException("Rating must be between 1 and 5.");

            var review = new Review
            {
                CustomerId = userId,
                AppointmentId = dto.AppointmentId,
                Rating = (byte)dto.Rating,
                Comment = dto.Comment,
                ReviewedDate = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Review created: {review.ReviewId} for appointment {dto.AppointmentId}");

            return MapToDto(review);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating review");
            throw;
        }
    }

    public async Task<ReviewResponseDto?> GetReviewByIdAsync(int reviewId)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId && !r.IsDeleted);
        return review == null ? null : MapToDto(review);
    }

    public async Task<List<ReviewResponseDto>> GetCustomerReviewsAsync(int userId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.CustomerId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.ReviewedDate)
            .ToListAsync();

        return reviews.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteReviewAsync(int reviewId)
    {
        try
        {
            var review = await _context.Reviews.FindAsync(reviewId)
                ?? throw new NotFoundException($"Review with ID {reviewId} not found.");
            if (review.IsDeleted)
                throw new NotFoundException($"Review with ID {reviewId} not found.");

            review.IsDeleted = true;
            review.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Review soft deleted: {ReviewId}", reviewId);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting review");
            throw;
        }
    }

    private ReviewResponseDto MapToDto(Review review) => new()
    {
        ReviewId = review.ReviewId,
        AppointmentId = review.AppointmentId,
        Rating = review.Rating,
        Comment = review.Comment,
        CreatedAt = review.ReviewedDate
    };
}
