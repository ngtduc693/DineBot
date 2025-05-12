using FoodOrderBots;
using FoodOrderBots.Bots;
using FoodOrderBots.Dialogs;
using FoodOrderBots.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

builder.Services.AddSingleton<IStorage, MemoryStorage>();

builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<UserState>();

builder.Services.AddSingleton<ILanguageUnderstandingService>(sp => {
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new LanguageUnderstandingService(
        endpoint: configuration["LanguageUnderstanding:Endpoint"],
        key: configuration["LanguageUnderstanding:Key"],
        projectName: configuration["LanguageUnderstanding:ProjectName"],
        deploymentName: configuration["LanguageUnderstanding:DeploymentName"],
        logger: sp.GetRequiredService<ILogger<LanguageUnderstandingService>>()
    );
});

builder.Services.AddSingleton<MenuService>();
builder.Services.AddSingleton<OrderService>();

builder.Services.AddSingleton<MainDialog>();
builder.Services.AddTransient<IBot, FoodOrderingBot>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseWebSockets();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();