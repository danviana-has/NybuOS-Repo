using System;
using System.Collections.Generic;
using System.Drawing;
using NybuOS.Drivers;

namespace NybuOS.GUI;

public enum StartMenuItem
{
    None,
    FileManager,
    TextEditor,
    PaintPad,
    MediaReproduction,
    ControlPanel,
    CentralLogic,
    Spreadsheet,
    Calculator,
    Terminal,
    Installer,
    About,
    Shutdown
}

/// <summary>
/// Barra de tarefas moderna, escura e translúcida (estilo COSMIC / Flat UI)
/// </summary>
public class Taskbar
{
    public const int Height = 36;
    private readonly WindowManager _windowManager;

    public bool IsStartMenuOpen { get; set; }

    public event Action<StartMenuItem> OnMenuItemSelected;

    private readonly (string Title, StartMenuItem Item, Func<IconImage> Icon)[] _menuItems = new (string Title, StartMenuItem Item, Func<IconImage> Icon)[]
    {
        ("Arquivos", StartMenuItem.FileManager, () => IconResources.FileManagerIcon),
        ("Editor de Texto", StartMenuItem.TextEditor, () => IconResources.TextEditorIcon),
        ("PaintPad", StartMenuItem.PaintPad, () => IconResources.PaintPadIcon),
        ("Reproducao de Midia", StartMenuItem.MediaReproduction, () => IconResources.MediaIcon),
        ("Painel de Controle", StartMenuItem.ControlPanel, () => IconResources.ControlPanelIcon),
        ("CentralLogic (Minas)", StartMenuItem.CentralLogic, () => IconResources.CentralLogicIcon),
        ("Planilha", StartMenuItem.Spreadsheet, () => IconResources.SpreadsheetIcon),
        ("Calculadora", StartMenuItem.Calculator, () => IconResources.CalculatorIcon),
        ("Terminal", StartMenuItem.Terminal, () => IconResources.TerminalIcon),
        ("Instalar no HD", StartMenuItem.Installer, () => IconResources.InstallerIcon),
        ("Sobre o NybuOS", StartMenuItem.About, () => IconResources.AboutIcon),
        ("Desligar", StartMenuItem.Shutdown, () => IconResources.ShutdownIcon)
    };

    public Taskbar(WindowManager windowManager)
    {
        _windowManager = windowManager;
        IsStartMenuOpen = false;
    }

    public void Draw(DisplayDriver display, int mouseX = -1, int mouseY = -1)
    {
        int screenW = display.Width;
        int screenH = display.Height;
        int y = screenH - Height;

        display.DrawModernTaskbar(y, screenW, Height);

        int startBtnW = 38;
        int startBtnH = 30;
        int startBtnX = 6;
        int startBtnY = y + 3;

        bool isStartHover = mouseX >= startBtnX && mouseX <= startBtnX + startBtnW &&
                            mouseY >= startBtnY && mouseY <= startBtnY + startBtnH;

        display.DrawModernStartButton(startBtnX, startBtnY, startBtnW, startBtnH, IsStartMenuOpen, isStartHover);

        int currentX = startBtnX + startBtnW + 8;
        var windows = _windowManager.Windows;

        DateTime now = DateTime.Now;
        string timeStr = $"{now.Hour:D2}:{now.Minute:D2}";
        int clockW = 55;
        int clockX = screenW - clockW - 12;
        display.DrawString(clockX, y + 10, timeStr, Theme.TextPrimary);

        int maxAvailableW = clockX - currentX - 10;
        if (windows.Count > 0 && maxAvailableW > 60)
        {
            int btnW = Math.Min(160, maxAvailableW / windows.Count);
            for (int i = 0; i < windows.Count; i++)
            {
                var win = windows[i];
                if (win.IsClosed) continue;

                int bx = currentX + (i * (btnW + 4));
                int by = y + 4;
                int bh = Height - 8;

                bool isHover = mouseX >= bx && mouseX <= bx + btnW &&
                               mouseY >= by && mouseY <= by + bh;

                Color itemBg = win.IsActive ? Theme.AccentColor : (isHover ? Theme.SurfaceHover : Theme.ButtonBg);
                display.DrawFilledRectangle(bx, by, btnW, bh, itemBg);

                if (win.IsActive)
                {
                    display.DrawFilledRectangle(bx, by + bh - 2, btnW, 2, Color.White);
                }
                else if (isHover)
                {
                    display.DrawRectangle(bx, by, btnW, bh, Theme.ButtonBorder);
                }

                int iconOffset = bx + 6;
                if (win.Icon != null)
                {
                    display.DrawIcon(iconOffset, by + (bh - win.Icon.Height) / 2, win.Icon);
                    iconOffset += win.Icon.Width + 8;
                }

                string label = win.Title;
                int maxChars = Math.Max(0, (btnW - (iconOffset - bx) - 8) / 8);
                if (maxChars > 0 && label.Length > maxChars)
                {
                    label = label.Substring(0, Math.Max(0, maxChars - 2)) + "..";
                }

                display.DrawString(iconOffset, by + (bh - 16) / 2, label, Theme.TextPrimary);
            }
        }

        if (IsStartMenuOpen)
        {
            DrawStartMenu(display, y, mouseX, mouseY);
        }
    }

    private void DrawStartMenu(DisplayDriver display, int taskbarY, int mouseX = -1, int mouseY = -1)
    {
        int menuW = 250;
        int itemH = 34;
        int menuH = (_menuItems.Length * itemH) + 12;
        int menuX = 6;
        int menuY = taskbarY - menuH - 6;

        // Fundo com transparência Dark Glass
        display.DrawTranslucentRectangle(menuX, menuY, menuW, menuH, Theme.StartMenuBg);
        display.DrawRectangle(menuX, menuY, menuW, menuH, Theme.WindowBorderInactive);

        for (int i = 0; i < _menuItems.Length; i++)
        {
            int itemY = menuY + 6 + (i * itemH);
            var item = _menuItems[i];

            bool isItemHover = mouseX >= menuX + 4 && mouseX <= menuX + menuW - 4 &&
                               mouseY >= itemY && mouseY < itemY + itemH;

            if (isItemHover)
            {
                display.DrawFilledRectangle(menuX + 4, itemY, menuW - 8, itemH, Theme.SurfaceHover);
                display.DrawRectangle(menuX + 4, itemY, menuW - 8, itemH, Theme.AccentColor);
            }

            var icon = item.Icon?.Invoke();
            if (icon != null)
            {
                display.DrawIcon(menuX + 10, itemY + (itemH - icon.Height) / 2, icon);
            }

            Color textCol = (item.Item == StartMenuItem.Shutdown) ? Color.FromArgb(248, 113, 113) : (isItemHover ? Color.White : Theme.TextPrimary);
            display.DrawString(menuX + 44, itemY + (itemH - 14) / 2, item.Title, textCol);
        }
    }

    public bool HandleMouseDown(int mouseX, int mouseY)
    {
        int screenH = 600;
        int y = screenH - Height;

        int startBtnW = 38;
        int startBtnH = 30;
        int startBtnX = 6;
        int startBtnY = y + 3;

        if (mouseX >= startBtnX && mouseX <= startBtnX + startBtnW &&
            mouseY >= startBtnY && mouseY <= startBtnY + startBtnH)
        {
            IsStartMenuOpen = !IsStartMenuOpen;
            return true;
        }

        if (IsStartMenuOpen)
        {
            int menuW = 250;
            int itemH = 34;
            int menuH = (_menuItems.Length * itemH) + 12;
            int menuX = 6;
            int menuY = y - menuH - 6;

            if (mouseX >= menuX && mouseX <= menuX + menuW &&
                mouseY >= menuY && mouseY <= menuY + menuH)
            {
                int clickRelY = mouseY - (menuY + 6);
                int clickedIndex = clickRelY / itemH;

                if (clickedIndex >= 0 && clickedIndex < _menuItems.Length)
                {
                    var selected = _menuItems[clickedIndex].Item;
                    IsStartMenuOpen = false;
                    OnMenuItemSelected?.Invoke(selected);
                    return true;
                }
            }
            else
            {
                IsStartMenuOpen = false;
            }
        }

        if (mouseY >= y && mouseY <= y + Height)
        {
            int currentX = startBtnX + startBtnW + 8;
            var windows = _windowManager.Windows;
            int clockW = 55;
            int clockX = 800 - clockW - 12;
            int maxAvailableW = clockX - currentX - 10;

            if (windows.Count > 0 && maxAvailableW > 60)
            {
                int btnW = Math.Min(160, maxAvailableW / windows.Count);
                for (int i = 0; i < windows.Count; i++)
                {
                    var win = windows[i];
                    if (win.IsClosed) continue;

                    int bx = currentX + (i * (btnW + 4));
                    if (mouseX >= bx && mouseX <= bx + btnW)
                    {
                        if (win.IsMinimized)
                        {
                            win.IsMinimized = false;
                            _windowManager.BringToFront(win);
                        }
                        else if (win.IsActive)
                        {
                            win.IsMinimized = true;
                            _windowManager.ActivateTopWindow();
                        }
                        else
                        {
                            _windowManager.BringToFront(win);
                        }
                        return true;
                    }
                }
            }
            return true;
        }

        return false;
    }
}
