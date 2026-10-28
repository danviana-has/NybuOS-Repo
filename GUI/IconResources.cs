using System;
using System.Drawing;

namespace NybuOS.GUI;

/// <summary>
/// Gerador procedural de ícones modernos e minimalistas (estilo COSMIC / Flat UI)
/// Substitui completamente ícones .ICO com renderização procedural vetorial nítida.
/// </summary>
public static class IconResources
{
    // Ícones de 24x24 para Barra de Tarefas e Menus
    public static IconImage StartButtonIcon { get; private set; } = null!;
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
    public static IconImage Help16 { get; private set; } = null!;

    // Ícones de 32x32 para Área de Trabalho
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
    public static IconImage Recycle32 { get; private set; } = null!;

    // Logo especial de 64x64 para o Splash Boot Screen
    public static IconImage BootLogo64 { get; private set; } = null!;

    public static void Initialize()
    {
        StartButtonIcon = CreateStartLogo(24);
        FileManagerIcon = CreateFolderIcon(24, Color.FromArgb(168, 85, 247));
        TextEditorIcon = CreateDocumentIcon(24, Color.FromArgb(59, 130, 246));
        PaintPadIcon = CreatePaletteIcon(24, Color.FromArgb(236, 72, 153));
        MediaIcon = CreateMediaIcon(24, Color.FromArgb(249, 115, 22));
        ControlPanelIcon = CreateGearIcon(24, Color.FromArgb(14, 165, 233));
        CentralLogicIcon = CreateGameIcon(24, Color.FromArgb(34, 197, 94));
        SpreadsheetIcon = CreateSpreadsheetIcon(24, Color.FromArgb(16, 185, 129));
        CalculatorIcon = CreateCalcIcon(24, Color.FromArgb(245, 158, 11));
        TerminalIcon = CreateTerminalIcon(24, Color.FromArgb(100, 116, 139));
        InstallerIcon = CreateInstallerIcon(24, Color.FromArgb(16, 185, 129));
        AboutIcon = CreateInfoIcon(24, Color.FromArgb(99, 102, 241));
        ShutdownIcon = CreatePowerIcon(24, Color.FromArgb(239, 68, 68));

        // 16x16
        FolderClosed16 = CreateFolderIcon(16, Color.FromArgb(168, 85, 247));
        FolderOpen16 = CreateFolderIcon(16, Color.FromArgb(192, 132, 252));
        Doc16 = CreateDocumentIcon(16, Color.FromArgb(59, 130, 246));
        Drive16 = CreateDriveIcon(16, Color.FromArgb(148, 163, 184));
        Computer16 = CreateComputerIcon(16, Color.FromArgb(59, 130, 246));
        Media16 = CreateMediaIcon(16, Color.FromArgb(249, 115, 22));
        ControlPanel16 = CreateGearIcon(16, Color.FromArgb(14, 165, 233));
        Calculator16 = CreateCalcIcon(16, Color.FromArgb(245, 158, 11));
        Terminal16 = CreateTerminalIcon(16, Color.FromArgb(100, 116, 139));
        PaintPad16 = CreatePaletteIcon(16, Color.FromArgb(236, 72, 153));
        Minesweeper16 = CreateGameIcon(16, Color.FromArgb(34, 197, 94));
        Spreadsheet16 = CreateSpreadsheetIcon(16, Color.FromArgb(16, 185, 129));
        Installer16 = CreateInstallerIcon(16, Color.FromArgb(16, 185, 129));
        Help16 = CreateInfoIcon(16, Color.FromArgb(99, 102, 241));

        // 32x32 Desktop
        Computer32 = CreateComputerIcon(32, Color.FromArgb(59, 130, 246));
        FolderClosed32 = CreateFolderIcon(32, Color.FromArgb(168, 85, 247));
        FileManager32 = CreateFolderIcon(32, Color.FromArgb(168, 85, 247));
        TextEditor32 = CreateDocumentIcon(32, Color.FromArgb(59, 130, 246));
        Doc32 = CreateDocumentIcon(32, Color.FromArgb(59, 130, 246));
        PaintPad32 = CreatePaletteIcon(32, Color.FromArgb(236, 72, 153));
        Media32 = CreateMediaIcon(32, Color.FromArgb(249, 115, 22));
        ControlPanel32 = CreateGearIcon(32, Color.FromArgb(14, 165, 233));
        CentralLogic32 = CreateGameIcon(32, Color.FromArgb(34, 197, 94));
        Minesweeper32 = CreateGameIcon(32, Color.FromArgb(34, 197, 94));
        Spreadsheet32 = CreateSpreadsheetIcon(32, Color.FromArgb(16, 185, 129));
        Calculator32 = CreateCalcIcon(32, Color.FromArgb(245, 158, 11));
        Terminal32 = CreateTerminalIcon(32, Color.FromArgb(100, 116, 139));
        Installer32 = CreateInstallerIcon(32, Color.FromArgb(16, 185, 129));
        Recycle32 = CreateRecycleIcon(32, Color.FromArgb(75, 85, 99));

        // Logo especial de Boot Splash
        BootLogo64 = CreateStartLogo(64);
    }

    private static IconImage CreateStartLogo(int size)
    {
        var img = new IconImage(size, size);
        int r = size / 2 - 2;
        int cx = size / 2;
        int cy = size / 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                int distSq = dx * dx + dy * dy;
                if (distSq <= r * r && distSq >= (r - 2) * (r - 2))
                {
                    img.SetPixel(x, y, 0xFFFFFFFF);
                }
                else if (distSq < (r - 2) * (r - 2))
                {
                    img.SetPixel(x, y, 0xFF252530);
                }
            }
        }

        int nx = cx - 4;
        int ny = cy - 5;
        for (int h = 0; h < 10; h++)
        {
            img.SetPixel(nx, ny + h, 0xFF3B82F6);
            img.SetPixel(nx + 1, ny + h, 0xFF3B82F6);
            img.SetPixel(nx + 7, ny + h, 0xFF3B82F6);
            img.SetPixel(nx + 8, ny + h, 0xFF3B82F6);
            int diag = nx + (h * 7) / 10;
            img.SetPixel(diag, ny + h, 0xFF60A5FA);
        }

        return img;
    }

    private static IconImage CreateFolderIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        uint tabCol = (uint)Color.FromArgb(accent.R > 30 ? accent.R - 30 : 0, accent.G > 30 ? accent.G - 30 : 0, accent.B).ToArgb();

        int tabW = size * 4 / 10;
        int tabH = Math.Max(2, size / 5);
        for (int y = size / 5; y < size / 5 + tabH; y++)
            for (int x = size / 6; x < size / 6 + tabW; x++)
                img.SetPixel(x, y, tabCol);

        int bodyY = size / 5 + tabH - 1;
        int bodyH = size * 6 / 10;
        int bodyW = size * 7 / 10;
        for (int y = bodyY; y < bodyY + bodyH; y++)
        {
            for (int x = size / 6; x < size / 6 + bodyW; x++)
            {
                img.SetPixel(x, y, col);
            }
        }
        return img;
    }

    private static IconImage CreateDocumentIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int margin = size / 6;
        int w = size - margin * 2;
        int h = size - margin * 2;

        for (int y = margin; y < margin + h; y++)
            for (int x = margin; x < margin + w; x++)
                img.SetPixel(x, y, col);

        for (int line = 0; line < 3; line++)
        {
            int ly = margin + 3 + (line * (h / 4));
            if (ly < margin + h - 2)
            {
                for (int lx = margin + 2; lx < margin + w - 2; lx++)
                {
                    img.SetPixel(lx, ly, 0xFFFFFFFF);
                }
            }
        }
        return img;
    }

    private static IconImage CreatePaletteIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int cx = size / 2;
        int cy = size / 2;
        int r = size * 4 / 10;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                if (dx * dx + dy * dy <= r * r)
                {
                    img.SetPixel(x, y, col);
                }
            }
        }

        uint[] dots = new uint[] { 0xFFFF0000, 0xFF00FF00, 0xFF0080FF, 0xFFFFFF00 };
        int[,] offsets = new int[,] { { -2, -2 }, { 2, -2 }, { -2, 2 }, { 2, 2 } };
        for (int i = 0; i < 4; i++)
        {
            int px = cx + offsets[i, 0] * (size / 8);
            int py = cy + offsets[i, 1] * (size / 8);
            if (px >= 0 && px < size && py >= 0 && py < size)
                img.SetPixel(px, py, dots[i]);
        }
        return img;
    }

    private static IconImage CreateMediaIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int margin = size / 6;

        for (int y = margin; y < size - margin; y++)
            for (int x = margin; x < size - margin; x++)
                img.SetPixel(x, y, col);

        int triX = size * 4 / 10;
        int triY = size * 3 / 10;
        int triH = size * 4 / 10;

        for (int row = 0; row < triH; row++)
        {
            int len = (row <= triH / 2) ? row : (triH - row);
            for (int colX = 0; colX <= len; colX++)
            {
                img.SetPixel(triX + colX, triY + row, 0xFFFFFFFF);
            }
        }
        return img;
    }

    private static IconImage CreateGearIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int cx = size / 2;
        int cy = size / 2;
        int r = size * 4 / 10;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int distSq = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (distSq <= r * r && distSq >= (r / 2) * (r / 2))
                {
                    img.SetPixel(x, y, col);
                }
            }
        }
        return img;
    }

    private static IconImage CreateGameIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int margin = size / 6;

        for (int y = margin + 2; y < size - margin - 2; y++)
            for (int x = margin; x < size - margin; x++)
                img.SetPixel(x, y, col);

        int cx = size / 2 - 3;
        int cy = size / 2;
        for (int i = -2; i <= 2; i++)
        {
            if (cx + i >= 0 && cx + i < size) img.SetPixel(cx + i, cy, 0xFFFFFFFF);
            if (cy + i >= 0 && cy + i < size) img.SetPixel(cx, cy + i, 0xFFFFFFFF);
        }
        return img;
    }

    private static IconImage CreateSpreadsheetIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int margin = size / 6;

        for (int y = margin; y < size - margin; y++)
            for (int x = margin; x < size - margin; x++)
                img.SetPixel(x, y, col);

        int midX = size / 2;
        int midY = size / 2;
        for (int y = margin; y < size - margin; y++) img.SetPixel(midX, y, 0xFFFFFFFF);
        for (int x = margin; x < size - margin; x++) img.SetPixel(x, midY, 0xFFFFFFFF);

        return img;
    }

    private static IconImage CreateCalcIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int margin = size / 6;

        for (int y = margin; y < size - margin; y++)
            for (int x = margin; x < size - margin; x++)
                img.SetPixel(x, y, col);

        for (int x = margin + 2; x < size - margin - 2; x++)
            img.SetPixel(x, margin + 2, 0xFFFFFFFF);

        int[] dots = new int[] { margin + 3, margin + 7 };
        foreach (int dy in dots)
        {
            foreach (int dx in dots)
            {
                if (dx < size && dy + 5 < size)
                    img.SetPixel(dx, dy + 5, 0xFFFFFFFF);
            }
        }
        return img;
    }

    private static IconImage CreateTerminalIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        int margin = size / 6;

        for (int y = margin; y < size - margin; y++)
            for (int x = margin; x < size - margin; x++)
                img.SetPixel(x, y, 0xFF1E293B);

        int py = size / 2 - 1;
        if (margin + 4 < size && py + 2 < size && py - 2 >= 0)
        {
            img.SetPixel(margin + 2, py - 2, 0xFF22C55E);
            img.SetPixel(margin + 3, py - 1, 0xFF22C55E);
            img.SetPixel(margin + 4, py, 0xFF22C55E);
            img.SetPixel(margin + 3, py + 1, 0xFF22C55E);
            img.SetPixel(margin + 2, py + 2, 0xFF22C55E);

            if (margin + 7 < size)
            {
                img.SetPixel(margin + 6, py + 2, 0xFFFFFFFF);
                img.SetPixel(margin + 7, py + 2, 0xFFFFFFFF);
            }
        }
        return img;
    }

    private static IconImage CreateInfoIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int cx = size / 2;
        int cy = size / 2;
        int r = size * 4 / 10;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    img.SetPixel(x, y, col);
            }
        }
        img.SetPixel(cx, cy - 3, 0xFFFFFFFF);
        for (int y = cy - 1; y <= cy + 3; y++) img.SetPixel(cx, y, 0xFFFFFFFF);

        return img;
    }

    private static IconImage CreatePowerIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int cx = size / 2;
        int cy = size / 2;
        int r = size * 4 / 10;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int distSq = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (distSq <= r * r && distSq >= (r - 2) * (r - 2))
                {
                    if (y >= cy - 2 || Math.Abs(x - cx) >= 2)
                        img.SetPixel(x, y, col);
                }
            }
        }
        for (int y = cy - r; y <= cy; y++) img.SetPixel(cx, y, col);

        return img;
    }

    private static IconImage CreateDriveIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int margin = size / 6;
        for (int y = margin + 3; y < size - margin - 3; y++)
            for (int x = margin; x < size - margin; x++)
                img.SetPixel(x, y, col);

        img.SetPixel(size - margin - 3, size / 2, 0xFF22C55E); // Led verde
        return img;
    }

    private static IconImage CreateComputerIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int margin = size / 6;

        // Monitor
        for (int y = margin; y < size - margin - 4; y++)
            for (int x = margin; x < size - margin; x++)
                img.SetPixel(x, y, col);

        // Tela interna escura
        for (int y = margin + 2; y < size - margin - 6; y++)
            for (int x = margin + 2; x < size - margin - 2; x++)
                img.SetPixel(x, y, 0xFF14141E);

        // Base
        int baseY = size - margin - 3;
        for (int x = margin + 4; x < size - margin - 4; x++)
            img.SetPixel(x, baseY, col);

        return img;
    }

    private static IconImage CreateRecycleIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int margin = size / 6;

        for (int y = margin + 4; y < size - margin; y++)
            for (int x = margin + 2; x < size - margin - 2; x++)
                img.SetPixel(x, y, col);

        for (int x = margin; x < size - margin; x++)
            img.SetPixel(x, margin + 2, col);

        return img;
    }

    private static IconImage CreateInstallerIcon(int size, Color accent)
    {
        var img = new IconImage(size, size);
        uint col = (uint)accent.ToArgb();
        int margin = size / 6;

        // Base do disco rígido
        for (int y = margin + 4; y < size - margin; y++)
            for (int x = margin; x < size - margin; x++)
                img.SetPixel(x, y, 0xFF334155);

        // Abertura / detalhe do disco
        for (int x = margin + 2; x < size - margin - 2; x++)
            img.SetPixel(x, size - margin - 2, 0xFF0F172A);

        // LED verde de leitura/escrita
        img.SetPixel(size - margin - 2, size - margin - 2, 0xFF22C55E);

        // Seta de Instalação (Download / Setup) para baixo no centro em cor de destaque
        int cx = size / 2;
        int topY = margin;
        int arrowH = size / 2;

        // Haste vertical da seta
        for (int y = topY; y < topY + arrowH; y++)
        {
            img.SetPixel(cx - 1, y, col);
            img.SetPixel(cx, y, col);
        }

        // Ponta da seta
        for (int r = 0; r <= 3; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                int py = topY + arrowH - 3 + r;
                if (py < size - margin - 3)
                    img.SetPixel(cx + dx, py, col);
            }
        }

        return img;
    }
}
