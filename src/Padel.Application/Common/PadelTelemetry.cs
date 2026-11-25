using System.Diagnostics;

namespace Padel.Application.Common;

public static class PadelTelemetry
{
    public const string ActivitySourceName = "Padel.Application";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
