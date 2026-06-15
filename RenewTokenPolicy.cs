using System.ClientModel;
using System.ClientModel.Primitives;

namespace BedrockMantleExperiments;

public sealed class RenewTokenPolicy(ApiKeyCredential credential, Func<string> provideToken) : PipelinePolicy
{
    static readonly TimeSpan RenewInterval = TimeSpan.FromMinutes(30);
    readonly Lock _gate = new();
    DateTimeOffset _renewAt = DateTimeOffset.UtcNow + RenewInterval;

    void RenewIfDue()
    {
        if (DateTimeOffset.UtcNow < _renewAt) return;
        lock (_gate)
        {
            if (DateTimeOffset.UtcNow < _renewAt) return;
            credential.Update(provideToken());
            _renewAt = DateTimeOffset.UtcNow + RenewInterval;
        }
    }

    public override void Process(PipelineMessage m, IReadOnlyList<PipelinePolicy> p, int i)
    {
        RenewIfDue();
        ProcessNext(m, p, i);
    }

    public override ValueTask ProcessAsync(PipelineMessage m, IReadOnlyList<PipelinePolicy> p, int i)
    {
        RenewIfDue();
        return ProcessNextAsync(m, p, i);
    }
}
