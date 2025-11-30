namespace medipanda_windows_admin.Models.Settlement
{
    /// <summary>
    /// 정산 데이터 통합 Row
    /// </summary>
    public class SettlementRow
    {
        public string HospitalName { get; set; } = string.Empty;      // 병원명
        public string ProductName { get; set; } = string.Empty;       // 제품명
        public decimal DrugPrice { get; set; }                        // 약가
        public int Quantity { get; set; }                             // 수량
        public decimal PrescriptionAmount { get; set; }               // 처방금액
        public string PrescriptionMonth { get; set; } = string.Empty; // 처방월
        public decimal CommissionAmount { get; set; }                 // 수수료금액
        public decimal CommissionRate { get; set; }                   // 수수료지급율
        public string Note { get; set; } = string.Empty;              // 비고

        public string ClientCode { get; set; } = string.Empty;        // 거래처코드
        public string ProductCode { get; set; } = string.Empty;       // 제품코드

        public string Etc { get; set; } = string.Empty;                // 기타정보
        public string Pending { get; set; } = string.Empty;            // 보류정보

        public string MemberNo { get; set; } = string.Empty;          // 회원번호
        public string ManagerName { get; set; } = string.Empty;       // 담당자명
        public string BusinessNumber { get; set; } = string.Empty;    // 사업자등록번호
    }
}