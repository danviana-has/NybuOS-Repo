using System;
using NybuOS.Core.WindowManager;

namespace NybuOS.Apps;

public static class AppRegistry
{
    private static NotepadApp? _activeNotepad;
    private static WindowManager? _windowManager;

    public static void Initialize(WindowManager windowManager)
    {
        _windowManager = windowManager;
    }

    public static void LaunchById(WindowManager wm, string appId)
    {
        _windowManager = wm;
        switch (appId.ToLowerInvariant())
        {
            case "files":
                wm.LaunchApp(new FileManagerApp(), 680, 480);
                break;
            case "calc":
                wm.LaunchApp(new CalculatorApp(), 340, 480);
                break;
            case "about":
                wm.LaunchApp(new AboutApp(), 580, 440);
                break;
            case "minesweeper":
                wm.LaunchApp(new CentralLogicApp(), 340, 420);
                break;
            case "calendar":
                wm.LaunchApp(new CalendarApp(), 640, 450);
                break;
            case "terminal":
                wm.LaunchApp(new TerminalApp(), 680, 440);
                break;
            case "settings":
                wm.LaunchApp(new SettingsApp(), 640, 460);
                break;
            case "notepad":
                _activeNotepad = new NotepadApp();
                wm.LaunchApp(_activeNotepad, 640, 460);
                break;
            case "paint":
                wm.LaunchApp(new PaintPadApp(), 680, 480);
                break;
            case "health":
                wm.LaunchApp(new SystemHealthApp(), 600, 460);
                break;
            case "help":
                wm.LaunchApp(new GetHelpApp(), 700, 500);
                break;
            case "sheets":
                wm.LaunchApp(new SpreadsheetApp(), 720, 480);
                break;
        }
    }

    public static void OpenInNotepad(string filePath)
    {
        if (_windowManager == null) return;
        _activeNotepad = new NotepadApp();
        _windowManager.LaunchApp(_activeNotepad, 640, 460);
        _activeNotepad.LoadFile(filePath);
    }

    public static void CloseApp(string appId)
    {
        _windowManager?.CloseApp(appId);
    }
}
