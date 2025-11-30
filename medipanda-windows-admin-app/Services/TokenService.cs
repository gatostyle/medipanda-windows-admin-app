using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace medipanda_windows_admin.Services
{
    public class TokenService
    {
        private static TokenService _instance;
        private string _accessToken;
        private string _refreshToken;
        private readonly string _tokenFilePath;

        public static TokenService Instance => _instance ??= new TokenService();

        private TokenService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "medipanda_windows_app");
            Directory.CreateDirectory(appFolder);
            _tokenFilePath = Path.Combine(appFolder, "tokens.dat");

            LoadTokens();
        }

        public string AccessToken
        {
            get => _accessToken;
            set
            {
                _accessToken = value;
                SaveTokens();
            }
        }

        public string RefreshToken
        {
            get => _refreshToken;
            set
            {
                _refreshToken = value;
                SaveTokens();
            }
        }

        public void SetTokens(string accessToken, string refreshToken)
        {
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            SaveTokens();
        }

        public void ClearTokens()
        {
            _accessToken = null;
            _refreshToken = null;
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
            }
        }

        private void SaveTokens()
        {
            try
            {
                var tokens = new { AccessToken = _accessToken, RefreshToken = _refreshToken };
                var json = JsonSerializer.Serialize(tokens);
                var encrypted = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(json),
                    null,
                    DataProtectionScope.CurrentUser
                );
                File.WriteAllBytes(_tokenFilePath, encrypted);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토큰 저장 실패: {ex.Message}");
            }
        }

        private void LoadTokens()
        {
            try
            {
                if (File.Exists(_tokenFilePath))
                {
                    var encrypted = File.ReadAllBytes(_tokenFilePath);
                    var decrypted = ProtectedData.Unprotect(
                        encrypted,
                        null,
                        DataProtectionScope.CurrentUser
                    );
                    var json = Encoding.UTF8.GetString(decrypted);
                    var tokens = JsonSerializer.Deserialize<TokenData>(json);
                    _accessToken = tokens?.AccessToken;
                    _refreshToken = tokens?.RefreshToken;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토큰 로드 실패: {ex.Message}");
            }
        }

        private class TokenData
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
        }
    }
}