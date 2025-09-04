using Microsoft.AspNetCore.Mvc;
using Yandes.Services;
using Yandes.DTOs;

namespace Yandes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataExplorerController : ControllerBase
    {
        private readonly IPythonIntegrationService _pythonService;
        private readonly ILogger<DataExplorerController> _logger;

        public DataExplorerController(IPythonIntegrationService pythonService, ILogger<DataExplorerController> logger)
        {
            _pythonService = pythonService;
            _logger = logger;
        }

        /// <summary>
        /// Mevcut veri setlerini listeler
        /// </summary>
        [HttpGet("datasets")]
        public async Task<ActionResult<object>> GetAvailableDatasets()
        {
            try
            {
                var dataRoot = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "datas");
                var datasets = new List<object>();

                // Sentinel-1 verileri
                var sentinel1Path = Path.Combine(dataRoot, "1a_c_İzmir_Yangın_SENTINEL1_SAR");
                if (Directory.Exists(sentinel1Path))
                {
                    var sentinel1Dirs = Directory.GetDirectories(sentinel1Path, "*.SAFE");
                    foreach (var dir in sentinel1Dirs)
                    {
                        var dirName = Path.GetFileName(dir);
                        datasets.Add(new
                        {
                            Type = "Sentinel-1 SAR",
                            Name = dirName,
                            Path = dir,
                            Date = ExtractDateFromSentinel1(dirName),
                            Size = GetDirectorySize(dir)
                        });
                    }
                }

                // Landsat verileri
                var landsatPath = Path.Combine(dataRoot, "1ab_İzmir_Yangın_LANDSAT8_9_OPTIK");
                if (Directory.Exists(landsatPath))
                {
                    var beforePath = Path.Combine(landsatPath, "Yangın Öncesi (12_13 Haziran) Landsat8&9 Mozaiklenmiş Veriler(PathRow180_33__181_33)");
                    var afterPath = Path.Combine(landsatPath, "Yangın Sonrası (6_7 Temmuz) Landsat8&9 Mozaiklenmiş Veriler(PathRow180_33__181_33)");

                    if (Directory.Exists(beforePath))
                    {
                        datasets.Add(new
                        {
                            Type = "Landsat 8&9",
                            Name = "Yangın Öncesi (12-13 Haziran)",
                            Path = beforePath,
                            Date = "2025-06-12",
                            Size = GetDirectorySize(beforePath)
                        });
                    }

                    if (Directory.Exists(afterPath))
                    {
                        datasets.Add(new
                        {
                            Type = "Landsat 8&9",
                            Name = "Yangın Sonrası (6-7 Temmuz)",
                            Path = afterPath,
                            Date = "2025-07-06",
                            Size = GetDirectorySize(afterPath)
                        });
                    }
                }

                // Sentinel-2 verileri
                var sentinel2Path = Path.Combine(dataRoot, "1aa_İzmir_Yangın_SENTINEL 2 OPTIK");
                if (Directory.Exists(sentinel2Path))
                {
                    var beforePath = Path.Combine(sentinel2Path, "12 Haziran_Yangın Öncesi_Mozaiklenmiş Veriler(TileNumber_T35SMC&T35SNC)");
                    var afterPath = Path.Combine(sentinel2Path, "5 Temmuz_YangınSonrası_Mozaiklenmiş Veriler(TileNumber_T35SMC&T35SNC)");

                    if (Directory.Exists(beforePath))
                    {
                        datasets.Add(new
                        {
                            Type = "Sentinel-2",
                            Name = "Yangın Öncesi (12 Haziran)",
                            Path = beforePath,
                            Date = "2025-06-12",
                            Size = GetDirectorySize(beforePath)
                        });
                    }

                    if (Directory.Exists(afterPath))
                    {
                        datasets.Add(new
                        {
                            Type = "Sentinel-2",
                            Name = "Yangın Sonrası (5 Temmuz)",
                            Path = afterPath,
                            Date = "2025-07-05",
                            Size = GetDirectorySize(afterPath)
                        });
                    }
                }

                return Ok(new { success = true, datasets = datasets });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available datasets");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }

        /// <summary>
        /// Belirli bir veri setinin detaylarını getirir
        /// </summary>
        [HttpGet("dataset/{dataType}/{*dataPath}")]
        public async Task<ActionResult<object>> GetDatasetInfo(string dataType, string dataPath)
        {
            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "datas", dataPath);
                
                if (!Directory.Exists(fullPath))
                {
                    return NotFound(new { success = false, error = "Dataset not found" });
                }

                var result = await _pythonService.GetDataInfoAsync(fullPath, dataType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dataset info");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }

        /// <summary>
        /// Veri işleme işlemini başlatır
        /// </summary>
        [HttpPost("process")]
        public async Task<ActionResult<object>> ProcessDataset([FromBody] ProcessDatasetRequest request)
        {
            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "datas", request.DataPath);
                
                if (!Directory.Exists(fullPath))
                {
                    return NotFound(new { success = false, error = "Dataset not found" });
                }

                object result;
                switch (request.DataType.ToLower())
                {
                    case "sentinel1":
                        var s1Result = await _pythonService.ProcessSentinel1DataAsync(fullPath);
                        result = s1Result;
                        break;
                    case "landsat":
                        var l8Result = await _pythonService.ProcessLandsatDataAsync(fullPath);
                        result = l8Result;
                        break;
                    case "sentinel2":
                        var s2Result = await _pythonService.ProcessSentinel2DataAsync(fullPath);
                        result = s2Result;
                        break;
                    default:
                        return BadRequest(new { success = false, error = "Invalid data type" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dataset");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }

        /// <summary>
        /// Yangın analizi yapar
        /// </summary>
        [HttpPost("analyze")]
        public async Task<ActionResult<object>> AnalyzeFireData([FromBody] FireAnalysisRequest request)
        {
            try
            {
                var result = await _pythonService.AnalyzeFireDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing fire data");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }

        private string ExtractDateFromSentinel1(string dirName)
        {
            var parts = dirName.Split('_');
            foreach (var part in parts)
            {
                if (part.Length == 8 && part.All(char.IsDigit))
                {
                    return $"{part.Substring(0, 4)}-{part.Substring(4, 2)}-{part.Substring(6, 2)}";
                }
            }
            return "Unknown";
        }

        private string GetDirectorySize(string path)
        {
            try
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                var totalSize = files.Sum(file => new FileInfo(file).Length);
                var sizeInMB = totalSize / (1024 * 1024);
                return $"{sizeInMB:F1} MB";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    public class ProcessDatasetRequest
    {
        public string DataType { get; set; } = string.Empty;
        public string DataPath { get; set; } = string.Empty;
    }
}
