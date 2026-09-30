using System.Drawing;
using System.Drawing.Imaging;
using Forms = System.Windows.Forms;

namespace ScreenSwift;

internal static class ScreenCapture
{
    public static Bitmap CaptureVirtualScreen()
    {
        var area = Forms.SystemInformation.VirtualScreen;
        var result = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(result);
        graphics.CopyFromScreen(area.Left, area.Top, 0, 0, result.Size, CopyPixelOperation.SourceCopy);
        return result;
    }
}
