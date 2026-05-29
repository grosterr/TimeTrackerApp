using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Core
{
    public static class SessionManager
    {
        public static int CurrentUserId { get; set; } = 0;

        public static string CurrentUsername { get; set; } = "Гість";

        public static bool IsGuest => CurrentUserId == 0;
    }
}
