using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Roshambo.Models;
using Roshambo.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(opt =>
{
    opt.SingleLine = true;
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options => options.AddDefaultPolicy(builder =>
{
    builder
        .WithOrigins("http://127.0.0.1:5500", "https://roshambo.codewithsaar.com")
        .WithHeaders("content-type")
        .WithMethods("GET", "POST", "OPTIONS")
        .AllowCredentials();
}));

builder.Services.AddSingleton<StatisticsService>();
builder.Services.AddTransient<RoshamboService>();

var app = builder.Build();
app.UseCors();

app.MapGet("/", async (HttpContext context, CancellationToken cancellationToken, StatisticsService globalStat, ILoggerFactory loggerFactory) =>
{
    ILogger logger = loggerFactory.CreateLogger("Get/");
    string? userIdCookieValue = context.Request.Cookies["user-id"];
    logger.LogInformation("User id: {0}", userIdCookieValue);

    if (userIdCookieValue is null)
    {
        userIdCookieValue = Guid.NewGuid().ToString();
        logger.LogInformation("New User id: {0}", userIdCookieValue);
    }

    Statistics statistics = await globalStat.GetGlobalStatisticsAsync(cancellationToken).ConfigureAwait(false);
    Statistics userStatistics = await globalStat.GetStatisticsForAsync(userIdCookieValue, cancellationToken).ConfigureAwait(false);

    context.Response.Cookies.Append("user-id", userIdCookieValue, new CookieOptions()
    {
        Secure = true,
        SameSite = SameSiteMode.None,
        HttpOnly = false,
        Expires = DateTimeOffset.UtcNow.AddYears(10),
    });
    return new
    {
        Statistics = statistics,
        UserStatistics = userStatistics,
        Actions = GetRelActions(),
    };
});

app.MapPost("/rounds/{actionName}", async (
    HttpContext context,
    ILoggerFactory loggerFactory,
    [FromRoute] string actionName,
    [FromServices] RoshamboService roshamboService,
    [FromServices] StatisticsService globalStatisticsService,
    CancellationToken cancellationToken) =>
{
    ILogger logger = loggerFactory.CreateLogger("POST/Rounds");
    string? cookie = context.Request.Cookies["user-id"];
    logger.LogInformation("POST cookie[user-id] = {0}", cookie);

    string userId = cookie ?? Guid.Empty.ToString("d");

    if (Enum.TryParse<RoshamboOption>(actionName, ignoreCase: true, out RoshamboOption userOption))
    {
        (RoshamboResult roundResult, RoshamboOption computerMove) = await roshamboService.GoAsync(userId, userOption, cancellationToken);
        Statistics globalStatistics = await globalStatisticsService.GetGlobalStatisticsAsync(cancellationToken).ConfigureAwait(false);
        Statistics userStatistics = await globalStatisticsService.GetStatisticsForAsync(userId, cancellationToken).ConfigureAwait(false);

        return new
        {
            Round = new RoundResult()
            {
                Result = roundResult,
                ComputerMove = computerMove.ToAction(),
                UserMove = userOption.ToAction(),
            },
            Actions = GetRelActions(),
            Statistics = globalStatistics,
            UserStatistics = userStatistics,
        };
    }
    throw new InvalidOperationException($"Invalid action name of {actionName}");
});

app.Run();

// TODO: Make this into its own service.
RelAction[] GetRelActions()
{
    return new RelAction[]{
            new RockAction(),
            new PaperAction(),
            new ScissorAction(),
        };
}