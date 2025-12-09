using medipanda_windows_admin.Models.Rate;
using NPOI.SS.UserModel;

namespace medipanda_windows_admin.Converters
{
    public class YoungjinRateConverter : BaseRateConverter
    {
        private static readonly string[] HeaderKeywords = { "EDI코드", "품목명", "기준약가", "수수료율" };

        public override Task ParseAsync()
        {
            Data = new RateData
            {
                ApplyMonth = ApplyMonth
            };

            var sheet = GetSheet(0);

            int headerRow = FindHeaderRow(sheet, HeaderKeywords);
            if (headerRow < 0)
            {
                throw new InvalidOperationException("헤더를 찾을 수 없습니다.");
            }

            var colIndexes = FindColumnIndexes(sheet, headerRow);

            int currentRow = headerRow + 1;
            while (!IsCellEmpty(sheet, currentRow, colIndexes["EDI코드"]))
            {
                var productCode = GetCellString(sheet, currentRow, colIndexes["EDI코드"]).Trim();

                if (!string.IsNullOrEmpty(productCode))
                {
                    var row = new RateRow
                    {
                        DrugCompanyName = DrugCompanyName,
                        ProductCode = productCode,
                        DrugPrice = GetCellDecimal(sheet, currentRow, colIndexes["기준약가"]),
                        BaseCommissionRate = GetCellDecimal(sheet, currentRow, colIndexes["수수료율"]),
                        Note = colIndexes["비고"] >= 0 ? GetCellString(sheet, currentRow, colIndexes["비고"]) : string.Empty
                    };

                    Data.Rows.Add(row);
                }

                currentRow++;
            }

            return Task.CompletedTask;
        }

        private Dictionary<string, int> FindColumnIndexes(ISheet sheet, int headerRow)
        {
            var indexes = new Dictionary<string, int>
            {
                { "EDI코드", -1 },
                { "기준약가", -1 },
                { "수수료율", -1 },
                { "비고", -1 }
            };

            var row = sheet.GetRow(headerRow);
            if (row == null) return indexes;

            for (int i = 0; i < row.LastCellNum; i++)
            {
                var cellValue = GetCellString(sheet, headerRow, i);
                // 모든 공백 제거 후 비교
                var cellValueNoSpace = cellValue.Replace(" ", "");

                if (cellValueNoSpace.Contains("EDI코드"))
                    indexes["EDI코드"] = i;
                else if (cellValueNoSpace.Contains("기준약가"))
                    indexes["기준약가"] = i;
                else if (cellValueNoSpace.Contains("수수료율"))
                    indexes["수수료율"] = i;
                else if (cellValueNoSpace.Contains("비고"))
                    indexes["비고"] = i;
            }

            return indexes;
        }
    }
}