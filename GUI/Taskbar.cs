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
    Updater,
    About,
    Shutdown
}

/// <summary>
/// Ambiente de desktop moderno do NybuOS RV (Revolution):
/// Barra superior estilo macOS com vidro fosco (Menu Bar), relógio e placeholders,
/// e Dock Flutuante com bordas arredondadas, ícones squircle de 44x44 e indicadores de execução.
/// </summary>
public class Taskbar
{
    public const int Height = 90;
    public const int TopBarHeight = 32;
    public const int DockHeight = 90;
    public const int DockMarginBottom = 16;
    public const int DockIconSize = 60;
    public const int DockIconSpacing = 12;
    public const int DockPadX = 18;

    private readonly WindowManager _windowManager;
    public bool IsStartMenuOpen { get; set; }

    public event Action<StartMenuItem> OnMenuItemSelected;

    // Definição dos itens do Dock Flutuante (44x44)
    public class DockItem
    {
        public string Title { get; set; }
        public StartMenuItem Item { get; set; }
        public Func<IconImage> IconResolver { get; set; }
        public bool IsSeparatorBefore { get; set; }

        public DockItem(string title, StartMenuItem item, Func<IconImage> icon, bool separator = false)
        {
            Title = title;
            Item = item;
            IconResolver = icon;
            IsSeparatorBefore = separator;
        }
    }

    private readonly List<DockItem> _dockItems = new List<DockItem>()
    {
        new DockItem("Finder", StartMenuItem.FileManager, () => IconResources.FinderDock),
        new DockItem("Editor de Texto", StartMenuItem.TextEditor, () => IconResources.TextEditorDock),
        new DockItem("PaintPad", StartMenuItem.PaintPad, () => IconResources.PaintPadDock),
        new DockItem("Midia", StartMenuItem.MediaReproduction, () => IconResources.MediaDock),
        new DockItem("Ajustes", StartMenuItem.ControlPanel, () => IconResources.ControlPanelDock),
        new DockItem("Minas", StartMenuItem.CentralLogic, () => IconResources.CentralLogicDock),
        new DockItem("Calculadora", StartMenuItem.Calculator, () => IconResources.CalculatorDock),
        new DockItem("Planilha", StartMenuItem.Spreadsheet, () => IconResources.SpreadsheetDock),
        new DockItem("Terminal", StartMenuItem.Terminal, () => IconResources.TerminalDock),
        new DockItem("Atualizacoes", StartMenuItem.Updater, () => IconResources.UpdaterDock),
        new DockItem("Instalar no HD", StartMenuItem.Installer, () => IconResources.InstallerDock),
        new DockItem("Sobre o NybuOS", StartMenuItem.About, () => IconResources.AboutDock),
        new DockItem("Lixeira", StartMenuItem.None, () => IconResources.TrashDock, separator: true),
        new DockItem("Desligar", StartMenuItem.Shutdown, () => IconResources.ShutdownDock)
    };

    public Taskbar(WindowManager windowManager)
    {
        _windowManager = windowManager;
        IsStartMenuOpen = false;
    }

    /// <summary>
    /// Configura os itens visíveis na barra para modo instalado no HD (remove o item do instalador).
    /// </summary>
    public void SetInstalledMode(bool isInstalled)
    {
        if (isInstalled)
        {
            _dockItems.RemoveAll(d => d.Item == StartMenuItem.Installer);
        }
    }

    /// <summary>
    /// Renderiza a Barra Superior e o Dock Flutuante.
    /// </summary>
    public void Draw(DisplayDriver display, int mouseX = -1, int mouseY = -1)
    {
        int screenW = display.Width;
        int screenH = display.Height;

        // 1. Barra Superior (Top Menu Bar) estilo macOS
        DrawTopMenuBar(display, screenW, mouseX, mouseY);

        // 2. Dock Flutuante no centro inferior
        DrawFloatingDock(display, screenW, screenH, mouseX, mouseY);

        // 3. Dropdown Menu do NybuOS (Apple Menu)
        if (IsStartMenuOpen)
        {
            DrawNybuAppleDropdown(display, mouseX, mouseY);
        }
    }

    private void DrawTopMenuBar(DisplayDriver display, int screenW, int mouseX, int mouseY)
    {
        // Barra Superior 3D Light com degradê de branco para cinza profundo
        display.Draw3DLightTopBar(screenW, TopBarHeight);

        // Ícone / Monograma do NybuOS no canto superior esquerdo
        int logoX = 14;
        int logoY = (TopBarHeight - 20) / 2;
        bool isLogoHover = mouseX >= 6 && mouseX <= 40 && mouseY >= 0 && mouseY <= TopBarHeight;
        if (isLogoHover || IsStartMenuOpen)
        {
            display.DrawFilledRoundedRectangle(6, 3, 34, TopBarHeight - 6, 4, Theme.SurfaceHover, Theme.LightBorder);
        }

        if (IconResources.NybuLogoTopBar != null)
        {
            display.DrawIcon(logoX, logoY, IconResources.NybuLogoTopBar);
        }

        // Título do aplicativo atualmente ativo (em destaque escuro de alto contraste)
        string activeApp = _windowManager.ActiveWindow?.Title ?? "NybuOS RV";
        if (activeApp.Contains(" - "))
        {
            activeApp = activeApp.Substring(0, activeApp.IndexOf(" - "));
        }
        int textY = (TopBarHeight - CosmosNativeFont.Height) / 2;
        display.DrawString(48, textY, activeApp, Theme.TextPrimary);

        // Placeholders de menus padrão do sistema
        int curMenuX = 48 + (activeApp.Length * CosmosNativeFont.Width) + 24;
        string[] standardMenus = new string[] { "Arquivo", "Editar", "Visualizar", "Ir", "Janela", "Ajuda" };
        foreach (var m in standardMenus)
        {
            display.DrawString(curMenuX, textY, m, Theme.TextSecondary);
            curMenuX += (m.Length * CosmosNativeFont.Width) + 18;
        }

        // Canto Superior Direito: Relógio e Placeholders (Wi-Fi, Volume, Bateria, Lupa Spotlight, Central de Controle)
        DateTime now = DateTime.Now;
        string timeStr = $"{now:ddd d MMM}  {now.Hour:D2}:{now.Minute:D2}";
        int clockW = timeStr.Length * CosmosNativeFont.Width;
        int clockX = screenW - clockW - 14;

        display.DrawString(clockX, textY, timeStr, Theme.TextPrimary);

        int rightX = clockX - 28;
        int iconY = (TopBarHeight - 18) / 2;

        // Central de Controle
        if (IconResources.ControlCenterIcon != null)
        {
            display.DrawIcon(rightX, iconY, IconResources.ControlCenterIcon);
            rightX -= 26;
        }

        // Spotlight / Lupa
        if (IconResources.SearchIcon != null)
        {
            display.DrawIcon(rightX, iconY, IconResources.SearchIcon);
            rightX -= 26;
        }

        // Bateria
        if (IconResources.BatteryIcon != null)
        {
            display.DrawIcon(rightX, iconY, IconResources.BatteryIcon);
            rightX -= 28;
        }

        // Wi-Fi
        if (IconResources.WifiIcon != null)
        {
            display.DrawIcon(rightX, iconY, IconResources.WifiIcon);
            rightX -= 26;
        }

        // Volume
        if (IconResources.VolumeIcon != null)
        {
            display.DrawIcon(rightX, iconY, IconResources.VolumeIcon);
        }
    }

    private void DrawFloatingDock(DisplayDriver display, int screenW, int screenH, int mouseX, int mouseY)
    {
        int iconSize = DockIconSize;
        int iconSpacing = DockIconSpacing;
        int padX = DockPadX;

        // Calcula largura total do dock
        int totalW = padX * 2;
        for (int i = 0; i < _dockItems.Count; i++)
        {
            if (_dockItems[i].IsSeparatorBefore) totalW += 16;
            totalW += iconSize;
            if (i < _dockItems.Count - 1) totalW += iconSpacing;
        }

        int dockX = (screenW - totalW) / 2;
        int dockY = screenH - DockHeight - DockMarginBottom;

        // Fundo Quadrado com VÉRTICES Arredondadas do Dock: cinza intenso e sutis gradientes brancos
        display.DrawOpaqueGradientDock(dockX, dockY, totalW, DockHeight, cornerRadius: 10);

        // Renderiza os ícones do Dock
        int curX = dockX + padX;
        DockItem? hoveredItem = null;
        int hoveredIconX = 0;

        for (int i = 0; i < _dockItems.Count; i++)
        {
            var item = _dockItems[i];

            // Linha divisória vertical antes da lixeira/desligar com relevo chanfrado
            if (item.IsSeparatorBefore)
            {
                int sepX = curX + 6;
                display.DrawLine(sepX, dockY + 16, sepX, dockY + DockHeight - 16, Color.FromArgb(64, 68, 78));
                display.DrawLine(sepX + 1, dockY + 16, sepX + 1, dockY + DockHeight - 16, Color.FromArgb(98, 104, 118));
                curX += 16;
            }

            int iconY = dockY + (DockHeight - iconSize) / 2;
            bool isHover = mouseX >= curX && mouseX <= curX + iconSize &&
                           mouseY >= iconY && mouseY <= iconY + iconSize;

            if (isHover)
            {
                hoveredItem = item;
                hoveredIconX = curX;
                // Efeito sutil de destaque no dock claro atrás do ícone
                display.DrawFilledRoundedRectangle(curX - 4, iconY - 4, iconSize + 8, iconSize + 8, 8, Theme.SurfaceHover, Theme.ShadowBorder);
            }

            // Desenha o ícone
            var icon = item.IconResolver?.Invoke();
            if (icon != null)
            {
                display.DrawIcon(curX, iconY, icon);
            }

            // Indicador de Execução estilo macOS: Ponto moderno abaixo do ícone sobre o dock claro
            bool isRunning = IsAppRunning(item.Item);
            if (isRunning)
            {
                int dotX = curX + (iconSize / 2);
                int dotY = dockY + DockHeight - 7;
                display.DrawFilledCircle(dotX, dotY, 3, Theme.AccentColor, Color.White);
            }

            curX += iconSize + iconSpacing;
        }

        // Tooltip flutuante acima do ícone no hover
        if (hoveredItem != null)
        {
            string ttText = hoveredItem.Title;
            int ttW = (ttText.Length * CosmosNativeFont.Width) + 20;
            int ttH = 26;
            int ttX = hoveredIconX + (iconSize / 2) - (ttW / 2);
            int ttY = dockY - ttH - 8;

            display.DrawFrostedGlass(ttX, ttY, ttW, ttH, Theme.Surface, alpha: 245, cornerRadius: 6);
            display.DrawRectangle(ttX, ttY, ttW, ttH, Theme.ShadowBorder);
            display.DrawString(ttX + 10, ttY + 5, ttText, Theme.TextPrimary);
        }
    }

    private bool IsAppRunning(StartMenuItem item)
    {
        if (item == StartMenuItem.None || item == StartMenuItem.Shutdown) return false;

        foreach (var win in _windowManager.Windows)
        {
            if (win.IsClosed) continue;
            switch (item)
            {
                case StartMenuItem.FileManager:
                    if (win is Apps.FileManagerApp) return true;
                    break;
                case StartMenuItem.TextEditor:
                    if (win is Apps.TextEditorApp) return true;
                    break;
                case StartMenuItem.PaintPad:
                    if (win is Apps.PaintPadApp) return true;
                    break;
                case StartMenuItem.MediaReproduction:
                    if (win is Apps.MediaReproductionApp) return true;
                    break;
                case StartMenuItem.ControlPanel:
                    if (win is Apps.ControlPanelApp) return true;
                    break;
                case StartMenuItem.CentralLogic:
                    if (win is Apps.CentralLogicApp) return true;
                    break;
                case StartMenuItem.Spreadsheet:
                    if (win is Apps.SpreadsheetApp) return true;
                    break;
                case StartMenuItem.Calculator:
                    if (win is Apps.CalculatorApp) return true;
                    break;
                case StartMenuItem.Terminal:
                    if (win is Apps.TerminalApp) return true;
                    break;
                case StartMenuItem.Installer:
                    if (win is Apps.InstallerApp) return true;
                    break;
                case StartMenuItem.About:
                    if (win is Apps.AboutApp) return true;
                    break;
            }
        }
        return false;
    }

    private void DrawNybuAppleDropdown(DisplayDriver display, int mouseX, int mouseY)
    {
        int menuX = 8;
        int menuY = TopBarHeight + 2;
        int menuW = 230;
        int itemH = 28;

        string[] items = new string[]
        {
            "Sobre este NybuOS RV",
            "Preferencias do Sistema...",
            "Atualizacoes do Sistema...",
            "-",
            "Reiniciar Computador...",
            "Desligar o NybuOS..."
        };

        int menuH = (items.Length * itemH) + 12;

        // Janela dropdown de vidro fosco com sombra
        display.DrawFrostedGlass(menuX, menuY, menuW, menuH, Theme.StartMenuBg, alpha: 235, cornerRadius: 8);

        for (int i = 0; i < items.Length; i++)
        {
            string label = items[i];
            int iy = menuY + 6 + (i * itemH);

            if (label == "-")
            {
                display.DrawLine(menuX + 10, iy + itemH / 2, menuX + menuW - 10, iy + itemH / 2, Theme.DockSeparator);
                continue;
            }

            bool isHover = mouseX >= menuX + 4 && mouseX <= menuX + menuW - 4 &&
                           mouseY >= iy && mouseY <= iy + itemH;

            if (isHover)
            {
                display.DrawFrostedGlass(menuX + 4, iy, menuW - 8, itemH, Theme.AccentColor, alpha: 220, cornerRadius: 4);
            }

            Color textCol = isHover ? Color.White : (label.Contains("Desligar") ? Color.FromArgb(248, 113, 113) : Theme.TextPrimary);
            display.DrawString(menuX + 16, iy + (itemH - CosmosNativeFont.Height) / 2, label, textCol);
        }
    }

    public bool HandleMouseDown(int mouseX, int mouseY, int screenW = 1920, int screenH = 1080)
    {
        // 1. Clique no logo do NybuOS na barra superior
        if (mouseY >= 0 && mouseY <= TopBarHeight)
        {
            if (mouseX >= 8 && mouseX <= 38)
            {
                IsStartMenuOpen = !IsStartMenuOpen;
                return true;
            }
        }

        // 2. Clique dentro do Dropdown Menu
        if (IsStartMenuOpen)
        {
            int menuX = 8;
            int menuY = TopBarHeight + 2;
            int menuW = 230;
            int itemH = 28;

            if (mouseX >= menuX && mouseX <= menuX + menuW &&
                mouseY >= menuY && mouseY <= menuY + (6 * itemH) + 12)
            {
                int relY = mouseY - (menuY + 6);
                int idx = relY / itemH;
                IsStartMenuOpen = false;

                switch (idx)
                {
                    case 0:
                        OnMenuItemSelected?.Invoke(StartMenuItem.About);
                        return true;
                    case 1:
                        OnMenuItemSelected?.Invoke(StartMenuItem.ControlPanel);
                        return true;
                    case 2:
                        OnMenuItemSelected?.Invoke(StartMenuItem.Updater);
                        return true;
                    case 4:
                        PowerDriver.Reboot();
                        return true;
                    case 5:
                        OnMenuItemSelected?.Invoke(StartMenuItem.Shutdown);
                        return true;
                }
                return true;
            }
            else
            {
                IsStartMenuOpen = false;
            }
        }

        // 3. Clique nos itens do Dock Flutuante
        int iconSize = DockIconSize;
        int iconSpacing = DockIconSpacing;
        int padX = DockPadX;

        int totalW = padX * 2;
        for (int i = 0; i < _dockItems.Count; i++)
        {
            if (_dockItems[i].IsSeparatorBefore) totalW += 16;
            totalW += iconSize;
            if (i < _dockItems.Count - 1) totalW += iconSpacing;
        }

        int dockX = (screenW - totalW) / 2;
        int dockY = screenH - DockHeight - DockMarginBottom;

        if (mouseX >= dockX && mouseX <= dockX + totalW &&
            mouseY >= dockY && mouseY <= dockY + DockHeight)
        {
            int curX = dockX + padX;
            for (int i = 0; i < _dockItems.Count; i++)
            {
                var item = _dockItems[i];
                if (item.IsSeparatorBefore) curX += 16;

                int iconY = dockY + (DockHeight - iconSize) / 2;
                if (mouseX >= curX && mouseX <= curX + iconSize &&
                    mouseY >= iconY && mouseY <= iconY + iconSize)
                {
                    ActivateOrLaunchDockApp(item.Item);
                    return true;
                }

                curX += iconSize + iconSpacing;
            }
            return true;
        }

        return false;
    }

    private void ActivateOrLaunchDockApp(StartMenuItem item)
    {
        if (item == StartMenuItem.None) return;

        if (item == StartMenuItem.Shutdown)
        {
            OnMenuItemSelected?.Invoke(StartMenuItem.Shutdown);
            return;
        }

        // Procura se já há uma janela aberta para este aplicativo
        Window? targetWin = null;
        foreach (var win in _windowManager.Windows)
        {
            if (win.IsClosed) continue;
            bool match = item switch
            {
                StartMenuItem.FileManager => win is Apps.FileManagerApp,
                StartMenuItem.TextEditor => win is Apps.TextEditorApp,
                StartMenuItem.PaintPad => win is Apps.PaintPadApp,
                StartMenuItem.MediaReproduction => win is Apps.MediaReproductionApp,
                StartMenuItem.ControlPanel => win is Apps.ControlPanelApp,
                StartMenuItem.CentralLogic => win is Apps.CentralLogicApp,
                StartMenuItem.Spreadsheet => win is Apps.SpreadsheetApp,
                StartMenuItem.Calculator => win is Apps.CalculatorApp,
                StartMenuItem.Terminal => win is Apps.TerminalApp,
                StartMenuItem.Installer => win is Apps.InstallerApp,
                StartMenuItem.About => win is Apps.AboutApp,
                _ => false
            };

            if (match)
            {
                targetWin = win;
                break;
            }
        }

        if (targetWin != null)
        {
            if (targetWin.IsMinimized)
            {
                targetWin.IsMinimized = false;
                _windowManager.BringToFront(targetWin);
            }
            else if (targetWin.IsActive)
            {
                targetWin.IsMinimized = true;
                _windowManager.ActivateTopWindow();
            }
            else
            {
                _windowManager.BringToFront(targetWin);
            }
        }
        else
        {
            // Se o app não estiver aberto, dispara o evento para iniciá-lo
            OnMenuItemSelected?.Invoke(item);
        }
    }
}
