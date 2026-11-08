using System;
using System.IO;
using System.Drawing;
using Cosmos.Kernel.System;
using Cosmos.Kernel.System.Input;
using Cosmos.Kernel.System.Network;
using CosmosHttpClient = Cosmos.Network.Http.HttpClient;
using Cosmos.Network.Http;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Download Central - Aplicativo oficial de downloads HTTP/HTTPS do NybuOS Purple.
/// Permite digitar qualquer URL (imagens, textos, HTML, áudios, binários) e baixar
/// diretamente para o disco rígido com feedback em tempo real.
/// </summary>
public class DownloadCentralApp : Window
{
    private readonly VirtualFileSystem _vfs;

    private string _url = "https://raw.githubusercontent.com/danviana-has/NybuOS-Repo/main/README.md";
    private bool _isInputFocused = true;
    private int _blinkTimer = 0;

    private bool _isDownloading = false;
    private bool _downloadSuccess = false;
    private string _statusText = "Pronto para baixar. Digite a URL e clique em Baixar.";
    private string _errorText = "";
    private string _savedFilePath = "";
    private int _downloadedBytesCount = 0;
    private int _progressPercent = 0;

    // Ações para abrir o arquivo após o download nos apps do sistema
    public Action<string>? OnOpenFileInMedia { get; set; }
    public Action<string>? OnOpenFileInTextEditor { get; set; }

    public DownloadCentralApp(int id, int x, int y, VirtualFileSystem vfs)
        : base(id, "Download Central - NybuOS Purple", x, y, 640, 390)
    {
        _vfs = vfs;
        Icon = IconResources.DownloadCentralIcon ?? IconResources.Doc16;
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // 1. Superfície translúcida refinada
        display.DrawFrostedGlass(cx, cy, cw, ch, Theme.Surface, alpha: 225, cornerRadius: 8);

        // 2. Cabeçalho com Ícone e Título
        int iconX = cx + 22;
        int iconY = cy + 18;
        if (IconResources.DownloadCentralDock != null)
        {
            display.DrawIcon(iconX, iconY, IconResources.DownloadCentralDock);
        }
        else if (IconResources.DownloadCentralIcon != null)
        {
            display.DrawIcon(iconX, iconY, IconResources.DownloadCentralIcon);
        }

        display.DrawString(cx + 94, cy + 22, "Download Central", Theme.TextPrimary);
        display.DrawString(cx + 94, cy + 44, "Central de Downloads de Arquivos HTTP / HTTPS", Theme.AccentColor);

        // Divisória sutil
        display.DrawLine(cx + 20, cy + 86, cx + cw - 20, cy + 86, Theme.DockSeparator);

        // 3. Campo de Entrada da URL
        display.DrawString(cx + 24, cy + 98, "URL do Arquivo para Download:", Theme.TextSecondary);

        int inputX = cx + 22;
        int inputY = cy + 118;
        int inputW = cw - 44;
        int inputH = 34;

        Color inputBorder = _isInputFocused ? Theme.AccentColor : Theme.ShadowBorder;
        display.DrawFilledRoundedRectangle(inputX, inputY, inputW, inputH, 6, Theme.InputBg, inputBorder);

        // Texto da URL ou placeholder
        _blinkTimer++;
        bool showCursor = _isInputFocused && ((_blinkTimer / 15) % 2 == 0);

        if (string.IsNullOrEmpty(_url))
        {
            display.DrawString(inputX + 10, inputY + 9, "https://exemplo.com/imagem.bmp ou arquivo.txt", Theme.TextMuted);
            if (showCursor)
            {
                display.DrawLine(inputX + 10, inputY + 8, inputX + 10, inputY + inputH - 8, Theme.TextPrimary);
            }
        }
        else
        {
            // Trunca visualmente com scroll para a direita se exceder o campo
            int maxChars = (inputW - 24) / CosmosNativeFont.Width;
            string visibleUrl = _url;
            if (visibleUrl.Length > maxChars)
            {
                visibleUrl = visibleUrl.Substring(visibleUrl.Length - maxChars);
            }

            int textEndX = inputX + 10 + (visibleUrl.Length * CosmosNativeFont.Width);
            display.DrawString(inputX + 10, inputY + 9, visibleUrl, Theme.TextPrimary);

            if (showCursor)
            {
                display.DrawLine(textEndX + 1, inputY + 8, textEndX + 1, inputY + inputH - 8, Theme.AccentColor);
            }
        }

        // 4. Botões de Ação
        int btnW = 110;
        int btnH = 32;
        int btnY = cy + 162;
        int downloadBtnX = cx + cw - 22 - btnW;
        int clearBtnX = downloadBtnX - 100;

        // Botão Limpar
        display.DrawFilledRoundedRectangle(clearBtnX, btnY, 90, btnH, 6, Theme.ButtonBg, Theme.ButtonBorder);
        display.DrawString(clearBtnX + 22, btnY + 8, "Limpar", Theme.TextSecondary);

        // Botão Baixar (destaque com cor de acento do NybuOS Purple)
        Color dlBtnBg = _isDownloading ? Theme.ButtonBg : Theme.AccentColor;
        Color dlBtnText = _isDownloading ? Theme.TextDisabled : Color.White;
        display.DrawFilledRoundedRectangle(downloadBtnX, btnY, btnW, btnH, 6, dlBtnBg, Theme.ShadowBorder);
        string dlBtnLabel = _isDownloading ? "Baixando..." : "Baixar";
        int dlLabelX = downloadBtnX + (btnW - (dlBtnLabel.Length * CosmosNativeFont.Width)) / 2;
        display.DrawString(dlLabelX, btnY + 8, dlBtnLabel, dlBtnText);

        // Atalhos rápidos
        display.DrawString(cx + 24, btnY + 8, "Exemplos:", Theme.TextMuted);
        int ex1X = cx + 96;
        display.DrawString(ex1X, btnY + 8, "[README.md]", Theme.AccentColor);
        int ex2X = ex1X + 100;
        display.DrawString(ex2X, btnY + 8, "[NybuOS.elf]", Theme.AccentColor);

        // 5. Painel de Status / Progresso
        int cardX = cx + 22;
        int cardY = cy + 206;
        int cardW = cw - 44;
        int cardH = ch - 220;

        Color cardBg = _errorText.Length > 0 ? Color.FromArgb(254, 242, 242) : Theme.SurfaceLight;
        Color cardBorder = _errorText.Length > 0 ? Color.FromArgb(239, 68, 68) : Theme.DockSeparator;

        display.DrawFilledRoundedRectangle(cardX, cardY, cardW, cardH, 8, cardBg, cardBorder);

        // Exibição de Status
        if (_isDownloading)
        {
            display.DrawString(cardX + 16, cardY + 16, _statusText, Theme.AccentColor);

            // Barra de Progresso
            int pbX = cardX + 16;
            int pbY = cardY + 44;
            int pbW = cardW - 32;
            int pbH = 10;
            display.DrawFilledRoundedRectangle(pbX, pbY, pbW, pbH, 5, Color.FromArgb(226, 232, 240), Theme.ShadowBorder);
            int fillW = Math.Max(12, pbW * _progressPercent / 100);
            display.DrawFilledRoundedRectangle(pbX, pbY, fillW, pbH, 5, Theme.AccentColor, Theme.AccentColor);
        }
        else if (_downloadSuccess)
        {
            display.DrawString(cardX + 16, cardY + 14, "! Download Concluido com Sucesso !", Color.FromArgb(22, 163, 74));
            display.DrawString(cardX + 16, cardY + 36, $"Arquivo Salvo: {_savedFilePath}", Theme.TextPrimary);
            display.DrawString(cardX + 16, cardY + 56, $"Tamanho Baixado: {_downloadedBytesCount / 1024} KB ({_downloadedBytesCount} bytes)", Theme.TextSecondary);

            // Botão para abrir o arquivo
            string ext = Path.GetExtension(_savedFilePath).ToLower();
            bool isImg = ext == ".bmp" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".ico";
            bool isTxt = ext == ".txt" || ext == ".md" || ext == ".html" || ext == ".css" || ext == ".json" || ext == ".log";

            if (isImg)
            {
                int openBtnW = 210;
                int openBtnX = cardX + 16;
                int openBtnY = cardY + 84;
                display.DrawFilledRoundedRectangle(openBtnX, openBtnY, openBtnW, 30, 6, Theme.ButtonBg, Theme.AccentColor);
                display.DrawString(openBtnX + 12, openBtnY + 7, "Abrir no MediaReproduction", Theme.AccentColor);
            }
            else if (isTxt)
            {
                int openBtnW = 190;
                int openBtnX = cardX + 16;
                int openBtnY = cardY + 84;
                display.DrawFilledRoundedRectangle(openBtnX, openBtnY, openBtnW, 30, 6, Theme.ButtonBg, Theme.AccentColor);
                display.DrawString(openBtnX + 12, openBtnY + 7, "Abrir no Editor de Texto", Theme.AccentColor);
            }
        }
        else if (!string.IsNullOrEmpty(_errorText))
        {
            display.DrawString(cardX + 16, cardY + 14, "! Falha no Download !", Color.FromArgb(220, 38, 38));
            display.DrawString(cardX + 16, cardY + 36, _errorText, Theme.TextPrimary);
            display.DrawString(cardX + 16, cardY + 58, "Verifique sua conexao de rede e a URL digitada.", Theme.TextSecondary);
        }
        else
        {
            display.DrawString(cardX + 16, cardY + 18, _statusText, Theme.TextSecondary);
            display.DrawString(cardX + 16, cardY + 42, "Suporta: Imagens (.BMP, .PNG, .JPG), Textos (.TXT, .MD, .HTML), Áudios e Binários.", Theme.TextMuted);
            display.DrawString(cardX + 16, cardY + 64, "Os arquivos sao salvos automaticamente na pasta C:\\Downloads\\.", Theme.TextMuted);
        }
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        int inputX = cx + 22;
        int inputY = cy + 118;
        int inputW = cw - 44;
        int inputH = 34;

        // Foco no campo de texto
        if (mouseX >= inputX && mouseX <= inputX + inputW && mouseY >= inputY && mouseY <= inputY + inputH)
        {
            _isInputFocused = true;
            return;
        }

        int btnW = 110;
        int btnH = 32;
        int btnY = cy + 162;
        int downloadBtnX = cx + cw - 22 - btnW;
        int clearBtnX = downloadBtnX - 100;

        // Botão Limpar
        if (mouseX >= clearBtnX && mouseX <= clearBtnX + 90 && mouseY >= btnY && mouseY <= btnY + btnH)
        {
            _url = "";
            _isInputFocused = true;
            _downloadSuccess = false;
            _errorText = "";
            _statusText = "URL limpa. Digite a nova URL desejada.";
            return;
        }

        // Botão Baixar
        if (mouseX >= downloadBtnX && mouseX <= downloadBtnX + btnW && mouseY >= btnY && mouseY <= btnY + btnH)
        {
            if (!_isDownloading && !string.IsNullOrWhiteSpace(_url))
            {
                StartDownload();
            }
            return;
        }

        // Exemplo 1: README.md
        int ex1X = cx + 96;
        if (mouseX >= ex1X && mouseX <= ex1X + 90 && mouseY >= btnY && mouseY <= btnY + btnH)
        {
            _url = "https://raw.githubusercontent.com/danviana-has/NybuOS-Repo/main/README.md";
            _isInputFocused = true;
            return;
        }

        // Exemplo 2: NybuOS.elf
        int ex2X = ex1X + 100;
        if (mouseX >= ex2X && mouseX <= ex2X + 100 && mouseY >= btnY && mouseY <= btnY + btnH)
        {
            _url = "https://raw.githubusercontent.com/danviana-has/NybuOS-Repo/main/obj/Release/net10.0/linux-x64/cosmos/iso_root/boot/NybuOS.elf";
            _isInputFocused = true;
            return;
        }

        // Clique no botão "Abrir no MediaReproduction" ou "Abrir no Editor"
        if (_downloadSuccess && !string.IsNullOrEmpty(_savedFilePath))
        {
            int cardX = cx + 22;
            int cardY = cy + 206;
            int openBtnX = cardX + 16;
            int openBtnY = cardY + 84;

            string ext = Path.GetExtension(_savedFilePath).ToLower();
            bool isImg = ext == ".bmp" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".ico";
            bool isTxt = ext == ".txt" || ext == ".md" || ext == ".html" || ext == ".css" || ext == ".json" || ext == ".log";

            if (isImg && mouseX >= openBtnX && mouseX <= openBtnX + 210 && mouseY >= openBtnY && mouseY <= openBtnY + 30)
            {
                OnOpenFileInMedia?.Invoke(_savedFilePath);
                return;
            }
            if (isTxt && mouseX >= openBtnX && mouseX <= openBtnX + 190 && mouseY >= openBtnY && mouseY <= openBtnY + 30)
            {
                OnOpenFileInTextEditor?.Invoke(_savedFilePath);
                return;
            }
        }

        _isInputFocused = false;
    }

    public override void OnKeyDown(KeyEvent e)
    {
        if (!_isInputFocused || _isDownloading) return;

        if (e.Key == Key.Enter)
        {
            if (!string.IsNullOrWhiteSpace(_url))
            {
                StartDownload();
            }
        }
        else if (e.Key == Key.Backspace)
        {
            if (_url.Length > 0)
            {
                _url = _url.Substring(0, _url.Length - 1);
            }
        }
        else if (e.Key == Key.Escape)
        {
            _isInputFocused = false;
        }
        else if (e.KeyChar >= 32 && e.KeyChar <= 126)
        {
            _url += e.KeyChar;
        }
    }

    /// <summary>
    /// Inicia o processo de download HTTP/HTTPS e salva o arquivo no sistema de arquivos.
    /// </summary>
    public void StartDownload()
    {
        if (_isDownloading) return;

        _isDownloading = true;
        _downloadSuccess = false;
        _errorText = "";
        _progressPercent = 10;
        _statusText = "Preparando conexão de rede...";

        try
        {
            // 1. Inicializa a pilha de rede e DNS se necessário
            EnsureNetworkConfigured();

            _progressPercent = 40;
            _statusText = "Conectando ao servidor e baixando arquivo...";

            // 2. Executa a requisição GET via CosmosHttpClient
            using CosmosHttpClient client = new CosmosHttpClient
            {
                Timeout = TimeSpan.FromSeconds(35),
                SslProtocols = System.Security.Authentication.SslProtocols.None,
                SslVerification = SslVerification.NoVerification
            };

            byte[] data = client.GetByteArray(_url.Trim());

            if (data == null || data.Length == 0)
            {
                throw new Exception("Nenhum dado retornado pelo servidor.");
            }

            _downloadedBytesCount = data.Length;
            _progressPercent = 80;
            _statusText = "Gravando arquivo no disco rígido...";

            // 3. Extrai nome do arquivo a partir da URL
            string fileName = ExtractFileNameFromUrl(_url);

            // 4. Cria o diretório C:\Downloads se não existir
            string downloadsDir = @"C:\Downloads\";
            try
            {
                if (!_vfs.DirectoryExists(downloadsDir))
                {
                    _vfs.CreateDirectory(downloadsDir);
                }
            }
            catch { }

            string destPath = downloadsDir + fileName;
            bool ok = _vfs.WriteFile(destPath, data);

            if (!ok)
            {
                // Fallback para gravação na raiz caso a pasta Downloads falhe
                destPath = @"C:\" + fileName;
                _vfs.WriteFile(destPath, data);
            }

            _savedFilePath = destPath;
            _downloadSuccess = true;
            _progressPercent = 100;
            _statusText = $"Download concluído! Salvo em: {destPath}";
        }
        catch (Exception ex)
        {
            _errorText = $"{ex.GetType().Name}: {ex.Message}";
            _downloadSuccess = false;
        }
        finally
        {
            _isDownloading = false;
        }
    }

    private void EnsureNetworkConfigured()
    {
        try
        {
            int devCount = NetworkManager.DeviceCount;
            if (devCount == 0) return;

            // Adiciona DNS comuns e gateway se ausentes
            DnsConfig.Add(new Address4(192, 168, 252, 2));
            DnsConfig.Add(new Address4(8, 8, 8, 8));
            DnsConfig.Add(new Address4(1, 1, 1, 1));

            var adapter = NetworkManager.GetAdapter(0);
            if (adapter.IsValid && adapter.IPConfig == null)
            {
                // Configuração estática de contingência
                var ip = new Address4(192, 168, 252, 150);
                var mask = new Address4(255, 255, 255, 0);
                var gw = new Address4(192, 168, 252, 2);
                IPConfig.Enable(adapter, ip, mask, gw);
            }
        }
        catch { }
    }

    private string ExtractFileNameFromUrl(string url)
    {
        try
        {
            string clean = url.Trim();
            if (clean.Contains('?'))
            {
                clean = clean.Substring(0, clean.IndexOf('?'));
            }
            if (clean.Contains('#'))
            {
                clean = clean.Substring(0, clean.IndexOf('#'));
            }

            int lastSlash = clean.LastIndexOf('/');
            if (lastSlash >= 0 && lastSlash < clean.Length - 1)
            {
                string seg = clean.Substring(lastSlash + 1);
                if (!string.IsNullOrWhiteSpace(seg) && seg.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
                {
                    return seg;
                }
            }
        }
        catch { }

        return "arquivo_download.bin";
    }
}
