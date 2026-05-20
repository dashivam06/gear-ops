using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Helpers;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Domain.Enums;
using gearOps.Infrastructure.Data;
using gearOps.Infrastructure.Extensions;

namespace gearOps.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : BaseApiController
{
    private readonly IStaffService _staffService;
    private readonly IPartService _partService;
    private readonly IVendorService _vendorService;
    private readonly IPurchaseInvoiceService _purchaseInvoiceService;
    private readonly IReportService _reportService;
    private readonly IScheduledNotificationService _scheduledNotificationService;
    private readonly IPartRequestService _partRequestService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly AppDbContext _db;

    public AdminController(
        IStaffService staffService,
        IPartService partService,
        IVendorService vendorService,
        IPurchaseInvoiceService purchaseInvoiceService,
        IReportService reportService,
        IScheduledNotificationService scheduledNotificationService,
        IPartRequestService partRequestService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        AppDbContext db)
    {
        _staffService = staffService;
        _partService = partService;
        _vendorService = vendorService;
        _purchaseInvoiceService = purchaseInvoiceService;
        _reportService = reportService;
        _scheduledNotificationService = scheduledNotificationService;
        _partRequestService = partRequestService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _db = db;
    }

    #region 1. Admin Profile & Settings
    /// <summary>Retrieve Administrator Profile</summary>
    /// <remarks>Fetches the associated data for the currently authenticated admin account, primarily used for dashboard renderings.</remarks>
    /// <response code="200">The profile data was retrieved successfully.</response>
    /// <response code="401">Unauthorized if the token is missing or invalid.</response>
    /// <response code="404">If the admin profile is unexpectedly not found.</response>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(AdminProfileResponseDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAdminProfile()
    {
        var admin = await GetCurrentAdminAsync();
        return Ok(MapAdminProfile(admin));
    }

    /// <summary>Update Administrator Profile</summary>
    /// <remarks>Applies modifications to the existing admin account data (name, phone, address, and profile image URL).</remarks>
    /// <param name="dto">The expected payload containing the profile fields that should update.</param>
    /// <response code="200">Returns the fully updated admin profile entity.</response>
    /// <response code="400">If the provided model/payload format is invalid.</response>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(AdminProfileResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateAdminProfile([FromBody] UpdateAdminProfileDto dto)
    {
        var admin = await GetCurrentAdminAsync();
        admin.FullName = dto.FullName;
        admin.Phone = dto.Phone;
        admin.Address = dto.Address;
        admin.ProfileImageUrl = dto.ProfileImageUrl;

        await _db.SaveChangesAsync();
        return Ok(MapAdminProfile(admin));
    }

    /// <summary>Modify Administrator Password</summary>
    /// <remarks>Securely authenticates the current password signature and establishes a newly-requested password.</remarks>
    /// <param name="dto">The JSON object detailing current and future password rules.</param>
    /// <response code="200">Password reset was completely successful.</response>
    /// <response code="400">Validation constraint trigger (e.g. constraints mismatch or confirm failed).</response>
    /// <response code="401">The current password failed the verification test.</response>
    [HttpPut("profile/password")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ChangeAdminPassword([FromBody] ChangePasswordDto dto)
    {
        var admin = await GetCurrentAdminAsync();

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, admin.PasswordHash))
            throw new AdminPasswordVerificationFailedException("Current password is incorrect.");

        if (dto.NewPassword != dto.ConfirmPassword)
            throw new BadRequestException("New password and confirm password do not match.");

        PasswordValidator.ValidateOrThrow(dto.NewPassword);
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { changed = true });
    }

    /// <summary>Deactivate Administrator Account</summary>
    /// <remarks>Soft-deletes the current admin. This irreversible phase requires manual password approval before proceeding.</remarks>
    /// <param name="dto">Confirmation payload requiring the user's password string.</param>
    /// <response code="200">The current administrator identity was soft-deleted.</response>
    /// <response code="401">Incorrect password supplied.</response>
    [HttpDelete("profile")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> DeleteAdminAccount([FromBody] DeleteAccountDto dto)
    {
        var admin = await GetCurrentAdminAsync();

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, admin.PasswordHash))
            throw new AdminPasswordVerificationFailedException("Password confirmation failed.");

        admin.IsDeleted = true;
        admin.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { deleted = true });
    }

    #endregion

    #region 2. Staff Management
    /// <summary>Register New Staff Member</summary>
    /// <remarks>Provisions a new staff user role on the platform and issues welcome emails natively. Passwords are automatically randomized if missing.</remarks>
    /// <param name="dto">The new staff details.</param>
    /// <response code="201">Created successfully and returns referencing resource ID.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="409">Email address already exists in system records.</response>
    [HttpPost("staff")]
    [ProducesResponseType(typeof(StaffResponseDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto dto)
    {
        var result = await _staffService.CreateStaffAsync(dto);
        return CreatedAtAction(nameof(GetStaffById), new { staffId = result.StaffId }, result);
    }

    /// <summary>Modify Staff Details</summary>
    /// <remarks>Alters existing configurations for a specified internal staff member.</remarks>
    /// <param name="staffId">The numeric identifier referencing the specific staff record.</param>
    /// <param name="dto">The updating payload container.</param>
    /// <response code="200">Returned the completely morphed entity output post database commits.</response>
    /// <response code="404">No matching staff configuration tracked within system.</response>
    /// <response code="409">Attempting to alter the ID matching conflicts with global unique scopes.</response>
    [HttpPut("staff/{staffId:int}")]
    [ProducesResponseType(typeof(StaffResponseDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> UpdateStaff(int staffId, [FromBody] UpdateStaffDto dto)
    {
        dto.StaffId = staffId;
        var result = await _staffService.UpdateStaffAsync(dto);
        return Ok(result);
    }

    /// <summary>Fetch Specific Staff Account</summary>
    /// <remarks>Looks up and formats output payload models representing the selected specific staff entity identifier.</remarks>
    /// <param name="staffId">Database generated system identifier.</param>
    /// <response code="200">Retrieves the accurate individual format object safely.</response>
    /// <response code="404">System was entirely unable to locate referenced primary target.</response>
    [HttpGet("staff/{staffId:int}")]
    [ProducesResponseType(typeof(StaffResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetStaffById(int staffId)
    {
        var result = await _staffService.GetStaffByIdAsync(staffId);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>List Paged Staff Accounts</summary>
    /// <remarks>Fetches universally defined operational staff data and securely paginates internal collection.</remarks>
    /// <param name="paging">Controls index offsetting and max query page depth sizes.</param>
    /// <response code="200">Generates safely normalized list alongside total counting attributes.</response>
    [HttpGet("staff")]
    [ProducesResponseType(typeof(PagedResult<StaffResponseDto>), 200)]
    public async Task<IActionResult> GetAllStaff([FromQuery] PaginationParams paging)
    {
        var result = await _staffService.GetAllStaffAsync(paging);
        return Ok(result);
    }

    /// <summary>Soft Delete Staff Entity</summary>
    /// <remarks>Purges internal system active lists, hides all UI elements from routing to them seamlessly behind soft delete tokens.</remarks>
    /// <param name="staffId">Numeric referenced user id format to hide.</param>
    /// <response code="200">The process resolved efficiently without database errors.</response>
    /// <response code="404">Entity referenced couldn't be discovered.</response>
    [HttpDelete("staff/{staffId:int}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteStaff(int staffId)
    {
        await _staffService.DeleteStaffAsync(staffId);
        return Ok(new { message = "Staff member marked as inactive and hidden from active staff lists." });
    }




    /// <summary>Switch Staff On/Off System Active States</summary>
    /// <remarks>Reconfigures permissions across access control arrays turning accounts valid/invalid momentarily.</remarks>
    /// <param name="staffId">Specific parameter for ID matching logic mapping.</param>
    /// <param name="dto">The expected payload handling flags.</param>
    /// <response code="200">System correctly synchronized permission values dynamically.</response>
    /// <response code="404">Internal mismatch handling failed entity search validation logic phase.</response>
    [HttpPatch("staff/{staffId:int}/status")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleStaffStatus(int staffId, [FromBody] ToggleStatusDto dto)
    {
        await _staffService.ToggleStaffStatusAsync(staffId, dto.IsActive);
        return Ok(new { message = $"Staff status updated to {(dto.IsActive ? "active" : "inactive")}." });
    }

    #endregion




    #region 3. Vendor Operations
    /// <summary>Create and Append Brand New Supply Vendor</summary>
    /// <remarks>Establishes new B2B supplier identity configurations globally storing the record tracking arrays internally.</remarks>
    /// <param name="dto">DTO formatting strict validation pipeline maps string bindings reliably.</param>
    /// <response code="201">Object persisted. Responds directly back dynamically appended properties.</response>
    /// <response code="400">Payload breaks DTO limits / attributes definitions mapping safely formats.</response>
    /// <response code="409">Email collisions found within active tables arrays preventing database duplicates securely safely limits.</response>
    [HttpPost("vendors")]
    [ProducesResponseType(typeof(VendorResponseDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorDto dto)
    {
        var result = await _vendorService.CreateVendorAsync(dto);
        return CreatedAtAction(nameof(GetVendorById), new { vendorId = result.VendorId }, result);
    }

    /// <summary>Update vendor details</summary>
    [HttpPut("vendors/{vendorId:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> UpdateVendor(int vendorId, [FromBody] UpdateVendorDto dto)
    {
        dto.VendorId = vendorId;
        var result = await _vendorService.UpdateVendorAsync(dto);
        return Ok(result);
    }

    /// <summary>Get vendor by ID</summary>
    [HttpGet("vendors/{vendorId:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetVendorById(int vendorId)
    {
        var result = await _vendorService.GetVendorByIdAsync(vendorId);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Get all vendors (paginated)</summary>
    [HttpGet("vendors")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAllVendors([FromQuery] PaginationParams paging)
    {
        var result = await _vendorService.GetAllVendorsAsync(paging);
        return Ok(result);
    }

    /// <summary>Delete vendor</summary>
    [HttpDelete("vendors/{vendorId:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DeleteVendor(int vendorId)
    {
        await _vendorService.DeleteVendorAsync(vendorId);
        return Ok(new { message = "Vendor marked as inactive. Existing parts remain active in inventory and historical purchase records are preserved." });
    }

    #endregion






    #region 4. Internal Part Inventory
    /// <summary>Create a new vehicle part</summary>
    [HttpPost("parts")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreatePart([FromBody] CreatePartDto dto)
    {
        var result = await _partService.CreatePartAsync(dto);
        return CreatedAtAction(nameof(GetPartById), new { partId = result.PartId }, result);
    }

    /// <summary>Update part details</summary>
    [HttpPut("parts/{partId:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdatePart(int partId, [FromBody] UpdatePartDto dto)
    {
        dto.PartId = partId;
        var result = await _partService.UpdatePartAsync(dto);
        return Ok(result);
    }

    /// <summary>Get part by ID</summary>
    [HttpGet("parts/{partId:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPartById(int partId)
    {
        var result = await _partService.GetPartByIdAsync(partId);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Get all parts (paginated)</summary>
    [HttpGet("parts")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAllParts([FromQuery] PaginationParams paging)
    {
        var result = await _partService.GetAllPartsAsync(paging);
        return Ok(result);
    }

    /// <summary>Get parts by category</summary>
    [HttpGet("parts/category/{category}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetPartsByCategory(string category, [FromQuery] PaginationParams paging)
    {
        var result = await _partService.GetPartsByCategoryAsync(category, paging);
        return Ok(result);
    }

    /// <summary>Search parts</summary>
    [HttpGet("parts/search")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SearchParts([FromQuery] string q, [FromQuery] PaginationParams paging)
    {
        var result = await _partService.SearchPartsAsync(q, paging);
        return Ok(result);
    }

    /// <summary>Get parts from a specific vendor</summary>
    [HttpGet("vendors/{vendorId:int}/parts")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPartsByVendor(int vendorId)
    {
        var result = await _partService.GetPartsByVendorAsync(vendorId);
        return Ok(result);
    }

    /// <summary>Delete part</summary>
    [HttpDelete("parts/{partId:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeletePart(int partId)
    {
        await _partService.DeletePartAsync(partId);
        return Ok(new { message = "Part marked as inactive and hidden from inventory lists." });
    }

    #endregion





    #region 5. Purchase Order Processing
    /// <summary>Create a new purchase invoice</summary>
    [HttpPost("purchase-orders")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderDto dto)
    {
        var result = await _purchaseInvoiceService.CreatePurchaseOrderAsync(dto);
        return CreatedAtAction(nameof(GetPurchaseOrderById), new { purchaseOrderId = result.PurchaseOrderId }, result);
    }

    /// <summary>Update purchase order items/invoice while it is not delivered</summary>
    [HttpPut("purchase-orders/{purchaseOrderId:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> UpdatePurchaseOrder(int purchaseOrderId, [FromBody] UpdatePurchaseOrderDto dto)
    {
        var result = await _purchaseInvoiceService.UpdatePurchaseOrderAsync(purchaseOrderId, dto);
        return Ok(result);
    }

    /// <summary>Send purchase order request email to vendor</summary>
    [HttpPost("purchase-orders/{purchaseOrderId:int}/send-to-vendor")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SendPurchaseOrderToVendor(int purchaseOrderId, [FromBody] SendPurchaseOrderToVendorDto dto)
    {
        var result = await _purchaseInvoiceService.SendPurchaseOrderToVendorAsync(purchaseOrderId, dto);
        return Ok(result);
    }

    /// <summary>Confirm purchase order after vendor invoice is received</summary>
    [HttpPost("purchase-orders/{purchaseOrderId:int}/confirm")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> ConfirmPurchaseOrder(int purchaseOrderId, [FromBody] ConfirmPurchaseOrderDto dto)
    {
        var result = await _purchaseInvoiceService.ConfirmPurchaseOrderAsync(purchaseOrderId, dto);
        return Ok(result);
    }

    /// <summary>Mark purchase order delivered and add items to inventory</summary>
    [HttpPost("purchase-orders/{purchaseOrderId:int}/deliver")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeliverPurchaseOrder(int purchaseOrderId, [FromBody] DeliverPurchaseOrderDto dto)
    {
        var result = await _purchaseInvoiceService.MarkPurchaseOrderDeliveredAsync(purchaseOrderId, dto);
        return Ok(result);
    }

    /// <summary>Get purchase order by ID</summary>
    [HttpGet("purchase-orders/{purchaseOrderId:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPurchaseOrderById(int purchaseOrderId)
    {
        var result = await _purchaseInvoiceService.GetPurchaseOrderByIdAsync(purchaseOrderId);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Get purchase orders for a vendor</summary>
    [HttpGet("vendors/{vendorId:int}/purchase-orders")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPurchaseOrdersByVendor(int vendorId)
    {
        var result = await _purchaseInvoiceService.GetPurchaseOrdersByVendorAsync(vendorId);
        return Ok(result);
    }

    /// <summary>Get all purchase orders (paginated)</summary>
    [HttpGet("purchase-orders")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAllPurchaseOrders([FromQuery] PaginationParams paging)
    {
        var result = await _purchaseInvoiceService.GetAllPurchaseOrdersAsync(paging);
        return Ok(result);
    }

    /// <summary>Order a part for an escalated part request</summary>
    [HttpPost("part-requests/{partRequestId:int}/order")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> OrderPartRequest(int partRequestId, [FromBody] OrderPartRequestDto dto)
    {
        var adminUserId = GetUserIdFromToken();
        var partRequest = await _partRequestService.OrderPartRequestAsync(adminUserId, partRequestId, dto);
        if (partRequest == null)
            return NotFound(new { message = "Part request not found" });

        return Ok(new
        {
            message = "Part ordered from vendor.",
            data = partRequest
        });
    }

    #endregion







    #region 6. Dynamic Financial Reports
    /// <summary>Get financial report (daily, monthly, or yearly)</summary>
    [HttpGet("reports/financial")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetFinancialReport(
        [FromQuery] ReportPeriod period = ReportPeriod.Daily,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] DateTime? date = null)
    {
        if (date.HasValue)
        {
            var datedReport = await _reportService.GetFinancialReportAsync(date.Value, date.Value);
            return Ok(datedReport);
        }

        var result = await _reportService.GetFinancialReportAsync(period, year, month);
        return Ok(result);
    }

    /// <summary>Get financial report for a custom inclusive date range</summary>
    [HttpGet("reports/financial/range")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetFinancialReportByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _reportService.GetFinancialReportAsync(startDate, endDate);
        return Ok(result);
    }

    /// <summary>Generate a custom date-range financial report PDF, upload it, and return the file link</summary>
    [HttpPost("reports/financial/range/pdf")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadFinancialReportPdf([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _reportService.UploadFinancialReportPdfAsync(startDate, endDate);
        return Ok(result);
    }

    /// <summary>Get inventory report</summary>
    [HttpGet("reports/inventory")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetInventoryReport()
    {
        var result = await _reportService.GetInventoryReportAsync();
        return Ok(result);
    }

    /// <summary>Get low stock alert parts</summary>
    [HttpGet("reports/low-stock")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetLowStockParts([FromQuery] int threshold = 10)
    {
        var result = await _reportService.GetLowStockPartsAsync(threshold);
        return Ok(result);
    }

    #endregion






    

    #region 7. Live Notification Feeds
    /// <summary>Alias for low-stock alerts used by admin alert banners</summary>
    [HttpGet("alerts/low-stock")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetLowStockAlerts([FromQuery] int threshold = 10)
    {
        var result = await _reportService.GetLowStockPartsAsync(threshold);
        return Ok(new { items = result });
    }

    /// <summary>Get admin notification center items</summary>
    [HttpGet("notifications")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetNotifications([FromQuery] PaginationParams paging)
    {
        var query = _db.Notifications
            .Include(n => n.User)
            .Where(n => n.User.Role == Role.Admin)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => MapNotification(n));

        var result = await query.ToPagedResultAsync(paging);
        return Ok(result);
    }

    /// <summary>Mark an admin notification as read</summary>
    [HttpPatch("notifications/{notificationId:int}/read")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkNotificationRead(int notificationId)
    {
        var notification = await _db.Notifications
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.User.Role == Role.Admin)
            ?? throw new NotFoundException($"Notification with ID {notificationId} not found.");

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return Ok(MapNotification(notification));
    }

    // GetUserIdFromToken() inherited from BaseApiController
    #endregion

    #region 9. Controller Utilities & Helpers

    private async Task<User> GetCurrentAdminAsync()
    {
        var userId = GetUserIdFromToken();
        return await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.Role == Role.Admin && !u.IsDeleted)
            ?? throw new AdminNotFoundException(userId);
    }

    private static AdminProfileResponseDto MapAdminProfile(User user) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        Address = user.Address,
        ProfileImageUrl = user.ProfileImageUrl,
        CreatedAt = user.CreatedAt
    };

    private static AdminNotificationDto MapNotification(Notification notification) => new()
    {
        NotificationId = notification.NotificationId,
        LogType = notification.LogType,
        Subject = notification.Subject,
        Message = notification.Message,
        MailedStatus = notification.MailedStatus.ToString(),
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt
    };

    #endregion

    #region 8. Automation Email Schedulers & Templates
    
    /// <summary>Get all scheduled notifications</summary>
    [HttpGet("scheduled-notifications")]
    public async Task<IActionResult> GetScheduledNotifications()
    {
        var notifications = await _scheduledNotificationService.GetAllNotificationsAsync();
        return Ok(notifications);
    }

    /// <summary>Get a specific scheduled notification</summary>
    [HttpGet("scheduled-notifications/{notificationId}")]
    public async Task<IActionResult> GetScheduledNotification(int notificationId)
    {
        var notification = await _scheduledNotificationService.GetNotificationByIdAsync(notificationId);
        if (notification == null)
            return NotFound(new { message = "Scheduled notification not found" });
        return Ok(notification);
    }

    /// <summary>Create a new scheduled notification</summary>
    [HttpPost("scheduled-notifications")]
    public async Task<IActionResult> CreateScheduledNotification([FromBody] CreateScheduledNotificationDto dto)
    {
        var notification = await _scheduledNotificationService.CreateNotificationAsync(dto);
        return CreatedAtAction(nameof(GetScheduledNotification), new { notificationId = notification.NotificationId }, notification);
    }

    /// <summary>Update a scheduled notification</summary>
    [HttpPut("scheduled-notifications/{notificationId}")]
    public async Task<IActionResult> UpdateScheduledNotification(int notificationId, [FromBody] CreateScheduledNotificationDto dto)
    {
        var notification = await _scheduledNotificationService.UpdateNotificationAsync(notificationId, dto);
        return Ok(notification);
    }

    /// <summary>Delete a scheduled notification</summary>
    [HttpDelete("scheduled-notifications/{notificationId}")]
    public async Task<IActionResult> DeleteScheduledNotification(int notificationId)
    {
        await _scheduledNotificationService.DeleteNotificationAsync(notificationId);
        return Ok(new { message = "Scheduled notification deleted successfully" });
    }

    /// <summary>Manually execute low stock notification job (demo)</summary>
    [HttpPost("scheduled-notifications/execute/low-stock")]
    [AllowAnonymous]
    public async Task<IActionResult> ExecuteLowStockNotification()
    {
        bool alreadySentToday = await _db.Notifications
            .AnyAsync(n => n.LogType == "LowStockAlert" && n.CreatedAt.Date == DateTime.UtcNow.Date);

        if (alreadySentToday)
        {
            return BadRequest(new { success = false, message = "Low stock emails have already been sent today." });
        }

        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Role == Role.Admin && !u.IsDeleted);
        if (admin == null || string.IsNullOrEmpty(admin.Email)) 
            return NotFound(new { success = false, message = "Admin user/email not found." });

        var threshold = 10;
        var lowStockParts = await _db.Parts
            .Where(p => p.StockQuantity < threshold && !p.IsDeleted)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

        var items = lowStockParts.Select(p => (p.PartName, p.StockQuantity)).ToList();
        var htmlBody = _emailTemplateService.RenderLowStockAlertHtml(admin.FullName, items, threshold);

        await _emailService.SendEmailAsync(admin.Email, "Action Required: Low Stock Alert", htmlBody);

        _db.Notifications.Add(new Notification
        {
            UserId = admin.UserId,
            LogType = "LowStockAlert",
            Subject = "Low Stock Email Sent",
            Message = $"Sent HTML email alert to admin for {lowStockParts.Count} items.",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Low stock HTML email template sent to admin." });
    }

    /// <summary>Manually execute unpaid credits notification job (demo)</summary>
    [HttpPost("scheduled-notifications/execute/unpaid-credits")]
    [AllowAnonymous]
    public async Task<IActionResult> ExecuteUnpaidCreditsNotification()
    {
        bool alreadySentToday = await _db.Notifications
            .AnyAsync(n => n.LogType == "UnpaidReminderAlert" && n.CreatedAt.Date == DateTime.UtcNow.Date);

        var overdueDays = 30;
        var overdueThreshold = DateTime.UtcNow.AddDays(-overdueDays);

        var customersWithOverdueCredits = await _db.Users
            .Include(u => u.CustomerInvoices)
            .Where(u => u.Role == Role.Customer && 
                        u.CreditsRemaining < 0 && 
                        u.CustomerInvoices.Any(i => !i.IsPaid && i.DueDate.HasValue && i.DueDate.Value < overdueThreshold) &&
                        !u.IsDeleted)
            .ToListAsync();

        if (customersWithOverdueCredits.Count == 0)
        {
            return Ok(new { success = true, message = "No customers with overdue credits found. No emails sent." });
        }

        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Role == Role.Admin && !u.IsDeleted);

        foreach (var customer in customersWithOverdueCredits)
        {
            var htmlBody = _emailTemplateService.RenderUnpaidReminderHtml(customer.FullName, Math.Abs(customer.CreditsRemaining), overdueDays);

            try
            {
                await _emailService.SendEmailAsync(customer.Email, "Payment Reminder - Overdue Balance", htmlBody);
            }
            catch (Exception)
            {
                // Continue with other emails even if one fails
            }
        }

        if (admin != null)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = admin.UserId,
                LogType = "UnpaidReminderAlert",
                Subject = "Unpaid Reminders Sent",
                Message = $"HTML reminders sent to {customersWithOverdueCredits.Count} customers.",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return Ok(new { success = true, message = $"Unpaid reminders HTML templates sent to {customersWithOverdueCredits.Count} customers." });
    }
    #endregion
}