using SixLabors.ImageSharp.Drawing.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using SixLabors.Fonts;
using Microsoft.Extensions.Logging;
namespace yolo_logic
{
    public static class Yolo_logic
    {
        public static ILogger? Logger = null;
        private const string URL = "https://storage.yandexcloud.net/dotnet4/tinyyolov2-8.onnx";
        private static InferenceSession? YoloSession = null;
        private static readonly SemaphoreSlim Semaphore = new(1, 1);
        private const int ClassCount = 20;
        private const int TargetSize = 416;
        private const int CellCount = 13;
        private const int CellSize = TargetSize / CellCount;
        private const int BoxCount = 5;
        public static readonly string[] Labels = new string[] 
        {
            "aeroplane", "bicycle", "bird", "boat", "bottle",
            "bus", "car", "cat", "chair", "cow",
            "diningtable", "dog", "horse", "motorbike", "person",
            "pottedplant", "sheep", "sofa", "train", "tvmonitor"
        };
        private static readonly (double, double)[] Anchors = new (double, double)[]
        {
            (1.08, 1.19), 
            (3.42, 4.41), 
            (6.63, 11.38), 
            (9.42, 5.11), 
            (16.62, 10.52)
        };

        private static Image<Rgb24> ImageResize(Image<Rgb24> image) 
        {
            var resized = image.Clone(x =>
            {
                x.Resize(new ResizeOptions
                {
                    Size = new Size(TargetSize, TargetSize),
                    Mode = ResizeMode.Pad
                });
            });
            return resized;
        }

        private static List<NamedOnnxValue> Image2Tensor(Image<Rgb24> image) 
        {
            var input = new DenseTensor<float>(new[] { 1, 3, TargetSize, TargetSize });
            image.ProcessPixelRows(pa => 
            {
                for (int y = 0; y < TargetSize; y++)
                {           
                    Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                    for (int x = 0; x < TargetSize; x++)
                    {
                        input[0, 0, y, x] = pixelSpan[x].R;
                        input[0, 1, y, x] = pixelSpan[x].G;
                        input[0, 2, y, x] = pixelSpan[x].B;
                    }
                }
            });
            return new List<NamedOnnxValue>  
            { 
               NamedOnnxValue.CreateFromTensor("image", input),
            };
        }
        private static async Task DownloadYolo(CancellationToken token) {
            Logger?.Log("Por que es malo y lento.");
            using var client = new HttpClient();
            using var data = await client.GetStreamAsync(URL, token);
            using var fs = new FileStream("model.onnx", FileMode.OpenOrCreate);
            await data.CopyToAsync(fs, token);
            Logger?.Log("LLuvia en Moscu es grande y poco degradable.");
        }

        private static async Task InitYolo(CancellationToken token) {
            Semaphore.Wait(token);
            bool ready = false;
            while (YoloSession == null) {
                try {
                    YoloSession = new InferenceSession("model.onnx");
                    ready = YoloSession != null;
                }
                catch (Exception exc) {
                    Logger?.Log(exc.Message);
                }
                if (!ready){
                    await DownloadYolo(token);
                }
            }
            Semaphore.Release();
        }
        private static Tensor<float> YoloMagic(List<NamedOnnxValue> inputs)
        {
            if (YoloSession == null) {
                throw new Exception("Yolo session is null.");
            } 
            var results = YoloSession.Run(inputs);
            return results[0].AsTensor<float>();
        }

        private static List<ObjectBox> CalculateBoxes(Tensor<float> outputs)
        {
            List<ObjectBox> objects = new();
            for (var row = 0; row < CellCount; row++)
            {
                for (var col = 0; col < CellCount; col++)
                {
                    for (var box = 0; box < BoxCount; box++)
                    {
                        var rawX = outputs[0, (5 + ClassCount) * box, row, col];
                        var rawY = outputs[0, (5 + ClassCount) * box + 1, row, col];

                        var rawW = outputs[0, (5 + ClassCount) * box + 2, row, col];
                        var rawH = outputs[0, (5 + ClassCount) * box + 3, row, col];

                        var x = (float)((col + Sigmoid(rawX)) * CellSize);
                        var y = (float)((row + Sigmoid(rawY)) * CellSize);

                        var w = (float)(Math.Exp(rawW) * Anchors[box].Item1 * CellSize);
                        var h = (float)(Math.Exp(rawH) * Anchors[box].Item2 * CellSize); 

                        var conf = Sigmoid(outputs[0, (5 + ClassCount) * box + 4, row, col]);

                        if (conf > 0.5)
                        {
                            var classes 
                            = Enumerable
                            .Range(0, ClassCount)
                            .Select(i => outputs[0, (5 + ClassCount) * box + 5 + i, row, col])
                            .ToArray();
                            objects.Add(new ObjectBox(x - w / 2, y - h / 2, x + w / 2, y + h / 2, conf, IndexOfMax(Softmax(classes))));
                        }
                    }
                }
            }
            return objects;
        }

        private static void RemoveDuplicateBoxes(List<ObjectBox> objects)
        {
            for (int i = 0; i < objects.Count; ++i)
            {
                var o1 = objects[i];
                for (int j = i + 1; j < objects.Count;)
                {
                    var o2 = objects[j];
                    if (o1.Class == o2.Class && o1.IoU(o2) > 0.6) 
                    {
                        if(o1.Confidence < o2.Confidence)
                        {
                            objects[i] = o1 = objects[j];
                        }
                        objects.RemoveAt(j);
                    }
                    else
                    {
                        j++;
                    }
                }
            }
        }

        private static void Annotate(Image<Rgb24> target, IEnumerable<ObjectBox> objects)
        {
            foreach (var objbox in objects) 
            {
                target.Mutate(ctx => 
                {
                    ctx.DrawPolygon(
                        Pens.Solid(Color.Blue, 2),
                        new PointF[] {
                            new ((float)objbox.XMin, (float)objbox.YMin),
                            new ((float)objbox.XMin, (float)objbox.YMax),
                            new ((float)objbox.XMax, (float)objbox.YMax),
                            new ((float)objbox.XMax, (float)objbox.YMin)
                        });
                    
                    ctx.DrawText(
                        $"{Labels[objbox.Class]}", 
                        SystemFonts.Families.First().CreateFont(16), 
                        Color.Blue, 
                        new PointF((float)objbox.XMin, (float)objbox.YMax));
                });
            }
        }

        public static async Task<DetectionInfo> ProcessImageAsync(Image<Rgb24> image, CancellationToken token) 
        {
            var initYoloTask             = Task.Run(() => InitYolo(token), token);
            var resizeImageTask          = Task.Run(() => ImageResize(image), token);
            var image2TensorTask         = resizeImageTask.ContinueWith(x => Image2Tensor(x.Result), token);
            var yoloMagicTask            = Task.WhenAll(new Task[] {initYoloTask, image2TensorTask}).ContinueWith(x => YoloMagic(image2TensorTask.Result), token);
            var calculateBoxesTask       = yoloMagicTask.ContinueWith(x => CalculateBoxes(x.Result), token);
            var removeDuplicateBoxesTask = calculateBoxesTask.ContinueWith(x => RemoveDuplicateBoxes(x.Result), token);
            var annotateTask             = removeDuplicateBoxesTask.ContinueWith(x => Annotate(resizeImageTask.Result, calculateBoxesTask.Result), token);
            await annotateTask;
            return new DetectionInfo(resizeImageTask.Result, calculateBoxesTask.Result);
        }

        private static float Sigmoid(float value)
        {
            var e = (float)Math.Exp(value);
            return e / (1.0f + e);
        } 

        private static float[] Softmax(float[] values)
        {
            var exps = values.Select(v => Math.Exp(v));
            var sum = exps.Sum();
            return exps.Select(e => (float)(e / sum)).ToArray();
        }

        private static int IndexOfMax(float[] values)
        {
            int idx = 0;
            for (int i = 1; i < values.Length; ++i)
                if (values[i] > values[idx])
                    idx = i;
            return idx;
        }
    }

    public record DetectionInfo(Image<Rgb24> Image, IEnumerable<ObjectBox> Box);

    public record ObjectBox(double XMin, double YMin, double XMax, double YMax, double Confidence, int Class)
    {
        public double IoU(ObjectBox b2) =>
             (Math.Min(XMax, b2.XMax) - Math.Max(XMin, b2.XMin)) * (Math.Min(YMax, b2.YMax) - Math.Max(YMin, b2.YMin)) /
            ((Math.Max(XMax, b2.XMax) - Math.Min(XMin, b2.XMin)) * (Math.Max(YMax, b2.YMax) - Math.Min(YMin, b2.YMin)));
    }

    public class Logger : ILogger
    {
        public void Log(string stringToLog)
        {
           Console.WriteLine(stringToLog);
        }
    }
    public interface ILogger
    {
        void Log(string stringToLog);
    }
}