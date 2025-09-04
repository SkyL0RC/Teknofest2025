using System.Security.Cryptography;
using System.Text;
using Yandes.DTOs;
using System.Data.Odbc;

namespace Yandes.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest req);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest req);
        Task<ApiResponse<object>> UpdateProfileAsync(string currentEmail, UpdateProfileRequest req);
        Task<ApiResponse<object>> ChangePasswordAsync(string email, ChangePasswordRequest req);
        (string? email, string? firstName, string? lastName) GetUserInfo(string email);
        bool ValidateToken(string token, out string email);
    }

    public class AuthService : IAuthService
    {
        private readonly string _dataDir;
        private readonly string _usersFile;
        private readonly Dictionary<string, string> _users = new();
        private readonly Dictionary<string, string> _tokens = new();
        private readonly string? _sqlConn;
        public bool UsingDatabase => !string.IsNullOrWhiteSpace(_sqlConn);

        public AuthService(IConfiguration config)
        {
            _dataDir = Path.Combine(AppContext.BaseDirectory, "data");
            Directory.CreateDirectory(_dataDir);
            _usersFile = Path.Combine(_dataDir, "users.txt");
            _sqlConn = Environment.GetEnvironmentVariable("SQL_CONN");

            if (!string.IsNullOrWhiteSpace(_sqlConn))
            {
                EnsureUsersTable();
                return; // DB modunda dosya okumaya gerek yok
            }
            if (File.Exists(_usersFile))
            {
                foreach (var line in File.ReadAllLines(_usersFile, Encoding.UTF8))
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        _users[parts[0]] = parts[1];
                    }
                }
            }
        }

        public Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest req)
        {
            // Tüm alanların zorunlu olduğunu kontrol et
            if (string.IsNullOrWhiteSpace(req.Password?.Trim()))
            {
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = "Şifre zorunludur" });
            }
            if (string.IsNullOrWhiteSpace(req.Email?.Trim()))
            {
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = "Email adresi zorunludur" });
            }
            if (string.IsNullOrWhiteSpace(req.FirstName?.Trim()))
            {
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = "Ad zorunludur" });
            }
            if (string.IsNullOrWhiteSpace(req.LastName?.Trim()))
            {
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = "Soyad zorunludur" });
            }

            // Email format kontrolü
            if (!IsValidEmail(req.Email))
            {
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = "Geçerli bir email adresi giriniz" });
            }

            if (EmailExists(req.Email))
            {
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = "Bu email adresi zaten kullanımda, lütfen farklı bir email deneyin" });
            }

            var hash = Hash(req.Password);
            try
            {
                SaveUser(req.Email, hash, req.FirstName, req.LastName);
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = $"Kayıt sırasında hata oluştu: {ex.Message}" });
            }
            var token = CreateToken(req.Email);
            return Task.FromResult(new ApiResponse<AuthResponse> { Success = true, Data = new AuthResponse { Token = token, Email = req.Email, FirstName = req.FirstName, LastName = req.LastName }, Message = "Kayıt başarılı" });
        }

        public Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest req)
        {
            // Login validasyonu
            if (string.IsNullOrWhiteSpace(req.Email?.Trim()))
            {
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = "Email zorunludur" });
            }
            if (string.IsNullOrWhiteSpace(req.Password?.Trim()))
            {
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = "Şifre zorunludur" });
            }

            if (!string.IsNullOrWhiteSpace(_sqlConn))
            {
                using var conn = new OdbcConnection(_sqlConn);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT PasswordHash, FirstName, LastName FROM dbo.Users WHERE Email = ?";
                cmd.Parameters.Add("@p1", OdbcType.NVarChar, 256).Value = req.Email;
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    var hashFromDb = r.GetString(0);
                    var firstName = r.IsDBNull(1) ? null : r.GetString(1);
                    var lastName = r.IsDBNull(2) ? null : r.GetString(2);
                    if (hashFromDb == Hash(req.Password))
                    {
                        var token = CreateToken(req.Email);
                        return Task.FromResult(new ApiResponse<AuthResponse> { Success = true, Data = new AuthResponse { Token = token, Email = req.Email, FirstName = firstName, LastName = lastName }, Message = "Giriş başarılı" });
                    }
                }
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = "Email veya şifre hatalı" });
            }
            // File-based fallback (no DB): check local users store
            var hash = GetPasswordHash(req.Email);
            if (hash != null && hash == Hash(req.Password))
            {
                var token = CreateToken(req.Email);
                var (email, first, last) = GetUserInfo(req.Email);
                return Task.FromResult(new ApiResponse<AuthResponse> { Success = true, Data = new AuthResponse { Token = token, Email = email!, FirstName = first, LastName = last }, Message = "Giriş başarılı" });
            }
            return Task.FromResult(new ApiResponse<AuthResponse> { Success = false, Message = "Email veya şifre hatalı" });
        }

        public Task<ApiResponse<object>> UpdateProfileAsync(string currentEmail, UpdateProfileRequest req)
        {
            // Validasyon
            if (string.IsNullOrWhiteSpace(req.FirstName?.Trim()))
            {
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Ad zorunludur" });
            }
            if (string.IsNullOrWhiteSpace(req.LastName?.Trim()))
            {
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Soyad zorunludur" });
            }
            if (string.IsNullOrWhiteSpace(req.Email?.Trim()))
            {
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Email zorunludur" });
            }

            // Email format kontrolü
            if (!IsValidEmail(req.Email))
            {
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Geçerli bir email adresi giriniz" });
            }

            // Email değişiyorsa, yeni email'in benzersiz olduğunu kontrol et
            if (req.Email != currentEmail && EmailExists(req.Email))
            {
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Bu email adresi zaten kullanımda" });
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(_sqlConn))
                {
                    using var conn = new OdbcConnection(_sqlConn);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE dbo.Users SET FirstName = ?, LastName = ?, Email = ? WHERE Email = ?";
                    cmd.Parameters.Add("@p1", OdbcType.NVarChar, 100).Value = req.FirstName;
                    cmd.Parameters.Add("@p2", OdbcType.NVarChar, 100).Value = req.LastName;
                    cmd.Parameters.Add("@p3", OdbcType.NVarChar, 256).Value = req.Email;
                    cmd.Parameters.Add("@p4", OdbcType.NVarChar, 256).Value = currentEmail;
                    cmd.ExecuteNonQuery();

                    // Email değiştiyse token'ı güncelle
                    if (req.Email != currentEmail)
                    {
                        var token = CreateToken(req.Email);
                        return Task.FromResult(new ApiResponse<object> { Success = true, Data = new { token, email = req.Email, firstName = req.FirstName, lastName = req.LastName }, Message = "Profil başarıyla güncellendi" });
                    }

                    return Task.FromResult(new ApiResponse<object> { Success = true, Data = new { email = req.Email, firstName = req.FirstName, lastName = req.LastName }, Message = "Profil başarıyla güncellendi" });
                }
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Veritabanı bağlantısı yok" });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = $"Profil güncellenirken hata oluştu: {ex.Message}" });
            }
        }

        public Task<ApiResponse<object>> ChangePasswordAsync(string email, ChangePasswordRequest req)
        {
            // Validasyon
            if (string.IsNullOrWhiteSpace(req.CurrentPassword?.Trim()))
            {
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Mevcut şifre zorunludur" });
            }
            if (string.IsNullOrWhiteSpace(req.NewPassword?.Trim()))
            {
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Yeni şifre zorunludur" });
            }
            if (req.NewPassword.Length < 6)
            {
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Yeni şifre en az 6 karakter olmalıdır" });
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(_sqlConn))
                {
                    using var conn = new OdbcConnection(_sqlConn);
                    conn.Open();
                    
                    // Mevcut şifreyi kontrol et
                    using var checkCmd = conn.CreateCommand();
                    checkCmd.CommandText = "SELECT PasswordHash FROM dbo.Users WHERE Email = ?";
                    checkCmd.Parameters.Add("@p1", OdbcType.NVarChar, 256).Value = email;
                    var currentHash = checkCmd.ExecuteScalar();
                    
                    if (currentHash == null || Convert.ToString(currentHash) != Hash(req.CurrentPassword))
                    {
                        return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Mevcut şifre hatalı" });
                    }

                    // Şifreyi güncelle
                    using var updateCmd = conn.CreateCommand();
                    updateCmd.CommandText = "UPDATE dbo.Users SET PasswordHash = ? WHERE Email = ?";
                    updateCmd.Parameters.Add("@p1", OdbcType.NVarChar, 256).Value = Hash(req.NewPassword);
                    updateCmd.Parameters.Add("@p2", OdbcType.NVarChar, 256).Value = email;
                    updateCmd.ExecuteNonQuery();

                    return Task.FromResult(new ApiResponse<object> { Success = true, Message = "Şifre başarıyla değiştirildi" });
                }
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = "Veritabanı bağlantısı yok" });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<object> { Success = false, Message = $"Şifre değiştirilirken hata oluştu: {ex.Message}" });
            }
        }

        public (string? email, string? firstName, string? lastName) GetUserInfo(string email)
        {
            if (!string.IsNullOrWhiteSpace(_sqlConn))
            {
                try
                {
                    using var conn = new OdbcConnection(_sqlConn);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Email, FirstName, LastName FROM dbo.Users WHERE Email = ?";
                    cmd.Parameters.Add("@p1", OdbcType.NVarChar, 256).Value = email;
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        return (r.IsDBNull(0) ? null : r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2));
                    }
                }
                catch
                {
                    // Hata durumunda boş değerler döndür
                }
            }
            return (email, null, null);
        }

        public bool ValidateToken(string token, out string email)
        {
            email = string.Empty;
            Console.WriteLine($"ValidateToken called with token: {token}");
            Console.WriteLine($"Token is null or whitespace: {string.IsNullOrWhiteSpace(token)}");
            
            if (string.IsNullOrWhiteSpace(token)) 
            {
                Console.WriteLine("Token is null or whitespace, returning false");
                return false;
            }
            
            // Önce memory'de kontrol et
            if (_tokens.TryGetValue(token, out var userEmail))
            {
                email = userEmail;
                Console.WriteLine($"Token found in memory for user: {email}");
                return true;
            }
            
            // Memory'de yoksa veritabanında kontrol et
            if (!string.IsNullOrWhiteSpace(_sqlConn))
            {
                try
                {
                    using var conn = new OdbcConnection(_sqlConn);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Email FROM dbo.Tokens WHERE Token = ?";
                    cmd.Parameters.Add("@p1", OdbcType.NVarChar, 256).Value = token;
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        email = result.ToString()!;
                        // Memory'e de ekle
                        _tokens[token] = email;
                        Console.WriteLine($"Token found in database for user: {email}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database token validation error: {ex.Message}");
                }
            }
            
            Console.WriteLine("Token not found in memory or database");
            return false;
        }

        private string CreateToken(string email)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _tokens[token] = email;
            
            // Token'ı veritabanına da kaydet
            if (!string.IsNullOrWhiteSpace(_sqlConn))
            {
                try
                {
                    using var conn = new OdbcConnection(_sqlConn);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO dbo.Tokens (Token, Email) VALUES (?, ?)";
                    cmd.Parameters.Add("@p1", OdbcType.NVarChar, 256).Value = token;
                    cmd.Parameters.Add("@p2", OdbcType.NVarChar, 256).Value = email;
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"Token saved to database for user: {email}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving token to database: {ex.Message}");
                }
            }
            
            return token;
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void Persist()
        {
            File.WriteAllLines(_usersFile, _users.Select(kv => kv.Key + ":" + kv.Value), Encoding.UTF8);
        }

        private void EnsureUsersTable()
        {
            if (string.IsNullOrWhiteSpace(_sqlConn)) return;
            using var conn = new OdbcConnection(_sqlConn);
            conn.Open();
            
            // Create Users table if not exists
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Users')
BEGIN
    CREATE TABLE dbo.Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(256) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(256) NOT NULL,
        FirstName NVARCHAR(100) NULL,
        LastName NVARCHAR(100) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END";
            cmd.ExecuteNonQuery();

            // Ensure unique index on Email (in case legacy table exists without UNIQUE)
            using (var ensureIdx = conn.CreateCommand())
            {
                ensureIdx.CommandText = @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);";
                ensureIdx.ExecuteNonQuery();
            }

            // Drop legacy FK if exists (to avoid creation errors on mismatched key)
            using (var dropFk = conn.CreateCommand())
            {
                dropFk.CommandText = @"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tokens_Users_Email')
ALTER TABLE dbo.Tokens DROP CONSTRAINT FK_Tokens_Users_Email;";
                try { dropFk.ExecuteNonQuery(); } catch { }
            }

            // Create Tokens table if not exists (without FK to avoid environment mismatches)
            using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Tokens')
BEGIN
    CREATE TABLE dbo.Tokens (
        Token NVARCHAR(256) PRIMARY KEY,
        Email NVARCHAR(256) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    );
END";
            cmd2.ExecuteNonQuery();

            // Note: Removed unique index creation to avoid duplicate key errors
            // Email uniqueness will be checked in application logic instead
        }

        private bool EmailExists(string email)
        {
            if (!string.IsNullOrWhiteSpace(_sqlConn))
            {
                using var conn = new OdbcConnection(_sqlConn);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM dbo.Users WHERE Email = ?";
                cmd.Parameters.Add("@p1", OdbcType.NVarChar, 256).Value = email;
                using var r = cmd.ExecuteReader();
                return r.Read();
            }
            return false;
        }

        private void SaveUser(string email, string passwordHash, string firstName, string lastName)
        {
            if (!string.IsNullOrWhiteSpace(_sqlConn))
            {
                try
            {
                using var conn = new OdbcConnection(_sqlConn);
                conn.Open();
                using var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO dbo.Users (Email, PasswordHash, FirstName, LastName) VALUES (?, ?, ?, ?)";
                    cmd.Parameters.Add("@p1", OdbcType.NVarChar, 256).Value = email;
                cmd.Parameters.Add("@p2", OdbcType.NVarChar, 256).Value = passwordHash;
                    cmd.Parameters.Add("@p3", OdbcType.NVarChar, 100).Value = firstName ?? string.Empty;
                    cmd.Parameters.Add("@p4", OdbcType.NVarChar, 100).Value = lastName ?? string.Empty;
                cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Veritabanına kayıt eklenemedi: {ex.Message}");
                }
                return;
            }

            // File-based fallback (no DB): persist to local users file
            _users[email] = passwordHash;
            Persist();
        }

        private string? GetPasswordHash(string email)
        {
            if (!string.IsNullOrWhiteSpace(_sqlConn))
            {
                using var conn = new OdbcConnection(_sqlConn);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT PasswordHash FROM dbo.Users WHERE Email = ?";
                cmd.Parameters.Add("@p1", OdbcType.NVarChar, 256).Value = email;
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : Convert.ToString(result);
            }
            return _users.TryGetValue(email, out var hash) ? hash : null;
        }

        private (string? email, string? firstName, string? lastName) GetUserProfile(string email)
        {
            if (!string.IsNullOrWhiteSpace(_sqlConn))
            {
                using var conn = new OdbcConnection(_sqlConn);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Email, FirstName, LastName FROM dbo.Users WHERE Email = ?";
                cmd.Parameters.Add("@p1", OdbcType.NVarChar, 256).Value = email;
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    return (r.IsDBNull(0) ? null : r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2));
                }
                return (null, null, null);
            }
            return (null, null, null);
        }
    }
}

