using System;
using System.Collections.Generic;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Core.WindowManager;

public class Taskbar
{
    public const int TaskbarHeight = 40;
    public const int StartButtonWidth = 48;
    public const int WindowTabWidth = 160;

    public bool IsStartMenuOpen { get; set; } = false;

    public void Render(uint[] buffer, int screenW, int screenH, List<Window> windows, Window? activeWindow)
    {
        int ty = screenH - TaskbarHeight;

        // 1. Solid White Background
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, 0, ty, screenW, TaskbarHeight, ColorScheme.TaskbarBackground);

        // 2. Subtle Top Divider Line
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, 0, ty, screenW, ColorScheme.TaskbarBorder);

        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        // 3. Start Button (NO text, only OS Icon centered)
        bool startHover = mx >= 0 && mx < StartButtonWidth && my >= ty && my < screenH;
        if (startHover || IsStartMenuOpen)
        {
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, 0, ty + 1, StartButtonWidth, TaskbarHeight - 1, ColorScheme.TaskbarHover);
        }

        if (AssetManager.IconOsLogoTaskbar != null)
        {
            int iconX = (StartButtonWidth - AssetManager.IconOsLogoTaskbar.Width) / 2;
            int iconY = ty + (TaskbarHeight - AssetManager.IconOsLogoTaskbar.Height) / 2;
            FastRenderer.DrawBitmap(buffer, screenW, screenH, AssetManager.IconOsLogoTaskbar, iconX, iconY);
        }

        // 4. Open Application Tabs
        int tabX = StartButtonWidth + 6;
        for (int i = 0; i < windows.Count; i++)
        {
            var win = windows[i];
            bool isTabHover = mx >= tabX && mx < tabX + WindowTabWidth && my >= ty && my < screenH;
            bool isActive = (win == activeWindow && win.State != WindowState.Minimized);

            if (isActive)
            {
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, tabX, ty + 1, WindowTabWidth, TaskbarHeight - 1, ColorScheme.TaskbarHover);
                // Active window underline indicator (Accent color, 2px)
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, tabX + 4, screenH - 3, WindowTabWidth - 8, 2, ColorScheme.Accent);
            }
            else if (isTabHover)
            {
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, tabX, ty + 1, WindowTabWidth, TaskbarHeight - 1, ColorScheme.ControlBackground);
            }

            // App Icon 24x24
            if (win.App.Icon24 != null)
            {
                FastRenderer.DrawBitmap(buffer, screenW, screenH, win.App.Icon24, tabX + 8, ty + 8);
            }

            // App Title (Regular Segoe UI)
            string tabTitle = win.Title;
            if (tabTitle.Length > 14) tabTitle = tabTitle.Substring(0, 12) + "...";
            FontRenderer.DrawText(buffer, screenW, screenH, tabTitle, tabX + 38, ty + 12, ColorScheme.TextPrimary, bold: false, size: 14);

            tabX += WindowTabWidth + 4;
            if (tabX + WindowTabWidth > screenW - 140) break; // Leave room for tray
        }

        // 5. System Tray (Clock & Indicators)
        int trayX = screenW - 130;

        // Quick indicators (Health shield & Volume)
        if (AssetManager.IconHealth16 != null)
        {
            FastRenderer.DrawBitmap(buffer, screenW, screenH, AssetManager.IconHealth16, trayX, ty + 12);
        }
        trayX += 24;

        // Volume Indicator
        FontRenderer.DrawText(buffer, screenW, screenH, "🔊", trayX, ty + 12, ColorScheme.TextSecondary, bold: false, size: 14);
        trayX += 28;

        // Digital Clock (HH:MM)
        string timeStr = DateTime.Now.ToString("HH:mm");
        FontRenderer.DrawText(buffer, screenW, screenH, timeStr, trayX, ty + 12, ColorScheme.TextPrimary, bold: true, size: 14);
    }

    public bool HandleInput(int screenW, int screenH, List<Window> windows, Action<Window> onActivateWindow)
    {
        int ty = screenH - TaskbarHeight;
        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        if (my < ty) return false;

        if (InputState.LeftClicked)
        {
            // Clicked Start Button
            if (mx >= 0 && mx < StartButtonWidth)
            {
                IsStartMenuOpen = !IsStartMenuOpen;
                return true;
            }

            // Clicked an App Tab
            int tabX = StartButtonWidth + 6;
            for (int i = 0; i < windows.Count; i++)
            {
                if (mx >= tabX && mx < tabX + WindowTabWidth)
                {
                    var win = windows[i];
                    if (win.State == WindowState.Minimized)
                    {
                        win.Restore();
                        onActivateWindow(win);
                    }
                    else if (win.IsActive)
                    {
                        win.Minimize();
                    }
                    else
                    {
                        win.Restore();
                        onActivateWindow(win);
                    }
                    return true;
                }
                tabX += WindowTabWidth + 4;
            }
        }

        return true;
    }
}
