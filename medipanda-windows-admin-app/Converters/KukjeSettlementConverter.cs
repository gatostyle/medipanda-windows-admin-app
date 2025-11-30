using medipanda_windows_admin.Models.Response;
using medipanda_windows_admin.Models.Settlement;
using medipanda_windows_admin.Services;
using NPOI.SS.UserModel;

namespace medipanda_windows_admin.Converters
{
    public class KukjeSettlementConverter : BaseSettlementConverter<KukjeSettlementRow>
    {
        private const int DATA_START_ROW_SHEET1 = 5;
        private const int DATA_START_ROW_SHEET0 = 2;
        private const string EDI_CHECK_COLUMN = "L";

        private readonly PartnerService _partnerService;

        public KukjeSettlementConverter()
        {
            _partnerService = new PartnerService();
        }

        public override async Task ParseAsync()
        {
            Data = new SettlementData<KukjeSettlementRow>();
            var sheet0 = GetSheet(0);
            var sheet1 = GetSheet(1);
            var validRows = ParseSheet1(sheet1);
            MergeSheet0Data(sheet0, validRows);
            Data.Rows = validRows;
            await FetchAdditionalInfoAsync();
        }

        private List<KukjeSettlementRow> ParseSheet1(ISheet sheet)
        {
            var rows = new List<KukjeSettlementRow>();
            int currentRow = DATA_START_ROW_SHEET1;
            while (!IsCellEmpty(sheet, currentRow, "C"))
            {
                var ediCheck = GetCellString(sheet, currentRow, EDI_CHECK_COLUMN);
                if (ediCheck.Equals("EDI", StringComparison.OrdinalIgnoreCase))
                {
                    var row = new KukjeSettlementRow
                    {
                        SettlementMonth = SettlementMonth,
                        DrugCompanyName = DrugCompanyName,
                        HospitalName = GetCellString(sheet, currentRow, "C"),
                        PrescriptionMonth = GetCellString(sheet, currentRow, "D").Replace("-", "."),
                        ProductName = GetCellString(sheet, currentRow, "F"),
                        UnitPrice = GetCellDecimal(sheet, currentRow, "G"),
                        Quantity = GetCellDecimal(sheet, currentRow, "H"),
                        PrescriptionAmount = GetCellDecimal(sheet, currentRow, "I"),
                        CommissionAmount = GetCellDecimal(sheet, currentRow, "J"),
                        CommissionRate = GetCellDecimal(sheet, currentRow, "K"),
                        Note = GetCellString(sheet, currentRow, "M")
                    };
                    rows.Add(row);
                }
                currentRow++;
            }
            return rows;
        }

        private void MergeSheet0Data(ISheet sheet, List<KukjeSettlementRow> rows)
        {
            int sheet0Row = DATA_START_ROW_SHEET0;
            foreach (var row in rows)
            {
                if (!IsCellEmpty(sheet, sheet0Row, "C"))
                {
                    row.ClientCode = GetCellString(sheet, sheet0Row, "C");
                    row.ProductCode = GetCellString(sheet, sheet0Row, "G");
                }
                sheet0Row++;
            }
        }

        private async Task FetchAdditionalInfoAsync()
        {
            // 처방처코드 중복 제거
            var uniqueClientCodes = Data.Rows
                .Select(r => r.ClientCode)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            // 파트너 정보 캐싱
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"조회 실패: {clientCode} - {ex.Message}");
                }
            }

            // Row에 파트너 정보 채우기
            foreach (var row in Data.Rows)
            {
                if (partnerCache.TryGetValue(row.ClientCode, out var partner))
                {
                    row.MemberNo = partner.MemberId.ToString();
                    row.ManagerName = partner.MemberName;
                    row.BusinessNumber = partner.BusinessNumber;
                }
            }
        }
    }
}