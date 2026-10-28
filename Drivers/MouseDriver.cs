using System;
using Cosmos.Kernel.System.Input;

namespace NybuOS.Drivers;

/// <summary>
/// Driver e tratador do mouse PS/2 com clamping de tela e detecção de cliques/arrasto.
/// </summary>
public class MouseDriver
{
    private int _screenWidth;
    private int _screenHeight;

    public int X { get; private set; }
    public int Y { get; private set; }
    public int PreviousX { get; private set; }
    public int PreviousY { get; private set; }
    public int DeltaX => X - PreviousX;
    public int DeltaY => Y - PreviousY;

    public bool LeftButton { get; private set; }
    public bool PreviousLeftButton { get; private set; }
    public bool RightButton { get; private set; }
    public bool PreviousRightButton { get; private set; }

    public bool JustPressedLeft => LeftButton && !PreviousLeftButton;
    public bool JustReleasedLeft => !LeftButton && PreviousLeftButton;
    public bool JustPressedRight => RightButton && !PreviousRightButton;
    public bool JustReleasedRight => !RightButton && PreviousRightButton;

    public MouseDriver(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        X = screenWidth / 2;
        Y = screenHeight / 2;
        PreviousX = X;
        PreviousY = Y;
    }

    public void Initialize()
    {
        try
        {
            MouseManager.SetScreenSize(_screenWidth, _screenHeight);
            MouseManager.SetPosition(X, Y);
        }
        catch
        {
            // Fallback caso a inicialização do HAL já tenha sido realizada
        }
    }

    public void Update()
    {
        PreviousX = X;
        PreviousY = Y;
        PreviousLeftButton = LeftButton;
        PreviousRightButton = RightButton;

        try
        {
            int rawX = MouseManager.X;
            int rawY = MouseManager.Y;

            // Clamping para não sair dos limites da resolução
            if (rawX < 0) rawX = 0;
            if (rawX >= _screenWidth) rawX = _screenWidth - 1;

            if (rawY < 0) rawY = 0;
            if (rawY >= _screenHeight) rawY = _screenHeight - 1;

            X = rawX;
            Y = rawY;

            LeftButton = MouseManager.LeftButton;
            RightButton = MouseManager.RightButton;
        }
        catch
        {
            // Proteção contra leitura instável do registrador PS/2
        }
    }
}
