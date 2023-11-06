using Newtonsoft.Json;
using System.Collections.Generic;

namespace Org.Quickstart.API.Models
{
    public class Product
    {
        public string ImageUrl { get; set; }
        public string Name { get; set; }
        public string Price { get; set; }
        public string Description { get; set; }
        public string Quantity { get; set; }
        public Category Category { get; set; }
        public Manufacturer Manufacturer { get; set; }

        public ICollection<Coupon> Coupons { get; set; }

        public Product GetProduct()
        {
            return new Product
            {
                ImageUrl = this.ImageUrl,
                Name = this.Name,
                Price = this.Price,
                Description = this.Description,
                Quantity = this.Quantity,
                Category = this.Category,
                Manufacturer = this.Manufacturer,
                Coupons = this.Coupons


            };
        }
        public string GenerateNextProductId(string lastId)
        {
            if (string.IsNullOrEmpty(lastId))
            {
                return "p1";
            }

            int lastNumber = int.Parse(lastId.Substring(1)); // Extract the numeric part
            int nextNumber = lastNumber + 1;

            return "p" + nextNumber;
        }
    }
    public class UserProduct
    {
        public string ProductId { get; set; }
        public string ImageUrl { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
