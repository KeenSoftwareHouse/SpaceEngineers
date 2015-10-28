using VRageMath;
using Sandbox.Common;

namespace Sandbox.Common.ModAPI.Ingame {
  /// <summary>
  /// This interface provides you with some information
  /// about nearby antennas. You may use it to scan for
  /// enemy ships or to exchange data with friendly ones.
  /// </summary>
  public interface IMyNearbyAntenna {
    /// <summary>
    /// Returns the nearby antenna's unique ID.
    /// </summary>
    long AntennaId { get; }
    //
    /// <summary>
    /// Returns the nearby antenna's radio range.
    /// The returned value might be smaller than your
    /// own antenna's radio range but not bigger, as you
    /// only get data from nearby antennas within your
    /// own reach.
    /// </summary>
    float Radius { get; }
    //
    /// <summary>
    /// Checks whether the nearby antenna is broadcasting.
    /// </summary>
    /// <remarks>
    /// You only receive a <code>ShipName</code> etc.,
    /// if the nearby antenna is broadcasting.
    /// </remarks>
    bool IsBroadcasting { get; }
    //
    /// <summary>
    /// Returns the name of the nearby antenna's ship.
    /// </summary>
    /// <remarks>
    /// The returned value is <code>null</code>, if the
    /// nearby antenna is not broadcasting.
    /// </remarks>
    string ShipName { get; }
    //
    /// <summary>
    /// Returns the nearby antenna's position.
    /// </summary>
    Vector3D GetPosition();
    //
    /// <summary>
    /// Returns the ID of the nearby antenna's owner.
    /// </summary>
    /// <remarks>
    /// Returns <code>null</code> if the nearby antenna is not broadcasting.
    /// </remarks>
    long? OwnerId { get; }
    //
    /// <summary>
    /// Returns the relation of the nearby antenna's owner to the player.
    /// </summary>
    /// <returns><code>null</code> if the nearby antenna is not broadcasting.</returns>
    MyRelationsBetweenPlayerAndBlock? GetPlayerRelationToOwner();
    //
    /// <summary>
    /// Returns the relation of the nearby antenna's owner to any other player.
    /// </summary>
    /// <param name="playerId">The other player's ID.</param>
    /// <returns><code>null</code> if the nearby antenna is not broadcasting.</returns>
    MyRelationsBetweenPlayerAndBlock? GetUserRelationToOwner(long playerId);
  }
}
