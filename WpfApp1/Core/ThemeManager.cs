using System;
using System.Linq;
using System.Windows;

namespace WpfApp1.Core
{
    public static class ThemeManager
    {
        private const string LightThemePath = "pack://application:,,,/WpfApp1;component/Themes/LightTheme.xaml";
        private const string DarkThemePath = "pack://application:,,,/WpfApp1;component/Themes/DarkTheme.xaml";

        public static bool IsDarkMode { get; private set; } = false;

        public static void ToggleTheme()
        {
            SetTheme(!IsDarkMode);
        }

        public static void SetTheme(bool isDark)
        {
            IsDarkMode = isDark;
            var themeUri = new Uri(isDark ? DarkThemePath : LightThemePath, UriKind.Absolute);
            
            var existingTheme = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && (d.Source.AbsoluteUri.Contains("Themes/LightTheme.xaml") || d.Source.AbsoluteUri.Contains("Themes/DarkTheme.xaml")));

            if (existingTheme != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(existingTheme);
            }

            var newThemeDict = new ResourceDictionary { Source = themeUri };
            Application.Current.Resources.MergedDictionaries.Add(newThemeDict);
        }
    }
}
