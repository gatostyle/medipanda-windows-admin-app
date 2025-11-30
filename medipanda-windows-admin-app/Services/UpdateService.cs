using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using medipanda_windows_admin.Models;

namespace medipanda_windows_admin.Services
{
    public class UpdateService
    {
        private static UpdateService _instance;
        private static readonly object _lock = new object();
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _downloadCancellationTokenSource;

        // CloudFront URL 사용
        private const string VersionCheckUrl = "https://cdn.medipanda.co.kr/app/windows/admin-app/version.json";

        public static UpdateService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new UpdateService();
                    }
                    return _instance;
                }
            }
        }

        private UpdateService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // User-Agent 헤더 추가
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
        }

        public string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        public async Task<VersionInfo> CheckForUpdateAsync()
        {
            // 기존 코드와 동일
            try
            {
                var url = $"{VersionCheckUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                using (var response = await _httpClient.SendAsync(request))
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"HTTP {response.StatusCode}");
                    }

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    return JsonSerializer.Deserialize<VersionInfo>(content, options);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"버전 확인 실패: {ex.Message}", ex);
            }
        }

        public bool IsNewVersionAvailable(string currentVersion, string latestVersion)
        {
            try
            {
                var current = ParseVersion(currentVersion);
                var latest = ParseVersion(latestVersion);
                return latest > current;
            }
            catch
            {
                return false;
            }
        }

        private Version ParseVersion(string versionString)
        {
            versionString = versionString.TrimStart('v', 'V').Trim();
            var parts = versionString.Split('.');

            if (parts.Length == 2)
                versionString = $"{versionString}.0";
            else if (parts.Length == 1)
                versionString = $"{versionString}.0.0";

            return new Version(versionString);
        }

        /// <summary>
        /// 취소 가능한 다운로드 및 설치
        /// </summary>
        public async Task<bool> DownloadAndInstallUpdateAsync(
            string downloadUrl,
            IProgress<(int percent, long bytesReceived, long totalBytes)> progress = null,
            CancellationToken cancellationToken = default)
        {
            // 새로운 CancellationTokenSource 생성 (내부 취소용)
            _downloadCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _downloadCancellationTokenSource.Token;

            string tempPath = null;

            try
            {
                var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
                tempPath = Path.Combine(Path.GetTempPath(), fileName);

                // 기존 파일이 있으면 삭제
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                // HttpClient의 요청에 CancellationToken 전달
                using (var response = await _httpClient.GetAsync(downloadUrl,
                    HttpCompletionOption.ResponseHeadersRead, token))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1 && progress != null;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempPath, FileMode.Create,
                        FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var totalRead = 0L;
                        var buffer = new byte[8192];
                        var isMoreToRead = true;

                        while (isMoreToRead)
                        {
                            // 취소 요청 확인
                            token.ThrowIfCancellationRequested();

                            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length, token);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read, token);

                                totalRead += read;

                                if (canReportProgress)
                                {
                                    var progressPercentage = (int)((totalRead * 100) / totalBytes);
                                    progress.Report((progressPercentage, totalRead, totalBytes));
                                }
                            }
                        }
                    }
                }

                // 파일이 제대로 다운로드되었는지 확인
                if (!File.Exists(tempPath))
                {
                    throw new Exception("설치 파일 다운로드에 실패했습니다.");
                }

                // 설치 프로그램 실행
                var startInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true,
                    Arguments = "/SILENT"
                };

                Process.Start(startInfo);

                // 현재 애플리케이션 종료
                Application.Current.Shutdown();

                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Update download was cancelled by user");

                // 임시 파일 정리
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                        Debug.WriteLine($"Deleted incomplete download: {tempPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to delete temp file: {ex.Message}");
                    }
                }

                return false; // 취소됨
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Download/Install error: {ex}");

                // 임시 파일 정리
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }

                throw new Exception($"업데이트 설치 실패: {ex.Message}", ex);
            }
            finally
            {
                _downloadCancellationTokenSource?.Dispose();
                _downloadCancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 진행 중인 다운로드 취소
        /// </summary>
        public void CancelDownload()
        {
            _downloadCancellationTokenSource?.Cancel();
        }
    }
}