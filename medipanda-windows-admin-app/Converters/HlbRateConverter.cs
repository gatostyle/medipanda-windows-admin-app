using medipanda_windows_admin.Models.Rate;
using NPOI.SS.UserModel;
using NPOI.SS.Util;

namespace medipanda_windows_admin.Converters
{
    public class HlbRateConverter : BaseRateConverter
    {
        private static readonly string[] HeaderKeywords = { "NO", "제품코드", "약가", "수수료율", "공지1" };

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
            const int MAX_EMPTY_ROWS = 5;

            while (emptyCount < MAX_EMPTY_ROWS)
            {
                // 행이 머지되어 있고 "출시 예정 상품"이면 종료
                var mergedText = GetMergedCellValue(sheet, currentRow, 0);  // 첫 번째 컬럼 체크
                if (IsMergedRow(sheet, currentRow) && mergedText.Contains("출시 예정 제품"))
                {
                    break;
                }

                var productCode = GetCellString(sheet, currentRow, colIndexes["제품코드"]).Trim();

                if (string.IsNullOrEmpty(productCode))
                {
                    emptyCount++;
                    currentRow++;
                    continue;
                }

                emptyCount = 0;

                var note = GetMergedCellValue(sheet, currentRow, colIndexes["공지1"]);

                var row = new RateRow
                {
                    DrugCompanyName = DrugCompanyName,
                    ProductCode = productCode,
                    DrugPrice = GetCellDecimal(sheet, currentRow, colIndexes["약가"]),
                    BaseCommissionRate = GetCellDecimal(sheet, currentRow, colIndexes["수수료율"]),
                    Note = note
                };

                Data.Rows.Add(row);
                currentRow++;
            }

            return Task.CompletedTask;
        }

        private bool IsMergedRow(ISheet sheet, int rowIndex)
        {
            for (int i = 0; i < sheet.NumMergedRegions; i++)
            {
                var mergedRegion = sheet.GetMergedRegion(i);

                // 해당 행이 머지 영역에 포함되고, 여러 컬럼에 걸쳐 있으면 행 머지로 판단
                if (rowIndex >= mergedRegion.FirstRow && rowIndex <= mergedRegion.LastRow &&
                    mergedRegion.LastColumn - mergedRegion.FirstColumn >= 3)  // 최소 4개 컬럼 이상 머지
                {
                    return true;
                }
            }

            return false;
        }

        private string GetMergedCellValue(ISheet sheet, int rowIndex, int colIndex)
        {
            // 먼저 현재 셀 값 체크
            var cellValue = GetCellString(sheet, rowIndex, colIndex).Trim();
            if (!string.IsNullOrEmpty(cellValue))
            {
                return cellValue;
            }

            // 머지 영역 체크
            for (int i = 0; i < sheet.NumMergedRegions; i++)
            {
                var mergedRegion = sheet.GetMergedRegion(i);

                // 현재 셀이 머지 영역에 포함되는지 확인
                if (rowIndex >= mergedRegion.FirstRow && rowIndex <= mergedRegion.LastRow &&
                    colIndex >= mergedRegion.FirstColumn && colIndex <= mergedRegion.LastColumn)
                {
                    // 머지 영역의 첫 번째 셀 값 반환
                    return GetCellString(sheet, mergedRegion.FirstRow, mergedRegion.FirstColumn).Trim();
                }
            }

            // 머지 아니면 빈 값
            return string.Empty;
        }

        private Dictionary<string, int> FindColumnIndexes(ISheet sheet, int headerRow)
        {
            var indexes = new Dictionary<string, int>
            {
                { "제품코드", -1 },
                { "약가", -1 },
                { "수수료율", -1 },
                { "공지1", -1 }
            };

            var row = sheet.GetRow(headerRow);
            if (row == null) return indexes;

            for (int i = 0; i < row.LastCellNum; i++)
            {
                var cellValue = GetCellString(sheet, headerRow, i);
                var cellValueNoSpace = cellValue.Replace(" ", "");

                if (cellValueNoSpace.Contains("제품코드"))
                    indexes["제품코드"] = i;
                else if (cellValueNoSpace.Contains("약가"))
                    indexes["약가"] = i;
                else if (cellValueNoSpace.Contains("수수료율"))
                    indexes["수수료율"] = i;
                else if (cellValueNoSpace.Contains("공지1"))
                    indexes["공지1"] = i;
            }

            return indexes;
        }
    }
}