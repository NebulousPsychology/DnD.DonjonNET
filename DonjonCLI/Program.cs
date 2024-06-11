// See https://aka.ms/new-console-template for more information
using Donjon;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello, World!");
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddSimpleConsole(options => { options.IncludeScopes = true; options.SingleLine = false; });
builder.Logging.SetMinimumLevel(LogLevel.Information);
// builder.Logging.AddJsonConsole()
builder.Services.AddSingleton<DungeonGen>();
using IHost host = builder.Build();

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
var pcon = host.Services.GetRequiredService<ILogger<Program>>();
var gen = host.Services.GetRequiredService<DungeonGen>();
pcon.LogInformation("host ready");
// host.Run();


Dungeon d = new() { seed = 55585,  };
// Dungeon d = new() { seed = 12345, n_rows = 21, n_cols = 15, room_max = 5 };
// _xunitLogger.LogInformation("{description}", DescribeDungeon(d));
try
{
    var d2 = gen.Create_dungeon(d);
}
catch (Exception e)
{
    pcon.LogError(e, "SWW");
    throw;
}
pcon.LogInformation("Exiting");
