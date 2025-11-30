using medipanda_windows_admin.Models.Settlement;

public class SettlementData<TRow> where TRow : BaseSettlementRow
{
    public List<TRow> Rows { get; set; } = new();
}