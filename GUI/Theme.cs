using System;
using System.Drawing;

namespace NybuOS.GUI;

/// <summary>
/// Sistema de temas do NybuOS RV (Revolution) no modo Light Theme com acabamento 3D:
/// Barra superior em degradê branco para cinzas profundos em relevo, dock flutuante
/// e cabeçalhos de janela em cinza claro intenso com sutis degradês brancos.
/// </summary>
public static class Theme
{
    /// <summary>
    /// Cor de destaque do sistema (System Accent). Padrão: Azul Apple (#007AFF)
    /// </summary>
    public static Color AccentColor { get; set; } = Color.FromArgb(0, 122, 255);

    // Barra Superior Translúcida / 3D Light
    public static Color TopBarBg => Color.FromArgb(248, 249, 252);
    public static Color TopBarAlphaColor => Color.FromArgb(235, 248, 249, 252);
    public static Color TopBarBorder => Color.FromArgb(145, 150, 162);

    // Dock Flutuante 3D Light (Cinza claro intenso com sutis degradês de branco)
    public static Color DockBg => Color.FromArgb(228, 231, 238);
    public static Color DockAlphaColor => Color.FromArgb(255, 228, 231, 238);
    public static Color DockBorder => Color.FromArgb(160, 165, 178);
    public static Color DockHighlight => Color.FromArgb(255, 255, 255);
    public static Color DockSeparator => Color.FromArgb(185, 190, 202);

    // Barra de Tarefas
    public static Color TaskbarBg => TopBarBg;
    public static Color TaskbarAlphaColor => TopBarAlphaColor;
    public static Color TaskbarBorder => TopBarBorder;

    // Janelas Light Theme (Design 3D e cabeçalho idêntico ao dock)
    public static Color WindowBg => Color.FromArgb(246, 247, 251);
    public static Color WindowHeaderBg => Color.FromArgb(228, 231, 238);
    public static Color WindowBorder => Color.FromArgb(160, 165, 178);
    public static Color WindowBorderInactive => Color.FromArgb(195, 200, 210);

    // Botões Circulares de Janela (Traffic Lights)
    public static Color CloseButton => Color.FromArgb(255, 95, 86);         // #FF5F56
    public static Color CloseButtonBorder => Color.FromArgb(224, 68, 62);   // #E0443E
    public static Color MinimizeButton => Color.FromArgb(255, 189, 46);      // #FFBD2E
    public static Color MinimizeButtonBorder => Color.FromArgb(222, 161, 35); // #DEA123
    public static Color MaximizeButton => Color.FromArgb(39, 201, 63);       // #27C93F
    public static Color MaximizeButtonBorder => Color.FromArgb(26, 171, 41);  // #1AAB29

    // Superfície de controles, caixas de diálogo e painéis
    public static Color Surface => Color.FromArgb(255, 255, 255);
    public static Color SurfaceLight => Color.FromArgb(242, 244, 248);
    public static Color SurfaceHover => Color.FromArgb(224, 228, 236);
    public static Color WindowClientBg => Color.FromArgb(246, 247, 251);
    public static Color InputBg => Color.FromArgb(255, 255, 255);

    // Bordas de controle e relevo 3D
    public static Color LightBorder => Color.FromArgb(255, 255, 255);
    public static Color ShadowBorder => Color.FromArgb(175, 180, 192);
    public static Color DarkBorder => Color.FromArgb(135, 140, 154);

    // Barra de Título
    public static Color TitleActive => AccentColor;
    public static Color TitleInactive => Color.FromArgb(215, 218, 226);
    public static Color TitleTextActive => Color.FromArgb(22, 24, 30);
    public static Color TitleTextInactive => Color.FromArgb(100, 105, 120);

    // Botões e Ações
    public static Color ButtonBg => Color.FromArgb(235, 238, 244);
    public static Color ButtonAccentBg => AccentColor;
    public static Color ButtonBorder => Color.FromArgb(175, 180, 194);

    // Textos (Tema Claro com alto contraste)
    public static Color Text => Color.FromArgb(22, 24, 30);
    public static Color TextPrimary => Color.FromArgb(22, 24, 30);
    public static Color TextSecondary => Color.FromArgb(70, 75, 90);
    public static Color TextMuted => Color.FromArgb(120, 125, 140);
    public static Color TextDisabled => Color.FromArgb(160, 165, 178);
    public static Color TextAccent => AccentColor;
    public static Color TextSelected => Color.White;

    // Seleção
    public static Color SelectionBg => AccentColor;

    // Menu Dropdown NybuOS
    public static Color StartMenuBg => Color.FromArgb(250, 251, 253);
    public static Color StartMenuSidebar => Color.FromArgb(238, 241, 246);
    public static Color StartMenuHover => AccentColor;
}
