using medipanda_windows_admin.Models.Rate;
using NPOI.SS.UserModel;

namespace medipanda_windows_admin.Converters
{
    public class DongguRateConverter : BaseRateConverter
    {
        private static readonly string[] HeaderKeywords = { "No", "제품명", "보험코드", "약가", "요율" };

        public override Task ParseAsync()
        {
            Data = new RateData
            {
                ApplyMonth = ApplyMonth
            };

            var sheet = GetSheet(0);

            // 헤더 행 찾기
            int headerRow = FindHeaderRow(sheet, HeaderKeywords);
            if (headerRow < 0)
            {
                throw new InvalidOperationException("헤더를 찾을 수 없습니다.");
            }

            // 컬럼 인덱스 찾기
            var colIndexes = FindColumnIndexes(sheet, headerRow);

            // 데이터 파싱
            int currentRow = headerRow + 1;
            while (!IsCellEmpty(sheet, currentRow, colIndexes["보험코드"]))
            {
                var productCode = GetCellString(sheet, currentRow, colIndexes["보험코드"]).Trim();

                if (!string.IsNullOrEmpty(productCode))
                {
                    var row = new RateRow
                    {
                        DrugCompanyName = DrugCompanyName,
                        ProductCode = productCode,
                        DrugPrice = GetCellDecimal(sheet, currentRow, colIndexes["약가"]),
                        BaseCommissionRate = GetCellDecimal(sheet, currentRow, colIndexes["요율"]) * 100,
                        Note = GetCellString(sheet, currentRow, colIndexes["비고"])
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
                { "보험코드", -1 },
                { "약가", -1 },
                { "요율", -1 },
                { "비고", -1 }
            };

            var row = sheet.GetRow(headerRow);
            if (row == null) return indexes;

            for (int i = 0; i < row.LastCellNum; i++)
            {
                var cellValue = GetCellString(sheet, headerRow, i);

                if (cellValue.Contains("보험코드"))
                    indexes["보험코드"] = i;
                else if (cellValue.Contains("약가"))
                    indexes["약가"] = i;
                else if (cellValue.Contains("요율"))
                    indexes["요율"] = i;
                else if (cellValue.Contains("비고"))
                    indexes["비고"] = i;
            }

            return indexes;
        }
    }
}