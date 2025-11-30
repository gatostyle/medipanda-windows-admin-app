namespace medipanda_windows_admin.Models.Rate
{
    public class RateRow
    {
        public string DrugCompanyName { get; set; } = string.Empty;      // 제약사명
        public string InsuranceType { get; set; } = string.Empty;        // 급여여부
        public string ProductName { get; set; } = string.Empty;          // 제품명
        public string IngredientName { get; set; } = string.Empty;       // 성분명
        public decimal DrugPrice { get; set; }                           // 약가
        public string ProductCode { get; set; } = string.Empty;          // 제품코드
        public decimal BaseCommissionRate { get; set; }                  // 기본수수료율
        public decimal ChangedCommissionRate { get; set; }               // 변경수수료율
        public string ChangedMonth { get; set; } = string.Empty;         // 변경월
        public string Status { get; set; } = string.Empty;               // 상태
        public string Note { get; set; } = string.Empty;                 // 비고
    }

    public class RateData
    {
        public List<RateRow> Rows { get; set; } = new();
    }
}