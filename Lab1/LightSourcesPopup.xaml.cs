using System.Windows;
using System.Windows.Controls;

namespace Lab1
{

    public partial class LightSourcesPopup : UserControl
    {
        public LightSourcesPopup()
        {
            InitializeComponent();
        }

        private void AddLightSourceButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.AddLightSource();
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                var selectedItem = e.NewValue as LightSourceModel;
                viewModel.SelectedLight = selectedItem;
            }
        }

        private void AddChildButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button
                && DataContext is MainWindowViewModel viewModel
                && button.DataContext is LightSourceModel)
            {
                viewModel.AddLightSource();
            }
        }

    }
}
