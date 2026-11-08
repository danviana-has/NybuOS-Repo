using System;
using System.Drawing;

namespace NybuOS.GUI;

/// <summary>
/// Gerador procedural de ícones do NybuOS RV (Revolution) com estética macOS Big Sur / Catalina.
/// Produz squircles vetorizados, gradientes sutis, relevos modernos e ícones refinados para o Dock (44x44),
/// Área de Trabalho (48x48 / 32x32), Barra Superior e Janelas.
/// </summary>
public static class IconResources
{
    // Ícones do Dock Flutuante (44x44)
    public static IconImage FinderDock { get; private set; } = null!;
    public static IconImage LaunchpadDock { get; private set; } = null!;
    public static IconImage TextEditorDock { get; private set; } = null!;
    public static IconImage PaintPadDock { get; private set; } = null!;
    public static IconImage MediaDock { get; private set; } = null!;
    public static IconImage ControlPanelDock { get; private set; } = null!;
    public static IconImage CentralLogicDock { get; private set; } = null!;
    public static IconImage SpreadsheetDock { get; private set; } = null!;
    public static IconImage CalculatorDock { get; private set; } = null!;
    public static IconImage TerminalDock { get; private set; } = null!;
    public static IconImage InstallerDock { get; private set; } = null!;
    public static IconImage UpdaterDock { get; private set; } = null!;
    public static IconImage DownloadCentralDock { get; private set; } = null!;
    public static IconImage AboutDock { get; private set; } = null!;
    public static IconImage TrashDock { get; private set; } = null!;
    public static IconImage ShutdownDock { get; private set; } = null!;

    // Placeholders e Símbolos da Barra Superior (Top Menu Bar)
    public static IconImage NybuLogoTopBar { get; private set; } = null!;
    public static IconImage WifiIcon { get; private set; } = null!;
    public static IconImage VolumeIcon { get; private set; } = null!;
    public static IconImage BatteryIcon { get; private set; } = null!;
    public static IconImage SearchIcon { get; private set; } = null!;
    public static IconImage ControlCenterIcon { get; private set; } = null!;

    // Ícones de 24x24 / Menus (Compatibilidade)
    public static IconImage StartButtonIcon => NybuLogoTopBar;
    public static IconImage FileManagerIcon { get; private set; } = null!;
    public static IconImage TextEditorIcon { get; private set; } = null!;
    public static IconImage PaintPadIcon { get; private set; } = null!;
    public static IconImage MediaIcon { get; private set; } = null!;
    public static IconImage ControlPanelIcon { get; private set; } = null!;
    public static IconImage CentralLogicIcon { get; private set; } = null!;
    public static IconImage SpreadsheetIcon { get; private set; } = null!;
    public static IconImage CalculatorIcon { get; private set; } = null!;
    public static IconImage TerminalIcon { get; private set; } = null!;
    public static IconImage InstallerIcon { get; private set; } = null!;
    public static IconImage UpdaterIcon { get; private set; } = null!;
    public static IconImage DownloadCentralIcon { get; private set; } = null!;
    public static IconImage AboutIcon { get; private set; } = null!;
    public static IconImage ShutdownIcon { get; private set; } = null!;

    // Ícones de 16x16 para barras de janelas e listas de arquivos
    public static IconImage FolderClosed16 { get; private set; } = null!;
    public static IconImage FolderOpen16 { get; private set; } = null!;
    public static IconImage Doc16 { get; private set; } = null!;
    public static IconImage Drive16 { get; private set; } = null!;
    public static IconImage Computer16 { get; private set; } = null!;
    public static IconImage Media16 { get; private set; } = null!;
    public static IconImage ControlPanel16 { get; private set; } = null!;
    public static IconImage Calculator16 { get; private set; } = null!;
    public static IconImage Terminal16 { get; private set; } = null!;
    public static IconImage PaintPad16 { get; private set; } = null!;
    public static IconImage Minesweeper16 { get; private set; } = null!;
    public static IconImage Spreadsheet16 { get; private set; } = null!;
    public static IconImage Installer16 { get; private set; } = null!;
    public static IconImage Updater16 { get; private set; } = null!;
    public static IconImage DownloadCentral16 { get; private set; } = null!;
    public static IconImage Help16 { get; private set; } = null!;

    // Ícones de 32x32 / 48x48 para Área de Trabalho
    public static IconImage Computer32 { get; private set; } = null!;
    public static IconImage FolderClosed32 { get; private set; } = null!;
    public static IconImage FileManager32 { get; private set; } = null!;
    public static IconImage TextEditor32 { get; private set; } = null!;
    public static IconImage Doc32 { get; private set; } = null!;
    public static IconImage PaintPad32 { get; private set; } = null!;
    public static IconImage Media32 { get; private set; } = null!;
    public static IconImage ControlPanel32 { get; private set; } = null!;
    public static IconImage CentralLogic32 { get; private set; } = null!;
    public static IconImage Minesweeper32 { get; private set; } = null!;
    public static IconImage Spreadsheet32 { get; private set; } = null!;
    public static IconImage Calculator32 { get; private set; } = null!;
    public static IconImage Terminal32 { get; private set; } = null!;
    public static IconImage Installer32 { get; private set; } = null!;
    public static IconImage Updater32 { get; private set; } = null!;
    public static IconImage DownloadCentral32 { get; private set; } = null!;
    public static IconImage Recycle32 { get; private set; } = null!;

    // Logo especial de Splash Boot Screen (64x64)
    public static IconImage BootLogo64 { get; private set; } = null!;

    private static IconImage LoadPng(byte[] pngData, int size, Func<int, IconImage> fallback)
    {
        try
        {
            var decoded = PngDecoder.Decode(pngData, size, size);
            if (decoded != null) return decoded;
        }
        catch { }
        return fallback(size);
    }

    public static void Initialize()
    {
        // 1. Ícones do Dock Flutuante (60x60) decodificados diretamente dos arquivos PNG reais baixados pelo usuário
        FinderDock = LoadPng(EmbeddedIconData.FileApp, 60, CreateFinderIcon);
        LaunchpadDock = CreateLaunchpadIcon(60);
        TextEditorDock = LoadPng(EmbeddedIconData.texteditor, 60, CreateTextEditorIcon);
        PaintPadDock = LoadPng(EmbeddedIconData.PaintPad, 60, CreatePaintPadIcon);
        MediaDock = LoadPng(EmbeddedIconData.MediaReproduction, 60, CreateMediaSafariIcon);
        ControlPanelDock = LoadPng(EmbeddedIconData.ControlPanel, 60, CreateControlPanelIcon);
        CentralLogicDock = LoadPng(EmbeddedIconData.CentralLogic, 60, CreateCentralLogicIcon);
        SpreadsheetDock = LoadPng(EmbeddedIconData.spreadsheet, 60, CreateSpreadsheetIcon);
        CalculatorDock = LoadPng(EmbeddedIconData.calculator, 60, CreateCalculatorIcon);
        TerminalDock = LoadPng(EmbeddedIconData.terminal, 60, CreateTerminalIcon);
        InstallerDock = LoadPng(EmbeddedIconData.InstallerApp, 60, CreateInstallerIcon);
        UpdaterDock = CreateUpdaterIcon(60);
        DownloadCentralDock = CreateDownloadCentralIcon(60);
        AboutDock = LoadPng(EmbeddedIconData.AboutApp, 60, CreateAboutIcon);
        TrashDock = CreateTrashIcon(60);
        ShutdownDock = CreatePowerIcon(60);

        // 2. Símbolos da Barra Superior
        NybuLogoTopBar = CreateAppleNybuLogo(20);
        WifiIcon = CreateWifiIcon(18);
        VolumeIcon = CreateVolumeIcon(18);
        BatteryIcon = CreateBatteryIcon(20);
        SearchIcon = CreateSearchIcon(18);
        ControlCenterIcon = CreateControlCenterIcon(18);

        // 3. Ícones de Menus (24x24)
        FileManagerIcon = LoadPng(EmbeddedIconData.FileApp, 24, CreateFinderIcon);
        TextEditorIcon = LoadPng(EmbeddedIconData.texteditor, 24, CreateTextEditorIcon);
        PaintPadIcon = LoadPng(EmbeddedIconData.PaintPad, 24, CreatePaintPadIcon);
        MediaIcon = LoadPng(EmbeddedIconData.MediaReproduction, 24, CreateMediaSafariIcon);
        ControlPanelIcon = LoadPng(EmbeddedIconData.ControlPanel, 24, CreateControlPanelIcon);
        CentralLogicIcon = LoadPng(EmbeddedIconData.CentralLogic, 24, CreateCentralLogicIcon);
        SpreadsheetIcon = LoadPng(EmbeddedIconData.spreadsheet, 24, CreateSpreadsheetIcon);
        CalculatorIcon = LoadPng(EmbeddedIconData.calculator, 24, CreateCalculatorIcon);
        TerminalIcon = LoadPng(EmbeddedIconData.terminal, 24, CreateTerminalIcon);
        InstallerIcon = LoadPng(EmbeddedIconData.InstallerApp, 24, CreateInstallerIcon);
        UpdaterIcon = CreateUpdaterIcon(24);
        DownloadCentralIcon = CreateDownloadCentralIcon(24);
        AboutIcon = LoadPng(EmbeddedIconData.AboutApp, 24, CreateAboutIcon);
        ShutdownIcon = CreatePowerIcon(24);

        // 4. Ícones de Janelas (16x16)
        FolderClosed16 = LoadPng(EmbeddedIconData.FileApp, 16, CreateFinderIcon);
        FolderOpen16 = LoadPng(EmbeddedIconData.FileApp, 16, CreateFinderIcon);
        Doc16 = LoadPng(EmbeddedIconData.texteditor, 16, CreateTextEditorIcon);
        Drive16 = LoadPng(EmbeddedIconData.InstallerApp, 16, CreateInstallerIcon);
        Computer16 = LoadPng(EmbeddedIconData.ControlPanel, 16, CreateControlPanelIcon);
        Media16 = LoadPng(EmbeddedIconData.MediaReproduction, 16, CreateMediaSafariIcon);
        ControlPanel16 = LoadPng(EmbeddedIconData.ControlPanel, 16, CreateControlPanelIcon);
        Calculator16 = LoadPng(EmbeddedIconData.calculator, 16, CreateCalculatorIcon);
        Terminal16 = LoadPng(EmbeddedIconData.terminal, 16, CreateTerminalIcon);
        PaintPad16 = LoadPng(EmbeddedIconData.PaintPad, 16, CreatePaintPadIcon);
        Minesweeper16 = LoadPng(EmbeddedIconData.CentralLogic, 16, CreateCentralLogicIcon);
        Spreadsheet16 = LoadPng(EmbeddedIconData.spreadsheet, 16, CreateSpreadsheetIcon);
        Installer16 = LoadPng(EmbeddedIconData.InstallerApp, 16, CreateInstallerIcon);
        Updater16 = CreateUpdaterIcon(16);
        DownloadCentral16 = CreateDownloadCentralIcon(16);
        Help16 = LoadPng(EmbeddedIconData.AboutApp, 16, CreateAboutIcon);

        // 5. Ícones da Área de Trabalho (48x48)
        Computer32 = LoadPng(EmbeddedIconData.ControlPanel, 48, CreateControlPanelIcon);
        FolderClosed32 = LoadPng(EmbeddedIconData.FileApp, 48, CreateFinderIcon);
        FileManager32 = LoadPng(EmbeddedIconData.FileApp, 48, CreateFinderIcon);
        TextEditor32 = LoadPng(EmbeddedIconData.texteditor, 48, CreateTextEditorIcon);
        Doc32 = LoadPng(EmbeddedIconData.texteditor, 48, CreateTextEditorIcon);
        PaintPad32 = LoadPng(EmbeddedIconData.PaintPad, 48, CreatePaintPadIcon);
        Media32 = LoadPng(EmbeddedIconData.MediaReproduction, 48, CreateMediaSafariIcon);
        ControlPanel32 = LoadPng(EmbeddedIconData.ControlPanel, 48, CreateControlPanelIcon);
        CentralLogic32 = LoadPng(EmbeddedIconData.CentralLogic, 48, CreateCentralLogicIcon);
        Minesweeper32 = LoadPng(EmbeddedIconData.CentralLogic, 48, CreateCentralLogicIcon);
        Spreadsheet32 = LoadPng(EmbeddedIconData.spreadsheet, 48, CreateSpreadsheetIcon);
        Calculator32 = LoadPng(EmbeddedIconData.calculator, 48, CreateCalculatorIcon);
        Terminal32 = LoadPng(EmbeddedIconData.terminal, 48, CreateTerminalIcon);
        Installer32 = LoadPng(EmbeddedIconData.InstallerApp, 48, CreateInstallerIcon);
        Updater32 = CreateUpdaterIcon(48);
        DownloadCentral32 = CreateDownloadCentralIcon(48);
        Recycle32 = CreateTrashIcon(48);

        // 6. Boot Logo (64x64)
        BootLogo64 = LoadPng(EmbeddedIconData.AboutApp, 64, CreateAppleNybuLogo);
    }

    /// <summary>
    /// Gera a base clássica de Squircle do macOS (retângulo com curvatura suave e brilho superior).
    /// </summary>
    private static IconImage CreateSquircleBase(int size, Color topGradient, Color bottomGradient)
    {
        var img = new IconImage(size, size);
        int r = Math.Max(3, size * 22 / 100);
        int r2 = r * r;

        for (int y = 0; y < size; y++)
        {
            float t = (float)y / (size - 1);
            int red = (int)(topGradient.R + (bottomGradient.R - topGradient.R) * t);
            int green = (int)(topGradient.G + (bottomGradient.G - topGradient.G) * t);
            int blue = (int)(topGradient.B + (bottomGradient.B - topGradient.B) * t);
            uint col = 0xFF000000 | ((uint)red << 16) | ((uint)green << 8) | (uint)blue;

            for (int x = 0; x < size; x++)
            {
                if (x < r && y < r)
                {
                    int dx = r - x; int dy = r - y;
                    if (dx * dx + dy * dy > r2) continue;
                }
                else if (x >= size - r && y < r)
                {
                    int dx = x - (size - r - 1); int dy = r - y;
                    if (dx * dx + dy * dy > r2) continue;
                }
                else if (x < r && y >= size - r)
                {
                    int dx = r - x; int dy = y - (size - r - 1);
                    if (dx * dx + dy * dy > r2) continue;
                }
                else if (x >= size - r && y >= size - r)
                {
                    int dx = x - (size - r - 1); int dy = y - (size - r - 1);
                    if (dx * dx + dy * dy > r2) continue;
                }

                if (y == 0 || (y == 1 && x >= r && x < size - r))
                {
                    img.SetPixel(x, y, 0xFFFFFFFF); // Brilho de topo
                }
                else
                {
                    img.SetPixel(x, y, col);
                }
            }
        }
        return img;
    }

    /// <summary>
    /// Ícone estilo Finder do macOS: Rosto azul sorridente de dois tons.
    /// </summary>
    public static IconImage CreateFinderIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(96, 165, 250), Color.FromArgb(37, 99, 235));
        int cx = size / 2;
        int cy = size / 2;
        int r = Math.Max(2, size * 20 / 100);

        // Lado esquerdo azul claro
        for (int y = r; y < size - r; y++)
        {
            for (int x = r; x < cx; x++)
            {
                img.SetPixel(x, y, 0xFF93C5FD);
            }
        }

        // Olho esquerdo e direito
        int eyeOff = Math.Max(3, size / 5);
        int eyeY = cy - Math.Max(2, size / 10);
        int eyeSize = Math.Max(1, size / 14);

        for (int dy = -eyeSize; dy <= eyeSize; dy++)
        {
            for (int dx = -eyeSize; dx <= eyeSize; dx++)
            {
                img.SetPixel(cx - eyeOff + dx, eyeY + dy, 0xFFFFFFFF);
                img.SetPixel(cx + eyeOff + dx, eyeY + dy, 0xFFFFFFFF);
            }
        }

        // Linha central do nariz
        for (int y = eyeY; y <= cy + 2; y++)
        {
            img.SetPixel(cx, y, 0xFF1E3A8A);
            img.SetPixel(cx - 1, y, 0xFF1E3A8A);
        }

        // Arco do sorriso
        int smileY = cy + Math.Max(3, size / 6);
        int smileW = Math.Max(4, size / 4);
        for (int dx = -smileW; dx <= smileW; dx++)
        {
            int sy = smileY + (dx * dx) / (smileW * 2);
            if (sy < size - r)
            {
                img.SetPixel(cx + dx, sy, 0xFFFFFFFF);
                img.SetPixel(cx + dx, sy + 1, 0xFF1E3A8A);
            }
        }

        return img;
    }

    /// <summary>
    /// Ícone Launchpad: Foguete espacial em squircle índigo.
    /// </summary>
    public static IconImage CreateLaunchpadIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(79, 70, 229), Color.FromArgb(30, 27, 75));
        int cx = size / 2;
        int cy = size / 2;

        // Foguete diagonal (branco prateado)
        for (int i = -size / 4; i <= size / 4; i++)
        {
            int px = cx + i;
            int py = cy - i;
            img.SetPixel(px, py, 0xFFFFFFFF);
            img.SetPixel(px - 1, py, 0xFFE2E8F0);
            img.SetPixel(px, py + 1, 0xFFE2E8F0);
        }

        // Janela do foguete (azul ciano)
        img.SetPixel(cx, cy, 0xFF38BDF8);
        img.SetPixel(cx + 1, cy - 1, 0xFF38BDF8);

        // Fogo / Chamas da turbina (laranja / amarelo)
        int flameX = cx - size / 4 - 2;
        int flameY = cy + size / 4 + 2;
        if (flameX >= 2 && flameY < size - 2)
        {
            img.SetPixel(flameX, flameY, 0xFFF97316);
            img.SetPixel(flameX - 1, flameY + 1, 0xFFEAB308);
        }

        return img;
    }

    /// <summary>
    /// Ícone Editor de Texto / Notas: Bloco pautado com lápis.
    /// </summary>
    public static IconImage CreateTextEditorIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(254, 243, 199), Color.FromArgb(253, 230, 138));
        int r = Math.Max(2, size * 20 / 100);

        // Faixa superior amarela/laranja do bloco
        int headerH = Math.Max(3, size / 5);
        for (int y = r; y < r + headerH; y++)
        {
            for (int x = r; x < size - r; x++)
            {
                img.SetPixel(x, y, 0xFFF59E0B);
            }
        }

        // Linhas pautadas horizontais
        for (int line = 1; line <= 3; line++)
        {
            int ly = r + headerH + line * (size / 6);
            if (ly < size - r - 2)
            {
                for (int x = r + 3; x < size - r - 3; x++)
                {
                    img.SetPixel(x, ly, 0xFFCBD5E1);
                }
            }
        }

        // Lápis na diagonal
        int cx = size / 2 + size / 8;
        int cy = size / 2 + size / 8;
        for (int i = 0; i < size / 3; i++)
        {
            img.SetPixel(cx - i, cy - i, 0xFFEF4444); // Grafite/Lápis
        }

        return img;
    }

    /// <summary>
    /// Ícone PaintPad / Fotos: Paleta de cores com pincel e tintas arco-íris.
    /// </summary>
    public static IconImage CreatePaintPadIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(248, 250, 252), Color.FromArgb(226, 232, 240));
        int cx = size / 2;
        int cy = size / 2;

        // Paleta oval em tom madeira suave
        int pr = size * 35 / 100;
        for (int dy = -pr; dy <= pr; dy++)
        {
            int py = cy + dy;
            int dxLim = (int)Math.Sqrt(Math.Max(0, pr * pr - dy * dy));
            for (int dx = -dxLim; dx <= dxLim; dx++)
            {
                img.SetPixel(cx + dx, py, 0xFFFDE68A);
            }
        }

        // Gotas de tinta coloridas na paleta
        uint[] colors = new uint[] { 0xFFEF4444, 0xFFF59E0B, 0xFF10B981, 0xFF3B82F6, 0xFFA855F7 };
        int[,] offsets = new int[,] { { -2, -2 }, { 0, -3 }, { 2, -2 }, { 3, 0 }, { 1, 2 } };

        int dotStep = Math.Max(2, size / 8);
        for (int i = 0; i < 5; i++)
        {
            int dotX = cx + offsets[i, 0] * dotStep / 2;
            int dotY = cy + offsets[i, 1] * dotStep / 2;
            if (dotX >= 2 && dotX < size - 2 && dotY >= 2 && dotY < size - 2)
            {
                img.SetPixel(dotX, dotY, colors[i]);
                img.SetPixel(dotX + 1, dotY, colors[i]);
                img.SetPixel(dotX, dotY + 1, colors[i]);
            }
        }

        return img;
    }

    /// <summary>
    /// Ícone Mídia / Safari: Bússola azul com agulha direcional vermelha e branca.
    /// </summary>
    public static IconImage CreateMediaSafariIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(14, 165, 233), Color.FromArgb(2, 132, 199));
        int cx = size / 2;
        int cy = size / 2;
        int r = size * 36 / 100;
        int r2 = r * r;

        // Anel circular da bússola
        for (int y = cy - r; y <= cy + r; y++)
        {
            int dy = y - cy;
            for (int x = cx - r; x <= cx + r; x++)
            {
                int dx = x - cx;
                int dist = dx * dx + dy * dy;
                if (dist <= r2 && dist >= (r - 2) * (r - 2))
                {
                    img.SetPixel(x, y, 0xFFFFFFFF);
                }
                else if (dist < (r - 2) * (r - 2))
                {
                    img.SetPixel(x, y, 0xFF0C4A6E);
                }
            }
        }

        // Agulha vermelha (Norte)
        for (int i = 0; i <= r - 3; i++)
        {
            img.SetPixel(cx + i / 2, cy - i, 0xFFEF4444);
            img.SetPixel(cx - i / 2, cy - i, 0xFFEF4444);
        }

        // Agulha branca (Sul)
        for (int i = 0; i <= r - 3; i++)
        {
            img.SetPixel(cx + i / 2, cy + i, 0xFFFFFFFF);
            img.SetPixel(cx - i / 2, cy + i, 0xFFFFFFFF);
        }

        // Pino central
        img.SetPixel(cx, cy, 0xFFE2E8F0);

        return img;
    }

    /// <summary>
    /// Ícone Ajustes / Preferências do Sistema: Engrenagens metálicas entrelaçadas.
    /// </summary>
    public static IconImage CreateControlPanelIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(226, 232, 240), Color.FromArgb(148, 163, 184));
        int cx = size / 2;
        int cy = size / 2;
        int r = size * 30 / 100;

        for (int dy = -r; dy <= r; dy++)
        {
            int py = cy + dy;
            int dxLim = (int)Math.Sqrt(Math.Max(0, r * r - dy * dy));
            for (int dx = -dxLim; dx <= dxLim; dx++)
            {
                img.SetPixel(cx + dx, py, 0xFF475569);
            }
        }

        // Dentes da engrenagem
        int toothLen = Math.Max(2, size / 8);
        for (int i = -2; i <= 2; i++)
        {
            img.SetPixel(cx + i, cy - r - 1, 0xFF475569);
            img.SetPixel(cx + i, cy + r + 1, 0xFF475569);
            img.SetPixel(cx - r - 1, cy + i, 0xFF475569);
            img.SetPixel(cx + r + 1, cy + i, 0xFF475569);
        }

        // Orifício central da engrenagem
        int holeR = Math.Max(1, size / 8);
        for (int dy = -holeR; dy <= holeR; dy++)
        {
            for (int dx = -holeR; dx <= holeR; dx++)
            {
                if (dx * dx + dy * dy <= holeR * holeR)
                {
                    img.SetPixel(cx + dx, cy + dy, 0xFFCBD5E1);
                }
            }
        }

        return img;
    }

    /// <summary>
    /// Ícone Calculadora: Teclas redondas com operações matemáticas.
    /// </summary>
    public static IconImage CreateCalculatorIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(39, 39, 42), Color.FromArgb(24, 24, 27));
        int r = Math.Max(2, size * 20 / 100);

        // Display de resultados no topo
        int dispY = r + 1;
        int dispH = Math.Max(3, size / 6);
        for (int y = dispY; y < dispY + dispH; y++)
        {
            for (int x = r + 2; x < size - r - 2; x++)
            {
                img.SetPixel(x, y, 0xFF52525B);
            }
        }

        // Teclas circulares laranjas e cinzas
        int keyY = dispY + dispH + 3;
        int keyStep = Math.Max(3, size / 6);
        for (int ky = 0; ky < 3; ky++)
        {
            for (int kx = 0; kx < 3; kx++)
            {
                int kpx = r + 4 + kx * keyStep;
                int kpy = keyY + ky * keyStep;
                if (kpx < size - r - 2 && kpy < size - r - 2)
                {
                    uint kcol = (kx == 2 || ky == 2) ? 0xFFF97316 : 0xFF71717A;
                    img.SetPixel(kpx, kpy, kcol);
                    img.SetPixel(kpx + 1, kpy, kcol);
                    img.SetPixel(kpx, kpy + 1, kcol);
                }
            }
        }

        return img;
    }

    /// <summary>
    /// Ícone Planilha: Grid de tabela em squircle esmeralda.
    /// </summary>
    public static IconImage CreateSpreadsheetIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(16, 185, 129), Color.FromArgb(5, 150, 105));
        int r = Math.Max(2, size * 20 / 100);

        int gx = r + 3;
        int gy = r + 3;
        int gw = size - (r * 2) - 6;
        int gh = size - (r * 2) - 6;

        // Fundo da tabela branco
        for (int y = gy; y < gy + gh; y++)
        {
            for (int x = gx; x < gx + gw; x++)
            {
                img.SetPixel(x, y, 0xFFFFFFFF);
            }
        }

        // Cabeçalho verde da planilha
        for (int y = gy; y < gy + Math.Max(3, gh / 4); y++)
        {
            for (int x = gx; x < gx + gw; x++)
            {
                img.SetPixel(x, y, 0xFF047857);
            }
        }

        // Linhas de grade
        for (int y = gy; y < gy + gh; y += Math.Max(3, gh / 4))
        {
            for (int x = gx; x < gx + gw; x++) img.SetPixel(x, y, 0xFF10B981);
        }
        for (int x = gx; x < gx + gw; x += Math.Max(3, gw / 3))
        {
            for (int y = gy; y < gy + gh; y++) img.SetPixel(x, y, 0xFF10B981);
        }

        return img;
    }

    /// <summary>
    /// Ícone Terminal: Console minimalista escuro com prompt neon verde '>_'.
    /// </summary>
    public static IconImage CreateTerminalIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(24, 24, 27), Color.FromArgb(9, 9, 11));
        int r = Math.Max(2, size * 20 / 100);

        // Barra superior da mini-janela
        for (int x = r + 2; x < size - r - 2; x++)
        {
            img.SetPixel(x, r + 2, 0xFF3F3F46);
        }

        // Seta prompt '>' em verde neon
        int py = size / 2;
        int px = r + 4;
        img.SetPixel(px, py - 2, 0xFF22C55E);
        img.SetPixel(px + 1, py - 1, 0xFF22C55E);
        img.SetPixel(px + 2, py, 0xFF22C55E);
        img.SetPixel(px + 1, py + 1, 0xFF22C55E);
        img.SetPixel(px, py + 2, 0xFF22C55E);

        // Cursor '_'
        img.SetPixel(px + 5, py + 2, 0xFFFFFFFF);
        img.SetPixel(px + 6, py + 2, 0xFFFFFFFF);
        img.SetPixel(px + 7, py + 2, 0xFFFFFFFF);

        return img;
    }

    /// <summary>
    /// Ícone CentralLogic / Jogos: Joystick retrô estilo console.
    /// </summary>
    public static IconImage CreateCentralLogicIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(168, 85, 247), Color.FromArgb(126, 34, 206));
        int cx = size / 2;
        int cy = size / 2;

        // D-Pad preto
        for (int i = -3; i <= 3; i++)
        {
            img.SetPixel(cx + i, cy, 0xFF18181B);
            img.SetPixel(cx, cy + i, 0xFF18181B);
        }

        // Botões coloridos (A/B/X/Y)
        int btnOff = Math.Max(3, size / 5);
        img.SetPixel(cx + btnOff, cy - 2, 0xFFEF4444);
        img.SetPixel(cx + btnOff + 2, cy, 0xFF3B82F6);
        img.SetPixel(cx + btnOff, cy + 2, 0xFFEAB308);

        return img;
    }

    /// <summary>
    /// Ícone Instalar no HD: Disco rígido externo de alumínio com seta de download esmeralda.
    /// </summary>
    public static IconImage CreateInstallerIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(56, 189, 248), Color.FromArgb(3, 105, 161));
        int cx = size / 2;
        int cy = size / 2;

        // Corpo do disco (cinza aço)
        int dw = size * 55 / 100;
        int dh = size * 30 / 100;
        for (int dy = -dh / 2; dy <= dh / 2; dy++)
        {
            for (int dx = -dw / 2; dx <= dw / 2; dx++)
            {
                img.SetPixel(cx + dx, cy + dy, 0xFF334155);
            }
        }
        // LED indicador verde
        img.SetPixel(cx + dw / 2 - 2, cy, 0xFF22C55E);

        // Seta de Instalação para baixo
        int arrowH = size / 4;
        for (int y = cy - arrowH; y <= cy; y++)
        {
            img.SetPixel(cx, y, 0xFFFFFFFF);
            img.SetPixel(cx - 1, y, 0xFFFFFFFF);
        }
        for (int r = 0; r <= 3; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                img.SetPixel(cx + dx, cy + r, 0xFF10B981);
            }
        }

        return img;
    }

    /// <summary>
    /// Ícone Sobre o NybuOS: Brasão monograma "N" em degradê violeta e azul safira.
    /// </summary>

    /// <summary>
    /// Ícone NybuOS Update: Gradiente violeta e anel de sincronização circular com seta de download.
    /// </summary>
    public static IconImage CreateUpdaterIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(79, 70, 229), Color.FromArgb(124, 58, 237));
        int cx = size / 2;
        int cy = size / 2;

        int radius = size * 28 / 100;
        for (int angle = 20; angle <= 340; angle += 6)
        {
            double rad = angle * 3.14159265 / 180.0;
            int px = cx + (int)(radius * Math.Cos(rad));
            int py = cy + (int)(radius * Math.Sin(rad));
            img.SetPixel(px, py, 0xFFFFFFFF);
            img.SetPixel(px + 1, py, 0xFFFFFFFF);
        }

        int arrowH = size * 24 / 100;
        for (int y = cy - arrowH / 2; y <= cy + arrowH / 2; y++)
        {
            img.SetPixel(cx, y, 0xFFFFFFFF);
            img.SetPixel(cx - 1, y, 0xFFFFFFFF);
        }
        for (int d = 1; d <= 4; d++)
        {
            img.SetPixel(cx - d, cy + arrowH / 2 - d, 0xFF38BDF8);
            img.SetPixel(cx + d, cy + arrowH / 2 - d, 0xFF38BDF8);
            img.SetPixel(cx - 1 - d, cy + arrowH / 2 - d, 0xFF38BDF8);
        }

        return img;
    }

    public static IconImage CreateDownloadCentralIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(147, 51, 234), Color.FromArgb(88, 28, 135));
        int cx = size / 2;
        int cy = size / 2;

        int trayW = size * 48 / 100;
        int trayY = cy + (size * 18 / 100);
        int trayH = Math.Max(2, size * 10 / 100);

        for (int x = cx - trayW / 2; x <= cx + trayW / 2; x++)
        {
            img.SetPixel(x, trayY, 0xFFFFFFFF);
            img.SetPixel(x, trayY + 1, 0xFFE9D5FF);
        }
        for (int y = trayY - trayH; y <= trayY; y++)
        {
            img.SetPixel(cx - trayW / 2, y, 0xFFFFFFFF);
            img.SetPixel(cx - trayW / 2 + 1, y, 0xFFFFFFFF);
            img.SetPixel(cx + trayW / 2, y, 0xFFFFFFFF);
            img.SetPixel(cx + trayW / 2 - 1, y, 0xFFFFFFFF);
        }

        int arrowTop = cy - (size * 25 / 100);
        int arrowBottom = cy + (size * 8 / 100);
        for (int y = arrowTop; y <= arrowBottom; y++)
        {
            img.SetPixel(cx - 1, y, 0xFFFFFFFF);
            img.SetPixel(cx, y, 0xFFFFFFFF);
            img.SetPixel(cx + 1, y, 0xFFFFFFFF);
        }

        int headSize = Math.Max(3, size * 16 / 100);
        for (int d = 1; d <= headSize; d++)
        {
            for (int w = 0; w <= d; w++)
            {
                img.SetPixel(cx - w, arrowBottom - (headSize - d), 0xFF38BDF8);
                img.SetPixel(cx + w, arrowBottom - (headSize - d), 0xFF38BDF8);
            }
        }

        return img;
    }

    public static IconImage CreateAboutIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(99, 102, 241), Color.FromArgb(67, 56, 202));
        int cx = size / 2;
        int cy = size / 2;
        int h = size / 3;

        // "N" estilizado e imponente
        int nx = cx - h / 2;
        int ny = cy - h / 2;
        for (int i = 0; i <= h; i++)
        {
            img.SetPixel(nx, ny + i, 0xFFFFFFFF);
            img.SetPixel(nx + 1, ny + i, 0xFFFFFFFF);
            img.SetPixel(nx + h, ny + i, 0xFFFFFFFF);
            img.SetPixel(nx + h - 1, ny + i, 0xFFFFFFFF);

            int diagX = nx + (i * (h - 2)) / h;
            img.SetPixel(diagX, ny + i, 0xFF67E8F9);
            img.SetPixel(diagX + 1, ny + i, 0xFF67E8F9);
        }

        return img;
    }

    /// <summary>
    /// Ícone Lixeira / Trash: Cesto de metal e papel amassado translúcido.
    /// </summary>
    public static IconImage CreateTrashIcon(int size)
    {
        var img = new IconImage(size, size);
        int cx = size / 2;
        int cy = size / 2;
        int w = size * 45 / 100;
        int h = size * 55 / 100;

        // Base do cesto
        int bottomY = cy + h / 2;
        int topY = cy - h / 2 + 4;

        for (int y = topY; y <= bottomY; y++)
        {
            int rowW = w - (bottomY - y) * 2 / 5;
            for (int dx = -rowW / 2; dx <= rowW / 2; dx++)
            {
                uint col = (dx == -rowW / 2 || dx == rowW / 2 || y == bottomY) ? 0xFF94A3B8 : 0xAA475569;
                img.SetPixel(cx + dx, y, col);
            }
        }

        // Papel amassado no topo da lixeira
        for (int dy = -3; dy <= 0; dy++)
        {
            for (int dx = -w / 3; dx <= w / 3; dx++)
            {
                img.SetPixel(cx + dx, topY + dy, 0xFFF1F5F9);
            }
        }

        return img;
    }

    /// <summary>
    /// Ícone Desligar / Power: Círculo vermelho carmim com símbolo de energia.
    /// </summary>
    public static IconImage CreatePowerIcon(int size)
    {
        var img = CreateSquircleBase(size, Color.FromArgb(239, 68, 68), Color.FromArgb(185, 28, 28));
        int cx = size / 2;
        int cy = size / 2;
        int r = size * 28 / 100;

        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                int dist = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (dist <= r * r && dist >= (r - 2) * (r - 2))
                {
                    if (y >= cy - 2 || Math.Abs(x - cx) >= 2)
                    {
                        img.SetPixel(x, y, 0xFFFFFFFF);
                    }
                }
            }
        }

        // Haste vertical superior
        for (int y = cy - r - 1; y <= cy; y++)
        {
            img.SetPixel(cx, y, 0xFFFFFFFF);
            img.SetPixel(cx - 1, y, 0xFFFFFFFF);
        }

        return img;
    }

    /// <summary>
    /// Logotipo Apple/NybuOS para a barra superior e telas de boot.
    /// </summary>
    public static IconImage CreateAppleNybuLogo(int size)
    {
        var img = new IconImage(size, size);
        int cx = size / 2;
        int cy = size / 2;
        int r = size / 2 - 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int distSq = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (distSq <= r * r)
                {
                    img.SetPixel(x, y, 0xFF242730);
                }
            }
        }

        // "N" vazado no centro do emblema
        int nx = cx - size / 5;
        int ny = cy - size / 4;
        int nh = size / 2;
        for (int i = 0; i <= nh; i++)
        {
            img.SetPixel(nx, ny + i, 0xFFFFFFFF);
            img.SetPixel(nx + size * 2 / 5, ny + i, 0xFFFFFFFF);
            int diag = nx + (i * (size * 2 / 5)) / nh;
            img.SetPixel(diag, ny + i, 0xFFFFFFFF);
        }

        return img;
    }

    public static IconImage CreateWifiIcon(int size)
    {
        var img = new IconImage(size, size);
        int cx = size / 2;
        int cy = size - 3;
        uint col = 0xFF242730;

        // Ponto base
        img.SetPixel(cx, cy, col);
        img.SetPixel(cx - 1, cy, col);

        // Arcos de sinal
        for (int dx = -2; dx <= 2; dx++) img.SetPixel(cx + dx, cy - 3, col);
        for (int dx = -4; dx <= 4; dx++) img.SetPixel(cx + dx, cy - 6, col);
        for (int dx = -6; dx <= 6; dx++) img.SetPixel(cx + dx, cy - 9, col);

        return img;
    }

    public static IconImage CreateVolumeIcon(int size)
    {
        var img = new IconImage(size, size);
        int cx = size / 2;
        int cy = size / 2;
        uint col = 0xFF242730;

        // Cone do alto-falante
        for (int y = cy - 3; y <= cy + 3; y++)
        {
            img.SetPixel(cx - 4, y, col);
            img.SetPixel(cx - 3, y, col);
        }
        for (int r = 0; r <= 4; r++)
        {
            img.SetPixel(cx - 2 + r, cy - r, col);
            img.SetPixel(cx - 2 + r, cy + r, col);
        }

        // Ondas sonoras
        img.SetPixel(cx + 4, cy - 2, col);
        img.SetPixel(cx + 5, cy, col);
        img.SetPixel(cx + 4, cy + 2, col);

        return img;
    }

    public static IconImage CreateBatteryIcon(int size)
    {
        var img = new IconImage(size, 10);
        int w = size;
        int h = 10;
        uint col = 0xFF242730;

        // Contorno da bateria
        for (int x = 0; x < w - 2; x++)
        {
            img.SetPixel(x, 0, col);
            img.SetPixel(x, h - 1, col);
        }
        for (int y = 0; y < h; y++)
        {
            img.SetPixel(0, y, col);
            img.SetPixel(w - 3, y, col);
        }
        // Polo positivo
        img.SetPixel(w - 1, 3, col);
        img.SetPixel(w - 1, 4, col);
        img.SetPixel(w - 1, 5, col);
        img.SetPixel(w - 1, 6, col);

        // Carga verde (100%)
        for (int y = 2; y <= h - 3; y++)
        {
            for (int x = 2; x < w - 5; x++)
            {
                img.SetPixel(x, y, 0xFF16A34A);
            }
        }

        return img;
    }

    public static IconImage CreateSearchIcon(int size)
    {
        var img = new IconImage(size, size);
        int cx = size / 2 - 2;
        int cy = size / 2 - 2;
        int r = Math.Max(2, size / 3);
        uint col = 0xFF242730;

        // Anel da lupa
        for (int y = -r; y <= r; y++)
        {
            for (int x = -r; x <= r; x++)
            {
                int d = x * x + y * y;
                if (d <= r * r && d >= (r - 1) * (r - 1))
                {
                    img.SetPixel(cx + x, cy + y, col);
                }
            }
        }

        // Cabo diagonal da lupa
        for (int i = 0; i <= size / 3; i++)
        {
            img.SetPixel(cx + r - 1 + i, cy + r - 1 + i, col);
            img.SetPixel(cx + r + i, cy + r - 1 + i, col);
        }

        return img;
    }

    public static IconImage CreateControlCenterIcon(int size)
    {
        var img = new IconImage(size, size);
        int cy = size / 2;
        uint col = 0xFF242730;

        // Dois sliders estilo Central de Controle do macOS
        for (int x = 2; x < size - 2; x++)
        {
            img.SetPixel(x, cy - 3, 0xFFCBD5E1);
            img.SetPixel(x, cy + 3, 0xFFCBD5E1);
        }
        // Botão slider 1
        img.SetPixel(4, cy - 3, col);
        img.SetPixel(5, cy - 3, col);
        // Botão slider 2
        img.SetPixel(size - 6, cy + 3, col);
        img.SetPixel(size - 5, cy + 3, col);

        return img;
    }
}
