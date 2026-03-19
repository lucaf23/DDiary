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

        // ── Spinner Ore ──────────────────────────────────────────────────────────

        private void HourUp_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is FoodInsertViewModel vm) vm.MealHour++;
        }

        private void HourDown_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is FoodInsertViewModel vm) vm.MealHour--;
        }

        /// <summary>
        /// Recupera l'ora dalla rotella del mouse sull'area delle ore.
        /// Scroll su → ora aumenta; scroll giù → ora diminuisce.
        /// </summary>
        private void HourSpinner_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is FoodInsertViewModel vm)
            {
                vm.MealHour += e.Delta > 0 ? 1 : -1;
                e.Handled = true;
            }
        }

        // ── Spinner Minuti ───────────────────────────────────────────────────────

        private void MinUp_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is FoodInsertViewModel vm) vm.MealMinute++;
        }

        private void MinDown_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is FoodInsertViewModel vm) vm.MealMinute--;
        }

        /// <summary>
        /// Recupera il minuto dalla rotella del mouse sull'area dei minuti.
        /// Scroll su → minuto aumenta; scroll giù → minuto diminuisce.
        /// </summary>
        private void MinSpinner_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is FoodInsertViewModel vm)
            {
                vm.MealMinute += e.Delta > 0 ? 1 : -1;
                e.Handled = true;
            }
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
    }
}
