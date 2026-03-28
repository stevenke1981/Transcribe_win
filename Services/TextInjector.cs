using System.Runtime.InteropServices;
using WindowsInput;

namespace TranscribeWin.Services;

public static class TextInjector
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private static IntPtr _previousWindow = IntPtr.Zero;
    private static readonly InputSimulator _simulator = new InputSimulator();

    public static void SaveFocusedWindow()
    {
        _previousWindow = GetForegroundWindow();
    }

    public static bool RestoreAndType(string text, bool autoEnterOnPunctuation = false)
    {
        if (_previousWindow == IntPtr.Zero || string.IsNullOrEmpty(text))
            return false;

        // Attach to the target window's thread to set focus properly
        uint targetThread = GetWindowThreadProcessId(_previousWindow, out _);
        uint currentThread = GetCurrentThreadId();

        if (targetThread != currentThread)
            AttachThreadInput(currentThread, targetThread, true);

        SetForegroundWindow(_previousWindow);
        Thread.Sleep(50); // Brief pause for window to come to front

        if (targetThread != currentThread)
            AttachThreadInput(currentThread, targetThread, false);

        // Type the text using InputSimulator
        _simulator.Keyboard.TextEntry(text);

        // Auto Enter on punctuation
        if (autoEnterOnPunctuation && text.Length > 0)
        {
            char lastChar = text[^1];
            if (lastChar == '。' || lastChar == '！' || lastChar == '？' ||
                lastChar == '.' || lastChar == '!' || lastChar == '?')
            {
                _simulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
            }
        }

        return true;
    }
}
