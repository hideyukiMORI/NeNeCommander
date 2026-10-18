namespace NeNeCommander.Complexity;

internal sealed class MetricFinding
{
    internal MetricFinding(MetricLocation location, string metric, int value, int limit)
    {
        Location = location;
        Metric = metric;
        Value = value;
        Limit = limit;
    }

    public MetricLocation Location { get; }
    public string Metric { get; }
    public int Value { get; }
    public int Limit { get; }
}
