using Yandes.DTOs;

namespace Yandes.Services
{
    public class FireService : IFireService
    {
        public Task<ApiResponse<IEnumerable<FireHotspotDto>>> GetNearbyHotspotsAsync(double lat, double lng, double radiusKm)
        {
            var now = DateTime.UtcNow;
            var data = new List<FireHotspotDto>
            {
                new FireHotspotDto { Latitude = lat + 0.05, Longitude = lng + 0.05, Confidence = 0.82, DetectedAtUtc = now.AddMinutes(-15), Source = "stub", Note = "Yakın sıcak nokta" },
                new FireHotspotDto { Latitude = lat - 0.08, Longitude = lng + 0.03, Confidence = 0.67, DetectedAtUtc = now.AddMinutes(-40), Source = "stub" }
            };
            return Task.FromResult(new ApiResponse<IEnumerable<FireHotspotDto>>
            {
                Success = true,
                Message = "Yakın sıcak noktalar",
                Data = data
            });
        }

        public Task<ApiResponse<IEnumerable<FireHotspotDto>>> GetRecentHotspotsAsync()
        {
            var now = DateTime.UtcNow;
            var data = new List<FireHotspotDto>
            {
                new FireHotspotDto { Latitude = 41.0, Longitude = 29.0, Confidence = 0.9, DetectedAtUtc = now.AddMinutes(-10), Source = "stub" },
                new FireHotspotDto { Latitude = 38.4, Longitude = 27.1, Confidence = 0.7, DetectedAtUtc = now.AddMinutes(-55), Source = "stub" }
            };
            return Task.FromResult(new ApiResponse<IEnumerable<FireHotspotDto>>
            {
                Success = true,
                Message = "Son sıcak noktalar",
                Data = data
            });
        }
    }
}

