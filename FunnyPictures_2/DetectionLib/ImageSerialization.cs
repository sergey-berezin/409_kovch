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
        public List<ObjectBox> ObjectBoxes { get; set; }
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
                ObjectBoxes = new List<ObjectBox>();
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
                ObjectBoxes = detection.Select(x => x.Box).ToList();
            }
            Filename = filename;
        }

        public List<DetectedObject> ToDetectedObjectList()
        {
            List<DetectedObject> detected = new List<DetectedObject>();
            byte[] bytePixels = Convert.FromBase64String(Pixels);
            Image<Rgb24> image = Image.LoadPixelData<Rgb24>(bytePixels, Width, Height);
            for (int i = 0; i < Classes.Count; i++)
            {
                detected.Add(
                    new DetectedObject(
                        image,
                        Classes[i],
                        Confidences[i],
                        ImageDetection.ExtractDetectedObject(image, ObjectBoxes[i]),
                        ObjectBoxes[i]
                    )
                );
            }
            return detected;
        }
    }

    public class JsonStorage
    {
        public string Path { get; private set; }
        private List<ImageSerialization> Images;

        public int Count
        {
            get => Images.Count;
        }

        public JsonStorage(string path = "storage.json")
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
            if (images != null)
                Images = images;
            else
                Images = new List<ImageSerialization>();
        }

        public void Save()
        {
            string tmpPath = Path + ".tmp";
            string serialized = JsonConvert.SerializeObject(Images, Formatting.Indented);
            using (StreamWriter writer = new(tmpPath))
            {
                writer.WriteLine(serialized);
            }
            if (File.Exists(tmpPath))
            {
                File.Delete(Path);
                File.Copy(tmpPath, Path);
            }
        }

        public void AddImage(ImageSerialization image)
        {
            bool imageExists = false;
            foreach (var existing in Images)
            {
                if (image.Pixels == existing.Pixels || image.Filename == existing.Filename)
                {
                    imageExists = true;
                    break;
                }
            }
            if (!imageExists && image.Height > 0 && image.Width > 0)
                Images.Add(image);
        }

        public IEnumerable<ImageSerialization> GetImagePresentations()
        {
            return Images.Select(x => x);
        }
    }
}
