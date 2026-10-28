using System;
using System.Drawing;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;
using NybuOS.GUI;

namespace NybuOS.Apps;

/// <summary>
/// CentralLogic: Jogo idêntico ao clássico Campo Minado (Minesweeper) do Windows 95.
/// Inclui grade 9x9, 10 minas, displays de contador/tempo digitais, botão de rostinho animado,
/// revelação recursiva (flood fill), sinalização com bandeiras e cores originais dos números.
/// </summary>
public class CentralLogicApp : Window
{
    public const int GridRows = 9;
    public const int GridCols = 9;
    public const int TotalMines = 10;
    public const int CellSize = 18;

    private struct Cell
    {
        public bool HasMine;
        public bool IsRevealed;
        public bool IsFlagged;
        public int AdjacentMines;
        public bool Exploded;
    }

    private readonly Cell[,] _grid = new Cell[GridRows, GridCols];
    private bool _gameOver = false;
    private bool _gameWon = false;
    private bool _firstClick = true;
    private bool _mouseIsDown = false;
    private bool _flagMode = false; // Alternador de ação para cliques
    private int _secondsElapsed = 0;
    private int _tickCount = 0;
    private int _flagsPlaced = 0;
    private readonly Random _rand = new Random();

    public CentralLogicApp(int id, int x, int y)
        : base(id, "CentralLogic - Campo Minado", x, y, 206, 260)
    {
        Icon = IconResources.Minesweeper16;
        StartNewGame();
    }

    public void StartNewGame()
    {
        _gameOver = false;
        _gameWon = false;
        _firstClick = true;
        _mouseIsDown = false;
        _secondsElapsed = 0;
        _tickCount = 0;
        _flagsPlaced = 0;

        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                _grid[r, c] = new Cell
                {
                    HasMine = false,
                    IsRevealed = false,
                    IsFlagged = false,
                    AdjacentMines = 0,
                    Exploded = false
                };
            }
        }
    }

    private void PlaceMines(int avoidRow, int avoidCol)
    {
        int placed = 0;
        while (placed < TotalMines)
        {
            int r = _rand.Next(GridRows);
            int c = _rand.Next(GridCols);

            if (_grid[r, c].HasMine || (r == avoidRow && c == avoidCol))
                continue;

            _grid[r, c].HasMine = true;
            placed++;
        }

        // Calcula contagem de minas vizinhas
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                if (_grid[r, c].HasMine) continue;

                int count = 0;
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int nr = r + dr;
                        int nc = c + dc;
                        if (nr >= 0 && nr < GridRows && nc >= 0 && nc < GridCols && _grid[nr, nc].HasMine)
                        {
                            count++;
                        }
                    }
                }
                _grid[r, c].AdjacentMines = count;
            }
        }
    }

    public override void DrawContent(DisplayDriver display)
    {
        int cx = ClientX;
        int cy = ClientY;
        int cw = ClientWidth;
        int ch = ClientHeight;

        // Fundo estilo Windows 95
        display.DrawFilledRectangle(cx, cy, cw, ch, Theme.Surface);

        // Moldura externa em chanfrado 3D
        display.Draw3DBevel(cx + 4, cy + 4, cw - 8, ch - 8, true);

        // Painel Superior de Status (Placar)
        int scoreBoxX = cx + 8;
        int scoreBoxY = cy + 8;
        int scoreBoxW = cw - 16;
        int scoreBoxH = 32;

        display.DrawFilledRectangle(scoreBoxX, scoreBoxY, scoreBoxW, scoreBoxH, Theme.Surface);
        display.Draw3DBevel(scoreBoxX, scoreBoxY, scoreBoxW, scoreBoxH, true);

        // Display Digital de Minas Restantes (Esquerda)
        int minesLeft = Math.Max(0, TotalMines - _flagsPlaced);
        DrawLedDisplay(display, scoreBoxX + 6, scoreBoxY + 5, minesLeft);

        // Botão Central do Rostinho (Smiley Face)
        int faceBtnX = scoreBoxX + (scoreBoxW - 24) / 2;
        int faceBtnY = scoreBoxY + 4;
        display.DrawButton(faceBtnX, faceBtnY, 24, 24, "", _mouseIsDown);
        DrawSmileyFace(display, faceBtnX + 4, faceBtnY + 4);

        // Display Digital de Tempo (Direita)
        DrawLedDisplay(display, scoreBoxX + scoreBoxW - 46, scoreBoxY + 5, Math.Min(999, _secondsElapsed));

        // Botão seletor de modo Bandeira / Revelar
        int modeBtnY = scoreBoxY + scoreBoxH + 4;
        display.DrawButton(cx + 8, modeBtnY, cw - 16, 18, _flagMode ? "Modo: BANDEIRA [P]" : "Modo: CAVAR [Abrir]", _flagMode);

        // Campo do Tabuleiro de Células
        int boardX = cx + 18;
        int boardY = modeBtnY + 22;
        int boardW = GridCols * CellSize;
        int boardH = GridRows * CellSize;

        display.Draw3DBevel(boardX - 2, boardY - 2, boardW + 4, boardH + 4, true);

        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                int cellX = boardX + (c * CellSize);
                int cellY = boardY + (r * CellSize);
                DrawCell(display, cellX, cellY, r, c);
            }
        }

        // Incremento do tempo de jogo
        if (!_gameOver && !_gameWon && !_firstClick)
        {
            _tickCount++;
            if (_tickCount % 60 == 0)
            {
                _secondsElapsed++;
            }
        }
    }

    private void DrawLedDisplay(DisplayDriver display, int x, int y, int value)
    {
        display.DrawFilledRectangle(x, y, 40, 22, Color.Black);
        display.Draw3DBevel(x, y, 40, 22, true);

        string s = $"{value:D3}";
        display.DrawString(x + 4, y + 5, s, Color.Red);
    }

    private void DrawSmileyFace(DisplayDriver display, int x, int y)
    {
        int cx = x + 7;
        int cy = y + 7;
        int r = 6;

        Color yellow = Color.Yellow;
        Color black = Color.Black;

        // Círculo amarelo
        for (int dy = -r; dy <= r; dy++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy <= r * r)
                {
                    display.DrawPixel(cx + dx, cy + dy, yellow);
                }
            }
        }

        if (_gameOver)
        {
            // Derrota: Olhos em X
            display.DrawPixel(cx - 3, cy - 2, black);
            display.DrawPixel(cx - 2, cy - 3, black);
            display.DrawPixel(cx - 3, cy - 3, black);
            display.DrawPixel(cx - 2, cy - 2, black);

            display.DrawPixel(cx + 2, cy - 2, black);
            display.DrawPixel(cx + 3, cy - 3, black);
            display.DrawPixel(cx + 2, cy - 3, black);
            display.DrawPixel(cx + 3, cy - 2, black);

            // Boca triste invertida
            display.DrawLine(cx - 3, cy + 3, cx + 3, cy + 3, black);
        }
        else if (_gameWon)
        {
            // Vitória: Óculos escuros
            display.DrawFilledRectangle(cx - 4, cy - 3, 4, 3, black);
            display.DrawFilledRectangle(cx + 1, cy - 3, 4, 3, black);
            display.DrawLine(cx - 5, cy - 3, cx + 5, cy - 3, black);

            // Sorriso
            display.DrawPixel(cx - 3, cy + 2, black);
            display.DrawLine(cx - 2, cy + 3, cx + 2, cy + 3, black);
            display.DrawPixel(cx + 3, cy + 2, black);
        }
        else if (_mouseIsDown)
        {
            // Tensão: Olhos normais e boca redonda "O"
            display.DrawPixel(cx - 2, cy - 2, black);
            display.DrawPixel(cx + 2, cy - 2, black);
            display.DrawRectangle(cx - 1, cy + 1, 3, 3, black);
        }
        else
        {
            // Normal: Olhos pontuais e sorriso clássico
            display.DrawPixel(cx - 2, cy - 2, black);
            display.DrawPixel(cx + 2, cy - 2, black);

            display.DrawPixel(cx - 3, cy + 2, black);
            display.DrawLine(cx - 2, cy + 3, cx + 2, cy + 3, black);
            display.DrawPixel(cx + 3, cy + 2, black);
        }
    }

    private void DrawCell(DisplayDriver display, int x, int y, int row, int col)
    {
        Cell cell = _grid[row, col];

        if (!cell.IsRevealed)
        {
            // Célula não aberta: Botão em relevo 3D
            display.DrawFilledRectangle(x, y, CellSize, CellSize, Theme.Surface);
            display.Draw3DBevel(x, y, CellSize, CellSize, false);

            if (cell.IsFlagged)
            {
                // Bandeira clássica do Windows (haste preta e flâmula vermelha)
                display.DrawLine(x + 9, y + 4, x + 9, y + 13, Color.Black);
                display.DrawFilledRectangle(x + 5, y + 4, 4, 4, Color.Red);
                display.DrawLine(x + 7, y + 13, x + 12, y + 13, Color.Black);
            }
        }
        else
        {
            // Célula revelada
            Color cellBg = cell.Exploded ? Color.Red : Color.FromArgb(210, 210, 210);
            display.DrawFilledRectangle(x, y, CellSize, CellSize, cellBg);
            display.DrawRectangle(x, y, CellSize, CellSize, Color.FromArgb(160, 160, 160));

            if (cell.HasMine)
            {
                // Mina clássica
                int mcx = x + CellSize / 2;
                int mcy = y + CellSize / 2;
                int mr = 4;

                for (int dy = -mr; dy <= mr; dy++)
                {
                    for (int dx = -mr; dx <= mr; dx++)
                    {
                        if (dx * dx + dy * dy <= mr * mr)
                            display.DrawPixel(mcx + dx, mcy + dy, Color.Black);
                    }
                }
                display.DrawLine(mcx - 5, mcy, mcx + 5, mcy, Color.Black);
                display.DrawLine(mcx, mcy - 5, mcx, mcy + 5, Color.Black);
                display.DrawPixel(mcx - 1, mcy - 1, Color.White); // Brilho na mina
            }
            else if (cell.AdjacentMines > 0)
            {
                Color numColor = GetNumberColor(cell.AdjacentMines);
                display.DrawChar(x + 5, y + 3, (char)('0' + cell.AdjacentMines), numColor);
            }
        }
    }

    private Color GetNumberColor(int number)
    {
        return number switch
        {
            1 => Color.FromArgb(0, 0, 255),     // Azul
            2 => Color.FromArgb(0, 128, 0),     // Verde
            3 => Color.FromArgb(255, 0, 0),     // Vermelho
            4 => Color.FromArgb(0, 0, 128),     // Azul Marinho
            5 => Color.FromArgb(128, 0, 0),     // Marrom
            6 => Color.FromArgb(0, 128, 128),   // Ciano
            7 => Color.FromArgb(0, 0, 0),       // Preto
            8 => Color.FromArgb(128, 128, 128), // Cinza
            _ => Color.Black
        };
    }

    public override void OnMouseDown(int mouseX, int mouseY)
    {
        int cx = ClientX;
        int cy = ClientY;
        int scoreBoxX = cx + 8;
        int scoreBoxY = cy + 8;
        int scoreBoxW = ClientWidth - 16;

        // Clique no Botão do Rostinho (Reiniciar)
        int faceBtnX = scoreBoxX + (scoreBoxW - 24) / 2;
        int faceBtnY = scoreBoxY + 4;
        if (mouseX >= faceBtnX && mouseX <= faceBtnX + 24 && mouseY >= faceBtnY && mouseY <= faceBtnY + 24)
        {
            StartNewGame();
            return;
        }

        // Clique no Seletor de Modo (Bandeira vs Cavar)
        int modeBtnY = scoreBoxY + 32 + 4;
        if (mouseX >= cx + 8 && mouseX <= cx + ClientWidth - 8 && mouseY >= modeBtnY && mouseY <= modeBtnY + 18)
        {
            _flagMode = !_flagMode;
            return;
        }

        if (_gameOver || _gameWon) return;

        // Clique nas Células do Tabuleiro
        int boardX = cx + 18;
        int boardY = modeBtnY + 22;
        int boardW = GridCols * CellSize;
        int boardH = GridRows * CellSize;

        if (mouseX >= boardX && mouseX < boardX + boardW && mouseY >= boardY && mouseY < boardY + boardH)
        {
            int col = (mouseX - boardX) / CellSize;
            int row = (mouseY - boardY) / CellSize;

            if (row >= 0 && row < GridRows && col >= 0 && col < GridCols)
            {
                _mouseIsDown = true;

                if (_flagMode)
                {
                    ToggleFlag(row, col);
                }
                else
                {
                    RevealCell(row, col);
                }
            }
        }
    }

    public override void OnMouseUp(int mouseX, int mouseY)
    {
        _mouseIsDown = false;
    }

    private void ToggleFlag(int row, int col)
    {
        if (_grid[row, col].IsRevealed) return;

        _grid[row, col].IsFlagged = !_grid[row, col].IsFlagged;
        _flagsPlaced += _grid[row, col].IsFlagged ? 1 : -1;
    }

    private void RevealCell(int row, int col)
    {
        if (_grid[row, col].IsRevealed || _grid[row, col].IsFlagged)
            return;

        if (_firstClick)
        {
            _firstClick = false;
            PlaceMines(row, col);
        }

        _grid[row, col].IsRevealed = true;

        if (_grid[row, col].HasMine)
        {
            // Detonou uma mina: Fim de Jogo!
            _grid[row, col].Exploded = true;
            _gameOver = true;

            // Revela todas as outras minas
            for (int r = 0; r < GridRows; r++)
            {
                for (int c = 0; c < GridCols; c++)
                {
                    if (_grid[r, c].HasMine)
                        _grid[r, c].IsRevealed = true;
                }
            }
            return;
        }

        // Revelação Recursiva (Flood Fill) se for célula vazia (0 minas vizinhas)
        if (_grid[row, col].AdjacentMines == 0)
        {
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    int nr = row + dr;
                    int nc = col + dc;
                    if (nr >= 0 && nr < GridRows && nc >= 0 && nc < GridCols)
                    {
                        if (!_grid[nr, nc].IsRevealed && !_grid[nr, nc].IsFlagged)
                        {
                            RevealCell(nr, nc);
                        }
                    }
                }
            }
        }

        CheckVictory();
    }

    private void CheckVictory()
    {
        int unrevealedSafeCells = 0;
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                if (!_grid[r, c].HasMine && !_grid[r, c].IsRevealed)
                {
                    unrevealedSafeCells++;
                }
            }
        }

        if (unrevealedSafeCells == 0)
        {
            _gameWon = true;
            // Marca automaticamente todas as minas com bandeiras na vitória
            for (int r = 0; r < GridRows; r++)
            {
                for (int c = 0; c < GridCols; c++)
                {
                    if (_grid[r, c].HasMine)
                        _grid[r, c].IsFlagged = true;
                }
            }
            _flagsPlaced = TotalMines;
        }
    }
}
