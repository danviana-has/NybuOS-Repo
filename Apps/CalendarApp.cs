using System;
using System.Collections.Generic;
using System.IO;
using NybuOS.Core.FileSystem;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class CalendarApp : AppBase
{
    public override string Id => "calendar";
    public override string Title => "Calendário";
    public override FastBitmap? Icon16 => AssetManager.IconCalendar16;
    public override FastBitmap? Icon24 => AssetManager.IconCalendar24;
    public override FastBitmap? Icon48 => AssetManager.IconCalendar48;

    private int _year = 2026;
    private int _month = 11;
    private int _selectedDay = 14;

    private readonly Dictionary<string, List<string>> _events = new();
    private string _newEventText = "";
    private bool _editingText = false;
    private readonly string _eventsFilePath = Path.Combine(FileService.RootPath, "sistema", "eventos.txt");

    private static readonly string[] MonthNames = {
        "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
    };

    private static readonly string[] DayNames = { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" };

    public override void Initialize(Core.WindowManager.Window window)
    {
        base.Initialize(window);
        var now = DateTime.Now;
        _year = now.Year;
        _month = now.Month;
        _selectedDay = now.Day;

        LoadEvents();
    }

    private void LoadEvents()
    {
        _events.Clear();
        if (FileService.FileExists(_eventsFilePath))
        {
            string content = FileService.ReadAllText(_eventsFilePath);
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                var parts = line.Split('|', 2);
                if (parts.Length == 2)
                {
                    string dateKey = parts[0].Trim();
                    string desc = parts[1].Trim();
                    if (!_events.ContainsKey(dateKey)) _events[dateKey] = new List<string>();
                    _events[dateKey].Add(desc);
                }
            }
        }
        else
        {
            // Seed sample events
            string key = $"{_year}-{_month:D2}-{_selectedDay:D2}";
            _events[key] = new List<string> { "Lançamento Oficial NybuOS V Lagoons" };
            SaveEvents();
        }
    }

    private void SaveEvents()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in _events)
        {
            for (int i = 0; i < kvp.Value.Count; i++)
            {
                sb.AppendLine($"{kvp.Key}|{kvp.Value[i]}");
            }
        }
        FileService.WriteAllText(_eventsFilePath, sb.ToString());
    }

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, ColorScheme.WindowBody);

        int leftW = Math.Max(260, clientW - 220);

        // 1. Month Header (< Month Year >)
        int headerH = 44;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, leftW, headerH, ColorScheme.BackgroundLight);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, clientY + headerH, leftW, ColorScheme.WindowBorder);

        // Navigation Buttons
        DrawNavBtn(buffer, screenW, screenH, clientX + 10, clientY + 8, 28, 28, "◀");
        DrawNavBtn(buffer, screenW, screenH, clientX + leftW - 38, clientY + 8, 28, 28, "▶");

        string monthTitle = $"{MonthNames[_month - 1]} {_year}";
        int titleW = FontRenderer.MeasureText(monthTitle, bold: true, size: 16);
        FontRenderer.DrawText(buffer, screenW, screenH, monthTitle, clientX + (leftW - titleW) / 2, clientY + 12, ColorScheme.TextPrimary, bold: true, size: 16);

        // 2. Day of Week Column Headers
        int dowY = clientY + headerH + 6;
        int colW = leftW / 7;
        for (int i = 0; i < 7; i++)
        {
            string dName = DayNames[i];
            int dw = FontRenderer.MeasureText(dName, bold: true, size: 12);
            FontRenderer.DrawText(buffer, screenW, screenH, dName, clientX + (i * colW) + (colW - dw) / 2, dowY, ColorScheme.TextSecondary, bold: true, size: 12);
        }

        // 3. Month Days Grid
        int gridStartY = dowY + 22;
        int daysInMonth = DateTime.DaysInMonth(_year, _month);
        int firstDow = (int)new DateTime(_year, _month, 1).DayOfWeek; // 0 = Sunday

        int cellH = (clientH - (gridStartY - clientY) - 10) / 6;

        int dayNum = 1;
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                if (row == 0 && col < firstDow) continue;
                if (dayNum > daysInMonth) break;

                int cx = clientX + (col * colW);
                int cy = gridStartY + (row * cellH);

                bool isSelected = (dayNum == _selectedDay);
                string dateKey = $"{_year}-{_month:D2}-{dayNum:D2}";
                bool hasEvents = _events.ContainsKey(dateKey) && _events[dateKey].Count > 0;

                if (isSelected)
                {
                    FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, cx + 2, cy + 2, colW - 4, cellH - 4, ColorScheme.Accent);
                    FontRenderer.DrawText(buffer, screenW, screenH, dayNum.ToString(), cx + (colW / 2) - 6, cy + (cellH / 2) - 8, ColorScheme.TextWhite, bold: true, size: 14);
                }
                else
                {
                    FastRenderer.DrawRectangle(buffer, screenW, screenH, cx + 2, cy + 2, colW - 4, cellH - 4, ColorScheme.ControlBackground);
                    FontRenderer.DrawText(buffer, screenW, screenH, dayNum.ToString(), cx + (colW / 2) - 6, cy + (cellH / 2) - 8, ColorScheme.TextPrimary, bold: false, size: 14);
                }

                // Event indicator dot
                if (hasEvents)
                {
                    uint dotCol = isSelected ? ColorScheme.TextWhite : ColorScheme.AccentOrange;
                    FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, cx + (colW / 2) - 2, cy + cellH - 8, 4, 4, dotCol);
                }

                dayNum++;
            }
        }

        // 4. Right Sidebar: Events Panel
        int sideX = clientX + leftW;
        int sideW = clientW - leftW;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, sideX, clientY, sideW, clientH, ColorScheme.BackgroundLight);
        FastRenderer.DrawVerticalLine(buffer, screenW, screenH, sideX, clientY, clientH, ColorScheme.WindowBorder);

        // Sidebar Header
        string selDateStr = $"{_selectedDay} de {MonthNames[_month - 1]}";
        FontRenderer.DrawText(buffer, screenW, screenH, selDateStr, sideX + 12, clientY + 14, ColorScheme.Accent, bold: true, size: 14);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, sideX + 10, clientY + 36, sideW - 20, ColorScheme.WindowBorder);

        // Event List
        string curKey = $"{_year}-{_month:D2}-{_selectedDay:D2}";
        int evY = clientY + 44;
        if (_events.TryGetValue(curKey, out var dayEvents) && dayEvents.Count > 0)
        {
            for (int i = 0; i < dayEvents.Count; i++)
            {
                if (evY > clientY + clientH - 80) break;
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, sideX + 10, evY, sideW - 20, 26, ColorScheme.WindowBody);
                FastRenderer.DrawRectangle(buffer, screenW, screenH, sideX + 10, evY, sideW - 20, 26, ColorScheme.WindowBorder);
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, sideX + 10, evY, 4, 26, ColorScheme.Accent);

                string desc = dayEvents[i];
                if (desc.Length > 20) desc = desc.Substring(0, 18) + "...";
                FontRenderer.DrawText(buffer, screenW, screenH, desc, sideX + 18, evY + 6, ColorScheme.TextPrimary, bold: false, size: 12);
                evY += 30;
            }
        }
        else
        {
            FontRenderer.DrawText(buffer, screenW, screenH, "Nenhum evento registrado.", sideX + 12, evY + 6, ColorScheme.TextMuted, bold: false, size: 12);
        }

        // Bottom Input for New Event
        int inputY = clientY + clientH - 70;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, sideX + 10, inputY, sideW - 20, 28, ColorScheme.WindowBody);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, sideX + 10, inputY, sideW - 20, 28, _editingText ? ColorScheme.Accent : ColorScheme.WindowBorder);
        string showTxt = string.IsNullOrEmpty(_newEventText) ? (_editingText ? "" : "Novo lembrete...") : _newEventText;
        uint txtCol = string.IsNullOrEmpty(_newEventText) ? ColorScheme.TextMuted : ColorScheme.TextPrimary;
        FontRenderer.DrawText(buffer, screenW, screenH, showTxt, sideX + 16, inputY + 6, txtCol, bold: false, size: 12);

        // Add Button
        int addBtnY = inputY + 34;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, sideX + 10, addBtnY, sideW - 20, 26, ColorScheme.Accent);
        int addW = FontRenderer.MeasureText("+ Adicionar Lembrete", bold: true, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, "+ Adicionar Lembrete", sideX + 10 + (sideW - 20 - addW) / 2, addBtnY + 6, ColorScheme.TextWhite, bold: true, size: 12);
    }

    private void DrawNavBtn(uint[] buffer, int screenW, int screenH, int x, int y, int w, int h, string text)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, x, y, w, h, ColorScheme.ControlBackground);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, x, y, w, h, ColorScheme.WindowBorder);
        FontRenderer.DrawText(buffer, screenW, screenH, text, x + 8, y + 6, ColorScheme.TextPrimary, bold: true, size: 12);
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        int leftW = Math.Max(260, clientW - 220);
        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        if (InputState.LeftClicked)
        {
            // Previous Month Button
            if (mx >= clientX + 10 && mx < clientX + 38 && my >= clientY + 8 && my < clientY + 36)
            {
                _month--;
                if (_month < 1) { _month = 12; _year--; }
                _selectedDay = Math.Min(_selectedDay, DateTime.DaysInMonth(_year, _month));
                return;
            }

            // Next Month Button
            if (mx >= clientX + leftW - 38 && mx < clientX + leftW - 10 && my >= clientY + 8 && my < clientY + 36)
            {
                _month++;
                if (_month > 12) { _month = 1; _year++; }
                _selectedDay = Math.Min(_selectedDay, DateTime.DaysInMonth(_year, _month));
                return;
            }

            // Days Grid click
            int headerH = 44;
            int dowY = clientY + headerH + 6;
            int colW = leftW / 7;
            int gridStartY = dowY + 22;
            int daysInMonth = DateTime.DaysInMonth(_year, _month);
            int firstDow = (int)new DateTime(_year, _month, 1).DayOfWeek;
            int cellH = (clientH - (gridStartY - clientY) - 10) / 6;

            if (mx >= clientX && mx < clientX + leftW && my >= gridStartY)
            {
                int col = (mx - clientX) / colW;
                int row = (my - gridStartY) / cellH;
                int clickedIndex = (row * 7) + col - firstDow + 1;
                if (clickedIndex >= 1 && clickedIndex <= daysInMonth)
                {
                    _selectedDay = clickedIndex;
                    _editingText = false;
                    return;
                }
            }

            // Sidebar Input Box click
            int sideX = clientX + leftW;
            int sideW = clientW - leftW;
            int inputY = clientY + clientH - 70;
            if (mx >= sideX + 10 && mx < sideX + sideW - 10 && my >= inputY && my < inputY + 28)
            {
                _editingText = true;
                return;
            }

            // Add Event Button click
            int addBtnY = inputY + 34;
            if (mx >= sideX + 10 && mx < sideX + sideW - 10 && my >= addBtnY && my < addBtnY + 26)
            {
                if (!string.IsNullOrWhiteSpace(_newEventText))
                {
                    string key = $"{_year}-{_month:D2}-{_selectedDay:D2}";
                    if (!_events.ContainsKey(key)) _events[key] = new List<string>();
                    _events[key].Add(_newEventText.Trim());
                    _newEventText = "";
                    _editingText = false;
                    SaveEvents();
                }
                return;
            }

            _editingText = false;
        }

        // Keyboard typing for new event
        if (_editingText)
        {
            while (InputState.TryReadKey(out var keyEvent))
            {
                if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.Enter)
                {
                    if (!string.IsNullOrWhiteSpace(_newEventText))
                    {
                        string key = $"{_year}-{_month:D2}-{_selectedDay:D2}";
                        if (!_events.ContainsKey(key)) _events[key] = new List<string>();
                        _events[key].Add(_newEventText.Trim());
                        _newEventText = "";
                        _editingText = false;
                        SaveEvents();
                    }
                }
                else if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.Backspace)
                {
                    if (_newEventText.Length > 0)
                    {
                        _newEventText = _newEventText.Substring(0, _newEventText.Length - 1);
                    }
                }
                else if (keyEvent.KeyChar >= ' ' && keyEvent.KeyChar <= '~')
                {
                    if (_newEventText.Length < 40)
                    {
                        _newEventText += keyEvent.KeyChar;
                    }
                }
            }
        }
    }
}
