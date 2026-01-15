namespace Backend.Models
{
    public class Station
    {
        /// <summary>
        /// Código identificador único de la estación
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// Nombre de la estación
        /// </summary>
        public string name { get; set; } = string.Empty;
        /// <summary>
        /// Tipo de estación (Fija, Móvil u Otros)
        /// </summary>
        public StationType type { get; set; }
        /// <summary>
        /// Dirección física de la estación
        /// </summary>
        public string? address { get; set; }
        /// <summary>
        /// Código postal de la estación
        /// </summary>
        public string? postal_code { get; set; }
        /// <summary>
        /// Longitud geográfica de la estación
        /// </summary>
        public double? longitude { get; set; }
        /// <summary>
        /// Latitud geográfica de la estación
        /// </summary>
        public double? latitude { get; set; }
        /// <summary>
        /// Descripción de la estación
        /// </summary>
        public string? description { get; set; }
        /// <summary>
        /// Horario de atención de la estación
        /// </summary>
        public string? schedule { get; set; }
        /// <summary>
        /// Información de contacto de la estación
        /// </summary>
        public string? contact { get; set; }
        /// <summary>
        /// URL del sitio web de la estación
        /// </summary>
        public string? url { get; set; }
        /// <summary>
        /// Código de la localidad a la que pertenece la estación
        /// </summary>
        public int locality_code { get; set; }
        /// <summary>
        /// Nombre de la localidad (rellenado en consultas con JOIN)
        /// </summary>
        public string? locality { get; set; }
        /// <summary>
        /// Nombre de la provincia (rellenado en consultas con JOIN)
        /// </summary>
        public string? province { get; set; }

        // Relación: Una estación pertenece a una localidad
        //public Locality? locality { get; set; }
    }

    /// <summary>
    /// Enumeración para el tipo de estación ITV
    /// </summary>
    public enum StationType
    {
        /// <summary>
        /// Estación fija (ubicación permanente)
        /// </summary>
        Fixed_station,
        /// <summary>
        /// Estación móvil (unidad itinerante)
        /// </summary>
        Mobile_station,
        /// <summary>
        /// Otro tipo de estación
        /// </summary>
        Others
    }
}
