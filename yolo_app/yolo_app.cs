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
                Console.WriteLine("YOLO detected that you forgot to specify the path to the image.");
                return;
            }
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Specified image seems to be absent.");
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
            FileStream ? fs = null;
            string csvPath = $"{ResultDirPath}\\boxes.csv";
            try 
            {
                if (!File.Exists(csvPath))
                {
                    File.Create(csvPath);
                }
                File.AppendAllLines(csvPath, boxes.Select(box => box.ToString()));
            }
            catch (Exception exc) 
            {
                Console.WriteLine(exc.Message);
            }
            finally
            {
                fs?.Close();
            }
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