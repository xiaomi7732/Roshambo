using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Roshambo.Models;
using Roshambo.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options => options.AddDefaultPolicy(builder =>{
    builder.AllowAnyHeader().WithOrigins("http://127.0.0.1:5500");
}));

builder.Services.AddTransient<GlobalStatisticsService>();
builder.Services.AddTransient<RoshamboService>();

var app = builder.Build();
app.UseCors();

app.MapGet("/", async (CancellationToken cancellationToken, GlobalStatisticsService globalStat) =>
{
    GlobalStatistics statistics = await globalStat.GetGlobalStatisticsAsync(cancellationToken).ConfigureAwait(false);
    return new
    {
        Statistics = statistics,
        Actions = GetRelActions(),
    };
});

app.MapPost("/rounds/{actionName}", async (
    [FromRoute] string actionName,
    [FromServices] RoshamboService roshamboService,
    [FromServices] GlobalStatisticsService globalStatisticsService,
    CancellationToken cancellationToken) =>
{
    if (Enum.TryParse<RoshamboOption>(actionName, ignoreCase: true, out RoshamboOption userOption))
    {
        (RoshamboResult roundResult, RoshamboOption computerMove) = roshamboService.Go(userOption);
        GlobalStatistics globalStatistics = await globalStatisticsService.GetGlobalStatisticsAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            Round = new RoundResult()
            {
                Result = roundResult,
                ComputerMove = computerMove.ToAction(),
                UserMove = userOption.ToAction(),
                ComputerWinning = globalStatistics.ComputerWinning,
                HumanWinning = globalStatistics.HumanWinning,
                Draw = globalStatistics.Draw,
            },
            Actions = GetRelActions(),
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