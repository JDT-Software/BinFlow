using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ProductionTracker.Models
{
    // Main shift report document
    public class ShiftReport
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)] // FIXED: Added this attribute
        public DateTime Date { get; set; }

        [BsonElement("lineManager")]
        public string LineManager { get; set; } = string.Empty;

        [BsonElement("shift")]
        public string Shift { get; set; } = string.Empty;

        [BsonElement("binTippings")]
        public List<BinTipping> BinTippings { get; set; } = new();

        [BsonElement("totalTipped")]
        public int TotalTipped { get; set; }

        [BsonElement("averageWeight")]
        public double AverageWeight { get; set; }

        [BsonElement("totalDowntime")]
        public int TotalDowntime { get; set; }

        [BsonElement("cartonCounts")]
        public CartonCounts CartonCounts { get; set; } = new();

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)] // Keep UTC for audit timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)] // Keep UTC for audit timestamps
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // Individual bin tipping entry (hourly entry)
    public class BinTipping
    {
        [BsonElement("time")]
        public TimeSpan Time { get; set; }

        [BsonElement("binsTipped")]
        public int BinsTipped { get; set; }

        [BsonElement("averageBinWeight")]
        public double AverageBinWeight { get; set; }

        [BsonElement("downTime")]
        public int DownTime { get; set; }

        [BsonElement("reasonForNotAchievingTarget")]
        public string ReasonForNotAchievingTarget { get; set; } = string.Empty;

        [BsonElement("isLunchBreak")]
        public bool IsLunchBreak { get; set; }
    }

    // Hourly entry model for the new system
    public class HourlyEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)] // FIXED: Added this attribute
        public DateTime Date { get; set; }

        [BsonElement("time")]
        public TimeSpan Time { get; set; }

        [BsonElement("lineManager")]
        public string LineManager { get; set; } = string.Empty;

        [BsonElement("shift")]
        public string Shift { get; set; } = string.Empty;

        [BsonElement("productionLine")]
        public string ProductionLine { get; set; } = string.Empty;

        [BsonElement("binsTipped")]
        public int BinsTipped { get; set; }

        [BsonElement("averageBinWeight")]
        public double AverageBinWeight { get; set; }

        [BsonElement("downTime")]
        public int DownTime { get; set; }

        [BsonElement("reasonsNotes")]
        public string ReasonsNotes { get; set; } = string.Empty;

        [BsonElement("isLunchBreak")]
        public bool IsLunchBreak { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)] // Keep UTC for audit timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Carton tracking
    public class CartonCounts
    {
        [BsonElement("winscanCartons")]
        public int WinscanCartons { get; set; }

        [BsonElement("packers")]
        public int Packers { get; set; }

        [BsonElement("divPerHour")]
        public double DivPerHour { get; set; }
    }

    // For dashboard filtering and summary views
    public class ShiftSummary
    {
        public string Id { get; set; } = string.Empty;
        
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)] // FIXED: Added this attribute
        public DateTime Date { get; set; }
        
        public string LineManager { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public int TotalTipped { get; set; }
        public double AverageWeight { get; set; }
        public int TotalDowntime { get; set; }
        public double Efficiency { get; set; }
    }

    // For creating new entries
    public class CreateShiftReportDto
    {
        [Required]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)] // FIXED: Added this attribute
        public DateTime Date { get; set; }

        [Required]
        [StringLength(100)]
        public string LineManager { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Shift { get; set; } = string.Empty;
    }

    // For dashboard charts
    public class ProductionMetrics
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)] // FIXED: Added this attribute
        public DateTime Date { get; set; }
        
        public int TotalBinsTipped { get; set; }
        public double AverageWeight { get; set; }
        public int TotalDowntime { get; set; }
        public double EfficiencyPercentage { get; set; }
        public string LineManager { get; set; } = string.Empty;
    }

    // Enum for common downtime reasons
    public enum DowntimeReason
    {
        None,
        RotationFromJumbleFillers,
        PucVarietyExchange,
        PucExchange,
        Lunch,
        WaitingForPackingInstruction,
        CleaningForNextShift,
        MachineBreakdown,
        MaterialShortage,
        QualityIssue,
        Other
    }
}