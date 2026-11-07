using System;
using System.Collections.Generic;
using System.Drawing;
using Cosmos.Kernel.System;
using NybuOS.Apps;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;

namespace NybuOS;

/// <summary>
/// Kernel do NybuOS - Sistema Operacional moderno, minimalista e fluido.
/// Double buffering de alto desempenho com wallpaper HD pré-renderizado,
/// tipografia Cosmos limpa e janelas opacas dark glass.
/// </summary>
public class Kernel : Cosmos.Kernel.System.Kernel
{
    private DisplayDriver _display = null!;
    private MouseDriver _mouse = null!;
    private KeyboardDriver _keyboard = null!;
    private VirtualFileSystem _vfs = null!;
    private WindowManager _windowManager = null!;
    private Taskbar _taskbar = null!;

    private readonly List<DesktopIcon> _desktopIcons = new List<DesktopIcon>();
    private int _nextWindowId = 1;
    private bool _isSetupMode = false;
    private bool _isInstalled = false;

    private IconImage? _scaledWallpaper = null;

    protected override void BeforeRun()
    {
        // 1. Inicializa o display imediatamente para esconder os logs textuais do bootloader Cosmos
        _display = new DisplayDriver();
        _display.Initialize();

        // 2. Inicializa recursos gráficos procedurais e logo de boot
        IconResources.Initialize();

        // 3. Exibe a tela de splash boot gráfica com o logo do OS e status
        DrawBootSplashScreen("Iniciando...", 25);

        _vfs = new VirtualFileSystem();
        _vfs.Initialize();
        DeployFilesToDisk();

        DrawBootSplashScreen("Carregando recursos visuais...", 50);

        // Carrega o wallpaper nativo Full HD 1920x1080
        try
        {
            _scaledWallpaper = WallpaperResources.GetScaledWallpaper(_display.Width, _display.Height);
        }
        catch
        {
            _scaledWallpaper = null;
        }

        DrawBootSplashScreen("Inicializando dispositivos de entrada e audio...", 75);

        _mouse = new MouseDriver(_display.Width, _display.Height);
        _mouse.Initialize();

        _keyboard = new KeyboardDriver();
        _keyboard.Initialize();

        DrawBootSplashScreen("Iniciando subsistema de janelas...", 90);

        // Verifica se o sistema operacional já está instalado no HD ou se está em modo de instalação
        _isInstalled = HardDiskInstaller.IsSystemInstalledOnHardDisk();

        _windowManager = new WindowManager();
        _taskbar = new Taskbar(_windowManager);
        _taskbar.SetInstalledMode(_isInstalled);

        _taskbar.OnMenuItemSelected += HandleStartMenuItem;

        SetupDesktopIcons();

        DrawBootSplashScreen("Pronto!", 100);

        // Som de inicialização pelo PC Speaker / sintetizador
        SoundDriver.PlayStartupSound();

        if (!_isInstalled)
        {
            _isSetupMode = true;
            LaunchInstaller();
        }
        else
        {
            _isSetupMode = false;
            // Modo HD permanente: o instalador não é aberto e o sistema inicia diretamente na Área de Trabalho limpa
        }
    }

    private void DrawBootSplashScreen(string message, int progress)
    {
        try
        {
            int sw = _display.Width;
            int sh = _display.Height;

            // Fundo escuro moderno
            _display.Clear(Color.FromArgb(15, 15, 23));

            int cx = sw / 2;
            int cy = (sh / 2) - 45;

            // Ícone do OS centralizado no boot
            if (IconResources.BootLogo64 != null)
            {
                _display.DrawIcon(cx - 32, cy - 32, IconResources.BootLogo64);
            }

            // Nome do Sistema Operacional
            string title = "NybuOS RV";
            int titleW = title.Length * CosmosNativeFont.Width;
            _display.DrawString(cx - (titleW / 2), cy + 44, title, Color.White);

            // Slogan
            string slogan = "Revolution Edition - Full HD";
            int sloganW = slogan.Length * CosmosNativeFont.Width;
            _display.DrawString(cx - (sloganW / 2), cy + 64, slogan, Color.FromArgb(148, 163, 184));

            // Legenda de boot ("Iniciando...")
            int msgW = message.Length * CosmosNativeFont.Width;
            _display.DrawString(cx - (msgW / 2), cy + 104, message, Color.FromArgb(96, 165, 250));

            // Barra de progresso moderna
            int barW = 240;
            int barH = 6;
            int barX = cx - (barW / 2);
            int barY = cy + 126;

            _display.DrawFilledRectangle(barX, barY, barW, barH, Color.FromArgb(30, 30, 45));
            int filled = Math.Min(barW, Math.Max(0, (barW * progress) / 100));
            if (filled > 0)
            {
                _display.DrawFilledRectangle(barX, barY, filled, barH, Theme.AccentColor);
            }

            _display.Swap();
        }
        catch { }
    }

    private void DeployFilesToDisk()
    {
        try
        {
            _vfs.CreateDirectory(@"C:\Documents");
            _vfs.WriteFile(@"C:\Documents\Readme.txt", "NybuOS RV (Revolution) - Full HD\nSistema Operacional Moderno em C# com Cosmos OS.\nFonte Arial PSF e novos ícones PNG nativos.");

            _vfs.CreateDirectory(@"C:\fonts");
            _vfs.WriteFile(@"C:\fonts\fonte.psf", EmbeddedFontData.FontePsf);

            _vfs.CreateDirectory(@"C:\icons");
            var allIcons = EmbeddedIconData.GetAll();
            foreach (var kvp in allIcons)
            {
                _vfs.WriteFile(@"C:\icons\" + kvp.Key, kvp.Value);
            }

            // Grava o papel de parede oficial no disco FAT32 (BMP e JPG)
            byte[]? wallBytes = EmbeddedWallpaperData.GetWallpaperBmpBytes();
            if (wallBytes != null && wallBytes.Length > 0)
            {
                _vfs.WriteFile(@"C:\wallpaper.bmp", wallBytes);
                _vfs.WriteFile(@"C:\Documents\wallpaper.bmp", wallBytes);
            }
        }
        catch
        {
        }
    }

    private void SetupDesktopIcons()
    {
        _desktopIcons.Clear();

        int startX = _display.Width - 134;
        int startY = Taskbar.TopBarHeight + 24;
        int spacingY = 130;

        _desktopIcons.Add(new DesktopIcon("NybuHD", () => IconResources.Installer32, startX, startY, () =>
        {
            LaunchFileManager(@"C:\");
        }));

        _desktopIcons.Add(new DesktopIcon("Documentos", () => IconResources.FileManager32, startX, startY + spacingY, () =>
        {
            LaunchFileManager(@"C:\Documents");
        }));

        // O ícone do instalador SÓ é exibido na Área de Trabalho quando o sistema estiver rodando da ISO Live
        if (!_isInstalled)
        {
            _desktopIcons.Add(new DesktopIcon("Instalar", () => IconResources.Installer32, startX, startY + (spacingY * 2), () =>
            {
                LaunchInstaller();
            }));
        }
    }

    private void HandleStartMenuItem(StartMenuItem item)
    {
        switch (item)
        {
            case StartMenuItem.FileManager:
                LaunchFileManager(@"C:\Documents");
                break;
            case StartMenuItem.TextEditor:
                LaunchTextEditor(null);
                break;
            case StartMenuItem.PaintPad:
                LaunchPaintPad();
                break;
            case StartMenuItem.MediaReproduction:
                LaunchMediaReproduction();
                break;
            case StartMenuItem.ControlPanel:
                LaunchControlPanel();
                break;
            case StartMenuItem.CentralLogic:
                LaunchCentralLogic();
                break;
            case StartMenuItem.Spreadsheet:
                LaunchSpreadsheet();
                break;
            case StartMenuItem.Calculator:
                LaunchCalculator();
                break;
            case StartMenuItem.Terminal:
                LaunchTerminal();
                break;
            case StartMenuItem.Installer:
                LaunchInstaller();
                break;
            case StartMenuItem.Updater:
                LaunchUpdater();
                break;
            case StartMenuItem.About:
                LaunchAbout();
                break;
            case StartMenuItem.Shutdown:
                PowerDriver.Shutdown();
                break;
        }
    }

    public void LaunchFileManager(string path)
    {
        var app = new FileManagerApp(_nextWindowId++, 280, 100, _vfs, (targetFile) =>
        {
            LaunchTextEditor(targetFile);
        });
        if (!string.IsNullOrEmpty(path))
        {
            app.NavigateTo(path);
        }
        _windowManager.AddWindow(app);
    }

    public void LaunchTextEditor(string? filePath)
    {
        var app = new TextEditorApp(_nextWindowId++, 360, 120, _vfs, _windowManager, filePath);
        _windowManager.AddWindow(app);
    }

    public void LaunchPaintPad()
    {
        var app = new PaintPadApp(_nextWindowId++, 320, 90, _vfs);
        _windowManager.AddWindow(app);
    }

    public void LaunchMediaReproduction()
    {
        var app = new MediaReproductionApp(_nextWindowId++, 350, 110, _vfs);
        _windowManager.AddWindow(app);
    }

    public void LaunchControlPanel()
    {
        var app = new ControlPanelApp(_nextWindowId++, 380, 100, _display);
        _windowManager.AddWindow(app);
    }

    public void LaunchCentralLogic()
    {
        var app = new CentralLogicApp(_nextWindowId++, 580, 160);
        _windowManager.AddWindow(app);
    }

    public void LaunchSpreadsheet()
    {
        var app = new SpreadsheetApp(_nextWindowId++, 340, 110, _vfs, _windowManager);
        _windowManager.AddWindow(app);
    }

    public void LaunchCalculator()
    {
        var app = new CalculatorApp(_nextWindowId++, 640, 140);
        _windowManager.AddWindow(app);
    }

    public void LaunchTerminal()
    {
        var app = new TerminalApp(_nextWindowId++, 380, 120, _vfs);
        _windowManager.AddWindow(app);
    }

    public void LaunchInstaller()
    {
        if (_isInstalled) return;

        int winW = 800;
        int winH = 560;
        int winX = Math.Max(20, (_display.Width - winW) / 2);
        int winY = Math.Max(40, (_display.Height - winH) / 2);

        var app = new InstallerApp(_nextWindowId++, winX, winY, _vfs);
        app.OnExitToDesktop = () =>
        {
            _isSetupMode = false;
        };
        _windowManager.AddWindow(app);
    }

    public void LaunchUpdater()
    {
        int winW = 620;
        int winH = 400;
        int winX = Math.Max(20, (_display.Width - winW) / 2);
        int winY = Math.Max(40, (_display.Height - winH) / 2);

        var app = new UpdaterApp(_nextWindowId++, winX, winY);
        _windowManager.AddWindow(app);
    }

    public void LaunchAbout()
    {
        var app = new AboutApp(_nextWindowId++, 520, 180);
        _windowManager.AddWindow(app);
    }

    protected override void Run()
    {
        _mouse.Update();
        _keyboard.Update();

        if (_mouse.JustPressedLeft)
        {
            if (_isSetupMode)
            {
                // No modo de instalação inicial, apenas as janelas respondem ao mouse
                _windowManager.HandleMouseDown(_mouse.X, _mouse.Y);
            }
            else
            {
                bool handledByTaskbar = _taskbar.HandleMouseDown(_mouse.X, _mouse.Y, _display.Width, _display.Height);

                if (!handledByTaskbar)
                {
                    if (_taskbar.IsStartMenuOpen)
                    {
                        _taskbar.IsStartMenuOpen = false;
                    }

                    bool windowClicked = false;
                    for (int i = _windowManager.Windows.Count - 1; i >= 0; i--)
                    {
                        var win = _windowManager.Windows[i];
                        if (!win.IsMinimized && !win.IsClosed && win.ContainsPoint(_mouse.X, _mouse.Y))
                        {
                            windowClicked = true;
                            break;
                        }
                    }

                    if (windowClicked)
                    {
                        _windowManager.HandleMouseDown(_mouse.X, _mouse.Y);
                    }
                    else
                    {
                        foreach (var icon in _desktopIcons)
                        {
                            if (icon.ContainsPoint(_mouse.X, _mouse.Y))
                            {
                                icon.OnClick?.Invoke();
                                break;
                            }
                        }
                    }
                }
            }
        }

        if (_mouse.DeltaX != 0 || _mouse.DeltaY != 0)
        {
            _windowManager.HandleMouseMove(_mouse.X, _mouse.Y);
        }

        if (_mouse.JustReleasedLeft)
        {
            _windowManager.HandleMouseUp(_mouse.X, _mouse.Y);
        }

        while (_keyboard.TryDequeueKey(out var keyEvent))
        {
            _windowManager.HandleKeyDown(keyEvent);
        }

        // Renderização com Double Buffering ultra-rápido Full HD
        if (_scaledWallpaper != null)
        {
            _display.DrawWallpaper(_scaledWallpaper);
        }
        else
        {
            _display.Clear(Color.FromArgb(16, 18, 26));
        }

        if (!_isSetupMode)
        {
            // Desenha os ícones da área de trabalho
            for (int i = 0; i < _desktopIcons.Count; i++)
            {
                _desktopIcons[i].Draw(_display);
            }
        }

        // Janelas (com efeito de vidro fosco e botões circulares)
        _windowManager.DrawAll(_display);

        if (!_isSetupMode)
        {
            // Barra Superior + Dock Flutuante estilo macOS
            _taskbar.Draw(_display, _mouse.X, _mouse.Y);
        }

        // Cursor do mouse estilo macOS
        _display.DrawCursor(_mouse.X, _mouse.Y);

        // Apresenta tela
        _display.Display();
    }
}
