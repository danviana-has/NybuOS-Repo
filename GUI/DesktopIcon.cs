using System;
using System.Drawing;
using NybuOS.Drivers;

namespace NybuOS.GUI;

/// <summary>
/// Ícone da Área de Trabalho moderno com badge flat estilizado e fundo de texto com cápsula.
/// Evita sobreposição e texto ilegível sobre wallpapers com estrelas.
/// </summary>
public class DesktopIcon
{
    public string Title { get; set; }
    public Func<IconImage> IconResolver { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 84;
    public int Height { get; set; } = 80;

    public Action OnClick { get; set; }

    public DesktopIcon(string title, Func<IconImage> iconResolver, int x, int y, Action onClick)
    {
        Title = title;
        IconResolver = iconResolver;
        X = x;
        Y = y;
        OnClick = onClick;
    }

    public bool ContainsPoint(int px, int py)
    {
        return px >= X && px <= X + Width && py >= Y && py <= Y + Height;
    }

    public void Draw(DisplayDriver display)
    {
        var icon = IconResolver?.Invoke();
        if (icon != null)
        {
            // Placa/fundo escuro translúcido para o ícone
            int iconX = X + (Width - icon.Width) / 2;
            int iconY = Y + 6;
            display.DrawFilledRectangle(iconX - 6, iconY - 4, icon.Width + 12, icon.Height + 8, Color.FromArgb(20, 20, 28));
            display.DrawRectangle(iconX - 6, iconY - 4, icon.Width + 12, icon.Height + 8, Color.FromArgb(45, 45, 60));
            display.DrawIcon(iconX, iconY, icon);
        }

        // Fundo do texto em cápsula escura para leitura 100% nítida
        int textW = Title.Length * 8;
        int badgeW = textW + 12;
        int badgeX = X + Math.Max(0, (Width - badgeW) / 2);
        int badgeY = Y + 50;

        display.DrawFilledRectangle(badgeX, badgeY, badgeW, 20, Color.FromArgb(16, 16, 24));
        display.DrawRectangle(badgeX, badgeY, badgeW, 20, Color.FromArgb(40, 40, 55));
        display.DrawString(badgeX + 6, badgeY + 2, Title, Color.White);
    }
}
