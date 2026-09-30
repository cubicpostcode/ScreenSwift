# ScreenSwift Microsoft Store package

This folder contains the MSIX packaging project for Microsoft Store submission.
It deliberately stays separate from the Inno Setup installer, which remains the
direct-download option on GitHub.

## First-time Store setup

1. Enrol in the [Microsoft Partner Center](https://partner.microsoft.com/dashboard).
2. Reserve the app name **ScreenSwift**.
3. Open `ScreenSwift.sln` in Visual Studio on Windows.
4. Right-click the `ScreenSwift.Store` project and choose **Associate App with the Store**.
5. Select the reserved ScreenSwift listing. Visual Studio updates the package
   identity with the exact identity assigned by Microsoft.
6. Select `Release` and `x64`, then use **Publish → Create App Packages**.
   Choose Microsoft Store distribution and create the `.msixupload` file.

Do not submit a generic package with the `CN=SCREEN_SWIFT_STORE_IDENTITY_REQUIRED`
placeholder. It is intentionally not a real publisher identity.

## Store listing notes

- Category: Productivity
- Licence: MIT
- Pricing: Free
- Privacy statement: ScreenSwift has no telemetry, advertising, accounts, cloud
  service, or image uploads. Captures exist only in memory and the Windows clipboard.
- Desktop capability: the package declares `runFullTrust` for the existing Win32
  global-input, clipboard, notification-area, and screen-capture implementation.

Test the locally installed MSIX before submission, particularly the tray icon,
global left-click gestures, selection overlays, clipboard output, and paste gesture.
