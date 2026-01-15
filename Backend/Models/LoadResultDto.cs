using System.Collections.Generic;

namespace Backend.Models
{
    public class LoadResultDto
    {
        /// <summary>
        /// Número de registros cargados correctamente en la base de datos
        /// </summary>
        public int RecordsLoadedCorrectly { get; set; }
        /// <summary>
        /// Número de registros reparados durante el proceso
        /// </summary>
        public int RecordsRepaired { get; set; }
        /// <summary>
        /// Lista de registros que fueron reparados
        /// </summary>
        public List<RepairedRecord> RepairedRecords { get; set; } = new();
        /// <summary>
        /// Número de registros descartados durante el proceso
        /// </summary>
        public int RecordsDiscarded { get; set; }
        /// <summary>
        /// Lista de registros descartados con razón de descarte
        /// </summary>
        public List<DiscardedRecord> DiscardedRecords { get; set; } = new();
    }

    public class RepairedOperation
    {
        /// <summary>
        /// Razón del error que requirió reparación
        /// </summary>
        public string ErrorReason { get; set; } = string.Empty;
        /// <summary>
        /// Operación realizada para corregir el error
        /// </summary>
        public string OperationPerformed { get; set; } = string.Empty;
    }

    public class RepairedRecord
    {
        /// <summary>
        /// Fuente de datos de donde proviene el registro (CAT, CV, GAL)
        /// </summary>
        public string DataSource { get; set; } = string.Empty;
        /// <summary>
        /// Nombre de la estación reparada
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Localidad de la estación reparada
        /// </summary>
        public string Locality { get; set; } = string.Empty;
        /// <summary>
        /// Lista de operaciones de reparación realizadas
        /// </summary>
        public List<RepairedOperation> Operations { get; set; } = new();
    }

    public class DiscardedRecord
    {
        /// <summary>
        /// Fuente de datos de donde proviene el registro (CAT, CV, GAL, DB)
        /// </summary>
        public string DataSource { get; set; }
        /// <summary>
        /// Nombre de la estación descartada
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Localidad de la estación descartada
        /// </summary>
        public string Locality { get; set; }
        /// <summary>
        /// Razón por la que el registro fue descartado
        /// </summary>
        public string ErrorReason { get; set; }
    }
}