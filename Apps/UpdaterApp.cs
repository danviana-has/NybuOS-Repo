using System;
using System.Collections.Generic;
using System.Drawing;
using Cosmos.Network.Http;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;
using CosmosHttpClient = Cosmos.Network.Http.HttpClient;

namespace NybuOS.Apps;

public enum UpdaterState
{
    Idle,
    Checking,
    Downloading,
    Installing,
    Success,
    Error
}

/// <summary>
/// Assistente de Atualizações Automáticas do NybuOS (NybuOS Update).
/// Utiliza a pilha de rede HTTPS (CosmosHTTP + BouncyCastle) para baixar
/// o executável binário mais recente do kernel (NybuOS.elf) do GitHub e grava
/// diretamente no disco rígido FAT32, reiniciando o sistema em seguida.
/// Contém painel detalhado de diagnóstico de rede e log de erro em tempo real.
/// </summary>
public class UpdaterApp : Window
{
    private const string KernelUpdateUrl = "https://raw.githubusercontent.com/danviana-has/NybuOS-Repo/main/obj/Release/net10.0/linux-x64/cosmos/iso_root/boot/NybuOS.elf";

    private UpdaterState _state = UpdaterState.Idle;
    private int _progressPercent = 0;
    private string _statusText = "O sistema operacional esta pronto para verificar atualizacoes.";
    private string _detailText = "Versao atual: NybuOS RV 3.0 (Build 2026.11) - Canal Oficial";
    private readonly List<string> _errorLogLines = new List<string>();
    private bool _actionPending = false;

    public UpdaterApp(int id, int x, int y)
        : base(id, "NybuOS Update", x, y, 700, 520)
    {
        Icon = IconResources.UpdaterIcon ?? IconResources.Drive16;
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo com estética macOS Light Glass refinada
        display.DrawFrostedGlass(cx, cy, cw, ch, Theme.Surface, alpha: 245, cornerRadius: 8);

        // 1. Cabeçalho moderno com ícone e títulos
        int headerY = cy + 16;
        if (IconResources.UpdaterDock != null)
        {
            display.DrawIcon(cx + 24, headerY, IconResources.UpdaterDock);
        }
        else if (IconResources.UpdaterIcon != null)
        {
            display.DrawIcon(cx + 24, headerY + 14, IconResources.UpdaterIcon);
        }

        display.DrawString(cx + 96, headerY + 6, "NybuOS Update", Theme.TextPrimary);
        display.DrawString(cx + 96, headerY + 28, "Central de Atualizacoes do Sistema Operacional", Theme.TextSecondary);

        // Linha divisória suave
        display.DrawLine(cx + 24, headerY + 64, cx + cw - 24, headerY + 64, Theme.DockSeparator);

        // 2. Área de Conteúdo
        int contentY = headerY + 76;

        switch (_state)
        {
            case UpdaterState.Idle:
                DrawIdleState(display, cx, contentY, cw, ch);
                break;
            case UpdaterState.Checking:
            case UpdaterState.Downloading:
            case UpdaterState.Installing:
                DrawProgressState(display, cx, contentY, cw, ch);
                break;
            case UpdaterState.Success:
                DrawSuccessState(display, cx, contentY, cw, ch);
                break;
            case UpdaterState.Error:
                DrawErrorState(display, cx, contentY, cw, ch);
                break;
        }

        // Se uma ação foi solicitada pelo clique do usuário, processa após renderizar o frame de feedback
        if (_actionPending)
        {
            _actionPending = false;
            PerformUpdateProcess();
        }
    }

    private void DrawIdleState(DisplayDriver display, int cx, int cy, int cw, int ch)
    {
        int cardX = cx + 24;
        int cardY = cy;
        int cardW = cw - 48;
        int cardH = 150;

        display.DrawFrostedGlass(cardX, cardY, cardW, cardH, Color.FromArgb(241, 245, 249), alpha: 200, cornerRadius: 6);
        display.DrawRectangle(cardX, cardY, cardW, cardH, Color.FromArgb(226, 232, 240));

        display.DrawString(cardX + 16, cardY + 16, "Status da Instalacao:", Theme.TextPrimary);
        display.DrawString(cardX + 16, cardY + 38, _detailText, Theme.TextSecondary);
        display.DrawString(cardX + 16, cardY + 60, "Repositorio: github.com/danviana-has/NybuOS-Repo", Theme.TextSecondary);
        display.DrawString(cardX + 16, cardY + 82, "Binario Alvo: /boot/NybuOS.elf (x86_64)", Theme.TextSecondary);
        display.DrawString(cardX + 16, cardY + 104, "Pilha de Rede: CosmosHTTP v3.0.0 com TLS 1.3 / BouncyCastle", Theme.AccentColor);
        display.DrawString(cardX + 16, cardY + 126, "Clique no botao abaixo para negociar DHCP e baixar a versao.", Theme.TextSecondary);

        // Botão "Buscar Atualizações"
        int btnW = 220;
        int btnH = 38;
        int btnX = cx + (cw - btnW) / 2;
        int btnY = cardY + cardH + 28;

        display.DrawModernButton(btnX, btnY, btnW, btnH, "Buscar Atualizacoes", true);
    }

    private void DrawProgressState(DisplayDriver display, int cx, int cy, int cw, int ch)
    {
        display.DrawString(cx + 28, cy + 10, _statusText, Theme.TextPrimary);
        display.DrawString(cx + 28, cy + 34, _detailText, Theme.TextSecondary);

        // Barra de progresso moderna com cantos arredondados
        int barX = cx + 28;
        int barY = cy + 70;
        int barW = cw - 56;
        int barH = 14;

        display.DrawFilledRectangle(barX, barY, barW, barH, Color.FromArgb(226, 232, 240));
        int filled = Math.Min(barW, Math.Max(0, (barW * _progressPercent) / 100));
        if (filled > 0)
        {
            display.DrawFilledRectangle(barX, barY, filled, barH, Theme.AccentColor);
        }

        string pctStr = $"{_progressPercent}%";
        int pctW = pctStr.Length * CosmosNativeFont.Width;
        display.DrawString(barX + barW - pctW, barY + 20, pctStr, Theme.AccentColor);

        display.DrawString(barX, barY + 50, "Aguarde a conclusao do download e gravacao em disco.", Color.FromArgb(100, 116, 139));
    }

    private void DrawSuccessState(DisplayDriver display, int cx, int cy, int cw, int ch)
    {
        int iconCenter = cx + (cw / 2);
        int iconY = cy + 16;

        display.DrawFilledCircle(iconCenter, iconY + 24, 24, Color.FromArgb(34, 197, 94));
        display.DrawString(iconCenter - 4, iconY + 16, "V", Color.White);

        string title = "Atualizacao Concluida!";
        int tw = title.Length * CosmosNativeFont.Width;
        display.DrawString(iconCenter - (tw / 2), iconY + 60, title, Color.FromArgb(22, 163, 74));

        string sub = "O novo kernel NybuOS.elf foi gravado no disco rigido com sucesso.";
        int sw = sub.Length * CosmosNativeFont.Width;
        display.DrawString(iconCenter - (sw / 2), iconY + 84, sub, Theme.TextPrimary);

        string reb = "Reiniciando o sistema automaticamente...";
        int rw = reb.Length * CosmosNativeFont.Width;
        display.DrawString(iconCenter - (rw / 2), iconY + 110, reb, Theme.AccentColor);
    }

    private void DrawErrorState(DisplayDriver display, int cx, int cy, int cw, int ch)
    {
        int boxX = cx + 20;
        int boxY = cy;
        int boxW = cw - 40;
        int boxH = 260;

        // Caixa de Log em Terminal Escuro (Console)
        display.DrawFilledRectangle(boxX, boxY, boxW, boxH, Color.FromArgb(15, 23, 42)); // Slate escuro
        display.DrawRectangle(boxX, boxY, boxW, boxH, Color.FromArgb(239, 68, 68)); // Borda vermelha de erro

        // Título do terminal
        display.DrawFilledRectangle(boxX, boxY, boxW, 24, Color.FromArgb(30, 41, 59));
        display.DrawString(boxX + 10, boxY + 5, "LOG DETALHADO DO ERRO E DIAGNOSTICO DE REDE:", Color.FromArgb(248, 113, 113));

        // Renderiza as linhas de log capturadas
        int textY = boxY + 30;
        int maxLines = 14;
        int startIdx = Math.Max(0, _errorLogLines.Count - maxLines);

        for (int i = startIdx; i < _errorLogLines.Count; i++)
        {
            string line = _errorLogLines[i];
            // Trunca se exceder largura util
            int maxChars = (boxW - 20) / CosmosNativeFont.Width;
            if (line.Length > maxChars)
            {
                line = line.Substring(0, maxChars);
            }

            Color lineCol = line.StartsWith("[ERRO") || line.StartsWith("[FALHA") || line.StartsWith("  [Inner")
                ? Color.FromArgb(248, 113, 113)
                : (line.StartsWith("[OK") ? Color.FromArgb(74, 222, 128) : Color.FromArgb(226, 232, 240));

            display.DrawString(boxX + 10, textY, line, lineCol);
            textY += 16;
        }

        // Botão Tentar Novamente centralizado abaixo do log
        int btnW = 180;
        int btnH = 34;
        int btnX = cx + (cw - btnW) / 2;
        int btnY = boxY + boxH + 16;

        display.DrawModernButton(btnX, btnY, btnW, btnH, "Tentar Novamente", true);
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;
        int contentY = cy + 16 + 76;

        if (_state == UpdaterState.Idle)
        {
            int cardH = 150;
            int btnW = 220;
            int btnH = 38;
            int btnX = cx + (cw - btnW) / 2;
            int btnY = contentY + cardH + 28;

            if (mouseX >= btnX && mouseX <= btnX + btnW && mouseY >= btnY && mouseY <= btnY + btnH)
            {
                _state = UpdaterState.Checking;
                _progressPercent = 10;
                _statusText = "Iniciando verificacao e conexao de rede...";
                _detailText = "Estabelecendo configuracoes de rede...";
                _actionPending = true;
            }
        }
        else if (_state == UpdaterState.Error)
        {
            int boxH = 260;
            int btnW = 180;
            int btnH = 34;
            int btnX = cx + (cw - btnW) / 2;
            int btnY = contentY + boxH + 16;

            if (mouseX >= btnX && mouseX <= btnX + btnW && mouseY >= btnY && mouseY <= btnY + btnH)
            {
                _state = UpdaterState.Checking;
                _progressPercent = 10;
                _statusText = "Reiniciando busca de atualizacoes...";
                _detailText = "Reconectando pilha de rede...";
                _actionPending = true;
            }
        }
    }

    private void Log(string message)
    {
        _errorLogLines.Add(message);
    }

    private void PerformUpdateProcess()
    {
        _errorLogLines.Clear();

        try
        {
            // 1. Diagnóstico do Disco Rígido
            Log("[1/5] Verificando disco rigido de destino...");
            var targetHdd = HardDiskInstaller.FindTargetHardDisk();
            if (targetHdd == null)
            {
                Log("[FALHA] Nenhum disco rigido SATA/AHCI encontrado.");
                _state = UpdaterState.Error;
                return;
            }
            Log($"[OK] Disco: {targetHdd.BlockCount} blocos de {targetHdd.BlockSize}B.");

            // 2. Diagnóstico de Placa de Rede
            Log("[2/5] Verificando placas de rede registradas...");
            int devCount = 0;
            try
            {
                devCount = Cosmos.Kernel.System.Network.NetworkManager.DeviceCount;
            }
            catch (Exception ex)
            {
                Log($"[ERRO NIC] Falha ao consultar placas: {ex.Message}");
            }

            Log($"[INFO] Total de interfaces de rede: {devCount}");
            if (devCount == 0)
            {
                Log("[FALHA] Nenhuma placa de rede detectada pelo Cosmos.");
                Log("[DICA] No VMware configure adaptador 'e1000' ou 'e1000e'.");
                _state = UpdaterState.Error;
                return;
            }

            for (int i = 0; i < devCount; i++)
            {
                try
                {
                    var dev = Cosmos.Kernel.System.Network.NetworkManager.GetAdapter(i);
                    Log($"  NIC {i}: '{dev.Name}' MAC={dev.MacAddress} Link={dev.LinkUp}");
                }
                catch { }
            }

            try
            {
                var primary = Cosmos.Kernel.System.Network.NetworkManager.Primary;
                if (primary.IsValid)
                {
                    Log($"  Primaria: '{primary.Name}' MAC={primary.MacAddress}");
                }
            }
            catch { }

            // 3. Inicialização e Negociação DHCP
            Log("[3/5] Enviando pacote DHCP Discover...");
            _statusText = "Negociando IP via DHCP...";
            _progressPercent = 20;

            try
            {
                using var dhcp = new Cosmos.Kernel.System.Network.DhcpClient();
                int dhcpRes = dhcp.SendDiscoverPacket();
                Log($"[DHCP] Retorno de SendDiscoverPacket(): {dhcpRes}");

                for (int i = 0; i < devCount; i++)
                {
                    try
                    {
                        var adapter = Cosmos.Kernel.System.Network.NetworkManager.GetAdapter(i);
                        if (adapter.IsValid && adapter.IPConfig != null)
                        {
                            Log($"  NIC {i} IP: {adapter.IPConfig.Address} GW: {adapter.IPConfig.DefaultGateway}");
                        }
                    }
                    catch { }
                }

                if (dhcpRes == -1)
                {
                    Log("[REDE] DHCP sem resposta. Ativando IP estatico VMware NAT...");
                    try
                    {
                        var adapter = Cosmos.Kernel.System.Network.NetworkManager.GetAdapter(0);
                        var ip = new Cosmos.Kernel.System.Network.Address4(192, 168, 252, 150);
                        var mask = new Cosmos.Kernel.System.Network.Address4(255, 255, 255, 0);
                        var gw = new Cosmos.Kernel.System.Network.Address4(192, 168, 252, 2);

                        bool ok = false;
                        if (adapter.IsValid)
                        {
                            ok = Cosmos.Kernel.System.Network.IPConfig.Enable(adapter, ip, mask, gw);
                        }
                        else
                        {
                            ok = Cosmos.Kernel.System.Network.IPConfig.Enable(ip, mask, gw);
                        }
                        Log($"[IP FIXO] 192.168.252.150 GW 192.168.252.2: {(ok ? "OK" : "FALHA")}");
                    }
                    catch (Exception ex)
                    {
                        Log($"[IP FIXO ERRO] {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[DHCP AVISO] {ex.GetType().Name}: {ex.Message}");
            }

            // 4. Configuração de Servidores DNS
            Log("[4/5] Registrando servidores DNS...");
            try
            {
                Cosmos.Kernel.System.Network.DnsConfig.Add(new Cosmos.Kernel.System.Network.Address4(192, 168, 252, 2));
                Cosmos.Kernel.System.Network.DnsConfig.Add(new Cosmos.Kernel.System.Network.Address4(8, 8, 8, 8));
                Cosmos.Kernel.System.Network.DnsConfig.Add(new Cosmos.Kernel.System.Network.Address4(1, 1, 1, 1));
                var ns = Cosmos.Kernel.System.Network.DnsConfig.Nameservers;
                Log($"[DNS OK] Total de servidores DNS ativos: {ns?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                Log($"[DNS ERRO] {ex.Message}");
            }

            // 5. Download HTTPS via CosmosHTTP
            Log("[5/5] Conectando via HTTPS ao GitHub...");
            _statusText = "Baixando novo kernel NybuOS.elf (HTTPS)...";
            _detailText = "URL: raw.githubusercontent.com/.../NybuOS.elf";
            _progressPercent = 40;

            using CosmosHttpClient client = new CosmosHttpClient
            {
                Timeout = TimeSpan.FromSeconds(30),
                SslProtocols = System.Security.Authentication.SslProtocols.None,
                SslVerification = SslVerification.NoVerification
            };

            byte[] elfBytes = client.GetByteArray(KernelUpdateUrl);

            if (elfBytes == null || elfBytes.Length < 1000000)
            {
                Log($"[FALHA] Resposta recebida invalida ou incompleta ({elfBytes?.Length ?? 0}B).");
                _state = UpdaterState.Error;
                return;
            }

            Log($"[OK] Download concluido: {elfBytes.Length / 1024} KB.");
            _state = UpdaterState.Installing;
            _statusText = "Gravando kernel no disco rigido FAT32...";
            _detailText = $"Gravando {elfBytes.Length / 1024} KB em /boot/NybuOS.elf";
            _progressPercent = 70;

            // 6. Gravação do Kernel no disco rígido FAT32
            var fat32 = new DirectFat32(targetHdd, HardDiskInstaller.PartitionStartLba);
            uint bootCluster = fat32.FindDirectoryCluster(2, "BOOT");
            if (bootCluster == 0)
            {
                bootCluster = fat32.CreateDirectory(2, "BOOT");
            }

            fat32.UpdateFile(bootCluster, "NYBUOS.ELF", "NybuOS.elf", elfBytes, (pct) =>
            {
                _progressPercent = 70 + (pct * 25 / 100);
            });

            fat32.UpdateFile(2, "NYBUOS.ELF", "NybuOS.elf", elfBytes);

            _progressPercent = 100;
            _state = UpdaterState.Success;
            _statusText = "Atualizacao concluida com sucesso!";
            _detailText = "Reiniciando o sistema em instantes...";

            PowerDriver.Reboot();
        }
        catch (Exception ex)
        {
            _state = UpdaterState.Error;
            Log($"[EXCECAO HTTP] {ex.GetType().Name}: {ex.Message}");

            Exception? cur = ex.InnerException;
            int depth = 1;
            while (cur != null && depth <= 4)
            {
                Log($"  [Inner {depth}] {cur.GetType().Name}: {cur.Message}");
                cur = cur.InnerException;
                depth++;
            }

            var baseEx = ex.GetBaseException();
            if (baseEx != null && baseEx != ex && baseEx != ex.InnerException)
            {
                Log($"  [BaseEx] {baseEx.GetType().Name}: {baseEx.Message}");
            }

            if (ex.ToString().Contains("Name or service not known"))
            {
                Log("[DIAGNOSTICO DNS] Servidor DNS nao acessivel (sem IP no DHCP).");
                Log("  SOLUCAO: No VMware, use adaptador 'e1000' e modo de rede NAT.");
            }
        }
    }
}
