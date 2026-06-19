namespace Repository.Pattern.Infrastructure;

/// <summary>Base class for trackable domain entities.</summary>
public abstract class Entity : IObjectState
{
    public ObjectState ObjectState { get; set; } = ObjectState.Unchanged;
}
