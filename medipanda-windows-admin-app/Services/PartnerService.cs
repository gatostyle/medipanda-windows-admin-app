using medipanda_windows_admin.Models.Response;
using medipanda_windows_admin.Services.Base;

namespace medipanda_windows_admin.Services
{
    public class PartnerService : BaseApiService
    {
        public async Task<List<PartnerResponse>> GetPartnersAsync(string institutionCode, string drugCompanyName)
        {
            try
            {
                var encoded = Uri.EscapeDataString(drugCompanyName);
                var response = await GetAsync<PartnerPageResponse>(
                    $"/v1/partners?institutionCode={institutionCode}&drugCompanyName={drugCompanyName}&size=99999"
                );

                System.Diagnostics.Debug.WriteLine($"encoded drugcompanyname: {drugCompanyName}");
                System.Diagnostics.Debug.WriteLine($"institutionCode: {institutionCode}");

                return response.Content;
            }
            catch (Exception ex)
            {
                throw new Exception($"파트너 정보 조회 실패: {ex.Message}", ex);
            }
        }
    }
}