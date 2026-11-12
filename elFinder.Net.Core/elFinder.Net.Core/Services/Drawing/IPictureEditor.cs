using SkiaSharp;

using System.IO;
using System.Threading.Tasks;

namespace elFinder.Net.Core.Services.Drawing {
    public interface IPictureEditor {
        SKColor BackgroundColor { get; set; }

        bool CanProcessFile(string fileExtension);
        Task<ImageWithMimeType> CropAsync(Stream input, int x, int y, int width, int height, int? quality = null);
        Task<ImageWithMimeType> GenerateThumbnailAsync(Stream input, int size, bool keepAspectRatio);
        SKSizeI ImageSize(Stream input);
        SKSizeI ImageSize(string fullPath);
        Task<ImageWithMimeType> RotateAsync(SKBitmap image, SKEncodedImageFormat format, int angle, SKColor? background = null, int? quality = null);
        Task<ImageWithMimeType> RotateAsync(Stream input, int angle, string backgroundHex = null, int? quality = null);
        Task<ImageWithMimeType> ScaleAsync(Stream input, int width, int height, int? quality = null);
    }
}