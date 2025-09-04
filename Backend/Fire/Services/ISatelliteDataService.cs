using Yandes.DTOs;

namespace Yandes.Services
{
    public interface ISatelliteDataService
    {
        Task<SatelliteDataResponse> ProcessSentinel1DataAsync(string dataPath);
        Task<SatelliteDataResponse> ProcessLandsatDataAsync(string dataPath);
        Task<SatelliteDataResponse> ProcessSentinel2DataAsync(string dataPath);
        Task<FireAnalysisResponse> AnalyzeFireDataAsync(FireAnalysisRequest request);
        Task<List<SatelliteDataInfo>> GetAvailableDataAsync();
        Task<byte[]> GetProcessedImageAsync(string dataId, string band);
    }
}

