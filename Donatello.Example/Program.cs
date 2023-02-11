using Donatello;
using Donatello.Entity;
using Donatello.Enum;
using Donatello.Gateway;
using Donatello.Gateway.Event;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using Donatello.Extension;

var token = "ODc1NTQ3NjkzMzk5ODE4MjUw.GfT3gl.hNTo_vRwxhXt42rsfxaJh-OTe0f-xlDhvK4xvY";
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger("Example");
var gatewayBot = new GatewayBot(token, GatewayIntent.Unprivileged, loggerFactory);

gatewayBot.Events.OfType<EntityCreatedEvent<Guild>>().SubscribeAsync(async e =>
{
    await foreach (var member in e.Entity.FetchMembersAsync())
        logger.LogInformation(member.AvatarUrl);
});

await gatewayBot.StartAsync();

await Task.Delay(-1);