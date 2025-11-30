using System.Text.Json.Serialization;

namespace medipanda_windows_admin.Models.Response
{
    public class UserInfoResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("birthDate")]
        public string BirthDate { get; set; }

        [JsonPropertyName("accountStatus")]
        public string AccountStatus { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("partnerContractStatus")]
        public string PartnerContractStatus { get; set; }

        [JsonPropertyName("marketingAgreements")]
        public MarketingAgreements MarketingAgreements { get; set; }

        [JsonPropertyName("referralCode")]
        public string ReferralCode { get; set; }

        [JsonPropertyName("csoCertUrl")]
        public string CsoCertUrl { get; set; }

        [JsonPropertyName("registrationDate")]
        public string RegistrationDate { get; set; }

        [JsonPropertyName("lastLoginDate")]
        public string LastLoginDate { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("nicknameHidden")]
        public bool NicknameHidden { get; set; }

        [JsonPropertyName("contractStatus")]
        public string ContractStatus { get; set; }

        [JsonPropertyName("contractDate")]
        public string ContractDate { get; set; }
    }

    public class MarketingAgreements
    {
        [JsonPropertyName("sms")]
        public bool Sms { get; set; }

        [JsonPropertyName("email")]
        public bool Email { get; set; }

        [JsonPropertyName("push")]
        public bool Push { get; set; }

        [JsonPropertyName("pushAgreedAt")]
        public string PushAgreedAt { get; set; }

        [JsonPropertyName("smsAgreedAt")]
        public string SmsAgreedAt { get; set; }

        [JsonPropertyName("emailAgreedAt")]
        public string EmailAgreedAt { get; set; }
    }
}