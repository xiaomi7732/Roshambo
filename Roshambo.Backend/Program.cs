using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Roshambo.Models;
using Roshambo.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(opt =>
{
    opt.SingleLine = true;
});

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddServiceProfiler();

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

builder.Services.AddOptions<StorageOptions>().Configure<IConfiguration>((opt, configuration) => configuration.GetSection("Storage").Bind(opt));
builder.Services.AddSingleton<IStorageService, StorageService>();

builder.Services.AddSingleton<ResultStorageService>();

builder.Services.AddSingleton<UserDataUtility>(_ => UserDataUtility.Instance);
builder.Services.AddSingleton<StatisticsService>();
builder.Services.AddTransient<RoshamboService>();

var app = builder.Build();
app.UseCors();

// Get basic info.
app.MapGet("/", (HttpContext httpContext, ILoggerFactory loggerFactory) =>
{
    ILogger logger = loggerFactory.CreateLogger("Get/");

    HttpRequest request = httpContext.Request;
    string myUrl = $"{request.Scheme}://{request.Host}{request.Path}";
    logger.LogInformation("MyUrl: {0}", myUrl);

    return new
    {
        SuggestedUserId = new UserId(),
        Self = new SelfRel(myUrl, HttpMethod.Get),
        Next = new RelModel[]{
            new ReadyRel(){Href=$"{request.Scheme}://{request.Host}/players/{{uid}}"},
        },
    };
});

// Get user id.
app.MapGet("/players/{uId}", async (
    [FromRoute] string uId,
    [FromServices] StatisticsService stat,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    UserId userId = new UserId(uId);
    Statistics userStatistics = await stat.GetStatisticsForAsync(userId, cancellationToken).ConfigureAwait(false);
    Statistics statistics = await stat.GetGlobalStatisticsAsync(cancellationToken).ConfigureAwait(false);

    HttpRequest request = httpContext.Request;
    string urlBase = $"{request.Scheme}://{request.Host}";
    string myUrl = $"{urlBase}{request.Path}";

    return new
    {
        Actions = GetRelActions(urlBase),
        Statistics = statistics,
        userStatistics,
        Self = new SelfRel(myUrl, HttpMethod.Get),
    };
});

app.MapPost("/rounds/{actionName}", async (
    [FromRoute] string actionName,
    [FromServices] RoshamboService roshamboService,
    [FromServices] StatisticsService globalStatisticsService,
    [FromBody] RoundRequestBody requestBody,
    HttpContext httpContext,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    ILogger logger = loggerFactory.CreateLogger("POST/Rounds");

    UserId userId = new UserId(requestBody.UserId);
    logger.LogInformation("POST User-id = {0}", userId);

    HttpRequest request = httpContext.Request;
    string urlBase = $"{request.Scheme}://{request.Host}";

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
                ComputerMove = computerMove.ToAction(urlBase),
                UserMove = userOption.ToAction(urlBase),
            },
            Actions = GetRelActions(urlBase),
            Statistics = globalStatistics,
            UserStatistics = userStatistics,
        };
    }
    throw new InvalidOperationException($"Invalid action name of {actionName}");
});

app.Run();

RelModel[] GetRelActions(string urlBase)
{
    return new RelModel[]{
        new RockAction(urlBase),
        new PaperAction(urlBase),
        new ScissorAction(urlBase),
    };
}