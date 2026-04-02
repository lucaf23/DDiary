using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DDiary.Models;
using PdfSharp.Fonts;

namespace DDiary.Services
{
    /// <summary>
    /// Servizio per esportare il diario giornaliero come PNG e PDF.
    /// </summary>
    public interface IExportService
    {
        Task<string> ExportAsPngAsync(FrameworkElement element, DailyDiary diary, UserProfile profile, string? folder = null);
        Task<string> ExportAsPdfAsync(DailyDiary diary, UserProfile profile, string? folder = null);
        void CopyToClipboard(FrameworkElement element);
    }

    public class ExportService : IExportService
    {
        static ExportService()
        {
            // PdfSharp 6.x (Core build) non risolve automaticamente i font di sistema.
            // Inizializzazione globale una sola volta, prima di creare qualsiasi XFont.
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }

        public async Task<string> ExportAsPngAsync(FrameworkElement element, DailyDiary diary, UserProfile profile, string? folder = null)
        {
            var exportFolder = GetExportFolder(folder, profile);
            var fileName = BuildFileName(diary, profile, "png");
            var fullPath = Path.Combine(exportFolder, fileName);

            Directory.CreateDirectory(exportFolder);

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RenderElementToPng(element, fullPath);
            });

            return fullPath;
        }

        public async Task<string> ExportAsPdfAsync(DailyDiary diary, UserProfile profile, string? folder = null)
        {
            var exportFolder = GetExportFolder(folder, profile);
            var fileName = BuildFileName(diary, profile, "pdf");
            var fullPath = Path.Combine(exportFolder, fileName);

            Directory.CreateDirectory(exportFolder);

            await Task.Run(() =>
            {
                GeneratePdf(diary, profile, fullPath);
            });

            return fullPath;
        }

        public void CopyToClipboard(FrameworkElement element)
        {
            try
            {
                var bmp = RenderToBitmap(element);
                System.Windows.Clipboard.SetImage(bmp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CopyToClipboard error: {ex.Message}");
            }
        }

        private static void RenderElementToPng(FrameworkElement element, string path)
        {
            var bmp = RenderToBitmap(element);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using var stream = File.OpenWrite(path);
            encoder.Save(stream);
        }

        private static BitmapSource RenderToBitmap(FrameworkElement element)
        {
            element.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            element.Arrange(new Rect(element.DesiredSize));
            element.UpdateLayout();

            var width = (int)element.ActualWidth;
            var height = (int)element.ActualHeight;

            // Se le dimensioni sono 0, tentare di usare DesiredSize
            if (width <= 0)
                width = (int)element.DesiredSize.Width;
            if (height <= 0)
                height = (int)element.DesiredSize.Height;

            // Se ancora 0, usare dimensioni minime ragionevoli
            if (width <= 0)
                width = 800;
            if (height <= 0)
                height = 600;

            var dpi = 96.0;
            var renderBitmap = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
            renderBitmap.Render(element);
            return renderBitmap;
        }

        private static void GeneratePdf(DailyDiary diary, UserProfile profile, string path)
        {
            // Use PdfSharp to generate a simple, clean white-background PDF
            var document = new PdfSharp.Pdf.PdfDocument();
            document.Info.Title = $"DDiary - {diary.Date:dd/MM/yyyy}";
            document.Info.Author = profile.DisplayName;

            var page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
            var fontTitle = new PdfSharp.Drawing.XFont("Arial", 16, PdfSharp.Drawing.XFontStyleEx.Bold);
            var fontHeader = new PdfSharp.Drawing.XFont("Arial", 10, PdfSharp.Drawing.XFontStyleEx.Bold);
            var fontNormal = new PdfSharp.Drawing.XFont("Arial", 9, PdfSharp.Drawing.XFontStyleEx.Regular);

            double margin = 40;
            double y = margin;
            double pageWidth = page.Width.Point;

            // Title
            gfx.DrawString($"DDiary – {diary.Date:dddd dd MMMM yyyy}", fontTitle,
                PdfSharp.Drawing.XBrushes.Black,
                new PdfSharp.Drawing.XRect(margin, y, pageWidth - 2 * margin, 24),
                PdfSharp.Drawing.XStringFormats.TopLeft);
            y += 28;

            gfx.DrawString($"Profilo: {profile.DisplayName}", fontNormal,
                PdfSharp.Drawing.XBrushes.Black,
                new PdfSharp.Drawing.XRect(margin, y, pageWidth - 2 * margin, 16),
                PdfSharp.Drawing.XStringFormats.TopLeft);
            y += 20;

            // Black horizontal rule
            gfx.DrawLine(PdfSharp.Drawing.XPens.Black, margin, y, pageWidth - margin, y);
            y += 8;

            // Meal sections
            foreach (var section in diary.MealSections)
            {
                // Section header
                string mealLabel = GetMealLabel(section.MealType);
                gfx.DrawString(mealLabel, fontHeader, PdfSharp.Drawing.XBrushes.Black,
                    new PdfSharp.Drawing.XRect(margin, y, pageWidth - 2 * margin, 16),
                    PdfSharp.Drawing.XStringFormats.TopLeft);
                y += 18;

                // Table header
                double col1 = margin, col2 = margin + 200, col3 = margin + 300, col4 = margin + 380;
                gfx.DrawString("Alimento", fontHeader, PdfSharp.Drawing.XBrushes.Black,
                    new PdfSharp.Drawing.XRect(col1, y, 190, 14), PdfSharp.Drawing.XStringFormats.TopLeft);
                gfx.DrawString("Porzione (g)", fontHeader, PdfSharp.Drawing.XBrushes.Black,
                    new PdfSharp.Drawing.XRect(col2, y, 90, 14), PdfSharp.Drawing.XStringFormats.TopLeft);
                gfx.DrawString("CHO (g)", fontHeader, PdfSharp.Drawing.XBrushes.Black,
                    new PdfSharp.Drawing.XRect(col3, y, 70, 14), PdfSharp.Drawing.XStringFormats.TopLeft);
                gfx.DrawString("Ora", fontHeader, PdfSharp.Drawing.XBrushes.Black,
                    new PdfSharp.Drawing.XRect(col4, y, 60, 14), PdfSharp.Drawing.XStringFormats.TopLeft);
                y += 16;

                gfx.DrawLine(PdfSharp.Drawing.XPens.Black, margin, y, pageWidth - margin, y);
                y += 4;

                foreach (var food in section.FoodEntries)
                {
                    gfx.DrawString(food.FoodName, fontNormal, PdfSharp.Drawing.XBrushes.Black,
                        new PdfSharp.Drawing.XRect(col1, y, 190, 12), PdfSharp.Drawing.XStringFormats.TopLeft);
                    gfx.DrawString(food.PortionGrams.ToString("F0"), fontNormal, PdfSharp.Drawing.XBrushes.Black,
                        new PdfSharp.Drawing.XRect(col2, y, 90, 12), PdfSharp.Drawing.XStringFormats.TopLeft);
                    gfx.DrawString(food.ChoGrams.ToString("F1"), fontNormal, PdfSharp.Drawing.XBrushes.Black,
                        new PdfSharp.Drawing.XRect(col3, y, 70, 12), PdfSharp.Drawing.XStringFormats.TopLeft);
                    gfx.DrawString(food.MealTime.ToString(@"hh\:mm"), fontNormal, PdfSharp.Drawing.XBrushes.Black,
                        new PdfSharp.Drawing.XRect(col4, y, 60, 12), PdfSharp.Drawing.XStringFormats.TopLeft);
                    y += 14;
                }

                // Summary row
                string summary = $"Totale CHO: {section.TotalCho:F1} g";
                if (section.GlycemiaBefore.HasValue)
                    summary += $"   Glicemia prima: {section.GlycemiaBefore:F0} mg/dL";
                if (section.GlycemiaAfter.HasValue)
                    summary += $"   Glicemia dopo: {section.GlycemiaAfter:F0} mg/dL";
                if (section.InsulinCarbRatio > 0)
                    summary += $"   Rapporto ins/carb: 1:{section.InsulinCarbRatio:F1}";

                gfx.DrawString(summary, fontNormal, PdfSharp.Drawing.XBrushes.Black,
                    new PdfSharp.Drawing.XRect(margin, y, pageWidth - 2 * margin, 12),
                    PdfSharp.Drawing.XStringFormats.TopLeft);
                y += 14;

                gfx.DrawLine(new PdfSharp.Drawing.XPen(PdfSharp.Drawing.XColors.LightGray, 0.5),
                    margin, y, pageWidth - margin, y);
                y += 8;

                // New page if needed
                if (y > page.Height.Point - 60)
                {
                    page = document.AddPage();
                    page.Size = PdfSharp.PageSize.A4;
                    gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
                    y = margin;
                }
            }

            // Notes
            if (!string.IsNullOrWhiteSpace(diary.Notes))
            {
                gfx.DrawString("Note:", fontHeader, PdfSharp.Drawing.XBrushes.Black,
                    new PdfSharp.Drawing.XRect(margin, y, pageWidth - 2 * margin, 16),
                    PdfSharp.Drawing.XStringFormats.TopLeft);
                y += 18;
                gfx.DrawString(diary.Notes, fontNormal, PdfSharp.Drawing.XBrushes.Black,
                    new PdfSharp.Drawing.XRect(margin, y, pageWidth - 2 * margin, 14),
                    PdfSharp.Drawing.XStringFormats.TopLeft);
                y += 18;
            }

            if (!string.IsNullOrWhiteSpace(diary.PhysicalActivityNotes))
            {
                gfx.DrawString($"Attività fisica: {diary.PhysicalActivityNotes}", fontNormal,
                    PdfSharp.Drawing.XBrushes.Black,
                    new PdfSharp.Drawing.XRect(margin, y, pageWidth - 2 * margin, 14),
                    PdfSharp.Drawing.XStringFormats.TopLeft);
            }

            document.Save(path);
        }

        private static string GetMealLabel(MealType type) => type switch
        {
            MealType.Colazione => "Colazione",
            MealType.MerendaMattina => "Merenda mattina",
            MealType.Pranzo => "Pranzo",
            MealType.MerendaPomeriggio => "Merenda pomeriggio",
            MealType.Cena => "Cena",
            MealType.DopoCena => "Dopo cena",
            _ => type.ToString()
        };

        private static string BuildFileName(DailyDiary diary, UserProfile profile, string extension)
        {
            var safeName = string.Concat(profile.DisplayName.Split(Path.GetInvalidFileNameChars()));
            return $"DDiary_{diary.Date:yyyy-MM-dd}_{safeName}.{extension}";
        }

        private static string GetExportFolder(string? folder, UserProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(folder))
                return folder;
            if (!string.IsNullOrWhiteSpace(profile.PreferredExportFolder))
                return profile.PreferredExportFolder;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
        }
    }
}
