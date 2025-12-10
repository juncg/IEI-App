using System.Collections.Generic;

namespace Backend.Models
{
    public class MapResult
    {
        public List<UnifiedData> UnifiedData { get; set; } = new();
        public List<RepairedRecord> RepairedRecords { get; set; } = new();
        public List<DiscardedRecord> DiscardedRecords { get; set; } = new();
    }
}