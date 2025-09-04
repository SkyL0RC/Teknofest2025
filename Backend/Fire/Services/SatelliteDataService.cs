using Yandes.DTOs;
using System.IO;
using System.Text.Json;

namespace Yandes.Services
{
    public class SatelliteDataService : ISatelliteDataService
    {
        private readonly string _dataRootPath;
        private readonly ILogger<SatelliteDataService> _logger;
        private readonly Dictionary<string, SatelliteDataInfo> _cachedData;
        private readonly IPythonIntegrationService _pythonService;

        public SatelliteDataService(ILogger<SatelliteDataService> logger, IPythonIntegrationService pythonService)
        {
            _logger = logger;
            _pythonService = pythonService;
            _dataRootPath = ResolveDataRoot();
            _cachedData = new Dictionary<string, SatelliteDataInfo>();
            InitializeDataCache();
        }

        private string ResolveDataRoot()
        {
            // 1) Environment override
            var env = Environment.GetEnvironmentVariable("DATA_ROOT");
            if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
            {
                _logger.LogInformation("Using DATA_ROOT env: {Path}", env);
                return env;
            }

            // 2) Try known relative paths from ContentRoot
            var candidates = new List<string>
            {
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "datas"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "datas"),
            };

            foreach (var c in candidates)
            {
                var full = Path.GetFullPath(c);
                if (Directory.Exists(full))
                {
                    _logger.LogInformation("Using data root: {Path}", full);
                    return full;
                }
                else
                {
                    _logger.LogWarning("Data root candidate not found: {Path}", full);
                }
            }

            // 3) Walk up to locate 'datas' folder
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 8 && dir != null; i++)
            {
                var probe = Path.Combine(dir.FullName, "datas");
                if (Directory.Exists(probe))
                {
                    _logger.LogInformation("Using discovered data root: {Path}", probe);
                    return probe;
                }
                dir = dir.Parent;
            }

            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "datas");
            _logger.LogWarning("Data root not found, using fallback: {Path}", fallback);
            return fallback;
        }

        private void InitializeDataCache()
        {
            try
            {
                if (!Directory.Exists(_dataRootPath))
                {
                    _logger.LogWarning("Data root path not found: {Path}", _dataRootPath);
                    return;
                }

                // Sentinel-1 SAR verileri
                var sentinel1Path = Path.Combine(_dataRootPath, "1a_c_İzmir_Yangın_SENTINEL1_SAR");
                if (Directory.Exists(sentinel1Path))
                {
                    var sentinel1Dirs = Directory.GetDirectories(sentinel1Path, "*.SAFE");
                    foreach (var dir in sentinel1Dirs)
                    {
                        var dirName = Path.GetFileName(dir);
                        var dateStr = ExtractDateFromSentinel1(dirName);
                        if (DateTime.TryParse(dateStr, out var date))
                        {
                            var dataInfo = new SatelliteDataInfo
                            {
                                Id = $"S1_{date:yyyyMMdd}",
                                SatelliteType = "Sentinel1",
                                DataType = "SAR",
                                AcquisitionDate = date,
                                DataPath = dir,
                                AvailableBands = new List<string> { "VV", "VH" },
                                BandInfo = new Dictionary<string, string>
                                {
                                    { "VV", "Vertical-Vertical polarization" },
                                    { "VH", "Vertical-Horizontal polarization" }
                                },
                                Projection = "WGS84",
                                Resolution = 10.0,
                                Coverage = "İzmir Yangın Bölgesi"
                            };
                            _cachedData[dataInfo.Id] = dataInfo;
                        }
                    }
                }

                // Landsat 8&9 verileri
                var landsatPath = Path.Combine(_dataRootPath, "1ab_İzmir_Yangın_LANDSAT8_9_OPTIK");
                if (Directory.Exists(landsatPath))
                {
                    var beforePath = Path.Combine(landsatPath, "Yangın Öncesi (12_13 Haziran) Landsat8&9 Mozaiklenmiş Veriler(PathRow180_33__181_33)");
                    var afterPath = Path.Combine(landsatPath, "Yangın Sonrası (6_7 Temmuz) Landsat8&9 Mozaiklenmiş Veriler(PathRow180_33__181_33)");

                    if (Directory.Exists(beforePath))
                    {
                        var beforeData = new SatelliteDataInfo
                        {
                            Id = "L8_BEFORE_20250612",
                            SatelliteType = "Landsat8",
                            DataType = "Optical",
                            AcquisitionDate = new DateTime(2025, 6, 12),
                            DataPath = beforePath,
                            AvailableBands = new List<string> { "Band1", "Band2", "Band3", "Band4", "Band5", "Band6", "Band7" },
                            BandInfo = new Dictionary<string, string>
                            {
                                { "Band1", "Coastal Aerosol (0.43-0.45 μm)" },
                                { "Band2", "Blue (0.45-0.51 μm)" },
                                { "Band3", "Green (0.53-0.59 μm)" },
                                { "Band4", "Red (0.64-0.67 μm)" },
                                { "Band5", "Near Infrared (0.85-0.88 μm)" },
                                { "Band6", "Shortwave Infrared 1 (1.57-1.65 μm)" },
                                { "Band7", "Shortwave Infrared 2 (2.11-2.29 μm)" }
                            },
                            Projection = "UTM Zone 35N",
                            Resolution = 30.0,
                            Coverage = "İzmir Yangın Bölgesi - Yangın Öncesi"
                        };
                        _cachedData[beforeData.Id] = beforeData;
                    }

                    if (Directory.Exists(afterPath))
                    {
                        var afterData = new SatelliteDataInfo
                        {
                            Id = "L8_AFTER_20250706",
                            SatelliteType = "Landsat8",
                            DataType = "Optical",
                            AcquisitionDate = new DateTime(2025, 7, 6),
                            DataPath = afterPath,
                            AvailableBands = new List<string> { "Band1", "Band2", "Band3", "Band4", "Band5", "Band6", "Band7" },
                            BandInfo = new Dictionary<string, string>
                            {
                                { "Band1", "Coastal Aerosol (0.43-0.45 μm)" },
                                { "Band2", "Blue (0.45-0.51 μm)" },
                                { "Band3", "Green (0.53-0.59 μm)" },
                                { "Band4", "Red (0.64-0.67 μm)" },
                                { "Band5", "Near Infrared (0.85-0.88 μm)" },
                                { "Band6", "Shortwave Infrared 1 (1.57-1.65 μm)" },
                                { "Band7", "Shortwave Infrared 2 (2.11-2.29 μm)" }
                            },
                            Projection = "UTM Zone 35N",
                            Resolution = 30.0,
                            Coverage = "İzmir Yangın Bölgesi - Yangın Sonrası"
                        };
                        _cachedData[afterData.Id] = afterData;
                    }
                }

                // Sentinel-2 verileri
                var sentinel2Path = Path.Combine(_dataRootPath, "1aa_İzmir_Yangın_SENTINEL 2 OPTIK");
                if (Directory.Exists(sentinel2Path))
                {
                    var beforePath = Path.Combine(sentinel2Path, "12 Haziran_Yangın Öncesi_Mozaiklenmiş Veriler(TileNumber_T35SMC&T35SNC)");
                    var afterPath = Path.Combine(sentinel2Path, "5 Temmuz_YangınSonrası_Mozaiklenmiş Veriler(TileNumber_T35SMC&T35SNC)");

                    if (Directory.Exists(beforePath))
                    {
                        var beforeData = new SatelliteDataInfo
                        {
                            Id = "S2_BEFORE_20250612",
                            SatelliteType = "Sentinel2",
                            DataType = "Optical",
                            AcquisitionDate = new DateTime(2025, 6, 12),
                            DataPath = beforePath,
                            AvailableBands = new List<string> { "B02", "B03", "B04", "B05", "B06", "B07", "B08", "B08A", "B11", "B12" },
                            BandInfo = new Dictionary<string, string>
                            {
                                { "B02", "Blue (10m)" },
                                { "B03", "Green (10m)" },
                                { "B04", "Red (10m)" },
                                { "B05", "Vegetation Red Edge (20m)" },
                                { "B06", "Vegetation Red Edge (20m)" },
                                { "B07", "Vegetation Red Edge (20m)" },
                                { "B08", "NIR (10m)" },
                                { "B08A", "NIR (20m)" },
                                { "B11", "SWIR (20m)" },
                                { "B12", "SWIR (20m)" }
                            },
                            Projection = "UTM Zone 35N",
                            Resolution = 10.0,
                            Coverage = "İzmir Yangın Bölgesi - Yangın Öncesi"
                        };
                        _cachedData[beforeData.Id] = beforeData;
                    }

                    if (Directory.Exists(afterPath))
                    {
                        var afterData = new SatelliteDataInfo
                        {
                            Id = "S2_AFTER_20250705",
                            SatelliteType = "Sentinel2",
                            DataType = "Optical",
                            AcquisitionDate = new DateTime(2025, 7, 5),
                            DataPath = afterPath,
                            AvailableBands = new List<string> { "B02", "B03", "B04", "B05", "B06", "B07", "B08", "B08A", "B11", "B12" },
                            BandInfo = new Dictionary<string, string>
                            {
                                { "B02", "Blue (10m)" },
                                { "B03", "Green (10m)" },
                                { "B04", "Red (10m)" },
                                { "B05", "Vegetation Red Edge (20m)" },
                                { "B06", "Vegetation Red Edge (20m)" },
                                { "B07", "Vegetation Red Edge (20m)" },
                                { "B08", "NIR (10m)" },
                                { "B08A", "NIR (20m)" },
                                { "B11", "SWIR (20m)" },
                                { "B12", "SWIR (20m)" }
                            },
                            Projection = "UTM Zone 35N",
                            Resolution = 10.0,
                            Coverage = "İzmir Yangın Bölgesi - Yangın Sonrası"
                        };
                        _cachedData[afterData.Id] = afterData;
                    }
                }

                _logger.LogInformation("Data cache initialized with {Count} datasets", _cachedData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing data cache");
            }
        }

        private string ExtractDateFromSentinel1(string dirName)
        {
            // S1C_IW_OCN__2SDV_20250706T160618_20250706T160643_003102_0064B7_810C.SAFE
            var parts = dirName.Split('_');
            foreach (var part in parts)
            {
                if (part.Length == 8 && part.All(char.IsDigit))
                {
                    return $"{part.Substring(0, 4)}-{part.Substring(4, 2)}-{part.Substring(6, 2)}";
                }
            }
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        public async Task<SatelliteDataResponse> ProcessSentinel1DataAsync(string dataPath)
        {
            try
            {
                // Python servisini kullan
                return await _pythonService.ProcessSentinel1DataAsync(dataPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Sentinel-1 data");
                return new SatelliteDataResponse
                {
                    Success = false,
                    Message = $"Error processing Sentinel-1 data: {ex.Message}"
                };
            }
        }

        public async Task<SatelliteDataResponse> ProcessLandsatDataAsync(string dataPath)
        {
            try
            {
                // Python servisini kullan
                return await _pythonService.ProcessLandsatDataAsync(dataPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Landsat data");
                return new SatelliteDataResponse
                {
                    Success = false,
                    Message = $"Error processing Landsat data: {ex.Message}"
                };
            }
        }

        public async Task<SatelliteDataResponse> ProcessSentinel2DataAsync(string dataPath)
        {
            try
            {
                // Python servisini kullan
                return await _pythonService.ProcessSentinel2DataAsync(dataPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Sentinel-2 data");
                return new SatelliteDataResponse
                {
                    Success = false,
                    Message = $"Error processing Sentinel-2 data: {ex.Message}"
                };
            }
        }

        public async Task<FireAnalysisResponse> AnalyzeFireDataAsync(FireAnalysisRequest request)
        {
            try
            {
                // Python servisini kullan
                return await _pythonService.AnalyzeFireDataAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing fire data");
                return new FireAnalysisResponse
                {
                    Success = false,
                    Message = $"Error analyzing fire data: {ex.Message}"
                };
            }
        }

        public async Task<List<SatelliteDataInfo>> GetAvailableDataAsync()
        {
            return _cachedData.Values.ToList();
        }

        public async Task<byte[]> GetProcessedImageAsync(string dataId, string band)
        {
            try
            {
                if (!_cachedData.ContainsKey(dataId))
                {
                    throw new ArgumentException($"Data ID {dataId} not found");
                }

                var dataInfo = _cachedData[dataId];
                // Try multiple strategies to locate file
                // 1) If band already looks like a filename (has extension), combine directly
                var candidatePaths = new List<string>();
                if (band.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) ||
                    band.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase) ||
                    band.EndsWith(".jp2", StringComparison.OrdinalIgnoreCase))
                {
                    candidatePaths.Add(Path.Combine(dataInfo.DataPath, band));
                }

                // 2) Common extensions
                candidatePaths.Add(Path.Combine(dataInfo.DataPath, $"{band}.TIF"));
                candidatePaths.Add(Path.Combine(dataInfo.DataPath, $"{band}.tif"));
                candidatePaths.Add(Path.Combine(dataInfo.DataPath, $"{band}.TIFF"));
                candidatePaths.Add(Path.Combine(dataInfo.DataPath, $"{band}.tiff"));
                candidatePaths.Add(Path.Combine(dataInfo.DataPath, $"{band}.jp2"));
                candidatePaths.Add(Path.Combine(dataInfo.DataPath, $"{band}.JP2"));

                foreach (var p in candidatePaths)
                {
                    if (File.Exists(p))
                    {
                        return await File.ReadAllBytesAsync(p);
                    }
                }

                // 3) Search by contains when exact name unknown (Sentinel-1 SAFE or generic band name)
                //    - Sentinel-1 SAFE measurement dir may hold *.tiff with VV/VH
                var searchDirs = new List<string> { dataInfo.DataPath };
                if (dataInfo.SatelliteType == "Sentinel1" && Directory.Exists(Path.Combine(dataInfo.DataPath, "measurement")))
                {
                    searchDirs.Add(Path.Combine(dataInfo.DataPath, "measurement"));
                }

                foreach (var dir in searchDirs)
                {
                    if (!Directory.Exists(dir)) continue;
                    var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".tif", StringComparison.OrdinalIgnoreCase)
                                 || f.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase)
                                 || f.EndsWith(".jp2", StringComparison.OrdinalIgnoreCase));

                    // Match if file name contains the band token (e.g., "VV", "VH", "B04_10m", "Band4_12_13_Haziran")
                    var match = files.FirstOrDefault(f => Path.GetFileName(f).IndexOf(band, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (match != null)
                    {
                        return await File.ReadAllBytesAsync(match);
                    }
                }

                // Fallback: Return a placeholder error
                return System.Text.Encoding.UTF8.GetBytes("Image not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting processed image");
                return System.Text.Encoding.UTF8.GetBytes($"Error: {ex.Message}");
            }
        }
    }
}

