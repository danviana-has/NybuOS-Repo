using System;
using System.Drawing;

namespace NybuOS.GUI;

/// <summary>
/// Sistema de temas moderno, minimalista e translúcido do NybuOS (Dark Glass / Flat COSMIC).
/// Todas as propriedades de cor são reativas à cor de destaque (Theme.AccentColor).
/// </summary>
public static class Theme
{
    /// <summary>
    /// Cor de destaque do sistema (Accent). Alterada pelo Painel de Controle!
    /// Padrão: Azul Neon (#3B82F6)
    /// </summary>
    public static Color AccentColor { get; set; } = Color.FromArgb(59, 130, 246);

    // Barra de Tarefas translúcida minimalista
    public static Color TaskbarBg => Color.FromArgb(16, 16, 20);
    public static Color TaskbarAlphaColor => Color.FromArgb(200, 15, 15, 18);
    public static Color TaskbarBorder => Color.FromArgb(35, 35, 42);

    // Janelas Modernas (Sem cantos arredondados, fundo escuro translúcido com requício de preto)
    public static Color WindowBg => Color.FromArgb(18, 18, 22);
    public static Color WindowHeaderBg => Color.FromArgb(26, 26, 32);
    public static Color WindowBorder => AccentColor;
    public static Color WindowBorderInactive => Color.FromArgb(45, 45, 54);

    // Superfície de controles, caixas de diálogo e painéis
    public static Color Surface => Color.FromArgb(24, 24, 30);
    public static Color SurfaceLight => Color.FromArgb(32, 32, 40);
    public static Color SurfaceHover => Color.FromArgb(42, 42, 54);
    public static Color WindowClientBg => Color.FromArgb(20, 20, 26);
    public static Color InputBg => Color.FromArgb(14, 14, 18);

    // Bordas de controle
    public static Color LightBorder => Color.FromArgb(45, 45, 56);
    public static Color ShadowBorder => Color.FromArgb(20, 20, 26);
    public static Color DarkBorder => Color.FromArgb(10, 10, 14);

    // Barra de Título (Compatibilidade com apps existentes)
    public static Color TitleActive => AccentColor;
    public static Color TitleInactive => Color.FromArgb(35, 35, 45);
    public static Color TitleTextActive => Color.White;
    public static Color TitleTextInactive => Color.FromArgb(160, 160, 175);

    // Botões e Ações
    public static Color ButtonBg => Color.FromArgb(36, 36, 46);
    public static Color ButtonAccentBg => AccentColor;
    public static Color ButtonBorder => Color.FromArgb(55, 55, 68);

    // Textos
    public static Color Text => Color.FromArgb(245, 245, 247);
    public static Color TextPrimary => Color.FromArgb(245, 245, 247);
    public static Color TextSecondary => Color.FromArgb(160, 160, 175);
    public static Color TextMuted => Color.FromArgb(110, 110, 125);
    public static Color TextDisabled => Color.FromArgb(90, 90, 105);
    public static Color TextAccent => AccentColor;
    public static Color TextSelected => Color.White;

    // Seleção
    public static Color SelectionBg => AccentColor;

    // Menu Iniciar Minimalista
    public static Color StartMenuBg => Color.FromArgb(20, 20, 26);
    public static Color StartMenuSidebar => Color.FromArgb(14, 14, 18);
    public static Color StartMenuHover => Color.FromArgb(35, 35, 45);
}
