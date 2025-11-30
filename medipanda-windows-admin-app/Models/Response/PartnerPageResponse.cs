namespace medipanda_windows_admin.Models.Response
{
    public class PartnerPageResponse
    {
        public List<PartnerResponse> Content { get; set; } = new();
    }

    public class PartnerResponse
    {
        public long Id { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public long MemberId { get; set; }
        public string InstitutionCode { get; set; } = string.Empty;
        public string BusinessNumber { get; set; } = string.Empty;
    }
}