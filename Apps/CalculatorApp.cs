using System;
using System.Collections.Generic;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class CalculatorApp : AppBase
{
    public override string Id => "calc";
    public override string Title => "Calculadora";
    public override FastBitmap? Icon16 => AssetManager.IconCalc16;
    public override FastBitmap? Icon24 => AssetManager.IconCalc24;
    public override FastBitmap? Icon48 => AssetManager.IconCalc48;

    private string _currentInput = "0";
    private string _expression = "";
    private double _storedValue = 0;
    private string _pendingOp = "";
    private bool _newNumber = true;
    private readonly List<string> _history = new();

    private readonly string[,] _buttons = {
        { "CE", "C", "DEL", "÷" },
        { "7", "8", "9", "×" },
        { "4", "5", "6", "-" },
        { "1", "2", "3", "+" },
        { "±", "0", ".", "=" }
    };

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, ColorScheme.WindowBody);

        // 1. Display Area (top 90px)
        int dispH = 90;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, dispH, ColorScheme.BackgroundLight);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, clientY + dispH, clientW, ColorScheme.WindowBorder);

        // Expression preview (Secondary display)
        int exprW = FontRenderer.MeasureText(_expression, bold: false, size: 12);
        FontRenderer.DrawText(buffer, screenW, screenH, _expression, clientX + clientW - exprW - 16, clientY + 12, ColorScheme.TextSecondary, bold: false, size: 12);

        // Current Main Value (Large display)
        int valW = FontRenderer.MeasureText(_currentInput, bold: true, size: 22);
        FontRenderer.DrawText(buffer, screenW, screenH, _currentInput, clientX + clientW - valW - 16, clientY + 44, ColorScheme.TextPrimary, bold: true, size: 22);

        // 2. Buttons Grid
        int gridY = clientY + dispH + 6;
        int gridH = clientH - dispH - 12;

        int rows = 5;
        int cols = 4;
        int btnW = (clientW - 20) / cols;
        int btnH = (gridH - 20) / rows;

        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int bx = clientX + 10 + (c * (btnW + 2));
                int by = gridY + (r * (btnH + 2));

                string label = _buttons[r, c];
                bool isOp = label == "=" || label == "+" || label == "-" || label == "×" || label == "÷";
                bool isEquals = label == "=";

                bool hover = mx >= bx && mx < bx + btnW && my >= by && my < by + btnH;
                uint bg = isEquals ? ColorScheme.Accent : (isOp ? ColorScheme.BackgroundLight : (hover ? ColorScheme.ControlHover : ColorScheme.ControlBackground));
                uint textCol = isEquals ? ColorScheme.TextWhite : ColorScheme.TextPrimary;

                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, bx, by, btnW, btnH, bg);
                FastRenderer.DrawRectangle(buffer, screenW, screenH, bx, by, btnW, btnH, ColorScheme.WindowBorder);

                int lw = FontRenderer.MeasureText(label, bold: isEquals, size: 14);
                FontRenderer.DrawText(buffer, screenW, screenH, label, bx + (btnW - lw) / 2, by + (btnH / 2) - 8, textCol, bold: isEquals, size: 14);
            }
        }
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        if (!InputState.LeftClicked) return;

        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        int dispH = 90;
        int gridY = clientY + dispH + 6;
        int gridH = clientH - dispH - 12;

        int rows = 5;
        int cols = 4;
        int btnW = (clientW - 20) / cols;
        int btnH = (gridH - 20) / rows;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int bx = clientX + 10 + (c * (btnW + 2));
                int by = gridY + (r * (btnH + 2));

                if (mx >= bx && mx < bx + btnW && my >= by && my < by + btnH)
                {
                    OnButtonClicked(_buttons[r, c]);
                    return;
                }
            }
        }
    }

    private void OnButtonClicked(string btn)
    {
        if (char.IsDigit(btn[0]))
        {
            if (_newNumber || _currentInput == "0")
            {
                _currentInput = btn;
                _newNumber = false;
            }
            else
            {
                if (_currentInput.Length < 14) _currentInput += btn;
            }
        }
        else if (btn == ".")
        {
            if (_newNumber)
            {
                _currentInput = "0.";
                _newNumber = false;
            }
            else if (!_currentInput.Contains('.'))
            {
                _currentInput += ".";
            }
        }
        else if (btn == "±")
        {
            if (_currentInput.StartsWith('-')) _currentInput = _currentInput.Substring(1);
            else if (_currentInput != "0") _currentInput = "-" + _currentInput;
        }
        else if (btn == "DEL")
        {
            if (!_newNumber && _currentInput.Length > 0)
            {
                _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
                if (_currentInput.Length == 0 || _currentInput == "-") _currentInput = "0";
            }
        }
        else if (btn == "C")
        {
            _currentInput = "0";
            _expression = "";
            _storedValue = 0;
            _pendingOp = "";
            _newNumber = true;
        }
        else if (btn == "CE")
        {
            _currentInput = "0";
            _newNumber = true;
        }
        else if (btn == "=")
        {
            if (!string.IsNullOrEmpty(_pendingOp))
            {
                double cur = double.TryParse(_currentInput, out var d) ? d : 0;
                double res = Compute(_storedValue, cur, _pendingOp);
                _history.Add($"{_storedValue} {_pendingOp} {cur} = {res}");
                _expression = $"{_storedValue} {_pendingOp} {cur} =";
                _currentInput = res.ToString();
                _pendingOp = "";
                _newNumber = true;
            }
        }
        else // +, -, ×, ÷
        {
            double cur = double.TryParse(_currentInput, out var d) ? d : 0;
            if (!string.IsNullOrEmpty(_pendingOp) && !_newNumber)
            {
                _storedValue = Compute(_storedValue, cur, _pendingOp);
                _currentInput = _storedValue.ToString();
            }
            else
            {
                _storedValue = cur;
            }
            _pendingOp = btn;
            _expression = $"{_storedValue} {btn}";
            _newNumber = true;
        }
    }

    private double Compute(double a, double b, string op)
    {
        return op switch
        {
            "+" => a + b,
            "-" => a - b,
            "×" => a * b,
            "÷" => b != 0 ? a / b : 0,
            _ => b
        };
    }
}
