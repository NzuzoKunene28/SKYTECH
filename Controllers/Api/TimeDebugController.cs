using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SkyTech.Services;

namespace SkyTech.Controllers.Api
{
    // TEMPORARY: shows what the server believes the time is. Delete when done.
    [ApiController]
    [AllowAnonymous]
    [Route("api/debug")]
    public class TimeDebugController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public TimeDebugController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("time")]
        public IActionResult GetTime()
        {
            if (!_env.IsDevelopment()) return NotFound();

            return Ok(new
            {
                build = "satime-v2",
                utcNow = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                serverLocalNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                serverTimeZone = TimeZoneInfo.Local.Id,
                serverUtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).ToString(),
                saNow = SaTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                saToday = SaTime.Today.ToString("yyyy-MM-dd dddd")
            });
        }
    }
}