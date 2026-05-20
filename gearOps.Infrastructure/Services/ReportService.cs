using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Domain.Enums;
using gearOps.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace gearOps.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportService> _logger;
    private readonly IConfiguration _config;

    public ReportService(AppDbContext context, ILogger<ReportService> logger, IConfiguration config)
    {
        _context = context;
        _logger = logger;
        _config = config;
    }

    public async Task<FinancialReportDto> GetFinancialReportAsync(ReportPeriod period, int? year = null, int? month = null)
    {
        try
        {
            var currentDate = DateTime.UtcNow;
            DateTime startDate = currentDate;
            DateTime endDate = currentDate;

            switch (period)
            {
                case ReportPeriod.Daily:
                    startDate = currentDate.Date;
                    endDate = currentDate.Date.AddDays(1);
                    break;

                case ReportPeriod.Monthly:
                    int reportMonth = month ?? currentDate.Month;
                    int reportYear = year ?? currentDate.Year;
                    if (reportMonth is < 1 or > 12)
                        throw new ArgumentException("Month must be between 1 and 12.");
                    startDate = new DateTime(reportYear, reportMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddMonths(1);
                    break;

                case ReportPeriod.Yearly:
                    int reportYr = year ?? currentDate.Year;
                    startDate = new DateTime(reportYr, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddYears(1);
                    break;
            }

            return await BuildFinancialReportAsync(startDate, endDate, period, period.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating financial report");
            throw;
        }
    }

    public async Task<FinancialReportDto> GetFinancialReportAsync(DateTime startDate, DateTime endDate)
    {
        var (rangeStart, rangeEnd) = NormalizeDateRange(startDate, endDate);
        var period = rangeStart.Date == rangeEnd.AddTicks(-1).Date ? ReportPeriod.Daily : ReportPeriod.Monthly;
        var label = rangeStart.Date == rangeEnd.AddTicks(-1).Date
            ? $"Daily ({rangeStart:yyyy-MM-dd})"
            : $"Custom ({rangeStart:yyyy-MM-dd} to {rangeEnd.AddDays(-1):yyyy-MM-dd})";

        return await BuildFinancialReportAsync(rangeStart, rangeEnd, period, label);
    }

    public async Task<FinancialReportPdfResponseDto> UploadFinancialReportPdfAsync(DateTime startDate, DateTime endDate)
    {
        var report = await GetFinancialReportAsync(startDate, endDate);
        var pdfBytes = BuildFinancialReportPdf(report);
        var fileName = $"financial-report-{report.StartDate:yyyyMMdd}-{report.EndDate:yyyyMMdd}.pdf";
        var uploadResult = await UploadPdfToCloudinaryAsync(pdfBytes, fileName);

        return new FinancialReportPdfResponseDto
        {
            Url = uploadResult.Url,
            SecureUrl = uploadResult.SecureUrl,
            PublicId = uploadResult.PublicId,
            FileName = fileName,
            Report = report
        };
    }

    public async Task<List<InventoryReportDto>> GetInventoryReportAsync()
    {
        try
        {
            var parts = await _context.Parts
                .Include(p => p.Vendor)
                .ToListAsync();

            var report = parts.Select(p => new InventoryReportDto
            {
                PartId = p.PartId,
                PartName = p.PartName,
                Category = p.Category,
                CurrentStock = p.StockQuantity,
                ReorderLevel = 10,
                CostPricePerUnit = p.CostPricePerUnit,
                InventoryValue = p.StockQuantity * p.CostPricePerUnit
            }).ToList();

            _logger.LogInformation($"Inventory report generated with {report.Count} parts");

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory report");
            throw;
        }
    }

    public async Task<List<LowStockPartDto>> GetLowStockPartsAsync(int threshold = 10)
    {
        try
        {
            if (threshold < 0)
                throw new ArgumentException("Threshold must be 0 or greater.");

            var lowStockParts = await _context.Parts
                .Where(p => p.StockQuantity < threshold)
                .Include(p => p.Vendor)
                .ToListAsync();

            var report = lowStockParts.Select(p => new LowStockPartDto
            {
                PartId = p.PartId,
                PartName = p.PartName,
                Category = p.Category,
                StockQuantity = p.StockQuantity,
                Unit = p.Unit
            }).ToList();

            _logger.LogInformation($"Low stock report generated: {report.Count} parts below threshold {threshold}");

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating low stock report");
            throw;
        }
    }

    private async Task<FinancialReportDto> BuildFinancialReportAsync(DateTime startDate, DateTime endDate, ReportPeriod period, string periodLabel)
    {
        var salesInvoices = await _context.SalesInvoices
            .Where(s => s.InvoiceDate >= startDate && s.InvoiceDate < endDate)
            .Include(s => s.SalesInvoiceItems)
            .ToListAsync();

        var purchaseOrders = await _context.PurchaseOrders
            .Where(p => p.DeliveredAt >= startDate && p.DeliveredAt < endDate && p.Status == PurchaseOrderStatus.Delivered)
            .ToListAsync();

        var totalSalesRevenue = salesInvoices
            .SelectMany(s => s.SalesInvoiceItems)
            .Sum(item => item.Quantity * item.PricePerUnit);

        var totalPurchaseCost = purchaseOrders.Sum(p => p.TotalAmount);
        var grossProfit = totalSalesRevenue - totalPurchaseCost;
        var profitMargin = totalSalesRevenue > 0 ? (grossProfit / totalSalesRevenue) * 100 : 0;

        _logger.LogInformation("Financial report generated for {PeriodLabel}: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
            periodLabel, startDate, endDate);

        return new FinancialReportDto
        {
            ReportDate = DateTime.UtcNow,
            Period = period,
            StartDate = startDate,
            EndDate = endDate.AddTicks(-1),
            PeriodLabel = periodLabel,
            TotalSalesRevenue = totalSalesRevenue,
            TotalPurchaseCost = totalPurchaseCost,
            GrossProfit = grossProfit,
            ProfitMargin = profitMargin,
            TotalTransactions = salesInvoices.Count,
            TotalPartsMovement = salesInvoices.SelectMany(s => s.SalesInvoiceItems).Sum(i => i.Quantity)
        };
    }

    private static (DateTime StartDate, DateTime EndDate) NormalizeDateRange(DateTime startDate, DateTime endDate)
    {
        var start = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc).AddDays(1);

        if (end <= start)
            throw new BadRequestException("End date must be on or after start date.");

        return (start, end);
    }

    private async Task<CloudinaryUploadResult> UploadPdfToCloudinaryAsync(byte[] pdfBytes, string fileName)
    {
        var cloudName = _config["CLOUDINARY_CLOUD_NAME"] ?? _config["NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME"] ?? "duiymmlq4";
        var uploadPreset = _config["CLOUDINARY_UPLOAD_PRESET"] ?? _config["NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET"] ?? "corerouter_uploads";

        using var httpClient = new HttpClient();
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        form.Add(fileContent, "\"file\"", $"\"{fileName}\"");
        form.Add(new StringContent(uploadPreset), "\"upload_preset\"");
        form.Add(new StringContent($"gearops/reports/{Path.GetFileNameWithoutExtension(fileName)}"), "\"public_id\"");

        var response = await httpClient.PostAsync($"https://api.cloudinary.com/v1_1/{cloudName}/image/upload", form);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new BadRequestException($"Cloudinary PDF upload failed: {body}");

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        // Handle pending or alternate Cloudinary responses
        var url = root.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : string.Empty;
        var secureUrl = root.TryGetProperty("secure_url", out var secureProp) ? secureProp.GetString() : string.Empty;
        var publicId = root.TryGetProperty("public_id", out var pubProp) ? pubProp.GetString() : string.Empty;

        return new CloudinaryUploadResult(
            url ?? string.Empty,
            secureUrl ?? string.Empty,
            publicId ?? string.Empty);
    }
    private static byte[] BuildFinancialReportPdf(FinancialReportDto report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Montserrat"));

                page.Header().Element(compose => ComposeHeader(compose, report));
                page.Content().Element(compose => ComposeContent(compose, report));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, FinancialReportDto report)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("GearOps").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("Financial Report").FontSize(16).FontColor(Colors.Grey.Darken2);
                column.Item().PaddingTop(5).Text($"Period: {report.PeriodLabel}").FontSize(10);
                column.Item().Text($"Date: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}").FontSize(10);
                column.Item().Text($"Generated: {report.ReportDate:yyyy-MM-dd HH:mm} UTC").FontSize(10);
            });

            row.ConstantItem(100).AlignRight().Text($"Rev: {FormatMoney(report.TotalRevenue)}").FontSize(12).Bold();
        });
    }

    private static void ComposeContent(IContainer container, FinancialReportDto report)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Item().PaddingBottom(10).Text("Key Performance Indicators").FontSize(14).SemiBold();

            column.Item().Row(row =>
            {
                ComposeKpiCard(row.RelativeItem(), "Total Revenue", FormatMoney(report.TotalRevenue), Colors.Green.Darken2);
                row.ConstantItem(10);
                ComposeKpiCard(row.RelativeItem(), "Net Profit", FormatMoney(report.NetProfit), Colors.Blue.Darken2);
                row.ConstantItem(10);
                ComposeKpiCard(row.RelativeItem(), "Gross Profit", FormatMoney(report.GrossProfit), Colors.Teal.Darken2);
            });

            column.Item().PaddingTop(10).Row(row =>
            {
                ComposeKpiCard(row.RelativeItem(), "Total Expenses", FormatMoney(report.TotalExpenses), Colors.Red.Darken2);
                row.ConstantItem(10);
                ComposeKpiCard(row.RelativeItem(), "Profit Margin", $"{report.ProfitMargin:0.0}%", Colors.Purple.Darken2);
                row.ConstantItem(10);
                ComposeKpiCard(row.RelativeItem(), "Transactions", report.TotalTransactions.ToString(), Colors.Orange.Darken2);
            });

            column.Item().PaddingTop(25).PaddingBottom(10).Text("Details Summary").FontSize(14).SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(1).Padding(2).Text("Metric").SemiBold();
                    header.Cell().BorderBottom(1).Padding(2).Text("Description").SemiBold();
                    header.Cell().BorderBottom(1).Padding(2).AlignRight().Text("Value").SemiBold();
                });

                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text("Purchases");
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text("Total value of all inventory parts ordered");
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignRight().Text(FormatMoney(report.TotalPurchaseCost));

                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text("Sales");
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text("Total value from finished customer orders");
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignRight().Text(FormatMoney(report.TotalSalesRevenue));

                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text("Parts Movement");
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text("Count of total individual items sold");
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignRight().Text(report.TotalPartsMovement.ToString());
            });
        });
    }

    private static void ComposeKpiCard(IContainer container, string title, string value, string color)
    {
        container.Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text(title).FontSize(10).FontColor(Colors.Grey.Darken3);
            column.Item().Text(value).FontSize(16).SemiBold().FontColor(color);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    }

    private static string FormatMoney(decimal value) => $"${value:0.00}";

    private sealed record CloudinaryUploadResult(string Url, string SecureUrl, string PublicId);
}
