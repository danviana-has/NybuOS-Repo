using System;
using System.Drawing;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Painel de Controle Moderno do NybuOS com dimensões amplas (480x400)
/// e alinhamento espaçado para que nenhum texto ou botão fique sobreposto.
/// </summary>
public class ControlPanelApp : Window
{
    private readonly DisplayDriver _displayDriver;

    private readonly (string Name, Color Color)[] _accentColors = new (string, Color)[]
    {
        ("Azul", Color.FromArgb(59, 130, 246)),
        ("Roxo", Color.FromArgb(168, 85, 247)),
        ("Verde", Color.FromArgb(34, 197, 94)),
        ("Laranja", Color.FromArgb(249, 115, 22)),
        ("Vermelho", Color.FromArgb(239, 68, 68)),
        ("Rosa", Color.FromArgb(236, 72, 153)),
        ("Ciano", Color.FromArgb(6, 182, 212)),
        ("Amarelo", Color.FromArgb(234, 179, 8))
    };

    private int _selectedColorIndex = 0;
    private int _brightnessPercent = 100;
    private bool _soundEnabled = true;

    public ControlPanelApp(int id, int x, int y, DisplayDriver displayDriver)
        : base(id, "Configuracoes do Sistema", x, y, 480, 390)
    {
        Icon = IconResources.ControlPanel16;
        _displayDriver = displayDriver;

        for (int i = 0; i < _accentColors.Length; i++)
        {
            if (_accentColors[i].Color.ToArgb() == Theme.AccentColor.ToArgb())
            {
                _selectedColorIndex = i;
                break;
            }
        }

        if (_displayDriver != null)
        {
            _brightnessPercent = (int)(_displayDriver.BrightnessMultiplier * 100);
        }
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Superfície interna limpa Dark Glass
        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.WindowClientBg);

        // ==========================================
        // SEÇÃO 1: COR DE DESTAQUE
        // ==========================================
        int sec1Y = cy + 14;
        display.DrawString(cx + 16, sec1Y, "Cor de Destaque das Janelas e Botoes:", Theme.TextPrimary);

        int cols = 4;
        int boxW = 100;
        int boxH = 34;

        for (int i = 0; i < _accentColors.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            int bx = cx + 16 + (col * (boxW + 12));
            int by = sec1Y + 28 + (row * (boxH + 10));

            Color itemColor = _accentColors[i].Color;
            bool isSelected = (i == _selectedColorIndex);

            // Retângulo preenchido de cor
            display.DrawFilledRectangle(bx, by, boxW, boxH, itemColor);

            if (isSelected)
            {
                display.DrawRectangle(bx - 2, by - 2, boxW + 4, boxH + 4, Color.White);
                display.DrawRectangle(bx - 1, by - 1, boxW + 2, boxH + 2, Color.Black);
            }
            else
            {
                display.DrawRectangle(bx, by, boxW, boxH, Color.FromArgb(50, 50, 60));
            }

            // Nome da cor centralizado
            string name = _accentColors[i].Name;
            int nameX = bx + Math.Max(4, (boxW - (name.Length * 8)) / 2);
            int nameY = by + (boxH - 14) / 2;
            display.DrawString(nameX, nameY, name, Color.White);
        }

        // ==========================================
        // SEÇÃO 2: BRILHO DA TELA
        // ==========================================
        int sec2Y = sec1Y + 130;
        display.DrawLine(cx + 16, sec2Y, cx + cw - 16, sec2Y, Theme.WindowBorderInactive);

        display.DrawString(cx + 16, sec2Y + 16, $"Brilho da Tela: {_brightnessPercent}%", Theme.TextPrimary);

        int btnMinusX = cx + 240;
        int btnPlusX = cx + 310;
        int btnBrightY = sec2Y + 12;

        display.DrawModernButton(btnMinusX, btnBrightY, 50, 26, " - ", false);
        display.DrawModernButton(btnPlusX, btnBrightY, 50, 26, " + ", false);

        int barX = cx + 16;
        int barY = sec2Y + 48;
        int barW = cw - 32;
        int barH = 12;
        display.DrawFilledRectangle(barX, barY, barW, barH, Theme.InputBg);
        display.DrawRectangle(barX, barY, barW, barH, Theme.ButtonBorder);

        int fillW = Math.Max(0, Math.Min(barW, (barW * _brightnessPercent) / 150));
        display.DrawFilledRectangle(barX, barY, fillW, barH, Theme.AccentColor);

        // ==========================================
        // SEÇÃO 3: SOM DO SISTEMA OPERACIONAL
        // ==========================================
        int sec3Y = sec2Y + 76;
        display.DrawLine(cx + 16, sec3Y, cx + cw - 16, sec3Y, Theme.WindowBorderInactive);

        display.DrawString(cx + 16, sec3Y + 16, "Efeitos Sonoros:", Theme.TextPrimary);

        int soundBtnX = cx + 240;
        int soundBtnY = sec3Y + 12;
        string soundState = _soundEnabled ? " Ativado " : " Mudo ";
        display.DrawModernButton(soundBtnX, soundBtnY, 120, 28, soundState, _soundEnabled);

        // Rodapé
        int footerY = ch - 24;
        display.DrawString(cx + 16, cy + footerY, "As alteracoes sao aplicadas instantaneamente no sistema.", Theme.TextMuted);
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;

        int sec1Y = cy + 14;
        int cols = 4;
        int boxW = 100;
        int boxH = 34;

        for (int i = 0; i < _accentColors.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            int bx = cx + 16 + (col * (boxW + 12));
            int by = sec1Y + 28 + (row * (boxH + 10));

            if (mouseX >= bx && mouseX <= bx + boxW && mouseY >= by && mouseY <= by + boxH)
            {
                _selectedColorIndex = i;
                Theme.AccentColor = _accentColors[i].Color;
                return;
            }
        }

        int sec2Y = sec1Y + 130;
        int btnMinusX = cx + 240;
        int btnPlusX = cx + 310;
        int btnBrightY = sec2Y + 12;

        if (mouseY >= btnBrightY && mouseY <= btnBrightY + 26)
        {
            if (mouseX >= btnMinusX && mouseX <= btnMinusX + 50)
            {
                _brightnessPercent = Math.Max(30, _brightnessPercent - 10);
                if (_displayDriver != null)
                {
                    _displayDriver.BrightnessMultiplier = _brightnessPercent / 100.0f;
                }
                return;
            }
            if (mouseX >= btnPlusX && mouseX <= btnPlusX + 50)
            {
                _brightnessPercent = Math.Min(150, _brightnessPercent + 10);
                if (_displayDriver != null)
                {
                    _displayDriver.BrightnessMultiplier = _brightnessPercent / 100.0f;
                }
                return;
            }
        }

        int sec3Y = sec2Y + 76;
        int soundBtnX = cx + 240;
        int soundBtnY = sec3Y + 12;

        if (mouseX >= soundBtnX && mouseX <= soundBtnX + 120 && mouseY >= soundBtnY && mouseY <= soundBtnY + 28)
        {
            _soundEnabled = !_soundEnabled;
            SoundDriver.IsEnabled = _soundEnabled;
            if (_soundEnabled)
            {
                SoundDriver.PlayAlert();
            }
            else
            {
                SoundDriver.StopBeep();
            }
            return;
        }
    }
}
