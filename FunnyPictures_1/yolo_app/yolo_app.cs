﻿using yolo_logic;
namespace yolo_app
{
    public class Yolo_app
    {
        private static readonly CancellationTokenSource cts = new();
        private static readonly CancellationToken token = cts.Token;
        private const string ResultDirPath = "detection_results";
        private static async Task Main(string[] args)
        {
            if (args.Length == 0) 
            {
                Console.WriteLine("YOLO detected that you forgot to specify the path to the images.");
                return;
            }
            
            foreach (var arg in args) 
            {
                if (!File.Exists(arg))
                {
                    Console.WriteLine("Specified image seems to be absent " + arg.ToString());
                    return;
                } 
            }

            List<CSVBoxInfo> boxes = new();
            Task mainTask = Task.WhenAll(args.Select(arg => {
                return Task.Run(async () => {
                    try {
                        Image<Rgb24> image = Image.Load<Rgb24>(arg);
                        Task<DetectionInfo> detectionTask = Yolo_logic.ProcessImageAsync(image, token);
                        if (!Directory.Exists(ResultDirPath))
                        {
                            Directory.CreateDirectory(ResultDirPath);
                        }
                        await detectionTask;
                        Image<Rgb24> detectedImage = detectionTask.Result.Image;
                        IEnumerable<ObjectBox> detectedBox = detectionTask.Result.Box;
                        string pathForSaving = $"{ResultDirPath}\\{arg}";
                        Task saveImageTask = detectedImage.SaveAsJpegAsync(pathForSaving, token);

                        boxes.AddRange(detectedBox.Select(
                            box => new CSVBoxInfo(arg, Yolo_logic.Labels[box.Class], box.XMin, box.YMax, box.XMax - box.XMin, box.YMax - box.YMin)
                        ));
                        await saveImageTask;
                    }
                    catch (Exception exc) 
                    {
                        Console.WriteLine(exc.Message);
                    }
                }, token);
            }));

            try 
            {
                await mainTask;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            
            try 
            {
                string csvPath = $"{ResultDirPath}\\boxes.csv";
                using FileStream fs = new(csvPath, FileMode.Append, FileAccess.Write);
                using StreamWriter sw = new(fs);
                foreach (var box in boxes) {
                    sw.WriteLine(box.ToString());
                }
            }
            catch (Exception exc) 
            {
                Console.WriteLine(exc.Message);
            }

            Console.CancelKeyPress += delegate {
                cts.Cancel();
            };
        }

        private record CSVBoxInfo(string Filename, string Classname, double X, double Y, double W, double H)
        {
            public override string ToString()
            {
                return $"\"{Filename}\", \"{Classname}\", \"{X}\", \"{Y}\", \"{W}\", \"{H}\"";
            }
        }
    }
}