using System.Windows;
using System.Windows.Input;
using DDiary.Converters;
using DDiary.ViewModels;

namespace DDiary.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Wire export element provider
            Loaded += async (_, _) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.DiaryElementProvider = () => DiaryViewControl;
                    await vm.InitializeAsync();
                }
            };

            // Keep IsFullscreen in sync when the user maximises/restores via OS controls
            StateChanged += (_, _) =>
            {
                if (DataContext is MainViewModel vm)
                    vm.IsFullscreen = WindowState == System.Windows.WindowState.Maximized;
            };
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.CloseInsertPanelCommand.Execute(null);
        }
    }
}
