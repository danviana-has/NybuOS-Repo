using System;
using System.Drawing;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// Calculadora clássica funcional estilo Windows 95 com botões clicáveis e suporte a teclado.
/// </summary>
public class CalculatorApp : Window
{
    private string _displayText = "0";
    private double _storedValue = 0;
    private char _pendingOp = '\0';
    private bool _startNewNumber = true;

    private readonly string[,] _buttons = new string[,]
    {
        { "C",  "CE", "+/-", "/" },
        { "7",  "8",  "9",   "*" },
        { "4",  "5",  "6",   "-" },
        { "1",  "2",  "3",   "+" },
        { "0",  ".",  "=",   "=" }
    };

    public CalculatorApp(int id, int x, int y)
        : base(id, "Calculadora", x, y, 220, 240)
    {
        Icon = IconResources.Calculator16;
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo cinza de superfície
        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.Surface);

        // Display numérico chanfrado afundado (Sunken)
        int dispX = cx + 8;
        int dispY = cy + 8;
        int dispW = cw - 16;
        int dispH = 26;

        display.DrawFilledRectangle(dispX, dispY, dispW, dispH, Color.White);
        display.Draw3DBevel(dispX, dispY, dispW, dispH, true);

        // Texto do display alinhado à direita
        int textW = _displayText.Length * Font8x12.Width;
        int textX = Math.Max(dispX + 4, dispX + dispW - textW - 6);
        display.DrawString(textX, dispY + 7, _displayText, Color.Black);

        // Grade de Botões
        int gridX = cx + 8;
        int gridY = dispY + dispH + 10;
        int btnW = (dispW - 12) / 4;
        int btnH = 24;

        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                if (r == 4 && c == 3) continue; // O botão '=' ocupa duas posições ou é duplo

                int bx = gridX + (c * (btnW + 4));
                int by = gridY + (r * (btnH + 4));
                int currentBtnW = (r == 4 && c == 2) ? (btnW * 2 + 4) : btnW;

                string label = _buttons[r, c];
                display.DrawButton(bx, by, currentBtnW, btnH, label, false);
            }
        }
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int dispW = cw - 16;
        int gridX = cx + 8;
        int gridY = cy + 8 + 26 + 10;
        int btnW = (dispW - 12) / 4;
        int btnH = 24;

        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                if (r == 4 && c == 3) continue;

                int bx = gridX + (c * (btnW + 4));
                int by = gridY + (r * (btnH + 4));
                int currentBtnW = (r == 4 && c == 2) ? (btnW * 2 + 4) : btnW;

                if (mouseX >= bx && mouseX <= bx + currentBtnW && mouseY >= by && mouseY <= by + btnH)
                {
                    HandleInput(_buttons[r, c]);
                    return;
                }
            }
        }
    }

    public override void OnKeyDown(KeyEvent key)
    {
        char c = key.KeyChar;
        if (c >= '0' && c <= '9')
        {
            HandleInput(c.ToString());
        }
        else if (c == '+' || c == '-' || c == '*' || c == '/')
        {
            HandleInput(c.ToString());
        }
        else if (c == '=' || key.Key == Key.Enter)
        {
            HandleInput("=");
        }
        else if (key.Key == Key.Escape || c == 'c' || c == 'C')
        {
            HandleInput("C");
        }
        else if (c == '.' || c == ',')
        {
            HandleInput(".");
        }
        else if (key.Key == Key.Backspace)
        {
            if (_displayText.Length > 1)
            {
                _displayText = _displayText.Substring(0, _displayText.Length - 1);
            }
            else
            {
                _displayText = "0";
            }
        }
    }

    private void HandleInput(string cmd)
    {
        if (double.TryParse(cmd, out double digit))
        {
            if (_startNewNumber || _displayText == "0")
            {
                _displayText = cmd;
                _startNewNumber = false;
            }
            else
            {
                if (_displayText.Length < 14)
                {
                    _displayText += cmd;
                }
            }
        }
        else if (cmd == ".")
        {
            if (_startNewNumber)
            {
                _displayText = "0.";
                _startNewNumber = false;
            }
            else if (!_displayText.Contains("."))
            {
                _displayText += ".";
            }
        }
        else if (cmd == "C")
        {
            _displayText = "0";
            _storedValue = 0;
            _pendingOp = '\0';
            _startNewNumber = true;
        }
        else if (cmd == "CE")
        {
            _displayText = "0";
            _startNewNumber = true;
        }
        else if (cmd == "+/-")
        {
            if (double.TryParse(_displayText, out double val))
            {
                _displayText = (-val).ToString();
            }
        }
        else if (cmd == "+" || cmd == "-" || cmd == "*" || cmd == "/")
        {
            EvaluatePending();
            if (double.TryParse(_displayText, out double val))
            {
                _storedValue = val;
            }
            _pendingOp = cmd[0];
            _startNewNumber = true;
        }
        else if (cmd == "=")
        {
            EvaluatePending();
            _pendingOp = '\0';
            _startNewNumber = true;
        }
    }

    private void EvaluatePending()
    {
        if (_pendingOp == '\0') return;

        if (double.TryParse(_displayText, out double currentVal))
        {
            double result = 0;
            switch (_pendingOp)
            {
                case '+': result = _storedValue + currentVal; break;
                case '-': result = _storedValue - currentVal; break;
                case '*': result = _storedValue * currentVal; break;
                case '/':
                    if (currentVal == 0)
                    {
                        _displayText = "Erro: Div por 0";
                        _startNewNumber = true;
                        _pendingOp = '\0';
                        return;
                    }
                    result = _storedValue / currentVal;
                    break;
            }

            _displayText = result.ToString();
            _storedValue = result;
        }
    }
}
