using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Data.Abstract
{
    public interface IBlogRepository
    {
        Task<Blog?> GetByIdAsync(int id);
        Task<List<Blog>> GetAllAsync();
        Task<Blog> CreateAsync(Blog blog);
        Task<Blog> UpdateAsync(Blog blog);
        Task DeleteAsync(int id);
        Task<List<Blog>> GetRecentBlogsAsync();
        // New Method to Remove Related Entities
        Task RemoveRelatedEntitiesAsync(int blogId);
            // Search
        Task<List<Blog>> SearchByTitleAsync(string searchTerm);
        Task<List<Blog>> SearchByContentAsync(string searchTerm);
        Task<List<Blog>> SearchByBrandAsync(string brand);
        Task<List<Blog>> SearchWithPaginationAsync(
            string searchTerm, 
            string category, 
            string brand, 
            int pageIndex, 
            int pageSize = 3);
        Task<List<Blog>> SearchWithFiltersAsync(string searchTerm, string category, string brand);
        //Category
        Task<List<int>> GetCategoryIdsByBlogIdAsync(int blogId);
        Task UpdateBlogCategories(int blogId, List<int> categoryIds);
    }
}