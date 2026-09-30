// SPDX-License-Identifier: MIT

using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using Forms = System.Windows.Forms;

namespace ScreenSwift;

/*
PROJECT REBUILD PROMPT — keep this block at the top of the main application file.

Create a clean, modern, Windows 11 C# free-software project named ScreenSwift.
It is a lightweight, privacy-respecting screenshot-to-clipboard utility for fast
sharing with ChatGPT. It must run unobtrusively from the notification area and
never save captures to disk unless a future user-facing option explicitly does so.

Core interaction: use global left-click gestures, never the right mouse button.
Three quick left-clicks capture the virtual desktop and put an upload-optimised JPEG
on the clipboard. Ctrl+three quick left-clicks opens a
rectangle-selection overlay; Ctrl+Shift+three opens freehand/lasso selection; and
Ctrl+Alt+Shift+three opens polygon selection. Four quick left-clicks paste the
current clipboard image into the focused app. Completing any selection copies its
result and returns ScreenSwift to its idle tray-resident state. ScreenSwift stays
resident until explicitly exited from its notification-area menu. That menu enables
or disables all gestures. Escape only cancels an active selection and keeps the
clipboard intact; otherwise it must remain available to Windows and other apps. The
first three clicks are deliberately left untouched so ordinary
Windows left-click behaviour remains available; users should invoke capture gestures
over a neutral area where that normal click action does not matter.

Engineering standards: use clean, explicit namespaces; keep input-state logic
deterministic and testable; avoid dependencies where Win32/WPF suffice; target
.NET 8 on Windows; provide an installer definition, README, .gitignore, and a
clear MIT licence. The Inno Setup installer must create Start-menu and Installed-apps
entries, offer a desktop shortcut, offer launch at sign-in, and uninstall cleanly. Use
the bundled ScreenSwift bird-and-screenshot icon consistently for the executable,
notification area, shortcuts, installer, and installed-apps entry. Publish as an MIT
licensed GitHub repository with an automated Windows installer release on version tags.
Make source comments explain non-obvious Windows-hook behaviour.
The repository must be suitable for public GitHub release: no credentials,
telemetry, advertising, hidden background collection, or proprietary restrictions.

Distribution: preserve the normal Inno Setup installer for GitHub releases and
maintain a separate Microsoft Store MSIX packaging project. The Store package must
use the same icon and privacy commitments, declare full-trust desktop execution
only where necessary for the Win32 hook/clipboard/tray/capture design, and be
associated with the Publisher identity assigned by Microsoft Partner Center before
submission. Never hard-code a personal or certificate identity in public source.
*/

public partial class App : Application
{
    private GlobalMouseHook? _mouseHook;
    private Forms.NotifyIcon? _trayIcon;
    private Icon? _applicationIcon;
    private Forms.ToolStripMenuItem? _toggleItem;
    private SelectionWindow? _selection;
    private Bitmap? _fullScreen;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _mouseHook = new GlobalMouseHook();
        _mouseHook.SelectionModeActive = () => _selection is not null;
        _mouseHook.TripleLeftClicked += (_, args) => Dispatcher.Invoke(() => OnTripleLeftClick(args.Gesture));
        _mouseHook.TetraLeftClicked += (_, _) => Dispatcher.Invoke(PasteClipboard);
        _mouseHook.EscapePressed += (_, _) => Dispatcher.BeginInvoke(new Action(CancelSelection));
        _mouseHook.Start();

        var menu = new Forms.ContextMenuStrip();
        _toggleItem = new Forms.ToolStripMenuItem("Disable ScreenSwift");
        _toggleItem.Click += (_, _) => ToggleEnabled();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Capture and select", null, (_, _) => CaptureAndSelect());
        menu.Items.Add("Paste last capture", null, (_, _) => PasteLastCapture());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit ScreenSwift", null, (_, _) => Shutdown());
        _applicationIcon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "ScreenSwift.ico"));
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _applicationIcon,
            Text = "ScreenSwift",
            Visible = true,
            ContextMenuStrip = menu
        };
        _trayIcon.DoubleClick += (_, _) => CaptureAndSelect(CaptureGesture.Rectangle);
    }

    private void OnTripleLeftClick(CaptureGesture gesture)
    {
        if (gesture == CaptureGesture.FullScreen) CaptureFullScreen();
        else CaptureAndSelect(gesture);
    }

    private void CaptureAndSelect(CaptureGesture gesture = CaptureGesture.Rectangle)
    {
        if (_selection is not null) return;
        _fullScreen?.Dispose();
        _fullScreen = ScreenCapture.CaptureVirtualScreen();
        ClipboardImage.SetUploadJpeg(_fullScreen);
        _selection = new SelectionWindow(_fullScreen, gesture);
        _selection.SelectionCompleted += (_, capture) =>
        {
            if (capture.IsTransparent) ClipboardImage.SetTransparentPng(capture.Image);
            else ClipboardImage.SetUploadJpeg(capture.Image);
            capture.Image.Dispose();
        };
        _selection.Closed += (_, _) =>
        {
            _selection = null;
            _mouseHook?.ResetClickState();
        };
        _selection.Show();
        _selection.Activate();
    }

    private void CaptureFullScreen()
    {
        _fullScreen?.Dispose();
        _fullScreen = ScreenCapture.CaptureVirtualScreen();
        ClipboardImage.SetUploadJpeg(_fullScreen);
    }

    private static void PasteLastCapture()
    {
        // Give the click which closed the overlay time to finish before Ctrl+V is injected.
        _ = Task.Run(async () =>
        {
            await Task.Delay(110);
            InputSender.Paste();
        });
    }

    private async void PasteClipboard()
    {
        // Give the destination application time to process the fourth click before Ctrl+V.
        await Task.Delay(110);
        InputSender.Paste();
    }

    private void CancelSelection()
    {
        // The initial full-screen capture is already on the clipboard, so cancellation preserves it.
        _selection?.Close();
    }

    private void ToggleEnabled()
    {
        if (_mouseHook is null || _toggleItem is null || _trayIcon is null) return;
        _mouseHook.Enabled = !_mouseHook.Enabled;
        _toggleItem.Text = _mouseHook.Enabled ? "Disable ScreenSwift" : "Enable ScreenSwift";
        _trayIcon.Text = _mouseHook.Enabled ? "ScreenSwift" : "ScreenSwift (disabled)";
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mouseHook?.Dispose();
        _trayIcon?.Dispose();
        _applicationIcon?.Dispose();
        _fullScreen?.Dispose();
        base.OnExit(e);
    }
}
