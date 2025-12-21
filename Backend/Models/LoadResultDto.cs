using System.Collections.Generic;

namespace Backend.Models
{
    public class LoadResultDto
    {
    public int RecordsLoadedCorrectly { get; set; }
    public int RecordsRepaired { get; set; }
    public List<RepairedRecord> RepairedRecords { get; set; } = new();
    public int RecordsDiscarded { get; set; }
    public List<DiscardedRecord> DiscardedRecords { get; set; } = new();
    }

    public class RepairedOperation
    {
        public string ErrorReason { get; set; } = string.Empty;
        public string OperationPerformed { get; set; } = string.Empty;
    }

    public class RepairedRecord
    {
        public string DataSource { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Locality { get; set; } = string.Empty;
        public List<RepairedOperation> Operations { get; set; } = new();
    }

    public class DiscardedRecord
    {
        public string DataSource { get; set; }
        public string Name { get; set; }
        public string Locality { get; set; }
        public string ErrorReason { get; set; }
    }
}