using WasteSubmitStubs.Stubs.CommonDataApi.Submissions;

namespace WasteSubmitStubs.Datasets;

/// <summary>
/// Generates a year of synthetic approved-submission rows at production size. The same year and sizes always give
/// the same rows: IDs are name-based UUIDs and the random values come from a generator seeded by the year.
/// </summary>
/// <remarks>
/// This copies the shape of the real response, not the procedure's rules. Each producer gets one obligation row
/// (as the procedure keeps), and joiner and leaver dates fall in the relevant year, the period year plus one.
/// </remarks>
public static class ApprovedSubmissionsGenerator
{
    private static readonly string[] Materials = ["AL", "FC", "GL", "PC", "PL", "ST", "WD"];

    private static readonly string[] JoinerCodes = ["01", "02", "03", "07", "09", "15", "17", "19", "20"];

    private static readonly string[] LeaverCodes = ["04", "05", "06", "08", "10", "11", "12", "13", "14", "16", "21"];

    public static IReadOnlyList<ApprovedSubmissionWithObligationsRow> Generate(int periodYear, GeneratedSizes sizes)
    {
        var random = new Random(periodYear);
        var relevantYear = periodYear + 1;
        var daysInYear = (short)(DateTime.IsLeapYear(relevantYear) ? 366 : 365);
        var rows = new List<ApprovedSubmissionWithObligationsRow>(sizes.TargetRows + 1024);
        var producerIndex = 0;

        void AddProducer(Guid submitterId, string submitterType, Guid producerId)
        {
            var obligation = random.NextDouble() < sizes.LeaverCodeShare
                ? JoinedOrLeft(random, relevantYear, daysInYear)
                : (Days: daysInYear, Code: (string?)null, Joined: (DateTime?)null, Left: (DateTime?)null);

            // An even spread of material counts, and a rotating start, so every material is used.
            var span = sizes.MaterialsPerProducerMax - sizes.MaterialsPerProducerMin + 1;
            var count = sizes.MaterialsPerProducerMin + producerIndex % span;
            var first = producerIndex * 3 % Materials.Length;
            producerIndex++;

            for (var m = 0; m < count; m++)
            {
                var kg = WeightKg(random);
                rows.Add(new ApprovedSubmissionWithObligationsRow
                {
                    SubmissionPeriod = periodYear.ToString(),
                    PackagingMaterial = Materials[(first + m) % Materials.Length],
                    PackagingMaterialWeight = (int)Math.Round(kg / 1000.0, MidpointRounding.AwayFromZero),
                    OrganisationId = producerId,
                    SubmitterId = submitterId,
                    SubmitterType = submitterType,
                    NumberOfDaysObligated = obligation.Days,
                    PackagingMaterialWeightKg = kg,
                    ObligationStatus = "O",
                    LeaverCode = obligation.Code,
                    JoinerDate = obligation.Joined,
                    LeaverDate = obligation.Left
                });
            }
        }

        for (var d = 0; d < sizes.DirectRegistrants; d++)
        {
            var submitterId = Id(periodYear, $"direct-registrant/{d}");
            AddProducer(submitterId, SubmitterTypes.DirectRegistrant, submitterId);

            if (random.NextDouble() < sizes.DirectRegistrantSubsidiaryShare)
            {
                AddProducer(submitterId, SubmitterTypes.DirectRegistrant, Id(periodYear, $"direct-registrant/{d}/subsidiary"));
            }
        }

        var schemeSizes = SchemeSizes(sizes);
        for (var s = 0; s < schemeSizes.Count; s++)
        {
            var schemeId = Id(periodYear, $"compliance-scheme/{s}");
            for (var p = 0; p < schemeSizes[s]; p++)
            {
                AddProducer(schemeId, SubmitterTypes.ComplianceScheme, Id(periodYear, $"compliance-scheme/{s}/producer/{p}"));
            }
        }

        return rows;
    }

    /// <summary>
    /// The largest scheme gets its measured size. The rest are spread evenly from an eighth of their average to just
    /// under twice it, so the overall average matches.
    /// </summary>
    public static IReadOnlyList<int> SchemeSizes(GeneratedSizes sizes)
    {
        if (sizes.ComplianceSchemes == 0)
        {
            return [];
        }

        var result = new List<int> { sizes.LargestSchemeProducers };
        var others = sizes.ComplianceSchemes - 1;
        if (others == 0)
        {
            return result;
        }

        var averageOfOthers = ((double)sizes.ProducersPerSchemeAverage * sizes.ComplianceSchemes - sizes.LargestSchemeProducers) / others;
        var smallest = Math.Max(1, Math.Round(averageOfOthers / 8));
        var largest = Math.Max(smallest, 2 * averageOfOthers - smallest);

        for (var i = 0; i < others; i++)
        {
            var step = others == 1 ? 0.5 : (double)i / (others - 1);
            result.Add((int)Math.Round(smallest + (largest - smallest) * step));
        }

        return result;
    }

    private static Guid Id(int periodYear, string name) =>
        DeterministicGuid.Create($"epr-common-data-api/approved-submissions/{periodYear}/{name}");

    /// <summary>Spread over several orders of magnitude, from 500 kg to 5,000 tonnes, as large producers are.</summary>
    private static long WeightKg(Random random) =>
        (long)Math.Round(500 * Math.Pow(10_000, random.NextDouble()));

    private static (short Days, string? Code, DateTime? Joined, DateTime? Left) JoinedOrLeft(Random random, int year, short daysInYear)
    {
        var date = new DateTime(year, 1, 1).AddDays(random.Next(1, daysInYear - 1));

        if (random.Next(2) == 0)
        {
            return ((short)(daysInYear - date.DayOfYear + 1), JoinerCodes[random.Next(JoinerCodes.Length)], date, null);
        }

        return ((short)date.DayOfYear, LeaverCodes[random.Next(LeaverCodes.Length)], null, date);
    }
}
