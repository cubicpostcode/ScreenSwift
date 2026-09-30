using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ScreenSwift;

/// <summary>
/// Global ScreenSwift input. It observes left-button sequences without consuming
/// them, so ordinary Windows left-click behaviour stays intact. Three quick clicks
/// activate a capture mode; a fourth quick click instead requests paste.
/// </summary>
internal sealed class GlobalMouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WhKeyboardLl = 13;
    private const int WmLButtonUp = 0x0202;
    private const int WmKeyDown = 0x0100;
    private const uint VkEscape = 0x1B;
    private const int VkControl = 0x11;
    private const int VkShift = 0x10;
    private const int VkMenu = 0x12;
    private const uint LlmhfInjected = 0x00000001;
    private const uint LlkhfInjected = 0x00000010;
    // Deliberately more forgiving than Windows' double-click defaults: this is an
    // intentional accessibility gesture, not a text-editor triple-click.
    // The third click waits briefly for a fourth click to claim paste. Four clicks
    // paste immediately; only a triple-click pays this 0.75-second decision wait.
    private const int GestureClickMilliseconds = 750;
    private const int GestureDistancePixels = 48;

    private readonly object _gate = new();
    private IntPtr _mouseHookId;
    private IntPtr _keyboardHookId;
    private LowLevelMouseProc? _mouseProcedure;
    private LowLevelKeyboardProc? _keyboardProcedure;
    private Timer? _thirdClickTimer;
    private int _clickCount;
    private POINT _lastClickPoint;
    private uint _lastClickTime;
    private CaptureGesture _pendingTripleGesture;

    public event EventHandler<CaptureGestureEventArgs>? TripleLeftClicked;
    public event EventHandler? TetraLeftClicked;
    public event EventHandler? EscapePressed;
    public Func<bool>? SelectionModeActive { get; set; }
    public bool Enabled { get; set; } = true;

    public void Start()
    {
        _mouseProcedure = MouseCallback;
        _keyboardProcedure = KeyboardCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        var moduleHandle = GetModuleHandle(module.ModuleName);
        _mouseHookId = SetMouseWindowsHookEx(WhMouseLl, _mouseProcedure, moduleHandle, 0);
        _keyboardHookId = SetKeyboardWindowsHookEx(WhKeyboardLl, _keyboardProcedure, moduleHandle, 0);
        if (_mouseHookId == IntPtr.Zero || _keyboardHookId == IntPtr.Zero)
        {
            Dispose();
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private IntPtr MouseCallback(int code, IntPtr message, IntPtr data)
    {
        if (code < 0) return CallNextHookEx(_mouseHookId, code, message, data);
        if (!Enabled) return CallNextHookEx(_mouseHookId, code, message, data);
        var details = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(data);
        if ((details.flags & LlmhfInjected) == 0 && message.ToInt32() == WmLButtonUp)
        {
            OnLeftButtonUp(details);
        }

        // Never suppress left input. A click gesture may therefore also carry out
        // its normal Windows action; invoke it over a neutral area when that matters.
        return CallNextHookEx(_mouseHookId, code, message, data);
    }

    private void OnLeftButtonUp(MSLLHOOKSTRUCT details)
    {
        if (SelectionModeActive?.Invoke() == true) return;

        var tetra = false;
        lock (_gate)
        {
            var near = Math.Abs(details.pt.x - _lastClickPoint.x) <= GestureDistancePixels &&
                       Math.Abs(details.pt.y - _lastClickPoint.y) <= GestureDistancePixels;
            var quick = _clickCount > 0 && near && details.time - _lastClickTime <= GestureClickMilliseconds;
            _clickCount = quick ? _clickCount + 1 : 1;
            _lastClickPoint = details.pt;
            _lastClickTime = details.time;

            if (_clickCount == 3)
            {
                _pendingTripleGesture = GetCaptureGesture();
                _thirdClickTimer ??= new Timer(_ => FirePendingTriple(), null, Timeout.Infinite, Timeout.Infinite);
                _thirdClickTimer.Change(GestureClickMilliseconds, Timeout.Infinite);
            }
            else if (_clickCount == 4)
            {
                _thirdClickTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                ResetClickState();
                tetra = true;
            }
            else if (_clickCount > 4)
            {
                ResetClickState();
            }
        }

        if (tetra) TetraLeftClicked?.Invoke(this, EventArgs.Empty);
    }

    private void FirePendingTriple()
    {
        CaptureGesture gesture;
        lock (_gate)
        {
            if (_clickCount != 3 || SelectionModeActive?.Invoke() == true) return;
            gesture = _pendingTripleGesture;
            ResetClickState();
        }
        TripleLeftClicked?.Invoke(this, new CaptureGestureEventArgs(gesture));
    }

    public void ResetClickState()
    {
        lock (_gate)
        {
            _thirdClickTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _clickCount = 0;
            _lastClickTime = 0;
        }
    }

    private IntPtr KeyboardCallback(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && message.ToInt32() == WmKeyDown)
        {
            var details = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(data);
            if (Enabled && details.vkCode == VkEscape && (details.flags & LlkhfInjected) == 0 && SelectionModeActive?.Invoke() == true)
            {
                EscapePressed?.Invoke(this, EventArgs.Empty);
                return (IntPtr)1;
            }
        }
        return CallNextHookEx(_keyboardHookId, code, message, data);
    }

    public void Dispose()
    {
        _thirdClickTimer?.Dispose();
        if (_mouseHookId != IntPtr.Zero) UnhookWindowsHookEx(_mouseHookId);
        if (_keyboardHookId != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHookId);
        _mouseHookId = IntPtr.Zero;
        _keyboardHookId = IntPtr.Zero;
    }

    private static CaptureGesture GetCaptureGesture()
    {
        var control = IsKeyDown(VkControl);
        var shift = IsKeyDown(VkShift);
        var alt = IsKeyDown(VkMenu);
        if (control && shift && alt) return CaptureGesture.Polygon;
        if (control && shift) return CaptureGesture.Freeform;
        if (control) return CaptureGesture.Rectangle;
        return CaptureGesture.FullScreen;
    }

    private static bool IsKeyDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x; public int y; }
    [StructLayout(LayoutKind.Sequential)] private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr dwExtraInfo; }
    [DllImport("user32.dll", EntryPoint = "SetWindowsHookEx", SetLastError = true)] private static extern IntPtr SetMouseWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hmod, uint threadId);
    [DllImport("user32.dll", EntryPoint = "SetWindowsHookEx", SetLastError = true)] private static extern IntPtr SetKeyboardWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hmod, uint threadId);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string? lpModuleName);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int virtualKey);
}

internal enum CaptureGesture
{
    FullScreen,
    Rectangle,
    Freeform,
    Polygon
}

internal sealed class CaptureGestureEventArgs : EventArgs
{
    public CaptureGestureEventArgs(CaptureGesture gesture) => Gesture = gesture;
    public CaptureGesture Gesture { get; }
}
