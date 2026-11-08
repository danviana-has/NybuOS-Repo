using System;
using System.Drawing;

namespace NybuOS.GUI;

/// <summary>
/// Sistema de temas do NybuOS Purple:
/// Interface minimalista reformulada com efeito de vidro fosco translúcido e desfoque (estilo macOS).
/// Dock inferior com borda grossa cinza em relevo 3D e centro translúcido com desfoque dos elementos ao fundo.
/// Barra superior translúcida e cabeçalhos de janela com acabamento de vidro refinado.
/// </summary>
public static class Theme
{
    /// <summary>
    /// Cor de destaque principal do sistema: Violeta / Roxo moderno do NybuOS Purple (#8E44ED).
    /// </summary>
    public static Color AccentColor { get; set; } = Color.FromArgb(142, 68, 237);

    // Barra Superior Translúcida (macOS Style - 62.7% opacidade / 37.3% transparência)
    public static Color TopBarBg => Color.FromArgb(242, 244, 250);
    public static Color TopBarAlphaColor => Color.FromArgb(160, 242, 244, 250);
    public static Color TopBarBorder => Color.FromArgb(180, 184, 196);

    // Dock Flutuante (Borda grossa cinza 3D e centro quase transparente - 8% opacidade / 92% transparência)
    public static Color DockBg => Color.FromArgb(240, 242, 248);
    public static Color DockAlphaColor => Color.FromArgb(20, 240, 242, 248);
    public static Color DockBorderOuterHighlight => Color.FromArgb(215, 218, 228); // 3D highlight superior
    public static Color DockBorderOuterShadow => Color.FromArgb(120, 124, 136);    // 3D shadow inferior
    public static Color DockBorderMid => Color.FromArgb(165, 170, 182);            // Borda cinza sólida
    public static Color DockBorderInner => Color.FromArgb(145, 150, 162);          // Contorno interno
    public static Color DockBorder => DockBorderMid;
    public static Color DockHighlight => Color.FromArgb(255, 255, 255);
    public static Color DockSeparator => Color.FromArgb(175, 180, 194);

    // Barra de Tarefas
    public static Color TaskbarBg => TopBarBg;
    public static Color TaskbarAlphaColor => TopBarAlphaColor;
    public static Color TaskbarBorder => TopBarBorder;

    // Janelas (Design minimalista translúcido estilo macOS - 62.7% a 66% opacidade)
    public static Color WindowBg => Color.FromArgb(248, 249, 252);
    public static Color WindowHeaderBg => Color.FromArgb(240, 242, 248);
    public static Color WindowHeaderAlphaColor => Color.FromArgb(160, 240, 242, 248);
    public static Color WindowBorder => Color.FromArgb(160, 165, 178);
    public static Color WindowBorderInactive => Color.FromArgb(195, 200, 210);

    // Botões Circulares de Janela (Traffic Lights estilo macOS)
    public static Color CloseButton => Color.FromArgb(255, 95, 86);         // #FF5F56
    public static Color CloseButtonBorder => Color.FromArgb(224, 68, 62);   // #E0443E
    public static Color MinimizeButton => Color.FromArgb(255, 189, 46);      // #FFBD2E
    public static Color MinimizeButtonBorder => Color.FromArgb(222, 161, 35); // #DEA123
    public static Color MaximizeButton => Color.FromArgb(39, 201, 63);       // #27C93F
    public static Color MaximizeButtonBorder => Color.FromArgb(26, 171, 41);  // #1AAB29

    // Superfície de controles, caixas de diálogo e painéis
    public static Color Surface => Color.FromArgb(255, 255, 255);
    public static Color SurfaceLight => Color.FromArgb(244, 245, 250);
    public static Color SurfaceHover => Color.FromArgb(232, 235, 244);
    public static Color WindowClientBg => Color.FromArgb(248, 249, 252);
    public static Color InputBg => Color.FromArgb(255, 255, 255);

    // Bordas de controle
    public static Color LightBorder => Color.FromArgb(255, 255, 255);
    public static Color ShadowBorder => Color.FromArgb(175, 180, 192);
    public static Color DarkBorder => Color.FromArgb(135, 140, 154);

    // Barra de Título
    public static Color TitleActive => AccentColor;
    public static Color TitleInactive => Color.FromArgb(215, 218, 226);
    public static Color TitleTextActive => Color.FromArgb(22, 24, 30);
    public static Color TitleTextInactive => Color.FromArgb(110, 115, 130);

    // Botões e Ações
    public static Color ButtonBg => Color.FromArgb(238, 240, 246);
    public static Color ButtonAccentBg => AccentColor;
    public static Color ButtonBorder => Color.FromArgb(180, 185, 198);

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
    public static Color StartMenuBg => Color.FromArgb(252, 252, 254);
    public static Color StartMenuSidebar => Color.FromArgb(240, 242, 248);
    public static Color StartMenuHover => AccentColor;
}
