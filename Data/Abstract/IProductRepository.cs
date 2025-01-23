using System.Collections.Generic;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Data.Abstract
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(int id);
        Task<List<Product>> GetRecentProductsAsync();
        Task<List<Product>> GetAllAsync();
        Task<Product> CreateAsync(Product product);
        Task UpdateAsync(Product entity);
        Task DeleteAsync(int id);
        Task<List<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int limit = 4);
    }
}
