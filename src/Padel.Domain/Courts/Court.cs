using Padel.Domain.Common;

namespace Padel.Domain.Courts;

public enum CourtStatus
{
    Available,
    Maintenance,
    Retired,
}

public class Court : EntityWithDomainEvents
{
    public const int MaxLength = 80;

    public string Name { get; private set; } = null!;
    public string Surface { get; private set; } = null!;
    public CourtStatus Status { get; private set; }

    public Court(string name, string surface)
    {
        Name = Normalize(name, "Court name");
        Surface = Normalize(surface, "Surface");
        Status = CourtStatus.Available;

        AddDomainEvent(new CourtCreated(Id));
    }

    private Court()
    {
    }

    public bool CanBeBooked => Status == CourtStatus.Available;

    public void Rename(string name)
    {
        Name = Normalize(name, "Court name");
    }

    public void SetMaintenance(bool isUnderMaintenance)
    {
        if (Status == CourtStatus.Retired)
        {
            throw new DomainException("A retired court cannot change maintenance state.");
        }

        Status = isUnderMaintenance ? CourtStatus.Maintenance : CourtStatus.Available;
    }

    public void Retire()
    {
        if (Status == CourtStatus.Retired)
        {
            return;
        }

        Status = CourtStatus.Retired;
        AddDomainEvent(new CourtRetired(Id));
    }

    private static string Normalize(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{label} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"{label} cannot be longer than {MaxLength} characters.");
        }

        return trimmed;
    }
}
