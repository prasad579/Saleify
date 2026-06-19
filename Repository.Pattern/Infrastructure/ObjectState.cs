namespace Repository.Pattern.Infrastructure;

/// <summary>
/// Tracks the persistence state of an entity. Mirrors the change-tracking marker used by the
/// classic SaaSify Repository.Pattern, adapted for a plain .NET 9 in-memory store.
/// </summary>
public enum ObjectState
{
    Unchanged,
    Added,
    Modified,
    Deleted
}
