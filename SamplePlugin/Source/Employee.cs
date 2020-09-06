using Newtonsoft.Json;

namespace SamplePlugin
{
    public class Employee
    {
        [JsonProperty("status")]
        public string Status { get; private set; }
    }
}