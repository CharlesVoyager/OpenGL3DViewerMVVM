namespace OpenGL3DViewerMVVM.Draw
{
    public class ModelsDraw : IDrawBase
    {
        public void Init()
        {
            // NOTE: Do NOT need to initialize each model's drawer here.
            //       Each model's drawer will be initialized when the model is added to the view model.
            return;
        }

        public void Draw()
        {
            if (CanDraw() == false) return;
            foreach (var m in MainWindow.main.viewModel.Models)
                m.Drawer.Draw();
        }

        public void Dispose()
        {
            foreach (var m in MainWindow.main.viewModel.Models)
                m.Drawer.Dispose();
        }
        public bool CanDraw() => true;
    }
}
