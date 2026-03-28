using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TranscribeWin.Services;

/// <summary>
/// Global hotkey manager using low-level keyboard hook.
/// Supports both key-down and key-up events for push-to-talk.
/// </summary>
public class HotkeyManager : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;
    private const int VK_LMENU = 0xA4;    // Left Alt
    private const int VK_RMENU = 0xA5;    // Right Alt
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;     // Alt
    private const int VK_SHIFT = 0x10;

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;
    private uint _targetVk;
    private bool _requireCtrl;
    private bool _requireAlt;
    private bool _requireShift;
    private bool _isKeyDown;

    /// <summary>Fires when the hotkey is pressed down.</summary>
    public event Action? HotkeyDown;

    /// <summary>Fires when the hotkey is released.</summary>
    public event Action? HotkeyUp;

    /// <summary>Fires on each press (for toggle mode).</summary>
    public event Action? HotkeyPressed;

    public bool Register(IntPtr windowHandle, string modifiers, string key)
    {
        Unregister();

        _requireCtrl = modifiers.Contains("Ctrl", StringComparison.OrdinalIgnoreCase);
        _requireAlt = modifiers.Contains("Alt", StringComparison.OrdinalIgnoreCase);
        _requireShift = modifiers.Contains("Shift", StringComparison.OrdinalIgnoreCase);
        _targetVk = KeyToVk(key);
        _isKeyDown = false;

        _proc = HookCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(module.ModuleName), 0);

        return _hookId != IntPtr.Zero;
    }

    public void Unregister()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        _isKeyDown = false;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            if ((uint)vkCode == _targetVk)
            {
                bool modifiersOk = CheckModifiers();

                if (modifiersOk)
                {
                    int msg = wParam.ToInt32();

                    if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                    {
                        if (!_isKeyDown)  // Avoid key-repeat
                        {
                            _isKeyDown = true;
                            HotkeyDown?.Invoke();
                            HotkeyPressed?.Invoke();
                        }
                        return (IntPtr)1; // Suppress the key
                    }
                    else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                    {
                        if (_isKeyDown)
                        {
                            _isKeyDown = false;
                            HotkeyUp?.Invoke();
                        }
                        return (IntPtr)1; // Suppress the key
                    }
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool CheckModifiers()
    {
        if (_requireCtrl && !IsKeyPressed(VK_CONTROL)) return false;
        if (_requireAlt && !IsKeyPressed(VK_MENU)) return false;
        if (_requireShift && !IsKeyPressed(VK_SHIFT)) return false;
        return true;
    }

    private static bool IsKeyPressed(int vk)
    {
        return (GetAsyncKeyState(vk) & 0x8000) != 0;
    }

    public static uint KeyToVk(string key)
    {
        return key.ToUpperInvariant() switch
        {
            "SPACE" => 0x20,
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            _ when key.Length == 1 && char.IsLetterOrDigit(key[0]) => (uint)char.ToUpper(key[0]),
            _ => 0x20
        };
    }

    // Keep for backward compatibility — no-op now since we use hook instead of WndProc
    public IntPtr HandleMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
    }
}
