using TheFunnyPicturesWeb.Models;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using yolo_logic;
namespace TheFunnyPicturesWeb.Controllers
{
    [Route("api/WebFunnyPictures")]
    [ApiController]
    public class FunnyPicturesController : Controller
    {
        private CancellationTokenSource Cts { get; set; }

        public FunnyPicturesController()
        {
            Cts = new();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] string imageString)
        {
            if (imageString.Length == 0 || imageString == null)
            {
                Console.WriteLine("Empty image string is received.");
                return BadRequest("Empty image string is received.");
            }
            try
            {
                byte[] imageByte = Convert.FromBase64String(imageString);
                Image<Rgb24> img;
                using (MemoryStream ms = new(imageByte))
                {
                    img = Image.Load<Rgb24>(ms);

                }
                var task = await Yolo_logic.ProcessImageAsync(img, Cts.Token);
                var detection = task.Box.Select(box => new DetectionPack(
                        ImageDetection.ExtractDetectedObject(img, box),
                        Yolo_logic.Labels[box.Class],
                        Math.Round(box.Confidence * 100, 2).ToString() + "%"
                    )
                ).ToList();
                return Ok(detection);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return BadRequest(exc.Message);
            }
        }
    }
}
