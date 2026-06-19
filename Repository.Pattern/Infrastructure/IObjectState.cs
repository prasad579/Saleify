namespace Repository.Pattern.Infrastructure;

/// <summary>Marker for entities whose persistence state can be tracked by the unit of work.</summary>
public interface IObjectState
{
    ObjectState ObjectState { get; set; }
}
