using medipanda_windows_admin.Models.Request;
using medipanda_windows_admin.Models.Rate;
using medipanda_windows_admin.Services.Base;

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
        public async Task UploadRateTableAsync(RateData rateData, int year, int month)
        {
            try
            {
                var requests = rateData.Rows.Select(row => new ProductExtraInfoUploadRequest
                {
                    ManufacturerName = string.IsNullOrEmpty(row.DrugCompanyName) ? null : row.DrugCompanyName,
                    ProductCode = row.ProductCode,
                    Price = row.DrugPrice > 0 ? (int)row.DrugPrice : null,
                    FeeRate = row.BaseCommissionRate > 0 ? row.BaseCommissionRate.ToString() : null,
                    Note = string.IsNullOrEmpty(row.Note) ? null : row.Note
                }).ToList();

                var yearMonth = $"{year}-{month:D2}";

                await PostAsync($"/v1/products/product-extra-info/upload-json?month={yearMonth}", requests);
            }
            catch (Exception ex)
            {
                throw new Exception($"요율표 업로드 실패: {ex.Message}", ex);
            }
        }
    }
}