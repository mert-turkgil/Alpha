using System;
using Alpha.Entity;
using Microsoft.EntityFrameworkCore;

namespace Data.Configuration
{
    public static class ModelBuilderExtensions
    {
        public static void Seed(this ModelBuilder builder)
        {
            // Images
            builder.Entity<Image>().HasData(
                new Image { ImageId = 1, ImageUrl = "image1.jpg", Text = "s1-s1p", ViewPhone = false },
                new Image { ImageId = 2, ImageUrl = "image2.jpg", Text = "S2-S2P", ViewPhone = false }
            );
            // Carousel
            builder.Entity<Carousel>().HasData(
                new Carousel
                {
                    CarouselId = 1,
                    CarouselTitle = "S1-S1P",
                    CarouselImage = "Carousel1.jpg",
                    CarouselImage600w = "600w-Carousel1.jpg",
                    CarouselImage1200w = "1200w-Carousel1.jpg",
                    CarouselDescription = "İş ayakkabılarını satın alırken kararsızlık yaşamak doğal olabilir...",
                    CarouselLink = "s1-s1p",
                    CarouselLinkText = "Buraya Tıkla"
                },
                new Carousel
                {
                    CarouselId = 2,
                    CarouselTitle = "S2-S2P",
                    CarouselImage = "Carousel2.jpg",
                    CarouselImage600w = "600w-Carousel2.jpg",
                    CarouselImage1200w = "1200w-Carousel2.jpg",
                    CarouselDescription = "İş ayakkabılarını satın alırken kararsızlık yaşamak doğal olabilir...",
                    CarouselLink = "s2-s2p",
                    CarouselLinkText = "Buraya Tıkla"
                }
            );
        }
    }
}
