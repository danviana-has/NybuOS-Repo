using System;
using System.Collections.Generic;
using NybuOS.Apps;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Core.WindowManager;

public class WindowManager
{
    public int ScreenWidth { get; private set; }
    public int ScreenHeight { get; private set; }

    private readonly List<Window> _windows = new();
    private Window? _activeWindow = null;

    private readonly Taskbar _taskbar = new();
    private readonly StartMenu _startMenu = new();
    private readonly uint[] _wallpaperCache;

    public WindowManager(int screenWidth, int screenHeight)
    {
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;

        // Initialize persistent wallpaper buffer
        _wallpaperCache = new uint[screenWidth * screenHeight];
        if (AssetManager.Wallpaper != null)
        {
            Array.Copy(AssetManager.Wallpaper.Pixels, _wallpaperCache, Math.Min(AssetManager.Wallpaper.Pixels.Length, _wallpaperCache.Length));
        }
        else
        {
            Array.Fill(_wallpaperCache, ColorScheme.BackgroundLight);
        }
    }

    public void AddWindow(Window window)
    {
        _windows.Add(window);
        SetActiveWindow(window);
    }

    public void SetActiveWindow(Window window)
    {
        if (_activeWindow != null) _activeWindow.IsActive = false;
        _activeWindow = window;
        if (window != null)
        {
            window.IsActive = true;
            // Bring to top of z-order
            _windows.Remove(window);
            _windows.Add(window);
        }
    }

    public void LaunchApp(IApp app, int defaultW = 640, int defaultH = 460)
    {
        // Check if already open
        for (int i = 0; i < _windows.Count; i++)
        {
            if (_windows[i].App.Id == app.Id)
            {
                var existing = _windows[i];
                if (existing.State == WindowState.Minimized) existing.Restore();
                SetActiveWindow(existing);
                return;
            }
        }

        // Stagger initial window position
        int offsetX = 40 + ((_windows.Count % 6) * 30);
        int offsetY = 40 + ((_windows.Count % 6) * 30);
        int w = Math.Min(defaultW, ScreenWidth - 60);
        int h = Math.Min(defaultH, ScreenHeight - Taskbar.TaskbarHeight - 60);

        var newWin = new Window(app, offsetX, offsetY, w, h);
        AddWindow(newWin);
    }

    public void CloseWindow(Window window)
    {
        _windows.Remove(window);
        if (_activeWindow == window)
        {
            _activeWindow = _windows.Count > 0 ? _windows[_windows.Count - 1] : null;
            if (_activeWindow != null) _activeWindow.IsActive = true;
        }
    }

    public void CloseApp(string appId)
    {
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            if (_windows[i].App.Id.Equals(appId, StringComparison.OrdinalIgnoreCase))
            {
                CloseWindow(_windows[i]);
            }
        }
    }

    public void Update()
    {
        InputState.Update();

        // 1. Handle Start Menu
        if (_taskbar.IsStartMenuOpen)
        {
            _startMenu.HandleInput(ScreenWidth, ScreenHeight,
                onLaunchApp: (id) => AppRegistry.LaunchById(this, id),
                onCloseMenu: () => _taskbar.IsStartMenuOpen = false);
            return;
        }

        // 2. Handle Taskbar Input
        int ty = ScreenHeight - Taskbar.TaskbarHeight;
        if (InputState.MouseY >= ty)
        {
            _taskbar.HandleInput(ScreenWidth, ScreenHeight, _windows, SetActiveWindow);
            return;
        }

        // 3. Handle Windows Input (top to bottom)
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            var win = _windows[i];
            if (win.State == WindowState.Minimized) continue;

            bool handled = win.HandleInput(ScreenWidth, ScreenHeight, Taskbar.TaskbarHeight, out bool requestClose);
            if (requestClose)
            {
                CloseWindow(win);
                return;
            }

            if (handled)
            {
                if (_activeWindow != win)
                {
                    SetActiveWindow(win);
                }
                return;
            }
        }
    }

    public void Render(uint[] backBuffer)
    {
        // 1. Double-Buffering: Instant blit of cached wallpaper (0 memory allocation)
        Array.Copy(_wallpaperCache, backBuffer, backBuffer.Length);

        // 2. Render Windows (bottom to top)
        for (int i = 0; i < _windows.Count; i++)
        {
            _windows[i].Render(backBuffer, ScreenWidth, ScreenHeight);
        }

        // 3. Render Taskbar
        _taskbar.Render(backBuffer, ScreenWidth, ScreenHeight, _windows, _activeWindow);

        // 4. Render Start Menu (if open)
        if (_taskbar.IsStartMenuOpen)
        {
            _startMenu.Render(backBuffer, ScreenWidth, ScreenHeight);
        }

        // 5. Render Mouse Cursor
        FastRenderer.DrawMouseCursor(backBuffer, ScreenWidth, ScreenHeight, InputState.MouseX, InputState.MouseY);

        // 6. Apply Brightness Adjustment
        if (ColorScheme.BrightnessPercent < 100)
        {
            FastRenderer.ApplyBrightness(backBuffer, ColorScheme.BrightnessPercent);
        }
    }
}
