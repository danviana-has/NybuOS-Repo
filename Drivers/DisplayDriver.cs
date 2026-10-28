using System;
using System.Drawing;
using Cosmos.Kernel.System.Graphics;
using Cosmos.Kernel.System.Graphics.Fonts;
using NybuOS.GUI;

namespace NybuOS.Drivers;

/// <summary>
/// Driver gráfico VBE / SVGA do NybuOS com suporte a Double Buffering,
/// renderização de fontes nativas com métricas exatas de largura (8x16),
/// janelas Dark Glass sólidas e botões planos modernos.
/// </summary>
public class DisplayDriver
{
    public const int DefaultWidth = 800;
    public const int DefaultHeight = 600;

    private Canvas _canvas;

    public int Width => _canvas?.Width ?? DefaultWidth;
    public int Height => _canvas?.Height ?? DefaultHeight;

    public float BrightnessMultiplier { get; set; } = 1.0f;

    public DisplayDriver()
    {
    }

    public void Initialize()
    {
        var mode = new Mode(DefaultWidth, DefaultHeight, ColorDepth.ColorDepth32);
        _canvas = Canvas.GetFullScreen(mode);
        Clear(Color.Black);
        Display();
    }

    public void Clear(Color color)
    {
        _canvas?.Clear(ApplyBrightness(color));
    }

    public void Display()
    {
        _canvas?.Display();
    }

    public void Swap()
    {
        _canvas?.Display();
    }

    public Color ApplyBrightness(Color c)
    {
        if (Math.Abs(BrightnessMultiplier - 1.0f) < 0.05f)
            return c;

        int r = (int)(c.R * BrightnessMultiplier);
        int g = (int)(c.G * BrightnessMultiplier);
        int b = (int)(c.B * BrightnessMultiplier);

        if (r > 255) r = 255; else if (r < 0) r = 0;
        if (g > 255) g = 255; else if (g < 0) g = 0;
        if (b > 255) b = 255; else if (b < 0) b = 0;

        return Color.FromArgb(r, g, b);
    }

    public void DrawPixel(int x, int y, Color color)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        _canvas?.DrawPoint(ApplyBrightness(color), x, y);
    }

    public void DrawLine(int x1, int y1, int x2, int y2, Color color)
    {
        if (_canvas == null) return;
        _canvas.DrawLine(ApplyBrightness(color), x1, y1, x2, y2);
    }

    public void DrawRectangle(int x, int y, int w, int h, Color color)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;
        _canvas.DrawRectangle(ApplyBrightness(color), x, y, w, h);
    }

    public void DrawFilledRectangle(int x, int y, int w, int h, Color color)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;
        _canvas.DrawFilledRectangle(ApplyBrightness(color), x, y, w, h);
    }

    /// <summary>
    /// Renderiza um retângulo translúcido com requício de cor preta (Dark Glass),
    /// mesclando em padrão xadrez 50% de alta performance para permitir ver o wallpaper de fundo!
    /// </summary>
    public void DrawTranslucentRectangle(int x, int y, int w, int h, Color tintColor)
    {
        if (_canvas == null || w <= 0 || h <= 0) return;

        Color darkTint = Color.FromArgb(14, 14, 18);
        Color applyColor = ApplyBrightness(darkTint);

        // Padrão de pontilhado translúcido (Dithered Alpha Glass)
        // Pinta 3 de cada 4 pixels com preto grafite e deixa 1 pixel do wallpaper brilhar através,
        // criando transparência translúcida real instantânea a 60 FPS no VMware/QEMU!
        for (int r = 0; r < h; r++)
        {
            int py = y + r;
            if (py < 0 || py >= Height) continue;

            for (int c = 0; c < w; c++)
            {
                int px = x + c;
                if (px < 0 || px >= Width) continue;

                // Transparência: deixa passar 25% da luz do wallpaper
                if ((px + py) % 4 != 0)
                {
                    _canvas.DrawPoint(applyColor, px, py);
                }
            }
        }
    }

    /// <summary>
    /// Renderiza texto usando nossa própria fonte idêntica à do Cosmos OS (CosmosNativeFont),
    /// sem cortar letras nem sofrer bugs de alinhamento.
    /// </summary>
    public void DrawString(int x, int y, string text, Color color, Color? bg = null)
    {
        if (string.IsNullOrEmpty(text) || _canvas == null) return;
        if (x >= Width || y >= Height) return;

        Color finalCol = ApplyBrightness(color);

        int curX = x;
        int curY = y;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                curX = x;
                curY += CosmosNativeFont.Height + 2;
                continue;
            }
            if (c == '\r') continue;

            DrawChar(curX, curY, c, finalCol, bg);
            curX += CosmosNativeFont.Width;
        }
    }

    /// <summary>
    /// Renderiza um caractere individual com precisão de pixel da fonte oficial.
    /// </summary>
    public void DrawChar(int x, int y, char c, Color color, Color? bg = null)
    {
        if (x + CosmosNativeFont.Width < 0 || x >= Width || y + CosmosNativeFont.Height < 0 || y >= Height)
            return;

        if (bg.HasValue)
        {
            DrawFilledRectangle(x, y, CosmosNativeFont.Width, CosmosNativeFont.Height, bg.Value);
        }

        for (int row = 0; row < CosmosNativeFont.Height; row++)
        {
            int py = y + row;
            if (py < 0 || py >= Height) continue;

            for (int col = 0; col < CosmosNativeFont.Width; col++)
            {
                int px = x + col;
                if (px < 0 || px >= Width) continue;

                if (CosmosNativeFont.GetPixel(c, col, row))
                {
                    _canvas.DrawPoint(color, px, py);
                }
            }
        }
    }

    /// <summary>
    /// Renderiza um ícone ou imagem (IconImage) com suporte a transparência e clipping seguro.
    /// </summary>
    public void DrawIcon(int x, int y, IconImage icon)
    {
        if (icon == null || _canvas == null) return;

        int iw = icon.Width;
        int ih = icon.Height;

        for (int row = 0; row < ih; row++)
        {
            int py = y + row;
            if (py < 0 || py >= Height) continue;

            for (int col = 0; col < iw; col++)
            {
                int px = x + col;
                if (px < 0 || px >= Width) continue;

                uint argb = icon.GetPixel(col, row);
                byte alpha = (byte)((argb >> 24) & 0xFF);
                if (alpha == 0) continue;

                byte r = (byte)((argb >> 16) & 0xFF);
                byte g = (byte)((argb >> 8) & 0xFF);
                byte b = (byte)(argb & 0xFF);

                _canvas.DrawPoint(ApplyBrightness(Color.FromArgb(r, g, b)), px, py);
            }
        }
    }

    /// <summary>
    /// Renderiza o papel de parede HD oficial com alta performance.
    /// </summary>
    public void DrawWallpaper(IconImage wallpaper)
    {
        if (wallpaper == null || _canvas == null) return;

        int ww = wallpaper.Width;
        int wh = wallpaper.Height;
        bool hasBrightness = Math.Abs(BrightnessMultiplier - 1.0f) >= 0.05f;

        for (int row = 0; row < wh; row++)
        {
            if (row >= Height) break;

            for (int col = 0; col < ww; col++)
            {
                if (col >= Width) break;

                uint argb = wallpaper.GetPixel(col, row);
                byte r = (byte)((argb >> 16) & 0xFF);
                byte g = (byte)((argb >> 8) & 0xFF);
                byte b = (byte)(argb & 0xFF);

                Color c = Color.FromArgb(r, g, b);
                if (hasBrightness)
                {
                    c = ApplyBrightness(c);
                }

                _canvas.DrawPoint(c, col, row);
            }
        }
    }

    /// <summary>
    /// Renderiza a barra de tarefas moderna: preta/grafite profunda e limpa, sem bordas pesadas e sem rastros.
    /// </summary>
    public void DrawModernTaskbar(int y, int width, int height)
    {
        if (_canvas == null) return;

        // Limpa e preenche 100% da área da barra de tarefas de forma sólida e de altíssima velocidade,
        // eliminando por completo qualquer rastro de mouse ou ghosting de botões!
        DrawFilledRectangle(0, y, width, height, Theme.TaskbarBg);

        // Fina linha minimalista no topo
        DrawLine(0, y, width - 1, y, Theme.TaskbarBorder);
    }

    /// <summary>
    /// Renderiza o botão Iniciar moderno com o logotipo circular e suporte a hover.
    /// </summary>
    public void DrawModernStartButton(int x, int y, int w, int h, bool isOpen, bool isHover = false)
    {
        Color bg = isOpen ? Theme.AccentColor : (isHover ? Theme.SurfaceHover : Theme.ButtonBg);
        DrawFilledRectangle(x, y, w, h, bg);

        if (isOpen)
        {
            DrawRectangle(x, y, w, h, Theme.AccentColor);
        }
        else if (isHover)
        {
            DrawRectangle(x, y, w, h, Theme.ButtonBorder);
        }

        if (IconResources.StartButtonIcon != null)
        {
            DrawIcon(x + (w - IconResources.StartButtonIcon.Width) / 2, y + (h - IconResources.StartButtonIcon.Height) / 2, IconResources.StartButtonIcon);
        }
    }

    /// <summary>
    /// Renderiza um botão moderno flat minimalista.
    /// </summary>
    public void DrawModernButton(int x, int y, int w, int h, string text, bool isAccent = false, bool isHover = false)
    {
        Color bg = isAccent ? Theme.AccentColor : (isHover ? Theme.SurfaceHover : Theme.ButtonBg);
        Color border = isAccent ? Theme.AccentColor : Theme.ButtonBorder;
        Color fg = isAccent ? Color.White : Theme.TextPrimary;

        DrawFilledRectangle(x, y, w, h, bg);
        DrawRectangle(x, y, w, h, border);

        if (!string.IsNullOrEmpty(text))
        {
            int textW = text.Length * 8;
            int textX = x + Math.Max(2, (w - textW) / 2);
            int textY = y + Math.Max(2, (h - 16) / 2);
            DrawString(textX, textY, text, fg);
        }
    }

    public void Draw3DBevel(int x, int y, int w, int h, bool sunken)
    {
        if (w <= 1 || h <= 1) return;
        Color border = sunken ? Color.FromArgb(20, 20, 26) : Theme.ButtonBorder;
        DrawRectangle(x, y, w, h, border);
    }

    public void DrawButton(int x, int y, int w, int h, string text, bool pressed = false)
    {
        DrawModernButton(x, y, w, h, text, pressed);
    }

    public void DrawModernInputBox(int x, int y, int w, int h, string text, bool hasFocus)
    {
        Color bg = Theme.InputBg;
        Color border = hasFocus ? Theme.AccentColor : Theme.ButtonBorder;

        DrawFilledRectangle(x, y, w, h, bg);
        DrawRectangle(x, y, w, h, border);

        if (!string.IsNullOrEmpty(text))
        {
            DrawString(x + 6, y + (h - 16) / 2, text, Theme.TextPrimary);
        }
    }

    /// <summary>
    /// Renderiza o cursor do mouse moderno e minimalista.
    /// </summary>
    public void DrawCursor(int x, int y)
    {
        Color white = Color.White;
        Color black = Color.Black;

        for (int r = 0; r < 14; r++)
        {
            int py = y + r;
            if (py < 0 || py >= Height) continue;

            for (int c = 0; c <= r * 6 / 10; c++)
            {
                int px = x + c;
                if (px < 0 || px >= Width) continue;

                if (c == 0 || c == r * 6 / 10 || r == 13)
                {
                    _canvas.DrawPoint(black, px, py);
                }
                else
                {
                    _canvas.DrawPoint(white, px, py);
                }
            }
        }
    }
}
