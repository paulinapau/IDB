using System;

namespace Org.Quickstart.API.Models
{
    public class Coupon
    {
        public string CouponCode { get; set; }
        public double DiscountAmount{ get; set; }
        public DateTime DateTime { get; set; }
        public DateTime EndDate { get; set; }
    }
}
