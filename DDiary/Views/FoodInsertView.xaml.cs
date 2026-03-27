using System.Windows;
using System.Windows.Input;
using DDiary.ViewModels;

namespace DDiary.Views
{
    public partial class FoodInsertView : System.Windows.Controls.UserControl
    {
        public FoodInsertView()
        {
            InitializeComponent();
            Loaded += (_, _) => FoodNameBox?.Focus();
        }

        // ── Tasti rapidi ─────────────────────────────────────────────────────────

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is FoodInsertViewModel vm)
            {
                if (e.Key == Key.Escape)
                {
                    vm.CancelCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter && vm.IsValid)
                {
                    vm.SaveCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        // ── Mouse-wheel support for the time spinner ──────────────────────────────

        private void HourSpinner_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is FoodInsertViewModel vm)
            {
                if (e.Delta > 0) vm.IncrementHourCommand.Execute(null);
                else             vm.DecrementHourCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void MinuteSpinner_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is FoodInsertViewModel vm)
            {
                if (e.Delta > 0) vm.IncrementMinuteCommand.Execute(null);
                else             vm.DecrementMinuteCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
