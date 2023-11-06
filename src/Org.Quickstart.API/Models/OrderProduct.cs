namespace Org.Quickstart.API.Models
{
    public class OrderProduct
    {
        public int Quantity { get; set; }
        public double SubTotal { get; set; }
        public UserProduct product { get; set; }
    }
}
