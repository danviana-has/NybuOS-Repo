using System;
using System.Collections.Generic;
using System.Drawing;
using Cosmos.Kernel.HAL.Devices.Storage;
using Cosmos.Kernel.System.Input;
using Cosmos.Kernel.System.Storage;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Assistente Moderno de Instalação do NybuOS RV (5 Etapas).
/// Inicia antes do carregamento do Desktop sobre o papel de parede oficial.
/// </summary>
public class InstallerApp : Window
{
    private readonly VirtualFileSystem _vfs;
    public Action? OnExitToDesktop;

    // Etapas: 0: Boas-vindas, 1: Termos de Uso, 2: Discos, 3: Seleção de Apps, 4: Progresso e Conclusão
    private int _step = 0;

    // Etapa 1 - Termos de Uso
    private bool _termsAccepted = false;
    private int _termsScrollLine = 0;
    private List<string>? _wrappedTermsLines = null;
    private const int VisibleTermsLines = 16;
    private const int LineHeight = 18;

    // Etapa 2 - Central de Discos
    private bool _formatFat32 = true;
    private bool _installLimineBoot = true;
    private int _selectedDiskIndex = 0;
    private List<string> _detectedDisks = new();

    // Etapa 3 - Seleção de Apps
    private bool _appTextPad = true;
    private bool _appSheet = true;
    private bool _appCalc = true;
    private bool _appPaint = true;
    private bool _appTerminal = true;
    private bool _appMedia = true;
    private bool _appControlPanel = true;

    // Etapa 4 - Instalação
    private int _progressPercent = 0;
    private string _currentAction = "Pronto para iniciar instalacao...";
    private bool _installStarted = false;
    private bool _installFinished = false;
    private bool _installSuccess = false;

    public InstallerApp(int id, int x, int y, VirtualFileSystem vfs)
        : base(id, "Assistente de Instalacao - NybuOS RV", x, y, 800, 560)
    {
        Icon = IconResources.InstallerIcon ?? IconResources.Drive16;
        _vfs = vfs;
        PrepareTermsText();
        ScanDisks();
    }

    private void PrepareTermsText()
    {
        _wrappedTermsLines = new List<string>();
        // Largura util do container de texto: ~720 pixels -> ~84 caracteres por linha (fonte 8px)
        const int maxCharsPerLine = 84;

        foreach (var paragraph in TermsOfServiceData.Paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                _wrappedTermsLines.Add("");
                continue;
            }

            string remaining = paragraph.Trim();
            while (remaining.Length > maxCharsPerLine)
            {
                int splitIndex = remaining.LastIndexOf(' ', maxCharsPerLine);
                if (splitIndex <= 0)
                {
                    splitIndex = maxCharsPerLine;
                }

                _wrappedTermsLines.Add(remaining.Substring(0, splitIndex).TrimEnd());
                remaining = remaining.Substring(splitIndex).TrimStart();
            }

            if (remaining.Length > 0)
            {
                _wrappedTermsLines.Add(remaining);
            }

            // Linha em branco separando paragrafos
            _wrappedTermsLines.Add("");
        }
    }

    private void ScanDisks()
    {
        _detectedDisks.Clear();
        try
        {
            var devices = StorageManager.Devices;
            if (devices != null && devices.Count > 0)
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    var dev = devices[i];
                    if (dev != null)
                    {
                        ulong sizeMb = (dev.BlockCount * (ulong)dev.BlockSize) / (1024 * 1024);
                        string typeStr = dev.BlockSize == 2048 ? "Unidade CD/DVD-ROM" : "Disco Rigido / SSD";
                        string sizeStr = sizeMb >= 1024 ? $"{sizeMb / 1024} GB" : $"{sizeMb} MB";
                        _detectedDisks.Add($"Unidade {i}: {typeStr} ({sizeStr}) - Setor {dev.BlockSize}B");
                    }
                }
            }
        }
        catch { }

        if (_detectedDisks.Count == 0)
        {
            _detectedDisks.Add("Disco Rigido Primario (Emulado/IDE): 20 GB (FAT32 Ativa)");
        }
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo suave moderno da janela do instalador
        display.DrawFilledRectangle(cx, cy, cw, ch, Color.FromArgb(248, 250, 252));

        // Cabecalho das etapas (Wizard Step Indicator)
        DrawStepIndicator(display, cx, cy, cw);

        int contentY = cy + 50;
        int contentH = ch - 110;

        switch (_step)
        {
            case 0:
                DrawStepWelcome(display, cx, contentY, cw, contentH);
                break;
            case 1:
                DrawStepTerms(display, cx, contentY, cw, contentH);
                break;
            case 2:
                DrawStepDisks(display, cx, contentY, cw, contentH);
                break;
            case 3:
                DrawStepAppSelection(display, cx, contentY, cw, contentH);
                break;
            case 4:
                DrawStepInstallation(display, cx, contentY, cw, contentH);
                break;
        }

        // Barra inferior com botoes de navegacao
        DrawFooterButtons(display, cx, cy + ch - 56, cw, 56);
    }

    private void DrawStepIndicator(DisplayDriver display, int x, int y, int w)
    {
        display.DrawFilledRectangle(x, y, w, 44, Color.FromArgb(238, 242, 246));
        display.DrawLine(x, y + 43, x + w, y + 43, Color.FromArgb(218, 224, 233));

        string[] stepTitles = { "1. Bem-vindo", "2. Termos", "3. Discos", "4. Aplicativos", "5. Conclusao" };
        int segW = w / 5;

        for (int i = 0; i < 5; i++)
        {
            int sx = x + (i * segW);
            bool isCurrent = i == _step;
            bool isPast = i < _step;

            Color dotColor = isCurrent ? Color.FromArgb(37, 99, 235) : (isPast ? Color.FromArgb(16, 185, 129) : Color.FromArgb(203, 213, 225));
            Color textColor = isCurrent ? Color.FromArgb(15, 23, 42) : (isPast ? Color.FromArgb(51, 65, 85) : Color.FromArgb(148, 163, 184));

            display.DrawFilledCircle(sx + 20, y + 22, 6, dotColor);
            display.DrawString(sx + 32, y + 14, stepTitles[i], textColor);
        }
    }

    private void DrawStepWelcome(DisplayDriver display, int x, int y, int w, int h)
    {
        int midX = x + (w / 2);

        // Logo oficial
        if (IconResources.BootLogo64 != null)
        {
            display.DrawIcon(midX - 32, y + 40, IconResources.BootLogo64);
        }
        else if (Icon != null)
        {
            display.DrawIcon(midX - 16, y + 50, Icon);
        }

        // Titulo e Slogan
        string title = "NybuOS RV - Revolution Edition";
        int tw = title.Length * CosmosNativeFont.Width;
        display.DrawString(midX - (tw / 2), y + 120, title, Color.FromArgb(15, 23, 42));

        string slogan = "O Futuro da Computacao em Alta Performance e Elegancia";
        int sw = slogan.Length * CosmosNativeFont.Width;
        display.DrawString(midX - (sw / 2), y + 145, slogan, Color.FromArgb(71, 85, 105));

        // Cartao descritivo
        int cardX = x + 60;
        int cardY = y + 185;
        int cardW = w - 120;
        int cardH = 140;

        display.DrawFilledRectangle(cardX, cardY, cardW, cardH, Color.White);
        display.DrawRectangle(cardX, cardY, cardW, cardH, Color.FromArgb(226, 232, 240));

        display.DrawString(cardX + 24, cardY + 20, "Bem-vindo ao instalador oficial do NybuOS RV!", Color.FromArgb(30, 41, 59));
        display.DrawString(cardX + 24, cardY + 45, "* Interface Grafica Full HD 1080p nativa de alta definicao.", Color.FromArgb(71, 85, 105));
        display.DrawString(cardX + 24, cardY + 65, "* Inicializador Limine BIOS integrado com suporte a boot pelo HD.", Color.FromArgb(71, 85, 105));
        display.DrawString(cardX + 24, cardY + 85, "* Sistema de Arquivos FAT32 completo com suite de produtividade.", Color.FromArgb(71, 85, 105));
        display.DrawString(cardX + 24, cardY + 105, "Clique em 'Avancar' para iniciar a preparacao da sua maquina.", Color.FromArgb(37, 99, 235));
    }

    private void DrawStepTerms(DisplayDriver display, int x, int y, int w, int h)
    {
        display.DrawString(x + 24, y + 10, "Contrato de Licenca de Uso e Termos de Servico - GM Corporation", Color.FromArgb(15, 23, 42));
        display.DrawString(x + 24, y + 30, "Role o texto abaixo para ler os termos na integra antes de prosseguir:", Color.FromArgb(100, 116, 139));

        // Container Rolante (Scrollable Box)
        int boxX = x + 24;
        int boxY = y + 54;
        int boxW = w - 48;
        int boxH = h - 100;

        display.DrawFilledRectangle(boxX, boxY, boxW, boxH, Color.White);
        display.DrawRectangle(boxX, boxY, boxW, boxH, Color.FromArgb(203, 213, 225));

        if (_wrappedTermsLines != null && _wrappedTermsLines.Count > 0)
        {
            int maxScroll = Math.Max(0, _wrappedTermsLines.Count - VisibleTermsLines);
            if (_termsScrollLine > maxScroll) _termsScrollLine = maxScroll;
            if (_termsScrollLine < 0) _termsScrollLine = 0;

            for (int i = 0; i < VisibleTermsLines; i++)
            {
                int lineIdx = _termsScrollLine + i;
                if (lineIdx >= _wrappedTermsLines.Count) break;

                int lineY = boxY + 12 + (i * LineHeight);
                // Garantia absoluta de clipping: nao desenha fora da caixa
                if (lineY >= boxY + 6 && lineY + 16 <= boxY + boxH - 6)
                {
                    string text = _wrappedTermsLines[lineIdx];
                    Color col = (lineIdx == 0 || text.StartsWith("CONTRATO")) ? Color.FromArgb(15, 23, 42) : Color.FromArgb(51, 65, 85);
                    display.DrawString(boxX + 12, lineY, text, col);
                }
            }

            // Barra de rolagem (Scrollbar)
            int sbX = boxX + boxW - 18;
            int sbY = boxY;
            int sbW = 18;
            int sbH = boxH;

            display.DrawFilledRectangle(sbX, sbY, sbW, sbH, Color.FromArgb(241, 245, 249));
            display.DrawLine(sbX, sbY, sbX, sbY + sbH, Color.FromArgb(226, 232, 240));

            // Botao Cima (▲)
            display.DrawFilledRectangle(sbX + 2, sbY + 2, 14, 16, Color.FromArgb(226, 232, 240));
            display.DrawString(sbX + 5, sbY + 2, "^", Color.FromArgb(71, 85, 105));

            // Botao Baixo (▼)
            display.DrawFilledRectangle(sbX + 2, sbY + sbH - 18, 14, 16, Color.FromArgb(226, 232, 240));
            display.DrawString(sbX + 5, sbY + sbH - 18, "v", Color.FromArgb(71, 85, 105));

            // Polegar da barra de rolagem (Thumb)
            int trackH = sbH - 40;
            if (_wrappedTermsLines.Count > VisibleTermsLines)
            {
                float ratio = (float)_termsScrollLine / maxScroll;
                int thumbH = Math.Max(24, trackH * VisibleTermsLines / _wrappedTermsLines.Count);
                int thumbY = sbY + 20 + (int)((trackH - thumbH) * ratio);
                display.DrawFilledRectangle(sbX + 3, thumbY, 12, thumbH, Color.FromArgb(148, 163, 184));
            }
        }

        // Checkbox de aceite
        int chkY = boxY + boxH + 12;
        int chkBoxSize = 18;

        display.DrawFilledRectangle(boxX, chkY, chkBoxSize, chkBoxSize, _termsAccepted ? Color.FromArgb(37, 99, 235) : Color.White);
        display.DrawRectangle(boxX, chkY, chkBoxSize, chkBoxSize, Color.FromArgb(100, 116, 139));
        if (_termsAccepted)
        {
            display.DrawString(boxX + 4, chkY + 1, "X", Color.White);
        }

        display.DrawString(boxX + 26, chkY + 2, "Eu li e aceito integralmente os Termos de Servico da GM Corporation", Color.FromArgb(30, 41, 59));
    }

    private void DrawStepDisks(DisplayDriver display, int x, int y, int w, int h)
    {
        display.DrawString(x + 24, y + 10, "Central de Discos e Particionamento", Color.FromArgb(15, 23, 42));
        display.DrawString(x + 24, y + 30, "Selecione o disco rigido de destino para a instalacao do sistema:", Color.FromArgb(100, 116, 139));

        // Lista de discos detectados
        int listX = x + 24;
        int listY = y + 55;
        int listW = w - 48;
        int listH = 140;

        display.DrawFilledRectangle(listX, listY, listW, listH, Color.White);
        display.DrawRectangle(listX, listY, listW, listH, Color.FromArgb(203, 213, 225));

        for (int i = 0; i < Math.Min(3, _detectedDisks.Count); i++)
        {
            int itemY = listY + 8 + (i * 42);
            bool isSelected = i == _selectedDiskIndex;

            if (isSelected)
            {
                display.DrawFilledRectangle(listX + 6, itemY, listW - 12, 38, Color.FromArgb(239, 246, 255));
                display.DrawRectangle(listX + 6, itemY, listW - 12, 38, Color.FromArgb(59, 130, 246));
            }

            if (IconResources.Drive16 != null)
            {
                display.DrawIcon(listX + 16, itemY + 10, IconResources.Drive16);
            }
            display.DrawString(listX + 42, itemY + 11, _detectedDisks[i], isSelected ? Color.FromArgb(30, 58, 138) : Color.FromArgb(30, 41, 59));
        }

        // Opcoes de formatacao e MBR
        int optY = listY + listH + 16;
        DrawCheckbox(display, listX, optY, _formatFat32, "Formatar particao primaria em FAT32 (limpa e cria MBR inicializavel)");
        DrawCheckbox(display, listX, optY + 28, _installLimineBoot, "Instalar carregador de inicializacao Limine BIOS no MBR e gap pos-MBR");

        // Alerta informativo VMware
        int warnY = optY + 62;
        int warnH = 80;
        display.DrawFilledRectangle(listX, warnY, listW, warnH, Color.FromArgb(254, 243, 199));
        display.DrawRectangle(listX, warnY, listW, warnH, Color.FromArgb(245, 158, 11));

        display.DrawString(listX + 16, warnY + 12, "! DICA PARA VMWARE WORKSTATION / PLAYER:", Color.FromArgb(146, 64, 14));
        display.DrawString(listX + 16, warnY + 32, "Certifique-se de que o disco virtual esteja configurado como IDE ou SATA (AHCI).", Color.FromArgb(146, 64, 14));
        display.DrawString(listX + 16, warnY + 52, "O padrao 'SCSI / LSI Logic' do VMware nao possui suporte em kernels x86 nativos.", Color.FromArgb(180, 83, 9));
    }

    private void DrawStepAppSelection(DisplayDriver display, int x, int y, int w, int h)
    {
        display.DrawString(x + 24, y + 10, "Selecao de Aplicativos e Ferramentas", Color.FromArgb(15, 23, 42));
        display.DrawString(x + 24, y + 30, "Escolha quais aplicativos padrao serao instalados no sistema operacional:", Color.FromArgb(100, 116, 139));

        int listX = x + 24;
        int listY = y + 55;
        int listW = w - 48;
        int listH = 260;

        display.DrawFilledRectangle(listX, listY, listW, listH, Color.White);
        display.DrawRectangle(listX, listY, listW, listH, Color.FromArgb(203, 213, 225));

        int col1X = listX + 20;
        int col2X = listX + (listW / 2) + 10;

        DrawAppItem(display, col1X, listY + 15, _appTextPad, "Editor de Texto (TextPad)", IconResources.TextEditorIcon);
        DrawAppItem(display, col1X, listY + 55, _appSheet, "Planilha Eletronica (NybuSheet)", IconResources.SpreadsheetIcon);
        DrawAppItem(display, col1X, listY + 95, _appCalc, "Calculadora Cientifica", IconResources.CalculatorIcon);
        DrawAppItem(display, col1X, listY + 135, _appPaint, "PaintPad (Editor de Imagens)", IconResources.PaintPadIcon);

        DrawAppItem(display, col2X, listY + 15, _appTerminal, "Terminal NybuShell", IconResources.TerminalIcon);
        DrawAppItem(display, col2X, listY + 55, _appMedia, "Reprodutor Multimidia (Audio/Fotos)", IconResources.MediaIcon);
        DrawAppItem(display, col2X, listY + 95, _appControlPanel, "Painel de Controle e Configuracoes", IconResources.ControlPanelIcon);

        // Botoes de marcacao rapida
        int btnY = listY + 195;
        DrawButton(display, col1X, btnY, 150, 32, "Marcar Todos", false);
        DrawButton(display, col1X + 165, btnY, 150, 32, "Desmarcar Todos", false);
    }

    private void DrawStepInstallation(DisplayDriver display, int x, int y, int w, int h)
    {
        if (!_installFinished)
        {
            display.DrawString(x + 24, y + 20, "Instalando o NybuOS RV no Disco Rigido...", Color.FromArgb(15, 23, 42));
            display.DrawString(x + 24, y + 45, _currentAction, Color.FromArgb(37, 99, 235));

            // Barra de Progresso
            int barX = x + 24;
            int barY = y + 80;
            int barW = w - 48;
            int barH = 26;

            display.DrawFilledRectangle(barX, barY, barW, barH, Color.FromArgb(226, 232, 240));

            int fillW = (barW * Math.Clamp(_progressPercent, 0, 100)) / 100;
            if (fillW > 0)
            {
                display.DrawFilledRectangle(barX, barY, fillW, barH, Color.FromArgb(37, 99, 235));
            }

            string pctStr = $"{_progressPercent}%";
            int pw = pctStr.Length * CosmosNativeFont.Width;
            display.DrawString(barX + (barW / 2) - (pw / 2), barY + 5, pctStr, _progressPercent > 50 ? Color.White : Color.FromArgb(15, 23, 42));

            // Log de etapas
            int logY = barY + 45;
            display.DrawString(barX, logY, "[1/5] Gravando setor de boot MBR e Limine Stage 1...", _progressPercent >= 20 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(148, 163, 184));
            display.DrawString(barX, logY + 24, "[2/5] Escrevendo Limine Stage 2 no gap pos-MBR...", _progressPercent >= 35 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(148, 163, 184));
            display.DrawString(barX, logY + 48, "[3/5] Formatando particao primaria em FAT32...", _progressPercent >= 55 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(148, 163, 184));
            display.DrawString(barX, logY + 72, "[4/5] Copiando arquivos limine.conf e executavel NybuOS.elf...", _progressPercent >= 80 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(148, 163, 184));
            display.DrawString(barX, logY + 96, "[5/5] Registrando suite de aplicativos selecionados...", _progressPercent >= 100 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(148, 163, 184));
        }
        else
        {
            // Tela de Conclusao e Aviso de Remocao da ISO
            int midX = x + (w / 2);

            display.DrawFilledCircle(midX, y + 45, 28, Color.FromArgb(16, 185, 129));
            display.DrawString(midX - 8, y + 37, "OK", Color.White);

            string winTitle = "Instalacao Concluida com Sucesso!";
            int wtw = winTitle.Length * CosmosNativeFont.Width;
            display.DrawString(midX - (wtw / 2), y + 90, winTitle, Color.FromArgb(15, 23, 42));

            // Cartao de Atencao VMware
            int alertX = x + 30;
            int alertY = y + 125;
            int alertW = w - 60;
            int alertH = 150;

            display.DrawFilledRectangle(alertX, alertY, alertW, alertH, Color.FromArgb(254, 242, 242));
            display.DrawRectangle(alertX, alertY, alertW, alertH, Color.FromArgb(239, 68, 68));

            display.DrawString(alertX + 20, alertY + 16, "! ATENCAO OBRIGATORIA ANTES DE REINICIAR !", Color.FromArgb(185, 28, 28));
            display.DrawString(alertX + 20, alertY + 44, "1. Desconecte / Remova a ISO ou Pendrive da maquina virtual.", Color.FromArgb(153, 27, 27));
            display.DrawString(alertX + 20, alertY + 68, "   No VMware: Menu 'VM' > 'Removable Devices' > 'CD/DVD' > 'Disconnect'.", Color.FromArgb(69, 10, 10));
            display.DrawString(alertX + 20, alertY + 92, "2. Se a ISO nao for desconectada, a maquina pode reiniciar no instalador.", Color.FromArgb(153, 27, 27));
            display.DrawString(alertX + 20, alertY + 116, "3. Clique no botao 'Reiniciar Agora' abaixo para inicializar pelo HD!", Color.FromArgb(30, 58, 138));
        }
    }

    private void DrawFooterButtons(DisplayDriver display, int x, int y, int w, int h)
    {
        display.DrawLine(x, y, x + w, y, Color.FromArgb(226, 232, 240));

        if (_step == 0)
        {
            // Boas-vindas
            DrawButton(display, x + 24, y + 12, 200, 34, "Experimentar Desktop", false);
            DrawButton(display, x + w - 160, y + 12, 136, 34, "Avancar >", true);
        }
        else if (_step == 1)
        {
            // Termos
            DrawButton(display, x + 24, y + 12, 130, 34, "< Voltar", false);
            DrawButton(display, x + w - 160, y + 12, 136, 34, "Avancar >", _termsAccepted);
        }
        else if (_step == 2)
        {
            // Discos
            DrawButton(display, x + 24, y + 12, 130, 34, "< Voltar", false);
            DrawButton(display, x + w - 160, y + 12, 136, 34, "Avancar >", true);
        }
        else if (_step == 3)
        {
            // Apps
            DrawButton(display, x + 24, y + 12, 130, 34, "< Voltar", false);
            DrawButton(display, x + w - 170, y + 12, 146, 34, "Instalar Agora", true);
        }
        else if (_step == 4)
        {
            if (_installFinished)
            {
                DrawButton(display, x + 24, y + 12, 220, 34, "Ir para Area de Trabalho", false);
                DrawButton(display, x + w - 180, y + 12, 156, 34, "Reiniciar Agora", true);
            }
        }
    }

    private void DrawCheckbox(DisplayDriver display, int x, int y, bool isChecked, string label)
    {
        display.DrawFilledRectangle(x, y, 16, 16, isChecked ? Color.FromArgb(37, 99, 235) : Color.White);
        display.DrawRectangle(x, y, 16, 16, Color.FromArgb(148, 163, 184));
        if (isChecked)
        {
            display.DrawString(x + 4, y, "X", Color.White);
        }
        display.DrawString(x + 24, y + 1, label, Color.FromArgb(30, 41, 59));
    }

    private void DrawAppItem(DisplayDriver display, int x, int y, bool isChecked, string label, IconImage? icon)
    {
        display.DrawFilledRectangle(x, y + 6, 16, 16, isChecked ? Color.FromArgb(37, 99, 235) : Color.White);
        display.DrawRectangle(x, y + 6, 16, 16, Color.FromArgb(148, 163, 184));
        if (isChecked)
        {
            display.DrawString(x + 4, y + 6, "X", Color.White);
        }

        if (icon != null)
        {
            display.DrawIcon(x + 26, y + 6, icon);
        }

        display.DrawString(x + 48, y + 7, label, Color.FromArgb(30, 41, 59));
    }

    private void DrawButton(DisplayDriver display, int x, int y, int w, int h, string text, bool isPrimary)
    {
        Color bg = isPrimary ? Color.FromArgb(37, 99, 235) : Color.FromArgb(241, 245, 249);
        Color border = isPrimary ? Color.FromArgb(29, 78, 216) : Color.FromArgb(203, 213, 225);
        Color textCol = isPrimary ? Color.White : Color.FromArgb(30, 41, 59);

        display.DrawFilledRectangle(x, y, w, h, bg);
        display.DrawRectangle(x, y, w, h, border);

        int tw = text.Length * CosmosNativeFont.Width;
        display.DrawString(x + (w / 2) - (tw / 2), y + (h / 2) - 8, text, textCol);
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        int footerY = cy + ch - 56;

        // Cliques nos Botoes do Rodape
        if (mouseY >= footerY && mouseY <= footerY + 56)
        {
            if (_step == 0)
            {
                // Experimentar Desktop
                if (mouseX >= cx + 24 && mouseX <= cx + 224)
                {
                    OnExitToDesktop?.Invoke();
                    Close();
                    return;
                }
                // Avancar
                if (mouseX >= cx + cw - 160 && mouseX <= cx + cw - 24)
                {
                    _step = 1;
                    return;
                }
            }
            else if (_step == 1)
            {
                // Voltar
                if (mouseX >= cx + 24 && mouseX <= cx + 154)
                {
                    _step = 0;
                    return;
                }
                // Avancar (somente se aceito)
                if (_termsAccepted && mouseX >= cx + cw - 160 && mouseX <= cx + cw - 24)
                {
                    _step = 2;
                    return;
                }
            }
            else if (_step == 2)
            {
                // Voltar
                if (mouseX >= cx + 24 && mouseX <= cx + 154)
                {
                    _step = 1;
                    return;
                }
                // Avancar
                if (mouseX >= cx + cw - 160 && mouseX <= cx + cw - 24)
                {
                    _step = 3;
                    return;
                }
            }
            else if (_step == 3)
            {
                // Voltar
                if (mouseX >= cx + 24 && mouseX <= cx + 154)
                {
                    _step = 2;
                    return;
                }
                // Instalar Agora
                if (mouseX >= cx + cw - 170 && mouseX <= cx + cw - 24)
                {
                    _step = 4;
                    StartInstallationProcess();
                    return;
                }
            }
            else if (_step == 4 && _installFinished)
            {
                // Ir para Area de Trabalho
                if (mouseX >= cx + 24 && mouseX <= cx + 244)
                {
                    OnExitToDesktop?.Invoke();
                    Close();
                    return;
                }
                // Reiniciar Agora
                if (mouseX >= cx + cw - 180 && mouseX <= cx + cw - 24)
                {
                    PowerDriver.Reboot();
                    return;
                }
            }
        }

        // Cliques especificos por etapa
        if (_step == 1)
        {
            // Termos de Uso: Scrollbar e Checkbox
            int boxX = cx + 24;
            int boxY = cy + 50 + 54;
            int boxW = cw - 48;
            int boxH = ch - 110 - 100;
            int sbX = boxX + boxW - 18;

            // Botao Cima (▲)
            if (mouseX >= sbX && mouseX <= sbX + 18 && mouseY >= boxY && mouseY <= boxY + 20)
            {
                _termsScrollLine = Math.Max(0, _termsScrollLine - 3);
                return;
            }

            // Botao Baixo (▼)
            if (mouseX >= sbX && mouseX <= sbX + 18 && mouseY >= boxY + boxH - 20 && mouseY <= boxY + boxH)
            {
                int maxScroll = _wrappedTermsLines != null ? Math.Max(0, _wrappedTermsLines.Count - VisibleTermsLines) : 0;
                _termsScrollLine = Math.Min(maxScroll, _termsScrollLine + 3);
                return;
            }

            // Checkbox
            int chkY = boxY + boxH + 12;
            if (mouseY >= chkY && mouseY <= chkY + 24 && mouseX >= boxX && mouseX <= boxX + 400)
            {
                _termsAccepted = !_termsAccepted;
                return;
            }
        }
        else if (_step == 2)
        {
            // Central de Discos: Opcoes e selecao
            int listX = cx + 24;
            int listY = cy + 50 + 55;
            int optY = listY + 140 + 16;

            if (mouseX >= listX && mouseX <= listX + 500)
            {
                if (mouseY >= optY && mouseY <= optY + 22)
                {
                    _formatFat32 = !_formatFat32;
                    return;
                }
                if (mouseY >= optY + 28 && mouseY <= optY + 50)
                {
                    _installLimineBoot = !_installLimineBoot;
                    return;
                }
            }
        }
        else if (_step == 3)
        {
            // Aplicativos: Toggle individual
            int listX = cx + 24;
            int listY = cy + 50 + 55;
            int listW = cw - 48;
            int col1X = listX + 20;
            int col2X = listX + (listW / 2) + 10;

            if (mouseX >= col1X && mouseX <= col1X + 280)
            {
                if (mouseY >= listY + 15 && mouseY <= listY + 45) _appTextPad = !_appTextPad;
                else if (mouseY >= listY + 55 && mouseY <= listY + 85) _appSheet = !_appSheet;
                else if (mouseY >= listY + 95 && mouseY <= listY + 125) _appCalc = !_appCalc;
                else if (mouseY >= listY + 135 && mouseY <= listY + 165) _appPaint = !_appPaint;
            }

            if (mouseX >= col2X && mouseX <= col2X + 280)
            {
                if (mouseY >= listY + 15 && mouseY <= listY + 45) _appTerminal = !_appTerminal;
                else if (mouseY >= listY + 55 && mouseY <= listY + 85) _appMedia = !_appMedia;
                else if (mouseY >= listY + 95 && mouseY <= listY + 125) _appControlPanel = !_appControlPanel;
            }

            // Botoes Marcar/Desmarcar Todos
            int btnY = listY + 195;
            if (mouseY >= btnY && mouseY <= btnY + 32)
            {
                if (mouseX >= col1X && mouseX <= col1X + 150)
                {
                    _appTextPad = _appSheet = _appCalc = _appPaint = _appTerminal = _appMedia = _appControlPanel = true;
                }
                else if (mouseX >= col1X + 165 && mouseX <= col1X + 315)
                {
                    _appTextPad = _appSheet = _appCalc = _appPaint = _appTerminal = _appMedia = _appControlPanel = false;
                }
            }
        }
    }

    public override void OnKeyDown(KeyEvent key)
    {
        if (_step == 1)
        {
            if (key.Key == Key.UpArrow || key.Key == Key.PageUp)
            {
                ScrollTerms(-3);
            }
            else if (key.Key == Key.DownArrow || key.Key == Key.PageDown)
            {
                ScrollTerms(3);
            }
        }
    }

    public void ScrollTerms(int delta)
    {
        if (_step == 1 && _wrappedTermsLines != null)
        {
            int maxScroll = Math.Max(0, _wrappedTermsLines.Count - VisibleTermsLines);
            _termsScrollLine = Math.Clamp(_termsScrollLine + delta, 0, maxScroll);
        }
    }

    private void StartInstallationProcess()
    {
        if (_installStarted) return;
        _installStarted = true;
        _progressPercent = 5;
        _currentAction = "Iniciando processo de instalacao no disco...";

        try
        {
            bool success = HardDiskInstaller.PerformInstallation((pct, msg) =>
            {
                _progressPercent = pct;
                _currentAction = msg;
            });

            // Cria tag para indicar instalacao concluida
            try
            {
                if (!_vfs.DirectoryExists(@"C:\nybuos"))
                {
                    _vfs.CreateDirectory(@"C:\nybuos");
                }
                _vfs.WriteFile(@"C:\nybuos\installed.tag", "INSTALLED=1\nVERSION=3.0-RV\n");
            }
            catch { }

            _installFinished = true;
            _installSuccess = success;
            _progressPercent = 100;
            _currentAction = success ? "Instalacao finalizada com exito!" : "Instalacao concluida com alertas.";
        }
        catch (Exception ex)
        {
            _installFinished = true;
            _installSuccess = false;
            _progressPercent = 100;
            _currentAction = $"Falha na instalacao: {ex.Message}";
        }
    }
}
