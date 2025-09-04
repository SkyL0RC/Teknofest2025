using System.Diagnostics;
using System.Text.Json;
using Yandes.DTOs;

namespace Yandes.Services
{
    public interface IPythonIntegrationService
    {
        Task<SatelliteDataResponse> ProcessSentinel1DataAsync(string dataPath);
        Task<SatelliteDataResponse> ProcessLandsatDataAsync(string dataPath);
        Task<SatelliteDataResponse> ProcessSentinel2DataAsync(string dataPath);
        Task<FireAnalysisResponse> AnalyzeFireDataAsync(FireAnalysisRequest request);
        Task<Dictionary<string, object>> GetDataInfoAsync(string dataPath, string dataType);
    }

    public class PythonIntegrationService : IPythonIntegrationService
    {
        private readonly ILogger<PythonIntegrationService> _logger;
        private readonly string _pythonScriptPath;
        private readonly string _pythonExecutable;

        public PythonIntegrationService(ILogger<PythonIntegrationService> logger)
        {
            _logger = logger;
            _pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "PythonScripts", "satellite_processor.py");
            _pythonExecutable = "python"; // Python komut satırından çalıştırılacak
        }

        public async Task<SatelliteDataResponse> ProcessSentinel1DataAsync(string dataPath)
        {
            try
            {
                var result = await ExecutePythonScriptAsync("sentinel1", dataPath);
                var ok = TryGetBool(result, "success");
                if (ok)
                {
                    return new SatelliteDataResponse
                    {
                        Success = true,
                        Message = "Sentinel-1 SAR data processed successfully",
                        Data = new SatelliteDataInfo
                        {
                            Id = $"S1_{DateTime.Now:yyyyMMdd_HHmmss}",
                            SatelliteType = "Sentinel1",
                            DataType = "SAR",
                            AcquisitionDate = DateTime.Now,
                            DataPath = dataPath,
                            AvailableBands = result.ContainsKey("available_polarizations") 
                                ? ((JsonElement)result["available_polarizations"]).EnumerateArray().Select(x => x.GetString()).ToList()
                                : new List<string>(),
                            Projection = "WGS84",
                            Resolution = 10.0,
                            Coverage = "İzmir Yangın Bölgesi"
                        },
                        Metadata = result
                    };
                }
                else
                {
                    return new SatelliteDataResponse
                    {
                        Success = false,
                        Message = result.ContainsKey("error") ? result["error"].ToString() : "Unknown error"
                    };
                }
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
                var result = await ExecutePythonScriptAsync("landsat", dataPath);
                var ok = TryGetBool(result, "success");
                if (ok)
                {
                    return new SatelliteDataResponse
                    {
                        Success = true,
                        Message = "Landsat data processed successfully",
                        Data = new SatelliteDataInfo
                        {
                            Id = $"L8_{DateTime.Now:yyyyMMdd_HHmmss}",
                            SatelliteType = "Landsat8",
                            DataType = "Optical",
                            AcquisitionDate = DateTime.Now,
                            DataPath = dataPath,
                            AvailableBands = result.ContainsKey("available_bands") 
                                ? ((JsonElement)result["available_bands"]).EnumerateArray().Select(x => x.GetString()).ToList()
                                : new List<string>(),
                            Projection = "UTM Zone 35N",
                            Resolution = 30.0,
                            Coverage = "İzmir Yangın Bölgesi"
                        },
                        Metadata = result
                    };
                }
                else
                {
                    return new SatelliteDataResponse
                    {
                        Success = false,
                        Message = result.ContainsKey("error") ? result["error"].ToString() : "Unknown error"
                    };
                }
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
                var result = await ExecutePythonScriptAsync("sentinel2", dataPath);
                var ok = TryGetBool(result, "success");
                if (ok)
                {
                    return new SatelliteDataResponse
                    {
                        Success = true,
                        Message = "Sentinel-2 data processed successfully",
                        Data = new SatelliteDataInfo
                        {
                            Id = $"S2_{DateTime.Now:yyyyMMdd_HHmmss}",
                            SatelliteType = "Sentinel2",
                            DataType = "Optical",
                            AcquisitionDate = DateTime.Now,
                            DataPath = dataPath,
                            AvailableBands = result.ContainsKey("available_bands") 
                                ? ((JsonElement)result["available_bands"]).EnumerateArray().Select(x => x.GetString()).ToList()
                                : new List<string>(),
                            Projection = "UTM Zone 35N",
                            Resolution = 10.0,
                            Coverage = "İzmir Yangın Bölgesi"
                        },
                        Metadata = result
                    };
                }
                else
                {
                    return new SatelliteDataResponse
                    {
                        Success = false,
                        Message = result.ContainsKey("error") ? result["error"].ToString() : "Unknown error"
                    };
                }
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
                var result = await ExecutePythonScriptAsync("analyze", 
                    request.BeforeDataId, request.AfterDataId, request.AnalysisType);
                
                var ok = TryGetBool(result, "success");
                if (ok)
                {
                    var response = new FireAnalysisResponse
                    {
                        Success = true,
                        Message = "Fire analysis completed successfully",
                        AnalysisId = $"ANALYSIS_{DateTime.Now:yyyyMMdd_HHmmss}",
                        ResultImagePath = $"/api/satellite/analysis/{DateTime.Now:yyyyMMdd_HHmmss}/result",
                        AnalysisDate = DateTime.Now
                    };

                    // Sonuçları ekle
                    if (result.ContainsKey("results"))
                    {
                        var results = (JsonElement)result["results"];
                        foreach (var property in results.EnumerateObject())
                        {
                            if (property.Value.ValueKind == JsonValueKind.Number)
                            {
                                response.Statistics[property.Name] = property.Value.GetDouble();
                            }
                        }
                    }

                    return response;
                }
                else
                {
                    return new FireAnalysisResponse
                    {
                        Success = false,
                        Message = result.ContainsKey("error") ? result["error"].ToString() : "Unknown error"
                    };
                }
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

        public async Task<Dictionary<string, object>> GetDataInfoAsync(string dataPath, string dataType)
        {
            try
            {
                return await ExecutePythonScriptAsync(dataType.ToLower(), dataPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data info");
                return new Dictionary<string, object> { { "success", false }, { "error", ex.Message } };
            }
        }

        private async Task<Dictionary<string, object>> ExecutePythonScriptAsync(string command, params string[] arguments)
        {
            try
            {
                if (!File.Exists(_pythonScriptPath))
                {
                    throw new FileNotFoundException($"Python script not found: {_pythonScriptPath}");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = _pythonExecutable,
                    Arguments = $"{_pythonScriptPath} {command} {string.Join(" ", arguments.Select(arg => $"\"{arg}\""))}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Python script failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                    throw new Exception($"Python script failed: {error}");
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    return new Dictionary<string, object> { { "success", false }, { "error", "No output from Python script" } };
                }

                try
                {
                    var jsonResult = JsonSerializer.Deserialize<Dictionary<string, object>>(output);
                    return jsonResult ?? new Dictionary<string, object> { { "success", false }, { "error", "Failed to parse JSON output" } };
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse Python script output as JSON: {Output}", output);
                    return new Dictionary<string, object> { { "success", false }, { "error", "Invalid JSON output from Python script" } };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Python script");
                throw;
            }
        }

        private bool TryGetBool(Dictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key)) return false;
            var v = dict[key];
            if (v is bool b) return b;
            if (v is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                if (je.ValueKind == System.Text.Json.JsonValueKind.False) return false;
                if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var s = je.GetString();
                    if (bool.TryParse(s, out var pb)) return pb;
                }
            }
            return false;
        }
    }
}
