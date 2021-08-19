using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace porukica.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UploadController : ControllerBase
    {
        [HttpGet]
        public IActionResult Something()
        {
            return Ok("kita");
        }
        [HttpPost]
        public async Task<IActionResult> Upload()
        {
            if (!HttpContext.Request.Headers.TryGetValue("key", out var key) || !Database.Uploads.TryGetValue(key, out var upload))
                return BadRequest();

            var fi = new FileInfo(upload.Path);
            using var stream = fi.Open(FileMode.Append, FileAccess.Write);

            await HttpContext.Request.Body.CopyToAsync(stream);

            return Ok();
        }
    }
}