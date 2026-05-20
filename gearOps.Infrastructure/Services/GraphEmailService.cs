using System;
using System.Threading.Tasks;
using Azure.Identity;
using gearOps.Application.Interfaces;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace gearOps.Infrastructure.Services;

public class GraphEmailService : IEmailService
{
    private readonly GraphServiceClient _graphClient;
    private readonly IOtpService _otpService;
    private readonly ILogger<GraphEmailService> _logger;
    private readonly string? _senderUserPrincipalName;
    private readonly string _frontendUrl;

    public GraphEmailService(IConfiguration config, IOtpService otpService, ILogger<GraphEmailService> logger)
    {
        _otpService = otpService;
        _logger = logger;
        var tenantId = config["AZURE_ENTRA_TENANT_ID"];
        var clientId = config["AZURE_ENTRA_CLIENT_ID"];
        var clientSecret = config["AZURE_ENTRA_CLIENT_SECRET"];
        _senderUserPrincipalName = config["AZURE_ENTRA_SENDER_USER"];
        _frontendUrl = config["FRONTEND_URL"] ?? "http://localhost:3000";

        if (string.IsNullOrEmpty(tenantId) ||
            string.IsNullOrEmpty(clientId) ||
            string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Azure credentials are not configured properly.");
        }
        var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        _graphClient = new GraphServiceClient(clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });
    }

    public async Task SendOtpEmailAsync(string email, string verificationId)
    {
        var actualOtp = await _otpService.GetOtpAsync(verificationId);
        if (string.IsNullOrWhiteSpace(actualOtp))
        {
            throw new InvalidOperationException("OTP could not be retrieved for email sending.");
        }

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = "GearOPS: Your Registration OTP",
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = $"Hello! Your OTP for registration is {actualOtp}. It expires in 5 minutes."
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        if (string.IsNullOrWhiteSpace(_senderUserPrincipalName))
        {
            _logger.LogError("Azure sender user is not configured. Set AZURE_ENTRA_SENDER_USER to a mailbox-enabled user or shared mailbox UPN before sending OTP emails.");
            throw new InvalidOperationException("Azure sender user is not configured.");
        }

        _logger.LogInformation("Sending OTP email to {Email} from {SenderUser}", email, _senderUserPrincipalName);
        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
            _logger.LogInformation("OTP email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email} from {SenderUser}. Error: {ErrorMessage}", email, _senderUserPrincipalName, ex.Message);
            throw;
        }
    }

    public async Task SendPasswordResetOtpEmailAsync(string email, string verificationId)
    {
        var actualOtp = await _otpService.GetOtpAsync(verificationId);
        if (string.IsNullOrWhiteSpace(actualOtp))
        {
            throw new InvalidOperationException("OTP could not be retrieved for password reset email sending.");
        }

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = "GearOPS: Password Reset Code",
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = $"Hello! Your OTP for password reset is {actualOtp}. It expires in 5 minutes."
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        if (string.IsNullOrWhiteSpace(_senderUserPrincipalName))
        {
            _logger.LogError("Azure sender user is not configured. Set AZURE_ENTRA_SENDER_USER to a mailbox-enabled user or shared mailbox UPN before sending OTP emails.");
            throw new InvalidOperationException("Azure sender user is not configured.");
        }

        _logger.LogInformation("Sending Password Reset OTP email to {Email} from {SenderUser}", email, _senderUserPrincipalName);
        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
            _logger.LogInformation("Password Reset OTP email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Password Reset OTP email to {Email} from {SenderUser}. Error: {ErrorMessage}", email, _senderUserPrincipalName, ex.Message);
            throw;
        }
    }

    public async Task SendStaffOnboardingEmailAsync(string email, string fullName, string position, string temporaryPassword)
    {
        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = "GearOps: Staff account setup",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = $"<p>Dear {fullName},</p><p>Your staff account has been created for the role of <strong>{position}</strong>.</p><p>Your temporary password is <strong>{temporaryPassword}</strong>.</p><p>Sign in at <a href=\"{_frontendUrl}\">{_frontendUrl}</a> and change your password from your profile settings as soon as possible.</p><p>If you were not expecting this email, please contact support.</p>"
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        if (string.IsNullOrWhiteSpace(_senderUserPrincipalName))
        {
            _logger.LogError("Azure sender user is not configured. Set AZURE_ENTRA_SENDER_USER to a mailbox-enabled user or shared mailbox UPN before sending staff onboarding emails.");
            throw new InvalidOperationException("Azure sender user is not configured.");
        }

        _logger.LogInformation("Sending staff onboarding email to {Email} from {SenderUser}", email, _senderUserPrincipalName);
        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
            _logger.LogInformation("Staff onboarding email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send staff onboarding email to {Email} from {SenderUser}. Error: {ErrorMessage}", email, _senderUserPrincipalName, ex.Message);
            throw;
        }
    }

    public async Task SendPartRequestDecisionEmailAsync(string email, string fullName, string partName, string status, string? decisionNote)
    {
        if (string.IsNullOrWhiteSpace(_senderUserPrincipalName))
        {
            _logger.LogError("Azure sender user is not configured. Set AZURE_ENTRA_SENDER_USER to a mailbox-enabled user or shared mailbox UPN before sending part request decision emails.");
            throw new InvalidOperationException("Azure sender user is not configured.");
        }

        var body = $"<p>Dear {fullName},</p><p>Your part request for <strong>{partName}</strong> has been <strong>{status.ToLowerInvariant()}</strong>.</p><p>{(string.IsNullOrWhiteSpace(decisionNote) ? "No additional notes were provided." : decisionNote)}</p>";

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = $"GearOps: Part Request {status}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = body
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        _logger.LogInformation("Sending part request decision email to {Email} from {SenderUser}", email, _senderUserPrincipalName);
        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send part request decision email to {Email} from {SenderUser}. Error: {ErrorMessage}", email, _senderUserPrincipalName, ex.Message);
            throw;
        }
    }

    public async Task SendAppointmentApprovedEmailAsync(string email, string customerName, string vehicleNumber, DateTime appointmentDate, string staffName, string? notes)
    {
        if (string.IsNullOrWhiteSpace(_senderUserPrincipalName))
        {
            _logger.LogError("Azure sender user is not configured. Set AZURE_ENTRA_SENDER_USER to a mailbox-enabled user or shared mailbox UPN before sending appointment emails.");
            throw new InvalidOperationException("Azure sender user is not configured.");
        }

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

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = "Appointment Confirmed - GearOps",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlContent
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        _logger.LogInformation("Sending appointment approval email to {Email} from {SenderUser}", email, _senderUserPrincipalName);
        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
            _logger.LogInformation("Appointment approval email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send appointment approval email to {Email} from {SenderUser}. Error: {ErrorMessage}", email, _senderUserPrincipalName, ex.Message);
            throw;
        }
    }

    public async Task SendAppointmentRejectedEmailAsync(string email, string customerName, string vehicleNumber, DateTime appointmentDate, string staffName, string reason)
    {
        if (string.IsNullOrWhiteSpace(_senderUserPrincipalName))
        {
            _logger.LogError("Azure sender user is not configured. Set AZURE_ENTRA_SENDER_USER to a mailbox-enabled user or shared mailbox UPN before sending appointment emails.");
            throw new InvalidOperationException("Azure sender user is not configured.");
        }

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

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = "Appointment Rejected - GearOps",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlContent
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        _logger.LogInformation("Sending appointment rejection email to {Email} from {SenderUser}", email, _senderUserPrincipalName);
        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
            _logger.LogInformation("Appointment rejection email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send appointment rejection email to {Email} from {SenderUser}. Error: {ErrorMessage}", email, _senderUserPrincipalName, ex.Message);
            throw;
        }
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

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = "Appointment No-Show - GearOps",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlContent
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        _logger.LogInformation("Sending appointment no-show email to {Email} from {SenderUser}", email, _senderUserPrincipalName);
        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
            _logger.LogInformation("Appointment no-show email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send appointment no-show email to {Email} from {SenderUser}. Error: {ErrorMessage}", email, _senderUserPrincipalName, ex.Message);
            throw;
        }
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

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = "Appointment Rescheduled - GearOps",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlContent
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        _logger.LogInformation("Sending appointment reschedule email to {Email} from {SenderUser}", email, _senderUserPrincipalName);
        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
            _logger.LogInformation("Appointment reschedule email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send appointment reschedule email to {Email} from {SenderUser}. Error: {ErrorMessage}", email, _senderUserPrincipalName, ex.Message);
            throw;
        }
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

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = "Service Completed - GearOps",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlContent
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        _logger.LogInformation("Sending appointment completion email to {Email} from {SenderUser}", email, _senderUserPrincipalName);
        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
            _logger.LogInformation("Appointment completion email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send appointment completion email to {Email} from {SenderUser}. Error: {ErrorMessage}", email, _senderUserPrincipalName, ex.Message);
            throw;
        }
    }

    public async Task SendPurchaseOrderEmailAsync(string email, string vendorName, int purchaseOrderId, string status, string message, string itemsSummary)
    {
        if (string.IsNullOrWhiteSpace(_senderUserPrincipalName))
        {
            _logger.LogError("Azure sender user is not configured. Set AZURE_ENTRA_SENDER_USER before sending purchase order emails.");
            throw new InvalidOperationException("Azure sender user is not configured.");
        }

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

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = $"GearOps Purchase Order #{purchaseOrderId} - {status}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlContent
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        _logger.LogInformation("Sending purchase order email {PurchaseOrderId} to {Email} from {SenderUser}", purchaseOrderId, email, _senderUserPrincipalName);
        await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
    }

    public async Task SendCustomerWelcomeEmailAsync(string email, string fullName, string temporaryPassword)
    {
        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = "GearOps: Customer Account Setup",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = $"<p>Dear {fullName},</p><p>Your GearOps customer account has been created successfully.</p><p>We have assigned you a temporary password: <strong>{temporaryPassword}</strong>.</p><p>Sign in at <a href=\"{_frontendUrl}\">{_frontendUrl}</a> and change your password from your profile settings as soon as possible.</p><p>Welcome to GearOps!</p>"
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        if (string.IsNullOrWhiteSpace(_senderUserPrincipalName))
        {
            _logger.LogError("Azure sender user is not configured.");
            throw new InvalidOperationException("Azure sender user is not configured.");
        }

        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
            _logger.LogInformation("Customer welcome email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            throw;
        }
    }

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = body
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = email } }
                }
            },
            SaveToSentItems = false
        };

        if (string.IsNullOrWhiteSpace(_senderUserPrincipalName))
        {
            _logger.LogError("Azure sender user is not configured.");
            throw new InvalidOperationException("Azure sender user is not configured.");
        }

        try
        {
            await _graphClient.Users[_senderUserPrincipalName].SendMail.PostAsync(requestBody);
            _logger.LogInformation("Email sent to {Email} with subject {Subject}", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            throw;
        }
    }
}
