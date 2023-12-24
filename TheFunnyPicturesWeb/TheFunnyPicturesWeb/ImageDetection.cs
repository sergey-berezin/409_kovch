using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using yolo_logic;
using SixLabors.ImageSharp.Drawing.Processing;

namespace TheFunnyPicturesWeb
{
    public record DetectedObject(Image<Rgb24> Original, string Class, double Confidence, Image<Rgb24> Detected_object, ObjectBox Box);
    public class ImageDetection
    {
        private const int TargetSize = 416;
        private static readonly ResizeOptions resizeOptions = new()
        {

            Size = new Size(TargetSize, TargetSize),
            Mode = ResizeMode.Pad
        };
        public static async Task<IEnumerable<DetectedObject>> DetectImage(string filename, CancellationToken token)
        {
            var original = Image.Load<Rgb24>(filename);
            var detectionTask = Yolo_logic.ProcessImageAsync(original, token);
            List<DetectedObject> imageSegmentation = [];

            await detectionTask;
            var boxes = detectionTask.Result.Box;
            var extractedImage = await Task.WhenAll(boxes.Select(box =>
                Task.Run(() =>
                ExtractDetectedObject(original, box), token)
            ));
            return extractedImage.Zip(boxes).Select(arg =>
                new DetectedObject(
                    original,
                    Yolo_logic.Labels[arg.Second.Class],
                    Math.Round(arg.Second.Confidence * 100, 2),
                    arg.First,
                    arg.Second)
                );
        }

        public static Image<Rgb24> ExtractDetectedObject(Image<Rgb24> original, ObjectBox box)
        {
            int x = (int)box.XMin;
            int y = (int)box.YMin;
            int width = (int)(box.XMax - box.XMin);
            int height = (int)(box.YMax - box.YMin);
            if (x < 0)
            {
                width += x;
                x = 0;
            }
            if (y < 0)
            {
                height += y;
                y = 0;
            }
            if (x + width > TargetSize)
            {
                width = TargetSize - x;
            }
            if (y + height > TargetSize)
            {
                height = TargetSize - y;
            }
            if (x > TargetSize || y > TargetSize)
            {
                return original.Clone(img => img.Resize(resizeOptions));
            }
            return original.Clone(
                img => img.Resize(resizeOptions).Crop(new Rectangle(x, y, width, height))
            );
        }

        public static Image<Rgb24> Annotate(Image<Rgb24> target, ObjectBox box)
        {
            int maxDimension = Math.Max(target.Width, target.Height);
            float scale = (float)maxDimension / TargetSize;
            return target.Clone(ctx =>
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxDimension, maxDimension),
                    Mode = ResizeMode.Pad
                }).DrawPolygon(Pens.Solid(Color.GreenYellow, 1 + maxDimension / TargetSize),
                    [
                        new((float)box.XMin * scale, (float)box.YMin * scale),
                        new((float)box.XMin * scale, (float)box.YMax * scale),
                        new((float)box.XMax * scale, (float)box.YMax * scale),
                        new((float)box.XMax * scale, (float)box.YMin * scale),
                    ]);
            });
        }
    }
}
