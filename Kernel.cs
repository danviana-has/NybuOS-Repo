using System;
using System.Runtime.CompilerServices;
using Cosmos.Kernel.System.Graphics;
using NybuOS.Apps;
using NybuOS.Core.FileSystem;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;
using NybuOS.Core.WindowManager;
using Sys = Cosmos.Kernel.System;

namespace NybuOS;

/// <summary>
/// Main Kernel for NybuOS — Versão V Lagoons (x64 Bare-Metal Cosmos Gen 3).
/// Built for ultra-fast, zero-allocation graphics rendering and full Windows 10 Light UI.
/// </summary>
public class Kernel : Sys.Kernel
{
    private Canvas? _canvas;
    private uint[]? _backBuffer;
    private WindowManager? _windowManager;

    protected override void BeforeRun()
    {
        // 1. Initialize Real FAT32 VFS
        FileService.Initialize();

        // 2. Initialize High-Resolution Framebuffer Canvas
        _canvas = Canvas.GetFullScreen();
        int width = _canvas.Width;
        int height = _canvas.Height;

        // 3. Bind Double-Buffer with Zero Allocation via Unsafe Reference
        int[] rawBuffer = _canvas.GetBuffer();
        _backBuffer = Unsafe.As<uint[]>(rawBuffer)!;

        // 4. Initialize Hardware Input Handlers
        InputState.Initialize(width, height);

        // 5. Load Embedded Assets (TrueType Fonts, Icons, Wallpaper)
        AssetManager.Initialize(width, height);

        // 6. Initialize LunarFonts Typography Engine
        FontRenderer.Initialize();

        // 7. Initialize Desktop & Window Manager
        _windowManager = new WindowManager(width, height);
        AppRegistry.Initialize(_windowManager);

        // 8. Launch Welcome Application (AboutApp)
        AppRegistry.LaunchById(_windowManager, "about");
    }

    protected override void Run()
    {
        if (_windowManager == null || _backBuffer == null || _canvas == null) return;

        // Process User Inputs & Window Management
        _windowManager.Update();

        // Render Frame Directly to Backbuffer (Zero Allocations in Loop)
        _windowManager.Render(_backBuffer);

        // Flush Frame to Video Hardware
        _canvas.Display();
    }
}
