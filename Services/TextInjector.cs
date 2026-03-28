using System.Runtime.InteropServices;

namespace TranscribeWin.Services;

public static class TextInjector
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_RETURN = 0x0D;

    private static IntPtr _previousWindow = IntPtr.Zero;

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

        // Send each character using Unicode SendInput
        SendUnicodeString(text);

        // Auto Enter on punctuation
        if (autoEnterOnPunctuation && text.Length > 0)
        {
            char lastChar = text[^1];
            if (lastChar == '。' || lastChar == '！' || lastChar == '？' ||
                lastChar == '.' || lastChar == '!' || lastChar == '?')
            {
                SendEnterKey();
            }
        }

        return true;
    }

    private static void SendUnicodeString(string text)
    {
        var inputs = new List<INPUT>();

        foreach (char c in text)
        {
            // Key down
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            });

            // Key up
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            });
        }

        var inputArray = inputs.ToArray();
        SendInput((uint)inputArray.Length, inputArray, Marshal.SizeOf<INPUT>());
    }

    private static void SendEnterKey()
    {
        var inputs = new INPUT[2];

        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_RETURN,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_RETURN,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }
}
