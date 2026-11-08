using System;
using System.Drawing;
using NybuOS.Drivers;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Janela "Sobre o NybuOS Purple" com estética macOS sobre este sistema, especificações Full HD e créditos.
/// </summary>
public class AboutApp : Window
{
    public AboutApp(int id, int x, int y)
        : base(id, "Sobre o NybuOS Purple", x, y, 540, 320)
    {
        Icon = IconResources.AboutIcon ?? IconResources.Help16;
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo com vidro fosco refinado
        display.DrawFrostedGlass(cx, cy, cw, ch, Theme.Surface, alpha: 220, cornerRadius: 8);

        // Ícone do NybuOS RV
        if (IconResources.AboutDock != null)
        {
            display.DrawIcon(cx + 20, cy + 20, IconResources.AboutDock);
        }
        else if (IconResources.AboutIcon != null)
        {
            display.DrawIcon(cx + 20, cy + 20, IconResources.AboutIcon);
        }

        display.DrawString(cx + 92, cy + 24, "NybuOS Purple", Theme.TextPrimary);
        display.DrawString(cx + 92, cy + 46, "Edicao Purple Minimalista - macOS Style", Theme.AccentColor);

        display.DrawLine(cx + 18, cy + 90, cx + cw - 18, cy + 90, Theme.DockSeparator);

        display.DrawString(cx + 24, cy + 104, "Resolucao: Full HD REAL 1920x1080 32bpp nativo", Theme.TextPrimary);
        display.DrawString(cx + 24, cy + 128, "Interface: Barra 3D Degradê, Dock Cinza Claro, Janelas Light", Theme.TextSecondary);
        display.DrawString(cx + 24, cy + 152, "Recursos: Download Central HTTP/HTTPS + Midia + VFS", Theme.TextSecondary);
        display.DrawString(cx + 24, cy + 176, "Tipografia: Fonte Arial TTF com Alpha Blending Suave", Theme.TextSecondary);
        display.DrawString(cx + 24, cy + 200, "Kernel: C# Nativo x86_64 Cosmos OS com VFS FAT32", Theme.TextSecondary);

        // Botão Fechar ampliado para Full HD
        int btnW = 120;
        int btnH = 34;
        int btnX = cx + (cw - btnW) / 2;
        int btnY = cy + ch - 48;

        display.DrawModernButton(btnX, btnY, btnW, btnH, "Fechar", true);
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;
        int btnW = 120;
        int btnH = 34;
        int btnX = cx + (cw - btnW) / 2;
        int btnY = cy + ch - 48;

        if (mouseX >= btnX && mouseX <= btnX + btnW && mouseY >= btnY && mouseY <= btnY + btnH)
        {
            Close();
        }
    }
}
