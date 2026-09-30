# Changelog

## Unreleased

- Added the ScreenSwift bird-and-screenshot application icon to the executable, notification area, Start menu, desktop shortcut, installer, and Installed apps entry.
- Added a GitHub Actions release workflow that builds the Windows installer and publishes it for version tags.
- Added the project rebuild prompt and MIT licence for a public GitHub release.
- Reworked ScreenSwift into a tray-resident utility: it remains active after capture, selection, and paste; the tray menu enables/disables gestures and provides explicit exit.
- Escape now cancels only an active selection, leaving ordinary Escape behaviour unchanged elsewhere.
- Replaced all right-button gestures with: three quick left-clicks for full-screen capture, Ctrl+three for rectangle selection, Ctrl+Shift+three for freehand selection, Ctrl+Alt+Shift+three for polygon selection, and four quick left-clicks to paste.
- Right-clicks are no longer intercepted, so Windows context menus always behave normally.
- Tuned the third-click wait to 0.75 seconds: four-click paste is immediate while triple-click capture remains responsive.
- Completed rectangle, freehand, and polygon selections now copy their result and return to the idle tray state.
- Added optimised JPEG clipboard output for full-screen and rectangular captures; transparent freeform/polygon captures use PNG.
- Removed selection-overlay instructions for a clean capture surface.
- Kept the global Escape exit, clipboard-preservation behaviour, and JPEG/PNG capture formats unchanged.
