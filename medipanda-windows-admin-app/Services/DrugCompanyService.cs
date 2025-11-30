using medipanda_windows_admin.Models.Response;
using medipanda_windows_admin.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace medipanda_windows_admin.Services
{
    public class DrugCompanyService : BaseApiService
    {
        /// <summary>
        /// 사용자 정보 조회
        /// </summary>
        public async Task<List<DrugCompanyResponse>> GetDrugCompaniesAsync()
        {
            try
            {
                return await GetAsync<List<DrugCompanyResponse>>("/v1/drug-companies");
            }
            catch (Exception ex)
            {
                throw new Exception($"제약사 정보 조회 실패: {ex.Message}", ex);
            }
        }
    }
}
