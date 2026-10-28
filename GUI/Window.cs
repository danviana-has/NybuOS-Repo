using System;
using System.Drawing;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;

namespace NybuOS.GUI;

/// <summary>
/// Janela base moderna com Dark Glass estilizado, cantos retos,
/// preenchimento opaco de janela para evitar sobreposição fantasma
/// e cabeçalho limpo com espaçamento correto de botões.
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

    public void Close()
    {
        IsClosed = true;
    }

    public const int TitleBarHeight = 28;
    public const int BorderWidth = 1;

    public int ClientX => X + BorderWidth;
    public int ClientY => Y + TitleBarHeight;
    public int ClientWidth => Width - (BorderWidth * 2);
    public int ClientHeight => Height - TitleBarHeight - BorderWidth;

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
            int btnCloseX = X + Width - 28;
            int btnMinX = X + Width - 56;
            if (mx >= btnMinX && mx <= X + Width)
            {
                return false;
            }
            return true;
        }
        return false;
    }

    public bool IsCloseButtonClicked(int mx, int my)
    {
        int btnX = X + Width - 28;
        int btnY = Y + 2;
        return mx >= btnX && mx <= btnX + 24 && my >= btnY && my <= btnY + 24;
    }

    public bool IsMinimizeButtonClicked(int mx, int my)
    {
        int btnX = X + Width - 56;
        int btnY = Y + 2;
        return mx >= btnX && mx <= btnX + 24 && my >= btnY && my <= btnY + 24;
    }

    public bool ContainsPoint(int px, int py)
    {
        if (IsMinimized || IsClosed) return false;
        return px >= X && px <= X + Width && py >= Y && py <= Y + Height;
    }

    public virtual void Draw(DisplayDriver display)
    {
        if (IsMinimized || IsClosed) return;

        // 1. Corpo da janela: Translúcido Dark Glass com requício preto
        display.DrawTranslucentRectangle(X, Y, Width, Height, Theme.WindowBg);

        // 2. Borda minimalista reta na cor de destaque
        Color border = IsActive ? Theme.WindowBorder : Theme.WindowBorderInactive;
        display.DrawRectangle(X, Y, Width, Height, border);

        // 3. Cabeçalho com transparência
        display.DrawFilledRectangle(X + 1, Y + 1, Width - 2, TitleBarHeight - 1, Theme.WindowHeaderBg);
        display.DrawLine(X + 1, Y + TitleBarHeight, X + Width - 2, Y + TitleBarHeight, Theme.WindowBorderInactive);

        // Ícone e Título da Janela
        int textOffset = X + 10;
        if (Icon != null)
        {
            display.DrawIcon(X + 8, Y + (TitleBarHeight - Icon.Height) / 2, Icon);
            textOffset = X + 8 + Icon.Width + 8;
        }

        int maxTitleChars = Math.Max(0, (Width - (textOffset - X) - 65) / 8);
        string displayTitle = Title;
        if (displayTitle.Length > maxTitleChars && maxTitleChars > 3)
        {
            displayTitle = displayTitle.Substring(0, maxTitleChars - 2) + "..";
        }
        display.DrawString(textOffset, Y + (TitleBarHeight - 14) / 2, displayTitle, IsActive ? Theme.TextPrimary : Theme.TextSecondary);

        // 4. Botão Minimizar [_]
        int minX = X + Width - 56;
        int minY = Y + 4;
        display.DrawFilledRectangle(minX, minY, 22, 20, Theme.WindowHeaderBg);
        display.DrawLine(minX + 6, minY + 14, minX + 16, minY + 14, Theme.TextSecondary);

        // 5. Botão Fechar [X]
        int closeX = X + Width - 28;
        int closeY = Y + 4;
        display.DrawFilledRectangle(closeX, closeY, 22, 20, Theme.WindowHeaderBg);
        display.DrawLine(closeX + 6, closeY + 5, closeX + 16, closeY + 15, Theme.TextSecondary);
        display.DrawLine(closeX + 16, closeY + 5, closeX + 6, closeY + 15, Theme.TextSecondary);

        // 6. Conteúdo interno do aplicativo
        DrawContent(display);
    }

    public abstract void DrawContent(DisplayDriver display);
    public virtual void OnMouseDown(int mouseX, int mouseY) { }
    public virtual void OnMouseUp(int mouseX, int mouseY) { }
    public virtual void OnMouseMove(int mouseX, int mouseY) { }
    public virtual void OnKeyDown(KeyEvent key) { }
}
