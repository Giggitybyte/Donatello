namespace Donatello.Example.Gateway;

using Donatello.Gateway;
using Microsoft.Extensions.Logging;

public class ExampleGatewayBot
{
    private GatewayBot _gatewayBot;

    public ExampleGatewayBot(ILoggerFactory loggerFactory)
    {
        var token = "ODc1NTQ3NjkzMzk5ODE4MjUw.GfT3gl.hNTo_vRwxhXt42rsfxaJh-OTe0f-xlDhvK4xvY";
        _gatewayBot = new GatewayBot(token, loggerFactory: loggerFactory);
    }

    public ValueTask RunAsync()
        => _gatewayBot.StartAsync();
}
