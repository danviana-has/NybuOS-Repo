using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.FileSystem;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Media Reproduction: Aplicativo multimídia completo para reprodução de som
/// e visualização de imagens Bitmap (.BMP) e Ícones (.ICO) a partir do sistema de arquivos FAT32.
/// </summary>
public class MediaReproductionApp : Window
{
    private readonly VirtualFileSystem _vfs;

    public enum MediaMode
    {
        AudioPlayer,
        ImageViewer
    }

    private MediaMode _currentMode = MediaMode.AudioPlayer;

    // Estado do Reprodutor de Áudio
    private bool _isPlaying = false;
    private bool _isPaused = false;
    private int _currentTrackIndex = 0;
    private int _elapsedSeconds = 0;
    private int _tickCounter = 0;
    private int _volume = 80; // 0 - 100
    private readonly int[] _spectrumBars = new int[16];
    private readonly Random _rand = new Random(42);

    private readonly List<string> _audioTracks = new List<string>()
    {
        "NybuOS_Startup.wav",
        "Classic_Chord.wav",
        "Ding_Alert.wav",
        "Retro_Chimes.wav",
        "Midi_Synthesizer.mid"
    };

    // Estado do Visualizador de Imagens
    private IconImage _loadedImage = null;
    private string _loadedImageName = "Nenhuma imagem carregada";
    private string _imageInfo = "";
    private List<string> _availableImages = new List<string>();
    private int _currentImageIndex = 0;

    public MediaReproductionApp(int id, int x, int y, VirtualFileSystem vfs)
        : base(id, "Media Reproduction - NybuOS", x, y, 400, 290)
    {
        Icon = IconResources.Media16;
        _vfs = vfs;
        ScanImages();
        LoadCurrentImage();
    }

    private void ScanImages()
    {
        _availableImages.Clear();

        // 1. Imagens do sistema embutidas para demonstração imediata
        _availableImages.Add("Sistema: Meu Computador");
        _availableImages.Add("Sistema: Midia Player");
        _availableImages.Add("Sistema: PaintPad");
        _availableImages.Add("Sistema: Terminal");
        _availableImages.Add("Sistema: Documentos");

        // 2. Busca arquivos .bmp e .ico no disco FAT32
        string[] searchPaths = new string[] { @"C:\Documents", @"C:\Desktop", @"C:\" };
        foreach (var path in searchPaths)
        {
            try
            {
                var files = _vfs.ListFiles(path);
                foreach (var f in files)
                {
                    string lower = f.ToLower();
                    if ((lower.EndsWith(".bmp") || lower.EndsWith(".ico")) && !_availableImages.Contains(f))
                    {
                        _availableImages.Add(f);
                    }
                }
            }
            catch { }
        }

        if (_currentImageIndex >= _availableImages.Count)
        {
            _currentImageIndex = 0;
        }
    }

    private void LoadCurrentImage()
    {
        if (_availableImages.Count == 0) return;

        if (_currentImageIndex < 0 || _currentImageIndex >= _availableImages.Count)
        {
            _currentImageIndex = 0;
        }

        string item = _availableImages[_currentImageIndex];

        // Se for um item do sistema
        if (item.StartsWith("Sistema:"))
        {
            _loadedImageName = item;
            if (item.Contains("Computador"))
            {
                _loadedImage = IconResources.Computer32;
                _imageInfo = "32x32 pixels, Ícone do Sistema";
            }
            else if (item.Contains("Midia"))
            {
                _loadedImage = IconResources.Media32 ?? IconResources.MediaIcon ?? IconResources.Computer32;
                _imageInfo = "32x32 pixels, Ícone do Reprodutor";
            }
            else if (item.Contains("PaintPad"))
            {
                _loadedImage = IconResources.PaintPad32 ?? IconResources.PaintPadIcon ?? IconResources.Computer32;
                _imageInfo = "32x32 pixels, Ícone do PaintPad";
            }
            else if (item.Contains("Terminal"))
            {
                _loadedImage = IconResources.Terminal32 ?? IconResources.TerminalIcon ?? IconResources.Computer32;
                _imageInfo = "32x32 pixels, Ícone do Terminal";
            }
            else
            {
                _loadedImage = IconResources.Doc32 ?? IconResources.Computer32;
                _imageInfo = "32x32 pixels, Ícone de Documento";
            }
            return;
        }

        // Se for arquivo em disco
        _loadedImageName = Path.GetFileName(item);
        try
        {
            byte[] bytes = _vfs.ReadFileBytes(item);
            if (bytes != null && bytes.Length > 0)
            {
                if (item.ToLower().EndsWith(".ico"))
                {
                    _loadedImage = IcoDecoder.Decode(bytes, 32);
                    _imageInfo = $"{_loadedImage.Width}x{_loadedImage.Height} pixels (.ICO FAT32)";
                }
                else if (item.ToLower().EndsWith(".bmp"))
                {
                    _loadedImage = BmpEncoder.Decode(bytes);
                    if (_loadedImage != null)
                    {
                        _imageInfo = $"{_loadedImage.Width}x{_loadedImage.Height} pixels (.BMP FAT32)";
                    }
                }
            }
        }
        catch
        {
            _imageInfo = "Falha ao decodificar imagem.";
        }
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo principal estilo Windows 95
        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.Surface);

        // Barra de Abas / Seleção de Modo
        display.DrawButton(cx + 4, cy + 3, 110, 22, "Reprodutor Som", _currentMode == MediaMode.AudioPlayer);
        display.DrawButton(cx + 116, cy + 3, 110, 22, "Ver Imagens", _currentMode == MediaMode.ImageViewer);

        if (_currentMode == MediaMode.AudioPlayer)
        {
            DrawAudioPlayer(display, cx, cy + 28, cw, ch - 28);
        }
        else
        {
            DrawImageViewer(display, cx, cy + 28, cw, ch - 28);
        }

        // Atualização contínua do áudio e visualizador de áudio durante a reprodução
        if (_isPlaying && !_isPaused)
        {
            _tickCounter++;
            if (_tickCounter % 5 == 0)
            {
                int noteStep = _tickCounter / 5;
                SoundDriver.PlayTrackNote(_currentTrackIndex, noteStep);
            }

            if (_tickCounter % 30 == 0)
            {
                _elapsedSeconds++;
            }

            // Anima barras do equalizador gráfico
            for (int i = 0; i < _spectrumBars.Length; i++)
            {
                _spectrumBars[i] = _rand.Next(4, 38);
            }
        }
        else
        {
            SoundDriver.StopBeep();
        }
    }

    private void DrawAudioPlayer(DisplayDriver display, int x, int y, int w, int h)
    {
        // Display LCD Clássico (Fundo Preto com Texto Verde Fluorescente)
        int lcdX = x + 6;
        int lcdY = y + 4;
        int lcdW = w - 12;
        int lcdH = 75;

        display.DrawFilledRectangle(lcdX, lcdY, lcdW, lcdH, Color.Black);
        display.Draw3DBevel(lcdX, lcdY, lcdW, lcdH, true);

        Color lcdGreen = Color.FromArgb(0, 255, 64);
        string trackName = _audioTracks[_currentTrackIndex];
        string statusText = _isPlaying ? (_isPaused ? "[PAUSADO]" : "[TOCANDO]") : "[PARADO]";
        string timeStr = $"TEMPO: {_elapsedSeconds / 60:D2}:{_elapsedSeconds % 60:D2}";

        display.DrawString(lcdX + 10, lcdY + 8, $"{statusText} {trackName}", lcdGreen);
        display.DrawString(lcdX + 10, lcdY + 24, timeStr, lcdGreen);
        display.DrawString(lcdX + 10, lcdY + 40, $"FORMATO: WAV PCM 22.05 kHz 16-bit | VOL: {_volume}%", lcdGreen);

        // Visualizador Gráfico de Espectro / Osciloscópio no display LCD
        int specX = lcdX + lcdW - 120;
        int specY = lcdY + 15;
        for (int i = 0; i < 16; i++)
        {
            int barH = _isPlaying && !_isPaused ? _spectrumBars[i] : 2;
            int bx = specX + (i * 7);
            int by = specY + 45 - barH;
            display.DrawFilledRectangle(bx, by, 5, barH, lcdGreen);
        }

        // Painel de Controles Estilo CD Player Win95
        int ctrlY = lcdY + lcdH + 12;
        display.DrawButton(x + 10, ctrlY, 60, 24, "Play", _isPlaying && !_isPaused);
        display.DrawButton(x + 74, ctrlY, 60, 24, "Pause", _isPaused);
        display.DrawButton(x + 138, ctrlY, 60, 24, "Stop", !_isPlaying);
        display.DrawButton(x + 202, ctrlY, 55, 24, "|<<", false);
        display.DrawButton(x + 261, ctrlY, 55, 24, ">>|", false);

        // Barra de Volume
        int volY = ctrlY + 36;
        display.DrawString(x + 10, volY + 4, "Volume:", Color.Black);
        display.DrawButton(x + 70, volY, 30, 20, "-", false);
        display.DrawFilledRectangle(x + 105, volY + 3, 150, 14, Color.White);
        display.Draw3DBevel(x + 105, volY + 3, 150, 14, true);

        int filledW = (_volume * 148) / 100;
        if (filledW > 0)
        {
            display.DrawFilledRectangle(x + 106, volY + 4, filledW, 12, Color.FromArgb(0, 0, 128));
        }

        display.DrawButton(x + 260, volY, 30, 20, "+", false);
        display.DrawString(x + 296, volY + 4, $"{_volume}%", Color.Black);

        // Lista de Faixas Disponíveis
        int listY = volY + 28;
        display.DrawFilledRectangle(x + 6, listY, w - 12, h - (listY - y) - 6, Color.White);
        display.Draw3DBevel(x + 6, listY, w - 12, h - (listY - y) - 6, true);

        for (int i = 0; i < _audioTracks.Count; i++)
        {
            int ty = listY + 4 + (i * 15);
            if (ty + 14 > y + h - 10) break;

            bool isSel = (i == _currentTrackIndex);
            if (isSel)
            {
                display.DrawFilledRectangle(x + 8, ty - 1, w - 16, 14, Theme.SelectionBg);
                display.DrawString(x + 10, ty, $"► {i + 1}. {_audioTracks[i]}", Theme.TextSelected);
            }
            else
            {
                display.DrawString(x + 10, ty, $"   {i + 1}. {_audioTracks[i]}", Color.Black);
            }
        }
    }

    private void DrawImageViewer(DisplayDriver display, int x, int y, int w, int h)
    {
        // Barra Superior do Visualizador
        display.DrawButton(x + 6, y + 4, 60, 22, "Anterior", false);
        display.DrawButton(x + 70, y + 4, 60, 22, "Proxima", false);
        display.DrawButton(x + 134, y + 4, 75, 22, "Atualizar", false);

        string countLabel = _availableImages.Count > 0 ? $"[{_currentImageIndex + 1}/{_availableImages.Count}] " : "";
        display.DrawString(x + 215, y + 8, countLabel + _loadedImageName, Color.Black);

        // Área Central de Exibição da Imagem (Sunken Branco)
        int viewX = x + 6;
        int viewY = y + 30;
        int viewW = w - 12;
        int viewH = h - 56;

        display.DrawFilledRectangle(viewX, viewY, viewW, viewH, Color.FromArgb(192, 192, 192));
        display.Draw3DBevel(viewX, viewY, viewW, viewH, true);

        if (_loadedImage != null)
        {
            // Centraliza a imagem na viewport
            int imgX = viewX + (viewW - _loadedImage.Width) / 2;
            int imgY = viewY + (viewH - _loadedImage.Height) / 2;

            if (imgX < viewX) imgX = viewX + 4;
            if (imgY < viewY) imgY = viewY + 4;

            // Fundo xadrez suave estilo transparência
            display.DrawFilledRectangle(imgX - 2, imgY - 2, _loadedImage.Width + 4, _loadedImage.Height + 4, Color.White);
            display.DrawRectangle(imgX - 2, imgY - 2, _loadedImage.Width + 4, _loadedImage.Height + 4, Color.Black);

            display.DrawIcon(imgX, imgY, _loadedImage);
        }
        else
        {
            display.DrawString(viewX + 20, viewY + 30, "Selecione uma imagem BMP ou ICO para visualizar.", Color.DarkGray);
        }

        // Barra de Informações Inferior
        int infoY = viewY + viewH + 4;
        display.DrawFilledRectangle(x + 6, infoY, w - 12, 18, Theme.Surface);
        display.Draw3DBevel(x + 6, infoY, w - 12, 18, true);
        display.DrawString(x + 10, infoY + 3, _imageInfo, Color.Black);
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;

        // Troca de Modo (Abas)
        if (mouseY >= cy + 3 && mouseY <= cy + 25)
        {
            if (mouseX >= cx + 4 && mouseX <= cx + 114)
            {
                _currentMode = MediaMode.AudioPlayer;
                return;
            }
            if (mouseX >= cx + 116 && mouseX <= cx + 226)
            {
                _currentMode = MediaMode.ImageViewer;
                ScanImages();
                LoadCurrentImage();
                return;
            }
        }

        if (_currentMode == MediaMode.AudioPlayer)
        {
            HandleAudioControls(mouseX, mouseY);
        }
        else
        {
            HandleImageControls(mouseX, mouseY);
        }
    }

    private void HandleAudioControls(int mx, int my)
    {
        int cx = ClientX;
        int cy = ClientY;
        int ctrlY = cy + 28 + 4 + 75 + 12;

        // Botões: Play, Pause, Stop, Prev, Next
        if (my >= ctrlY && my <= ctrlY + 24)
        {
            if (mx >= cx + 10 && mx <= cx + 70) // Play
            {
                _isPlaying = true;
                _isPaused = false;
                SoundDriver.Volume = _volume;
                return;
            }
            if (mx >= cx + 74 && mx <= cx + 134) // Pause
            {
                if (_isPlaying)
                {
                    _isPaused = !_isPaused;
                    if (_isPaused) SoundDriver.StopBeep();
                }
                return;
            }
            if (mx >= cx + 138 && mx <= cx + 198) // Stop
            {
                _isPlaying = false;
                _isPaused = false;
                _elapsedSeconds = 0;
                SoundDriver.StopBeep();
                return;
            }
            if (mx >= cx + 202 && mx <= cx + 257) // Prev
            {
                if (_currentTrackIndex > 0) _currentTrackIndex--;
                else _currentTrackIndex = _audioTracks.Count - 1;
                _elapsedSeconds = 0;
                return;
            }
            if (mx >= cx + 261 && mx <= cx + 316) // Next
            {
                _currentTrackIndex = (_currentTrackIndex + 1) % _audioTracks.Count;
                _elapsedSeconds = 0;
                return;
            }
        }

        // Controle de Volume
        int volY = ctrlY + 36;
        if (my >= volY && my <= volY + 20)
        {
            if (mx >= cx + 70 && mx <= cx + 100) // -
            {
                _volume = Math.Max(0, _volume - 10);
                SoundDriver.Volume = _volume;
                return;
            }
            if (mx >= cx + 260 && mx <= cx + 290) // +
            {
                _volume = Math.Min(100, _volume + 10);
                SoundDriver.Volume = _volume;
                return;
            }
            if (mx >= cx + 105 && mx <= cx + 255) // Clique na barra
            {
                int clickW = mx - (cx + 105);
                _volume = Math.Clamp((clickW * 100) / 150, 0, 100);
                SoundDriver.Volume = _volume;
                return;
            }
        }

        // Seleção de Faixa na Lista
        int listY = volY + 28;
        if (my >= listY + 4 && my <= listY + 80 && mx >= cx + 6 && mx <= cx + ClientWidth - 6)
        {
            int clicked = (my - (listY + 4)) / 15;
            if (clicked >= 0 && clicked < _audioTracks.Count)
            {
                _currentTrackIndex = clicked;
                _elapsedSeconds = 0;
                _isPlaying = true;
                _isPaused = false;
            }
        }
    }

    private void HandleImageControls(int mx, int my)
    {
        int cx = ClientX;
        int cy = ClientY + 28;

        if (my >= cy + 4 && my <= cy + 26)
        {
            if (mx >= cx + 6 && mx <= cx + 66) // Anterior
            {
                if (_availableImages.Count > 0)
                {
                    _currentImageIndex = (_currentImageIndex - 1 + _availableImages.Count) % _availableImages.Count;
                    LoadCurrentImage();
                }
                return;
            }
            if (mx >= cx + 70 && mx <= cx + 130) // Próxima
            {
                if (_availableImages.Count > 0)
                {
                    _currentImageIndex = (_currentImageIndex + 1) % _availableImages.Count;
                    LoadCurrentImage();
                }
                return;
            }
            if (mx >= cx + 134 && mx <= cx + 209) // Atualizar
            {
                ScanImages();
                LoadCurrentImage();
                return;
            }
        }
    }
}
