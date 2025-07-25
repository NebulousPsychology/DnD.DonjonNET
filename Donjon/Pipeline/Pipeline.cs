using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

namespace Donjon.Pipeline;

public interface IPipelineOperation<T>
{
    public bool TryInvoke(T input, [NotNullWhen(true), MaybeNullWhen(false)] out T? result);
}

public class Pipeline<T>(ILogger<Pipeline<T>> logger, string name = "unknown")
: IPipelineOperation<T>
{
    protected List<IPipelineOperation<T>> Steps { get; } = [];

    public Pipeline<T> RegisterStep(Func<T, T> step, string? stepname = null)
        => RegisterStep(new PipelineFuncStep<T>(step, stepname ?? $"anonymous step {Steps.Count.ToString()}"));

    public Pipeline<T> RegisterStep(IPipelineOperation<T> step)
    {
        Steps.Add(step);
        return this;
    }

    public bool TryInvoke(T input, [NotNullWhen(true), MaybeNullWhen(false)] out T? result)
    {
        T x = input;
        foreach (var (i, op) in Steps.Index())
        {
            using (logger.BeginScope($"Pipeline {name} step {i}"))
            {
                if (op.TryInvoke(x, out var intermediate))
                {
                    x = intermediate;
                }
                else
                {
                    logger.LogWarning("FAILED at step{stepnum} '{stepname}'", i, op is PipelineFuncStep<T> pfs ? pfs.StepName : op.GetType());
                    result = intermediate;
                    return false;
                }
            }
            ;
        }
        logger.LogDebug("Pipeline {name} Finished", name);
        result = x!;
        return true;
    }
}

class PipelineFuncStep<T>(Func<T, T> operation, string stepname) : IPipelineOperation<T>
{
    public string StepName { get; } = stepname;
    public bool TryInvoke(T input, [MaybeNullWhen(false), NotNullWhen(true)] out T? result)
    {
        try
        {
            result = operation(input)!;
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}