using Sandbox.Common;
using Sandbox.Common.ModAPI.Ingame;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Antenna block interface
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
        /// Returns a list of IDs of broadcasting antennas
        /// within the reach of this antenna.
        /// </summary>
        /// <returns>This list might be empty.</returns>
        List<long> FindNearbyAntennas();
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
        /// Returns the name of the nearby antenna's ship.
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
	}
}
