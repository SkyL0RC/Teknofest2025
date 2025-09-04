namespace Yandes.DTOs
{
    public class SatelliteDataResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public SatelliteDataInfo Data { get; set; }
        public List<string> AvailableBands { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SatelliteDataInfo
    {
        public string Id { get; set; }
        public string SatelliteType { get; set; } // Sentinel1, Landsat8, Sentinel2
        public string DataType { get; set; } // SAR, Optical
        public DateTime AcquisitionDate { get; set; }
        public string DataPath { get; set; }
        public List<string> AvailableBands { get; set; } = new();
        public Dictionary<string, string> BandInfo { get; set; } = new();
        public string Projection { get; set; }
        public double Resolution { get; set; }
        public string Coverage { get; set; }
    }

    public class FireAnalysisRequest
    {
        public string BeforeDataId { get; set; }
        public string AfterDataId { get; set; }
        public string AnalysisType { get; set; } // NDVI, NBR, ChangeDetection
        public List<string> Bands { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class FireAnalysisResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AnalysisId { get; set; }
        public string ResultImagePath { get; set; }
        public Dictionary<string, double> Statistics { get; set; } = new();
        public List<string> AffectedAreas { get; set; } = new();
        public DateTime AnalysisDate { get; set; }
    }

    public class DataProcessingStatus
    {
        public string JobId { get; set; }
        public string Status { get; set; } // Processing, Completed, Failed
        public int Progress { get; set; }
        public string Message { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}

