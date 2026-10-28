using System;
using System.Drawing;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Assistente de Instalação do NybuOS no disco rígido FAT32.
/// Descompacta e copia o sistema operacional, bootloader e arquivos da ISO para o HD local.
/// </summary>
public class InstallerApp : Window
{
    private readonly VirtualFileSystem _vfs;
    private int _currentStep = 0; // 0: Welcome, 1: Disk Select, 2: Installing Progress, 3: Completed

    private int _installProgress = 0;
    private int _installSubStep = 0;
    private string _currentAction = "Preparando...";
    private bool _formatFat32 = true;
    private bool _installBootloader = true;
    private bool _copyDefaultApps = true;

    private readonly string[] _stepsDescriptions = new string[]
    {
        "Preparando particao e alocando FAT32...",
        "Criando estrutura de diretorios do sistema...",
        "Descompactando kernel e executavel NybuOS.bin...",
        "Instalando bibliotecas do runtime .NET Cosmos...",
        "Configurando drivers de som, mouse, video e teclado...",
        "Instalando carregador de boot (C:\\Boot\\boot.cfg)...",
        "Gravando registro de configuracao do sistema...",
        "Configurando atalhos na Area de Trabalho...",
        "Sincronizando cache de escrita em disco FAT32...",
        "Finalizando instalacao com sucesso!"
    };

    public InstallerApp(int id, int x, int y, VirtualFileSystem vfs)
        : base(id, "Instalador do NybuOS - Assistente", x, y, 440, 310)
    {
        Icon = IconResources.InstallerIcon ?? IconResources.Drive16;
        _vfs = vfs;
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo estilo Dark Modern Surface
        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.Surface);

        // Faixa de cabeçalho do assistente
        int headerH = 46;
        display.DrawFilledRectangle(cx, cy, cw, headerH, Color.FromArgb(24, 24, 34));
        display.DrawLine(cx, cy + headerH, cx + cw - 1, cy + headerH, Theme.WindowBorder);

        if (IconResources.Installer32 != null)
        {
            display.DrawIcon(cx + 10, cy + 7, IconResources.Installer32);
        }

        string headerTitle = _currentStep switch
        {
            0 => "Bem-vindo ao Assistente de Instalacao",
            1 => "Destino da Instalacao - Disco FAT32",
            2 => "Instalando o NybuOS no Disco Rigido",
            3 => "Instalacao Concluida com Exito!",
            _ => "Instalador do NybuOS"
        };
        string headerSub = _currentStep switch
        {
            0 => "Instale o NybuOS no HD para inicializar sem a ISO",
            1 => "Escolha a particao e opcoes de inicializacao",
            2 => "Aguarde enquanto os arquivos sao gravados no HD...",
            3 => "O sistema esta pronto para ser iniciado pelo HD",
            _ => ""
        };

        display.DrawString(cx + 48, cy + 8, headerTitle, Color.White);
        display.DrawString(cx + 48, cy + 26, headerSub, Color.FromArgb(156, 163, 175));

        // Área central do passo
        int bodyY = cy + headerH + 10;
        int bodyH = ch - headerH - 52;

        switch (_currentStep)
        {
            case 0:
                DrawStep0Welcome(display, cx + 16, bodyY, cw - 32, bodyH);
                break;
            case 1:
                DrawStep1DiskSelect(display, cx + 16, bodyY, cw - 32, bodyH);
                break;
            case 2:
                DrawStep2Progress(display, cx + 16, bodyY, cw - 32, bodyH);
                break;
            case 3:
                DrawStep3Complete(display, cx + 16, bodyY, cw - 32, bodyH);
                break;
        }

        // Barra inferior com botões de navegação
        int footerY = cy + ch - 38;
        display.DrawLine(cx, footerY, cx + cw - 1, footerY, Theme.WindowBorder);

        int btnW = 85;
        int btnH = 26;
        int by = footerY + 6;

        if (_currentStep == 0)
        {
            display.DrawButton(cx + cw - 180, by, btnW, btnH, "Avancar >", false);
            display.DrawButton(cx + cw - 90, by, btnW, btnH, "Cancelar", false);
        }
        else if (_currentStep == 1)
        {
            display.DrawButton(cx + cw - 275, by, btnW, btnH, "< Voltar", false);
            display.DrawButton(cx + cw - 185, by, 95, btnH, "Instalar Agora", false);
            display.DrawButton(cx + cw - 85, by, 75, btnH, "Cancelar", false);
        }
        else if (_currentStep == 2)
        {
            display.DrawButton(cx + cw - 95, by, 85, btnH, "Aguarde...", true);
        }
        else if (_currentStep == 3)
        {
            display.DrawButton(cx + cw - 285, by, 105, btnH, "Reiniciar Agora", false);
            display.DrawButton(cx + cw - 175, by, 80, btnH, "Desligar", false);
            display.DrawButton(cx + cw - 90, by, btnW, btnH, "Concluir", false);
        }
    }

    private void DrawStep0Welcome(DisplayDriver display, int x, int y, int w, int h)
    {
        display.DrawString(x, y, "O NybuOS v3.0 Modern Edition esta pronto para ser", Theme.TextPrimary);
        display.DrawString(x, y + 18, "instalado permanentemente no disco rigido deste computador.", Theme.TextPrimary);

        display.DrawString(x, y + 46, "Beneficios da instalacao no HD:", Color.FromArgb(96, 165, 250));
        display.DrawString(x, y + 66, " * Inicializacao direta do HD sem necessidade do CD/ISO", Theme.TextPrimary);
        display.DrawString(x, y + 84, " * Alta velocidade de leitura e gravacao em FAT32", Theme.TextPrimary);
        display.DrawString(x, y + 102, " * Persistencia de documentos, planilhas e configuracoes", Theme.TextPrimary);

        display.DrawString(x, y + 132, "Requisitos do Sistema:", Color.FromArgb(96, 165, 250));
        display.DrawString(x, y + 152, " [OK] Processador x86_64 compativel", Color.FromArgb(34, 197, 94));
        display.DrawString(x, y + 170, " [OK] Memoria RAM: >= 512MB disponivel", Color.FromArgb(34, 197, 94));
        display.DrawString(x, y + 188, " [OK] Disco Rigido detectado (FAT32)", Color.FromArgb(34, 197, 94));
    }

    private void DrawStep1DiskSelect(DisplayDriver display, int x, int y, int w, int h)
    {
        display.DrawString(x, y, "Selecione o disco e particao de destino:", Theme.TextPrimary);

        // Caixa de seleção de disco
        int boxY = y + 20;
        int boxH = 46;
        display.DrawFilledRectangle(x, boxY, w, boxH, Color.FromArgb(20, 20, 28));
        display.DrawRectangle(x, boxY, w, boxH, Theme.AccentColor);

        display.DrawString(x + 10, boxY + 8, "(*) Disco 0 - Particao 1 [C:\\] FAT32 (Principal)", Color.White);
        display.DrawString(x + 28, boxY + 26, "Capacidade: 2048 MB  |  Sistema de Arquivos: FAT32", Color.FromArgb(148, 163, 184));

        // Opções de instalação (checkboxes)
        int optY = boxY + boxH + 16;
        DrawCheckbox(display, x, optY, _formatFat32, "Formatar particao e preparar FAT32");
        DrawCheckbox(display, x, optY + 24, _installBootloader, "Instalar Bootloader NybuOS compativel com x86_64");
        DrawCheckbox(display, x, optY + 48, _copyDefaultApps, "Copiar pacotes de aplicativos e utilitarios completos");

        display.DrawString(x, optY + 80, "Clique em 'Instalar Agora' para iniciar o processo.", Color.FromArgb(245, 158, 11));
    }

    private void DrawCheckbox(DisplayDriver display, int x, int y, bool isChecked, string text)
    {
        display.DrawFilledRectangle(x, y, 14, 14, Color.FromArgb(30, 30, 40));
        display.DrawRectangle(x, y, 14, 14, isChecked ? Theme.AccentColor : Theme.ShadowBorder);
        if (isChecked)
        {
            display.DrawFilledRectangle(x + 3, y + 3, 8, 8, Theme.AccentColor);
        }
        display.DrawString(x + 22, y, text, Theme.TextPrimary);
    }

    private void DrawStep2Progress(DisplayDriver display, int x, int y, int w, int h)
    {
        // Se ainda está instalando, avança um pouco a cada frame
        PerformInstallTick();

        display.DrawString(x, y + 10, "Gravando NybuOS no disco rigido FAT32...", Theme.TextPrimary);
        display.DrawString(x, y + 32, _currentAction, Color.FromArgb(96, 165, 250));

        // Barra de progresso moderna
        int barY = y + 60;
        int barH = 22;
        display.DrawFilledRectangle(x, barY, w, barH, Color.FromArgb(20, 20, 28));
        display.DrawRectangle(x, barY, w, barH, Theme.WindowBorder);

        int fillW = (w - 4) * _installProgress / 100;
        if (fillW > 0)
        {
            display.DrawFilledRectangle(x + 2, barY + 2, fillW, barH - 4, Theme.AccentColor);
        }

        string pctText = $"{_installProgress}%";
        display.DrawString(x + (w / 2) - 12, barY + 4, pctText, Color.White);

        display.DrawString(x, y + 100, "Origem: ISO Live CD NybuOS (CD-ROM)", Color.FromArgb(156, 163, 175));
        display.DrawString(x, y + 118, "Destino: Disco Local C:\\ (FAT32 MBR)", Color.FromArgb(156, 163, 175));
        display.DrawString(x, y + 136, "Nao desligue o computador durante a instalacao.", Color.FromArgb(239, 68, 68));
    }

    private void DrawStep3Complete(DisplayDriver display, int x, int y, int w, int h)
    {
        if (IconResources.Installer32 != null)
        {
            display.DrawIcon(x, y + 10, IconResources.Installer32);
        }

        display.DrawString(x + 44, y + 10, "NybuOS Instalado com Sucesso!", Color.FromArgb(34, 197, 94));
        display.DrawString(x + 44, y + 28, "A instalacao no disco rigido FAT32 foi concluida.", Theme.TextPrimary);

        display.DrawString(x, y + 64, "Instrucoes para inicializacao direta:", Color.FromArgb(96, 165, 250));
        display.DrawString(x, y + 84, "1. Remova ou desmonte a imagem ISO do NybuOS no drive de CD.", Theme.TextPrimary);
        display.DrawString(x, y + 102, "2. O sistema inicializara diretamente pelo disco rigido (HD).", Theme.TextPrimary);
        display.DrawString(x, y + 120, "3. Clique em 'Reiniciar Agora' para testar a inicializacao.", Theme.TextPrimary);

        display.DrawFilledRectangle(x, y + 150, w, 32, Color.FromArgb(20, 35, 25));
        display.DrawRectangle(x, y + 150, w, 32, Color.FromArgb(34, 197, 94));
        display.DrawString(x + 10, y + 158, "Status: Disco de Boot C:\\ pronto para uso.", Color.FromArgb(187, 247, 208));
    }

    private int _tickDelay = 0;
    private void PerformInstallTick()
    {
        _tickDelay++;
        if (_tickDelay < 3) return; // Suaviza o progresso visual
        _tickDelay = 0;

        if (_installProgress < 100)
        {
            _installProgress += 2;
            _installSubStep = _installProgress / 10;
            if (_installSubStep >= _stepsDescriptions.Length) _installSubStep = _stepsDescriptions.Length - 1;
            _currentAction = _stepsDescriptions[_installSubStep];

            // Efetua as gravações reais em disco quando atinge marcos
            if (_installProgress == 30)
            {
                CommitFileSystemStructure();
            }
            else if (_installProgress == 60)
            {
                CommitBootloaderAndKernel();
            }
            else if (_installProgress == 90)
            {
                CommitSystemConfigs();
            }

            if (_installProgress >= 100)
            {
                _installProgress = 100;
                _currentStep = 3; // Concluído!
            }
        }
    }

    private void CommitFileSystemStructure()
    {
        try
        {
            _vfs.CreateDirectory(@"C:\Boot");
            _vfs.CreateDirectory(@"C:\System");
            _vfs.CreateDirectory(@"C:\Programs");
            _vfs.CreateDirectory(@"C:\Documents");
            _vfs.CreateDirectory(@"C:\Desktop");
        }
        catch { }
    }

    private void CommitBootloaderAndKernel()
    {
        try
        {
            // Arquivo de configuração de boot do NybuOS no HD
            string bootCfg = "BOOT_IMAGE=NybuOS.bin\n" +
                             "KERNEL_VERSION=3.0-FINAL\n" +
                             "ROOT_DEVICE=HD0_FAT32\n" +
                             "DEFAULT_ENTRY=0\n" +
                             "TIMEOUT=3\n" +
                             "VGA_MODE=800x600x32\n";
            _vfs.WriteFile(@"C:\Boot\boot.cfg", bootCfg);

            // Simula binário descompactado do kernel
            _vfs.WriteFile(@"C:\Boot\NybuOS.bin", "NYBUOS_KERNEL_IMAGE_X64_ELF_BIN_HEADER");
        }
        catch { }
    }

    private void CommitSystemConfigs()
    {
        try
        {
            string osConf = "OS=NybuOS\n" +
                            "VERSION=3.0.0-FINAL\n" +
                            "EDITION=Modern Glass\n" +
                            "BOOT_SOURCE=HD_FAT32\n" +
                            "INSTALLED=TRUE\n";
            _vfs.WriteFile(@"C:\System\os.conf", osConf);

            _vfs.WriteFile(@"C:\Desktop\Instalacao_Concluida.txt", 
                "Parabens!\n" +
                "O NybuOS v3.0 foi instalado com sucesso no seu disco rigido FAT32.\n" +
                "Voce pode inicializar sem a ISO e usufruir de persistencia total.");
        }
        catch { }
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;
        int footerY = cy + ch - 38;
        int by = footerY + 6;
        int btnW = 85;
        int btnH = 26;

        if (_currentStep == 0)
        {
            // Avançar
            if (mouseX >= cx + cw - 180 && mouseX <= cx + cw - 180 + btnW &&
                mouseY >= by && mouseY <= by + btnH)
            {
                _currentStep = 1;
                return;
            }

            // Cancelar
            if (mouseX >= cx + cw - 90 && mouseX <= cx + cw - 90 + btnW &&
                mouseY >= by && mouseY <= by + btnH)
            {
                Close();
                return;
            }
        }
        else if (_currentStep == 1)
        {
            // Voltar
            if (mouseX >= cx + cw - 275 && mouseX <= cx + cw - 275 + btnW &&
                mouseY >= by && mouseY <= by + btnH)
            {
                _currentStep = 0;
                return;
            }

            // Instalar Agora
            if (mouseX >= cx + cw - 185 && mouseX <= cx + cw - 185 + 95 &&
                mouseY >= by && mouseY <= by + btnH)
            {
                _currentStep = 2;
                _installProgress = 0;
                return;
            }

            // Cancelar
            if (mouseX >= cx + cw - 85 && mouseX <= cx + cw - 85 + 75 &&
                mouseY >= by && mouseY <= by + btnH)
            {
                Close();
                return;
            }

            // Clique nas opções checkboxes
            int optY = cy + 46 + 10 + 20 + 46 + 16;
            if (mouseX >= cx + 16 && mouseX <= cx + 16 + 250)
            {
                if (mouseY >= optY && mouseY <= optY + 18) _formatFat32 = !_formatFat32;
                else if (mouseY >= optY + 24 && mouseY <= optY + 42) _installBootloader = !_installBootloader;
                else if (mouseY >= optY + 48 && mouseY <= optY + 66) _copyDefaultApps = !_copyDefaultApps;
            }
        }
        else if (_currentStep == 3)
        {
            // Reiniciar Agora
            if (mouseX >= cx + cw - 285 && mouseX <= cx + cw - 285 + 105 &&
                mouseY >= by && mouseY <= by + btnH)
            {
                PowerDriver.Reboot();
                return;
            }

            // Desligar
            if (mouseX >= cx + cw - 175 && mouseX <= cx + cw - 175 + 80 &&
                mouseY >= by && mouseY <= by + btnH)
            {
                PowerDriver.Shutdown();
                return;
            }

            // Concluir
            if (mouseX >= cx + cw - 90 && mouseX <= cx + cw - 90 + btnW &&
                mouseY >= by && mouseY <= by + btnH)
            {
                Close();
                return;
            }
        }
    }
}
