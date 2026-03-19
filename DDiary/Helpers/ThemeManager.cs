using System;
using System.Windows;
using DDiary.Services;

namespace DDiary.Helpers
{
    /// <summary>
    /// Gestisce il cambio di tema (Light/Dark/System) a runtime.
    /// </summary>
    public static class ThemeManager
    {
        public static void ApplyTheme(string theme)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            // Determine which theme to actually use
            var normalizedTheme = (theme ?? string.Empty).Trim();
            string resolvedTheme = normalizedTheme;

            if (string.Equals(normalizedTheme, "White", StringComparison.OrdinalIgnoreCase))
                resolvedTheme = "Light";
            else if (string.Equals(normalizedTheme, "Black", StringComparison.OrdinalIgnoreCase))
                resolvedTheme = "Dark";

            if (string.Equals(resolvedTheme, "System", StringComparison.OrdinalIgnoreCase))
            {
                resolvedTheme = IsSystemDark() ? "Dark" : "Light";
            }

            var themeUri = string.Equals(resolvedTheme, "Dark", StringComparison.OrdinalIgnoreCase)
                ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
                : new Uri("Themes/LightTheme.xaml", UriKind.Relative);

            // Replace current theme dictionary
            var dict = app.Resources.MergedDictionaries;
            for (int i = dict.Count - 1; i >= 0; i--)
            {
                var src = dict[i].Source?.OriginalString ?? string.Empty;
                if (src.Contains("LightTheme") || src.Contains("DarkTheme"))
                {
                    dict.RemoveAt(i);
                    break;
                }
            }

            dict.Add(new ResourceDictionary { Source = themeUri });
        }

        private static bool IsSystemDark()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int v && v == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
