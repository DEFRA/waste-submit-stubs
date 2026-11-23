using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WasteSubmitStubs.Datasets;

namespace WasteSubmitStubs.Stubs.CommonDataApi.Submissions;

/// <summary>What one period year returns: its profile, and the serialised body when it has rows.</summary>
public sealed record YearResponse(int Year, DatasetProfile Profile, string? Sizes, byte[]? Body, int RowCount, IReadOnlyList<SubmitterSummary> Submitters)
{
    public int Bytes => Body?.Length ?? 0;
}

/// <summary>One submitter in a year, for test data discovery.</summary>
public sealed record SubmitterSummary(Guid Id, string Type, int Producers, int Rows);

/// <summary>
/// Builds every configured year's response once, when the stub starts, and keeps the serialised bytes. Every
/// request then serves the same bytes, so the stub adds no cost of its own to a measurement.
/// </summary>
public sealed class ApprovedSubmissionsCatalogue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<int, YearResponse> _years;

    public ApprovedSubmissionsCatalogue(IOptions<CommonDataApiOptions> options)
    {
        var settings = options.Value.ApprovedSubmissions;
        _years = settings.Years.ToDictionary(entry => entry.Key, entry => Build(entry.Key, entry.Value, settings.Sizes));
    }

    public IReadOnlyCollection<YearResponse> Years => _years.Values;

    public YearResponse ForYear(int year) =>
        _years.TryGetValue(year, out var response) ? response : new YearResponse(year, DatasetProfile.Empty, null, null, 0, []);

    private static YearResponse Build(int year, YearOptions options, IReadOnlyDictionary<string, GeneratedSizes> sizes)
    {
        IReadOnlyList<ApprovedSubmissionWithObligationsRow> rows = options.Profile switch
        {
            DatasetProfile.Fixtures => ApprovedSubmissionsFixtures.ForYear(year),
            DatasetProfile.Generated => ApprovedSubmissionsGenerator.Generate(year, SizesFor(year, options, sizes)),
            _ => []
        };

        // The real route orders by submitter, producer and material, comparing the IDs as text.
        var ordered = rows
            .OrderBy(row => row.SubmitterId.ToString(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.OrganisationId.ToString(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.PackagingMaterial, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var body = ordered.Count == 0 ? null : JsonSerializer.SerializeToUtf8Bytes(ordered, JsonOptions);
        var submitters = ordered
            .GroupBy(row => (row.SubmitterId, row.SubmitterType))
            .Select(group => new SubmitterSummary(group.Key.SubmitterId, group.Key.SubmitterType,
                group.Select(row => row.OrganisationId).Distinct().Count(), group.Count()))
            .ToList();
        var sizesName = options.Profile == DatasetProfile.Generated ? options.Sizes : null;
        return new YearResponse(year, options.Profile, sizesName, body, ordered.Count, submitters);
    }

    private static GeneratedSizes SizesFor(int year, YearOptions options, IReadOnlyDictionary<string, GeneratedSizes> sizes)
    {
        if (options.Sizes is null || !sizes.TryGetValue(options.Sizes, out var found))
        {
            throw new OptionsValidationException(nameof(CommonDataApiOptions), typeof(CommonDataApiOptions),
                [$"CommonDataApi:ApprovedSubmissions:Years:{year} is Generated but its Sizes '{options.Sizes}' isn't defined under Sizes."]);
        }

        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(found, new ValidationContext(found), errors, validateAllProperties: true)
            || found.MaterialsPerProducerMin > found.MaterialsPerProducerMax)
        {
            throw new OptionsValidationException(nameof(CommonDataApiOptions), typeof(CommonDataApiOptions),
                [$"CommonDataApi:ApprovedSubmissions:Sizes:{options.Sizes} is invalid: " +
                 string.Join("; ", errors.Select(error => error.ErrorMessage).DefaultIfEmpty("MaterialsPerProducerMin is above MaterialsPerProducerMax"))]);
        }

        return found;
    }
}
