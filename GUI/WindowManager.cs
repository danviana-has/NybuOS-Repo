using System;
using System.Collections.Generic;
using Cosmos.Kernel.System.Input;
using NybuOS.Drivers;

namespace NybuOS.GUI;

/// <summary>
/// Gerenciador de Janelas com pilha Z-order, drag & drop, controle de foco e eventos.
/// </summary>
public class WindowManager
{
    private readonly List<Window> _windows = new List<Window>();
    private Window _draggingWindow = null;
    private int _dragOffsetX = 0;
    private int _dragOffsetY = 0;

    public IReadOnlyList<Window> Windows => _windows;
    public Window ActiveWindow => _windows.Count > 0 ? _windows[_windows.Count - 1] : null;

    public void AddWindow(Window window)
    {
        if (window == null) return;
        _windows.Add(window);
        BringToFront(window);
    }

    public void RemoveWindow(Window window)
    {
        if (window == null) return;
        _windows.Remove(window);
        ActivateTopWindow();
    }

    public void BringToFront(Window window)
    {
        if (window == null || !_windows.Contains(window)) return;

        _windows.Remove(window);
        _windows.Add(window);

        foreach (var w in _windows)
        {
            w.IsActive = false;
        }

        window.IsActive = true;
        window.IsMinimized = false;
    }

    public void ActivateTopWindow()
    {
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            if (!_windows[i].IsMinimized && !_windows[i].IsClosed)
            {
                foreach (var w in _windows) w.IsActive = false;
                _windows[i].IsActive = true;
                return;
            }
        }
    }

    public void HandleMouseDown(int mouseX, int mouseY)
    {
        // Itera de cima para baixo na pilha Z-order
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            var win = _windows[i];
            if (win.IsMinimized || win.IsClosed) continue;

            if (win.ContainsPoint(mouseX, mouseY))
            {
                // Clique no Botão Fechar
                if (win.IsCloseButtonClicked(mouseX, mouseY))
                {
                    win.IsClosed = true;
                    _windows.RemoveAt(i);
                    ActivateTopWindow();
                    return;
                }

                // Clique no Botão Minimizar
                if (win.IsMinimizeButtonClicked(mouseX, mouseY))
                {
                    win.IsMinimized = true;
                    ActivateTopWindow();
                    return;
                }

                // Traz a janela para a frente e ativa
                BringToFront(win);

                // Clique na barra de título para arrastar
                if (win.IsTitleBarClicked(mouseX, mouseY))
                {
                    _draggingWindow = win;
                    _dragOffsetX = mouseX - win.X;
                    _dragOffsetY = mouseY - win.Y;
                    return;
                }

                // Encaminha clique para o conteúdo da janela
                win.OnMouseDown(mouseX, mouseY);
                return;
            }
        }
    }

    public void HandleMouseMove(int mouseX, int mouseY)
    {
        if (_draggingWindow != null)
        {
            _draggingWindow.X = mouseX - _dragOffsetX;
            _draggingWindow.Y = mouseY - _dragOffsetY;

            // Clamping para não desaparecer da tela
            if (_draggingWindow.X < -_draggingWindow.Width + 40) _draggingWindow.X = -_draggingWindow.Width + 40;
            if (_draggingWindow.Y < 0) _draggingWindow.Y = 0;
            if (_draggingWindow.Y > 600 - Taskbar.Height - 20) _draggingWindow.Y = 600 - Taskbar.Height - 20;

            return;
        }

        var active = ActiveWindow;
        if (active != null && !active.IsMinimized && !active.IsClosed)
        {
            active.OnMouseMove(mouseX, mouseY);
        }
    }

    public void HandleMouseUp(int mouseX, int mouseY)
    {
        if (_draggingWindow != null)
        {
            _draggingWindow = null;
            return;
        }

        var active = ActiveWindow;
        if (active != null && !active.IsMinimized && !active.IsClosed)
        {
            active.OnMouseUp(mouseX, mouseY);
        }
    }

    public void HandleKeyDown(KeyEvent key)
    {
        var active = ActiveWindow;
        if (active != null && !active.IsMinimized && !active.IsClosed)
        {
            active.OnKeyDown(key);
        }
    }

    public void DrawAll(DisplayDriver display)
    {
        // Desenha as janelas do fundo para a frente
        for (int i = 0; i < _windows.Count; i++)
        {
            var win = _windows[i];
            if (!win.IsMinimized && !win.IsClosed)
            {
                win.Draw(display);
            }
        }
    }
}
