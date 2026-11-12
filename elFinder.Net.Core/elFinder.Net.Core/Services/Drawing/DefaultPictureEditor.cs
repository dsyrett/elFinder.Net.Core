using SkiaSharp;

using System;
using System.IO;
using System.Threading.Tasks;

namespace elFinder.Net.Core.Services.Drawing {
    /// <summary>
    /// Represents default pictures editor
    /// </summary>
    public class DefaultPictureEditor : IPictureEditor {
        public DefaultPictureEditor(SKColor backgroundColor) {
            BackgroundColor = backgroundColor;
        }

        public DefaultPictureEditor()
            : this(SKColors.Transparent) {
        }

        public virtual SKColor BackgroundColor { get; set; }

        public virtual bool CanProcessFile(string fileExtension) {
            string ext = fileExtension.ToLower();

            return ext == ".png"
                || ext == ".jpg"
                || ext == ".jpeg"
                || ext == ".gif"
                || ext == ".tiff"
                || ext == ".bmp"
                || ext == ".pbm"
                || ext == ".tga"
                || ext == ".webp";
        }

        public virtual Task<ImageWithMimeType> CropAsync(Stream input, int x, int y, int width, int height, int? quality = null) {
            // Buffer once so the codec and the decoder don't compete for a forward-only stream
            using SKData data = SKData.Create(input);
            using SKCodec codec = SKCodec.Create(data);
            if (codec == null) return Task.FromResult<ImageWithMimeType>(null);

            using SKBitmap source = SKBitmap.Decode(codec);
            if (source == null) return Task.FromResult<ImageWithMimeType>(null);

            SKBitmap output = new(width, height);
            if (source.ExtractSubset(output, new SKRectI(x, y, x + width, y + height))) {
                using SKImage image = SKImage.FromBitmap(output);
                return SaveImageAsync(image, codec.EncodedFormat);
            }
            throw new System.Exception("Failed to crop image.");
        }

        public virtual Task<ImageWithMimeType> ScaleAsync(Stream input, int width, int height, int? quality = null) {
            // Buffer once so the codec and the decoder don't compete for a forward-only stream
            using SKData data = SKData.Create(input);
            using SKCodec codec = SKCodec.Create(data);
            if (codec == null) return Task.FromResult<ImageWithMimeType>(null);

            using SKBitmap source = SKBitmap.Decode(codec);
            if (source == null) return Task.FromResult<ImageWithMimeType>(null);

            using SKImage image = SKImage.FromBitmap(source);
            return ScaleAsync(image, codec.EncodedFormat, width, height, quality);
        }
        protected virtual Task<ImageWithMimeType> ScaleAsync(SKImage image, SKEncodedImageFormat format, int width, int height, int? quality = null) {
            SKCanvas canvas = new(SKBitmap.FromImage(image));
            canvas.Scale(width / (float)image.Width, height / (float)image.Height);

            return SaveImageAsync(image, format, quality);
        }

        public virtual Task<ImageWithMimeType> GenerateThumbnailAsync(Stream input, int size, bool keepAspectRatio) {
            // Buffer once so the codec and the decoder don't compete for a forward-only stream
            using SKData data = SKData.Create(input);
            using SKCodec codec = SKCodec.Create(data);
            if (codec == null) return Task.FromResult<ImageWithMimeType>(null);

            using SKBitmap source = SKBitmap.Decode(codec);
            if (source == null) return Task.FromResult<ImageWithMimeType>(null);

            int targetWidth = size, targetHeight = size;
            if (keepAspectRatio) {
                double scale = Math.Min((double)size / source.Width, (double)size / source.Height);
                targetWidth = Math.Max(1, (int)(source.Width * scale));
                targetHeight = Math.Max(1, (int)(source.Height * scale));
            }

            using SKBitmap resized = source.Resize(
                new SKImageInfo(targetWidth, targetHeight, source.ColorType, source.AlphaType),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            if (resized == null) return Task.FromResult<ImageWithMimeType>(null);

            // Skia can't encode GIF/BMP, so fall back to PNG for those
            var format = codec.EncodedFormat is SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp
                ? codec.EncodedFormat
                : SKEncodedImageFormat.Png;

            using SKImage image = SKImage.FromBitmap(resized);
            return SaveImageAsync(image, format);
        }

        public virtual Task<ImageWithMimeType> GenerateThumbnailAsyncOLD(Stream input, int size, bool keepAspectRatio) {
            SKImageInfo info = new();
            Console.WriteLine($"GenerateThumbnailAsync:bytes");
            using SKData data = SKData.Create(input);
            using SKCodec codec = SKCodec.Create(data);
            Console.WriteLine($"GenerateThumbnailAsync: data created");
            using SKBitmap source = SKBitmap.Decode(data, info);
            Console.WriteLine($"GenerateThumbnailAsync: source decoded");

            Console.WriteLine($"GenerateThumbnailAsync: source.Width={source.Width}, source.Height={info.Height}, Codec={(codec != null ? codec.EncodedFormat : "UNKNOWN")}");
            //Console.WriteLine($"GenerateThumbnailAsync: {input.Length} bytes");
            using SKImage image = SKImage.FromBitmap(source);

            //using var image = SKImage.FromEncodedData(input);
            Console.WriteLine($"stream length after : {input.Length} bytes");

            int targetWidth;
            int targetHeight;

            if (keepAspectRatio) {
                double width = info.Width;
                double height = info.Height;
                double percentWidth = width != 0 ? size / width : 0;
                double percentHeight = height != 0 ? size / height : 0;
                double percent = percentHeight < percentWidth ? percentHeight : percentWidth;

                targetWidth = (int)(width * percent);
                targetHeight = (int)(height * percent);
            } else {
                targetWidth = size;
                targetHeight = size;
            }

            Console.WriteLine($"GenerateThumbnailAsync: targetWidth={targetWidth}, targetHeight={targetHeight}, Codec={codec.EncodedFormat}");

            return ScaleAsync(image, codec.EncodedFormat, targetWidth, targetHeight);
        }

        public virtual SKSizeI ImageSize(Stream input) {
            SKImageInfo info = new();
            using SKBitmap source = SKBitmap.Decode(input, info);
            return info.Size;
        }

        public virtual SKSizeI ImageSize(string fullPath) {
            SKImageInfo info = new();
            using SKBitmap source = SKBitmap.Decode(fullPath, info);
            return info.Size;
        }

        public virtual Task<ImageWithMimeType> RotateAsync(SKBitmap image, SKEncodedImageFormat format, int angle, SKColor? background = null, int? quality = null) {
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            // Calculate the new dimensions of the rotated bitmap
            double radians = Math.PI * angle / 180;
            float sine = (float)Math.Abs(Math.Sin(radians));
            float cosine = (float)Math.Abs(Math.Cos(radians));
            int rotatedWidth = (int)(cosine * originalWidth + sine * originalHeight);
            int rotatedHeight = (int)(cosine * originalHeight + sine * originalWidth);

            // Create a new bitmap with the rotated dimensions
            var rotatedBitmap = new SKBitmap(rotatedWidth, rotatedHeight);

            using (var surface = new SKCanvas(rotatedBitmap)) {
                surface.Clear(background ?? BackgroundColor);
                surface.Translate(rotatedWidth / 2, rotatedHeight / 2);
                surface.RotateDegrees(angle);
                surface.Translate(-originalWidth / 2, -originalHeight / 2);
                surface.DrawBitmap(image, new SKPoint(), new SKSamplingOptions());
            }


            return SaveImageAsync(SKImage.FromBitmap(rotatedBitmap), format, quality);
        }

        public virtual Task<ImageWithMimeType> RotateAsync(Stream input, int angle, string backgroundHex = null, int? quality = null) {
            SKColor bgColor = BackgroundColor;
            // Buffer once so the codec and the decoder don't compete for a forward-only stream
            using SKData data = SKData.Create(input);
            using SKCodec codec = SKCodec.Create(data);
            if (codec == null) return Task.FromResult<ImageWithMimeType>(null);

            using SKBitmap image = SKBitmap.Decode(codec);
            if (image == null) return Task.FromResult<ImageWithMimeType>(null);

            if (!string.IsNullOrEmpty(backgroundHex)) {
                bgColor = SKColor.Parse($"{backgroundHex[1..]}FF");
            }

            return RotateAsync(image, codec.EncodedFormat, angle, bgColor, quality);
        }

        protected virtual async Task<ImageWithMimeType> SaveImageAsync(SKImage image, SKEncodedImageFormat format, int? quality = null) {
            using SKData data = image.Encode(format, quality ?? 100);

            var memoryStream = new MemoryStream();
            data.SaveTo(memoryStream);
            memoryStream.Position = 0;

            return new ImageWithMimeType($"image/{format.ToString().ToLower()}" ?? "image/png", memoryStream);
        }

        //protected virtual async Task<ImageWithMimeType> ChangeQualityAsync(Image image, IImageFormat format, int? quality = null)
        //{
        //    var memoryStream = new MemoryStream();
        //    IImageEncoder imageEncoder;
        //    string mimeType = JpegFormat.Instance.DefaultMimeType;
        //    imageEncoder = new JpegEncoder
        //    {
        //        Quality = quality,
        //    };

        //    await image.SaveAsync(memoryStream, imageEncoder);
        //    memoryStream.Position = 0;

        //    return new ImageWithMimeType(mimeType, memoryStream);
        //}
    }
}
