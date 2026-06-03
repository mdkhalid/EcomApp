using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class ReturnRepository : IReturnRepository
{
    private readonly ApplicationDbContext _context;

    public ReturnRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReturnRequest?> GetByIdAsync(int id)
    {
        return await _context.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<ReturnRequest>> GetByUserIdAsync(int userId)
    {
        return await _context.ReturnRequests
            .Include(r => r.Order)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<ReturnRequest?> GetByOrderIdAsync(int orderId, int userId)
    {
        return await _context.ReturnRequests
            .Include(r => r.Order)
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.UserId == userId);
    }

    public async Task<bool> HasPendingReturnAsync(int orderId, int userId)
    {
        return await _context.ReturnRequests
            .AnyAsync(r => r.OrderId == orderId && r.UserId == userId
                && r.Status == ReturnStatus.Requested);
    }

    public async Task<ReturnRequest> CreateAsync(ReturnRequest returnRequest)
    {
        _context.ReturnRequests.Add(returnRequest);
        await _context.SaveChangesAsync();
        return returnRequest;
    }

    public async Task<ReturnRequest?> UpdateStatusAsync(int id, ReturnStatus status, string? adminNote = null)
    {
        var returnRequest = await _context.ReturnRequests.FindAsync(id);
        if (returnRequest == null)
            return null;

        returnRequest.Status = status;
        returnRequest.UpdatedAt = DateTime.UtcNow;

        if (adminNote != null)
            returnRequest.AdminNote = adminNote;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<(List<ReturnRequest> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? status = null)
    {
        var query = _context.ReturnRequests
            .AsNoTracking()
            .Include(r => r.Order)
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReturnStatus>(status, true, out var returnStatus))
        {
            query = query.Where(r => r.Status == returnStatus);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
