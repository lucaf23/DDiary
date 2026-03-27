using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DDiary.ViewModels;

namespace DDiary.Views
{
    public partial class DiaryView : System.Windows.Controls.UserControl
    {
        public DiaryView()
        {
            InitializeComponent();
        }

        // ── Mouse-wheel support for the time spinner ──────────────────────────────

        private void HourSpinner_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is MealSectionViewModel vm)
            {
                if (e.Delta > 0) vm.IncrementHourCommand.Execute(null);
                else             vm.DecrementHourCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void MinuteSpinner_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is MealSectionViewModel vm)
            {
                if (e.Delta > 0) vm.IncrementMinuteCommand.Execute(null);
                else             vm.DecrementMinuteCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
