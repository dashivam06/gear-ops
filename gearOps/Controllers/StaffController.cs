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
public class StaffController : ControllerBase
{
    private readonly IStaffProfileService _profileService;
    private readonly IStaffScheduleService _scheduleService;
    private readonly IStaffServiceRecordService _serviceRecordService;
    private readonly IStaffReportService _reportService;
    private readonly IStaffCustomerService _customerService;
    private readonly IStaffSalesService _salesService;

    public StaffController(
        IStaffProfileService profileService,
        IStaffScheduleService scheduleService,
        IStaffServiceRecordService serviceRecordService,
        IStaffReportService reportService,
        IStaffCustomerService customerService,
        IStaffSalesService salesService)
    {
        _profileService = profileService;
        _scheduleService = scheduleService;
        _serviceRecordService = serviceRecordService;
        _reportService = reportService;
        _customerService = customerService;
        _salesService = salesService;
    }

    // ============= PROFILE ENDPOINTS =============

    /// <summary>Get staff member's profile</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var staffId = GetStaffIdFromToken();
        var profile = await _profileService.GetProfileAsync(staffId);
        return Ok(profile);
    }

    /// <summary>Update staff member's profile</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateStaffProfileDto dto)
    {
        var staffId = GetStaffIdFromToken();
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
        var staffId = GetStaffIdFromToken();
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
    public async Task<IActionResult> GetAllSalesInvoices([FromQuery] PaginationParams paging)
    {
        var invoices = await _salesService.GetAllSalesInvoicesAsync(paging);
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
        var staffId = GetStaffIdFromToken();
        var appointments = await _scheduleService.GetTodayAppointmentsAsync(staffId);
        return Ok(appointments);
    }

    /// <summary>Get upcoming appointments</summary>
    [HttpGet("schedule/upcoming")]
    public async Task<IActionResult> GetUpcomingAppointments()
    {
        var staffId = GetStaffIdFromToken();
        var appointments = await _scheduleService.GetUpcomingAppointmentsAsync(staffId);
        return Ok(appointments);
    }

    /// <summary>Get all appointments</summary>
    [HttpGet("schedule/all")]
    public async Task<IActionResult> GetAllAppointments()
    {
        var staffId = GetStaffIdFromToken();
        var appointments = await _scheduleService.GetAllAppointmentsAsync(staffId);
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
        var staffId = GetStaffIdFromToken();
        var summary = await _scheduleService.GetScheduleSummaryAsync(staffId);
        return Ok(summary);
    }

    // ============= SERVICE RECORD ENDPOINTS =============

    /// <summary>Complete an appointment by creating a service record</summary>
    [HttpPost("service-records")]
    public async Task<IActionResult> CreateServiceRecord([FromBody] CompleteAppointmentDto dto)
    {
        var staffId = GetStaffIdFromToken();
        var serviceRecord = await _serviceRecordService.CreateServiceRecordAsync(staffId, dto);
        return CreatedAtAction(nameof(GetServiceRecord), new { serviceRecordId = serviceRecord.ServiceRecordId }, serviceRecord);
    }

    /// <summary>Update a service record</summary>
    [HttpPut("service-records/{serviceRecordId}")]
    public async Task<IActionResult> UpdateServiceRecord(int serviceRecordId, [FromBody] UpdateServiceRecordDto dto)
    {
        var staffId = GetStaffIdFromToken();
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

    /// <summary>Get all service records by staff member</summary>
    [HttpGet("service-records")]
    public async Task<IActionResult> GetServiceRecords()
    {
        var staffId = GetStaffIdFromToken();
        var serviceRecords = await _serviceRecordService.GetStaffServiceRecordsAsync(staffId);
        return Ok(serviceRecords);
    }

    /// <summary>Get service records for a specific month</summary>
    [HttpGet("service-records/monthly/{year}/{month}")]
    public async Task<IActionResult> GetMonthlyServiceRecords(int year, int month)
    {
        if (month < 1 || month > 12)
            return BadRequest(new { message = "Month must be between 1 and 12." });

        var staffId = GetStaffIdFromToken();
        var serviceRecords = await _serviceRecordService.GetMonthlyServiceRecordsAsync(staffId, year, month);
        return Ok(serviceRecords);
    }

    // ============= REPORT ENDPOINTS =============

    /// <summary>Get performance report (all-time statistics)</summary>
    [HttpGet("reports/performance")]
    public async Task<IActionResult> GetPerformanceReport()
    {
        var staffId = GetStaffIdFromToken();
        var report = await _reportService.GetPerformanceReportAsync(staffId);
        return Ok(report);
    }

    /// <summary>Get monthly report for a specific month</summary>
    [HttpGet("reports/monthly/{year}/{month}")]
    public async Task<IActionResult> GetMonthlyReport(int year, int month)
    {
        if (month < 1 || month > 12)
            return BadRequest(new { message = "Month must be between 1 and 12." });

        var staffId = GetStaffIdFromToken();
        var report = await _reportService.GetMonthlyReportAsync(staffId, year, month);
        return Ok(report);
    }

    // ============= DASHBOARD ENDPOINTS =============

    /// <summary>Get comprehensive staff dashboard</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var staffId = GetStaffIdFromToken();
        var dashboard = await _reportService.GetDashboardAsync(staffId);
        return Ok(dashboard);
    }

    // ============= HELPER METHODS =============

    private int GetStaffIdFromToken()
    {
        var staffIdClaim = User.FindFirst("sub") ?? User.FindFirst("nameid");
        if (staffIdClaim == null || !int.TryParse(staffIdClaim.Value, out var staffId))
            throw new UnauthorizedAccessException("Staff ID not found in token");
        return staffId;
    }
}
