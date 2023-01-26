using Donatello.Example.Gateway;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
var exampleGatewayBot = new ExampleGatewayBot(loggerFactory);