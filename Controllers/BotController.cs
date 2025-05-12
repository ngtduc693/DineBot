using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace FoodOrderBots.Controllers;

[Route("api/messages")]
[ApiController]
public class BotController : ControllerBase
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IBot _bot;
    private readonly IConfiguration _configuration;

    public BotController(IBotFrameworkHttpAdapter adapter, IBot bot, IConfiguration configuration)
    {
        _adapter = adapter;
        _bot = bot;
        _configuration = configuration;
    }

    [HttpPost]
    [HttpGet]
    public async Task PostAsync()
    {
        var appId = _configuration["MicrosoftAppId"];
        if (!string.IsNullOrEmpty(appId))
        {
            var headers = Request.Headers;
            if (!headers.ContainsKey("Authorization"))
            {
                Response.StatusCode = 401;
                await Response.WriteAsync("Unauthorized: Missing Authorization header.");
                return;
            }
        }

        await _adapter.ProcessAsync(Request, Response, _bot);
    }
}