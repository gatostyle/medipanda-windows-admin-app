using medipanda_windows_admin.Models.Rate;
using NPOI.SS.UserModel;

namespace medipanda_windows_admin.Converters
{
    public class KukjeRateConverter : BaseRateConverter
    {
        private static readonly string[] HeaderKeywords = { "NO", "제품명", "보험약가", "청구코드", "수수료" };

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
            int emptyCount = 0;
            const int MAX_EMPTY_ROWS = 5;  // 연속 5개 빈 행이면 종료

            while (emptyCount < MAX_EMPTY_ROWS)
            {
                var productCode = GetCellString(sheet, currentRow, colIndexes["청구코드"]).Trim();

                if (string.IsNullOrEmpty(productCode))
                {
                    emptyCount++;
                    currentRow++;
                    continue;
                }

                emptyCount = 0;  // 데이터 있으면 리셋

                var row = new RateRow
                {
                    DrugCompanyName = DrugCompanyName,
                    ProductCode = productCode,
                    DrugPrice = GetCellDecimal(sheet, currentRow, colIndexes["보험약가"]),
                    BaseCommissionRate = GetCellDecimal(sheet, currentRow, colIndexes["수수료"]) * 100,
                    Note = colIndexes["비고"] >= 0 ? GetCellString(sheet, currentRow, colIndexes["비고"]) : string.Empty
                };

                Data.Rows.Add(row);
                currentRow++;
            }

            return Task.CompletedTask;
        }

        private Dictionary<string, int> FindColumnIndexes(ISheet sheet, int headerRow)
        {
            var indexes = new Dictionary<string, int>
            {
                { "청구코드", -1 },
                { "보험약가", -1 },
                { "수수료", -1 },
                { "비고", -1 }
            };

            var row = sheet.GetRow(headerRow);
            if (row == null) return indexes;

            for (int i = 0; i < row.LastCellNum; i++)
            {
                var cellValue = GetCellString(sheet, headerRow, i);
                var cellValueNoSpace = cellValue.Replace(" ", "");

                if (cellValueNoSpace.Contains("청구코드"))
                    indexes["청구코드"] = i;
                else if (cellValueNoSpace.Contains("보험약가"))
                    indexes["보험약가"] = i;
                else if (cellValueNoSpace == "수수료")  // 정확히 "수수료"만 매칭
                    indexes["수수료"] = i;
                else if (cellValueNoSpace.Contains("비고"))
                    indexes["비고"] = i;
            }

            return indexes;
        }
    }
}