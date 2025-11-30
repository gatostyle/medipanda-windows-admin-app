using NPOI.SS.UserModel;
using medipanda_windows_admin.Models.Settlement;
using medipanda_windows_admin.Models.Response;
using medipanda_windows_admin.Services;

namespace medipanda_windows_admin.Converters
{
    public class DongguSettlementConverter : BaseSettlementConverter<DongguSettlementRow>
    {
        private const int SHEET3_START_ROW = 3;
        private const int SHEET4_START_ROW = 3;

        private readonly PartnerService _partnerService;

        public DongguSettlementConverter()
        {
            _partnerService = new PartnerService();
        }

        public override async Task ParseAsync()
        {
            Data = new SettlementData<DongguSettlementRow>();

            var sheet3 = GetSheet(2);
            var sheet4 = GetSheet(3);

            var sheet3Rows = ParseSheet3(sheet3);
            var sheet4Rows = ParseSheet4(sheet4);
            var mergedRows = MergeSheets(sheet3Rows, sheet4Rows);

            Data.Rows = mergedRows;

            await FetchAdditionalInfoAsync();
        }

        private List<DongguSettlementRow> ParseSheet3(ISheet sheet)
        {
            var rows = new List<DongguSettlementRow>();
            int currentRow = SHEET3_START_ROW;

            while (!IsCellEmpty(sheet, currentRow, "D"))
            {
                var productCode = GetCellString(sheet, currentRow, "L").Trim();

                // 제품코드가 '.'만 있으면 스킵
                if (productCode == "." || productCode == "-")
                {
                    currentRow++;
                    continue;
                }

                var row = new DongguSettlementRow
                {
                    SettlementMonth = SettlementMonth,
                    DrugCompanyName = DrugCompanyName,
                    CorporateCode = GetCellString(sheet, currentRow, "D"),
                    ClientCode = GetCellString(sheet, currentRow, "H"),
                    ProductCode = productCode,
                    HospitalName = GetCellString(sheet, currentRow, "J"),
                    BusinessNumber = GetCellString(sheet, currentRow, "I"),
                    Grade = GetCellString(sheet, currentRow, "K"),
                    ProductName = GetCellString(sheet, currentRow, "M"),
                    Seq = GetCellInt(sheet, currentRow, "N"),
                    Quantity = GetCellDecimal(sheet, currentRow, "O"),
                    UnitPrice = GetCellDecimal(sheet, currentRow, "P"),
                    PrescriptionAmount = GetCellDecimal(sheet, currentRow, "Q"),
                    CommissionRate = GetCellDecimal(sheet, currentRow, "R"),
                    CommissionAmount = GetCellDecimal(sheet, currentRow, "S"),
                    PrescriptionMonth = ParsePrescriptionMonth(GetCellString(sheet, currentRow, "T")),
                    Hold = GetCellString(sheet, currentRow, "V"),
                    Sheet3Note = GetCellString(sheet, currentRow, "U")
                };

                rows.Add(row);
                currentRow++;
            }

            return rows;
        }

        private List<DongguSettlementRow> ParseSheet4(ISheet sheet)
        {
            var rows = new List<DongguSettlementRow>();
            int currentRow = SHEET4_START_ROW;

            while (!IsCellEmpty(sheet, currentRow, "B"))
            {
                var productCode = GetCellString(sheet, currentRow, "J").Trim();

                // 제품코드가 '.'만 있으면 스킵
                if (productCode == "." || productCode == "-")
                {
                    currentRow++;
                    continue;
                }

                var row = new DongguSettlementRow
                {
                    SettlementMonth = SettlementMonth,
                    DrugCompanyName = DrugCompanyName,
                    CorporateCode = GetCellString(sheet, currentRow, "B"),
                    ClientCode = GetCellString(sheet, currentRow, "F"),
                    ProductCode = productCode,
                    HospitalName = GetCellString(sheet, currentRow, "H"),
                    BusinessNumber = GetCellString(sheet, currentRow, "G"),
                    Grade = GetCellString(sheet, currentRow, "I"),
                    ProductName = GetCellString(sheet, currentRow, "K"),
                    Seq = GetCellInt(sheet, currentRow, "L"),
                    Quantity = GetCellDecimal(sheet, currentRow, "M"),
                    UnitPrice = GetCellDecimal(sheet, currentRow, "N"),
                    PrescriptionAmount = GetCellDecimal(sheet, currentRow, "O"),
                    CommissionRate = GetCellDecimal(sheet, currentRow, "P"),
                    CommissionAmount = GetCellDecimal(sheet, currentRow, "Q"),
                    PrescriptionMonth = ParsePrescriptionMonth(GetCellString(sheet, currentRow, "R")),
                    Etc = GetCellString(sheet, currentRow, "S"),
                    EtcCommissionAmount = GetCellDecimal(sheet, currentRow, "T"),
                    Sheet4Note = GetCellString(sheet, currentRow, "U")
                };

                rows.Add(row);
                currentRow++;
            }

            return rows;
        }

        private string GetMergeKey(DongguSettlementRow row)
        {
            return $"{row.CorporateCode}|{row.ClientCode}|{row.ProductCode}";
        }

        private List<DongguSettlementRow> MergeSheets(List<DongguSettlementRow> sheet3Rows, List<DongguSettlementRow> sheet4Rows)
        {
            var result = new List<DongguSettlementRow>();

            // 4번 시트를 Dictionary로 (Key: 법인코드+처방처코드+Seq)
            var sheet4Dict = sheet4Rows
                .GroupBy(r => GetMergeKey(r))
                .ToDictionary(g => g.Key, g => g.First());

            // 3번 시트 기준 merge
            foreach (var row3 in sheet3Rows)
            {
                var key = GetMergeKey(row3);

                if (sheet4Dict.TryGetValue(key, out var row4))
                {
                    // 매칭: 4번 시트의 기타 정보 추가
                    row3.Etc = row4.Etc;
                    row3.EtcCommissionAmount = row4.EtcCommissionAmount;
                    row3.Note = BuildNote(row3.Sheet3Note, row4.Sheet4Note);

                    sheet4Dict.Remove(key);
                }
                else
                {
                    // 3번 시트에만 있음
                    row3.Note = BuildNote(row3.Sheet3Note, null);
                }

                result.Add(row3);
            }

            // 4번 시트에만 있는 항목
            foreach (var row4 in sheet4Dict.Values)
            {
                row4.Note = BuildNote(null, row4.Sheet4Note);
                result.Add(row4);
            }

            return result;
        }

        private string BuildNote(string? sheet3Note, string? sheet4Note)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(sheet3Note))
            {
                parts.Add($"[처방]{sheet3Note}");
            }

            if (!string.IsNullOrWhiteSpace(sheet4Note))
            {
                parts.Add($"[기타]{sheet4Note}");
            }

            return string.Join("\n", parts);
        }

        private string ParsePrescriptionMonth(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var trimmed = value.Trim();

            if (trimmed == ".")
            {
                return GetPreviousSettlementMonth();
            }

            return trimmed;
        }

        private string GetPreviousSettlementMonth()
        {
            if (string.IsNullOrEmpty(SettlementMonth))
                return string.Empty;

            var cleaned = "20" + SettlementMonth.Replace(".", "").Replace("-", "").Replace("/", "");

            if (cleaned.Length >= 6 &&
                int.TryParse(cleaned.Substring(0, 4), out int year) &&
                int.TryParse(cleaned.Substring(4, 2), out int month))
            {
                var date = new DateTime(year, month, 1).AddMonths(-1);
                return date.ToString("yyyy.MM");  // 2025.01 형식으로 반환
            }

            return string.Empty;
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
                    if (string.IsNullOrEmpty(row.BusinessNumber))
                    {
                        row.BusinessNumber = partner.BusinessNumber;
                    }
                }
            }
        }
    }
}