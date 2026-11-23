# epr-common-data-api: approved submissions with obligations

`GET api/submissions/v1/pom/approved-submissions/{periodYear}`

The shape the stub copies, and where it comes from. The real route is not on `main` yet, so this sample was taken from
the code on its branches, not captured from a running service. Capture a live 200 and 204 once the route is deployed
somewhere with synthetic data, and replace the example below.

## Sources

| What | Repo | File | Branch and commit |
| --- | --- | --- | --- |
| Route and status codes | epr-common-data-api | `src/EPR.CommonDataService.Api/Controllers/SubmissionsController.cs` (`GetApprovedSubmissionsWithObligations`) | `SUB-410-approved-submissions-with-obligations` at `1373146` |
| Row properties and types | epr-common-data-api | `src/EPR.CommonDataService.Data/Entities/ApprovedSubmissionWithObligationsEntity.cs` | same |
| Column values, nulls and order | epr-data-sqldb | `dbo/Stored Procedures/sp_GetApprovedSubmissionsMycWithObligations.sql` | `SUB-410-myc-with-obligations` at `94a7780` |
| What the consumer reads | waste-submit-reg-packaging-api | `WasteSubmitRegPackagingApi/Obligations/Services/ApprovedSubmissionWithObligationsRow.cs` | `main` at `bc8d8af` |

## Status codes

| Status | When |
| --- | --- |
| `200` | A JSON array of rows |
| `204` | The year has no approved submissions |
| `400` | `periodYear` isn't a number |
| `500` | The database call failed; the body is the exception message |
| `504` | The database call timed out; the body is the exception message |

## A 200 response

A flat array, ordered by submitter ID, producer ID and material, with the IDs compared as text. Serialised with ASP.NET
web defaults: camelCase, and null values included. Values below are synthetic.

```json
[
  {
    "submissionPeriod": "2025",
    "packagingMaterial": "PL",
    "packagingMaterialWeight": 412,
    "organisationId": "11111111-1111-4111-8111-111111111111",
    "submitterId": "11111111-1111-4111-8111-111111111111",
    "submitterType": "DirectRegistrant",
    "numberOfDaysObligated": 365,
    "packagingMaterialWeightKg": 412350,
    "obligationStatus": "O",
    "leaverCode": null,
    "joinerDate": null,
    "leaverDate": null
  },
  {
    "submissionPeriod": "2025",
    "packagingMaterial": "PC",
    "packagingMaterialWeight": 120,
    "organisationId": "33333333-3333-4333-8333-333333333333",
    "submitterId": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    "submitterType": "ComplianceScheme",
    "numberOfDaysObligated": 180,
    "packagingMaterialWeightKg": 120000,
    "obligationStatus": "O",
    "leaverCode": "03",
    "joinerDate": "2026-07-05T00:00:00",
    "leaverDate": null
  }
]
```

## Field notes

- `submissionPeriod`: the period year as text.
- `packagingMaterialWeight`: net weight in whole tonnes, rounded. `packagingMaterialWeightKg`: the same in kilograms,
  a `bigint`.
- `submitterType`: only ever `DirectRegistrant` or `ComplianceScheme`.
- `organisationId`: the producer. For a direct registrant's own packaging, it equals `submitterId`.
- Obligation fields (`numberOfDaysObligated`, `obligationStatus`, `leaverCode`, `joinerDate`, `leaverDate`) come from
  `t_producer_obligation_determination` for the relevant year (period year plus one). They're the same on every row
  for a producer, and are all null when the producer has no obligation row.
- `obligationStatus`: `O` obligated, `N` not obligated, `E` invalid leaver code.
- `leaverCode`: the raw registration-file value, with or without a leading zero.
- `joinerDate`, `leaverDate`: dates with no time zone.
