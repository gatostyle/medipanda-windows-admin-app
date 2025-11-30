namespace medipanda_windows_admin.Models.Settlement
{
    public class DongguSettlementRow : BaseSettlementRow
    {
        // Merge용 키 필드
        public string CorporateCode { get; set; } = string.Empty;  // 법인코드

        // 비고 합치기용 임시 필드
        public string Sheet3Note { get; set; } = string.Empty;
        public string Sheet4Note { get; set; } = string.Empty;
    }
}