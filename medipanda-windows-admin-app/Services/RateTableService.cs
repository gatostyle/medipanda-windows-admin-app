using medipanda_windows_admin.Services.Base;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace medipanda_windows_admin.Services
{
    public class RateTableService : BaseApiService
    {
        private static RateTableService? _instance;
        public static RateTableService Instance => _instance ??= new RateTableService();

        private RateTableService() : base() { }

        /// <summary>
        /// 요율표 업로드
        /// </summary>
        public async Task UploadRateTableAsync(string filePath, int year, int month)
        {
            try
            {
                var yearMonth = $"{year}-{month:D2}";
                var fullUrl = $"{BaseUrl}/v1/product-extra-info/upload?month={yearMonth}";

                using var form = new MultipartFormDataContent();

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                );
                var fileName = Path.GetFileName(filePath);
                form.Add(fileContent, "file", fileName);

                // 직접 요청 생성해서 헤더 확인
                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl)
                {
                    Content = form
                };

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    TokenService.Instance.AccessToken
                );

                // 디버깅: 헤더 확인
                System.Diagnostics.Debug.WriteLine($"Auth Header: {request.Headers.Authorization}");

                var response = await _httpClient.SendAsync(request);

                System.Diagnostics.Debug.WriteLine($"Status: {response.StatusCode}");
                var body = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Response: {body}");

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new Exception($"요율표 업로드 실패: {ex.Message}", ex);
            }
        }
    }
}