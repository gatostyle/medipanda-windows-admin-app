namespace medipanda_windows_admin.Models.Settlement
{
    /// <summary>
    /// 국제약품 정산 Row
    /// </summary>
    public class KukjeSettlementRow : BaseSettlementRow
    {
        // 국제약품 기본값 설정
        public KukjeSettlementRow()
        {
            Grade = "A";
            Seq = 1;
            Etc = string.Empty;
            EtcCommissionAmount = 0;
            Hold = string.Empty;
        }
    }
}