namespace medipanda_windows_admin.Models.Rate
{
    public class RateRow
    {
        public string DrugCompanyName { get; set; } = string.Empty;  // 제약사명
        public string ProductCode { get; set; } = string.Empty;       // 제품코드 (보험코드)
        public decimal DrugPrice { get; set; }                        // 약가
        public decimal BaseCommissionRate { get; set; }               // 기본수수료율
        public string Note { get; set; } = string.Empty;              // 비고
    }

    public class RateData
    {
        public string ApplyMonth { get; set; } = string.Empty;        // 적용월
        public List<RateRow> Rows { get; set; } = new();
    }
}