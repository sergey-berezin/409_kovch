using AsyncCommand;
using DetectionLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using yolo_logic;

namespace ViewModel
{
    public interface IUIShaha
    {
        void ShowError(string message);
        List<string> ExtractFilenames(string folderName, string format);
        string? GetPwd();
    }

    public class DetectedImageView
    {
        private ObjectBox Box { get; set; }
        private Image<Rgb24> Original { get; }

        public BitmapSource SelectedImage
        {
            get
            {
                return ImageToBitmapSource(ImageDetection.Annotate(Original, Box));
            }
        }
        public BitmapSource Image { get; }
        public string Class { get; set; }
        public double Confidence { get; set; }

        public DetectedImageView(DetectedObject detectedObject)
        {
            Box = detectedObject.Box;
            Original = detectedObject.Original;
            Image = ImageToBitmapSource(detectedObject.Detected_object);
            Class = detectedObject.Class;
            Confidence = detectedObject.Confidence;
        }

        private static BitmapSource ImageToBitmapSource(Image<Rgb24> image)
        {
            byte[] pixels = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgb24>()];
            image.CopyPixelDataTo(pixels);

            return BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Rgb24, null, pixels, 3 * image.Width);
        }
    }
    public class ViewModelClass : ViewModelBase
    {
        private string pwd { get; set; } = string.Empty;
        private bool IsDetecting { get; set; } = false;
        private CancellationTokenSource? Cts { get; set; }

        public string Pwd
        {
            get => pwd;
            set
            {
                if (value != null && value != pwd)
                {
                    pwd = value;
                    RaisePropertyChanged(nameof(Pwd));
                }
            }
        }

        public List<DetectedImageView> DetectedImages { get; private set; }

        private readonly IUIShaha uiShaha;

        private void OnSelectFolder(object arg)
        {
            string? folderName = uiShaha.GetPwd();
            if (folderName == null) { return; }
            Pwd = folderName;
        }

        public async Task OnRunModel(object arg)
        {
            DetectedImages.Clear();
            RaisePropertyChanged(nameof(DetectedImages));
            try
            {
                IsDetecting = true;
                Cts = new CancellationTokenSource();

                List<string> fileNames = uiShaha.ExtractFilenames(Pwd, ".jpg");
                if (fileNames.Count == 0)
                {
                    uiShaha.ShowError("There are no jpg files in selected folder.");
                    return;
                }

                var tasks = fileNames.Select(arg =>
                    Task.Run(() => ImageDetection.DetectImage(arg, Cts.Token))
                ).ToList();

                while (tasks.Any())
                {
                    var task = await Task.WhenAny(tasks);
                    var detectedObjects = task.Result.ToList();
                    tasks.Remove(task);
                    DetectedImages = DetectedImages.Concat(
                        detectedObjects.Select(x => new DetectedImageView(x))
                    ).ToList();
                    RaisePropertyChanged(nameof(DetectedImages));
                }
            }
            catch (Exception exc)
            {
                uiShaha.ShowError(exc.Message);
            }
            finally
            {
                IsDetecting = false;
            }
        }

        public void OnRequestCancellation(object arg)
        {
            Cts?.Cancel();
        }

        public ICommand SelectFolderCommand { get; private set; }
        public ICommand RunModelCommand { get; private set; }
        public ICommand AbortCommand { get; private set; }

        public ViewModelClass(IUIShaha uiShaha)
        {
            Pwd = string.Empty;
            DetectedImages = new List<DetectedImageView>();

            this.uiShaha = uiShaha;

            SelectFolderCommand = new RelayCommand(OnSelectFolder, x => !IsDetecting);
            RunModelCommand = new AsyncRelayCommand(OnRunModel, x => Pwd != string.Empty && !IsDetecting);
            AbortCommand = new RelayCommand(OnRequestCancellation, x => IsDetecting);
        }
    }
}