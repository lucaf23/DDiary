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
            string resolvedTheme = theme;
            if (theme == "System")
            {
                resolvedTheme = IsSystemDark() ? "Dark" : "Light";
            }

            var themeUri = resolvedTheme == "Dark"
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
