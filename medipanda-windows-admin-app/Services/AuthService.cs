using medipanda_windows_admin.Services;
using medipanda_windows_admin.Services.Base;
using medipanda_windows_admin.Models.Request;
using medipanda_windows_admin.Models.Response;
using System;
using System.Threading.Tasks;

namespace medipanda_windows_admin.Services
{
    public class AuthService : BaseApiService
    {
        private static AuthService _instance;
        public static AuthService Instance => _instance ??= new AuthService();

        private AuthService() : base() { }

        /// <summary>
        /// 로그인
        /// </summary>
        public async Task<LoginResponse> LoginAsync(string userId, string password)
        {
            try
            {
                var request = new LoginRequest
                {
                    UserId = userId,
                    Password = password,
                    Device = new DeviceInfo
                    {
                        DeviceUuid = GetDeviceId(),
                        Platform = "windows",
                        AppVersion = "1.0.0",
                        FcmToken = ""
                    }
                };

                var response = await PostAsync<LoginResponse>("/v1/auth/login", request, useAuth: false);

                // 토큰 저장
                TokenService.Instance.SetTokens(
                    response.AccessToken,
                    response.RefreshToken
                );

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"로그인 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 사용자 정보 조회
        /// </summary>
        public async Task<UserInfoResponse> GetUserInfoAsync()
        {
            try
            {
                return await GetAsync<UserInfoResponse>("/v1/auth/me");
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 로그아웃
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                // 서버에 로그아웃 요청 (필요한 경우)
                //await PostAsync<object>("/v1/auth/logout");

                // 세션 및 토큰 삭제
                UserSessionService.Instance.ClearSession();
            }
            catch (Exception ex)
            {
                throw new Exception($"로그아웃 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 토큰 갱신
        /// </summary>
        public async Task<LoginResponse> RefreshTokenAsync()
        {
            try
            {
                var request = new
                {
                    userId = UserSessionService.Instance.CurrentUser.UserId,
                    refreshToken = TokenService.Instance.RefreshToken
                };

                var response = await PostAsync<LoginResponse>("/v1/auth/token/refresh", request, useAuth: false);

                // 새 토큰 저장
                TokenService.Instance.SetTokens(
                    response.AccessToken,
                    response.RefreshToken
                );

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"토큰 갱신 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 비밀번호 변경
        /// </summary>
        public async Task ChangePasswordAsync(string oldPassword, string newPassword)
        {
            try
            {
                var request = new
                {
                    oldPassword,
                    newPassword
                };

                await PostAsync<object>("/v1/auth/change-password", request);
            }
            catch (Exception ex)
            {
                throw new Exception($"비밀번호 변경 실패: {ex.Message}", ex);
            }
        }
    }
}