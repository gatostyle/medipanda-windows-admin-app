using Amazon.S3;
using Amazon.S3.Transfer;
using medipanda_windows_admin.Services.Base;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using System.Text;

namespace medipanda_windows_admin.Services
{
    public class KimsService : BaseApiService
    {
        private static KimsService? _instance;
        public static KimsService Instance => _instance ??= new KimsService();

        private const string S3_BUCKET = "medipanda";

        private KimsService() { }

        public async Task UploadKimsAsync(string excelFilePath)
        {
            var prefix = DateTime.Now.ToString("yyyy-MM-dd");
            var tsvFileName = $"{prefix}.tsv";
            var tsvFilePath = Path.Combine(Path.GetTempPath(), tsvFileName);

            try
            {
                // 1. 엑셀 → TSV 변환
                ConvertExcelToTsv(excelFilePath, tsvFilePath);

                // 2. S3 업로드
                var s3Prefix = await UploadToS3Async(tsvFilePath, prefix);

                // 3. API 호출
                await TriggerKimsUploadAsync(s3Prefix);
            }
            finally
            {
                if (File.Exists(tsvFilePath))
                {
                    File.Delete(tsvFilePath);
                }
            }
        }

        private async Task<string> UploadToS3Async(string tsvFilePath, string datePrefix)
        {
            var env = AppConfig.IsDevMode ? "dev" : "prod";
            var s3Prefix = $"product/uploaded-tsv/{env}/{datePrefix}";
            var fileName = Path.GetFileName(tsvFilePath);
            var s3Key = $"{s3Prefix}/{fileName}";

            var credentials = new Amazon.Runtime.BasicAWSCredentials(
                AppConfig.AwsAccessKeyId,
                AppConfig.AwsSecretAccessKey
            );

            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.APNortheast2
            };

            using var s3Client = new AmazonS3Client(credentials, config);
            var transferUtility = new TransferUtility(s3Client);

            await transferUtility.UploadAsync(tsvFilePath, S3_BUCKET, s3Key);

            return s3Prefix;
        }

        private async Task TriggerKimsUploadAsync(string prefix)
        {
            await PostAsync($"/v1/products/upload-kims-from-s3?prefix={prefix}");
        }

        private void ConvertExcelToTsv(string excelFilePath, string tsvFilePath)
        {
            using var fs = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var extension = Path.GetExtension(excelFilePath).ToLower();

            IWorkbook workbook = extension switch
            {
                ".xlsx" => new XSSFWorkbook(fs),
                ".xls" => new HSSFWorkbook(fs),
                _ => throw new NotSupportedException($"지원하지 않는 파일 형식입니다: {extension}")
            };

            var sheet = workbook.GetSheetAt(0);
            var sb = new StringBuilder();

            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                var cells = new List<string>();
                for (int j = 0; j < row.LastCellNum; j++)
                {
                    var cell = row.GetCell(j);
                    var value = GetCellValue(cell);
                    cells.Add(value);
                }

                sb.AppendLine(string.Join("\t", cells));
            }

            File.WriteAllText(tsvFilePath, sb.ToString(), Encoding.UTF8);
            workbook.Close();
        }

        private string GetCellValue(ICell? cell)
        {
            if (cell == null) return string.Empty;

            var value = cell.CellType switch
            {
                CellType.String => cell.StringCellValue ?? string.Empty,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? cell.DateCellValue?.ToString("yyyy-MM-dd") ?? string.Empty
                    : cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => GetFormulaCellValue(cell),
                _ => string.Empty
            };

            return value.Trim();
        }

        private string GetFormulaCellValue(ICell cell)
        {
            try
            {
                return cell.CachedFormulaResultType switch
                {
                    CellType.String => cell.StringCellValue ?? string.Empty,
                    CellType.Numeric => cell.NumericCellValue.ToString(),
                    _ => string.Empty
                };
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}