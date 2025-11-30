using medipanda_windows_admin.Models.Settlement;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;

namespace medipanda_windows_admin.Converters
{
    public abstract class BaseSettlementConverter<TRow> : ISettlementConverter
        where TRow : BaseSettlementRow, new()
    {
        protected IWorkbook? Workbook { get; private set; }
        private FileStream? _fileStream;

        public SettlementData<TRow> Data { get; protected set; } = new();

        public string SettlementMonth { get; set; } = string.Empty;
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

        public virtual void SaveConverted(string outputPath)
        {
            if (File.Exists(outputPath))
            {
                try
                {
                    File.Delete(outputPath);
                }
                catch (IOException)
                {
                    throw new IOException($"파일이 다른 프로그램에서 사용 중입니다. 파일을 닫고 다시 시도해주세요.\n{outputPath}");
                }
            }

            IWorkbook workbook = Path.GetExtension(outputPath).ToLower() == ".xls"
                ? new HSSFWorkbook()
                : new XSSFWorkbook();

            var sheet = workbook.CreateSheet("정산데이터");

            // 헤더 스타일
            var headerStyle = workbook.CreateCellStyle();
            var font = workbook.CreateFont();
            font.IsBold = true;
            headerStyle.SetFont(font);
            headerStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var headers = new[]
            {
                "정산월", "제약사명", "회원번호", "담당자명", "처방처코드",
                "사업자번호", "처방처명", "등급", "처방품목코드", "품명",
                "seq", "수량", "단가", "처방금액", "수수료지급율", "수수료금액",
                "처방월", "기타", "기타수수료 금액", "비고", "보류"
            };

            // 헤더 작성
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }

            // 데이터 작성
            int rowIndex = 1;
            foreach (var item in Data.Rows)
            {
                var row = sheet.CreateRow(rowIndex);
                row.CreateCell(0).SetCellValue(item.SettlementMonth);
                row.CreateCell(1).SetCellValue(item.DrugCompanyName);
                row.CreateCell(2).SetCellValue(item.MemberNo);
                row.CreateCell(3).SetCellValue(item.ManagerName);
                row.CreateCell(4).SetCellValue(item.ClientCode);
                row.CreateCell(5).SetCellValue(item.BusinessNumber);
                row.CreateCell(6).SetCellValue(item.HospitalName);
                row.CreateCell(7).SetCellValue(item.Grade);
                row.CreateCell(8).SetCellValue(item.ProductCode);
                row.CreateCell(9).SetCellValue(item.ProductName);
                row.CreateCell(10).SetCellValue(item.Seq);
                row.CreateCell(11).SetCellValue((double)item.Quantity);
                row.CreateCell(12).SetCellValue((double)item.UnitPrice);
                row.CreateCell(13).SetCellValue((double)item.PrescriptionAmount);
                row.CreateCell(14).SetCellValue((double)item.CommissionRate);
                row.CreateCell(15).SetCellValue((double)item.CommissionAmount); 
                row.CreateCell(16).SetCellValue(item.PrescriptionMonth);
                row.CreateCell(17).SetCellValue(item.Etc);
                row.CreateCell(18).SetCellValue((double)item.EtcCommissionAmount);
                row.CreateCell(19).SetCellValue(item.Note);
                row.CreateCell(20).SetCellValue(item.Hold);

                rowIndex++;
            }

            // 열 너비 자동 조정
            var columnWidths = new[]
{
    12,  // 정산월
    15,  // 제약사명
    15,  // 회원번호
    12,  // 담당자명
    15,  // 처방처코드
    18,  // 사업자번호
    25,  // 처방처명
    8,   // 등급
    15,  // 처방품목코드
    30,  // 품명
    8,   // seq
    12,  // 수량
    12,  // 단가
    15,  // 처방금액
    15,  // 수수료지급율
    12,  // 처방월
    12,  // 기타
    18,  // 기타수수료금액
    20,  // 비고
    10   // 보류
};

            for (int i = 0; i < columnWidths.Length; i++)
            {
                sheet.SetColumnWidth(i, columnWidths[i] * 256);
            }

            // 저장
            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            workbook.Write(fs);
            workbook.Close();
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
                CellType.String => decimal.TryParse(cell.StringCellValue, out var result) ? result : 0,
                CellType.Formula => (decimal)cell.NumericCellValue,
                _ => 0
            };
        }

        protected int GetCellInt(ISheet sheet, int rowIndex, string column)
        {
            var row = sheet.GetRow(rowIndex - 1);
            if (row == null) return 0;

            var cell = row.GetCell(ColumnToIndex(column));
            if (cell == null) return 0;

            return cell.CellType switch
            {
                CellType.Numeric => (int)cell.NumericCellValue,
                CellType.String => int.TryParse(cell.StringCellValue, out var result) ? result : 0,
                CellType.Formula => (int)cell.NumericCellValue,
                _ => 0
            };
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