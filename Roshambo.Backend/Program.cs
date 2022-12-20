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

// Get basic info.
app.MapGet("/", async (
    StatisticsService stat, 
    ILoggerFactory loggerFactory, 
    CancellationToken cancellationToken) =>
{
    ILogger logger = loggerFactory.CreateLogger("Get/");

    Statistics statistics = await stat.GetGlobalStatisticsAsync(cancellationToken).ConfigureAwait(false);

    return new
    {
        Statistics = statistics,
        Actions = GetRelActions(),
        SuggestedUserId = new UserId(),
    };
});

// Get user id.
app.MapGet("/users/{uId}", async (
    [FromRoute] string uId,
    [FromServices] StatisticsService stat,
    CancellationToken cancellationToken) =>
{
    UserId userId = new UserId(uId);
    Statistics userStatistics = await stat.GetStatisticsForAsync(userId, cancellationToken).ConfigureAwait(false);

    return new
    {
        userStatistics,
    };
});

app.MapPost("/rounds/{actionName}", async (
    [FromRoute] string actionName,
    [FromServices] RoshamboService roshamboService,
    [FromServices] StatisticsService globalStatisticsService,
    [FromBody] RoundRequestBody requestBody,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    ILogger logger = loggerFactory.CreateLogger("POST/Rounds");

    UserId userId = new UserId(requestBody.UserId);
    logger.LogInformation("POST User-id = {0}", userId);

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