using System;
using System.Collections.Generic;
using Cosmos.Kernel.System;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Core.WindowManager;

public class StartMenuItem
{
    public string AppId { get; set; } = "";
    public string Name { get; set; } = "";
    public string GroupLetter { get; set; } = "";
    public FastBitmap? Icon24 { get; set; }
}

public class LiveTile
{
    public string AppId { get; set; } = "";
    public string Title { get; set; } = "";
    public int RelX { get; set; }
    public int RelY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public uint BgColor { get; set; }
    public FastBitmap? Icon { get; set; }
}

public class StartMenu
{
    public const int MenuWidth = 600;
    public const int MenuHeight = 520;
    public const int Col1Width = 48;
    public const int Col2Width = 220;
    public const int Col3Width = 332;

    private readonly List<StartMenuItem> _appList = new();
    private readonly List<LiveTile> _liveTiles = new();
    private int _col2ScrollOffset = 0;

    public StartMenu()
    {
        BuildAppList();
        BuildLiveTiles();
    }

    private void BuildAppList()
    {
        _appList.Clear();
        // A
        _appList.Add(new StartMenuItem { AppId = "about", Name = "AboutApp", GroupLetter = "A", Icon24 = AssetManager.IconAboutApp24 });
        _appList.Add(new StartMenuItem { AppId = "help", Name = "Obter Ajuda", GroupLetter = "A", Icon24 = AssetManager.IconHelp24 });
        // B
        _appList.Add(new StartMenuItem { AppId = "notepad", Name = "Bloco de Notas", GroupLetter = "B", Icon24 = AssetManager.IconNotepad24 });
        // C
        _appList.Add(new StartMenuItem { AppId = "calc", Name = "Calculadora", GroupLetter = "C", Icon24 = AssetManager.IconCalc24 });
        _appList.Add(new StartMenuItem { AppId = "calendar", Name = "Calendário", GroupLetter = "C", Icon24 = AssetManager.IconCalendar24 });
        _appList.Add(new StartMenuItem { AppId = "minesweeper", Name = "CentralLogic", GroupLetter = "C", Icon24 = AssetManager.IconCentralLogic24 });
        _appList.Add(new StartMenuItem { AppId = "settings", Name = "Configurações", GroupLetter = "C", Icon24 = AssetManager.IconSettings24 });
        // G
        _appList.Add(new StartMenuItem { AppId = "files", Name = "Gerenciador de Arquivos", GroupLetter = "G", Icon24 = AssetManager.IconFiles24 });
        // P
        _appList.Add(new StartMenuItem { AppId = "paint", Name = "PaintPad", GroupLetter = "P", Icon24 = AssetManager.IconPaint24 });
        _appList.Add(new StartMenuItem { AppId = "sheets", Name = "Planilha", GroupLetter = "P", Icon24 = AssetManager.IconSheets24 });
        // S
        _appList.Add(new StartMenuItem { AppId = "health", Name = "Saúde do Sistema", GroupLetter = "S", Icon24 = AssetManager.IconHealth24 });
        // T
        _appList.Add(new StartMenuItem { AppId = "terminal", Name = "Terminal", GroupLetter = "T", Icon24 = AssetManager.IconTerminal24 });
    }

    private void BuildLiveTiles()
    {
        _liveTiles.Clear();

        // Section 1: Produtividade (y: 35)
        _liveTiles.Add(new LiveTile { AppId = "notepad", Title = "Bloco de Notas", RelX = 14, RelY = 35, Width = 196, Height = 94, BgColor = ColorScheme.TileBlue, Icon = AssetManager.IconNotepad48 });
        _liveTiles.Add(new LiveTile { AppId = "sheets", Title = "Planilha", RelX = 218, RelY = 35, Width = 94, Height = 94, BgColor = ColorScheme.TileGreen, Icon = AssetManager.IconSheets48 });

        _liveTiles.Add(new LiveTile { AppId = "calendar", Title = "Calendário", RelX = 14, RelY = 137, Width = 94, Height = 94, BgColor = ColorScheme.TileOrange, Icon = AssetManager.IconCalendar48 });
        _liveTiles.Add(new LiveTile { AppId = "calc", Title = "Calculadora", RelX = 116, RelY = 137, Width = 94, Height = 94, BgColor = ColorScheme.TilePurple, Icon = AssetManager.IconCalc48 });
        _liveTiles.Add(new LiveTile { AppId = "files", Title = "Arquivos", RelX = 218, RelY = 137, Width = 94, Height = 94, BgColor = ColorScheme.TileCyan, Icon = AssetManager.IconFiles48 });

        // Section 2: Ferramentas & Sistema (y: 260)
        _liveTiles.Add(new LiveTile { AppId = "terminal", Title = "Terminal", RelX = 14, RelY = 265, Width = 196, Height = 94, BgColor = ColorScheme.TileSlate, Icon = AssetManager.IconTerminal48 });
        _liveTiles.Add(new LiveTile { AppId = "minesweeper", Title = "CentralLogic", RelX = 218, RelY = 265, Width = 94, Height = 94, BgColor = ColorScheme.TileDarkBlue, Icon = AssetManager.IconCentralLogic48 });

        _liveTiles.Add(new LiveTile { AppId = "health", Title = "Saúde do Sistema", RelX = 14, RelY = 367, Width = 196, Height = 94, BgColor = ColorScheme.TileTeal, Icon = AssetManager.IconHealth48 });
        _liveTiles.Add(new LiveTile { AppId = "settings", Title = "Configurações", RelX = 218, RelY = 367, Width = 94, Height = 94, BgColor = ColorScheme.TileRed, Icon = AssetManager.IconSettings48 });
    }

    public void Render(uint[] buffer, int screenW, int screenH)
    {
        int menuX = 0;
        int menuY = screenH - Taskbar.TaskbarHeight - MenuHeight;

        // Shadow around Start Menu
        FastRenderer.DrawShadow(buffer, screenW, screenH, menuX, menuY, MenuWidth, MenuHeight, 8);

        // Solid White Background
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, menuX, menuY, MenuWidth, MenuHeight, ColorScheme.WindowBody);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, menuX, menuY, MenuWidth, MenuHeight, ColorScheme.WindowBorder);

        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        // ==========================================
        // 1. COLUMN 1: Narrow Lateral Actions Bar
        // ==========================================
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, menuX, menuY, Col1Width, MenuHeight, ColorScheme.BackgroundLight);
        FastRenderer.DrawVerticalLine(buffer, screenW, screenH, menuX + Col1Width - 1, menuY, MenuHeight, ColorScheme.WindowBorder);

        // Top Hamburger Button
        bool hamHover = mx >= menuX && mx < menuX + Col1Width && my >= menuY + 6 && my < menuY + 42;
        if (hamHover) FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, menuX, menuY + 6, Col1Width, 36, ColorScheme.ControlHover);
        FontRenderer.DrawText(buffer, screenW, screenH, "☰", menuX + 16, menuY + 16, ColorScheme.TextPrimary, bold: false, size: 14);

        // Bottom Actions: User, Docs, Settings, Power
        int actionBtnY = menuY + MenuHeight - 160;

        // User Action
        bool userHover = mx >= menuX && mx < menuX + Col1Width && my >= actionBtnY && my < actionBtnY + 36;
        if (userHover) FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, menuX, actionBtnY, Col1Width, 36, ColorScheme.ControlHover);
        FontRenderer.DrawText(buffer, screenW, screenH, "👤", menuX + 15, actionBtnY + 10, ColorScheme.TextPrimary, bold: false, size: 14);
        actionBtnY += 38;

        // Documents Action
        bool docHover = mx >= menuX && mx < menuX + Col1Width && my >= actionBtnY && my < actionBtnY + 36;
        if (docHover) FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, menuX, actionBtnY, Col1Width, 36, ColorScheme.ControlHover);
        FontRenderer.DrawText(buffer, screenW, screenH, "📁", menuX + 15, actionBtnY + 10, ColorScheme.TextPrimary, bold: false, size: 14);
        actionBtnY += 38;

        // Settings Action
        bool setHover = mx >= menuX && mx < menuX + Col1Width && my >= actionBtnY && my < actionBtnY + 36;
        if (setHover) FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, menuX, actionBtnY, Col1Width, 36, ColorScheme.ControlHover);
        FontRenderer.DrawText(buffer, screenW, screenH, "⚙️", menuX + 15, actionBtnY + 10, ColorScheme.TextPrimary, bold: false, size: 14);
        actionBtnY += 38;

        // Power Action
        bool pwrHover = mx >= menuX && mx < menuX + Col1Width && my >= actionBtnY && my < actionBtnY + 36;
        if (pwrHover) FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, menuX, actionBtnY, Col1Width, 36, ColorScheme.CloseButtonHover);
        FontRenderer.DrawText(buffer, screenW, screenH, "⏻", menuX + 15, actionBtnY + 10, pwrHover ? ColorScheme.TextWhite : ColorScheme.TextPrimary, bold: false, size: 14);

        // ==========================================
        // 2. COLUMN 2: Alphabetical Apps List
        // ==========================================
        int col2X = menuX + Col1Width;
        FastRenderer.DrawVerticalLine(buffer, screenW, screenH, col2X + Col2Width - 1, menuY, MenuHeight, ColorScheme.WindowBorder);

        int itemY = menuY + 12 - _col2ScrollOffset;
        string lastLetter = "";

        for (int i = 0; i < _appList.Count; i++)
        {
            var item = _appList[i];

            // Render Letter Header if new group
            if (item.GroupLetter != lastLetter)
            {
                lastLetter = item.GroupLetter;
                if (itemY >= menuY && itemY < menuY + MenuHeight - 20)
                {
                    FontRenderer.DrawText(buffer, screenW, screenH, lastLetter, col2X + 14, itemY + 2, ColorScheme.Accent, bold: true, size: 14);
                }
                itemY += 24;
            }

            // Render App List Row
            if (itemY >= menuY && itemY < menuY + MenuHeight - 34)
            {
                bool itemHover = mx >= col2X && mx < col2X + Col2Width - 2 && my >= itemY && my < itemY + 32;
                if (itemHover)
                {
                    FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, col2X + 2, itemY, Col2Width - 4, 32, ColorScheme.ControlBackground);
                }

                // Small colored square with icon
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, col2X + 10, itemY + 4, 24, 24, ColorScheme.ControlHover);
                if (item.Icon24 != null)
                {
                    FastRenderer.DrawBitmap(buffer, screenW, screenH, item.Icon24, col2X + 10, itemY + 4);
                }

                // App Name
                string displayName = item.Name;
                if (displayName.Length > 16) displayName = displayName.Substring(0, 14) + "...";
                FontRenderer.DrawText(buffer, screenW, screenH, displayName, col2X + 42, itemY + 8, ColorScheme.TextPrimary, bold: false, size: 14);
            }

            itemY += 34;
        }

        // ==========================================
        // 3. COLUMN 3: Live Tiles Grid
        // ==========================================
        int col3X = col2X + Col2Width;

        // Section Headers (Segoe UI Bold)
        FontRenderer.DrawText(buffer, screenW, screenH, "Produtividade", col3X + 14, menuY + 12, ColorScheme.TextSecondary, bold: true, size: 14);
        FontRenderer.DrawText(buffer, screenW, screenH, "Explorar e Ferramentas", col3X + 14, menuY + 242, ColorScheme.TextSecondary, bold: true, size: 14);

        for (int i = 0; i < _liveTiles.Count; i++)
        {
            var tile = _liveTiles[i];
            int tx = col3X + tile.RelX;
            int ty = menuY + tile.RelY;

            bool tileHover = mx >= tx && mx < tx + tile.Width && my >= ty && my < ty + tile.Height;

            // Draw Tile Body
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, tx, ty, tile.Width, tile.Height, tile.BgColor);

            if (tileHover)
            {
                // Hover highlight border
                FastRenderer.DrawRectangle(buffer, screenW, screenH, tx, ty, tile.Width, tile.Height, ColorScheme.White);
            }

            // Centered Tile Icon (48x48)
            if (tile.Icon != null)
            {
                int iconX = tx + (tile.Width - tile.Icon.Width) / 2;
                int iconY = ty + (tile.Height - tile.Icon.Height) / 2 - 6;
                FastRenderer.DrawBitmap(buffer, screenW, screenH, tile.Icon, iconX, iconY);
            }

            // Tile Label (Bottom Left, Segoe UI)
            FontRenderer.DrawText(buffer, screenW, screenH, tile.Title, tx + 6, ty + tile.Height - 18, ColorScheme.TextWhite, bold: false, size: 12);
        }
    }

    public bool HandleInput(int screenW, int screenH, Action<string> onLaunchApp, Action onCloseMenu)
    {
        int menuX = 0;
        int menuY = screenH - Taskbar.TaskbarHeight - MenuHeight;
        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        // Check if click was outside the Start Menu
        if (InputState.LeftClicked)
        {
            if (mx < menuX || mx >= menuX + MenuWidth || my < menuY || my >= screenH - Taskbar.TaskbarHeight)
            {
                onCloseMenu();
                return false;
            }

            // 1. Check Column 1 Quick Actions
            if (mx >= menuX && mx < menuX + Col1Width)
            {
                int actionBtnY = menuY + MenuHeight - 160;
                // User
                if (my >= actionBtnY && my < actionBtnY + 36)
                {
                    onLaunchApp("about");
                    onCloseMenu();
                    return true;
                }
                actionBtnY += 38;
                // Documents
                if (my >= actionBtnY && my < actionBtnY + 36)
                {
                    onLaunchApp("files");
                    onCloseMenu();
                    return true;
                }
                actionBtnY += 38;
                // Settings
                if (my >= actionBtnY && my < actionBtnY + 36)
                {
                    onLaunchApp("settings");
                    onCloseMenu();
                    return true;
                }
                actionBtnY += 38;
                // Power
                if (my >= actionBtnY && my < actionBtnY + 36)
                {
                    Power.Shutdown();
                    return true;
                }
            }

            // 2. Check Column 2 App List
            int col2X = menuX + Col1Width;
            if (mx >= col2X && mx < col2X + Col2Width)
            {
                int itemY = menuY + 12 - _col2ScrollOffset;
                string lastLetter = "";

                for (int i = 0; i < _appList.Count; i++)
                {
                    var item = _appList[i];
                    if (item.GroupLetter != lastLetter)
                    {
                        lastLetter = item.GroupLetter;
                        itemY += 24;
                    }

                    if (my >= itemY && my < itemY + 32)
                    {
                        onLaunchApp(item.AppId);
                        onCloseMenu();
                        return true;
                    }

                    itemY += 34;
                }
            }

            // 3. Check Column 3 Live Tiles
            int col3X = col2X + Col2Width;
            if (mx >= col3X && mx < col3X + Col3Width)
            {
                for (int i = 0; i < _liveTiles.Count; i++)
                {
                    var tile = _liveTiles[i];
                    int tx = col3X + tile.RelX;
                    int ty = menuY + tile.RelY;

                    if (mx >= tx && mx < tx + tile.Width && my >= ty && my < ty + tile.Height)
                    {
                        onLaunchApp(tile.AppId);
                        onCloseMenu();
                        return true;
                    }
                }
            }
        }

        // Scroll Column 2 with mouse wheel
        if (InputState.ScrollDelta != 0)
        {
            _col2ScrollOffset = Math.Clamp(_col2ScrollOffset - (InputState.ScrollDelta * 20), 0, 100);
        }

        return true;
    }
}
