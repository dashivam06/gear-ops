using System;

namespace gearOps.Application.Exceptions;

/// <summary>
/// Base exception for all GearOps application-level errors.
/// Carries an HTTP status code so the global exception handler can map it directly.
/// </summary>
public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string ErrorType { get; }

    protected AppException(string message, int statusCode, string errorType) : base(message)
    {
        StatusCode = statusCode;
        ErrorType = errorType;
    }
}

// ─── Generic HTTP-Level Exceptions ────────────────────────────────────────────

public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message, 400, "BadRequest") { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message, 401, "Unauthorized") { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to access this resource.")
        : base(message, 403, "Forbidden") { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404, "NotFound") { }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, 409, "Conflict") { }
}

public class ValidationException : AppException
{
    public string[] Errors { get; }
    public ValidationException(string message, string[]? errors = null)
        : base(message, 422, "ValidationError")
    {
        Errors = errors ?? Array.Empty<string>();
    }
}

// ─── Entity-Specific Exceptions ───────────────────────────────────────────────

public class StaffNotFoundException : NotFoundException
{
    public StaffNotFoundException(int staffId)
        : base($"Staff member with ID {staffId} not found.") { }
}

public class VendorNotFoundException : NotFoundException
{
    public VendorNotFoundException(int vendorId)
        : base($"Vendor with ID {vendorId} not found.") { }
}

public class PartNotFoundException : NotFoundException
{
    public PartNotFoundException(int partId)
        : base($"Part with ID {partId} not found.") { }
}

public class CustomerNotFoundException : NotFoundException
{
    public CustomerNotFoundException(int customerId)
        : base($"Customer with ID {customerId} not found.") { }
}

public class VehicleNotFoundException : NotFoundException
{
    public VehicleNotFoundException(int vehicleId)
        : base($"Vehicle with ID {vehicleId} not found.") { }
}

public class AppointmentNotFoundException : NotFoundException
{
    public AppointmentNotFoundException(int appointmentId)
        : base($"Appointment with ID {appointmentId} not found.") { }
}

public class InvoiceNotFoundException : NotFoundException
{
    public InvoiceNotFoundException(int invoiceId)
        : base($"Invoice with ID {invoiceId} not found.") { }
}

public class ServiceRecordNotFoundException : NotFoundException
{
    public ServiceRecordNotFoundException(int serviceRecordId)
        : base($"Service record with ID {serviceRecordId} not found.") { }
}

public class ReviewNotFoundException : NotFoundException
{
    public ReviewNotFoundException(int reviewId)
        : base($"Review with ID {reviewId} not found.") { }
}

public class PartRequestNotFoundException : NotFoundException
{
    public PartRequestNotFoundException(int partRequestId)
        : base($"Part request with ID {partRequestId} not found.") { }
}

public class PurchaseOrderNotFoundException : NotFoundException
{
    public PurchaseOrderNotFoundException(int purchaseOrderId)
        : base($"Purchase order with ID {purchaseOrderId} not found.") { }
}

// ─── Business Rule Exceptions ─────────────────────────────────────────────────

public class InsufficientStockException : BadRequestException
{
    public InsufficientStockException(int partId, int requested, int available)
        : base($"Insufficient stock for part {partId}. Requested: {requested}, Available: {available}.") { }
}

public class DuplicateEmailException : ConflictException
{
    public DuplicateEmailException(string email)
        : base($"The email '{email}' is already registered.") { }
}

public class DuplicatePhoneException : ConflictException
{
    public DuplicatePhoneException(string phone)
        : base($"The phone number '{phone}' is already in use.") { }
}

public class InvalidPasswordException : BadRequestException
{
    public InvalidPasswordException(string message)
        : base(message) { }
}

public class PasswordMismatchException : BadRequestException
{
    public PasswordMismatchException()
        : base("Password and confirm password do not match.") { }
}

public class OtpExpiredException : BadRequestException
{
    public OtpExpiredException()
        : base("The OTP has expired or is invalid. Please request a new one.") { }
}

public class AccountInactiveException : ForbiddenException
{
    public AccountInactiveException()
        : base("This account has been deactivated. Please contact an administrator.") { }
}
