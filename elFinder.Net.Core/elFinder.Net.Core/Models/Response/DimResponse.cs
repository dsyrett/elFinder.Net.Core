using SkiaSharp;

namespace elFinder.Net.Core.Models.Response {
    public class DimResponse {
        public DimResponse() {
        }

        public DimResponse(SKSizeI size) {
            dim = $"{size.Width}x{size.Height}";
        }

        public string dim { get; set; }
    }
}
