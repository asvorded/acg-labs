using CommunityToolkit.Mvvm.Input;
using GraphicsLib.Types2;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
namespace Lab1
{


    public class LightEditorViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<LightSourceModel> LightSources { get; } = [];

        private LightSourceModel? _selectedLight;
        public bool ChangesPending { get; private set; }
        public LightSourceModel? SelectedLight
        {
            get => _selectedLight;
            set { _selectedLight = value; OnPropertyChanged(); }
        }
        private bool _isPopupOpen;
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                _isPopupOpen = value;
                OnPropertyChanged(nameof(IsPopupOpen));
            }
        }
        public ICommand AddPointLightCommand { get; }
        public ICommand AddDirectionalLightCommand { get; }
        public ICommand AddSpotLightCommand { get; }
        public ICommand DeleteLightCommand { get; }
        public LightEditorViewModel()
        {
            AddPointLightCommand = new RelayCommand(AddLight<PointLightSourceModel>);
            AddDirectionalLightCommand = new RelayCommand(AddLight<DirectionalLightSourceModel>);
            AddSpotLightCommand = new RelayCommand(AddLight<SpotLightSourceModel>);
            DeleteLightCommand = new RelayCommand(DeleteLight);
            PropertyChanged += ApplyChanges;
            LightSources.CollectionChanged += ApplyChanges;
        }

        private void ApplyChanges(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ChangesPending = true;
        }

        private void ApplyChanges(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(IsPopupOpen) || e.PropertyName == nameof(SelectedLight))
            {
                return;
            }
            ChangesPending = true;
        }

        private void AddLight<T>() where T : LightSourceModel, new()
        {
            var light = new T();
            light.PropertyChanged += ApplyChanges;
            LightSources.Add(light);
        }

        private void DeleteLight()
        {
            if (SelectedLight != null)
            {
                SelectedLight.PropertyChanged -= ApplyChanges;
                LightSources.Remove(SelectedLight);
                SelectedLight = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void ApproveChanges()
        {
            ChangesPending = false;
        }
    }

}
