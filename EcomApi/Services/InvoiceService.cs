using EcomApi.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EcomApi.Services;

public class InvoiceService : IInvoiceService
{
    public async Task<byte[]> GenerateInvoicePdfAsync(Order order)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, order));
                page.Content().Element(c => ComposeContent(c, order));
                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        using var ms = new MemoryStream();
        doc.GeneratePdf(ms);
        return ms.ToArray();
    }

    private void ComposeHeader(IContainer container, Order order)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("INVOICE").FontSize(24).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text($"Invoice #INV-{order.Id:D5}").FontSize(14).FontColor(Colors.Grey.Darken2);
                    c.Item().Text($"Order #ORD-{order.Id:D5}").FontSize(12).FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"Date: {order.CreatedAt:dd MMM yyyy}").FontSize(11).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(150).Column(c =>
                {
                    c.Item().AlignRight().Text("EcomX").FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().AlignRight().Text("Your Online Store").FontSize(10).FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container, Order order)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Bill To:").FontSize(12).Bold();
                    c.Item().PaddingTop(4).Text(order.ShippingName).FontSize(11);
                    c.Item().Text(order.ShippingAddress).FontSize(11);
                    c.Item().Text($"{order.ShippingCity}, {order.ShippingZip}").FontSize(11);
                });

                row.ConstantItem(180).Column(c =>
                {
                    c.Item().Text("Order Details:").FontSize(12).Bold();
                    c.Item().PaddingTop(4).Text($"Status: {order.Status}").FontSize(11);
                    if (!string.IsNullOrEmpty(order.TrackingNumber))
                        c.Item().Text($"Tracking: {order.TrackingNumber}").FontSize(11);
                    if (!string.IsNullOrEmpty(order.Carrier))
                        c.Item().Text($"Carrier: {order.Carrier}").FontSize(11);
                });
            });

            col.Item().PaddingVertical(12).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Medium).Padding(8).Text("Item").FontSize(10).Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Medium).Padding(8).Text("Qty").FontSize(10).Bold().FontColor(Colors.White).AlignCenter();
                    header.Cell().Background(Colors.Blue.Medium).Padding(8).Text("Price").FontSize(10).Bold().FontColor(Colors.White).AlignRight();
                    header.Cell().Background(Colors.Blue.Medium).Padding(8).Text("Total").FontSize(10).Bold().FontColor(Colors.White).AlignRight();
                });

                foreach (var item in order.Items)
                {
                    var productLabel = item.VariantName != null ? $"{item.ProductName} ({item.VariantName})" : item.ProductName;
                    table.Cell().Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(productLabel).FontSize(10);
                    table.Cell().Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.Quantity.ToString()).FontSize(10).AlignCenter();
                    table.Cell().Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text($"${item.UnitPrice:F2}").FontSize(10).AlignRight();
                    table.Cell().Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text($"${item.TotalPrice:F2}").FontSize(10).AlignRight();
                }
            });

            col.Item().PaddingTop(8).AlignRight().Column(c =>
            {
                var subtotal = order.Items.Sum(i => i.TotalPrice);
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text("Subtotal:").FontSize(11).AlignRight();
                    r.ConstantItem(80).Text($"${subtotal:F2}").FontSize(11).AlignRight();
                });

                if (order.DiscountAmount > 0)
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Discount ({order.CouponCode}):").FontSize(11).AlignRight().FontColor(Colors.Green.Medium);
                        r.ConstantItem(80).Text($"-${order.DiscountAmount:F2}").FontSize(11).AlignRight().FontColor(Colors.Green.Medium);
                    });
                }

                c.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                c.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total:").FontSize(14).Bold().AlignRight();
                    r.ConstantItem(80).Text($"${order.TotalAmount:F2}").FontSize(14).Bold().AlignRight();
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Column(col =>
        {
            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(8).Text("Thank you for your purchase!").FontSize(10).FontColor(Colors.Grey.Darken1);
            col.Item().Text("EcomX - Your Online Store").FontSize(10).FontColor(Colors.Grey.Darken1);
            col.Item().Text("For any queries, contact support@ecomx.com").FontSize(9).FontColor(Colors.Grey.Lighten1);
        });
    }
}
