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

public class StaffServiceRecordService : IStaffServiceRecordService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StaffServiceRecordService> _logger;

    public StaffServiceRecordService(AppDbContext context, ILogger<StaffServiceRecordService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StaffServiceRecordDto> CreateServiceRecordAsync(int staffId, CompleteAppointmentDto dto)
    {
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Vehicle)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.AppointmentId == dto.AppointmentId)
                ?? throw new NotFoundException($"Appointment with ID {dto.AppointmentId} not found.");

            if (appointment.Status != AppointmentStatus.Pending)
                throw new BadRequestException("Only pending appointments can be completed.");

            if (dto.ServiceCost < 0)
                throw new BadRequestException("Service cost cannot be negative.");

            var serviceRecord = new ServiceRecord
            {
                AppointmentId = dto.AppointmentId,
                VehicleId = appointment.VehicleId,
                StaffId = staffId,
                ServiceDescription = dto.ServiceDescription,
                ServiceCost = dto.ServiceCost,
                ServiceDate = DateTime.UtcNow,
                Status = ServiceRecordStatus.Completed
            };

            _context.ServiceRecords.Add(serviceRecord);
            
            // Update appointment status
            appointment.Status = AppointmentStatus.Completed;
            _context.Appointments.Update(appointment);

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Service record created: {serviceRecord.ServiceRecordId} by staff {staffId}");

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

            _logger.LogInformation($"Service record updated: {dto.ServiceRecordId}");

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
            .Include(sr => sr.Vehicle)
            .OrderByDescending(sr => sr.ServiceDate)
            .ToListAsync();

        return serviceRecords.Select(sr => MapToDto(sr, sr.Appointment)).ToList();
    }

    public async Task<List<StaffServiceRecordDto>> GetMonthlyServiceRecordsAsync(int staffId, int year, int month)
    {
        var serviceRecords = await _context.ServiceRecords
            .Where(sr => sr.StaffId == staffId &&
                         sr.ServiceDate.Year == year &&
                         sr.ServiceDate.Month == month)
            .Include(sr => sr.Appointment)
            .Include(sr => sr.Vehicle)
            .OrderByDescending(sr => sr.ServiceDate)
            .ToListAsync();

        return serviceRecords.Select(sr => MapToDto(sr, sr.Appointment)).ToList();
    }

    private StaffServiceRecordDto MapToDto(ServiceRecord serviceRecord, Appointment appointment)
    {
        var reviews = _context.Reviews
            .Where(r => r.AppointmentId == appointment.AppointmentId)
            .FirstOrDefault();

        return new StaffServiceRecordDto
        {
            ServiceRecordId = serviceRecord.ServiceRecordId,
            AppointmentId = serviceRecord.AppointmentId,
            VehicleId = serviceRecord.VehicleId,
            VehicleNumber = serviceRecord.Vehicle.VehicleNumber,
            CustomerName = appointment.Customer.FullName,
            ServiceDescription = serviceRecord.ServiceDescription,
            ServiceCost = serviceRecord.ServiceCost,
            ServiceDate = serviceRecord.ServiceDate,
            Status = serviceRecord.Status.ToString(),
            ReviewRating = reviews?.Rating,
            ReviewComment = reviews?.Comment,
            CreatedAt = serviceRecord.ServiceDate
        };
    }
}
