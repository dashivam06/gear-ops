using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using gearOps.Application.DTOs;
using gearOps.Application.Interfaces;

namespace gearOps.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize(Policy = "CustomerOnly")]
public class CustomerController : BaseApiController
{
    private readonly ICustomerProfileService _profileService;
    private readonly IAppointmentService _appointmentService;
    private readonly IReviewService _reviewService;
    private readonly IPartRequestService _partRequestService;
    private readonly IPartService _partService;
    private readonly IPurchaseHistoryService _purchaseHistoryService;
    private readonly ILoyaltyProgramService _loyaltyService;
    private readonly IInvoicePdfService _pdfService;
    private readonly ICreditService _creditService;

    public CustomerController(
        ICustomerProfileService profileService,
        IAppointmentService appointmentService,
        IReviewService reviewService,
        IPartRequestService partRequestService,
        IPartService partService,
        IPurchaseHistoryService purchaseHistoryService,
        ILoyaltyProgramService loyaltyService,
        IInvoicePdfService pdfService,
        ICreditService creditService)
    {
        _profileService = profileService;
        _appointmentService = appointmentService;
        _reviewService = reviewService;
        _partRequestService = partRequestService;
        _partService = partService;
        _purchaseHistoryService = purchaseHistoryService;
        _loyaltyService = loyaltyService;
        _pdfService = pdfService;
        _creditService = creditService;
    }

    // ============= PROFILE ENDPOINTS =============
    
    /// <summary>
    /// Get customer profile with all vehicles
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserIdFromToken();
        var profile = await _profileService.GetProfileAsync(userId);
        return Ok(profile);
    }

    /// <summary>
    /// Update customer profile
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateCustomerProfileDto dto)
    {
        var userId = GetUserIdFromToken();
        var profile = await _profileService.UpdateProfileAsync(userId, dto);
        return Ok(profile);
    }

    // ============= VEHICLE ENDPOINTS =============

    /// <summary>
    /// Add a new vehicle
    /// </summary>
    [HttpPost("vehicles")]
    public async Task<IActionResult> AddVehicle([FromBody] CreateVehicleDto dto)
    {
        var userId = GetUserIdFromToken();
        var vehicle = await _profileService.AddVehicleAsync(userId, dto);
        return CreatedAtAction(nameof(GetVehicle), new { vehicleId = vehicle.VehicleId }, vehicle);
    }

    /// <summary>
    /// Update vehicle details
    /// </summary>
    [HttpPut("vehicles/{vehicleId}")]
    public async Task<IActionResult> UpdateVehicle(int vehicleId, [FromBody] UpdateVehicleDto dto)
    {
        var userId = GetUserIdFromToken();
        dto.VehicleId = vehicleId;
        var vehicle = await _profileService.UpdateVehicleAsync(userId, dto);
        return Ok(vehicle);
    }

    /// <summary>
    /// Get all customer vehicles
    /// </summary>
    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles()
    {
        var userId = GetUserIdFromToken();
        var vehicles = await _profileService.GetCustomerVehiclesAsync(userId);
        return Ok(vehicles);
    }

    /// <summary>
    /// Get specific vehicle details
    /// </summary>
    [HttpGet("vehicles/{vehicleId}")]
    public async Task<IActionResult> GetVehicle(int vehicleId)
    {
        var vehicle = await _profileService.GetVehicleByIdAsync(vehicleId);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });
        return Ok(vehicle);
    }

    /// <summary>
    /// Delete a vehicle
    /// </summary>
    [HttpDelete("vehicles/{vehicleId}")]
    public async Task<IActionResult> DeleteVehicle(int vehicleId)
    {
        var userId = GetUserIdFromToken();
        var result = await _profileService.DeleteVehicleAsync(userId, vehicleId);
        return Ok(new { message = "Vehicle removed from active profile. Historical records are preserved." });
    }

    // ============= APPOINTMENT ENDPOINTS =============
    
    /// <summary>
    /// Book a new appointment
    /// </summary>
    [HttpPost("appointments")]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        var userId = GetUserIdFromToken();
        var appointment = await _appointmentService.CreateAppointmentAsync(userId, dto);
        return CreatedAtAction(nameof(GetAppointment), new { appointmentId = appointment.AppointmentId }, appointment);
    }

    /// <summary>
    /// Update appointment details
    /// </summary>
    [HttpPut("appointments/{appointmentId}")]
    public async Task<IActionResult> UpdateAppointment(int appointmentId, [FromBody] UpdateAppointmentDto dto)
    {
        var userId = GetUserIdFromToken();
        dto.AppointmentId = appointmentId;
        var appointment = await _appointmentService.UpdateAppointmentAsync(userId, dto);
        return Ok(appointment);
    }

    /// <summary>
    /// Get specific appointment details
    /// </summary>
    [HttpGet("appointments/{appointmentId}")]
    public async Task<IActionResult> GetAppointment(int appointmentId)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
        if (appointment == null)
            return NotFound(new { message = "Appointment not found" });
        return Ok(appointment);
    }

    /// <summary>
    /// Get all customer appointments
    /// </summary>
    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments()
    {
        var userId = GetUserIdFromToken();
        var appointments = await _appointmentService.GetCustomerAppointmentsAsync(userId);
        return Ok(appointments);
    }

    /// <summary>
    /// Get upcoming appointments only
    /// </summary>
    [HttpGet("appointments/upcoming")]
    public async Task<IActionResult> GetUpcomingAppointments()
    {
        var userId = GetUserIdFromToken();
        var appointments = await _appointmentService.GetUpcomingAppointmentsAsync(userId);
        return Ok(appointments);
    }

    /// <summary>
    /// Cancel an appointment (only Pending or Confirmed; must belong to the caller)
    /// </summary>
    [HttpDelete("appointments/{appointmentId}")]
    public async Task<IActionResult> CancelAppointment(int appointmentId)
    {
        var userId = GetUserIdFromToken();
        await _appointmentService.CancelAppointmentAsync(appointmentId, userId);
        return Ok(new { success = true, message = "Appointment cancelled successfully" });
    }

    // ============= REVIEW ENDPOINTS =============
    
    /// <summary>
    /// Submit a review for a completed service
    /// </summary>
    [HttpPost("reviews")]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        var userId = GetUserIdFromToken();
        var review = await _reviewService.CreateReviewAsync(userId, dto);
        return CreatedAtAction(nameof(GetReview), new { reviewId = review.ReviewId }, review);
    }

    /// <summary>
    /// Get specific review details
    /// </summary>
    [HttpGet("reviews/{reviewId}")]
    public async Task<IActionResult> GetReview(int reviewId)
    {
        var review = await _reviewService.GetReviewByIdAsync(reviewId);
        if (review == null)
            return NotFound(new { message = "Review not found" });
        return Ok(review);
    }

    /// <summary>
    /// Get all customer reviews
    /// </summary>
    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviews()
    {
        var userId = GetUserIdFromToken();
        var reviews = await _reviewService.GetCustomerReviewsAsync(userId);
        return Ok(reviews);
    }

    /// <summary>
    /// Delete a review
    /// </summary>
    [HttpDelete("reviews/{reviewId}")]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        var result = await _reviewService.DeleteReviewAsync(reviewId);
        return Ok(new { message = "Review removed successfully" });
    }

    // ============= PART CATALOG ENDPOINTS =============

    /// <summary>
    /// Browse all available parts
    /// </summary>
    [HttpGet("parts")]
    public async Task<IActionResult> GetParts([FromQuery] PaginationParams paging)
    {
        var parts = await _partService.GetAllPartsAsync(paging);
        return Ok(parts);
    }

    /// <summary>
    /// Browse parts by category
    /// </summary>
    [HttpGet("parts/category/{category}")]
    public async Task<IActionResult> GetPartsByCategory(string category, [FromQuery] PaginationParams paging)
    {
        var parts = await _partService.GetPartsByCategoryAsync(category, paging);
        return Ok(parts);
    }

    /// <summary>
    /// Search parts by keyword
    /// </summary>
    [HttpGet("parts/search")]
    public async Task<IActionResult> SearchParts([FromQuery] string q, [FromQuery] PaginationParams paging)
    {
        var parts = await _partService.SearchPartsAsync(q, paging);
        return Ok(parts);
    }

    // ============= PART REQUEST ENDPOINTS =============
    
    /// <summary>
    /// Request a part that is currently unavailable
    /// </summary>
    [HttpPost("part-requests")]
    public async Task<IActionResult> CreatePartRequest([FromBody] CreatePartRequestDto dto)
    {
        var userId = GetUserIdFromToken();
        var partRequest = await _partRequestService.CreatePartRequestAsync(userId, dto);
        return CreatedAtAction(nameof(GetPartRequest), new { partRequestId = partRequest.PartRequestId }, partRequest);
    }

    /// <summary>
    /// Get specific part request details
    /// </summary>
    [HttpGet("part-requests/{partRequestId}")]
    public async Task<IActionResult> GetPartRequest(int partRequestId)
    {
        var partRequest = await _partRequestService.GetPartRequestByIdAsync(partRequestId);
        if (partRequest == null)
            return NotFound(new { message = "Part request not found" });
        return Ok(partRequest);
    }

    /// <summary>
    /// Get all customer part requests
    /// </summary>
    [HttpGet("part-requests")]
    public async Task<IActionResult> GetPartRequests()
    {
        var userId = GetUserIdFromToken();
        var partRequests = await _partRequestService.GetCustomerPartRequestsAsync(userId);
        return Ok(partRequests);
    }

    /// <summary>
    /// Get pending part requests only
    /// </summary>
    [HttpGet("part-requests/pending")]
    public async Task<IActionResult> GetPendingPartRequests()
    {
        var userId = GetUserIdFromToken();
        var partRequests = await _partRequestService.GetPendingPartRequestsAsync(userId);
        return Ok(partRequests);
    }

    // ============= PURCHASE HISTORY ENDPOINTS =============
    
    /// <summary>
    /// Buy parts directly
    /// </summary>
    [HttpPost("purchase-parts")]
    public async Task<IActionResult> PurchaseParts([FromBody] DirectBuyPartsDto dto)
    {
        var userId = GetUserIdFromToken();
        var invoice = await _purchaseHistoryService.BuyPartsDirectlyAsync(userId, dto);
        return Ok(invoice);
    }

    /// <summary>
    /// Get all purchase invoices
    /// </summary>
    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchaseHistory()
    {
        var userId = GetUserIdFromToken();
        var invoices = await _purchaseHistoryService.GetCustomerPurchaseHistoryAsync(userId);
        return Ok(invoices);
    }

    /// <summary>
    /// Get specific invoice details
    /// </summary>
    [HttpGet("purchases/{invoiceId}")]
    public async Task<IActionResult> GetInvoiceDetails(int invoiceId)
    {
        var invoice = await _purchaseHistoryService.GetInvoiceDetailsAsync(invoiceId);
        if (invoice == null)
            return NotFound(new { message = "Invoice not found" });
        return Ok(invoice);
    }

    // ============= SERVICE HISTORY ENDPOINTS =============
    
    /// <summary>
    /// Get all service records
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServiceHistory()
    {
        var userId = GetUserIdFromToken();
        var services = await _purchaseHistoryService.GetCustomerServiceHistoryAsync(userId);
        return Ok(services);
    }

    // ============= HISTORY SUMMARY ENDPOINTS =============
    
    /// <summary>
    /// Get comprehensive history summary
    /// </summary>
    [HttpGet("history-summary")]
    public async Task<IActionResult> GetHistorySummary()
    {
        var userId = GetUserIdFromToken();
        var summary = await _purchaseHistoryService.GetCustomerHistorySummaryAsync(userId);
        return Ok(summary);
    }

    // ============= DASHBOARD ENDPOINTS =============
    
    /// <summary>
    /// Get comprehensive customer dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetUserIdFromToken();
        
        var profile = await _profileService.GetProfileAsync(userId);
        var credits = await _creditService.GetCreditBalanceAsync(userId);
        var loyalty = await _loyaltyService.GetLoyaltyStatusAsync(userId);
        var appointments = await _appointmentService.GetUpcomingAppointmentsAsync(userId);
        var partRequests = await _partRequestService.GetPendingPartRequestsAsync(userId);

        var dashboard = new CustomerDashboardDto
        {
            Profile = profile,
            CreditBalance = credits,
            LoyaltyStatus = loyalty,
            UpcomingAppointments = appointments,
            PendingPartRequests = partRequests
        };


        return Ok(dashboard);
    }

    // ============= LOYALTY ENDPOINTS =============
    
    /// <summary>
    /// Get loyalty program status and details
    /// </summary>
    [HttpGet("loyalty")]
    public async Task<IActionResult> GetLoyaltyStatus()
    {
        var userId = GetUserIdFromToken();
        var status = await _loyaltyService.GetLoyaltyStatusAsync(userId);
        return Ok(status);
    }

    // ============= CREDIT ENDPOINTS =============
    
    /// <summary>
    /// Get credit balance and information
    /// </summary>
    [HttpGet("credits")]
    public async Task<IActionResult> GetCreditBalance()
    {
        var userId = GetUserIdFromToken();
        var credits = await _creditService.GetCreditBalanceAsync(userId);
        return Ok(credits);
    }

    /// <summary>
    /// Get overdue credits
    /// </summary>
    [HttpGet("credits/overdue")]
    public async Task<IActionResult> GetOverdueCredits([FromQuery] int daysOverdue = 30)
    {
        var userId = GetUserIdFromToken();
        var overdueCredits = await _creditService.GetOverdueCreditsAsync(userId, daysOverdue);
        return Ok(overdueCredits);
    }

    // ============= PDF INVOICE ENDPOINTS =============
    
    /// <summary>
    /// Generate and download invoice as PDF
    /// </summary>
    [HttpGet("invoices/{invoiceId}/pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(int invoiceId)
    {
        var userId = GetUserIdFromToken();
        var pdfResponse = await _pdfService.GenerateInvoicePdfAsync(invoiceId, userId);
        
        return File(
            pdfResponse.PdfBytes,
            pdfResponse.ContentType,
            pdfResponse.FileName
        );
    }

    // ============= HELPER METHODS =============
    
    /// <summary>
    /// Extract user ID from JWT token
    /// </summary>
    // GetUserIdFromToken() inherited from BaseApiController
}
