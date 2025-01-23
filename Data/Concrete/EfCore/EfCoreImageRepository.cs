using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Alpha.Entity;
using Data.Abstract;

namespace Data.Concrete.EfCore
{
    public class EfCoreImageRepository : IImageRepository
    {
        private readonly ShopContext _context;

        public EfCoreImageRepository(ShopContext context)
        {
            _context = context;
        }

        public async Task<Image?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0.", nameof(id));

            return await _context.Images.FindAsync(id);
        }

        public async Task<List<Image>> GetAllAsync()
        {
            return await _context.Images.ToListAsync();
        }

        public async Task CreateAsync(Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            await _context.Images.AddAsync(image);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            _context.Images.Update(image);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0.", nameof(id));

            var image = await GetByIdAsync(id);
            if (image != null)
            {
                _context.Images.Remove(image);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException($"Image with ID {id} does not exist.");
            }
        }
        public async Task<List<Image>> GetImagesByProductIdAsync(int productId)
        {
            if (productId <= 0) throw new ArgumentException("Product ID must be greater than 0.", nameof(productId));

            return await _context.Images
                .Where(img => img.ProductImages.Any(pi => pi.ProductId == productId))
                .ToListAsync();
        }
        public async Task UpdateProductImagesAsync(int productId, List<int> imageIds)
        {
            if (productId <= 0) throw new ArgumentException("Product ID must be greater than 0.", nameof(productId));
            if (imageIds == null) throw new ArgumentNullException(nameof(imageIds), "Image IDs cannot be null.");

            // Fetch the product
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                throw new InvalidOperationException($"No product found with ID {productId}.");
            }

            // Call the new helper method
            await UpdateProductImageAssociationsAsync(product, imageIds);
        }


        private async Task UpdateProductImageAssociationsAsync(Product product, List<int> imageIds)
        {
            // Step 1: Remove existing associations
            product.ProductImages.Clear();

            // Step 2: Add new associations
            foreach (var imageId in imageIds)
            {
                var image = await _context.Images.FindAsync(imageId);
                if (image != null)
                {
                    product.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageId = imageId
                    });
                }
            }

            // Step 3: Save changes to the database
            await _context.SaveChangesAsync();
            Console.WriteLine($"Image associations for product ID {product.ProductId} have been updated.");
        }

    }
}
