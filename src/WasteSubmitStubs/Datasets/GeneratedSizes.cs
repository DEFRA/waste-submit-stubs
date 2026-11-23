using System.ComponentModel.DataAnnotations;

namespace WasteSubmitStubs.Datasets;

/// <summary>
/// The shape of a generated year, from production counts. Only counts are used; no production rows or IDs.
/// </summary>
public sealed class GeneratedSizes
{
    [Range(0, 100_000)]
    public int DirectRegistrants { get; init; }

    /// <summary>The share of direct registrants that also report a subsidiary, giving them a second producer.</summary>
    [Range(0.0, 1.0)]
    public double DirectRegistrantSubsidiaryShare { get; init; }

    [Range(0, 10_000)]
    public int ComplianceSchemes { get; init; }

    /// <summary>Members of the largest scheme. The others are spread evenly so the average comes out right.</summary>
    [Range(0, 100_000)]
    public int LargestSchemeProducers { get; init; }

    [Range(0, 100_000)]
    public int ProducersPerSchemeAverage { get; init; }

    /// <summary>Each producer reports between this many and <see cref="MaterialsPerProducerMax"/> materials, evenly.</summary>
    [Range(1, 7)]
    public int MaterialsPerProducerMin { get; init; } = 1;

    [Range(1, 7)]
    public int MaterialsPerProducerMax { get; init; } = 6;

    /// <summary>The share of producers that carry a joiner or leaver code.</summary>
    [Range(0.0, 1.0)]
    public double LeaverCodeShare { get; init; }

    /// <summary>The production row count this is sized to, checked by the tests.</summary>
    public int TargetRows { get; init; }
}
