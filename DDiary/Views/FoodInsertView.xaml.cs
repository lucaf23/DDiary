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
    }
}
