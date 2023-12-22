using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using yolo_logic;

namespace FunnyPicturesWeb.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FunnyPicturesController : ControllerBase
    {
        private CancellationTokenSource Cts { get; set; }

        public FunnyPicturesController()
        {
            Cts = new();
            try
            {
                var downloadTask = Yolo_logic.InitYolo(Cts.Token);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }


        }

        [HttpPost]
        [Route("DetectImages")]
        public async Task<ActionResult<List<ObjectBox>>> DetectImages([FromBody] string imageString)
        {
            try
            {
                byte[] imageByte = Convert.FromBase64String(imageString);
                var image = Image.Load<Rgb24>(imageByte);
                var task = await Yolo_logic.ProcessImageAsync(image, Cts.Token);
                IEnumerable<ObjectBox> detection = task.Box;
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
