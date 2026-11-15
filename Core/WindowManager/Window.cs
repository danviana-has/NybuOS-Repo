using System;
using NybuOS.Apps;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Core.WindowManager;

public enum WindowState
{
    Normal,
    Maximized,
    Minimized
}

public class Window
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public int RestoreX { get; set; }
    public int RestoreY { get; set; }
    public int RestoreW { get; set; }
    public int RestoreH { get; set; }

    public WindowState State { get; set; } = WindowState.Normal;
    public bool IsActive { get; set; } = true;

    public IApp App { get; }
    public string Title => App.Title;
    public FastBitmap? Icon16 => App.Icon16;

    public const int TitlebarHeight = 32;
    public const int ButtonWidth = 46;

    private bool _isDragging = false;
    private int _dragOffsetX = 0;
    private int _dragOffsetY = 0;

    public Window(IApp app, int x, int y, int width, int height)
    {
        App = app;
        X = x;
        Y = y;
        Width = width;
        Height = height;

        RestoreX = x;
        RestoreY = y;
        RestoreW = width;
        RestoreH = height;

        app.Initialize(this);
    }

    public int ClientX => X;
    public int ClientY => Y + TitlebarHeight;
    public int ClientWidth => Width;
    public int ClientHeight => Math.Max(0, Height - TitlebarHeight);

    public void Maximize(int screenW, int screenH, int taskbarH)
    {
        if (State == WindowState.Maximized)
        {
            // Restore
            State = WindowState.Normal;
            X = RestoreX;
            Y = RestoreY;
            Width = RestoreW;
            Height = RestoreH;
        }
        else
        {
            // Maximize
            RestoreX = X;
            RestoreY = Y;
            RestoreW = Width;
            RestoreH = Height;

            State = WindowState.Maximized;
            X = 0;
            Y = 0;
            Width = screenW;
            Height = screenH - taskbarH;
        }

        App.OnResize(ClientWidth, ClientHeight);
    }

    public void Minimize()
    {
        State = WindowState.Minimized;
        IsActive = false;
    }

    public void Restore()
    {
        State = WindowState.Normal;
        IsActive = true;
    }

    public void Render(uint[] buffer, int screenW, int screenH)
    {
        if (State == WindowState.Minimized) return;

        // 1. Draw Soft Shadow if in Normal state
        if (State == WindowState.Normal)
        {
            FastRenderer.DrawShadow(buffer, screenW, screenH, X, Y, Width, Height, 5);
        }

        // 2. Draw Window Body
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, X, Y, Width, Height, ColorScheme.WindowBody);

        // 3. Draw Window Border (1px)
        uint borderColor = IsActive ? ColorScheme.Accent : ColorScheme.WindowBorder;
        FastRenderer.DrawRectangle(buffer, screenW, screenH, X, Y, Width, Height, borderColor);

        // 4. Draw Titlebar
        uint tbColor = IsActive ? ColorScheme.WindowTitlebar : ColorScheme.WindowTitlebarInactive;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, X + 1, Y + 1, Width - 2, TitlebarHeight - 1, tbColor);
        // Subtle divider between titlebar and client area
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, X, Y + TitlebarHeight - 1, Width, ColorScheme.WindowBorder);

        // Titlebar Icon (16x16)
        if (Icon16 != null)
        {
            FastRenderer.DrawBitmap(buffer, screenW, screenH, Icon16, X + 8, Y + 8);
        }

        // Titlebar Text (Bold Segoe UI)
        int titleX = X + (Icon16 != null ? 30 : 10);
        FontRenderer.DrawText(buffer, screenW, screenH, Title, titleX, Y + 8, ColorScheme.TextPrimary, bold: true, size: 14);

        // Titlebar Buttons (Minimize, Maximize, Close)
        int btnY = Y + 1;
        int btnH = TitlebarHeight - 2;

        int closeX = X + Width - ButtonWidth - 1;
        int maxBtnX = closeX - ButtonWidth;
        int minBtnX = maxBtnX - ButtonWidth;

        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        // Close Button
        bool closeHover = mx >= closeX && mx < closeX + ButtonWidth && my >= btnY && my < btnY + btnH;
        if (closeHover)
        {
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, closeX, btnY, ButtonWidth, btnH, ColorScheme.CloseButtonHover);
            FontRenderer.DrawText(buffer, screenW, screenH, "X", closeX + 18, btnY + 8, ColorScheme.TextWhite, bold: false, size: 14);
        }
        else
        {
            FontRenderer.DrawText(buffer, screenW, screenH, "X", closeX + 18, btnY + 8, ColorScheme.TextPrimary, bold: false, size: 14);
        }

        // Maximize Button
        bool maxHover = mx >= maxBtnX && mx < maxBtnX + ButtonWidth && my >= btnY && my < btnY + btnH;
        if (maxHover)
        {
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, maxBtnX, btnY, ButtonWidth, btnH, ColorScheme.ControlHover);
        }
        string maxSymbol = State == WindowState.Maximized ? "❐" : "□";
        FontRenderer.DrawText(buffer, screenW, screenH, maxSymbol, maxBtnX + 18, btnY + 8, ColorScheme.TextPrimary, bold: false, size: 14);

        // Minimize Button
        bool minHover = mx >= minBtnX && mx < minBtnX + ButtonWidth && my >= btnY && my < btnY + btnH;
        if (minHover)
        {
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, minBtnX, btnY, ButtonWidth, btnH, ColorScheme.ControlHover);
        }
        FontRenderer.DrawText(buffer, screenW, screenH, "—", minBtnX + 18, btnY + 8, ColorScheme.TextPrimary, bold: false, size: 14);

        // 5. Render Client App Area
        App.Render(buffer, screenW, screenH, ClientX, ClientY, ClientWidth, ClientHeight);
    }

    public bool HandleInput(int screenW, int screenH, int taskbarH, out bool requestClose)
    {
        requestClose = false;
        if (State == WindowState.Minimized) return false;

        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        // Check titlebar interaction
        if (my >= Y && my < Y + TitlebarHeight && mx >= X && mx < X + Width)
        {
            int closeX = X + Width - ButtonWidth - 1;
            int maxBtnX = closeX - ButtonWidth;
            int minBtnX = maxBtnX - ButtonWidth;

            if (InputState.LeftClicked)
            {
                if (mx >= closeX && mx < closeX + ButtonWidth)
                {
                    requestClose = true;
                    App.OnClose();
                    return true;
                }
                if (mx >= maxBtnX && mx < maxBtnX + ButtonWidth)
                {
                    Maximize(screenW, screenH, taskbarH);
                    return true;
                }
                if (mx >= minBtnX && mx < minBtnX + ButtonWidth)
                {
                    Minimize();
                    return true;
                }

                // Start Dragging if not maximized
                if (State != WindowState.Maximized)
                {
                    _isDragging = true;
                    _dragOffsetX = mx - X;
                    _dragOffsetY = my - Y;
                    return true;
                }
            }
        }

        // Handle Dragging
        if (_isDragging)
        {
            if (InputState.LeftDown)
            {
                X = Math.Clamp(mx - _dragOffsetX, 0, screenW - Width);
                Y = Math.Clamp(my - _dragOffsetY, 0, screenH - taskbarH - TitlebarHeight);
            }
            else
            {
                _isDragging = false;
            }
            return true;
        }

        // Delegate input to App
        if (mx >= ClientX && mx < ClientX + ClientWidth && my >= ClientY && my < ClientY + ClientHeight)
        {
            App.HandleInput(ClientX, ClientY, ClientWidth, ClientHeight);
            return true;
        }

        return false;
    }
}
