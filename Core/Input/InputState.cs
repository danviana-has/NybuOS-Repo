using Cosmos.Kernel.System.Input;

namespace NybuOS.Core.Input;

public static class InputState
{
    public static int MouseX { get; private set; }
    public static int MouseY { get; private set; }

    public static bool LeftDown { get; private set; }
    public static bool LeftClicked { get; private set; }
    public static bool LeftReleased { get; private set; }

    public static bool RightDown { get; private set; }
    public static bool RightClicked { get; private set; }
    public static bool RightReleased { get; private set; }

    public static int ScrollDelta { get; private set; }

    private static bool _prevLeft = false;
    private static bool _prevRight = false;

    public static void Initialize(int screenWidth, int screenHeight)
    {
        try
        {
            MouseManager.SetScreenSize(screenWidth, screenHeight);
            MouseManager.SetPosition(screenWidth / 2, screenHeight / 2);
        }
        catch { }
    }

    public static void Update()
    {
        try
        {
            MouseX = (int)MouseManager.X;
            MouseY = (int)MouseManager.Y;

            bool curLeft = MouseManager.LeftButton;
            bool curRight = MouseManager.RightButton;

            LeftClicked = curLeft && !_prevLeft;
            LeftReleased = !curLeft && _prevLeft;
            LeftDown = curLeft;

            RightClicked = curRight && !_prevRight;
            RightReleased = !curRight && _prevRight;
            RightDown = curRight;

            ScrollDelta = MouseManager.ScrollDelta;
            if (ScrollDelta != 0) MouseManager.ResetScrollDelta();

            _prevLeft = curLeft;
            _prevRight = curRight;
        }
        catch { }
    }

    public static bool TryReadKey(out KeyEvent key)
    {
        if (KeyboardManager.TryReadKey(out var k))
        {
            key = k;
            return true;
        }
        key = default;
        return false;
    }
}
