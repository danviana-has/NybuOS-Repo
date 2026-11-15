using System;
using Cosmos.Kernel.System.Storage;
using NybuOS.Core.FileSystem;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;

namespace NybuOS.Apps;

public class AboutApp : AppBase
{
    public override string Id => "about";
    public override string Title => "Sobre o NybuOS";
    public override FastBitmap? Icon16 => AssetManager.IconAboutApp16;
    public override FastBitmap? Icon24 => AssetManager.IconAboutApp24;
    public override FastBitmap? Icon48 => AssetManager.IconAboutApp48;

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, ColorScheme.WindowBody);

        // Header Banner with Accent Color
        int bannerH = 70;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, bannerH, ColorScheme.Accent);

        // Icon in Header
        if (AssetManager.IconAboutApp48 != null)
        {
            FastRenderer.DrawBitmap(buffer, screenW, screenH, AssetManager.IconAboutApp48, clientX + 16, clientY + 11);
        }

        // Title and Subtitle in Header
        FontRenderer.DrawText(buffer, screenW, screenH, "NybuOS — Versão V Lagoons", clientX + 76, clientY + 14, ColorScheme.TextWhite, bold: true, size: 18);
        FontRenderer.DrawText(buffer, screenW, screenH, "Sistema Operacional Bare-Metal de Alta Performance", clientX + 76, clientY + 40, 0xD0FFFFFF, bold: false, size: 12);

        // Information Sections (Windows 10 Settings Style)
        int curY = clientY + bannerH + 20;
        int leftCol = clientX + 24;

        DrawSectionHeader(buffer, screenW, screenH, "Especificações do Sistema", leftCol, curY);
        curY += 28;

        DrawSpecRow(buffer, screenW, screenH, "Edição:", "NybuOS V Lagoons (Edição Bare-Metal)", leftCol, curY);
        curY += 22;
        DrawSpecRow(buffer, screenW, screenH, "Arquitetura:", "x86_64 (64-bit AMD64 / Intel 64)", leftCol, curY);
        curY += 22;
        DrawSpecRow(buffer, screenW, screenH, "Kernel:", "Cosmos OS Gen 3 (Build 3.0.97)", leftCol, curY);
        curY += 22;
        DrawSpecRow(buffer, screenW, screenH, "Compilador & Runtime:", ".NET 10 com NativeAOT (Bare-Metal)", leftCol, curY);
        curY += 30;

        DrawSectionHeader(buffer, screenW, screenH, "Subsistemas & Memória", leftCol, curY);
        curY += 28;

        string vfsStatus = FileService.IsFatMounted ? "FAT32 Montado com Sucesso (0:\\)" : "FAT32 Simulado / VFS em Inicialização";
        DrawSpecRow(buffer, screenW, screenH, "Sistema de Arquivos:", vfsStatus, leftCol, curY);
        curY += 22;

        long memAlloc = GC.GetTotalMemory(false) / 1024;
        DrawSpecRow(buffer, screenW, screenH, "Memória Gerenciada:", $"{memAlloc} KB alocados pelo GC", leftCol, curY);
        curY += 22;

        DrawSpecRow(buffer, screenW, screenH, "Gráficos & Renderização:", "Double-Buffering Otimizado (0 alocações no loop)", leftCol, curY);
        curY += 22;

        DrawSpecRow(buffer, screenW, screenH, "Motor Tipográfico:", "LunarFonts TrueType C# (Segoe UI)", leftCol, curY);
        curY += 32;

        // Bottom Copyright card
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX + 20, curY, clientW - 40, ColorScheme.WindowBorder);
        curY += 12;
        FontRenderer.DrawText(buffer, screenW, screenH, "© 2026 NybuOS Team. Desenvolvido para desempenho bare-metal extremo.", leftCol, curY, ColorScheme.TextMuted, bold: false, size: 12);
    }

    private void DrawSectionHeader(uint[] buffer, int screenW, int screenH, string title, int x, int y)
    {
        FontRenderer.DrawText(buffer, screenW, screenH, title, x, y, ColorScheme.Accent, bold: true, size: 14);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, x, y + 20, 360, ColorScheme.ControlHover);
    }

    private void DrawSpecRow(uint[] buffer, int screenW, int screenH, string label, string value, int x, int y)
    {
        FontRenderer.DrawText(buffer, screenW, screenH, label, x, y, ColorScheme.TextSecondary, bold: true, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, value, x + 160, y, ColorScheme.TextPrimary, bold: false, size: 12);
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH) { }
}
