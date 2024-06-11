
using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Xunit.Abstractions;

/// <summary>
/// Logs to both testoutput and Debug
/// supports scopes with ellipsis if the scope depth is too great
/// supports loglevel overrides by pushing loglevel as a scope
/// </summary>
/// <typeparam name="T"></typeparam>
public class XunitLogger<T>(ITestOutputHelper output, LogLevel min = LogLevel.Information) : ILogger<T>
{
    readonly ConcurrentStack<object> _scopes = [];
    struct Ephemeral(XunitLogger<T> issuer) : IDisposable
    {
        public readonly void Dispose()
        {
            if (issuer._scopes.TryPeek(out var prestate) && prestate is not LogLevel)
                issuer.LogTrace(new EventId(2, "End"), "Exit {state}", prestate);
            if (!issuer._scopes.TryPop(out var state)) { issuer.LogWarning("Stack going wrong!"); }
            // issuer.LogTrace(new EventId(2, "End"), "Exit {state}", state);
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        _scopes.Push(state);
        if (state is not LogLevel)
            this.LogTrace(new EventId(1, "Start"), message: "Enter {state}", state);
        return new Ephemeral(this);
    }

    public bool IsEnabled(LogLevel logLevel) => // logLevel >= min ||
        // (logLevel >= ((_scopes.FirstOrDefault(s => s is LogLevel) as LogLevel?) ?? min))
        (logLevel >= (LogLevel)Math.Min((int)min, (int)((_scopes.FirstOrDefault(s => s is LogLevel) as LogLevel?) ?? min)))
        || (logLevel >= LogLevel.Debug && Debugger.IsAttached);

    public void Log<TState>(LogLevel logLevel,
                            EventId eventId,
                            TState state,
                            Exception? exception,
                            Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        int ellipsisPrefix = 3;
        int ellipsisTail = 2;
        var overrideless = _scopes.Where(s => s is not LogLevel);

        string scopetxt = string.Join(" > ", (overrideless.Count() > ellipsisPrefix + ellipsisTail
            ? overrideless.Take(ellipsisTail)
                .Concat([$"...{overrideless.Count() - (ellipsisPrefix + ellipsisTail)}"])
                .Concat(overrideless.Skip(ellipsisTail).TakeLast(ellipsisPrefix))
            : overrideless
            ).Reverse());
        string text = string.Format("[{0}] >> [{1}] {2}", scopetxt, eventId, formatter(state, exception));
        Debug.WriteLine(message: text, category: logLevel.ToString());
        output.WriteLine("{0,11}: {1}", logLevel.ToString(), text);
    }
}