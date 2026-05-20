using gearOps.Application.Interfaces;

namespace gearOps.Infrastructure.Services;

/// <inheritdoc />
public class EmailTemplateService : IEmailTemplateService
{
    public string RenderLowStockAlertHtml(string adminName, IEnumerable<(string PartName, int Stock)> items, int threshold)
    {
        var rows = string.Join("", items.Select(p =>
            $"<tr><td style='padding:10px;border:1px solid #ddd;'>{p.PartName}</td>" +
            $"<td style='padding:10px;border:1px solid #ddd;color:#d9534f;font-weight:bold;'>{p.Stock}</td></tr>"));

        return $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;'>
            <h2 style='color: #d9534f;'>Critical Low Stock Alert</h2>
            <p>Dear {adminName},</p>
            <p>The following items are running below the minimum stock threshold ({threshold} units):</p>
            <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                <thead>
                    <tr style='background-color: #f7f7f7; text-align: left;'>
                        <th style='padding: 10px; border: 1px solid #ddd;'>Part Name</th>
                        <th style='padding: 10px; border: 1px solid #ddd;'>Current Stock</th>
                    </tr>
                </thead>
                <tbody>
                    {rows}
                </tbody>
            </table>
            <p style='margin-top: 20px;'>Please log into the GearOps admin panel and reorder these parts to prevent any disruption in services.</p>
            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
            <p style='font-size: 12px; color: #777;'>This is an automated notification from the GearOps System.</p>
        </div>";
    }

    public string RenderUnpaidReminderHtml(string customerName, decimal outstandingBalance, int overdueDays)
    {
        return $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;'>
            <h2 style='color: #f0ad4e;'>Payment Reminder Notice</h2>
            <p>Dear {customerName},</p>
            <p>This is a friendly reminder that you have an outstanding balance that is more than {overdueDays} days overdue on your account.</p>
            <div style='background-color: #fcf8e3; border-left: 5px solid #f0ad4e; padding: 15px; margin: 20px 0;'>
                <strong>Total Outstanding Balance:</strong> <span style='font-size: 18px; color: #d9534f;'>{outstandingBalance:C}</span>
            </div>
            <p>Please kindly settle this balance at your earliest convenience to avoid any service disruptions from future appointments.</p>
            <p>If you have already made the payment within the last 24 hours, please disregard this notice.</p>
            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
            <p style='margin-top: 20px;'>Best regards,<br><strong>GearOps Assistance Team</strong></p>
        </div>";
    }
}
