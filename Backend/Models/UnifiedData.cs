namespace Backend.Models
{
    public class UnifiedData
    {
        /// <summary>
        /// Nombre de la provincia de la estación
        /// </summary>
        public string ProvinceName { get; set; } = string.Empty;
        /// <summary>
        /// Nombre de la localidad de la estación
        /// </summary>
        public string LocalityName { get; set; } = string.Empty;
        /// <summary>
        /// Datos de la estación
        /// </summary>
        public Station Station { get; set; } = new Station();
    }
}
