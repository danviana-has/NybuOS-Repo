using System;
using System.Collections.Generic;
using Cosmos.Kernel.System.Input;

namespace NybuOS.Drivers;

/// <summary>
/// Driver do teclado PS/2 com buffer de digitação para a janela/aplicativo em foco.
/// </summary>
public class KeyboardDriver
{
    private readonly Queue<KeyEvent> _keyBuffer = new Queue<KeyEvent>();
    private const int MaxBufferSize = 64;

    public bool HasKeys => _keyBuffer.Count > 0;

    public void Initialize()
    {
    }

    public void Update()
    {
        try
        {
            while (KeyboardManager.KeyAvailable)
            {
                if (KeyboardManager.TryReadKey(out KeyEvent keyEvent))
                {
                    if (_keyBuffer.Count < MaxBufferSize)
                    {
                        _keyBuffer.Enqueue(keyEvent);
                    }
                }
            }
        }
        catch
        {
            // Ignora falhas temporárias na leitura de interrupção
        }
    }

    public bool TryDequeueKey(out KeyEvent keyEvent)
    {
        if (_keyBuffer.Count > 0)
        {
            keyEvent = _keyBuffer.Dequeue();
            return true;
        }

        keyEvent = default;
        return false;
    }
}
