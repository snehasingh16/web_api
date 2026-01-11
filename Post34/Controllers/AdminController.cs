using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Post34.Helpers;

namespace Post34.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly MongoSettings _mongo;
    private readonly IHostEnvironment _env;

    public AdminController(MongoSettings mongo, IHostEnvironment env)
    {
        _mongo = mongo;
        _env = env;
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        if (!_env.IsDevelopment())
            return Forbid();

        string MaskConnection(string conn)
        {
            if (string.IsNullOrWhiteSpace(conn)) return string.Empty;
            try
            {
                // mask credentials between :// and @
                var idx = conn.IndexOf("@");
                var start = conn.IndexOf("://");
                if (start >= 0 && idx > start)
                {
                    var prefix = conn.Substring(0, start + 3);
                    var rest = conn.Substring(idx);
                    return prefix + "***REDACTED***" + rest;
                }
            }
            catch { }
            return "***REDACTED***";
        }

        return Ok(new {
            usingMongo = !string.IsNullOrEmpty(_mongo.ConnectionString),
            database = _mongo.Database,
            connection = MaskConnection(_mongo.ConnectionString)
        });
    }
}
