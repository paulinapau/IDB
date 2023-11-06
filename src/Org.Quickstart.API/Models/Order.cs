using System;
using System.Collections.Generic;

namespace Org.Quickstart.API.Models
{
    public class Order
    {
        
        public DateTime SubmissionDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public ICollection<OrderProduct> OrderProducts { get; set; }
    }
}
