using Newtonsoft.Json;

namespace Org.Quickstart.API.Models
{
    public class Address
    {

        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string FlatNumber { get; set; }
        public string City { get; set; }

    }
    public class MaxIdResult
    {
        [JsonProperty("$1")]
        public string MaxId { get; set; }
    }
}
