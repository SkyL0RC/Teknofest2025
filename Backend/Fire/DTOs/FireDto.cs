namespace Yandes.DTOs
{
    public class FireHotspotDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Confidence { get; set; }
        public DateTime DetectedAtUtc { get; set; }
        public string Source { get; set; } = "stub";
        public string? Note { get; set; }
    }
}

