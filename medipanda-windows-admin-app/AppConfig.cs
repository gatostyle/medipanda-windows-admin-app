using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace medipanda_windows_admin
{
    public static class AppConfig
    {
        private static bool _isDevMode = false;

        public static bool IsDevMode
        {
            get => _isDevMode;
            set => _isDevMode = value;
        }

        public static string GetBaseUrl()
        {
            return _isDevMode
                ? "https://dev.api.medipanda.co.kr"
                : "https://prod.api.medipanda.co.kr";
        }
    }
}
