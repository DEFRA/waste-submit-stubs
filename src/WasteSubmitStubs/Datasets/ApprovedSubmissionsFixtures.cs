using WasteSubmitStubs.Stubs.CommonDataApi.Submissions;

namespace WasteSubmitStubs.Datasets;

/// <summary>
/// The small hand-written dataset for exact-value checks. It keeps the IDs and weights of the packaging API's
/// <c>ApprovedSubmissionsFixtures</c>: direct registrant <c>1111…</c>, compliance scheme <c>aaaa…</c> with four
/// members, and <c>bbbb…</c>, a known submitter with no rows. All of it is synthetic.
/// </summary>
/// <remarks>
/// The real procedure keeps one obligation row per producer, so each producer has at most one leaver code. Producer
/// <c>5555…</c> joined, resigned and rejoined in the packaging API's fixtures; here it carries only its latest
/// event, a rejoin. Joiner and leaver dates fall in the relevant year, the period year plus one.
/// </remarks>
public static class ApprovedSubmissionsFixtures
{
    public static Guid DirectRegistrantId { get; } = Guid.Parse("11111111-1111-4111-8111-111111111111");

    public static Guid ComplianceSchemeId { get; } = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    public static Guid NoSubmissionsId { get; } = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");

    public static Guid FullYearMemberId { get; } = Guid.Parse("22222222-2222-4222-8222-222222222222");

    public static Guid JoinerId { get; } = Guid.Parse("33333333-3333-4333-8333-333333333333");

    public static Guid LeaverId { get; } = Guid.Parse("44444444-4444-4444-8444-444444444444");

    public static Guid RejoinerId { get; } = Guid.Parse("55555555-5555-4555-8555-555555555555");

    public static IReadOnlyList<ApprovedSubmissionWithObligationsRow> ForYear(int periodYear)
    {
        var relevantYear = periodYear + 1;
        var rows = new List<ApprovedSubmissionWithObligationsRow>();

        void Add(Guid submitterId, string submitterType, Guid producerId, Obligation obligation, params (string Material, long Kg)[] materials)
        {
            rows.AddRange(materials.Select(material => new ApprovedSubmissionWithObligationsRow
            {
                SubmissionPeriod = periodYear.ToString(),
                PackagingMaterial = material.Material,
                PackagingMaterialWeight = (int)Math.Round(material.Kg / 1000.0, MidpointRounding.AwayFromZero),
                OrganisationId = producerId,
                SubmitterId = submitterId,
                SubmitterType = submitterType,
                NumberOfDaysObligated = obligation.Days,
                PackagingMaterialWeightKg = material.Kg,
                ObligationStatus = "O",
                LeaverCode = obligation.LeaverCode,
                JoinerDate = obligation.JoinerDate,
                LeaverDate = obligation.LeaverDate
            }));
        }

        var fullYear = Obligation.FullYear(relevantYear);

        Add(DirectRegistrantId, SubmitterTypes.DirectRegistrant, DirectRegistrantId, fullYear,
            ("PL", 412350), ("PC", 655120), ("GL", 167800));

        Add(ComplianceSchemeId, SubmitterTypes.ComplianceScheme, FullYearMemberId, fullYear,
            ("PL", 100000), ("WD", 50000), ("AL", 25000), ("ST", 40000), ("PC", 150000), ("GL", 200000), ("FC", 30000));

        // Joined the scheme part-way through the relevant year.
        Add(ComplianceSchemeId, SubmitterTypes.ComplianceScheme, JoinerId, Obligation.Joined(relevantYear, "03", 7, 5),
            ("PL", 75000), ("PC", 120000));

        // Resigned from the scheme part-way through the relevant year.
        Add(ComplianceSchemeId, SubmitterTypes.ComplianceScheme, LeaverId, Obligation.Left(relevantYear, "13", 9, 30),
            ("GL", 64000));

        // Rejoined the scheme: only the latest event survives the procedure's one-row-per-producer rule.
        Add(ComplianceSchemeId, SubmitterTypes.ComplianceScheme, RejoinerId, Obligation.Joined(relevantYear, "01", 10, 1),
            ("PL", 40000), ("GL", 22000));

        return rows;
    }

    private sealed record Obligation(short Days, string? LeaverCode, DateTime? JoinerDate, DateTime? LeaverDate)
    {
        private static short DaysInYear(int year) => (short)(DateTime.IsLeapYear(year) ? 366 : 365);

        public static Obligation FullYear(int year) => new(DaysInYear(year), null, null, null);

        public static Obligation Joined(int year, string code, int month, int day)
        {
            var date = new DateTime(year, month, day);
            return new((short)(DaysInYear(year) - date.DayOfYear + 1), code, date, null);
        }

        public static Obligation Left(int year, string code, int month, int day)
        {
            var date = new DateTime(year, month, day);
            return new((short)date.DayOfYear, code, null, date);
        }
    }
}
