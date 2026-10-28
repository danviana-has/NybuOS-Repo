using System;
using System.Drawing;
using NybuOS.Drivers;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Janela modal "Sobre o NybuOS" com especificações e créditos.
/// </summary>
public class AboutApp : Window
{
    public AboutApp(int id, int x, int y)
        : base(id, "Sobre o NybuOS", x, y, 320, 190)
    {
        Icon = IconResources.AboutIcon ?? IconResources.Help16;
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.Surface);

        // Ícone moderno do NybuOS
        if (IconResources.AboutIcon != null)
        {
            display.DrawIcon(cx + 12, cy + 12, IconResources.AboutIcon);
        }
        else if (IconResources.Computer32 != null)
        {
            display.DrawIcon(cx + 12, cy + 12, IconResources.Computer32);
        }

        display.DrawString(cx + 48, cy + 12, "NybuOS Versao 3.0 Final", Theme.TextPrimary);
        display.DrawString(cx + 48, cy + 28, "Simplicidade, Estabilidade e Elegancia", Theme.AccentColor);

        display.DrawLine(cx + 10, cy + 46, cx + cw - 10, cy + 46, Theme.ButtonBorder);

        display.DrawString(cx + 12, cy + 54, "Sistema Operacional em C# Nativo", Theme.TextPrimary);
        display.DrawString(cx + 12, cy + 68, "Cosmos OS Engine (x86_64 AOT)", Theme.TextSecondary);
        display.DrawString(cx + 12, cy + 82, "Resolucao: 800x600 32bpp Dark Glass", Theme.TextSecondary);
        display.DrawString(cx + 12, cy + 96, "VFS FAT32 + Gerenciador de Janelas", Theme.TextSecondary);

        // Botão OK
        int btnW = 70;
        int btnH = 24;
        int btnX = cx + (cw - btnW) / 2;
        int btnY = cy + ch - 30;

        display.DrawModernButton(btnX, btnY, btnW, btnH, "OK", true);
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;
        int btnW = 60;
        int btnH = 22;
        int btnX = cx + (cw - btnW) / 2;
        int btnY = cy + ch - 30;

        if (mouseX >= btnX && mouseX <= btnX + btnW && mouseY >= btnY && mouseY <= btnY + btnH)
        {
            IsClosed = true;
        }
    }
}
