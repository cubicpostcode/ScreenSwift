using System;
using System.Runtime.InteropServices;

namespace ScreenSwift;

internal static class InputSender
{
    private const ushort VkControl = 0x11;
    private const ushort VkV = 0x56;
    private const uint KeyUp = 0x0002;

    public static void Paste()
    {
        var inputs = new[]
        {
            Key(VkControl, 0), Key(VkV, 0), Key(VkV, KeyUp), Key(VkControl, KeyUp)
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT Key(ushort virtualKey, uint flags) => new()
    {
        type = 1,
        U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey, dwFlags = flags } }
    };

    // INPUT must include the largest member of the native union. Without the
    // MOUSEINPUT member, Marshal reports 32 bytes on 64-bit Windows instead of
    // the required 40, and SendInput silently rejects the Ctrl+V sequence.
    [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }
    [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int size);
}
