namespace Backend.Models
{
    public class Locality
    {
        public int code { get; set; }
        public string name { get; set; } = string.Empty;
        public int province_code { get; set; }

        // Relación: Una localidad pertenece a una provincia
        //public Province? province { get; set; }

        // Relación: Una localidad tiene muchas estaciones
        //public ICollection<Station>? stations { get; set; }
    }
}
