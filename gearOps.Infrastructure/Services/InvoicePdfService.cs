using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Infrastructure.Data;

namespace gearOps.Infrastructure.Services;

public class InvoicePdfService : IInvoicePdfService
{
    private readonly AppDbContext _context;
    private readonly ILogger<InvoicePdfService> _logger;

    public InvoicePdfService(AppDbContext context, ILogger<InvoicePdfService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InvoicePdfResponseDto> GenerateInvoicePdfAsync(int invoiceId, int userId)
    {
        try
        {
            var invoice = await _context.SalesInvoices
                .Include(s => s.Customer)
                .Include(s => s.Vehicle)
                .Include(s => s.SalesInvoiceItems)
                .ThenInclude(item => item.Part)
                .FirstOrDefaultAsync(s => s.SalesInvoiceId == invoiceId && s.CustomerId == userId)
                ?? throw new NotFoundException("Invoice not found or does not belong to this customer.");

            // Generate PDF content as simple text/bytes (in production, use iText, SelectPdf, etc.)
            var pdfContent = GeneratePdfContent(invoice);

            _logger.LogInformation($"Invoice PDF generated: {invoiceId} for customer {userId}");

            return new InvoicePdfResponseDto
            {
                PdfBytes = pdfContent,
                FileName = $"invoice_{invoiceId}_{DateTime.UtcNow:yyyyMMdd}.pdf",
                ContentType = "application/pdf"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice PDF");
            throw;
        }
    }

    private byte[] GeneratePdfContent(dynamic invoice)
    {
        // This is a simplified version that generates a PDF-like structure
        // In production, use iText7, SelectPdf, or similar
        var sb = new StringBuilder();

        sb.AppendLine("%PDF-1.4");
        sb.AppendLine("1 0 obj");
        sb.AppendLine("<<");
        sb.AppendLine("/Type /Catalog");
        sb.AppendLine("/Pages 2 0 R");
        sb.AppendLine(">>");
        sb.AppendLine("endobj");
        sb.AppendLine("2 0 obj");
        sb.AppendLine("<<");
        sb.AppendLine("/Type /Pages");
        sb.AppendLine("/Kids [3 0 R]");
        sb.AppendLine("/Count 1");
        sb.AppendLine(">>");
        sb.AppendLine("endobj");
        sb.AppendLine("3 0 obj");
        sb.AppendLine("<<");
        sb.AppendLine("/Type /Page");
        sb.AppendLine("/Parent 2 0 R");
        sb.AppendLine("/MediaBox [0 0 612 792]");
        sb.AppendLine("/Contents 4 0 R");
        sb.AppendLine("/Resources <<");
        sb.AppendLine("/Font <<");
        sb.AppendLine("/F1 <<");
        sb.AppendLine("/Type /Font");
        sb.AppendLine("/Subtype /Type1");
        sb.AppendLine("/BaseFont /Helvetica");
        sb.AppendLine(">>>");
        sb.AppendLine(">>>");
        sb.AppendLine(">>");
        sb.AppendLine("endobj");
        sb.AppendLine("4 0 obj");
        sb.AppendLine("<<");

        // Generate invoice text content
        var invoiceContent = new StringBuilder();
        invoiceContent.AppendLine("BT");
        invoiceContent.AppendLine("/F1 12 Tf");
        invoiceContent.AppendLine("50 750 Td");
        invoiceContent.AppendLine("(VEHICLE PARTS SALES INVOICE) Tj");
        invoiceContent.AppendLine("0 -30 Td");
        invoiceContent.AppendLine($"(Invoice #: {invoice.SalesInvoiceId}) Tj");
        invoiceContent.AppendLine($"(Date: {invoice.InvoiceDate:yyyy-MM-dd}) Tj");
        invoiceContent.AppendLine("0 -30 Td");
        invoiceContent.AppendLine("(Customer Information:) Tj");
        invoiceContent.AppendLine($"(Name: {invoice.Customer.FullName}) Tj");
        invoiceContent.AppendLine($"(Email: {invoice.Customer.Email}) Tj");
        invoiceContent.AppendLine($"(Phone: {invoice.Customer.Phone}) Tj");
        invoiceContent.AppendLine("0 -30 Td");
        invoiceContent.AppendLine("(Vehicle:) Tj");
        invoiceContent.AppendLine($"({invoice.Vehicle.Brand} {invoice.Vehicle.Model} ({invoice.Vehicle.VehicleNumber})) Tj");
        invoiceContent.AppendLine("0 -30 Td");
        invoiceContent.AppendLine("(Items:) Tj");

        var yOffset = 0;
        foreach (var item in invoice.SalesInvoiceItems)
        {
            invoiceContent.AppendLine($"({item.Part.PartName} - Qty: {item.Quantity} x ${item.PricePerUnit} = ${item.Quantity * item.PricePerUnit}) Tj");
            yOffset -= 15;
        }

        invoiceContent.AppendLine("0 -30 Td");
        invoiceContent.AppendLine($"(Total: ${invoice.TotalAmount}) Tj");
        invoiceContent.AppendLine($"(Payment Status: {invoice.PaymentStatus}) Tj");
        invoiceContent.AppendLine("ET");

        var streamContent = invoiceContent.ToString();
        sb.AppendLine($"/Length {streamContent.Length}");
        sb.AppendLine(">>");
        sb.AppendLine("stream");
        sb.Append(streamContent);
        sb.AppendLine("endstream");
        sb.AppendLine("endobj");
        sb.AppendLine("xref");
        sb.AppendLine("0 5");
        sb.AppendLine("0000000000 65535 f");
        sb.AppendLine("0000000009 00000 n");
        sb.AppendLine("0000000058 00000 n");
        sb.AppendLine("0000000115 00000 n");
        sb.AppendLine("0000000280 00000 n");
        sb.AppendLine("trailer");
        sb.AppendLine("<<");
        sb.AppendLine("/Size 5");
        sb.AppendLine("/Root 1 0 R");
        sb.AppendLine(">>");
        sb.AppendLine("startxref");
        sb.AppendLine("800");
        sb.AppendLine("%%EOF");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
