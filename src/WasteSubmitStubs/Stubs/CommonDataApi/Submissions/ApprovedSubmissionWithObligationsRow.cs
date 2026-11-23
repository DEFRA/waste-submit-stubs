namespace WasteSubmitStubs.Stubs.CommonDataApi.Submissions;

/// <summary>
/// One row of the response: a submitter, producer and material. Same properties, types and order as
/// epr-common-data-api's <c>ApprovedSubmissionWithObligationsEntity</c>; serialised with web defaults (camelCase,
/// nulls included), as the real API does.
/// </summary>
public sealed record ApprovedSubmissionWithObligationsRow
{
    /// <summary>The period year, as text (for example <c>"2025"</c>).</summary>
    public required string SubmissionPeriod { get; init; }

    public required string PackagingMaterial { get; init; }

    /// <summary>Net weight in whole tonnes, rounded.</summary>
    public required int PackagingMaterialWeight { get; init; }

    /// <summary>The producer's organisation ID.</summary>
    public required Guid OrganisationId { get; init; }

    public required Guid SubmitterId { get; init; }

    /// <summary><c>DirectRegistrant</c> or <c>ComplianceScheme</c>, the only values the procedure returns.</summary>
    public required string SubmitterType { get; init; }

    public short? NumberOfDaysObligated { get; init; }

    /// <summary>Net weight in kilograms.</summary>
    public required long PackagingMaterialWeightKg { get; init; }

    /// <summary><c>O</c> (obligated), <c>N</c> (not obligated) or <c>E</c> (invalid leaver code).</summary>
    public string? ObligationStatus { get; init; }

    /// <summary>The raw registration-file leaver_code, for example <c>"03"</c>.</summary>
    public string? LeaverCode { get; init; }

    public DateTime? JoinerDate { get; init; }

    public DateTime? LeaverDate { get; init; }
}

public static class SubmitterTypes
{
    public const string DirectRegistrant = "DirectRegistrant";
    public const string ComplianceScheme = "ComplianceScheme";
}
