using Yandes.DTOs;

namespace Yandes.Services
{
    public interface IFireService
    {
        Task<ApiResponse<IEnumerable<FireHotspotDto>>> GetNearbyHotspotsAsync(double lat, double lng, double radiusKm);
        Task<ApiResponse<IEnumerable<FireHotspotDto>>> GetRecentHotspotsAsync();
    }
}

