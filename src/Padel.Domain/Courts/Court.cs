using Padel.Domain.Common;

namespace Padel.Domain.Courts;

public class Court : EntityWithDomainEvents
{
    public const int MaxLength = 80;

    private Court(string name, string surface)
    {
        Name = name;
        Surface = surface;
        Status = CourtStatus.Available;

        AddDomainEvent(new CourtCreated(Id));
    }

    private Court()
    {
    }

    public string Name { get; private set; } = null!;
    public string Surface { get; private set; } = null!;
    public CourtStatus Status { get; private set; }

    public bool CanBeBooked => Status == CourtStatus.Available;

    public static Result<Court> Create(string? name, string? surface)
    {
        var normalizedName = CreateName(name);
        if (normalizedName is not string validName)
        {
            return normalizedName.Error;
        }

        var normalizedSurface = CreateSurface(surface);
        if (normalizedSurface is not string validSurface)
        {
            return normalizedSurface.Error;
        }

        return new Court(validName, validSurface);
    }

    public Result Rename(string? name)
    {
        var normalizedName = CreateName(name);
        if (normalizedName is not string validName)
        {
            return normalizedName.Error;
        }

        Name = validName;
        return Success.Instance;
    }

    public Result SetMaintenance(bool isUnderMaintenance)
    {
        if (Status == CourtStatus.Retired)
        {
            return CourtErrors.RetiredCannotChangeMaintenance;
        }

        Status = isUnderMaintenance ? CourtStatus.Maintenance : CourtStatus.Available;
        return Success.Instance;
    }

    /// <summary>Retires the court permanently. Retiring a retired court has no effect.</summary>
    public void Retire()
    {
        if (Status == CourtStatus.Retired)
        {
            return;
        }

        Status = CourtStatus.Retired;
        AddDomainEvent(new CourtRetired(Id));
    }

    /// <summary>Validates and trims a court name.</summary>
    public static Result<string> CreateName(string? value) => Normalize(value, "Court name");

    /// <summary>Validates and trims a surface, e.g. <c>glass</c>.</summary>
    public static Result<string> CreateSurface(string? value) => Normalize(value, "Surface");

    private static Result<string> Normalize(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CourtErrors.Required(label);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            return CourtErrors.TooLong(label);
        }

        return trimmed;
    }
}
