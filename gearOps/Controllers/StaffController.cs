using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using gearOps.Application.DTOs;
using gearOps.Application.Interfaces;

namespace gearOps.Controllers;

[ApiController]
[Route("api/v1/staff")]
[Authorize(Policy = "StaffOnly")]
public class StaffController : BaseApiController
{
    private readonly IStaffProfileService _profileService;
    private readonly IStaffScheduleService _scheduleService;
    private readonly IStaffServiceRecordService _serviceRecordService;
    private readonly IStaffReportService _reportService;
    private readonly IStaffCustomerService _customerService;
    private readonly ICustomerProfileService _customerProfileService;
    private readonly IStaffSalesService _salesService;
    private readonly IPartRequestService _partRequestService;
    private readonly IPartService _partService;

    public StaffController(
        IStaffProfileService profileService,
        IStaffScheduleService scheduleService,
        IStaffServiceRecordService serviceRecordService,
        IStaffReportService reportService,
        IStaffCustomerService customerService,
        ICustomerProfileService customerProfileService,
        IStaffSalesService salesService,
        IPartRequestService partRequestService,
        IPartService partService)
    {
        _profileService = profileService;
        _scheduleService = scheduleService;
        _serviceRecordService = serviceRecordService;
        _reportService = reportService;
        _customerService = customerService;
        _customerProfileService = customerProfileService;
        _salesService = salesService;
        _partRequestService = partRequestService;
        _partService = partService;
    }

    // ============= PROFILE ENDPOINTS =============

    /// <summary>Get staff member's profile</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var staffId = GetUserIdFromToken();
        var profile = await _profileService.GetProfileAsync(staffId);
        return Ok(profile);
    }

    /// <summary>Update staff member's profile</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateStaffProfileDto dto)
    {
        var staffId = GetUserIdFromToken();
        var profile = await _profileService.UpdateProfileAsync(staffId, dto);
        return Ok(profile);
    }

    // ============= CUSTOMER MANAGEMENT ENDPOINTS =============

    /// <summary>Register a new customer</summary>
    [HttpPost("customers")]
    public async Task<IActionResult> RegisterCustomer([FromBody] StaffRegisterCustomerDto dto)
    {
        var customer = await _customerService.RegisterCustomerAsync(dto);
        return CreatedAtAction(nameof(GetCustomer), new { customerId = customer.UserId }, customer);
    }

    /// <summary>Get customer by ID with vehicle details</summary>
    [HttpGet("customers/{customerId}")]
    public async Task<IActionResult> GetCustomer(int customerId)
    {
        var customer = await _customerService.GetCustomerByIdAsync(customerId);
        if (customer == null)
            return NotFound(new { message = "Customer not found" });
        return Ok(customer);
    }

    /// <summary>Get all customers (paginated)</summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetAllCustomers([FromQuery] PaginationParams paging)
    {
        var customers = await _customerService.GetAllCustomersAsync(paging);
        return Ok(customers);
    }

    /// <summary>Search customers by name, phone, ID, or vehicle number</summary>
    [HttpGet("customers/search")]
    public async Task<IActionResult> SearchCustomers([FromQuery] string q)
    {
        var customers = await _customerService.SearchCustomersAsync(q);
        return Ok(customers);
    }

    /// <summary>Add a vehicle to an existing customer</summary>
    [HttpPost("customers/{customerId}/vehicles")]
    public async Task<IActionResult> AddVehicleToCustomer(int customerId, [FromBody] CreateVehicleDto dto)
    {
        var vehicle = await _customerProfileService.AddVehicleAsync(customerId, dto);
        if (vehicle == null)
            return NotFound(new { message = "Customer not found" });

        return CreatedAtAction(nameof(GetVehicle), new { vehicleId = vehicle.VehicleId }, vehicle);
    }

    /// <summary>Get customer reports (top spenders, regulars, pending credits)</summary>
    [HttpGet("customers/reports")]
    public async Task<IActionResult> GetCustomerReports()
    {
        var report = await _customerService.GetCustomerReportsAsync();
        return Ok(report);
    }

    // ============= SALES INVOICE ENDPOINTS =============

    /// <summary>Create a sales invoice</summary>
    [HttpPost("sales-invoices")]
    public async Task<IActionResult> CreateSalesInvoice([FromBody] CreateSalesInvoiceDto dto)
    {
        var staffId = GetUserIdFromToken();
        var invoice = await _salesService.CreateSalesInvoiceAsync(staffId, dto);
        return CreatedAtAction(nameof(GetSalesInvoice), new { invoiceId = invoice.SalesInvoiceId }, invoice);
    }

    /// <summary>Get sales invoice by ID</summary>
    [HttpGet("sales-invoices/{invoiceId}")]
    public async Task<IActionResult> GetSalesInvoice(int invoiceId)
    {
        var invoice = await _salesService.GetSalesInvoiceByIdAsync(invoiceId);
        if (invoice == null)
            return NotFound(new { message = "Invoice not found" });
        return Ok(invoice);
    }

    /// <summary>Get all sales invoices (paginated)</summary>
    [HttpGet("sales-invoices")]
    public async Task<IActionResult> GetAllSalesInvoices([FromQuery] PaginationParams paging, [FromQuery] string? search)
    {
        var invoices = await _salesService.GetAllSalesInvoicesAsync(paging, search);
        return Ok(invoices);
    }

    /// <summary>Mark invoice as paid</summary>
    [HttpPatch("sales-invoices/{invoiceId}/pay")]
    public async Task<IActionResult> MarkInvoicePaid(int invoiceId)
    {
        await _salesService.MarkInvoicePaidAsync(invoiceId);
        return Ok(new { message = "Invoice marked as paid." });
    }

    /// <summary>Send invoice via email to customer</summary>
    [HttpPost("sales-invoices/{invoiceId}/email")]
    public async Task<IActionResult> SendInvoiceEmail(int invoiceId)
    {
        await _salesService.SendInvoiceEmailAsync(invoiceId);
        return Ok(new { message = "Invoice email sent successfully." });
    }

    // ============= SCHEDULE ENDPOINTS =============

    /// <summary>Get today's appointments</summary>
    [HttpGet("schedule/today")]
    public async Task<IActionResult> GetTodayAppointments()
    {
        var staffId = GetUserIdFromToken();
        var appointments = await _scheduleService.GetTodayAppointmentsAsync(staffId);
        return Ok(appointments);
    }

    /// <summary>Get upcoming appointments</summary>
    [HttpGet("schedule/upcoming")]
    public async Task<IActionResult> GetUpcomingAppointments()
    {
        var staffId = GetUserIdFromToken();
        var appointments = await _scheduleService.GetUpcomingAppointmentsAsync(staffId);
        return Ok(appointments);
    }

    /// <summary>Get all appointments</summary>
    [HttpGet("schedule/all")]
    public async Task<IActionResult> GetAllAppointments([FromQuery] PaginationParams paging)
    {
        var staffId = GetUserIdFromToken();
        var appointments = await _scheduleService.GetAllAppointmentsPagedAsync(staffId, paging);
        return Ok(appointments);
    }

    /// <summary>Get specific appointment details</summary>
    [HttpGet("schedule/{appointmentId}")]
    public async Task<IActionResult> GetAppointment(int appointmentId)
    {
        var appointment = await _scheduleService.GetAppointmentByIdAsync(appointmentId);
        if (appointment == null)
            return NotFound(new { message = "Appointment not found" });
        return Ok(appointment);
    }

    /// <summary>Get schedule summary</summary>
    [HttpGet("schedule/summary")]
    public async Task<IActionResult> GetScheduleSummary()
    {
        var staffId = GetUserIdFromToken();
        var summary = await _scheduleService.GetScheduleSummaryAsync(staffId);
        return Ok(summary);
    }

    /// <summary>Get available time slots for a specific date (10 AM - 5 PM, 1-hour intervals, break 1-2 PM)</summary>
    [AllowAnonymous]
    [HttpGet("appointments/available-slots")]
    public async Task<IActionResult> GetAvailableTimeSlots([FromQuery] DateTime date)
    {
        if (date == default)
            return BadRequest(new { message = "Date is required (format: yyyy-MM-dd)" });

        var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var slots = await _scheduleService.GetAvailableTimeSlotsAsync(utcDate);
        return Ok(slots);
    }

    /// <summary>Approve a pending appointment</summary>
    [HttpPost("appointments/{appointmentId}/approve")]
    public async Task<IActionResult> ApproveAppointment(int appointmentId, [FromBody] AppointmentDecisionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Decision) || dto.Decision.ToLower() != "approved")
            return BadRequest(new { message = "Decision must be 'Approved'" });

        var staffId = GetUserIdFromToken();
        var result = await _scheduleService.ApproveAppointmentAsync(staffId, appointmentId, dto.Notes);
        return Ok(result);
    }

    /// <summary>Reject a pending appointment</summary>
    [HttpPost("appointments/{appointmentId}/reject")]
    public async Task<IActionResult> RejectAppointment(int appointmentId, [FromBody] AppointmentDecisionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Decision) || dto.Decision.ToLower() != "rejected")
            return BadRequest(new { message = "Decision must be 'Rejected'" });

        if (string.IsNullOrWhiteSpace(dto.Notes))
            return BadRequest(new { message = "Rejection reason is required" });

        var staffId = GetUserIdFromToken();
        var result = await _scheduleService.RejectAppointmentAsync(staffId, appointmentId, dto.Notes);
        return Ok(result);
    }

    /// <summary>Mark a confirmed appointment as no-show</summary>
    [HttpPost("appointments/{appointmentId}/no-show")]
    public async Task<IActionResult> MarkAppointmentAsNoShow(int appointmentId, [FromBody] AppointmentDecisionDto dto)
    {
        var staffId = GetUserIdFromToken();
        var result = await _scheduleService.MarkAppointmentAsNoShowAsync(staffId, appointmentId, dto.Notes);
        return Ok(result);
    }

    /// <summary>Reschedule an appointment to a different date</summary>
    [HttpPost("appointments/{appointmentId}/reschedule")]
    public async Task<IActionResult> RescheduleAppointment(int appointmentId, [FromBody] RescheduleAppointmentDto dto)
    {
        if (dto.NewDate == default)
            return BadRequest(new { message = "NewDate is required (format: yyyy-MM-dd)" });

        var staffId = GetUserIdFromToken();
        var result = await _scheduleService.RescheduleAppointmentAsync(staffId, appointmentId, dto.NewDate);
        return Ok(result);
    }

    /// <summary>Mark a confirmed appointment as completed</summary>
    [HttpPost("appointments/{appointmentId}/complete")]
    public async Task<IActionResult> CompleteAppointment(int appointmentId)
    {
        var staffId = GetUserIdFromToken();
        var result = await _scheduleService.CompleteAppointmentAsync(staffId, appointmentId);
        return Ok(result);
    }

    // ============= SERVICE RECORD ENDPOINTS =============

    /// <summary>Create a service record for a Confirmed or Completed appointment.
    /// ServiceCost may be 0 at creation and updated later via PUT.</summary>
    [HttpPost("service-records")]
    public async Task<IActionResult> CreateServiceRecord([FromBody] CreateServiceRecordDto dto)
    {
        var staffId = GetUserIdFromToken();
        var serviceRecord = await _serviceRecordService.CreateServiceRecordAsync(staffId, dto);
        return CreatedAtAction(nameof(GetServiceRecord), new { serviceRecordId = serviceRecord.ServiceRecordId }, serviceRecord);
    }

    /// <summary>Update a service record (description and/or cost)</summary>
    [HttpPut("service-records/{serviceRecordId}")]
    public async Task<IActionResult> UpdateServiceRecord(int serviceRecordId, [FromBody] UpdateServiceRecordDto dto)
    {
        var staffId = GetUserIdFromToken();
        dto.ServiceRecordId = serviceRecordId;
        var serviceRecord = await _serviceRecordService.UpdateServiceRecordAsync(staffId, dto);
        return Ok(serviceRecord);
    }

    /// <summary>Get specific service record details</summary>
    [HttpGet("service-records/{serviceRecordId}")]
    public async Task<IActionResult> GetServiceRecord(int serviceRecordId)
    {
        var serviceRecord = await _serviceRecordService.GetServiceRecordByIdAsync(serviceRecordId);
        if (serviceRecord == null)
            return NotFound(new { message = "Service record not found" });
        return Ok(serviceRecord);
    }

    /// <summary>Get all service records across all staff (used to link labor to invoices by appointmentId)</summary>
    [HttpGet("service-records")]
    public async Task<IActionResult> GetServiceRecords([FromQuery] PaginationParams paging)
    {
        var serviceRecords = await _serviceRecordService.GetAllServiceRecordsAsync(paging);
        return Ok(serviceRecords);
    }

    /// <summary>Get service records for a specific month (scoped to the calling staff member)</summary>
    [HttpGet("service-records/monthly/{year}/{month}")]
    public async Task<IActionResult> GetMonthlyServiceRecords(int year, int month)
    {
        if (month < 1 || month > 12)
            return BadRequest(new { message = "Month must be between 1 and 12." });

        var staffId = GetUserIdFromToken();
        var serviceRecords = await _serviceRecordService.GetMonthlyServiceRecordsAsync(staffId, year, month);
        return Ok(serviceRecords);
    }

    // ============= REPORT ENDPOINTS =============

    /// <summary>Get performance report (all-time statistics)</summary>
    [HttpGet("reports/performance")]
    public async Task<IActionResult> GetPerformanceReport()
    {
        var staffId = GetUserIdFromToken();
        var report = await _reportService.GetPerformanceReportAsync(staffId);
        return Ok(report);
    }

    /// <summary>Get monthly report for a specific month</summary>
    [HttpGet("reports/monthly/{year}/{month}")]
    public async Task<IActionResult> GetMonthlyReport(int year, int month)
    {
        if (month < 1 || month > 12)
            return BadRequest(new { message = "Month must be between 1 and 12." });

        var staffId = GetUserIdFromToken();
        var report = await _reportService.GetMonthlyReportAsync(staffId, year, month);
        return Ok(report);
    }

    // ============= DASHBOARD ENDPOINTS =============

    /// <summary>Get comprehensive staff dashboard</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var staffId = GetUserIdFromToken();
        var dashboard = await _reportService.GetDashboardAsync(staffId);
        return Ok(dashboard);
    }

    // ============= PART CATALOG ENDPOINTS =============

    /// <summary>Get all parts in inventory</summary>
    [HttpGet("parts")]
    public async Task<IActionResult> GetParts([FromQuery] PaginationParams paging)
    {
        var parts = await _partService.GetAllPartsAsync(paging);
        return Ok(parts);
    }

    /// <summary>Get parts by category</summary>
    [HttpGet("parts/category/{category}")]
    public async Task<IActionResult> GetPartsByCategory(string category, [FromQuery] PaginationParams paging)
    {
        var parts = await _partService.GetPartsByCategoryAsync(category, paging);
        return Ok(parts);
    }

    /// <summary>Search parts in inventory</summary>
    [HttpGet("parts/search")]
    public async Task<IActionResult> SearchParts([FromQuery] string q, [FromQuery] PaginationParams paging)
    {
        var parts = await _partService.SearchPartsAsync(q, paging);
        return Ok(parts);
    }

    // ============= PART REQUEST REVIEW ENDPOINTS =============

    /// <summary>Get all part requests for review</summary>
    [HttpGet("part-requests")]
    public async Task<IActionResult> GetPartRequests([FromQuery] PaginationParams paging, [FromQuery] string? status = null)
    {
        var partRequests = await _partRequestService.GetAllPartRequestsPagedAsync(paging, status);
        return Ok(partRequests);
    }

    /// <summary>Get pending part requests for review</summary>
    [HttpGet("part-requests/pending")]
    public async Task<IActionResult> GetPendingPartRequests()
    {
        var partRequests = await _partRequestService.GetPendingPartRequestsForReviewAsync();
        return Ok(partRequests);
    }

    /// <summary>Approve a part request and mark it available</summary>
    [HttpPatch("part-requests/{partRequestId:int}/approve")]
    public async Task<IActionResult> ApprovePartRequest(int partRequestId, [FromBody] ReviewPartRequestDto dto)
    {
        var staffUserId = GetUserIdFromToken();
        var partRequest = await _partRequestService.ApprovePartRequestAsync(staffUserId, partRequestId, dto);
        if (partRequest == null)
            return NotFound(new { message = "Part request not found" });

        return Ok(new
        {
            message = "Part request approved and marked available.",
            data = partRequest
        });
    }

    /// <summary>Reject a part request</summary>
    [HttpPatch("part-requests/{partRequestId:int}/reject")]
    public async Task<IActionResult> RejectPartRequest(int partRequestId, [FromBody] ReviewPartRequestDto dto)
    {
        var staffUserId = GetUserIdFromToken();
        var partRequest = await _partRequestService.RejectPartRequestAsync(staffUserId, partRequestId, dto);
        if (partRequest == null)
            return NotFound(new { message = "Part request not found" });

        return Ok(new
        {
            message = "Part request rejected.",
            data = partRequest
        });
    }

    // ============= VEHICLE ENDPOINTS =============

    /// <summary>Get all vehicles with customer info</summary>
    [HttpGet("vehicles")]
    public async Task<IActionResult> GetAllVehicles()
    {
        var customers = await _customerService.GetAllCustomersAsync();
        var vehicles = customers
            .SelectMany(c => c.Vehicles.Select(v => new
            {
                v.VehicleId,
                v.VehicleNumber,
                v.Brand,
                v.Model,
                v.Year,
                CustomerName = c.FullName,
                CustomerPhone = c.Phone
            }))
            .ToList();
        return Ok(vehicles);
    }

    /// <summary>Get specific vehicle details</summary>
    [HttpGet("vehicles/{vehicleId}")]
    public async Task<IActionResult> GetVehicle(int vehicleId)
    {
        var vehicle = await _customerProfileService.GetVehicleByIdAsync(vehicleId);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });
        return Ok(vehicle);
    }

    // GetUserIdFromToken() inherited from BaseApiController
}
