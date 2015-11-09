using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using System.Collections.Generic;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Antenna block interface
    /// 
    /// Added "Nearby Antenna Feature".
    /// This is a programmable block only feature. To
    /// use it, your programmable block has to grab the
    /// interface of an antenna on the same ship. How
    /// you get this interface is up to you. By name, by
    /// position,...
    /// <code>
    /// IMyRadioAntenna antenna = ...;
    /// </code>
    /// 
    /// Once you have your antenna interface, you can get
    /// a list of all nearby antennae by ID by calling this
    /// function:
    /// <code>
    /// var nearbyAntennaIds = antenna.FindNearbyAntennas();
    /// </code>
    /// You'll get a list of antenna IDs excluding the ID of
    /// the antenna you're currently using. The returned list
    /// might be empty if no antennas are within your radio
    /// range.
    /// 
    /// You may query information about any existing antenna
    /// in the game. However, you'll only receive actual data
    /// if the antenna of the given ID is within your radio
    /// range.
    /// <code>
    /// // Returns `null` if out of range or `ShowShipName` is disabled.
    /// string otherShipName = antenna.GetNearbyAntennaShipName(42);
    /// </code>
    /// 
    /// An other feature of the nearby antenna patch is the ability to
    /// instantly send or broadcast some data to nearby antennas and
    /// handle those data packs in programmable blocks. However, each
    /// antenna has to enable this feature individually.
    /// <code>
    /// antenna.DataTransferEnabled = true;
    /// </code>
    /// You can then send data to nearby antennas which also have this
    /// feature enabled.
    /// <code>
    /// bool success = antenna.SendToNearbyAntenna(42,"Hello, antenna #42!");
    /// if(!success) {
    ///   antenna.BroadcastToNearbyAntennas("HELP! My bestest buddy #42 is gone!");
    /// }
    /// </code>
    /// Finally, you can read the data packs you received from other
    /// antennas. A data pack is a string which has the sending antenna's
    /// ID appended to the front end. E.g. if antenna #42 sent you a message,
    /// this message will start with the substring ":42;".
    /// <code>
    /// string msg = antenna.GetReceivedData();
    /// if(msg != null && msg.StartsWith(":42;")) {
    ///   antenna.BroadcastToNearbyAntennas("Everything's well again. My buddy is back.");
    /// }
    /// </code>
    /// </summary>
    public interface IMyRadioAntenna : IMyFunctionalBlock
    {
        /// <summary>
        /// Broadcasting/Receiving range (read-only)
        /// </summary>
        float Radius {get; }

        /// <summary>
        /// Show shipname on hud (read-only)
        /// </summary>
        bool ShowShipName { get; }

        /// <summary>
        /// Returns true if antena is broadcasting (read-only)
        /// </summary>
        bool IsBroadcasting { get; }
        
        /*
        Nearby Antenna Patch!
        ---------------------
        Created by "Evrey".

        Allowes to scan for other antennas by script.
        */
        //
        /// <summary>
        /// Returns the antenna's unique ID.
        /// </summary>
        /// <remarks>
        /// This actually just returns the antenna's
        /// <code>EntityId</code>, but I think that this
        /// alias gives people a better hint on where
        /// to get the IDs for the patch-functions from
        /// and which IDs won't make sense there.
        /// </remarks>
        long AntennaId { get; }
        //
        /// <summary>
        /// Configure whether this antenna is able to send
        /// and receive data from or to other antennas.
        /// 
        /// Default value is <code>false</code>.
        /// </summary>
        bool DataTransferEnabled { get; set; }
        //
        /// <summary>
        /// Tries to send data to a nearby antenna.
        /// The string <code>":42;"</code>, where 42 will be this
        /// antenna's ID, will be appended to the data's beginning.
        /// Thus, programmable blocks can find out who sent the data.
        /// 
        /// Sending data will fail if the target antenna is out of
        /// your antenna's range or if this antenna has broadcasting
        /// disabled. It will also fail if this antenna or the target
        /// antenna have data transfer disabled.
        /// </summary>
        /// <param name="antennaId">The target antenna's ID.</param>
        /// <param name="data">The data to send.</param>
        /// <returns><code>false</code> if the target antenna is unreachable.</returns>
        bool SendToNearbyAntenna(long antennaId, string data);
        //
        /// <summary>
        /// Broadcasts a message to all nearby antennas, which may be
        /// used for SOS signals and such.
        /// 
        /// Broadcasting data will fail if this antenna has data transfer
        /// disabled. To receive the broadcasted data, a nearby antenna
        /// has to be within this antenna's radio range, must have broadcast
        /// enabled, and must have data transfer enabled.
        /// </summary>
        /// <param name="data">The data to broadcast.</param>
        void BroadcastToNearbyAntennas(string data);
        //
        /// <summary>
        /// Returns one pack of transfered data at a time. If there is no
        /// data to be read, returns <code>null</code>. All data strings
        /// begin with the substring <code>":42;"</code>, where 42 will be
        /// the ID of the antenna which sent this piece of data to you.
        /// </summary>
        /// <remarks>
        /// A call to this function removes the returned piece of data
        /// from the antenna's input queue. Thus, be very careful about
        /// concurrent access to an antenna.
        /// </remarks>
        /// <returns><code>null</code> if there is no data available.</returns>
        string GetReceivedData();
        //
        /// <summary>
        /// This antenna's radio range within which further information
        /// about a nearby ship can be gathered, e.g. it's mass.
        /// </summary>
        /// <remarks>This detail range is about 10% of the full radio range.</remarks>
        float DetailScanRange { get; }
        //
        /// <summary>
        /// Fills a list with IDs of broadcasting antennas
        /// within the reach of this antenna.
        /// </summary>
        /// <param name="antennaIds">
        /// Appends all found antenna IDs to this list.
        /// Must not be <code>null</code>.
        /// </param>
        void FindNearbyAntennas(ref List<long> antennaIds);
        //
        /// <summary>
        /// Checks whether the specific antenna is within
        /// this antenna's radio range.
        /// </summary>
        /// <param name="antennaId">The ID of the antenna to check.</param>
        /// <returns><code>true</code> if the nearby antenna is in reach.</returns>
        bool IsNearbyAntennaInReach(long antennaId);
        //
        /// <summary>
        /// Are this and the nearby antenna within each other's radio reach?
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>true</code> if both antennas are within each other's reach.</returns>
        bool IsNearbyAntennaInResponseReach(long antennaId);
        //
        /// <summary>
        /// Returns the nearby antenna's radio reach if it is within
        /// this antenna's radio reach and if the other antenna is
        /// broadcasting.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if the other antenna is out of reach.</returns>
        float? GetNearbyAntennaRadius(long antennaId);
        //
        /// <summary>
        /// Returns the name of the nearby antenna's ship, if showing
        /// the ship's name is enabled on the nearby antenna.
        /// </summary>
        /// <param name="antennaId">The ID of the nearby antenna.</param>
        /// <returns><code>null</code> if the other antenna is not broadcasting or out of reach.</returns>
        string GetNearbyAntennaShipName(long antennaId);
        //
        /// <summary>
        /// Get the nearby antenna's position if the nearby antenna
        /// is broadcasting and within your antenna's reach.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if the other antenna is unreachable.</returns>
        Vector3D? GetNearbyAntennaPosition(long antennaId);
        //
        /// <summary>
        /// Get the nearby antenna's owner ID if the nearby antenna
        /// is broadcasting and within your antenna's reach.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns>The nearby antenna's owner's ID or <code>null</code>.</returns>
        long? GetNearbyAntennaOwnerId(long antennaId);
        //
        /// <summary>
        /// Get the player's relation to the nearby antenna's owner.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if the other antenna is unreachable.</returns>
        MyRelationsBetweenPlayerAndBlock? GetNearbyAntennaPlayerRelationToOwner(long antennaId);
        //
        /// <summary>
        /// Get any player's relation to the nearby antenna's owner.
        /// </summary>
        /// <param name="antennaId">The ID of the nearby antenna.</param>
        /// <param name="playerId">The ID of the player to check.</param>
        /// <returns><code>null</code> if the nearby antenna is unreachable.</returns>
        MyRelationsBetweenPlayerAndBlock? GetNearbyAntennaUserRelationToOwner(long antennaId, long playerId);
        //
        /// <summary>
        /// Get the nearby antenna's cube size if it is within
        /// this antenna's detail scan range.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if the nearby antenna is out of detail scan range.</returns>
        MyCubeSize? GetNearbyAntennaCubeSize(long antennaId);
        //
        /// <summary>
        /// Check whether the nearby antenna belongs to a station (static)
        /// or a ship, if it is within this antenna's detail scan range.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if out of detail scan range.</returns>
        bool? GetNearbyAntennaIsStatic(long antennaId);
        //
        /// <summary>
        /// Get the nearby antenna's ship's mass, if it is within this
        /// antenna's detail scan range.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if out of detail scan range.</returns>
        float? GetNearbyAntennaMass(long antennaId);
        //
        /// <summary>
        /// Gets the nearby antenna's ship's bounding sphere. Useful
        /// for collision avoidance.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if out of detail scan range.</returns>
        BoundingSphereD? GetNearbyAntennaWorldVolume(long antennaId);
        //
        /// <summary>
        /// Gets the nearby antenna's ship's world AABB. Useful
        /// for collision avoidance.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if out of detail scan range.</returns>
        BoundingBoxD? GetNearbyAntennaWorldAABB(long antennaId);
        //
        // TODO Find broadcasted raw material deposits. => MyOreDetectorComponent.Update
        // TODO Find broadcasting beacons.
	}
}
