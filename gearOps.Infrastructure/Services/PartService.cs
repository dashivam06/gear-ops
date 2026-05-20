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
using gearOps.Domain.Enums;
using gearOps.Infrastructure.Data;

namespace gearOps.Infrastructure.Services;

public class PartService : IPartService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PartService> _logger;

    public PartService(AppDbContext context, ILogger<PartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PartResponseDto> CreatePartAsync(CreatePartDto dto)
    {
        try
        {
            // Validate vendor exists
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == dto.VendorId && !v.IsDeleted)
                ?? throw new NotFoundException($"Vendor with ID {dto.VendorId} not found.");

            var part = new Part
            {
                VendorId = dto.VendorId,
                PartName = dto.PartName,
                Description = dto.Description,
                Category = dto.Category,
                StockQuantity = dto.StockQuantity,
                Unit = dto.Unit,
                CostPricePerUnit = dto.CostPricePerUnit,
                SellingPricePerUnit = dto.SellingPricePerUnit,
                ImageUrl = dto.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Parts.Add(part);
            await _context.SaveChangesAsync();
            await NotifyAdminsIfLowStockAsync(part);

            _logger.LogInformation($"Part created: {part.PartId} - {part.PartName}");

            return MapToDto(part, vendor.VendorName);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating part");
            throw;
        }
    }

    public async Task<PartResponseDto> UpdatePartAsync(UpdatePartDto dto)
    {
        try
        {
            var part = await _context.Parts
                .Include(p => p.Vendor)
                .FirstOrDefaultAsync(p => p.PartId == dto.PartId)
                ?? throw new NotFoundException($"Part with ID {dto.PartId} not found.");
            if (part.IsDeleted)
                throw new NotFoundException($"Part with ID {dto.PartId} not found.");

            var vendor = dto.VendorId == part.VendorId
                ? part.Vendor
                : await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == dto.VendorId && !v.IsDeleted)
                  ?? throw new NotFoundException($"Vendor with ID {dto.VendorId} not found.");

            part.VendorId = dto.VendorId;
            part.PartName = dto.PartName;
            part.Description = dto.Description;
            part.Category = dto.Category;
            part.StockQuantity = dto.StockQuantity;
            part.Unit = dto.Unit;
            part.CostPricePerUnit = dto.CostPricePerUnit;
            part.SellingPricePerUnit = dto.SellingPricePerUnit;
            part.ImageUrl = dto.ImageUrl;

            _context.Parts.Update(part);
            await _context.SaveChangesAsync();
            await NotifyAdminsIfLowStockAsync(part);

            _logger.LogInformation($"Part updated: {part.PartId}");

            return MapToDto(part, vendor.VendorName);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating part");
            throw;
        }
    }

    public async Task<PartResponseDto?> GetPartByIdAsync(int partId)
    {
        var part = await _context.Parts
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.PartId == partId && !p.IsDeleted);

        return part == null ? null : MapToDto(part, part.Vendor.VendorName);
    }

    public async Task<List<PartResponseDto>> GetAllPartsAsync()
    {
        var parts = await _context.Parts
            .Include(p => p.Vendor)
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        return parts.Select(p => MapToDto(p, p.Vendor.VendorName)).ToList();
    }

    public async Task<PagedResult<PartResponseDto>> GetAllPartsAsync(PaginationParams paging)
    {
        var query = _context.Parts
            .Include(p => p.Vendor)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedPartResultAsync(query, paging);
    }

    public async Task<List<PartResponseDto>> GetPartsByCategoryAsync(string category)
    {
        var parts = await _context.Parts
            .Where(p => p.Category == category && !p.IsDeleted)
            .Include(p => p.Vendor)
            .ToListAsync();

        return parts.Select(p => MapToDto(p, p.Vendor.VendorName)).ToList();
    }

    public async Task<PagedResult<PartResponseDto>> GetPartsByCategoryAsync(string category, PaginationParams paging)
    {
        var query = _context.Parts
            .Include(p => p.Vendor)
            .Where(p => p.Category == category && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedPartResultAsync(query, paging);
    }

    public async Task<List<PartResponseDto>> SearchPartsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllPartsAsync();

        query = query.Trim().ToLower();

        var parts = await _context.Parts
            .Include(p => p.Vendor)
            .Where(p => !p.IsDeleted
                        && (p.PartName.ToLower().Contains(query)
                        || p.Category.ToLower().Contains(query)
                        || (p.Description != null && p.Description.ToLower().Contains(query))
                        || p.Vendor.VendorName.ToLower().Contains(query)))
            .ToListAsync();

        return parts.Select(p => MapToDto(p, p.Vendor.VendorName)).ToList();
    }

    public async Task<PagedResult<PartResponseDto>> SearchPartsAsync(string query, PaginationParams paging)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllPartsAsync(paging);

        query = query.Trim().ToLower();

        var pagedQuery = _context.Parts
            .Include(p => p.Vendor)
            .Where(p => !p.IsDeleted
                        && (p.PartName.ToLower().Contains(query)
                        || p.Category.ToLower().Contains(query)
                        || (p.Description != null && p.Description.ToLower().Contains(query))
                        || p.Vendor.VendorName.ToLower().Contains(query)))
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedPartResultAsync(pagedQuery, paging);
    }

    public async Task<List<PartResponseDto>> GetPartsByVendorAsync(int vendorId)
    {
        var vendor = await _context.Vendors.FindAsync(vendorId)
            ?? throw new NotFoundException($"Vendor with ID {vendorId} not found.");

        var parts = await _context.Parts
            .Where(p => p.VendorId == vendorId && !p.IsDeleted)
            .ToListAsync();

        return parts.Select(p => MapToDto(p, vendor.VendorName)).ToList();
    }

    public async Task<bool> DeletePartAsync(int partId)
    {
        try
        {
            var part = await _context.Parts.FindAsync(partId)
                ?? throw new NotFoundException($"Part with ID {partId} not found.");
            if (part.IsDeleted)
                throw new NotFoundException($"Part with ID {partId} not found.");

            part.IsDeleted = true;
            part.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Part soft deleted: {PartId}", partId);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting part");
            throw;
        }
    }

    private static PartResponseDto MapToDto(Part part, string vendorName) => new()
    {
        PartId = part.PartId,
        VendorId = part.VendorId,
        PartName = part.PartName,
        Description = part.Description,
        Category = part.Category,
        StockQuantity = part.StockQuantity,
        Unit = part.Unit,
        CostPricePerUnit = part.CostPricePerUnit,
        SellingPricePerUnit = part.SellingPricePerUnit,
        ImageUrl = part.ImageUrl,
        CreatedAt = part.CreatedAt,
        VendorName = vendorName,
        IsDeleted = part.IsDeleted,
        DeletedAt = part.DeletedAt,
        Status = part.IsDeleted ? "inactive" : "active"
    };

    private static async Task<PagedResult<PartResponseDto>> ToPagedPartResultAsync(
        IOrderedQueryable<Part> query,
        PaginationParams paging)
    {
        var totalItems = await query.CountAsync();
        var parts = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync();

        return new PagedResult<PartResponseDto>
        {
            Items = parts.Select(p => MapToDto(p, p.Vendor.VendorName)).ToList(),
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)paging.PageSize)
        };
    }

    private async Task NotifyAdminsIfLowStockAsync(Part part, int threshold = 10)
    {
        if (part.StockQuantity >= threshold)
            return;

        var subject = $"Low stock: {part.PartName} (#{part.PartId})";
        var alreadyOpen = await _context.Notifications.AnyAsync(n =>
            n.LogType == "LowStock" &&
            n.Subject == subject &&
            !n.IsRead);

        if (alreadyOpen)
            return;

        var adminIds = await _context.Users
            .Where(u => u.Role == Role.Admin)
            .Select(u => u.UserId)
            .ToListAsync();

        foreach (var adminId in adminIds)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = adminId,
                LogType = "LowStock",
                Subject = subject,
                Message = $"{part.PartName} has {part.StockQuantity} {part.Unit} remaining. Reorder threshold is {threshold}.",
                MailedStatus = NotificationStatus.Pending
            });
        }

        if (adminIds.Count > 0)
            await _context.SaveChangesAsync();
    }
}
