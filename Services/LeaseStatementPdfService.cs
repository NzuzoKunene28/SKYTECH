using QuestPDF.Fluent;
using QuestPDF.Helpers;
using SkyTech.Models;
using System;
using System.Linq;

namespace SkyTech.Services
{
    public class LeaseStatementPdfService
    {
        // agreement must have .Payments and .Customer loaded (Include them
        // wherever you call this from).
        public static byte[] Generate(LeaseAgreement agreement, LeasePayment justPaid)
        {
            return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("SkyTech Leasing").FontSize(18).Bold();
                        col.Item().Text($"Lease Statement — Agreement #{agreement.LeaseAgreementId}").FontSize(12);
                        col.Item().Text($"Customer: {agreement.Customer?.FirstName} {agreement.Customer?.LastName}");
                        col.Item().Text($"Statement generated: {DateTime.Now:dd MMM yyyy HH:mm}");
                    });

                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Due Date").Bold();
                            header.Cell().Text("Type").Bold();
                            header.Cell().Text("Amount Due").Bold();
                            header.Cell().Text("Amount Paid").Bold();
                            header.Cell().Text("Status").Bold();
                        });

                        foreach (var payment in agreement.Payments.OrderBy(p => p.DueDate))
                        {
                            bool isJustPaid = payment.LeasePaymentId == justPaid.LeasePaymentId;

                            table.Cell().Text(payment.DueDate.ToString("dd MMM yyyy"));
                            table.Cell().Text(payment.Type.ToString() + (payment.LateFeeApplied ? " (+late fee)" : ""));
                            table.Cell().Text($"R{payment.AmountDue:N2}");
                            table.Cell().Text($"R{payment.AmountPaid:N2}");
                            table.Cell().Text(isJustPaid ? "Paid (this statement)" : payment.Status.ToString());
                        }
                    });

                    page.Footer().Column(col =>
                    {
                        double totalPaid = agreement.Payments.Sum(p => p.AmountPaid);
                        double totalOutstanding = agreement.Payments
                            .Where(p => p.Status == LeasePaymentStatus.Pending)
                            .Sum(p => p.AmountDue);

                        col.Item().Text($"Total paid to date: R{totalPaid:N2}").Bold();
                        col.Item().Text($"Total currently outstanding: R{totalOutstanding:N2}");
                    });
                });
            }).GeneratePdf();
        }
    }
}

