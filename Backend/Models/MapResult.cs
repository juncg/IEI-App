using System.Collections.Generic;

namespace Backend.Models
{
    public class MapResult
    {
        /// <summary>
        /// Lista de datos unificados de estaciones de todas las fuentes
        /// </summary>
        public List<UnifiedData> UnifiedData { get; set; } = new();
        /// <summary>
        /// Lista de registros reparados durante el mapeo
        /// </summary>
        public List<RepairedRecord> RepairedRecords { get; set; } = new();
        /// <summary>
        /// Lista de registros descartados durante el mapeo
        /// </summary>
        public List<DiscardedRecord> DiscardedRecords { get; set; } = new();
    }
}