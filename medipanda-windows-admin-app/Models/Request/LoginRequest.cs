using System.Text.Json.Serialization;

namespace medipanda_windows_admin.Models.Request
{
    public class LoginRequest
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("device")]
        public DeviceInfo Device { get; set; }
    }

    public class DeviceInfo
    {
        [JsonPropertyName("deviceUuid")]
        public string DeviceUuid { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; } = "windows";

        [JsonPropertyName("appVersion")]
        public string AppVersion { get; set; } = "1.0.0";

        [JsonPropertyName("fcmToken")]
        public string FcmToken { get; set; } = "";
    }
}