using System;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class SettingsApp : AppBase
{
    public override string Id => "settings";
    public override string Title => "Configurações";
    public override FastBitmap? Icon16 => AssetManager.IconSettings16;
    public override FastBitmap? Icon24 => AssetManager.IconSettings24;
    public override FastBitmap? Icon48 => AssetManager.IconSettings48;

    private int _selectedTab = 0; // 0 = Personalização, 1 = Tela, 2 = Som
    private readonly string[] _tabNames = { "Personalização", "Tela & Brilho", "Áudio & Som" };

    private readonly struct ColorOption
    {
        public readonly string Name;
        public readonly uint Color;
        public ColorOption(string name, uint color) { Name = name; Color = color; }
    }

    private readonly ColorOption[] _accentColors = {
        new ColorOption("Azul", ColorScheme.AccentBlue),
        new ColorOption("Laranja", ColorScheme.AccentOrange),
        new ColorOption("Roxo", ColorScheme.AccentPurple),
        new ColorOption("Verde", ColorScheme.AccentGreen),
        new ColorOption("Amarelo", ColorScheme.AccentYellow),
        new ColorOption("Vermelho", ColorScheme.AccentRed)
    };

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, ColorScheme.WindowBody);

        // 1. Sidebar Navigation (Left 160px)
        int navW = 160;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, navW, clientH, ColorScheme.BackgroundLight);
        FastRenderer.DrawVerticalLine(buffer, screenW, screenH, clientX + navW, clientY, clientH, ColorScheme.WindowBorder);

        FontRenderer.DrawText(buffer, screenW, screenH, "Configurações", clientX + 16, clientY + 16, ColorScheme.TextPrimary, bold: true, size: 16);

        int tabY = clientY + 54;
        for (int i = 0; i < _tabNames.Length; i++)
        {
            bool isSel = (i == _selectedTab);
            if (isSel)
            {
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 4, tabY, navW - 8, 32, ColorScheme.ControlBackground);
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 4, tabY, 4, 32, ColorScheme.Accent);
            }
            FontRenderer.DrawText(buffer, screenW, screenH, _tabNames[i], clientX + 16, tabY + 8, isSel ? ColorScheme.Accent : ColorScheme.TextPrimary, bold: isSel, size: 12);
            tabY += 36;
        }

        // 2. Content Area (Right)
        int contentX = clientX + navW + 24;
        int contentY = clientY + 20;

        if (_selectedTab == 0) // Personalização (Accent Colors)
        {
            FontRenderer.DrawText(buffer, screenW, screenH, "Cor de Destaque do Sistema", contentX, contentY, ColorScheme.TextPrimary, bold: true, size: 16);
            FontRenderer.DrawText(buffer, screenW, screenH, "Selecione a cor que será aplicada aos botões, janelas e barra de tarefas:", contentX, contentY + 26, ColorScheme.TextSecondary, bold: false, size: 12);

            int colorGridY = contentY + 60;
            int boxSize = 48;
            int gap = 14;

            for (int i = 0; i < _accentColors.Length; i++)
            {
                int bx = contentX + (i * (boxSize + gap));
                int by = colorGridY;

                bool isSelected = ColorScheme.Accent == _accentColors[i].Color;

                // Color Box
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, bx, by, boxSize, boxSize, _accentColors[i].Color);
                if (isSelected)
                {
                    FastRenderer.DrawRectangle(buffer, screenW, screenH, bx - 3, by - 3, boxSize + 6, boxSize + 6, ColorScheme.Black);
                    FastRenderer.DrawRectangle(buffer, screenW, screenH, bx - 2, by - 2, boxSize + 4, boxSize + 4, ColorScheme.White);
                    FontRenderer.DrawText(buffer, screenW, screenH, "✓", bx + 16, by + 14, ColorScheme.TextWhite, bold: true, size: 18);
                }

                // Label
                int lw = FontRenderer.MeasureText(_accentColors[i].Name, bold: false, size: 12);
                FontRenderer.DrawText(buffer, screenW, screenH, _accentColors[i].Name, bx + (boxSize - lw) / 2, by + boxSize + 6, ColorScheme.TextPrimary, bold: false, size: 12);
            }

            // Current Active Info
            int infoY = colorGridY + boxSize + 40;
            FontRenderer.DrawText(buffer, screenW, screenH, $"Cor Atual: {ColorScheme.AccentName}", contentX, infoY, ColorScheme.Accent, bold: true, size: 14);
        }
        else if (_selectedTab == 1) // Tela & Brilho
        {
            FontRenderer.DrawText(buffer, screenW, screenH, "Ajuste de Brilho da Tela", contentX, contentY, ColorScheme.TextPrimary, bold: true, size: 16);
            FontRenderer.DrawText(buffer, screenW, screenH, "Controle deslizante para ajuste de luminosidade em tempo real:", contentX, contentY + 26, ColorScheme.TextSecondary, bold: false, size: 12);

            int sliderY = contentY + 70;
            int sliderW = 320;
            int sliderH = 10;

            // Slider track
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, contentX, sliderY, sliderW, sliderH, ColorScheme.ControlBackground);
            FastRenderer.DrawRectangle(buffer, screenW, screenH, contentX, sliderY, sliderW, sliderH, ColorScheme.WindowBorder);

            // Filled portion
            int fillW = (sliderW * (ColorScheme.BrightnessPercent - 20)) / 80;
            if (fillW > 0)
            {
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, contentX, sliderY, fillW, sliderH, ColorScheme.Accent);
            }

            // Thumb
            int thumbX = contentX + fillW - 8;
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, thumbX, sliderY - 6, 16, 22, ColorScheme.White);
            FastRenderer.DrawRectangle(buffer, screenW, screenH, thumbX, sliderY - 6, 16, 22, ColorScheme.Accent);

            // Value text
            FontRenderer.DrawText(buffer, screenW, screenH, $"{ColorScheme.BrightnessPercent}%", contentX + sliderW + 16, sliderY - 4, ColorScheme.TextPrimary, bold: true, size: 14);

            // Buttons: - e +
            int btnY = sliderY + 36;
            DrawSmallBtn(buffer, screenW, screenH, contentX, btnY, 40, 28, "- 10%");
            DrawSmallBtn(buffer, screenW, screenH, contentX + 50, btnY, 40, 28, "+ 10%");
        }
        else if (_selectedTab == 2) // Áudio & Som
        {
            FontRenderer.DrawText(buffer, screenW, screenH, "Volume do Sistema", contentX, contentY, ColorScheme.TextPrimary, bold: true, size: 16);
            FontRenderer.DrawText(buffer, screenW, screenH, "Ajuste o volume geral de saída de áudio:", contentX, contentY + 26, ColorScheme.TextSecondary, bold: false, size: 12);

            int sliderY = contentY + 70;
            int sliderW = 320;
            int sliderH = 10;

            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, contentX, sliderY, sliderW, sliderH, ColorScheme.ControlBackground);
            FastRenderer.DrawRectangle(buffer, screenW, screenH, contentX, sliderY, sliderW, sliderH, ColorScheme.WindowBorder);

            int fillW = (sliderW * ColorScheme.VolumePercent) / 100;
            if (fillW > 0)
            {
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, contentX, sliderY, fillW, sliderH, ColorScheme.Accent);
            }

            int thumbX = contentX + fillW - 8;
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, thumbX, sliderY - 6, 16, 22, ColorScheme.White);
            FastRenderer.DrawRectangle(buffer, screenW, screenH, thumbX, sliderY - 6, 16, 22, ColorScheme.Accent);

            FontRenderer.DrawText(buffer, screenW, screenH, $"{ColorScheme.VolumePercent}%", contentX + sliderW + 16, sliderY - 4, ColorScheme.TextPrimary, bold: true, size: 14);

            int btnY = sliderY + 36;
            DrawSmallBtn(buffer, screenW, screenH, contentX, btnY, 40, 28, "- 10%");
            DrawSmallBtn(buffer, screenW, screenH, contentX + 50, btnY, 40, 28, "+ 10%");
            DrawSmallBtn(buffer, screenW, screenH, contentX + 100, btnY, 60, 28, "Mudo");
        }
    }

    private void DrawSmallBtn(uint[] buffer, int screenW, int screenH, int x, int y, int w, int h, string text)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, x, y, w, h, ColorScheme.ControlBackground);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, x, y, w, h, ColorScheme.WindowBorder);
        int tw = FontRenderer.MeasureText(text, bold: false, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, text, x + (w - tw) / 2, y + 6, ColorScheme.TextPrimary, bold: false, size: 12);
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        if (!InputState.LeftClicked && !InputState.LeftDown) return;

        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        // 1. Sidebar tab selection
        int navW = 160;
        if (InputState.LeftClicked && mx >= clientX && mx < clientX + navW)
        {
            int tabY = clientY + 54;
            for (int i = 0; i < _tabNames.Length; i++)
            {
                if (my >= tabY && my < tabY + 32)
                {
                    _selectedTab = i;
                    return;
                }
                tabY += 36;
            }
        }

        // 2. Content area interaction
        int contentX = clientX + navW + 24;
        int contentY = clientY + 20;

        if (_selectedTab == 0 && InputState.LeftClicked)
        {
            int colorGridY = contentY + 60;
            int boxSize = 48;
            int gap = 14;

            for (int i = 0; i < _accentColors.Length; i++)
            {
                int bx = contentX + (i * (boxSize + gap));
                int by = colorGridY;

                if (mx >= bx && mx < bx + boxSize && my >= by && my < by + boxSize)
                {
                    ColorScheme.SetAccent(_accentColors[i].Name);
                    return;
                }
            }
        }
        else if (_selectedTab == 1) // Brilho
        {
            int sliderY = contentY + 70;
            int sliderW = 320;

            if (mx >= contentX && mx <= contentX + sliderW && my >= sliderY - 10 && my <= sliderY + 20)
            {
                int ratio = ((mx - contentX) * 80) / sliderW;
                ColorScheme.BrightnessPercent = Math.Clamp(20 + ratio, 20, 100);
            }

            if (InputState.LeftClicked)
            {
                int btnY = sliderY + 36;
                if (mx >= contentX && mx < contentX + 40 && my >= btnY && my < btnY + 28)
                {
                    ColorScheme.BrightnessPercent = Math.Clamp(ColorScheme.BrightnessPercent - 10, 20, 100);
                }
                else if (mx >= contentX + 50 && mx < contentX + 90 && my >= btnY && my < btnY + 28)
                {
                    ColorScheme.BrightnessPercent = Math.Clamp(ColorScheme.BrightnessPercent + 10, 20, 100);
                }
            }
        }
        else if (_selectedTab == 2) // Som
        {
            int sliderY = contentY + 70;
            int sliderW = 320;

            if (mx >= contentX && mx <= contentX + sliderW && my >= sliderY - 10 && my <= sliderY + 20)
            {
                int ratio = ((mx - contentX) * 100) / sliderW;
                ColorScheme.VolumePercent = Math.Clamp(ratio, 0, 100);
            }

            if (InputState.LeftClicked)
            {
                int btnY = sliderY + 36;
                if (mx >= contentX && mx < contentX + 40 && my >= btnY && my < btnY + 28)
                {
                    ColorScheme.VolumePercent = Math.Clamp(ColorScheme.VolumePercent - 10, 0, 100);
                }
                else if (mx >= contentX + 50 && mx < contentX + 90 && my >= btnY && my < btnY + 28)
                {
                    ColorScheme.VolumePercent = Math.Clamp(ColorScheme.VolumePercent + 10, 0, 100);
                }
                else if (mx >= contentX + 100 && mx < contentX + 160 && my >= btnY && my < btnY + 28)
                {
                    ColorScheme.VolumePercent = (ColorScheme.VolumePercent > 0) ? 0 : 80;
                }
            }
        }
    }
}
