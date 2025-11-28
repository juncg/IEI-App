namespace Backend.Models
{
    public class UnifiedData
    {
        public string ProvinceName { get; set; } = string.Empty;
        public string LocalityName { get; set; } = string.Empty;
        public Station Station { get; set; } = new Station();
    }
}
