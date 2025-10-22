namespace Backend.Models
{
    public class Province
    {
        public int code { get; set; }
        public string name { get; set; } = string.Empty;

        // Relación: Una provincia tiene muchas localidades
        //public ICollection<Locality>? Localities { get; set; }
    }
}
