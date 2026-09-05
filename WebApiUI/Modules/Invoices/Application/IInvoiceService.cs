using WebApiUI.Modules.Invoices.Domain;

namespace WebApiUI.Modules.Invoices.Application
{
    public interface IInvoiceService
    {
        Task<Invoice?> GetByIdAsync(int id);
        Task<List<Invoice>> GetAllAsync();
        Task<Invoice> CreateAsync(Invoice invoice);
        Task<bool> UpdateAsync(int id, Invoice invoice);
        Task<bool> DeleteAsync(int id);
    }
}
