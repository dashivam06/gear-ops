using System;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using gearOps.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace gearOps.Infrastructure.Services;

public class AcsEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly ILogger<AcsEmailService> _logger;
    private readonly IOtpService _otpService;
    private readonly string _frontendUrl;
    private const string SenderEmail = "noreply@corerouter.me";

    public AcsEmailService(IConfiguration config, IOtpService otpService, ILogger<AcsEmailService> logger)
    {
        _otpService = otpService;
        _logger = logger;
        var endpoint = config["AZURE_COMMUNICATION_COREROUTER_ENDPOINT"];
        var accessKey = config["AZURE_COMMUNICATION_COREROUTER_ACCESS_KEY"];
        _frontendUrl = config["FRONTEND_URL"] ?? "http://localhost:3000";

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(accessKey))
        {
            throw new InvalidOperationException("Azure Communication Services configuration is missing.");
        }

        _emailClient = new EmailClient(new Uri(endpoint), new Azure.AzureKeyCredential(accessKey));
    }

    public async Task SendOtpEmailAsync(string email, string verificationId)
    {
        var actualOtp = await _otpService.GetOtpAsync(verificationId);
        if (string.IsNullOrWhiteSpace(actualOtp))
        {
            throw new InvalidOperationException("OTP could not be retrieved for email sending.");
        }

        var templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Templates", "RegistrationOtpEmail.html");
        string htmlContent;
        if (System.IO.File.Exists(templatePath))
        {
            htmlContent = await System.IO.File.ReadAllTextAsync(templatePath);
            htmlContent = htmlContent.Replace("{{USERNAME}}", email.Split('@')[0])
                                     .Replace("{{OTP_CODE}}", actualOtp)
                                     .Replace("{{EXPIRY_TIME}}", "5");
        }
        else
        {
            htmlContent = $"<p>Hello! Your OTP for registration is <strong>{actualOtp}</strong>. It expires in 5 minutes.</p>";
        }

        var emailContent = new EmailContent("GearOps: Complete your registration")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending Registration OTP email to {Email}", email);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendPasswordResetOtpEmailAsync(string email, string verificationId)
    {
        var actualOtp = await _otpService.GetOtpAsync(verificationId);
        if (string.IsNullOrWhiteSpace(actualOtp))
        {
            throw new InvalidOperationException("OTP could not be retrieved for password reset email sending.");
        }

        var templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Templates", "PasswordResetOtpEmail.html");
        string htmlContent;
        if (System.IO.File.Exists(templatePath))
        {
            htmlContent = await System.IO.File.ReadAllTextAsync(templatePath);
            htmlContent = htmlContent.Replace("{{USERNAME}}", email.Split('@')[0])
                                     .Replace("{{OTP_CODE}}", actualOtp)
                                     .Replace("{{EXPIRY_TIME}}", "5");
        }
        else
        {
            htmlContent = $"<p>Hello! Your OTP for password reset is <strong>{actualOtp}</strong>. It expires in 5 minutes.</p>";
        }

        var emailContent = new EmailContent("GearOps: Password Reset Code")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending Password Reset OTP email to {Email}", email);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendStaffOnboardingEmailAsync(string email, string fullName, string position, string temporaryPassword)
    {
        var templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Templates", "StaffOnboardingEmail.html");
        string htmlContent;

        if (System.IO.File.Exists(templatePath))
        {
            htmlContent = await System.IO.File.ReadAllTextAsync(templatePath);
            htmlContent = htmlContent.Replace("{{FULL_NAME}}", fullName)
                                     .Replace("{{POSITION}}", position)
                                     .Replace("{{TEMP_PASSWORD}}", temporaryPassword)
                                     .Replace("{{LOGIN_URL}}", _frontendUrl)
                                     .Replace("{{SUPPORT_EMAIL}}", SenderEmail);
        }
        else
        {
            htmlContent = $"<p>Dear {fullName},</p><p>Your GearOps staff account has been created for the role of {position}. Your temporary password is <strong>{temporaryPassword}</strong>. Please sign in and change it from your profile settings as soon as possible.</p>";
        }

        var emailContent = new EmailContent("GearOps: Staff account setup")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending staff onboarding email to {Email}", email);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendCustomerWelcomeEmailAsync(string email, string fullName, string temporaryPassword)
    {
        var templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Templates", "CustomerWelcomeEmail.html");
        string htmlContent;

        if (System.IO.File.Exists(templatePath))
        {
            htmlContent = await System.IO.File.ReadAllTextAsync(templatePath);
            htmlContent = htmlContent.Replace("{{FULL_NAME}}", fullName)
                                     .Replace("{{TEMP_PASSWORD}}", temporaryPassword)
                                     .Replace("{{LOGIN_URL}}", _frontendUrl)
                                     .Replace("{{SUPPORT_EMAIL}}", SenderEmail);
        }
        else
        {
            htmlContent = $"<p>Dear {fullName},</p><p>Your GearOps customer account has been created successfully.</p><p>Your temporary password is <strong>{temporaryPassword}</strong>.</p><p>Sign in at <a href=\"{_frontendUrl}\">{_frontendUrl}</a> and change your password from your profile settings as soon as possible.</p>";
        }

        var emailContent = new EmailContent("GearOps: Customer account setup")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending customer welcome email to {Email}", email);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendPartRequestDecisionEmailAsync(string email, string fullName, string partName, string status, string? decisionNote)
    {
        var templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Templates", "PartRequestDecisionEmail.html");
        string htmlContent;

        if (System.IO.File.Exists(templatePath))
        {
            htmlContent = await System.IO.File.ReadAllTextAsync(templatePath);
            htmlContent = htmlContent.Replace("{{FULL_NAME}}", fullName)
                                     .Replace("{{PART_NAME}}", partName)
                                     .Replace("{{STATUS}}", status)
                                     .Replace("{{DECISION_NOTE}}", string.IsNullOrWhiteSpace(decisionNote) ? "No additional notes were provided." : decisionNote);
        }
        else
        {
            htmlContent = $"<p>Dear {fullName},</p><p>Your part request for <strong>{partName}</strong> has been <strong>{status.ToLowerInvariant()}</strong>.</p><p>{decisionNote}</p>";
        }

        var emailContent = new EmailContent($"GearOps: Part Request {status}")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending part request decision email to {Email} with status {Status}", email, status);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendAppointmentApprovedEmailAsync(string email, string customerName, string vehicleNumber, DateTime appointmentDate, string staffName, string? notes)
    {
        var formattedDate = appointmentDate.ToString("dddd, MMMM d, yyyy");
        var formattedTime = appointmentDate.ToString("h:mm tt");

        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border: 1px solid #e0e0e0; }}
        .confirmation {{ background: #4caf50; color: white; padding: 15px; border-radius: 5px; text-align: center; margin: 20px 0; font-weight: bold; }}
        .details {{ background: white; padding: 20px; margin: 20px 0; border-left: 4px solid #667eea; }}
        .detail-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #667eea; }}
        .footer {{ background: #f0f0f0; padding: 20px; text-align: center; font-size: 12px; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Appointment Confirmed ✓</h1>
        </div>
        <div class=""content"">
            <p>Hello <strong>{customerName}</strong>,</p>
            <p>Great news! Your appointment has been <strong>approved and confirmed</strong>.</p>
            
            <div class=""confirmation"">
                Your appointment is confirmed
            </div>

            <div class=""details"">
                <div class=""detail-row"">
                    <span class=""label"">📅 Date:</span> {formattedDate}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">🕐 Time:</span> {formattedTime}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">🚗 Vehicle:</span> {vehicleNumber}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">👨‍🔧 Assigned Staff:</span> {staffName}
                </div>
                {(!string.IsNullOrWhiteSpace(notes) ? $"<div class=\"detail-row\"><span class=\"label\">📝 Notes:</span> {notes}</div>" : "")}
            </div>

            <p><strong>What's Next?</strong></p>
            <ul>
                <li>Please arrive 10 minutes early</li>
                <li>Keep your vehicle keys ready</li>
                <li>Our team will be ready to assist you</li>
            </ul>

            <p>If you need to reschedule or have any questions, please contact us as soon as possible.</p>
        </div>
        <div class=""footer"">
            <p>© 2026 GearOps. All rights reserved.</p>
            <p>This is an automated email. Please do not reply directly.</p>
        </div>
    </div>
</body>
</html>";

        var emailContent = new EmailContent("Appointment Confirmed - GearOps")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending appointment approval email to {Email} for date {AppointmentDate}", email, appointmentDate);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendAppointmentRejectedEmailAsync(string email, string customerName, string vehicleNumber, DateTime appointmentDate, string staffName, string reason)
    {
        var formattedDate = appointmentDate.ToString("dddd, MMMM d, yyyy");
        var formattedTime = appointmentDate.ToString("h:mm tt");

        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f56565 0%, #c53030 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border: 1px solid #e0e0e0; }}
        .notice {{ background: #fed7d7; color: #c53030; padding: 15px; border-radius: 5px; border-left: 4px solid #f56565; margin: 20px 0; }}
        .details {{ background: white; padding: 20px; margin: 20px 0; border-left: 4px solid #f56565; }}
        .detail-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #c53030; }}
        .footer {{ background: #f0f0f0; padding: 20px; text-align: center; font-size: 12px; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Appointment Could Not Be Confirmed</h1>
        </div>
        <div class=""content"">
            <p>Hello <strong>{customerName}</strong>,</p>
            <p>We regret to inform you that your appointment request has been <strong>rejected</strong>.</p>
            
            <div class=""notice"">
                <strong>⚠️ Appointment Status:</strong> Cancelled
            </div>

            <div class=""details"">
                <div class=""detail-row"">
                    <span class=""label"">📅 Original Date:</span> {formattedDate}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">🕐 Original Time:</span> {formattedTime}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">🚗 Vehicle:</span> {vehicleNumber}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">👨‍🔧 Reviewed By:</span> {staffName}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">📌 Reason:</span><br/>
                    <div style=""margin-top: 10px; padding: 10px; background: #fff5f5; border-radius: 5px;"">
                        {reason}
                    </div>
                </div>
            </div>

            <p><strong>What Can You Do?</strong></p>
            <ul>
                <li>📞 Contact us to discuss alternative dates and times</li>
                <li>📧 Reply to this email with your availability</li>
                <li>🗓️ Check our available time slots and book a new appointment</li>
            </ul>

            <p>We apologize for any inconvenience. We're here to help!</p>
        </div>
        <div class=""footer"">
            <p>© 2026 GearOps. All rights reserved.</p>
            <p>This is an automated email. Please do not reply directly.</p>
        </div>
    </div>
</body>
</html>";

        var emailContent = new EmailContent("Appointment Rejected - GearOps")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending appointment rejection email to {Email} for date {AppointmentDate}", email, appointmentDate);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendAppointmentNoShowEmailAsync(string email, string customerName, string vehicleNumber, DateTime appointmentDate, string staffName, string? reason)
    {
        var formattedDate = appointmentDate.ToString("dddd, MMMM d, yyyy");
        var formattedTime = appointmentDate.ToString("h:mm tt");

        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border: 1px solid #e0e0e0; }}
        .notice {{ background: #fef3c7; color: #92400e; padding: 15px; border-radius: 5px; border-left: 4px solid #f59e0b; margin: 20px 0; }}
        .details {{ background: white; padding: 20px; margin: 20px 0; border-left: 4px solid #f59e0b; }}
        .detail-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #d97706; }}
        .footer {{ background: #f0f0f0; padding: 20px; text-align: center; font-size: 12px; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Appointment Marked as No-Show</h1>
        </div>
        <div class=""content"">
            <p>Hello <strong>{customerName}</strong>,</p>
            <p>We noticed that you did not show up for your scheduled appointment.</p>
            
            <div class=""notice"">
                <strong>⚠️ Status:</strong> No-Show Recorded
            </div>

            <div class=""details"">
                <div class=""detail-row"">
                    <span class=""label"">📅 Appointment Date:</span> {formattedDate}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">🕐 Appointment Time:</span> {formattedTime}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">🚗 Vehicle:</span> {vehicleNumber}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">👨‍🔧 Recorded By:</span> {staffName}
                </div>
                {(!string.IsNullOrWhiteSpace(reason) ? $"<div class=\"detail-row\"><span class=\"label\">📝 Note:</span> {reason}</div>" : "")}
            </div>

            <p><strong>What Happens Next?</strong></p>
            <ul>
                <li>This appointment has been recorded as a no-show</li>
                <li>You may reschedule your appointment at your earliest convenience</li>
                <li>Please give us advance notice if you cannot make a future appointment</li>
            </ul>

            <p>If this was a mistake or if you'd like to reschedule, please contact us immediately.</p>
        </div>
        <div class=""footer"">
            <p>© 2026 GearOps. All rights reserved.</p>
            <p>This is an automated email. Please do not reply directly.</p>
        </div>
    </div>
</body>
</html>";

        var emailContent = new EmailContent("Appointment No-Show - GearOps")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending appointment no-show email to {Email} for date {AppointmentDate}", email, appointmentDate);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendAppointmentRescheduleEmailAsync(string email, string customerName, string vehicleNumber, DateTime oldDate, DateTime newDate, string staffName)
    {
        var oldFormattedDate = oldDate.ToString("dddd, MMMM d, yyyy");
        var oldFormattedTime = oldDate.ToString("h:mm tt");
        var newFormattedDate = newDate.ToString("dddd, MMMM d, yyyy");
        var newFormattedTime = newDate.ToString("h:mm tt");

        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border: 1px solid #e0e0e0; }}
        .notice {{ background: #dbeafe; color: #1e40af; padding: 15px; border-radius: 5px; border-left: 4px solid #3b82f6; margin: 20px 0; }}
        .details {{ background: white; padding: 20px; margin: 20px 0; border-left: 4px solid #3b82f6; }}
        .detail-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #1d4ed8; }}
        .dates-compare {{ display: flex; justify-content: space-between; margin: 20px 0; }}
        .date-box {{ flex: 1; padding: 15px; background: #f0f9ff; border-radius: 5px; text-align: center; }}
        .date-box.old {{ border-left: 4px solid #ef4444; }}
        .date-box.new {{ border-left: 4px solid #22c55e; }}
        .footer {{ background: #f0f0f0; padding: 20px; text-align: center; font-size: 12px; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Appointment Rescheduled</h1>
        </div>
        <div class=""content"">
            <p>Hello <strong>{customerName}</strong>,</p>
            <p>Your appointment has been successfully <strong>rescheduled</strong>.</p>
            
            <div class=""notice"">
                <strong>✓ Your new appointment is confirmed and awaiting approval</strong>
            </div>

            <div class=""details"">
                <div class=""detail-row"">
                    <span class=""label"">🚗 Vehicle:</span> {vehicleNumber}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">👨‍🔧 Scheduled By:</span> {staffName}
                </div>
            </div>

            <div class=""dates-compare"">
                <div class=""date-box old"">
                    <strong>Original Date</strong><br/>
                    {oldFormattedDate}<br/>
                    {oldFormattedTime}
                </div>
                <div style=""text-align: center; margin: 0 10px; display: flex; align-items: center;"">
                    <strong>→</strong>
                </div>
                <div class=""date-box new"">
                    <strong>New Date</strong><br/>
                    {newFormattedDate}<br/>
                    {newFormattedTime}
                </div>
            </div>

            <p><strong>Important:</strong> Your new appointment is pending approval from our team. You will receive a confirmation email once it has been reviewed.</p>

            <p>If you have any questions or need further changes, please contact us.</p>
        </div>
        <div class=""footer"">
            <p>© 2026 GearOps. All rights reserved.</p>
            <p>This is an automated email. Please do not reply directly.</p>
        </div>
    </div>
</body>
</html>";

        var emailContent = new EmailContent("Appointment Rescheduled - GearOps")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending appointment reschedule email to {Email} from {OldDate} to {NewDate}", email, oldDate, newDate);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendAppointmentCompletionEmailAsync(string email, string customerName, string vehicleNumber, DateTime appointmentDate, string staffName)
    {
        var formattedDate = appointmentDate.ToString("dddd, MMMM d, yyyy");
        var formattedTime = appointmentDate.ToString("h:mm tt");

        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #22c55e 0%, #15803d 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border: 1px solid #e0e0e0; }}
        .confirmation {{ background: #dcfce7; color: #166534; padding: 15px; border-radius: 5px; border-left: 4px solid #22c55e; margin: 20px 0; }}
        .details {{ background: white; padding: 20px; margin: 20px 0; border-left: 4px solid #22c55e; }}
        .detail-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #15803d; }}
        .cta {{ background: #22c55e; color: white; padding: 12px 20px; border-radius: 5px; text-align: center; display: inline-block; margin-top: 20px; text-decoration: none; }}
        .footer {{ background: #f0f0f0; padding: 20px; text-align: center; font-size: 12px; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Service Completed ✓</h1>
        </div>
        <div class=""content"">
            <p>Hello <strong>{customerName}</strong>,</p>
            <p>Thank you for choosing our service! Your appointment has been <strong>completed successfully</strong>.</p>
            
            <div class=""confirmation"">
                ✓ Your service appointment is now complete
            </div>

            <div class=""details"">
                <div class=""detail-row"">
                    <span class=""label"">📅 Service Date:</span> {formattedDate}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">🕐 Service Time:</span> {formattedTime}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">🚗 Vehicle:</span> {vehicleNumber}
                </div>
                <div class=""detail-row"">
                    <span class=""label"">👨‍🔧 Serviced By:</span> {staffName}
                </div>
            </div>

            <p><strong>What's Next?</strong></p>
            <ul>
                <li>Your detailed service report will be available shortly</li>
                <li>You can view your invoice and service details in your account</li>
                <li>Leave a review to help us serve you better</li>
                <li>Book your next maintenance appointment</li>
            </ul>

            <p>We appreciate your business and look forward to serving you again!</p>
        </div>
        <div class=""footer"">
            <p>© 2026 GearOps. All rights reserved.</p>
            <p>This is an automated email. Please do not reply directly.</p>
        </div>
    </div>
</body>
</html>";

        var emailContent = new EmailContent("Service Completed - GearOps")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending appointment completion email to {Email} for date {AppointmentDate}", email, appointmentDate);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendPurchaseOrderEmailAsync(string email, string vendorName, int purchaseOrderId, string status, string message, string itemsSummary)
    {
        var htmlContent = $@"
<html>
<body style=""font-family:Arial,sans-serif;color:#222;"">
    <h2>GearOps Purchase Order #{purchaseOrderId}</h2>
    <p>Hello <strong>{vendorName}</strong>,</p>
    <p>{message}</p>
    <p><strong>Status:</strong> {status}</p>
    <pre style=""background:#f6f6f6;padding:12px;border:1px solid #ddd;"">{itemsSummary}</pre>
    <p>Please reply with the invoice number and any item changes if needed.</p>
    <p>Regards,<br/>GearOps</p>
</body>
</html>";

        var emailContent = new EmailContent($"GearOps Purchase Order #{purchaseOrderId} - {status}")
        {
            Html = htmlContent
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending purchase order email {PurchaseOrderId} to vendor {Email}", purchaseOrderId, email);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        var htmlContent = $@"
<html>
<body style=""font-family:Arial,sans-serif;color:#222;line-height:1.6;"">
    <div style=""max-width:600px;margin:0 auto;padding:20px;"">
        {body.Replace("\n", "<br/>")}
    </div>
</body>
</html>";

        var emailContent = new EmailContent(subject)
        {
            Html = htmlContent,
            PlainText = body
        };

        var emailMessage = new EmailMessage(
            senderAddress: SenderEmail,
            recipients: new EmailRecipients(to: new[] { new EmailAddress(email) }),
            content: emailContent);

        _logger.LogInformation("Sending email to {Email} with subject {Subject}", email, subject);
        await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
    }
}
