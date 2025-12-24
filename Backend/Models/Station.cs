namespace Backend.Models
{
    public class Station
    {
        public int code { get; set; }
        public string name { get; set; } = string.Empty;
        public StationType type { get; set; }
        public string? address { get; set; }
        public string? postal_code { get; set; }
        public double? longitude { get; set; }
        public double? latitude { get; set; }
        public string? description { get; set; }
        public string? schedule { get; set; }
        public string? contact { get; set; }
        public string? url { get; set; }
        public int locality_code { get; set; }
        public string? locality { get; set; }
        public string? province { get; set; }

        // Relación: Una estación pertenece a una localidad
        //public Locality? locality { get; set; }
    }

    // Enumeración para el tipo de estación
    public enum StationType
    {
        Fixed_station,
        Mobile_station,
        Others
    }
}
