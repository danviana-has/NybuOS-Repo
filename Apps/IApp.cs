using NybuOS.Core.Graphics;
using NybuOS.Core.WindowManager;

namespace NybuOS.Apps;

public interface IApp
{
    string Id { get; }
    string Title { get; }
    FastBitmap? Icon16 { get; }
    FastBitmap? Icon24 { get; }
    FastBitmap? Icon48 { get; }

    void Initialize(Window window);
    void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH);
    void HandleInput(int clientX, int clientY, int clientW, int clientH);
    void OnResize(int newClientW, int newClientH);
    void OnClose();
}
