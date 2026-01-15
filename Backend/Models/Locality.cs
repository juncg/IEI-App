namespace Backend.Models
{
    public class Locality
    {
        /// <summary>
        /// Código identificador único de la localidad
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// Nombre de la localidad
        /// </summary>
        public string name { get; set; } = string.Empty;
        /// <summary>
        /// Código de la provincia a la que pertenece la localidad
        /// </summary>
        public int province_code { get; set; }

        // Relación: Una localidad pertenece a una provincia
        //public Province? province { get; set; }

        // Relación: Una localidad tiene muchas estaciones
        //public ICollection<Station>? stations { get; set; }
    }
}
