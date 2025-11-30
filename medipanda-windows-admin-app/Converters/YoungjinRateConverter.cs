using medipanda_windows_admin.Models.Rate;
using NPOI.SS.UserModel;

namespace medipanda_windows_admin.Converters
{
    public class YoungjinRateConverter : BaseRateConverter
    {
        private const int DATA_START_ROW = 2;  // 실제 데이터 시작 행에 맞게 조정

        public override Task ParseAsync()
        {
            Data = new RateData();
            var sheet = GetSheet(0);

            int currentRow = DATA_START_ROW;
            while (!IsCellEmpty(sheet, currentRow, "A"))  // 기준 컬럼에 맞게 조정
            {
                var row = new RateRow
                {
                    DrugCompanyName = DrugCompanyName,
                    InsuranceType = GetCellString(sheet, currentRow, "A"),
                    ProductName = GetCellString(sheet, currentRow, "B"),
                    IngredientName = GetCellString(sheet, currentRow, "C"),
                    DrugPrice = GetCellDecimal(sheet, currentRow, "D"),
                    ProductCode = GetCellString(sheet, currentRow, "E"),
                    BaseCommissionRate = GetCellPercent(sheet, currentRow, "F"),
                    ChangedCommissionRate = GetCellPercent(sheet, currentRow, "G"),
                    ChangedMonth = GetCellString(sheet, currentRow, "H"),
                    Status = GetCellString(sheet, currentRow, "I"),
                    Note = GetCellString(sheet, currentRow, "J")
                };

                Data.Rows.Add(row);
                currentRow++;
            }

            return Task.CompletedTask;
        }
    }
}