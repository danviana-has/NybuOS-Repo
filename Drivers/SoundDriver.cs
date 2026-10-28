using System;

namespace NybuOS.Drivers;

/// <summary>
/// Driver de áudio nativo e sintetizador sonoro do NybuOS.
/// Utiliza o subsistema de áudio / PC Speaker do sistema,
/// compatível com hardware real x86_64, QEMU, VMware e VirtualBox.
/// </summary>
public static class SoundDriver
{
    public static bool IsEnabled { get; set; } = true;
    public static int Volume { get; set; } = 80; // 0 - 100

    public static void Initialize()
    {
    }

    /// <summary>
    /// Emite um tom em uma frequência específica (em Hertz).
    /// </summary>
    public static void PlayBeep(int frequency)
    {
        if (!IsEnabled || Volume <= 0 || frequency <= 20)
        {
            return;
        }

        try
        {
            Console.Beep(frequency, 50);
        }
        catch { }
    }

    /// <summary>
    /// Silencia o som.
    /// </summary>
    public static void StopBeep()
    {
    }

    /// <summary>
    /// Toca um breve acorde de inicialização do sistema.
    /// </summary>
    public static void PlayStartupSound()
    {
        if (!IsEnabled) return;

        try
        {
            Console.Beep(523, 100); // C5
            Console.Beep(659, 100); // E5
            Console.Beep(784, 100); // G5
            Console.Beep(1046, 200); // C6
        }
        catch { }
    }

    /// <summary>
    /// Emite um tom por uma duração definida.
    /// </summary>
    public static void PlayFrequency(int freq, int durationMs = 150)
    {
        if (!IsEnabled || freq <= 20) return;

        try
        {
            Console.Beep(freq, durationMs);
        }
        catch { }
    }

    /// <summary>
    /// Emite um bipe de alerta do sistema.
    /// </summary>
    public static void PlayAlert()
    {
        PlayFrequency(880, 100);
    }

    /// <summary>
    /// Toca a nota correspondente ao passo da faixa musical atual.
    /// </summary>
    public static void PlayTrackNote(int trackIndex, int step)
    {
        PlayTrackStep(trackIndex, step);
    }

    /// <summary>
    /// Toca a nota correspondente ao passo da faixa musical atual.
    /// </summary>
    public static void PlayTrackStep(int trackIndex, int step)
    {
        if (!IsEnabled) return;

        int[] notes;
        switch (trackIndex)
        {
            case 0: // Tema Nybu Synthwave
                notes = new int[] { 440, 0, 523, 659, 587, 0, 440, 392, 440, 523, 659, 784, 659, 587, 523, 440 };
                break;
            case 1: // Ode to Joy
                notes = new int[] { 659, 659, 698, 784, 784, 698, 659, 587, 523, 523, 587, 659, 659, 587, 587, 0 };
                break;
            case 2: // Chiptune Pulse
                notes = new int[] { 880, 1046, 1318, 1046, 880, 659, 784, 880, 1175, 1046, 880, 659, 587, 659, 784, 880 };
                break;
            default:
                notes = new int[] { 440, 523, 659, 784 };
                break;
        }

        int note = notes[step % notes.Length];
        if (note > 0)
        {
            PlayBeep(note);
        }
    }
}
