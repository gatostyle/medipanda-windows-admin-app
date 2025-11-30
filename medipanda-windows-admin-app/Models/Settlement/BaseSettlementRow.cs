namespace medipanda_windows_admin.Models.Settlement
{
    /// <summary>
    /// 정산 데이터 기본 Row - 출력 공통 필드
    /// </summary>
    public abstract class BaseSettlementRow
    {
        public string SettlementMonth { get; set; } = string.Empty;      // 정산월 (25-08)
        public string DrugCompanyName { get; set; } = string.Empty;      // 제약사명
        public string MemberNo { get; set; } = string.Empty;             // 회원번호
        public string ManagerName { get; set; } = string.Empty;          // 담당자명
        public string ClientCode { get; set; } = string.Empty;           // 처방처코드
        public string BusinessNumber { get; set; } = string.Empty;       // 사업자번호
        public string HospitalName { get; set; } = string.Empty;         // 처방처명
        public string Grade { get; set; } = string.Empty;                // 등급
        public string ProductCode { get; set; } = string.Empty;          // 처방품목코드
        public string ProductName { get; set; } = string.Empty;          // 품명
        public int Seq { get; set; }                                     // seq
        public decimal Quantity { get; set; }                            // 수량
        public decimal UnitPrice { get; set; }                           // 단가
        public decimal PrescriptionAmount { get; set; }                  // 처방금액
        public decimal CommissionRate { get; set; }                      // 수수료지급율
        public decimal CommissionAmount { get; set; }                    // 수수료금액
        public string PrescriptionMonth { get; set; } = string.Empty;    // 처방월
        public string Etc { get; set; } = string.Empty;                  // 기타
        public decimal EtcCommissionAmount { get; set; }                 // 기타수수료금액
        public string Note { get; set; } = string.Empty;                 // 비고
        public string Hold { get; set; } = string.Empty;                 // 보류
    }
}