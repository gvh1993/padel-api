namespace Padel.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    /// <summary>
    /// Changes on every save (the database row version). Clients send it back (HTTP <c>If-Match</c>)
    /// to make sure they change the version they have seen.
    /// </summary>
    public uint Version { get; private set; }

    protected Entity(Guid id)
    {
        Id = id;
    }

    protected Entity()
    {
        Id = Guid.CreateVersion7();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType() && Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
