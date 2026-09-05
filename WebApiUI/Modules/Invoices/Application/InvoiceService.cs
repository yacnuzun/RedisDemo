using Microsoft.EntityFrameworkCore;
using WebApiUI.Modules.Invoices.Domain;
using WebApiUI.Shared.Caching;
using WebApiUI.Shared.Persistence;

namespace WebApiUI.Modules.Invoices.Application
{
    public class InvoiceService: IInvoiceService
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public InvoiceService(AppDbContext context, ICacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        private static string CacheKey(int id) => $"invoice:{id}";

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            var cacheKey = CacheKey(id);

            var cached = await _cache.GetAsync<Invoice>(cacheKey);
            if (cached != null)
                return cached; // cache hit — DB'ye hiç gitmedik

            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
                await _cache.SetAsync(cacheKey, invoice, CacheDuration); // cache miss — DB'den geldi, cache'e yaz

            return invoice;
        }

        public async Task<List<Invoice>> GetAllAsync()
        {
            // Liste endpoint'i basit tutuluyor, cache'lenmiyor (liste cache'lemek invalidation'ı karmaşıklaştırır, demo kapsamı dışı)
            return await _context.Invoices.ToListAsync();
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            invoice.IssueDate = DateTime.SpecifyKind(invoice.IssueDate, DateTimeKind.Utc);
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task<bool> UpdateAsync(int id, Invoice updated)
        {
            updated.IssueDate = DateTime.SpecifyKind(updated.IssueDate, DateTimeKind.Utc);
            var existing = await _context.Invoices.FindAsync(id);
            if (existing == null) return false;

            existing.InvoiceNumber = updated.InvoiceNumber;
            existing.Amount = updated.Amount;
            existing.IssueDate = updated.IssueDate;
            existing.Status = updated.Status;

            await _context.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKey(id)); // invalidation — veri değişti, eski cache geçersiz

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.Invoices.FindAsync(id);
            if (existing == null) return false;

            _context.Invoices.Remove(existing);
            await _context.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKey(id)); // invalidation

            return true;
        }

    }
}
