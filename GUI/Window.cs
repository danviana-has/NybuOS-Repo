using System;
using System.Drawing;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;

namespace NybuOS.GUI;

/// <summary>
/// Janela base do NybuOS RV (Revolution) com estética macOS,
/// superfícies de vidro fosco (Frosted Glass), botões circulares (fechar, minimizar, zoom)
/// e cantos arredondados de alta fidelidade visual.
/// </summary>
public abstract class Window
{
    public int Id { get; set; }
    public string Title { get; set; }
    public IconImage Icon { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public bool IsActive { get; set; }
    public bool IsMinimized { get; set; }
    public bool IsClosed { get; set; }

    public const int TitleBarHeight = 38;
    public const int BorderWidth = 1;

    public int ClientX => X + BorderWidth;
    public int ClientY => Y + TitleBarHeight;
    public int ClientWidth => Width - (BorderWidth * 2);
    public int ClientHeight => Height - TitleBarHeight - BorderWidth;

    public void Close()
    {
        IsClosed = true;
    }

    protected Window(int id, string title, int x, int y, int width, int height)
    {
        Id = id;
        Title = title;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        IsActive = false;
        IsMinimized = false;
        IsClosed = false;
    }

    public bool IsTitleBarClicked(int mx, int my)
    {
        if (mx >= X && mx <= X + Width && my >= Y && my <= Y + TitleBarHeight)
        {
            // Não arrasta a janela se o clique foi nos botões circulares à esquerda
            if (mx <= X + 80)
            {
                return false;
            }
            return true;
        }
        return false;
    }

    public bool IsCloseButtonClicked(int mx, int my)
    {
        int dx = mx - (X + 18);
        int dy = my - (Y + 19);
        return (dx * dx + dy * dy) <= 100; // raio de clique 10px
    }

    public bool IsMinimizeButtonClicked(int mx, int my)
    {
        int dx = mx - (X + 42);
        int dy = my - (Y + 19);
        return (dx * dx + dy * dy) <= 100; // raio de clique 10px
    }

    public bool ContainsPoint(int px, int py)
    {
        if (IsMinimized || IsClosed) return false;
        return px >= X && px <= X + Width && py >= Y && py <= Y + Height;
    }

    public virtual void Draw(DisplayDriver display)
    {
        if (IsMinimized || IsClosed) return;

        // 1. Moldura da Janela Light Theme: cabeçalho com o mesmo design do dock e corpo claro 3D
        display.DrawWindowFrameLight(X, Y, Width, Height, TitleBarHeight, 10, IsActive);

        // 2. Botões circulares (Traffic Lights) ampliados para Full HD no canto superior esquerdo
        display.DrawTrafficLight(X + 18, Y + 19, 7, Theme.CloseButton, Theme.CloseButtonBorder, false, 'x');
        display.DrawTrafficLight(X + 42, Y + 19, 7, Theme.MinimizeButton, Theme.MinimizeButtonBorder, false, '-');
        display.DrawTrafficLight(X + 66, Y + 19, 7, Theme.MaximizeButton, Theme.MaximizeButtonBorder, false, '+');

        // 4. Título da Janela centralizado
        int textW = Title.Length * CosmosNativeFont.Width;
        int titleX = X + Math.Max(84, (Width - textW) / 2);
        int titleY = Y + (TitleBarHeight - CosmosNativeFont.Height) / 2;

        int maxChars = Math.Max(0, (Width - 160) / CosmosNativeFont.Width);
        string displayTitle = Title;
        if (displayTitle.Length > maxChars && maxChars > 3)
        {
            displayTitle = displayTitle.Substring(0, maxChars - 2) + "..";
            titleX = X + 84;
        }

        display.DrawString(titleX, titleY, displayTitle, IsActive ? Theme.TextPrimary : Theme.TextSecondary);

        // 5. Conteúdo interno do aplicativo
        DrawContent(display);
    }

    public abstract void DrawContent(DisplayDriver display);
    public virtual void OnMouseDown(int mouseX, int mouseY) { }
    public virtual void OnMouseUp(int mouseX, int mouseY) { }
    public virtual void OnMouseMove(int mouseX, int mouseY) { }
    public virtual void OnKeyDown(KeyEvent key) { }
}
