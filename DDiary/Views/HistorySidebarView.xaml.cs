using System.Windows.Controls;
using DDiary.ViewModels;

namespace DDiary.Views
{
    public partial class HistorySidebarView : System.Windows.Controls.UserControl
    {
        public HistorySidebarView()
        {
            InitializeComponent();
        }

        private void DiaryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is HistoryViewModel vm &&
                (sender as System.Windows.Controls.ListBox)?.SelectedItem is DiaryHistoryItemViewModel item)
            {
                vm.SelectDiary(item);
            }
        }
    }
}
