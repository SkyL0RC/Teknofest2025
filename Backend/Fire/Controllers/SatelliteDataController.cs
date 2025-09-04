using Microsoft.AspNetCore.Mvc;
using Yandes.Services;
using Yandes.DTOs;

namespace Yandes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SatelliteDataController : ControllerBase
    {
        private readonly ISatelliteDataService _satelliteDataService;
        private readonly ILogger<SatelliteDataController> _logger;

        public SatelliteDataController(ISatelliteDataService satelliteDataService, ILogger<SatelliteDataController> logger)
        {
            _satelliteDataService = satelliteDataService;
            _logger = logger;
        }

        /// <summary>
        /// Mevcut uydu verilerini listeler
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult<List<SatelliteDataInfo>>> GetAvailableData()
        {
            try
            {
                var data = await _satelliteDataService.GetAvailableDataAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available satellite data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Sentinel-1 SAR verilerini işler
        /// </summary>
        [HttpPost("sentinel1")]
        public async Task<ActionResult<SatelliteDataResponse>> ProcessSentinel1([FromBody] string dataPath)
        {
            try
            {
                var result = await _satelliteDataService.ProcessSentinel1DataAsync(dataPath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Sentinel-1 data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Landsat verilerini işler
        /// </summary>
        [HttpPost("landsat")]
        public async Task<ActionResult<SatelliteDataResponse>> ProcessLandsat([FromBody] string dataPath)
        {
            try
            {
                var result = await _satelliteDataService.ProcessLandsatDataAsync(dataPath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Landsat data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Sentinel-2 verilerini işler
        /// </summary>
        [HttpPost("sentinel2")]
        public async Task<ActionResult<SatelliteDataResponse>> ProcessSentinel2([FromBody] string dataPath)
        {
            try
            {
                var result = await _satelliteDataService.ProcessSentinel2DataAsync(dataPath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Sentinel-2 data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Yangın analizi yapar
        /// </summary>
        [HttpPost("analysis")]
        public async Task<ActionResult<FireAnalysisResponse>> AnalyzeFireData([FromBody] FireAnalysisRequest request)
        {
            try
            {
                var result = await _satelliteDataService.AnalyzeFireDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing fire data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// İşlenmiş görüntüyü döner
        /// </summary>
        [HttpGet("image/{dataId}/{band}")]
        public async Task<IActionResult> GetProcessedImage(string dataId, string band)
        {
            try
            {
                var imageData = await _satelliteDataService.GetProcessedImageAsync(dataId, band);
                
                if (imageData == null || imageData.Length == 0)
                {
                    return NotFound("Image not found");
                }

                // Determine content type based on file extension
                string contentType = "image/tiff";
                if (band.EndsWith(".jp2", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = "image/jp2";
                }

                return File(imageData, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting processed image");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Veri istatistiklerini döner
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetDataStatistics()
        {
            try
            {
                var availableData = await _satelliteDataService.GetAvailableDataAsync();
                
                var stats = new
                {
                    TotalDatasets = availableData.Count,
                    Sentinel1Count = availableData.Count(d => d.SatelliteType == "Sentinel1"),
                    LandsatCount = availableData.Count(d => d.SatelliteType == "Landsat8"),
                    Sentinel2Count = availableData.Count(d => d.SatelliteType == "Sentinel2"),
                    DataTypes = availableData.GroupBy(d => d.DataType).Select(g => new { Type = g.Key, Count = g.Count() }),
                    DateRange = new
                    {
                        Earliest = availableData.Min(d => d.AcquisitionDate),
                        Latest = availableData.Max(d => d.AcquisitionDate)
                    }
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data statistics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Belirli bir veri setinin detaylarını döner
        /// </summary>
        [HttpGet("dataset/{dataId}")]
        public async Task<ActionResult<SatelliteDataInfo>> GetDatasetDetails(string dataId)
        {
            try
            {
                var availableData = await _satelliteDataService.GetAvailableDataAsync();
                var dataset = availableData.FirstOrDefault(d => d.Id == dataId);
                
                if (dataset == null)
                {
                    return NotFound($"Dataset with ID {dataId} not found");
                }

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dataset details");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}

