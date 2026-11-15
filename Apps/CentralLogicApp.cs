using System;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class CentralLogicApp : AppBase
{
    public override string Id => "minesweeper";
    public override string Title => "CentralLogic";
    public override FastBitmap? Icon16 => AssetManager.IconCentralLogic16;
    public override FastBitmap? Icon24 => AssetManager.IconCentralLogic24;
    public override FastBitmap? Icon48 => AssetManager.IconCentralLogic48;

    private const int GridWidth = 10;
    private const int GridHeight = 10;
    private const int TotalMines = 12;
    private const int CellSize = 28;

    private bool[,] _mines = new bool[GridWidth, GridHeight];
    private bool[,] _revealed = new bool[GridWidth, GridHeight];
    private bool[,] _flagged = new bool[GridWidth, GridHeight];
    private int[,] _neighborMines = new int[GridWidth, GridHeight];

    private bool _gameOver = false;
    private bool _gameWon = false;
    private bool _firstClick = true;
    private int _remainingMines = TotalMines;
    private int _startTimeTick = 0;
    private int _elapsedSeconds = 0;

    public override void Initialize(Core.WindowManager.Window window)
    {
        base.Initialize(window);
        ResetGame();
    }

    private void ResetGame()
    {
        _gameOver = false;
        _gameWon = false;
        _firstClick = true;
        _remainingMines = TotalMines;
        _elapsedSeconds = 0;
        _startTimeTick = Environment.TickCount;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                _mines[x, y] = false;
                _revealed[x, y] = false;
                _flagged[x, y] = false;
                _neighborMines[x, y] = 0;
            }
        }
    }

    private void GenerateMines(int safeX, int safeY)
    {
        var rand = new Random(Environment.TickCount);
        int placed = 0;
        while (placed < TotalMines)
        {
            int rx = rand.Next(GridWidth);
            int ry = rand.Next(GridHeight);
            if (!_mines[rx, ry] && !(Math.Abs(rx - safeX) <= 1 && Math.Abs(ry - safeY) <= 1))
            {
                _mines[rx, ry] = true;
                placed++;
            }
        }

        // Count neighbors
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                if (_mines[x, y]) continue;
                int count = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < GridWidth && ny >= 0 && ny < GridHeight && _mines[nx, ny])
                        {
                            count++;
                        }
                    }
                }
                _neighborMines[x, y] = count;
            }
        }
    }

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, ColorScheme.BackgroundLight);

        // Header Panel (Score, Smiley, Timer)
        int headerH = 44;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 8, clientY + 8, clientW - 16, headerH, ColorScheme.ControlBackground);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, clientX + 8, clientY + 8, clientW - 16, headerH, ColorScheme.WindowBorder);

        // Mine Counter
        string mineStr = Math.Max(0, _remainingMines).ToString("D3");
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 16, clientY + 14, 50, 30, ColorScheme.Black);
        FontRenderer.DrawText(buffer, screenW, screenH, mineStr, clientX + 22, clientY + 18, ColorScheme.AccentRed, bold: true, size: 18);

        // Smiley Face Reset Button
        int smileyX = clientX + (clientW / 2) - 16;
        int smileyY = clientY + 14;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, smileyX, smileyY, 32, 30, ColorScheme.WindowBody);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, smileyX, smileyY, 32, 30, ColorScheme.WindowBorder);
        string face = _gameOver ? "X(" : (_gameWon ? "B)" : ":)");
        FontRenderer.DrawText(buffer, screenW, screenH, face, smileyX + 8, smileyY + 6, ColorScheme.TextPrimary, bold: true, size: 14);

        // Timer
        if (!_gameOver && !_gameWon && !_firstClick)
        {
            _elapsedSeconds = (Environment.TickCount - _startTimeTick) / 1000;
        }
        string timeStr = Math.Min(999, _elapsedSeconds).ToString("D3");
        int timerX = clientX + clientW - 74;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, timerX, clientY + 14, 50, 30, ColorScheme.Black);
        FontRenderer.DrawText(buffer, screenW, screenH, timeStr, timerX + 6, clientY + 18, ColorScheme.AccentRed, bold: true, size: 18);

        // Grid Area
        int gridTotalW = GridWidth * CellSize;
        int gridStartX = clientX + (clientW - gridTotalW) / 2;
        int gridStartY = clientY + headerH + 18;

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                int cx = gridStartX + (x * CellSize);
                int cy = gridStartY + (y * CellSize);

                bool isRev = _revealed[x, y];
                bool isFlg = _flagged[x, y];
                bool isM = _mines[x, y];

                if (isRev)
                {
                    FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, cx, cy, CellSize, CellSize, ColorScheme.WindowBody);
                    FastRenderer.DrawRectangle(buffer, screenW, screenH, cx, cy, CellSize, CellSize, ColorScheme.WindowBorder);

                    if (isM)
                    {
                        // Exploded Mine
                        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, cx + 2, cy + 2, CellSize - 4, CellSize - 4, ColorScheme.CloseButtonHover);
                        FontRenderer.DrawText(buffer, screenW, screenH, "💣", cx + 6, cy + 4, ColorScheme.TextWhite, bold: false, size: 14);
                    }
                    else if (_neighborMines[x, y] > 0)
                    {
                        int n = _neighborMines[x, y];
                        uint nCol = GetNumberColor(n);
                        FontRenderer.DrawText(buffer, screenW, screenH, n.ToString(), cx + 9, cy + 4, nCol, bold: true, size: 14);
                    }
                }
                else
                {
                    // Hidden Cell (3D effect)
                    FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, cx, cy, CellSize, CellSize, ColorScheme.ControlBackground);
                    FastRenderer.DrawRectangle(buffer, screenW, screenH, cx, cy, CellSize, CellSize, ColorScheme.ControlHover);

                    if (isFlg)
                    {
                        FontRenderer.DrawText(buffer, screenW, screenH, "🚩", cx + 6, cy + 4, ColorScheme.AccentRed, bold: false, size: 14);
                    }
                    else if (_gameOver && isM)
                    {
                        FontRenderer.DrawText(buffer, screenW, screenH, "💣", cx + 6, cy + 4, ColorScheme.TextPrimary, bold: false, size: 14);
                    }
                }
            }
        }
    }

    private uint GetNumberColor(int n)
    {
        return n switch
        {
            1 => 0xFF0000FF, // Blue
            2 => 0xFF008000, // Green
            3 => 0xFFFF0000, // Red
            4 => 0xFF000080, // Navy
            5 => 0xFF800000, // Maroon
            6 => 0xFF008080, // Teal
            7 => 0xFF000000, // Black
            _ => 0xFF808080  // Gray
        };
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        // Smiley face reset
        int smileyX = clientX + (clientW / 2) - 16;
        int smileyY = clientY + 14;
        if (InputState.LeftClicked && mx >= smileyX && mx < smileyX + 32 && my >= smileyY && my < smileyY + 30)
        {
            ResetGame();
            return;
        }

        if (_gameOver || _gameWon) return;

        // Grid clicks
        int headerH = 44;
        int gridTotalW = GridWidth * CellSize;
        int gridTotalH = GridHeight * CellSize;
        int gridStartX = clientX + (clientW - gridTotalW) / 2;
        int gridStartY = clientY + headerH + 18;

        if (mx >= gridStartX && mx < gridStartX + gridTotalW && my >= gridStartY && my < gridStartY + gridTotalH)
        {
            int gx = (mx - gridStartX) / CellSize;
            int gy = (my - gridStartY) / CellSize;

            if (gx >= 0 && gx < GridWidth && gy >= 0 && gy < GridHeight)
            {
                if (InputState.RightClicked)
                {
                    if (!_revealed[gx, gy])
                    {
                        _flagged[gx, gy] = !_flagged[gx, gy];
                        _remainingMines += _flagged[gx, gy] ? -1 : 1;
                    }
                }
                else if (InputState.LeftClicked && !_flagged[gx, gy])
                {
                    if (_firstClick)
                    {
                        _firstClick = false;
                        _startTimeTick = Environment.TickCount;
                        GenerateMines(gx, gy);
                    }

                    Reveal(gx, gy);
                    CheckVictory();
                }
            }
        }
    }

    private void Reveal(int x, int y)
    {
        if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight) return;
        if (_revealed[x, y] || _flagged[x, y]) return;

        _revealed[x, y] = true;

        if (_mines[x, y])
        {
            _gameOver = true;
            return;
        }

        // Auto-cascade if 0 neighbor mines
        if (_neighborMines[x, y] == 0)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx != 0 || dy != 0) Reveal(x + dx, y + dy);
                }
            }
        }
    }

    private void CheckVictory()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                if (!_mines[x, y] && !_revealed[x, y]) return;
            }
        }
        _gameWon = true;
    }
}
