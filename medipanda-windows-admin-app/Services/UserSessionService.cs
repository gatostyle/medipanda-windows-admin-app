using medipanda_windows_admin.Services;
using medipanda_windows_admin.Models.Response;

namespace medipanda_windows_admin.Services
{
    public class UserSessionService
    {
        private static UserSessionService _instance;
        private UserInfoResponse _currentUser;

        public static UserSessionService Instance => _instance ??= new UserSessionService();

        private UserSessionService() { }

        public UserInfoResponse CurrentUser
        {
            get => _currentUser;
            set => _currentUser = value;
        }

        public bool IsPartnerUser()
        {
            return _currentUser?.PartnerContractStatus == "ORGANIZATION"
                || _currentUser?.PartnerContractStatus == "INDIVIDUAL";
        }

        public bool IsAdmin()
        {
            return _currentUser?.Role == "ADMIN"
                || _currentUser?.Role == "SUPER_ADMIN";
        }

        public void ClearSession()
        {
            _currentUser = null;
            TokenService.Instance.ClearTokens();
        }
    }
}