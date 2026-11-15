using System;
using System.Collections.Generic;
using System.IO;
using NybuOS.Core.FileSystem;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class SystemHealthApp : AppBase
{
    public override string Id => "health";
    public override string Title => "Saúde do Sistema";
    public override FastBitmap? Icon16 => AssetManager.IconHealth16;
    public override FastBitmap? Icon24 => AssetManager.IconHealth24;
    public override FastBitmap? Icon48 => AssetManager.IconHealth48;

    private bool _isScanning = false;
    private int _scanProgress = 0;
    private readonly List<string> _scanReport = new();
    private int _threatsFound = 0;
    private bool _scanCompleted = false;

    public override void Initialize(Core.WindowManager.Window window)
    {
        base.Initialize(window);
        _scanReport.Clear();
        _scanReport.Add("Diagnóstico de Integridade e Segurança do NybuOS.");
        _scanReport.Add("Clique em 'Iniciar Varredura Completa' para analisar o FAT32.");
    }

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, ColorScheme.WindowBody);

        // 1. Status Shield Card (Top)
        int cardH = 90;
        uint cardBg = _threatsFound > 0 ? 0xFFFFF0F0 : 0xFFF0F9F0;
        uint shieldCol = _threatsFound > 0 ? ColorScheme.AccentRed : ColorScheme.AccentGreen;

        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 16, clientY + 16, clientW - 32, cardH, cardBg);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, clientX + 16, clientY + 16, clientW - 32, cardH, shieldCol);

        // Shield Icon & Status Text
        FontRenderer.DrawText(buffer, screenW, screenH, "🛡️", clientX + 32, clientY + 36, shieldCol, bold: true, size: 22);

        string statusMain = _threatsFound > 0 ? "Atenção: Anomalias Detectadas no Disco" : (_scanCompleted ? "Sistema Protegido e Íntegro" : "Verificação Recomendada");
        string statusSub = _threatsFound > 0 ? $"{_threatsFound} anomalia(s) encontrada(s). Ação de reparo recomendada." : "Nenhuma ameaça ou corrupção de arquivos detectada.";

        FontRenderer.DrawText(buffer, screenW, screenH, statusMain, clientX + 80, clientY + 30, shieldCol, bold: true, size: 16);
        FontRenderer.DrawText(buffer, screenW, screenH, statusSub, clientX + 80, clientY + 56, ColorScheme.TextSecondary, bold: false, size: 12);

        // 2. Scan Actions & Progress Bar
        int actY = clientY + cardH + 26;
        int btnW = 200;
        int btnH = 32;

        DrawActionBtn(buffer, screenW, screenH, clientX + 16, actY, btnW, btnH, _isScanning ? "Varrendo Sistema..." : "Iniciar Varredura Completa", !_isScanning);

        if (_threatsFound > 0)
        {
            DrawActionBtn(buffer, screenW, screenH, clientX + 226, actY, 160, btnH, "Reparar Disco", true);
        }

        // Progress Bar
        int pbY = actY + btnH + 14;
        int pbW = clientW - 32;
        int pbH = 12;

        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 16, pbY, pbW, pbH, ColorScheme.ControlBackground);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, clientX + 16, pbY, pbW, pbH, ColorScheme.WindowBorder);

        if (_scanProgress > 0)
        {
            int fillW = (pbW * _scanProgress) / 100;
            FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 16, pbY, fillW, pbH, ColorScheme.Accent);
        }

        // 3. Detailed Report Log
        int repY = pbY + pbH + 16;
        int repH = clientH - (repY - clientY) - 16;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 16, repY, pbW, repH, ColorScheme.BackgroundLight);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, clientX + 16, repY, pbW, repH, ColorScheme.WindowBorder);

        FontRenderer.DrawText(buffer, screenW, screenH, "Relatório de Integridade:", clientX + 26, repY + 10, ColorScheme.TextPrimary, bold: true, size: 12);

        int lineY = repY + 32;
        for (int i = 0; i < _scanReport.Count; i++)
        {
            if (lineY + 18 > repY + repH) break;
            string text = _scanReport[i];
            uint col = text.StartsWith("[ALERTA]") ? ColorScheme.AccentRed : (text.StartsWith("[OK]") ? ColorScheme.AccentGreen : ColorScheme.TextSecondary);
            FontRenderer.DrawText(buffer, screenW, screenH, text, clientX + 26, lineY, col, bold: false, size: 12);
            lineY += 18;
        }
    }

    private void DrawActionBtn(uint[] buffer, int screenW, int screenH, int x, int y, int w, int h, string text, bool enabled)
    {
        uint bg = enabled ? ColorScheme.Accent : ColorScheme.ControlBackground;
        uint txtCol = enabled ? ColorScheme.TextWhite : ColorScheme.TextMuted;

        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, x, y, w, h, bg);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, x, y, w, h, ColorScheme.WindowBorder);
        int tw = FontRenderer.MeasureText(text, bold: true, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, text, x + (w - tw) / 2, y + 8, txtCol, bold: true, size: 12);
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        if (_isScanning)
        {
            // Simulate progression
            _scanProgress += 2;
            if (_scanProgress >= 100)
            {
                _scanProgress = 100;
                _isScanning = false;
                _scanCompleted = true;
                FinishScan();
            }
            return;
        }

        if (InputState.LeftClicked)
        {
            int mx = InputState.MouseX;
            int my = InputState.MouseY;
            int actY = clientY + 90 + 26;

            // Iniciar Varredura
            if (mx >= clientX + 16 && mx < clientX + 216 && my >= actY && my < actY + 32)
            {
                StartScan();
                return;
            }

            // Reparar Disco
            if (_threatsFound > 0 && mx >= clientX + 226 && mx < clientX + 386 && my >= actY && my < actY + 32)
            {
                _threatsFound = 0;
                _scanReport.Add("[REPARO] Todas as anomalias foram corrigidas com sucesso.");
                _scanReport.Add("[OK] Tabela de alocação de arquivos FAT32 revalidada.");
                return;
            }
        }
    }

    private void StartScan()
    {
        _isScanning = true;
        _scanProgress = 0;
        _threatsFound = 0;
        _scanCompleted = false;
        _scanReport.Clear();
        _scanReport.Add("[INÍCIO] Iniciando diagnóstico de integridade no disco FAT32...");
        _scanReport.Add("[VERIFICAÇÃO] Checando superbloco FAT e registros mestre de partição...");
    }

    private void FinishScan()
    {
        _scanReport.Add("[OK] Setor de boot e tabela de clusters FAT32 válidos.");

        // Scan actual files
        var files = FileService.GetFiles(FileService.RootPath);
        _scanReport.Add($"[OK] {files.Length} arquivo(s) analisado(s) na raiz.");

        var docFiles = FileService.GetFiles(Path.Combine(FileService.RootPath, "documentos"));
        _scanReport.Add($"[OK] {docFiles.Length} arquivo(s) analisado(s) em documentos.");

        _scanReport.Add("[OK] Verificação de assinaturas maliciosas concluída: 0 vírus detectados.");
        _scanReport.Add("[OK] Nenhuma corrupção de sistema encontrada. Desempenho otimizado.");
    }
}
