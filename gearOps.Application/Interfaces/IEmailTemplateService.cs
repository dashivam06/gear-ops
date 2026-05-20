namespace gearOps.Application.Interfaces;

/// <summary>
/// Generates pre-formatted HTML email bodies for automated notification workflows.
/// Keeps email rendering logic out of controllers and background jobs.
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>Renders the low-stock alert HTML table sent to admin.</summary>
    string RenderLowStockAlertHtml(string adminName, IEnumerable<(string PartName, int Stock)> items, int threshold);

    /// <summary>Renders the overdue payment reminder HTML sent to a customer.</summary>
    string RenderUnpaidReminderHtml(string customerName, decimal outstandingBalance, int overdueDays);
}
