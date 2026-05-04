using OpenGL3DViewerMVVM.ModelLib.model;
using System.Collections.ObjectModel;

namespace OpenGL3DViewerMVVM.View
{
    public class STLComposerViewModel
    {
        public ObservableCollection<ThreeDModel> Models { get; } = new ObservableCollection<ThreeDModel>();

        private ThreeDModel selectedModel;

        public ThreeDModel SelectedModel
        {
            get => selectedModel;
            set
            {
                if (selectedModel != value)
                {
                    selectedModel = value;
                }
            }
        }
    }
}
