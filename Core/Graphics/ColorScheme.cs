namespace NybuOS.Core.Graphics;

public static class ColorScheme
{
    // 32-bit ARGB colors (uint)
    public const uint White = 0xFFFFFFFF;
    public const uint Black = 0xFF000000;
    public const uint Transparent = 0x00000000;

    // Windows 10 Light Theme Surface Colors
    public const uint BackgroundLight = 0xFFF3F3F3;
    public const uint WindowBody = 0xFFFFFFFF;
    public const uint WindowTitlebar = 0xFFFFFFFF;
    public const uint WindowTitlebarInactive = 0xFFF9F9F9;
    public const uint WindowBorder = 0xFFD2D2D2;
    public const uint ControlBackground = 0xFFEAEAEA;
    public const uint ControlHover = 0xFFDADADA;
    public const uint ControlPressed = 0xFFCCCCCC;
    public const uint TaskbarBackground = 0xFFFFFFFF;
    public const uint TaskbarBorder = 0xFFE0E0E0;
    public const uint TaskbarHover = 0xFFEFEFEF;
    public const uint TaskbarActiveIndicator = 0xFF0078D7;
    public const uint CloseButtonHover = 0xFFE81123;
    public const uint CloseButtonPressed = 0xFFC70D1D;

    // Text Colors
    public const uint TextPrimary = 0xFF1A1A1A;
    public const uint TextSecondary = 0xFF5A5A5A;
    public const uint TextMuted = 0xFF8A8A8A;
    public const uint TextWhite = 0xFFFFFFFF;

    // Accent Color Palette
    public const uint AccentBlue = 0xFF0078D7;
    public const uint AccentOrange = 0xFFCA5010;
    public const uint AccentPurple = 0xFF8764B8;
    public const uint AccentGreen = 0xFF107C41;
    public const uint AccentYellow = 0xFFFFB900;
    public const uint AccentRed = 0xFFE81123;

    // Live Tile Preset Palette
    public const uint TileBlue = 0xFF0078D7;
    public const uint TileCyan = 0xFF008299;
    public const uint TileTeal = 0xFF00A4EF;
    public const uint TileGreen = 0xFF107C41;
    public const uint TilePurple = 0xFF8764B8;
    public const uint TileOrange = 0xFFCA5010;
    public const uint TileRed = 0xFFE81123;
    public const uint TileDarkBlue = 0xFF004E8C;
    public const uint TileSlate = 0xFF4A607A;

    // Dynamic User Accent & Adjustments
    public static uint Accent = AccentBlue;
    public static string AccentName = "Azul";
    public static int BrightnessPercent = 100;
    public static int VolumePercent = 80;

    public static void SetAccent(string name)
    {
        switch (name.ToLowerInvariant())
        {
            case "azul":
            case "blue":
                Accent = AccentBlue;
                AccentName = "Azul";
                break;
            case "laranja":
            case "orange":
                Accent = AccentOrange;
                AccentName = "Laranja";
                break;
            case "roxo":
            case "purple":
                Accent = AccentPurple;
                AccentName = "Roxo";
                break;
            case "verde":
            case "green":
                Accent = AccentGreen;
                AccentName = "Verde";
                break;
            case "amarelo":
            case "yellow":
                Accent = AccentYellow;
                AccentName = "Amarelo";
                break;
            case "vermelho":
            case "red":
                Accent = AccentRed;
                AccentName = "Vermelho";
                break;
        }
    }
}
