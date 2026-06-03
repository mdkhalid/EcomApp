using EcomApi.Models;

namespace EcomApi.Services;

public interface IInvoiceService
{
    Task<byte[]> GenerateInvoicePdfAsync(Order order);
}
