using System;
using System.IO;
using System.Reflection;

namespace NybuOS.Core.Graphics;

public static class AssetManager
{
    public static byte[]? RegularFontData { get; private set; }
    public static byte[]? BoldFontData { get; private set; }

    public static FastBitmap? Wallpaper { get; private set; }

    // Icons: Original & Pre-scaled
    public static FastBitmap? IconAboutApp16 { get; private set; }
    public static FastBitmap? IconAboutApp24 { get; private set; }
    public static FastBitmap? IconAboutApp48 { get; private set; }

    public static FastBitmap? IconHelp16 { get; private set; }
    public static FastBitmap? IconHelp24 { get; private set; }
    public static FastBitmap? IconHelp48 { get; private set; }

    public static FastBitmap? IconNotepad16 { get; private set; }
    public static FastBitmap? IconNotepad24 { get; private set; }
    public static FastBitmap? IconNotepad48 { get; private set; }

    public static FastBitmap? IconCalc16 { get; private set; }
    public static FastBitmap? IconCalc24 { get; private set; }
    public static FastBitmap? IconCalc48 { get; private set; }

    public static FastBitmap? IconCalendar16 { get; private set; }
    public static FastBitmap? IconCalendar24 { get; private set; }
    public static FastBitmap? IconCalendar48 { get; private set; }

    public static FastBitmap? IconCentralLogic16 { get; private set; }
    public static FastBitmap? IconCentralLogic24 { get; private set; }
    public static FastBitmap? IconCentralLogic48 { get; private set; }

    public static FastBitmap? IconSettings16 { get; private set; }
    public static FastBitmap? IconSettings24 { get; private set; }
    public static FastBitmap? IconSettings48 { get; private set; }

    public static FastBitmap? IconFiles16 { get; private set; }
    public static FastBitmap? IconFiles24 { get; private set; }
    public static FastBitmap? IconFiles48 { get; private set; }

    public static FastBitmap? IconPaint16 { get; private set; }
    public static FastBitmap? IconPaint24 { get; private set; }
    public static FastBitmap? IconPaint48 { get; private set; }

    public static FastBitmap? IconSheets16 { get; private set; }
    public static FastBitmap? IconSheets24 { get; private set; }
    public static FastBitmap? IconSheets48 { get; private set; }

    public static FastBitmap? IconHealth16 { get; private set; }
    public static FastBitmap? IconHealth24 { get; private set; }
    public static FastBitmap? IconHealth48 { get; private set; }

    public static FastBitmap? IconTerminal16 { get; private set; }
    public static FastBitmap? IconTerminal24 { get; private set; }
    public static FastBitmap? IconTerminal48 { get; private set; }

    public static FastBitmap? IconOsLogoTaskbar { get; private set; } // 32x18 for Start Button

    public static void Initialize(int screenWidth, int screenHeight)
    {
        var asm = Assembly.GetExecutingAssembly();

        // 1. Load TrueType Fonts
        RegularFontData = LoadResourceBytes(asm, "NybuOS.assets.fonts.segoe-ui.tff") 
                       ?? LoadResourceBytes(asm, "NybuOS.assets.fonts.segoe-ui.ttf");
        BoldFontData = LoadResourceBytes(asm, "NybuOS.assets.fonts.segoe-ui-bold.tff") 
                    ?? LoadResourceBytes(asm, "NybuOS.assets.fonts.segoe-ui-bold.ttf");

        // 2. Load Wallpaper: Prioritize lossless BMP for 100% pixel fidelity and instantaneous loading
        byte[]? wpBmpBytes = LoadResourceBytes(asm, "NybuOS.assets.wallpaper.bmp");
        if (wpBmpBytes != null)
        {
            var rawWp = BmpDecoder.Decode(wpBmpBytes);
            if (rawWp != null)
            {
                Wallpaper = (rawWp.Width == screenWidth && rawWp.Height == screenHeight)
                    ? rawWp
                    : rawWp.Resize(screenWidth, screenHeight);
            }
        }

        // Fallback to JPEG if BMP is unavailable
        if (Wallpaper == null)
        {
            byte[]? wpJpgBytes = LoadResourceBytes(asm, "NybuOS.assets.wallpaper.jpg");
            if (wpJpgBytes != null)
            {
                var rawWp = JpegDecoder.Decode(wpJpgBytes);
                if (rawWp != null)
                {
                    Wallpaper = rawWp.Resize(screenWidth, screenHeight);
                }
            }
        }

        // Ultimate fallback to clean Windows 10 Lagoons gradient
        if (Wallpaper == null)
        {
            Wallpaper = GenerateFallbackWallpaper(screenWidth, screenHeight);
        }

        // 3. Load App Icons (Pre-scaled 32bpp BMPs with full alpha channel)
        IconAboutApp16 = LoadBmp(asm, "AboutApp_16.bmp");
        IconAboutApp24 = LoadBmp(asm, "AboutApp_24.bmp");
        IconAboutApp48 = LoadBmp(asm, "AboutApp_48.bmp");

        IconHelp16 = LoadBmp(asm, "ajuda_16.bmp");
        IconHelp24 = LoadBmp(asm, "ajuda_24.bmp");
        IconHelp48 = LoadBmp(asm, "ajuda_48.bmp");

        IconNotepad16 = LoadBmp(asm, "bloco-de-anotacoes_16.bmp");
        IconNotepad24 = LoadBmp(asm, "bloco-de-anotacoes_24.bmp");
        IconNotepad48 = LoadBmp(asm, "bloco-de-anotacoes_48.bmp");

        IconCalc16 = LoadBmp(asm, "calculadora_16.bmp");
        IconCalc24 = LoadBmp(asm, "calculadora_24.bmp");
        IconCalc48 = LoadBmp(asm, "calculadora_48.bmp");

        IconCalendar16 = LoadBmp(asm, "calendario_16.bmp");
        IconCalendar24 = LoadBmp(asm, "calendario_24.bmp");
        IconCalendar48 = LoadBmp(asm, "calendario_48.bmp");

        IconCentralLogic16 = LoadBmp(asm, "CentralLogic_16.bmp");
        IconCentralLogic24 = LoadBmp(asm, "CentralLogic_24.bmp");
        IconCentralLogic48 = LoadBmp(asm, "CentralLogic_48.bmp");

        IconSettings16 = LoadBmp(asm, "configuracoes_16.bmp");
        IconSettings24 = LoadBmp(asm, "configuracoes_24.bmp");
        IconSettings48 = LoadBmp(asm, "configuracoes_48.bmp");

        IconFiles16 = LoadBmp(asm, "gerenciamento-de-arquivos_16.bmp");
        IconFiles24 = LoadBmp(asm, "gerenciamento-de-arquivos_24.bmp");
        IconFiles48 = LoadBmp(asm, "gerenciamento-de-arquivos_48.bmp");

        IconPaint16 = LoadBmp(asm, "paintpad_16.bmp");
        IconPaint24 = LoadBmp(asm, "paintpad_24.bmp");
        IconPaint48 = LoadBmp(asm, "paintpad_48.bmp");

        IconSheets16 = LoadBmp(asm, "planilha_16.bmp");
        IconSheets24 = LoadBmp(asm, "planilha_24.bmp");
        IconSheets48 = LoadBmp(asm, "planilha_48.bmp");

        IconHealth16 = LoadBmp(asm, "saude_16.bmp");
        IconHealth24 = LoadBmp(asm, "saude_24.bmp");
        IconHealth48 = LoadBmp(asm, "saude_48.bmp");

        IconTerminal16 = LoadBmp(asm, "terminal_16.bmp");
        IconTerminal24 = LoadBmp(asm, "terminal_24.bmp");
        IconTerminal48 = LoadBmp(asm, "terminal_48.bmp");

        // OS Icon for Start button: 32x18
        IconOsLogoTaskbar = LoadBmp(asm, "os_logo_32.bmp");
    }

    private static FastBitmap? LoadBmp(Assembly asm, string filename)
    {
        byte[]? bytes = LoadResourceBytes(asm, "NybuOS.assets.icons." + filename)
                     ?? LoadResourceBytes(asm, "NybuOS.assets.icons_bmp." + filename);
        if (bytes != null)
        {
            return BmpDecoder.Decode(bytes);
        }
        return null;
    }

    private static byte[]? LoadResourceBytes(Assembly asm, string resourceName)
    {
        try
        {
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) return null;
            byte[] bytes = new byte[stream.Length];
            int read = 0;
            while (read < bytes.Length)
            {
                int r = stream.Read(bytes, read, bytes.Length - read);
                if (r <= 0) break;
                read += r;
            }
            return bytes;
        }
        catch
        {
            return null;
        }
    }

    private static FastBitmap GenerateFallbackWallpaper(int width, int height)
    {
        var bmp = new FastBitmap(width, height);
        var pixels = bmp.Pixels;

        // Windows 10 Lagoons soft gradient (#0067B8 to #00A4EF)
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            int r = (int)(0 + t * 0);
            int g = (int)(103 + t * (164 - 103));
            int b = (int)(184 + t * (239 - 184));
            uint color = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b;

            int rowOff = y * width;
            for (int x = 0; x < width; x++)
            {
                pixels[rowOff + x] = color;
            }
        }

        return bmp;
    }
}
