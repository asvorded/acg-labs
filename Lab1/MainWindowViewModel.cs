using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab1
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows;

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<LightSourceModel> LightSources { get; set; }
        private LightSourceModel _selectedLight;
        private bool _isPopupOpen;
        private Visibility _detailsVisibility;

        public LightSourceModel SelectedLight
        {
            get => _selectedLight;
            set
            {
                _selectedLight = value;
                OnPropertyChanged(nameof(SelectedLight));
                DetailsVisibility = _selectedLight != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility DetailsVisibility
        {
            get => _detailsVisibility;
            set
            {
                if (_detailsVisibility != value)
                {
                    _detailsVisibility = value;
                    OnPropertyChanged(nameof(DetailsVisibility));
                }
            }
        }

        public ObservableCollection<string> LightTypes { get; set; }

        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                _isPopupOpen = value;
                OnPropertyChanged(nameof(IsPopupOpen));
            }
        }

        public MainWindowViewModel()
        {
            LightSources = [];
            LightTypes = ["Point", "Directional", "Spot", "Ambient"];
            DetailsVisibility = Visibility.Collapsed;
        }

        public void AddLightSource()
        {
            var newLight = new LightSourceModel();
            LightSources.Add(newLight);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
