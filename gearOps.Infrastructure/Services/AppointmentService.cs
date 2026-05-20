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

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AppointmentService> _logger;
    private readonly IEmailService _emailService;

    public AppointmentService(AppDbContext context, ILogger<AppointmentService> logger, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<AppointmentResponseDto> CreateAppointmentAsync(int userId, CreateAppointmentDto dto)
    {
        try
        {
            // Verify vehicle belongs to user
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == dto.VehicleId && v.CustomerId == userId && !v.IsDeleted)
                ?? throw new NotFoundException("Vehicle not found or does not belong to this customer.");

            if (dto.RequestedDate < DateTime.UtcNow)
                throw new BadRequestException("Appointment date must be in the future.");

            var appointment = new Appointment
            {
                CustomerId = userId,
                VehicleId = dto.VehicleId,
                RequestedDate = dto.RequestedDate,
                Remarks = dto.Remarks,
                Status = AppointmentStatus.Pending
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Appointment created: {appointment.AppointmentId} for customer {userId}");

            return MapToDto(appointment, vehicle.VehicleNumber);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating appointment");
            throw;
        }
    }

    public async Task<AppointmentResponseDto> UpdateAppointmentAsync(int userId, UpdateAppointmentDto dto)
    {
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Vehicle)
                .FirstOrDefaultAsync(a => a.AppointmentId == dto.AppointmentId)
                ?? throw new NotFoundException($"Appointment with ID {dto.AppointmentId} not found.");

            if (appointment.CustomerId != userId)
                throw new UnauthorizedException("You do not have permission to update this appointment.");

            if (appointment.Vehicle.IsDeleted)
                throw new BadRequestException("Cannot reschedule an appointment for a deleted vehicle.");

            if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Confirmed)
                throw new BadRequestException("Only pending or confirmed appointments can be rescheduled.");

            if (dto.RequestedDate < DateTime.UtcNow)
                throw new BadRequestException("Appointment date must be in the future.");

            var oldDate = appointment.RequestedDate;
            appointment.RequestedDate = dto.RequestedDate;
            appointment.Status = AppointmentStatus.Pending; // Reset to Pending for re-approval

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            // Send reschedule notification to customer
            var customer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);
            if (customer != null)
            {
                await _emailService.SendAppointmentRescheduleEmailAsync(
                    customer.Email,
                    customer.FullName,
                    appointment.Vehicle.VehicleNumber,
                    oldDate,
                    appointment.RequestedDate,
                    "Customer (self-reschedule)");
            }

            _logger.LogInformation("Appointment rescheduled: {Id}", dto.AppointmentId);

            return MapToDto(appointment, appointment.Vehicle.VehicleNumber);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating appointment");
            throw;
        }
    }

    public async Task<AppointmentResponseDto?> GetAppointmentByIdAsync(int appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        return appointment == null ? null : MapToDto(appointment, appointment.Vehicle.VehicleNumber);
    }

    public async Task<List<AppointmentResponseDto>> GetCustomerAppointmentsAsync(int userId)
    {
        var appointments = await _context.Appointments
            .Where(a => a.CustomerId == userId)
            .Include(a => a.Vehicle)
            .OrderByDescending(a => a.RequestedDate)
            .ToListAsync();

        return appointments.Select(a => MapToDto(a, a.Vehicle.VehicleNumber)).ToList();
    }

    public async Task<List<AppointmentResponseDto>> GetUpcomingAppointmentsAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var appointments = await _context.Appointments
            .Where(a => a.CustomerId == userId && a.RequestedDate >= now)
            .Include(a => a.Vehicle)
            .OrderBy(a => a.RequestedDate)
            .ToListAsync();

        return appointments.Select(a => MapToDto(a, a.Vehicle.VehicleNumber)).ToList();
    }

    public async Task<bool> CancelAppointmentAsync(int appointmentId, int userId)
    {
        try
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId)
                ?? throw new NotFoundException($"Appointment with ID {appointmentId} not found.");

            if (appointment.CustomerId != userId)
                throw new UnauthorizedException("You do not have permission to cancel this appointment.");

            if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Confirmed)
                throw new BadRequestException($"Only Pending or Confirmed appointments can be cancelled. Current status: {appointment.Status}");

            appointment.Status = AppointmentStatus.Cancelled;
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Appointment cancelled: {appointmentId} by customer {userId}");
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error cancelling appointment");
            throw;
        }
    }

    private AppointmentResponseDto MapToDto(Appointment appointment, string vehicleNumber) => new()
    {
        AppointmentId = appointment.AppointmentId,
        CustomerId = appointment.CustomerId,
        VehicleId = appointment.VehicleId,
        VehicleNumber = vehicleNumber,
        RequestedDate = appointment.RequestedDate,
        Remarks = appointment.Remarks,
        Status = appointment.Status.ToString(),
        CreatedAt = appointment.CreatedAt
    };
}
