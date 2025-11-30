using medipanda_windows_admin.Models.Rate;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;

namespace medipanda_windows_admin.Converters
{
    public abstract class BaseRateConverter : IDisposable
    {
        protected IWorkbook? Workbook { get; private set; }
        private FileStream? _fileStream;

        public RateData Data { get; protected set; } = new();
        public string DrugCompanyName { get; set; } = string.Empty;
        public string ApplyMonth { get; set; } = string.Empty;

        public void LoadFile(string filePath)
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var extension = Path.GetExtension(filePath).ToLower();

            Workbook = extension switch
            {
                ".xlsx" => new XSSFWorkbook(_fileStream),
                ".xls" => new HSSFWorkbook(_fileStream),
                _ => throw new NotSupportedException($"지원하지 않는 파일 형식입니다: {extension}")
            };
        }

        public abstract Task ParseAsync();

        public void Dispose()
        {
            Workbook?.Close();
            _fileStream?.Dispose();
        }

        #region Helper Methods

        protected ISheet GetSheet(int index)
        {
            if (Workbook == null)
                throw new InvalidOperationException("파일이 로드되지 않았습니다.");

            return Workbook.GetSheetAt(index);
        }

        protected string GetCellString(ISheet sheet, int rowIndex, int colIndex)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null) return string.Empty;

            var cell = row.GetCell(colIndex);
            if (cell == null) return string.Empty;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => GetFormulaCellValue(cell),
                _ => string.Empty
            };
        }

        protected decimal GetCellDecimal(ISheet sheet, int rowIndex, int colIndex)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null) return 0;

            var cell = row.GetCell(colIndex);
            if (cell == null) return 0;

            return cell.CellType switch
            {
                CellType.Numeric => (decimal)cell.NumericCellValue,
                CellType.String => decimal.TryParse(cell.StringCellValue?.Replace(",", ""), out var result) ? result : 0,
                CellType.Formula => (decimal)cell.NumericCellValue,
                _ => 0
            };
        }

        protected bool IsCellEmpty(ISheet sheet, int rowIndex, int colIndex)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null) return true;

            var cell = row.GetCell(colIndex);
            if (cell == null) return true;

            return cell.CellType == CellType.Blank ||
                   (cell.CellType == CellType.String && string.IsNullOrWhiteSpace(cell.StringCellValue));
        }

        private string GetFormulaCellValue(ICell cell)
        {
            try
            {
                return cell.CachedFormulaResultType switch
                {
                    CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                    CellType.Numeric => cell.NumericCellValue.ToString(),
                    _ => string.Empty
                };
            }
            catch
            {
                return string.Empty;
            }
        }

        protected int FindHeaderRow(ISheet sheet, string[] headerKeywords)
        {
            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                int matchCount = 0;
                for (int j = 0; j < row.LastCellNum; j++)
                {
                    var cellValue = GetCellString(sheet, i, j);
                    if (headerKeywords.Any(k => cellValue.Contains(k)))
                    {
                        matchCount++;
                    }
                }

                if (matchCount >= 3)  // 최소 3개 이상 매칭되면 헤더로 판단
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion
    }
}