using medipanda_windows_admin.Models.Rate;
using NPOI.SS.UserModel;

namespace medipanda_windows_admin.Converters
{
    public class HanpoongRateConverter : BaseRateConverter
    {
        // 헤더 키워드 (용역\n수수료 는 줄바꿈 포함)
        private static readonly string[] HeaderKeywords = { "분류번호", "용역", "제품명", "성분", "약가" };

        public override Task ParseAsync()
        {
            Data = new RateData
            {
                ApplyMonth = ApplyMonth
            };

            var sheet = GetSheet(0);

            // 전체 시트를 순회하면서 헤더를 찾고 데이터를 파싱
            int currentRow = 0;
            int lastRow = sheet.LastRowNum;

            while (currentRow <= lastRow)
            {
                var row = sheet.GetRow(currentRow);

                // 빈 행이면 스킵
                if (row == null)
                {
                    currentRow++;
                    continue;
                }

                var firstCellValue = GetCellString(sheet, currentRow, 0);

                // ○로 시작하는 분류 행이면 스킵
                if (firstCellValue.StartsWith("○"))
                {
                    currentRow++;
                    continue;
                }

                // 헤더 행인지 확인
                if (IsHeaderRow(sheet, currentRow))
                {
                    // 헤더 다음 행부터 데이터 파싱
                    currentRow++;
                    currentRow = ParseDataRows(sheet, currentRow, lastRow);
                    continue;
                }

                currentRow++;
            }

            return Task.CompletedTask;
        }

        private bool IsHeaderRow(ISheet sheet, int rowIndex)
        {
            var cell1 = GetCellString(sheet, rowIndex, 0);
            var cell2 = GetCellString(sheet, rowIndex, 1);
            var cell3 = GetCellString(sheet, rowIndex, 2);

            // "분류번호", "용역\n수수료" 또는 "용역", "제품명" 패턴 확인
            return cell1.Contains("분류번호") &&
                   (cell2.Contains("용역") || cell2.Contains("수수료")) &&
                   cell3.Contains("제품명");
        }

        private int ParseDataRows(ISheet sheet, int startRow, int lastRow)
        {
            int currentRow = startRow;

            while (currentRow <= lastRow)
            {
                var row = sheet.GetRow(currentRow);

                // 빈 행이면 계속 진행 (빈 행이 여러 개 있을 수 있음)
                if (row == null || IsRowEmpty(row))
                {
                    currentRow++;
                    continue;
                }

                var firstCellValue = GetCellString(sheet, currentRow, 0);

                // ○로 시작하면 새로운 분류 시작 -> 루프 종료
                if (firstCellValue.StartsWith("○"))
                {
                    return currentRow;
                }

                // 헤더 행이면 루프 종료
                if (IsHeaderRow(sheet, currentRow))
                {
                    return currentRow;
                }

                // 데이터 행 파싱
                var productCode = GetCellString(sheet, currentRow, 7).Trim();  // 보험코드 (H열, index 7)

                if (!string.IsNullOrEmpty(productCode))
                {
                    var rateRow = new RateRow
                    {
                        DrugCompanyName = DrugCompanyName,
                        ProductCode = productCode,
                        DrugPrice = GetDrugPrice(sheet, currentRow, 6),             // 약가(원) (G열, index 6) - 두 줄이면 두번째 줄
                        BaseCommissionRate = GetCellDecimal(sheet, currentRow, 1) / 100,  // 용역수수료 (B열, index 1) - 100으로 나눔
                        Note = string.Empty
                    };

                    Data.Rows.Add(rateRow);
                }

                currentRow++;
            }

            return currentRow;
        }

        private bool IsRowEmpty(IRow row)
        {
            for (int i = 0; i < 8; i++)
            {
                var cell = row.GetCell(i);
                if (cell != null && !string.IsNullOrWhiteSpace(GetCellValueAsString(cell)))
                {
                    return false;
                }
            }
            return true;
        }

        private string GetCellValueAsString(ICell cell)
        {
            if (cell == null) return string.Empty;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue ?? string.Empty,
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.CellType == CellType.String
                    ? cell.StringCellValue ?? string.Empty
                    : cell.NumericCellValue.ToString(),
                _ => string.Empty
            };
        }

        /// <summary>
        /// 약가 셀에서 값을 가져옴. 두 줄인 경우 두번째 줄만 파싱
        /// </summary>
        private decimal GetDrugPrice(ISheet sheet, int rowIndex, int colIndex)
        {
            var cellValue = GetCellString(sheet, rowIndex, colIndex);

            if (string.IsNullOrWhiteSpace(cellValue))
                return 0;

            // 줄바꿈이 있으면 두번째 줄만 사용
            if (cellValue.Contains('\n'))
            {
                var lines = cellValue.Split('\n');
                if (lines.Length >= 2)
                {
                    cellValue = lines[1].Trim();
                }
            }

            // 숫자만 추출해서 파싱
            var numericString = new string(cellValue.Where(c => char.IsDigit(c) || c == '.').ToArray());

            if (decimal.TryParse(numericString, out var result))
                return result;

            return 0;
        }
    }
}