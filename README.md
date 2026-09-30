# ScreenSwift

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

ScreenSwift is a small installed Windows utility for moving screenshots into ChatGPT with minimal friction. Its blue ScreenSwift icon appears in Start, desktop shortcuts, Installed apps, the installer, and the notification area. It runs in the notification area after sign-in and keeps the most recent capture on the Windows clipboard.

## Gestures

| Action | Result |
| --- | --- |
| Three quick left-clicks | Captures all screens to an upload-optimised JPEG clipboard image. |
| `Ctrl` + three quick left-clicks | Captures all screens and opens rectangle-selection mode. Drag to select; the result is JPEG. |
| `Ctrl+Shift` + three quick left-clicks | Captures all screens and opens freehand/lasso-selection mode. Drag to select; transparent surroundings are retained as PNG. |
| `Ctrl+Alt+Shift` + three quick left-clicks | Captures all screens and opens polygon-selection mode. Click vertices, then click the first vertex again, double-click the final vertex, or press `Enter`; transparent surroundings are retained as PNG. |
| Four quick left-clicks | Pastes the current clipboard image into the focused application. |
| `Esc` during selection | Cancels the selection and retains the initial full-screen clipboard capture. |

ScreenSwift remains running quietly in the notification area. Its tray menu can **Enable/Disable ScreenSwift**, capture/select, paste the last capture, or exit the application completely. Use each gesture as one natural, quick sequence, with no pause longer than roughly 0.75 seconds between clicks; slight pointer movement is allowed. A recognised fourth click pastes immediately. A three-click capture waits 0.75 seconds after its final click, solely to let a possible fourth click claim the paste action. ScreenSwift does not intercept the right mouse button at all, so normal Windows context menus work normally. The left-click gestures are observed rather than blocked, so their clicks retain ordinary Windows behaviour too; where that would be undesirable, use a neutral surface such as an empty desktop area.

## Installation

ScreenSwift installs as a normal Windows application—no loose executable needs to be kept on the desktop. It appears in Start and Installed apps with its custom icon, and the installer offers to run it automatically when you sign in.

On the development PC, install **Inno Setup 6** once. Then double-click `Build-Installer.cmd` in the project root. It publishes a self-contained Release build and creates:

`Installer\Output\ScreenSwift-Setup.exe`

Run that Setup file to install ScreenSwift. It includes an uninstaller in **Settings → Apps → Installed apps**. Alternatively, open `ScreenSwift.sln` in Visual Studio 2026 and build **Release**, then use the following publish command:

```powershell
dotnet publish .\ScreenSwift\ScreenSwift.csproj -c Release -r win-x64 --self-contained true
```

Compile `Installer\ScreenSwift.iss` with Inno Setup 6. The resulting `ScreenSwift-Setup.exe` is a normal installer: it adds ScreenSwift to the Start menu and Installed apps, creates an uninstaller, and can optionally add a desktop shortcut and start ScreenSwift when you sign in.

## Microsoft Store edition

The repository also contains a separate MSIX packaging project in
`Store\ScreenSwift.Store.wapproj`. This is the Store edition; it does not replace
the GitHub/Inno Setup installer. Microsoft signs the MSIX package it distributes,
so Store customers see Microsoft as the trusted distributor rather than an
unknown publisher warning.

Before building the Store package, reserve **ScreenSwift** in Microsoft Partner
Center and use Visual Studio's **Associate App with the Store** command on the
`ScreenSwift.Store` project. This replaces the placeholder identity in
`Store\Package.appxmanifest` with the identity assigned to the Store listing.
Build the `Release|x64` configuration in Visual Studio on Windows to create the
uploadable `.msixupload` submission file. The Store listing should state clearly
that ScreenSwift captures only when invoked, keeps images only in memory/the
Windows clipboard, and has no telemetry, advertising, accounts, or uploads.

The app is packaged as a full-trust desktop app because it uses standard Win32
desktop facilities for global input gestures, the clipboard, the notification
area, and screen capture. Test the MSIX locally before submitting it, then use
Partner Center's certification feedback as the final authority on Store approval.

## Notes

The source intentionally avoids recording or saving screenshots to disk. Images remain in memory and on the normal Windows clipboard. A screenshot can still be pasted manually with `Ctrl+V` at any time.

## Licence

ScreenSwift is free software released under the [MIT Licence](LICENSE). It permits use, modification, redistribution, and commercial use, provided the copyright and licence notice are retained.
