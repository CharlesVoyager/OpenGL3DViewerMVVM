using OpenGL3DViewerMVVM.ModelLib.model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenGL3DViewerMVVM.View
{
    public class STLComposerViewModel : ViewModelBase
    {
        public ObservableCollection<ThreeDModel> Models { get; set; }

        public STLComposerViewModel()
        {
            Models = new ObservableCollection<ThreeDModel>();
        }

        private ThreeDModel selectedModel;

        public ThreeDModel SelectedModel
        {
            get => selectedModel;
            set
            {
                if (selectedModel != value)
                {
                    selectedModel = value;
                    OnPropertyChanged();
                }
            }
        }
    }


    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
