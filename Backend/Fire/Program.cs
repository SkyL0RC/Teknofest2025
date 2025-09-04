using Yandes.Services;
using System.Text;
using Yandes.DTOs;
using Yandes.Controllers;
using System.Data.Odbc;

var builder = WebApplication.CreateBuilder(args);

// Ensure a default URL if none is provided by environment or launch settings
var defaultUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrWhiteSpace(defaultUrls))
{
    // Force default to 5299 to avoid collisions with 5180
    builder.WebHost.UseUrls("http://localhost:5299");
}

// Load .env if present
try
{
    var contentRoot = Directory.GetCurrentDirectory();
    var envPath = Path.Combine(contentRoot, ".env");
    if (File.Exists(envPath))
    {
        foreach (var rawLine in File.ReadAllLines(envPath, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            var idx = line.IndexOf('=');
            if (idx <= 0) continue;
            var key = line.Substring(0, idx).Trim();
            var value = line.Substring(idx + 1).Trim().Trim('"');
            if (!string.IsNullOrEmpty(key))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
catch { /* ignore .env errors */ }

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add CORS (from CORS_ORIGINS or AllowAll)
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS");
builder.Services.AddCors(options =>
{
    if (!string.IsNullOrWhiteSpace(corsOrigins))
    {
        var origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        options.AddPolicy("CustomOrigins", policy =>
        {
            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
    else
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
});

// Add Services
builder.Services.AddScoped<IFireService, FireService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddScoped<ISatelliteDataService, SatelliteDataService>();
builder.Services.AddScoped<IPythonIntegrationService, PythonIntegrationService>();

var app = builder.Build();

// Configure URLs from .env if provided
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrWhiteSpace(urls))
{
    app.Urls.Clear();
    foreach (var u in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        app.Urls.Add(u);
    }
}
else
{
    // Force default to 5299 if no environment override
    app.Urls.Clear();
    app.Urls.Add("http://localhost:5299");
}

// Minimal pipeline without Swagger/EF/NLog

// No HTTPS redirection to avoid dev-certificate requirement

// Use CORS
if (!string.IsNullOrWhiteSpace(corsOrigins))
{
    app.UseCors("CustomOrigins");
}
else
{
    app.UseCors("AllowAll");
}

app.UseRouting();

app.UseAuthorization();

// Simple token auth middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
    if (path.StartsWith("/api/game") || path.StartsWith("/api/profile"))
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        Console.WriteLine($"Auth header: {authHeader}");
        
        var token = authHeader.Replace("Bearer ", "");
        Console.WriteLine($"Extracted token: {token}");
        Console.WriteLine($"Token length: {token.Length}");
        
        var auth = context.RequestServices.GetRequiredService<IAuthService>();
        if (!auth.ValidateToken(token, out var username))
        {
            Console.WriteLine($"Token validation failed for path: {path}");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { success = false, message = "Yetkisiz" });
            return;
        }
        Console.WriteLine($"Token validation successful for user: {username}");
        context.Items["username"] = username;
        context.Items["email"] = username; // Email ve username aynı
    }
    await next();
});

app.MapControllers();

// Fallback minimal endpoints for auth (in case attribute routing not discovered)
app.MapPost("/api/auth/register", async (IAuthService auth, RegisterRequest req) =>
{
    var res = await auth.RegisterAsync(req);
    return Results.Json(res, statusCode: res.Success ? 200 : 400);
});

app.MapPost("/api/auth/login", async (IAuthService auth, LoginRequest req) =>
{
    var res = await auth.LoginAsync(req);
    return Results.Json(res, statusCode: res.Success ? 200 : 401);
});

app.MapGet("/api/auth/ping", (IAuthService auth) =>
{
    var db = (auth as AuthService)?.UsingDatabase ?? false;
    return Results.Json(new { ok = true, database = db });
});

app.MapGet("/api/auth/dbinfo", () =>
{
    try
    {
        var cs = Environment.GetEnvironmentVariable("SQL_CONN");
        if (string.IsNullOrWhiteSpace(cs)) return Results.Json(new { database = false, reason = "SQL_CONN empty" });
        using var conn = new OdbcConnection(cs);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DB_NAME(), (SELECT COUNT(1) FROM dbo.Users)";
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            var dbname = r.IsDBNull(0) ? null : r.GetString(0);
            var count = r.IsDBNull(1) ? 0 : r.GetInt32(1);
            return Results.Json(new { database = true, db = dbname, users = count });
        }
        return Results.Json(new { database = true, db = (string?)null, users = 0 });
    }
    catch (Exception ex)
    {
        return Results.Json(new { database = false, error = ex.Message });
    }
});

// Satellite data alias endpoint for compatibility
app.MapGet("/api/satellite/available", async (ISatelliteDataService svc) =>
{
    var data = await svc.GetAvailableDataAsync();
    return Results.Json(data);
});

// Aliases for dataset process/analyze to avoid routing mismatches
app.MapPost("/api/dataexplorer/process", async (IPythonIntegrationService py, ISatelliteDataService sat, Yandes.Controllers.ProcessDatasetRequest req) =>
{
    if (req == null || string.IsNullOrWhiteSpace(req.DataPath) || string.IsNullOrWhiteSpace(req.DataType))
        return Results.BadRequest(new { success = false, error = "Invalid request" });

    var dataRoot = Environment.GetEnvironmentVariable("DATA_ROOT");
    string fullPath = string.IsNullOrWhiteSpace(dataRoot)
        ? Path.GetFullPath(req.DataPath)
        : Path.Combine(dataRoot!, req.DataPath);

    if (!Directory.Exists(fullPath))
        return Results.NotFound(new { success = false, error = "Dataset not found" });

    object result;
    switch (req.DataType.ToLowerInvariant())
    {
        case "sentinel1":
            result = await py.ProcessSentinel1DataAsync(fullPath);
            break;
        case "landsat":
            result = await py.ProcessLandsatDataAsync(fullPath);
            break;
        case "sentinel2":
            result = await py.ProcessSentinel2DataAsync(fullPath);
            break;
        default:
            return Results.BadRequest(new { success = false, error = "Invalid data type" });
    }
    return Results.Json(result);
});

app.MapPost("/api/dataexplorer/analyze", async (IPythonIntegrationService py, Yandes.DTOs.FireAnalysisRequest req) =>
{
    if (req == null || string.IsNullOrWhiteSpace(req.BeforeDataId) || string.IsNullOrWhiteSpace(req.AfterDataId) || string.IsNullOrWhiteSpace(req.AnalysisType))
        return Results.BadRequest(new { success = false, error = "Invalid request" });
    var result = await py.AnalyzeFireDataAsync(req);
    return Results.Json(result);
});

// Alias for image serving to ensure consistent routing
app.MapGet("/api/satellite/image/{dataId}/{band}", async (ISatelliteDataService svc, string dataId, string band) =>
{
    var bytes = await svc.GetProcessedImageAsync(dataId, band);
    if (bytes == null || bytes.Length == 0) return Results.NotFound();
    var ct = band.EndsWith(".jp2", StringComparison.OrdinalIgnoreCase) ? "image/jp2" : "image/tiff";
    return Results.File(bytes, ct);
});

app.Run();
