using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Alpha.Entity; // Assuming Product is defined in this namespace
using Data.Abstract;

namespace Data.Concrete.EfCore
{
    public class EfCoreProductRepository : IProductRepository
    {
        private readonly ShopContext _context;

        public EfCoreProductRepository(ShopContext context)
        {
            _context = context;
        }


        public async Task<Product?> GetByIdAsync(int id) 
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0.", nameof(id));

            return await _context.Products
                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Image)
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Image)
                .ToListAsync();
        }

        public async Task<Product> CreateAsync(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product; // Return the created product
        }


        public async Task UpdateAsync(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0.", nameof(id));

            var product = await _context.Products
                .Include(p => p.ProductCategories) // Include child entities
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product != null)
            {
                // Remove child relationships
                _context.ProductCategory.RemoveRange(product.ProductCategories);
                _context.ProductImage.RemoveRange(product.ProductImages);

                // Remove the parent product
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException($"Product with ID {id} does not exist.");
            }
        }

        public async Task<List<Product>> GetRecentProductsAsync()
        {
            var oneMonthAgo = DateTime.Now.AddMonths(-1);
            var oneYearAgo = DateTime.Now.AddYears(-1);

            // Fetch products with their images using lazy loading or eager loading
            var recentProducts = await _context.Products
                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Image) // Include the Image entity
                .Where(p => p.DateAdded >= oneMonthAgo)
                .OrderByDescending(p => p.DateAdded)
                .ToListAsync();

            if (!recentProducts.Any())
            {
                recentProducts = await _context.Products
                    .Include(p => p.ProductImages)
                        .ThenInclude(pi => pi.Image)
                    .Where(p => p.DateAdded >= oneYearAgo)
                    .OrderByDescending(p => p.DateAdded)
                    .ToListAsync();
            }

            if (!recentProducts.Any())
            {
                var lastProduct = await _context.Products
                    .Include(p => p.ProductImages)
                        .ThenInclude(pi => pi.Image)
                    .OrderByDescending(p => p.DateAdded)
                    .FirstOrDefaultAsync();

                if (lastProduct != null)
                {
                    recentProducts.Add(lastProduct);
                }
            }

            return recentProducts;
        }
        public async Task<List<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int limit = 4)
            {
                return await _context.Products
                    .Where(p => p.CategoryId == categoryId && p.ProductId != excludeProductId) // Filter by category and exclude the product
                    .Include(p => p.ProductImages) // Include images
                        .ThenInclude(pi => pi.Image)
                    .OrderByDescending(p => p.DateAdded) // Optional: Order by recent additions
                    .Take(limit) // Limit the number of results
                    .ToListAsync();
            }



    }
}
