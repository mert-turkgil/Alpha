using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Data.Configuration;
using Data.Abstract;
using Alpha.Entity;

namespace Data.Concrete.EfCore
{
    public class EfCoreBlogRepository : IBlogRepository
    {
        private readonly ShopContext _context;

        public EfCoreBlogRepository(ShopContext context)
        {
            _context = context;
        }

        
        #region Search

        // 1. Search by Title
        public async Task<List<Blog>> SearchByTitleAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Blog>();

            searchTerm = searchTerm.ToLower();

            return await IncludeAllRelatedEntities(_context.Blogs)
                        .Where(b => b.Title.ToLower().Contains(searchTerm))
                        .ToListAsync();
        }

        // 2. Search by Content
        public async Task<List<Blog>> SearchByContentAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Blog>();

            searchTerm = searchTerm.ToLower();

            return await IncludeAllRelatedEntities(_context.Blogs)
                        .Where(b => b.Content.ToLower().Contains(searchTerm))
                        .ToListAsync();
        }

        // 4. Search by Brand
        public async Task<List<Blog>> SearchByBrandAsync(string brand)
        {
            if (string.IsNullOrWhiteSpace(brand))
                return new List<Blog>();

            brand = brand.ToLower();

            return await IncludeAllRelatedEntities(_context.Blogs)
                        .Where(b => b.ProductBlogs.Any(pb => 
                                pb.Product.Brand.ToLower().Contains(brand)))
                        .ToListAsync();
        }

        // 5. Search with Multiple Filters
        public async Task<List<Blog>> SearchWithFiltersAsync(string searchTerm, string category, string brand)
        {
            var query = IncludeAllRelatedEntities(_context.Blogs);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(lowerSearchTerm)
                                        || b.Content.ToLower().Contains(lowerSearchTerm));
            }

            if (!string.IsNullOrEmpty(category) && category != "All Categories")
            {
                query = query.Where(b => b.CategoryBlogs.Any(cb => cb.Category.Name == category));
            }

            if (!string.IsNullOrEmpty(brand))
            {
                var lowerBrand = brand.ToLower();
                query = query.Where(b => b.ProductBlogs.Any(pb => pb.Product.Brand.ToLower().Contains(lowerBrand)));
            }

            return await query.ToListAsync();
        }

        // 6. Paginate Results
        public async Task<List<Blog>> SearchWithPaginationAsync(
            string searchTerm, 
            string category, 
            string brand, 
            int pageIndex, 
            int pageSize = 3)
        {
            var query = IncludeAllRelatedEntities(_context.Blogs);

            // Apply Filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(lowerSearchTerm)
                                        || b.Content.ToLower().Contains(lowerSearchTerm));
            }

            if (!string.IsNullOrEmpty(category) && category != "All Categories")
            {
                query = query.Where(b => b.CategoryBlogs.Any(cb => cb.Category.Name == category));
            }

            if (!string.IsNullOrEmpty(brand))
            {
                var lowerBrand = brand.ToLower();
                query = query.Where(b => b.ProductBlogs.Any(pb => pb.Product.Brand.ToLower().Contains(lowerBrand)));
            }

            // Apply Pagination
            return await query
                        .Skip(pageIndex * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
        }


        // Helper Method to Include Related Entities
        private IQueryable<Blog> IncludeAllRelatedEntities(IQueryable<Blog>? query = null)
        {
            query ??= _context.Blogs;

            return query
                .Include(b => b.ProductBlogs)
                    .ThenInclude(pb => pb.Product)
                        .ThenInclude(p => p.ProductImages)
                            .ThenInclude(pi => pi.Image)
                .Include(b => b.CategoryBlogs)
                    .ThenInclude(cb => cb.Category);
        }

        #endregion

        public async Task<Blog?> GetByIdAsync(int id)
        {
            return await _context.Blogs
                .Where(m => m.BlogId == id)
                .Include(m => m.ProductBlogs)
                    .ThenInclude(pb => pb.Product)
                        .ThenInclude(p => p.ProductImages)
                            .ThenInclude(pi => pi.Image)
                .Include(m => m.CategoryBlogs)
                    .ThenInclude(m => m.Category)
                .FirstOrDefaultAsync();
        }



public async Task<List<Blog>> GetAllAsync()
{
    return await _context.Blogs
        .Include(b => b.ProductBlogs)            // Bridge table between Blog & Product
            .ThenInclude(pb => pb.Product)       // The actual Product
                .ThenInclude(p => p.ProductImages) 
                    .ThenInclude(pi => pi.Image)  // The "Image" entity
        .Include(b => b.CategoryBlogs)           // Bridge table between Blog & Category
            .ThenInclude(cb => cb.Category)      // The actual Category
        .ToListAsync();
}


        public async Task<Blog> CreateAsync(Blog blog)
        {
            await _context.Blogs.AddAsync(blog);
            await _context.SaveChangesAsync();
            return blog;
        }

        public async Task<Blog> UpdateAsync(Blog blog)
        {
            _context.Blogs.Update(blog);
            await _context.SaveChangesAsync();
            return blog;
        }

        public async Task DeleteAsync(int id)
        {
            // Retrieve the blog
            var blog = await GetByIdAsync(id);
            if (blog == null)
            {
                Console.WriteLine($"[ERROR] Blog with ID {id} not found.");
                return;
            }

            // Remove related entities (ProductBlogs, CategoryBlogs)
            await RemoveRelatedEntitiesAsync(id);

            // Remove the blog itself
            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[DEBUG] Blog with ID {id} deleted successfully.");
        }

        public async Task<List<Blog>> GetRecentBlogsAsync()
        {
            var oneWeekAgo = DateTime.Now.AddDays(-7);
            var oneMonthAgo = DateTime.Now.AddMonths(-1);

            var recentBlogs = await _context.Blogs
                .Where(b => b.Date >= oneWeekAgo)
                .OrderByDescending(b => b.Date)
                  .Include(m => m.ProductBlogs)                  // Include ProductBlogs
                    .ThenInclude(pb => pb.Product)             // Include related Product
                .Include(m=>m.CategoryBlogs).ThenInclude(m=>m.Category)
                .ToListAsync();

            if (!recentBlogs.Any())
            {
                recentBlogs = await _context.Blogs
                    .Where(b => b.Date >= oneMonthAgo)
                    .OrderByDescending(b => b.Date)
                      .Include(m => m.ProductBlogs)                  // Include ProductBlogs
                        .ThenInclude(pb => pb.Product)             // Include related Product
                        .ThenInclude(p => p.ProductImages)     // Include ProductImages in Product
                        .ThenInclude(pi => pi.Image)       // Include Image in ProductImages
                        .Include(m => m.Image)
                        .Include(m=>m.CategoryBlogs).ThenInclude(m=>m.Category)
                    .ToListAsync();
            }

            if (!recentBlogs.Any())
            {
                var lastBlog = await _context.Blogs
                    .OrderByDescending(b => b.Date)
                    .Include(m => m.ProductBlogs)                  // Include ProductBlogs
                    .ThenInclude(pb => pb.Product)             // Include related Product
                    .ThenInclude(p => p.ProductImages)     // Include ProductImages in Product
                    .ThenInclude(pi => pi.Image)       // Include Image in ProductImages
                    .Include(m => m.Image)
                    .Include(m=>m.CategoryBlogs).ThenInclude(m=>m.Category)
                    .FirstOrDefaultAsync();

                if (lastBlog != null)
                    recentBlogs.Add(lastBlog);
            }

            return recentBlogs;
        }
        
        public async Task RemoveRelatedEntitiesAsync(int blogId)
        {
            // Find all related ProductBlogs
            var productBlogs = await _context.ProductBlog
                .Where(pb => pb.BlogId == blogId)
                .ToListAsync();

            if (productBlogs.Any())
            {
                _context.ProductBlog.RemoveRange(productBlogs);
                Console.WriteLine($"[DEBUG] Removed {productBlogs.Count} related ProductBlogs.");
            }

            // Find all related CategoryBlogs
            var categoryBlogs = await _context.CategoryBlog
                .Where(cb => cb.BlogId == blogId)
                .ToListAsync();

            if (categoryBlogs.Any())
            {
                _context.CategoryBlog.RemoveRange(categoryBlogs);
                Console.WriteLine($"[DEBUG] Removed {categoryBlogs.Count} related CategoryBlogs.");
            }

            // Commit changes to the database
            await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] Related entities for Blog ID {blogId} removed successfully.");
        }
        //Category
            public async Task<List<int>> GetCategoryIdsByBlogIdAsync(int blogId)
            {
                return await _context.CategoryBlog
                    .Where(bc => bc.BlogId == blogId)
                    .Select(bc => bc.CategoryId)
                    .ToListAsync();
            }

            public async Task UpdateBlogCategories(int blogId, List<int> categoryIds)
            {
                var existingMappings = _context.CategoryBlog.Where(cb => cb.BlogId == blogId).ToList();
                _context.CategoryBlog.RemoveRange(existingMappings);

                foreach (var categoryId in categoryIds)
                {
                    _context.CategoryBlog.Add(new CategoryBlog
                    {
                        BlogId = blogId,
                        CategoryId = categoryId
                    });
                }

                await _context.SaveChangesAsync();
}


    }
}
