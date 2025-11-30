using medipanda_windows_admin;
using medipanda_windows_admin.Models.Response;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace medipanda_windows_admin.Services.Base
{
    public abstract class BaseApiService
    {
        protected readonly HttpClient _httpClient;
        protected readonly JsonSerializerOptions _jsonOptions;
        private static readonly object _refreshLock = new object();
        private static bool _isRefreshing = false;

        // BaseUrl을 동적으로 가져오도록 변경
        protected string BaseUrl => AppConfig.GetBaseUrl();

        protected BaseApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
        }

        private async Task<HttpResponseMessage> SendWithTokenRefreshAsync(HttpRequestMessage request, bool useAuth)
        {
            if (useAuth)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    TokenService.Instance.AccessToken
                );
            }

            var response = await _httpClient.SendAsync(request);

            // 401 Unauthorized이고 인증을 사용하는 경우, 토큰 갱신 시도
            if (response.StatusCode == HttpStatusCode.Unauthorized && useAuth)
            {
                // 중복 갱신 방지
                lock (_refreshLock)
                {
                    if (!_isRefreshing)
                    {
                        _isRefreshing = true;
                    }
                    else
                    {
                        // 다른 스레드가 이미 갱신 중이면 대기
                        System.Threading.Thread.Sleep(1000);
                        _isRefreshing = false;
                    }
                }

                if (_isRefreshing)
                {
                    try
                    {
                        // 토큰 갱신
                        await RefreshTokenInternalAsync();

                        // 새 토큰으로 재시도
                        var newRequest = await CloneRequestAsync(request);
                        newRequest.Headers.Authorization = new AuthenticationHeaderValue(
                            "Bearer",
                            TokenService.Instance.AccessToken
                        );

                        response = await _httpClient.SendAsync(newRequest);
                    }
                    catch (Exception ex)
                    {
                        // 토큰 갱신 실패 시 로그아웃 처리
                        UserSessionService.Instance.ClearSession();
                        throw new Exception("토큰 갱신 실패. 다시 로그인해주세요.", ex);
                    }
                    finally
                    {
                        _isRefreshing = false;
                    }
                }
            }

            return response;
        }

        private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            // Content 복사
            if (request.Content != null)
            {
                var contentBytes = await request.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(contentBytes);

                // Content headers 복사
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Request headers 복사 (Authorization 제외)
            foreach (var header in request.Headers)
            {
                if (header.Key != "Authorization")
                {
                    clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }

        private async Task RefreshTokenInternalAsync()
        {
            try
            {
                var userId = UserSessionService.Instance.CurrentUser.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("사용자 ID를 찾을 수 없습니다.");
                }

                var request = new
                {
                    userId = userId,
                    refreshToken = TokenService.Instance.RefreshToken
                };

                var fullUrl = $"{BaseUrl}/v1/auth/token/refresh";
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, fullUrl)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RefreshTokenResponse>(responseJson, _jsonOptions);

                if (result == null)
                {
                    throw new Exception("토큰 갱신 응답이 null입니다.");
                }

                // 새 토큰 저장
                TokenService.Instance.SetTokens(
                    result.AccessToken,
                    result.RefreshToken
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"토큰 갱신 실패: {ex.Message}", ex);
            }
        }

        protected async Task<T> GetAsync<T>(string endpoint, bool useAuth = true)
        {
            try
            {
                var fullUrl = $"{BaseUrl}{endpoint}";
                var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);

                var response = await SendWithTokenRefreshAsync(request, useAuth);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);

                return result ?? throw new Exception("응답 데이터가 null입니다.");
            }
            catch (Exception ex)
            {
                throw new Exception($"GET 요청 실패: {ex.Message}", ex);
            }
        }

        protected async Task<T> PostAsync<T>(string endpoint, object data, bool useAuth = true)
        {
            try
            {
                var fullUrl = $"{BaseUrl}{endpoint}";
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl)
                {
                    Content = content
                };

                var response = await SendWithTokenRefreshAsync(request, useAuth);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);

                return result ?? throw new Exception("응답 데이터가 null입니다.");
            }
            catch (Exception ex)
            {
                throw new Exception($"POST 요청 실패: {ex.Message}", ex);
            }
        }

        protected async Task<T> PutAsync<T>(string endpoint, object data, bool useAuth = true)
        {
            try
            {
                var fullUrl = $"{BaseUrl}{endpoint}";
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, fullUrl)
                {
                    Content = content
                };

                var response = await SendWithTokenRefreshAsync(request, useAuth);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);

                return result ?? throw new Exception("응답 데이터가 null입니다.");
            }
            catch (Exception ex)
            {
                throw new Exception($"PUT 요청 실패: {ex.Message}", ex);
            }
        }

        protected async Task<T> DeleteAsync<T>(string endpoint, bool useAuth = true)
        {
            try
            {
                var fullUrl = $"{BaseUrl}{endpoint}";
                var request = new HttpRequestMessage(HttpMethod.Delete, fullUrl);

                var response = await SendWithTokenRefreshAsync(request, useAuth);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);

                return result ?? throw new Exception("응답 데이터가 null입니다.");
            }
            catch (Exception ex)
            {
                throw new Exception($"DELETE 요청 실패: {ex.Message}", ex);
            }
        }

        protected string GetDeviceId()
        {
            var deviceInfo = $"{Environment.MachineName}-{Environment.UserName}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceInfo));
            return Convert.ToBase64String(hash);
        }
    }
}