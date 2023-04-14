using Donatello.Gateway;
using Donatello.Gateway.Event;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Donatello.Common.Entity.Guild;
using Donatello.Common.Entity.Guild.Channel;
using Donatello.Common.Enum;
using Donatello.Common.Extension;

var token = "ODc1NTQ3NjkzMzk5ODE4MjUw.GfT3gl.hNTo_vRwxhXt42rsfxaJh-OTe0f-xlDhvK4xvY";
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
var logger = loggerFactory.CreateLogger("Example");
var gatewayBot = new GatewayBot(token, GatewayIntent.Unprivileged, loggerFactory);

gatewayBot.Events.OfType<EntityCreatedEvent<GuildTextChannel>>().Subscribe(async e =>
{
    await foreach (var message in e.Entity.GetMessagesAsync())
        logger.LogInformation(message.Id);
});

await gatewayBot.StartAsync();

await Task.Delay(-1);