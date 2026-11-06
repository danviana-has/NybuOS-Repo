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
    public int Width { get; set; } = 104;
    public int Height { get; set; } = 106;

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
        int iconBottom = Y + 56;
        if (icon != null)
        {
            // Placa/fundo claro de vidro translúcido para o ícone
            int iconX = X + (Width - icon.Width) / 2;
            int iconY = Y + 8;
            display.DrawFrostedGlass(iconX - 6, iconY - 6, icon.Width + 12, icon.Height + 12, Theme.SurfaceLight, alpha: 220, cornerRadius: 8);
            display.DrawRectangle(iconX - 6, iconY - 6, icon.Width + 12, icon.Height + 12, Theme.ShadowBorder);
            display.DrawIcon(iconX, iconY, icon);
            iconBottom = iconY + icon.Height;
        }

        // Fundo do texto em cápsula de vidro fosco claro para leitura 100% nítida em Full HD
        int textW = Title.Length * CosmosNativeFont.Width;
        int badgeW = textW + 16;
        int badgeX = X + Math.Max(0, (Width - badgeW) / 2);
        int badgeY = iconBottom + 8;

        display.DrawFrostedGlass(badgeX, badgeY, badgeW, 24, Theme.Surface, alpha: 240, cornerRadius: 6);
        display.DrawRectangle(badgeX, badgeY, badgeW, 24, Theme.ShadowBorder);
        display.DrawString(badgeX + 8, badgeY + 4, Title, Theme.TextPrimary);
    }
}
