namespace Backend.Models
{
    public class StationDto
    {
        /// <summary>
        /// Código identificador único de la estación
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// Nombre de la estación
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Tipo de estación (Fija, Móvil u Otros)
        /// </summary>
        public StationType Type { get; set; }
        /// <summary>
        /// Dirección física de la estación
        /// </summary>
        public string? Address { get; set; }
        /// <summary>
        /// Código postal de la estación
        /// </summary>
        public string? PostalCode { get; set; }
        /// <summary>
        /// Longitud geográfica de la estación
        /// </summary>
        public double? Longitude { get; set; }
        /// <summary>
        /// Latitud geográfica de la estación
        /// </summary>
        public double? Latitude { get; set; }
        /// <summary>
        /// Nombre de la localidad
        /// </summary>
        public string? Locality { get; set; }
        /// <summary>
        /// Nombre de la provincia
        /// </summary>
        public string? Province { get; set; }
        /// <summary>
        /// Descripción de la estación
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// Horario de atención de la estación
        /// </summary>
        public string? Schedule { get; set; }
        /// <summary>
        /// Información de contacto de la estación
        /// </summary>
        public string? Contact { get; set; }
        /// <summary>
        /// URL del sitio web de la estación
        /// </summary>
        public string? Url { get; set; }
    }
}