using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.CompilerServices;
using yolo_logic;

namespace DetectionLib
{
    public class ImageSerialization
    {
        public string Pixels { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public List<string> Classes { get; set; }
        public List<double> Confidences { get; set; }
        public List<ObjectBox> Boxes { get; set; }
        public string Filename { get; set; }

        public ImageSerialization(IEnumerable<DetectedObject> detection, string filename)
        {
            Filename = filename;
            if (detection == null || !detection.Any())
            {
                Pixels = string.Empty;
                Height = 0;
                Width = 0;
                Classes = new List<string>();
                Confidences = new List<double>();
                Boxes = new List<ObjectBox>();
            }
            else
            {
                Image<Rgb24> image = detection.First().Original;
                byte[] bytePixels = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgb24>()];
                image.CopyPixelDataTo(bytePixels);
                Pixels = Convert.ToBase64String(bytePixels);
                Height = image.Height;
                Width  = image.Width;
                Classes     = detection.Select(x => x.Class).ToList();
                Confidences = detection.Select(x => x.Confidence).ToList();
                Boxes = detection.Select(x => x.Box).ToList();
            }
        }

        public List<DetectedObject> ToDetectedObjectList()
        {
            List<DetectedObject> detected = new();
            byte[] bytePixels = Convert.FromBase64String(Pixels);
            Image<Rgb24> image = Image.LoadPixelData<Rgb24>(bytePixels, Width, Height);
            for (int i = 0; i < Classes.Count; i++)
            {
                detected.Add(
                    new DetectedObject(
                        image,
                        Classes[i],
                        Confidences[i],
                        ImageDetection.ExtractDetectedObject(image, Boxes[i]),
                        Boxes[i]
                    )
                );
            }
            return detected;
        }
    }

    public class JsonStorage
    {
        public string Path { get; private set; }
        public List<ImageSerialization> Images;

        public int Count
        {
            get => Images.Count;
        }

        public JsonStorage(string path = "JsonStorage.json")
        {
            Path = path;
            Images = new List<ImageSerialization>();
        }

        public void Delete()
        {
            Images.Clear();
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }

        public void Load()
        {
            if (!File.Exists(Path))
            {
                return;
            }
            var images = JsonConvert.DeserializeObject<List<ImageSerialization>>(File.ReadAllText(Path));
            if (images == null)
            {
                Images = new List<ImageSerialization>();
            }
            else
            {
                Images = images;
            }
        }

        public void Save()
        {
            string tmp = Path + ".tmp";
            string serialized = JsonConvert.SerializeObject(Images, Formatting.Indented);
            using (StreamWriter sw = new(tmp))
            {
                sw.WriteLine(serialized);
            }
            if (File.Exists(tmp))
            {
                File.Delete(Path);
                File.Copy(tmp, Path);
                File.Delete(tmp);
            }
        }

        public void AddImage(ImageSerialization newImg)
        {
            bool dublicate = false;

            foreach (var img in Images)
            {
                if (newImg.Pixels == img.Pixels || newImg.Filename == img.Filename)
                {
                    dublicate = true;
                    break;
                }
            }
            if (newImg.Height != 0 && newImg.Width != 0 && !dublicate)
            {
                Images.Add(newImg);
            }
        }
    }
}
