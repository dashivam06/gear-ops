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
using gearOps.Infrastructure.Extensions;

namespace gearOps.Infrastructure.Services;

public class StaffServiceRecordService : IStaffServiceRecordService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StaffServiceRecordService> _logger;

    public StaffServiceRecordService(AppDbContext context, ILogger<StaffServiceRecordService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Legacy overload (used internally when completing via CompleteAppointmentDto) ──

    public async Task<StaffServiceRecordDto> CreateServiceRecordAsync(int staffId, CompleteAppointmentDto dto)
    {
        return await CreateCoreAsync(staffId, dto.AppointmentId, dto.ServiceDescription, dto.ServiceCost);
    }

    // ── New standalone overload: serviceCost may be 0 at creation time ──

    /// <inheritdoc />
    public async Task<StaffServiceRecordDto> CreateServiceRecordAsync(int staffId, CreateServiceRecordDto dto)
    {
        if (dto.ServiceCost < 0)
            throw new BadRequestException("Service cost cannot be negative.");

        return await CreateCoreAsync(staffId, dto.AppointmentId, dto.ServiceDescription, dto.ServiceCost);
    }

    private async Task<StaffServiceRecordDto> CreateCoreAsync(
        int staffId, int appointmentId, string description, decimal cost)
    {
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Vehicle)
                .Include(a => a.Customer)
                .Include(a => a.Reviews)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId)
                ?? throw new NotFoundException($"Appointment with ID {appointmentId} not found.");

            // Allow creating a record for Confirmed or Completed appointments
            if (appointment.Status != AppointmentStatus.Confirmed &&
                appointment.Status != AppointmentStatus.Completed)
            {
                throw new BadRequestException(
                    $"Service records can only be created for Confirmed or Completed appointments. " +
                    $"Current status: {appointment.Status}");
            }

            if (cost < 0)
                throw new BadRequestException("Service cost cannot be negative.");

            var serviceRecord = new ServiceRecord
            {
                AppointmentId = appointmentId,
                VehicleId = appointment.VehicleId,
                StaffId = staffId,
                ServiceDescription = description,
                ServiceCost = cost,
                ServiceDate = DateTime.UtcNow,
                Status = ServiceRecordStatus.Completed
            };

            _context.ServiceRecords.Add(serviceRecord);

            // Mark appointment as Completed if it was still Confirmed
            if (appointment.Status == AppointmentStatus.Confirmed)
            {
                appointment.Status = AppointmentStatus.Completed;
                _context.Appointments.Update(appointment);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Service record {RecordId} created by staff {StaffId}", serviceRecord.ServiceRecordId, staffId);

            return MapToDto(serviceRecord, appointment);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating service record");
            throw;
        }
    }

    public async Task<StaffServiceRecordDto> UpdateServiceRecordAsync(int staffId, UpdateServiceRecordDto dto)
    {
        try
        {
            var serviceRecord = await _context.ServiceRecords
                .Include(sr => sr.Appointment)
                    .ThenInclude(a => a.Customer)
                .Include(sr => sr.Appointment)
                    .ThenInclude(a => a.Reviews)
                .Include(sr => sr.Vehicle)
                .Include(sr => sr.Staff)
                .FirstOrDefaultAsync(sr => sr.ServiceRecordId == dto.ServiceRecordId && sr.StaffId == staffId)
                ?? throw new NotFoundException($"Service record with ID {dto.ServiceRecordId} not found or does not belong to you.");

            if (dto.ServiceCost < 0)
                throw new BadRequestException("Service cost cannot be negative.");

            serviceRecord.ServiceDescription = dto.ServiceDescription;
            serviceRecord.ServiceCost = dto.ServiceCost;

            _context.ServiceRecords.Update(serviceRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Service record updated: {RecordId}", dto.ServiceRecordId);

            return MapToDto(serviceRecord, serviceRecord.Appointment);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating service record");
            throw;
        }
    }

    public async Task<StaffServiceRecordDto?> GetServiceRecordByIdAsync(int serviceRecordId)
    {
        var serviceRecord = await _context.ServiceRecords
            .Include(sr => sr.Appointment)
                .ThenInclude(a => a.Customer)
            .Include(sr => sr.Appointment)
                .ThenInclude(a => a.Reviews)
            .Include(sr => sr.Vehicle)
            .Include(sr => sr.Staff)
            .FirstOrDefaultAsync(sr => sr.ServiceRecordId == serviceRecordId);

        return serviceRecord == null ? null : MapToDto(serviceRecord, serviceRecord.Appointment);
    }

    public async Task<List<StaffServiceRecordDto>> GetStaffServiceRecordsAsync(int staffId)
    {
        var serviceRecords = await _context.ServiceRecords
            .Where(sr => sr.StaffId == staffId)
            .Include(sr => sr.Appointment)
                .ThenInclude(a => a.Customer)
            .Include(sr => sr.Appointment)
                .ThenInclude(a => a.Reviews)
            .Include(sr => sr.Vehicle)
            .OrderByDescending(sr => sr.ServiceDate)
            .ToListAsync();

        return serviceRecords.Select(sr => MapToDto(sr, sr.Appointment)).ToList();
    }

    /// <summary>Returns all service records across all staff — used by the global list endpoint.</summary>
    public async Task<List<StaffServiceRecordDto>> GetAllServiceRecordsAsync()
    {
        var serviceRecords = await _context.ServiceRecords
            .Include(sr => sr.Appointment)
                .ThenInclude(a => a.Customer)
            .Include(sr => sr.Appointment)
                .ThenInclude(a => a.Reviews)
            .Include(sr => sr.Vehicle)
            .OrderByDescending(sr => sr.ServiceDate)
            .ToListAsync();

        return serviceRecords.Select(sr => MapToDto(sr, sr.Appointment)).ToList();
    }

    public async Task<PagedResult<StaffServiceRecordDto>> GetAllServiceRecordsAsync(PaginationParams paging)
    {
        var query = _context.ServiceRecords
            .Include(sr => sr.Appointment)
                .ThenInclude(a => a.Customer)
            .Include(sr => sr.Appointment)
                .ThenInclude(a => a.Reviews)
            .Include(sr => sr.Vehicle)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(paging.Search))
        {
            var search = paging.Search.ToLower();
            query = query.Where(sr =>
                (sr.Appointment.Customer.FullName != null && sr.Appointment.Customer.FullName.ToLower().Contains(search)) ||
                (sr.Vehicle.VehicleNumber != null && sr.Vehicle.VehicleNumber.ToLower().Contains(search)) ||
                (sr.ServiceDescription != null && sr.ServiceDescription.ToLower().Contains(search))
            );
        }

        query = query.OrderByDescending(sr => sr.ServiceDate);

        var paged = await query.ToPagedResultAsync(paging);
        
        return new PagedResult<StaffServiceRecordDto>
        {
            Items = paged.Items.Select(sr => MapToDto(sr, sr.Appointment)).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
            TotalPages = paged.TotalPages
        };
    }

    public async Task<List<StaffServiceRecordDto>> GetMonthlyServiceRecordsAsync(int staffId, int year, int month)
    {
        var serviceRecords = await _context.ServiceRecords
            .Where(sr => sr.StaffId == staffId &&
                         sr.ServiceDate.Year == year &&
                         sr.ServiceDate.Month == month)
            .Include(sr => sr.Appointment)
                .ThenInclude(a => a.Customer)
            .Include(sr => sr.Appointment)
                .ThenInclude(a => a.Reviews)
            .Include(sr => sr.Vehicle)
            .OrderByDescending(sr => sr.ServiceDate)
            .ToListAsync();

        return serviceRecords.Select(sr => MapToDto(sr, sr.Appointment)).ToList();
    }

    private static StaffServiceRecordDto MapToDto(ServiceRecord serviceRecord, Appointment appointment)
    {
        var review = appointment.Reviews?.FirstOrDefault();

        return new StaffServiceRecordDto
        {
            ServiceRecordId = serviceRecord.ServiceRecordId,
            AppointmentId = serviceRecord.AppointmentId,
            VehicleId = serviceRecord.VehicleId,
            VehicleNumber = serviceRecord.Vehicle?.VehicleNumber ?? "N/A",
            CustomerId = appointment.CustomerId,
            CustomerName = appointment.Customer?.FullName ?? "Unknown Customer",
            ServiceDescription = serviceRecord.ServiceDescription,
            ServiceCost = serviceRecord.ServiceCost,
            ServiceDate = serviceRecord.ServiceDate,
            Status = serviceRecord.Status.ToString(),
            ReviewRating = review?.Rating,
            ReviewComment = review?.Comment,
            CreatedAt = serviceRecord.ServiceDate
        };
    }
}
