
namespace OpenGL3DViewerMVVM.Draw
{
    public interface IDrawBase
    {
        void Init();
        void Draw();
        void Dispose();
        bool CanDraw();
    }
}
