using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenSwift;

internal static class ClipboardImage
{
    private const long UploadJpegQuality = 82;

    public static void SetUploadJpeg(Bitmap bitmap) => Set(bitmap, EncodeJpeg(bitmap), "JPEG");

    public static void SetTransparentPng(Bitmap bitmap) => Set(bitmap, EncodePng(bitmap), "PNG");

    private static void Set(Bitmap bitmap, byte[] encodedBytes, string nativeFormat)
    {
        using var stream = new MemoryStream(encodedBytes);
        var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        frame.Freeze();

        // DIB/Bitmap makes pasting work broadly. Chromium expects the native PNG
        // clipboard format as a stream rather than a byte array; using a stream is
        // what lets transparent polygon/freehand captures upload successfully.
        var data = new DataObject();
        data.SetData(DataFormats.Bitmap, frame);
        data.SetData(nativeFormat, new MemoryStream(encodedBytes, writable: false), false);
        Clipboard.SetDataObject(data, true);
    }

    private static byte[] EncodeJpeg(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, UploadJpegQuality);
        bitmap.Save(stream, codec, parameters);
        return stream.ToArray();
    }

    private static byte[] EncodePng(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
