using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lab1
{
    /// <summary>
    /// Логика взаимодействия для LightSourcesPopup.xaml
    /// </summary>
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
            var button = sender as Button;
            if (DataContext is MainWindowViewModel viewModel && button.DataContext is LightSourceModel)
            {
                viewModel.AddLightSource();
            }
        }
    }
}
