using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace TheFunnyPicturesWeb.Models
{
    public class DetectionPack

    {
        public byte[] Image { get; set; }
        public string Label { get; set; }
        public string Confidence { get; set; }

        public DetectionPack(Image<Rgb24> image, string label, string confidence)
        {
            using (MemoryStream ms = new())
            {
                image.Save(ms, JpegFormat.Instance);
                Image = ms.ToArray();
            }
            Label = label;
            Confidence = confidence;
        }
    }
}
