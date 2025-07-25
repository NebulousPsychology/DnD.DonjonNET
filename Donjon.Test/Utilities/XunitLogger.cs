
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

using Microsoft.Extensions.Logging;

using Xunit.Abstractions;
namespace Donjon.Test.Utilities;

/// <summary>
/// Logs to both testoutput and Debug
/// supports scopes with ellipsis if the scope depth is too great
/// supports loglevel overrides by pushing loglevel as a scope
/// </summary>
/// <typeparam name="T"></typeparam>
public class XunitLogger<T>(ITestOutputHelper output, string? category = null, LogLevel min = LogLevel.Information) : ILogger<T>
{
    readonly ConcurrentStack<object> _scopes = [];
    struct Ephemeral(XunitLogger<T> issuer) : IDisposable
    {
        public readonly void Dispose()
        {
            if (issuer.ShowScopeTransition == ScopeVisibility.Visible
                && issuer._scopes.TryPeek(out var prestate)
                && prestate is not LogLevel or ScopeVisibility)
                issuer.LogTrace(new EventId(2, "End"), "Exit {state}", prestate);

            if (!issuer._scopes.TryPop(out var state)) { issuer.LogWarning("Stack going wrong!"); }
            // issuer.LogTrace(new EventId(2, "End"), "Exit {state}", state);
        }
    }

    public IDisposable? BeginScopeVisibility(bool visible)
    {
        _scopes.Push(visible ? ScopeVisibility.Visible : ScopeVisibility.Hidden);
        return new Ephemeral(this);
    }
    enum ScopeVisibility
    {
        Hidden,
        Visible,
    }
    ScopeVisibility ShowScopeTransition => _scopes.FirstOrDefault(o => o is ScopeVisibility) as ScopeVisibility? ?? ScopeVisibility.Hidden;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        _scopes.Push(state);
        if (ShowScopeTransition == ScopeVisibility.Visible && state is not LogLevel or ScopeVisibility)
            this.LogTrace(new EventId(1, "Start"), message: "Enter {state}", state);
        return new Ephemeral(this);
    }

    public bool IsEnabled(LogLevel logLevel) => // logLevel >= min ||
                                                // (logLevel >= ((_scopes.FirstOrDefault(s => s is LogLevel) as LogLevel?) ?? min))
        (logLevel >= (LogLevel)Math.Min((int)min, (int)((_scopes.FirstOrDefault(s => s is LogLevel) as LogLevel?) ?? min)))
        || (logLevel >= LogLevel.Debug && Debugger.IsAttached);

    private string GetScopeText(bool granularIncreasing = true, int ellipsisPrefix = 3, int ellipsisTail = 2)
    {
        var overrideless = _scopes.Where(s => s is not LogLevel or ScopeVisibility);
        int ellipsisCount = ellipsisPrefix + ellipsisTail;
        int ellipsisDelta = overrideless.Count() - ellipsisCount;
        var seq = ellipsisDelta switch
        {
            > 0 => overrideless
                .Take(ellipsisTail)
                .Concat([$"... (x{ellipsisDelta})"])
                .Concat(overrideless.Skip(ellipsisTail).TakeLast(ellipsisPrefix)) switch
            {
                var x when granularIncreasing => x.Reverse(),
                var x => x,
            },
            _ => overrideless,
        };//                               "
        return string.Join(" >\n              ", seq);
    }
    public void Log<TState>(LogLevel logLevel,
                            EventId eventId,
                            TState state,
                            Exception? exception,
                            Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        string scopetxt = GetScopeText(granularIncreasing: true);
        StringBuilder body = new();
        string inset = "\t";
        body.AppendFormat("{0,12}: ", logLevel.ToString().ToUpperInvariant());
        body.Append(scopetxt);
        body.AppendFormat("[{0}]", category);
        body.AppendLine();
        body.AppendLine();
        body.AppendFormat("{0}{1}", inset, formatter(state, exception).Replace("\n", $"\n{inset}"));
        body.AppendLine();
        body.AppendLine();

        Debug.WriteLine(message: body.ToString(), category: logLevel.ToString());
        output.WriteLine("{0}", body);
    }
}