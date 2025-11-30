using medipanda_windows_admin.Models.Rate;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;

namespace medipanda_windows_admin.Converters
{
    public abstract class BaseRateConverter : IRateConverter
    {
        protected IWorkbook? Workbook { get; private set; }
        private FileStream? _fileStream;

        public RateData Data { get; protected set; } = new();
        public string DrugCompanyName { get; set; } = string.Empty;

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

        protected string GetCellString(ISheet sheet, int rowIndex, string column)
        {
            var row = sheet.GetRow(rowIndex - 1);
            if (row == null) return string.Empty;

            var cell = row.GetCell(ColumnToIndex(column));
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

        protected decimal GetCellDecimal(ISheet sheet, int rowIndex, string column)
        {
            var row = sheet.GetRow(rowIndex - 1);
            if (row == null) return 0;

            var cell = row.GetCell(ColumnToIndex(column));
            if (cell == null) return 0;

            return cell.CellType switch
            {
                CellType.Numeric => (decimal)cell.NumericCellValue,
                CellType.String => decimal.TryParse(cell.StringCellValue?.Replace(",", ""), out var result) ? result : 0,
                CellType.Formula => (decimal)cell.NumericCellValue,
                _ => 0
            };
        }

        protected decimal GetCellPercent(ISheet sheet, int rowIndex, string column)
        {
            var row = sheet.GetRow(rowIndex - 1);
            if (row == null) return 0;

            var cell = row.GetCell(ColumnToIndex(column));
            if (cell == null) return 0;

            return cell.CellType switch
            {
                CellType.Numeric => (decimal)cell.NumericCellValue * 100,
                CellType.String => ParsePercentString(cell.StringCellValue),
                CellType.Formula => (decimal)cell.NumericCellValue * 100,
                _ => 0
            };
        }

        private decimal ParsePercentString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            value = value.Trim().Replace("%", "");
            if (decimal.TryParse(value, out var result))
            {
                return result < 1 ? result * 100 : result;
            }
            return 0;
        }

        protected bool IsCellEmpty(ISheet sheet, int rowIndex, string column)
        {
            var row = sheet.GetRow(rowIndex - 1);
            if (row == null) return true;

            var cell = row.GetCell(ColumnToIndex(column));
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

        private int ColumnToIndex(string column)
        {
            int index = 0;
            foreach (char c in column.ToUpper())
            {
                index = index * 26 + (c - 'A' + 1);
            }
            return index - 1;
        }

        #endregion
    }
}