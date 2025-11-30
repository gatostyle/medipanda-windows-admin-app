using NPOI.SS.UserModel;
using medipanda_windows_admin.Models.Settlement;
using medipanda_windows_admin.Models.Response;
using medipanda_windows_admin.Services;

namespace medipanda_windows_admin.Converters
{
    public class YoungjinSettlementConverter : BaseSettlementConverter<YoungjinSettlementRow>
    {
        private const int DATA_START_ROW = 4;

        private readonly PartnerService _partnerService;

        public YoungjinSettlementConverter()
        {
            _partnerService = new PartnerService();
        }

        public override async Task ParseAsync()
        {
            Data = new SettlementData<YoungjinSettlementRow>();
            var sheet = GetSheet(0);
            var rows = ParseSheet(sheet);
            Data.Rows = rows;
            await FetchAdditionalInfoAsync();
        }

        private List<YoungjinSettlementRow> ParseSheet(ISheet sheet)
        {
            var rows = new List<YoungjinSettlementRow>();
            int currentRow = DATA_START_ROW;

            while (!IsCellEmpty(sheet, currentRow, "B"))
            {
                var row = new YoungjinSettlementRow
                {
                    SettlementMonth = SettlementMonth,
                    DrugCompanyName = DrugCompanyName,
                    ClientCode = GetCellString(sheet, currentRow, "E"),
                    HospitalName = GetCellString(sheet, currentRow, "F"),
                    ProductCode = GetCellString(sheet, currentRow, "K"),
                    ProductName = GetCellString(sheet, currentRow, "L"),
                    Quantity = GetCellDecimal(sheet, currentRow, "O"),
                    UnitPrice = GetCellDecimal(sheet, currentRow, "P"),
                    PrescriptionAmount = GetCellDecimal(sheet, currentRow, "Q"),
                    CommissionRate = GetCellDecimal(sheet, currentRow, "S"),
                    CommissionAmount = GetCellDecimal(sheet, currentRow, "V"),
                    Note = GetCellString(sheet, currentRow, "W")
                };
                rows.Add(row);
                currentRow++;
            }

            return rows;
        }

        private async Task FetchAdditionalInfoAsync()
        {
            var uniqueClientCodes = Data.Rows
                .Select(r => r.ClientCode)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            var partnerCache = new Dictionary<string, PartnerResponse>();

            foreach (var clientCode in uniqueClientCodes)
            {
                try
                {
                    var partners = await _partnerService.GetPartnersAsync(clientCode, DrugCompanyName);
                    if (partners.Count > 0)
                    {
                        partnerCache[clientCode] = partners[0];
                    }
                }
                catch
                {
                    // 조회 실패 시 무시
                }
            }

            foreach (var row in Data.Rows)
            {
                if (partnerCache.TryGetValue(row.ClientCode, out var partner))
                {
                    row.MemberNo = partner.Id.ToString();
                    row.ManagerName = partner.MemberName;
                    row.BusinessNumber = partner.BusinessNumber;
                }
            }
        }
    }
}