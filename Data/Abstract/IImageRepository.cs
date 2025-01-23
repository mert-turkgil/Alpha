using System.Collections.Generic;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Data.Abstract
{
    public interface IImageRepository
    {
        Task UpdateProductImagesAsync(int productId, List<int> imageIds);
        Task<List<Image>> GetImagesByProductIdAsync(int productId);
        Task<Image?> GetByIdAsync(int id);
        Task<List<Image>> GetAllAsync();
        Task CreateAsync(Image entity);
        Task UpdateAsync(Image entity);
        Task DeleteAsync(int id);
    }
}
