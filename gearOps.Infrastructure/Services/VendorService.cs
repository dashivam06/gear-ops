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

public class VendorService : IVendorService
{
    private readonly AppDbContext _context;
    private readonly ILogger<VendorService> _logger;

    public VendorService(AppDbContext context, ILogger<VendorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<VendorResponseDto> CreateVendorAsync(CreateVendorDto dto)
    {
        try
        {
            // Validate unique phone
            var existingPhone = await _context.Vendors
                .FirstOrDefaultAsync(v => v.Phone == dto.Phone && !v.IsDeleted);
            if (existingPhone != null)
                throw new ConflictException($"Vendor with phone '{dto.Phone}' already exists.");

            // Validate unique vendor name
            var existingName = await _context.Vendors
                .FirstOrDefaultAsync(v => v.VendorName == dto.VendorName && !v.IsDeleted);
            if (existingName != null)
                throw new ConflictException($"Vendor with name '{dto.VendorName}' already exists.");

            var vendor = new Vendor
            {
                VendorName = dto.VendorName,
                ContactPerson = dto.ContactPerson,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                ImageUrl = dto.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Vendor created: {vendor.VendorId} - {vendor.VendorName}");

            return MapToDto(vendor);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating vendor");
            throw;
        }
    }

    public async Task<VendorResponseDto> UpdateVendorAsync(UpdateVendorDto dto)
    {
        try
        {
            var vendor = await _context.Vendors.FindAsync(dto.VendorId)
                ?? throw new NotFoundException($"Vendor with ID {dto.VendorId} not found.");
            if (vendor.IsDeleted)
                throw new NotFoundException($"Vendor with ID {dto.VendorId} not found.");

            // Check phone uniqueness (exclude current vendor)
            var phoneExists = await _context.Vendors
                .Where(v => v.Phone == dto.Phone && v.VendorId != dto.VendorId && !v.IsDeleted)
                .FirstOrDefaultAsync();
            if (phoneExists != null)
                throw new ConflictException($"Phone '{dto.Phone}' is already used by another vendor.");

            // Check name uniqueness (exclude current vendor)
            var nameExists = await _context.Vendors
                .Where(v => v.VendorName == dto.VendorName && v.VendorId != dto.VendorId && !v.IsDeleted)
                .FirstOrDefaultAsync();
            if (nameExists != null)
                throw new ConflictException($"Vendor name '{dto.VendorName}' is already in use.");

            vendor.VendorName = dto.VendorName;
            vendor.ContactPerson = dto.ContactPerson;
            vendor.Phone = dto.Phone;
            vendor.Email = dto.Email;
            vendor.Address = dto.Address;
            vendor.ImageUrl = dto.ImageUrl;

            _context.Vendors.Update(vendor);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Vendor updated: {vendor.VendorId}");

            return MapToDto(vendor);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating vendor");
            throw;
        }
    }

    public async Task<VendorResponseDto?> GetVendorByIdAsync(int vendorId)
    {
        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == vendorId && !v.IsDeleted);
        return vendor == null ? null : MapToDto(vendor);
    }

    public async Task<List<VendorResponseDto>> GetAllVendorsAsync()
    {
        var vendors = await _context.Vendors
            .Where(v => !v.IsDeleted)
            .ToListAsync();
        return vendors.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<VendorResponseDto>> GetAllVendorsAsync(PaginationParams paging)
    {
        var query = _context.Vendors
            .Where(v => !v.IsDeleted);

        if (!string.IsNullOrWhiteSpace(paging.Search))
        {
            var search = paging.Search.ToLower();
            query = query.Where(v =>
                v.VendorName.ToLower().Contains(search) ||
                (v.ContactPerson != null && v.ContactPerson.ToLower().Contains(search)) ||
                (v.Email != null && v.Email.ToLower().Contains(search)) ||
                (v.Phone != null && v.Phone.ToLower().Contains(search))
            );
        }

        query = query.OrderByDescending(v => v.CreatedAt);
        var totalItems = await query.CountAsync();
        var vendors = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync();

        return new PagedResult<VendorResponseDto>
        {
            Items = vendors.Select(MapToDto).ToList(),
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)paging.PageSize)
        };
    }

    public async Task<bool> DeleteVendorAsync(int vendorId)
    {
        try
        {
            var vendor = await _context.Vendors
                .Include(v => v.Parts)
                .Include(v => v.PurchaseOrders)
                .FirstOrDefaultAsync(v => v.VendorId == vendorId)
                ?? throw new NotFoundException($"Vendor with ID {vendorId} not found.");
                
            if (vendor.IsDeleted)
                throw new NotFoundException($"Vendor with ID {vendorId} not found.");

            if (!vendor.Parts.Any() && !vendor.PurchaseOrders.Any())
            {
                // Hard delete if no dependencies exist (e.g., mistaken creation)
                _context.Vendors.Remove(vendor);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Vendor hard deleted: {VendorId} as no parts or purchase orders were associated.", vendorId);
            }
            else
            {
                // Soft delete if dependencies exist
                vendor.IsDeleted = true;
                vendor.DeletedAt = DateTime.UtcNow;
                _context.Vendors.Update(vendor);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Vendor soft deleted: {VendorId}. Existing parts and records remain.", vendorId);
            }
            
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting vendor");
            throw;
        }
    }

    private static VendorResponseDto MapToDto(Vendor vendor) => new()
    {
        VendorId = vendor.VendorId,
        VendorName = vendor.VendorName,
        ContactPerson = vendor.ContactPerson,
        Phone = vendor.Phone,
        Email = vendor.Email,
        Address = vendor.Address,
        ImageUrl = vendor.ImageUrl,
        CreatedAt = vendor.CreatedAt,
        IsDeleted = vendor.IsDeleted,
        DeletedAt = vendor.DeletedAt,
        Status = vendor.IsDeleted ? "inactive" : "active"
    };
}
