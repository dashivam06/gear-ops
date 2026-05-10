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

public class CustomerProfileService : ICustomerProfileService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomerProfileService> _logger;

    public CustomerProfileService(AppDbContext context, ILogger<CustomerProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CustomerProfileResponseDto> GetProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Vehicles)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new NotFoundException($"Customer with ID {userId} not found.");

        return MapUserToProfileDto(user);
    }

    public async Task<CustomerProfileResponseDto> UpdateProfileAsync(int userId, UpdateCustomerProfileDto dto)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Vehicles)
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new NotFoundException($"Customer with ID {userId} not found.");

            // Check phone uniqueness (exclude current user)
            var phoneExists = await _context.Users
                .Where(u => u.Phone == dto.Phone && u.UserId != userId)
                .FirstOrDefaultAsync();
            if (phoneExists != null)
                throw new ConflictException($"Phone '{dto.Phone}' is already in use by another customer.");

            user.FullName = dto.FullName;
            user.Phone = dto.Phone;
            user.Address = dto.Address;
            user.ProfileImageUrl = dto.ProfileImageUrl;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Customer profile updated: {userId}");

            return MapUserToProfileDto(user);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating customer profile");
            throw;
        }
    }

    public async Task<VehicleResponseDto> AddVehicleAsync(int userId, CreateVehicleDto dto)
    {
        try
        {
            // Verify user exists
            var user = await _context.Users.FindAsync(userId)
                ?? throw new NotFoundException($"Customer with ID {userId} not found.");

            // Check for duplicate vehicle number
            var existingVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleNumber == dto.VehicleNumber && v.CustomerId == userId);
            if (existingVehicle != null)
                throw new ConflictException($"Vehicle with number '{dto.VehicleNumber}' already registered.");

            var vehicle = new Vehicle
            {
                CustomerId = userId,
                VehicleNumber = dto.VehicleNumber,
                Brand = dto.Brand,
                Model = dto.Model,
                Year = dto.Year,
                ImageUrl = dto.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Vehicle added: {vehicle.VehicleId} for customer {userId}");

            return MapVehicleToDto(vehicle);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error adding vehicle");
            throw;
        }
    }

    public async Task<VehicleResponseDto> UpdateVehicleAsync(int userId, UpdateVehicleDto dto)
    {
        try
        {
            var vehicle = await _context.Vehicles.FindAsync(dto.VehicleId)
                ?? throw new NotFoundException($"Vehicle with ID {dto.VehicleId} not found.");

            // Verify ownership
            if (vehicle.CustomerId != userId)
                throw new UnauthorizedException("You do not have permission to update this vehicle.");

            // Check for duplicate vehicle number (exclude current)
            var existingVehicle = await _context.Vehicles
                .Where(v => v.VehicleNumber == dto.VehicleNumber && v.VehicleId != dto.VehicleId && v.CustomerId == userId)
                .FirstOrDefaultAsync();
            if (existingVehicle != null)
                throw new ConflictException($"Vehicle with number '{dto.VehicleNumber}' already registered.");

            vehicle.VehicleNumber = dto.VehicleNumber;
            vehicle.Brand = dto.Brand;
            vehicle.Model = dto.Model;
            vehicle.Year = dto.Year;
            vehicle.ImageUrl = dto.ImageUrl;

            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Vehicle updated: {dto.VehicleId}");

            return MapVehicleToDto(vehicle);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating vehicle");
            throw;
        }
    }

    public async Task<VehicleResponseDto?> GetVehicleByIdAsync(int vehicleId)
    {
        var vehicle = await _context.Vehicles.FindAsync(vehicleId);
        return vehicle == null ? null : MapVehicleToDto(vehicle);
    }

    public async Task<List<VehicleResponseDto>> GetCustomerVehiclesAsync(int userId)
    {
        var vehicles = await _context.Vehicles
            .Where(v => v.CustomerId == userId)
            .ToListAsync();

        return vehicles.Select(MapVehicleToDto).ToList();
    }

    public async Task<bool> DeleteVehicleAsync(int userId, int vehicleId)
    {
        try
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId)
                ?? throw new NotFoundException($"Vehicle with ID {vehicleId} not found.");

            if (vehicle.CustomerId != userId)
                throw new UnauthorizedException("You do not have permission to delete this vehicle.");

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Vehicle deleted: {vehicleId}");
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting vehicle");
            throw;
        }
    }

    private CustomerProfileResponseDto MapUserToProfileDto(User user) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        Address = user.Address,
        ProfileImageUrl = user.ProfileImageUrl,
        CreditsRemaining = user.CreditsRemaining,
        CreatedAt = user.CreatedAt,
        Vehicles = user.Vehicles.Select(MapVehicleToDto).ToList()
    };

    private VehicleResponseDto MapVehicleToDto(Vehicle vehicle) => new()
    {
        VehicleId = vehicle.VehicleId,
        VehicleNumber = vehicle.VehicleNumber,
        Brand = vehicle.Brand,
        Model = vehicle.Model,
        Year = vehicle.Year,
        ImageUrl = vehicle.ImageUrl,
        CreatedAt = vehicle.CreatedAt
    };
}
