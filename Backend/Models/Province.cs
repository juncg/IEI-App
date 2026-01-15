namespace Backend.Models
{
    public class Province
    {
        /// <summary>
        /// Código identificador único de la provincia
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// Nombre de la provincia
        /// </summary>
        public string name { get; set; } = string.Empty;

        // Relación: Una provincia tiene muchas localidades
        //public ICollection<Locality>? Localities { get; set; }
    }
}
