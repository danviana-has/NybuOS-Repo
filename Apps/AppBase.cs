using NybuOS.Core.Graphics;
using NybuOS.Core.WindowManager;

namespace NybuOS.Apps;

public abstract class AppBase : IApp
{
    public abstract string Id { get; }
    public abstract string Title { get; }
    public abstract FastBitmap? Icon16 { get; }
    public abstract FastBitmap? Icon24 { get; }
    public abstract FastBitmap? Icon48 { get; }

    protected Window? ParentWindow { get; private set; }

    public virtual void Initialize(Window window)
    {
        ParentWindow = window;
    }

    public abstract void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH);

    public abstract void HandleInput(int clientX, int clientY, int clientW, int clientH);

    public virtual void OnResize(int newClientW, int newClientH) { }

    public virtual void OnClose() { }

    protected bool IsMouseIn(int mx, int my, int x, int y, int w, int h)
    {
        return mx >= x && mx < x + w && my >= y && my < y + h;
    }
}
