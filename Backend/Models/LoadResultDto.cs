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

    public class RepairedRecord
    {
        public string DataSource { get; set; }
        public string Name { get; set; }
        public string Locality { get; set; }
        public string ErrorReason { get; set; }
        public string OperationPerformed { get; set; }
    }

    public class DiscardedRecord
    {
        public string DataSource { get; set; }
        public string Name { get; set; }
        public string Locality { get; set; }
        public string ErrorReason { get; set; }
    }
}