// #define USE_REFLECTED_MOCK_LOGGER
namespace Donjon.Test.Utilities;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

public class HostedTestBase<T> // where T : class
{
    protected IHost TestHost { get; private init; }
    protected ILogger<T> Logger { get; private init; }
    protected ILoggerFactory LoggerFactory { get; private set; }
    protected ITestOutputHelper XunitOutput { get; }

    [NotNullIfNotNull(nameof(TestHost))]
    protected IServiceProvider Services => TestHost.Services;

    public HostedTestBase(ITestOutputHelper output, LogLevel min = LogLevel.Information)
    {
        XunitOutput = output;

        TestHost = Host.CreateDefaultBuilder()
            .ConfigureLogging(b => PerformLoggingConfig(b, min))
            .ConfigureServices(PerformServiceConfig)
            .ConfigureHostOptions(PerformOptionsConfig)
            .Build();

        Logger = Services.GetRequiredService<ILogger<T>>();
        LoggerFactory = Services.GetRequiredService<ILoggerFactory>();
    }

    ILoggerProvider MockProvider(LogLevel level)
    {
        ILoggerProvider toh = Substitute.For<ILoggerProvider>();
#if USE_REFLECTED_MOCK_LOGGER
            toh.CreateLogger(NSubstitute.Arg.Any<string>()).Returns(o =>
                {
                    string category = o.Arg<string>();
                    var types = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(asm => asm.GetTypes().Where(t => t.FullName == category));
                    if (!types.Any()) { return new TestLogger<object>(output, parameters, category); }
                    // FIXME: assignableTo/From [ms].[ext].[h].[in].ApplicationLifetime as [ms].[ext].[h].[in].IHostApplicationLifetime

                    Type genericType = types.FirstOrDefault() // Type.GetType(o.Arg<string>())
                        ?? throw new Exception($"Unable to prepare loggerprovider for '{o.Arg<string>()}'");

                    Type genericClassType = typeof(TestLogger<>);


                    // Make the generic type with the given type
                    Type constructedType = genericClassType.MakeGenericType(genericType);

                    // Create an instance of the constructed type
                    object instance = Activator.CreateInstance(constructedType, output, parameters)
                        ?? throw new Exception($"sww1");

                    // Use reflection to access the Value property
                    return instance;
                    //?
                    // constructedType.get

                    // System.Reflection.PropertyInfo valueProperty = constructedType.GetProperty("Value")
                    //     ?? throw new Exception($"sww2");
                    // object value = valueProperty.GetValue(instance) ?? throw new Exception($"sww");

                    // return value;
                }
                );
#else
        toh.CreateLogger(Arg.Any<string>())
            .Returns(o => new XunitLogger<object>(XunitOutput, min: level, category: o.Arg<string>()));
#endif
        return toh;
    }

    private void PerformLoggingConfig(ILoggingBuilder context, LogLevel level)
    {
        context.ClearProviders().AddProvider(MockProvider(level));
        ApplyFilters(context);
    }

    protected virtual Dictionary<LogLevel, IEnumerable<string>> Filters => [];

    private void ApplyFilters(ILoggingBuilder context)
    {
        foreach (var f in Filters)
            foreach (var category in f.Value)
                context.AddFilter(category, level: f.Key);
    }

    // protected virtual IHostBuilder PerformHostConfig(IHostBuilder builder) => builder;
    protected virtual void PerformOptionsConfig(HostBuilderContext context, HostOptions options) { }
    protected virtual void PerformServiceConfig(HostBuilderContext context, IServiceCollection services) { }
}