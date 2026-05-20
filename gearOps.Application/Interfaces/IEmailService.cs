using System.Threading.Tasks;

namespace gearOps.Application.Interfaces;

public interface IEmailService
{
    Task SendOtpEmailAsync(string email, string otp);
    Task SendPasswordResetOtpEmailAsync(string email, string verificationId);
    Task SendStaffOnboardingEmailAsync(string email, string fullName, string position, string temporaryPassword);
    Task SendCustomerWelcomeEmailAsync(string email, string fullName, string temporaryPassword);
    Task SendPartRequestDecisionEmailAsync(string email, string fullName, string partName, string status, string? decisionNote);
    Task SendAppointmentApprovedEmailAsync(string email, string customerName, string vehicleNumber, DateTime appointmentDate, string staffName, string? notes);
    Task SendAppointmentRejectedEmailAsync(string email, string customerName, string vehicleNumber, DateTime appointmentDate, string staffName, string reason);
    Task SendAppointmentNoShowEmailAsync(string email, string customerName, string vehicleNumber, DateTime appointmentDate, string staffName, string? reason);
    Task SendAppointmentRescheduleEmailAsync(string email, string customerName, string vehicleNumber, DateTime oldDate, DateTime newDate, string staffName);
    Task SendAppointmentCompletionEmailAsync(string email, string customerName, string vehicleNumber, DateTime appointmentDate, string staffName);
    Task SendPurchaseOrderEmailAsync(string email, string vendorName, int purchaseOrderId, string status, string message, string itemsSummary);
    Task SendEmailAsync(string email, string subject, string body);
}
