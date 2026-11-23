using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace WasteSubmitStubs.Logging;

[ExcludeFromCodeCoverage]
public class TraceHeader
{
    [ConfigurationKeyName("TraceHeader")]
    [Required]
    public required string Name { get; set; }
}
