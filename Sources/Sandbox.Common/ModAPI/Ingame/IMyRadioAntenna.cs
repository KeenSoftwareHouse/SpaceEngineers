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
    /// <example>
    /// var antenna = (IMyRadioAntenna) GridTerminalSystem.GetBlockWithName("steve");
    /// </example>
    /// 
    /// Once you have your antenna interface, you can get
    /// a list of all nearby antennas by ID by calling this
    /// function:
    /// <example>
    /// List<long> antennaIds;
    /// antenna.FindNearbyAntennas(ref antennaIds);
    /// </example>
    /// You'll get a list of antenna IDs excluding the ID of
    /// the antenna you're currently using. The returned list
    /// might be empty if no antennas are within your radio
    /// range.
    /// 
    /// You may query information about any existing antenna
    /// in the game. However, you'll only receive actual data
    /// if the antenna of the given ID is within your radio
    /// range.
    /// <example>
    /// // Returns `null` if out of range or `ShowShipName` is disabled.
    /// string otherShipName = antenna.GetNearbyAntennaShipName(42);
    /// </example>
    /// 
    /// An other feature of the nearby antenna patch is the ability to
    /// instantly send or broadcast some data to nearby antennas and
    /// handle those data packs in programmable blocks. However, each
    /// antenna has to enable this feature individually.
    /// <example>
    /// antenna.DataTransferEnabled = true;
    /// </example>
    /// You can then send data to nearby antennas which also have this
    /// feature enabled.
    /// <example>
    /// bool success = antenna.SendToNearbyAntenna(42,"Hello, antenna #42!");
    /// if(!success) {
    ///   antenna.BroadcastToNearbyAntennas("HELP! My bestest buddy #42 is gone!");
    /// }
    /// </example>
    /// Finally, you can read the data packs you received from other
    /// antennas. A data pack is actually just a string.
    /// <example>
    /// long senderId;
    /// string msg = antenna.GetReceivedData(out senderId);
    /// if(msg != null) {
    ///   antenna.BroadcastToNearbyAntennas("Everything's well again. My buddy is back.");
    /// }
    /// </example>
    /// </summary>
    /// <remarks>
    /// You may wonder why this patch does not just return some interface of
    /// nearby ships which can give you all the information directly rather
    /// than querying it through the current antenna's interface.
    /// 
    /// The reason for this is simple: Security and balancing.
    /// 
    /// If it'd pass an interface, you could save this interface for later use.
    /// Once grabbed within your antenna's reach, you could then exchange data
    /// far beyond any antenna's radius, thus breaking the range balancing.
    /// Additionally, you could use any of such interfaces to quickly gain control
    /// over the other ship's whole grid, which is a dead serious security leak.
    /// </remarks>
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

        #region Nearby Antenna Patch
        // TODO Patch if the JSON storage patch will make it into the master branch.
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

        /// <summary>
        /// Get this antenna's ship's ID.
        /// </summary>
        long ShipId { get; }
        
        /// <summary>
        /// Configure whether this antenna is able to send
        /// and receive data from or to other antennas.
        /// 
        /// Default value is <code>false</code>.
        /// </summary>
        /// <remarks>
        /// Disabling data transfer discards all pending
        /// messages from other antennas. Thus you may
        /// want to ensure that there are no pending
        /// messages, before you disable data transfer.
        /// </remarks>
        bool DataTransferEnabled { get; set; }
        
        /// <summary>
        /// Tries to send data to a nearby antenna.
        /// 
        /// Sending data will fail if the target antenna is out of
        /// your antenna's range or if this antenna or the target
        /// antenna have data transfer disabled.
        /// </summary>
        /// <remarks>
        /// It does not check whether antennas have broadcasting
        /// enabled, as broadcasting is used to share HUD markers,
        /// which can quickly become very annoying in a complicated
        /// sattelite network.
        /// </remarks>
        /// <param name="antennaId">The target antenna's ID.</param>
        /// <param name="data">The data to send.</param>
        /// <returns><code>false</code> if the target antenna is unreachable.</returns>
        bool SendToNearbyAntenna(long antennaId, string data);
        
        /// <summary>
        /// Broadcasts a message to all nearby antennas, which may be
        /// used for SOS signals and such.
        /// 
        /// Broadcasting data will fail if this antenna has data transfer
        /// disabled. To receive the broadcasted data, a nearby antenna
        /// has to be within this antenna's radio range and must have data
        /// transfer enabled.
        /// </summary>
        /// <remarks>
        /// This function checks all existing antennas to find the ones
        /// matching the data receiving criteria, thus, in a world with
        /// many antennas, this method can become very slow.
        /// 
        /// If you have to broadcast data very often, it is highly
        /// advised to manually send data to previously found antenna IDs.
        /// </remarks>
        /// <param name="data">The data to broadcast.</param>
        void BroadcastToNearbyAntennas(string data);
        
        /// <summary>
        /// Returns one pack of transfered data at a time. If there is no
        /// data to be read, returns <code>null</code>. In that case,
        /// <code>antennaId</code> will contain nothing but rubbish.
        /// </summary>
        /// <remarks>
        /// A call to this function removes the returned piece of data
        /// from the antenna's input queue. Thus, be very careful about
        /// concurrent access to an antenna.
        /// </remarks>
        /// <param name="antennaId">Receives the sender's ID.</param>
        /// <returns>Some sent data or <code>null</code>.</returns>
        string GetReceivedData(out long antennaId);
        
        /// <summary>
        /// This antenna's radio range within which further information
        /// about a nearby ship can be gathered, e.g. it's mass.
        /// </summary>
        /// <remarks>This detail range is about 10% of the full radio range.</remarks>
        float DetailScanRange { get; }
        
        /// <summary>
        /// Fills a list with IDs of antennas
        /// within the reach of this antenna.
        /// 
        /// In a world with many antennas, this method can
        /// be very slow, as this method checks all existing
        /// antennas. It is thus highly advised to search
        /// nearby antennas only once in a while and cache
        /// the found IDs for frequent use.
        /// </summary>
        /// <param name="antennaIds">
        /// Appends all found antenna IDs to this list.
        /// Does not check the given list's current contents.
        /// Also does not discard the list's current contents.
        /// If the reference is <code>null</code>, assigns a
        /// newly created list to it.
        /// </param>
        void FindNearbyAntennas(ref List<long> antennaIds);
        
        /// <summary>
        /// Fills a list with IDs of antennas within the
        /// reach of this antenna.
        /// 
        /// This method only picks one antenna per ship,
        /// which is slower than just getting all the
        /// nearby antennas.
        /// </summary>
        /// <param name="found">
        /// Appends all found antenna IDs to this list.
        /// Does not check the given list's current contents,
        /// which includes checking for double entries or
        /// multiple antennas from the same ship. Also does
        /// not discard the list's current contents. If the
        /// reference is <code>null</code>, assigns a newly
        /// created list to it.
        /// </param>
        /// <remarks>
        /// If a ship has multiple antennas installed, those
        /// working and having data transfer enabled will be
        /// preferred. If there are multiple antennas working
        /// and having data transfer enabled, any of those
        /// will be picked randomly. If no antennas fulfill
        /// these criteria, any antenna will be picked randomly.
        /// 
        /// I.e.: If a ship has working data transfering antennas,
        /// you'll get a data transfering antenna's ID.
        /// </remarks>
        void FindDistinctNearbyAntennas(ref List<long> found);

        /// <summary>
        /// Checks whether the specific antenna is within
        /// this antenna's radio range.
        /// </summary>
        /// <param name="antennaId">The ID of the antenna to check.</param>
        /// <returns><code>true</code> if the nearby antenna is in reach.</returns>
        bool IsNearbyAntennaInReach(long antennaId);
        
        /// <summary>
        /// Are this and the nearby antenna within each other's radio reach?
        /// 
        /// In case you want the target antenna to respond to your sent messages,
        /// you may want to check this method, first. Otherwise you may find
        /// yourself sending a bunch of data and waiting, like, forever for an
        /// answer.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>true</code> if both antennas are within each other's reach.</returns>
        bool IsNearbyAntennaInResponseReach(long antennaId);
        
        /// <summary>
        /// Returns the nearby antenna's radio reach if it is within
        /// this antenna's radio reach.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if the other antenna is out of reach.</returns>
        float? GetNearbyAntennaRadius(long antennaId);
        
        /// <summary>
        /// Get the nearby antenna's ship's ID. Use this to only detect one
        /// antenna per ship, no matter how many antennas a ship may have.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns>A unique ship ID or <code>null</code> if out of reach.</returns>
        long? GetNearbyAntennaShipId(long antennaId);

        /// <summary>
        /// Returns the name of the nearby antenna's ship, if showing
        /// the ship's name is enabled.
        /// </summary>
        /// <param name="antennaId">The ID of the nearby antenna.</param>
        /// <returns><code>null</code> if the other antenna is out of reach.</returns>
        string GetNearbyAntennaShipName(long antennaId);
        
        /// <summary>
        /// Get the nearby antenna's position if the nearby antenna
        /// is within your antenna's reach.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if the other antenna is unreachable.</returns>
        Vector3D? GetNearbyAntennaPosition(long antennaId);
        
        /// <summary>
        /// Get the nearby antenna's owner ID if the nearby antenna
        /// is within your antenna's reach.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns>The nearby antenna's owner's ID or <code>null</code>.</returns>
        long? GetNearbyAntennaOwnerId(long antennaId);
        
        /// <summary>
        /// Get the player's relation to the nearby antenna's owner.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if the other antenna is unreachable.</returns>
        MyRelationsBetweenPlayerAndBlock? GetNearbyAntennaPlayerRelationToOwner(long antennaId);
        
        /// <summary>
        /// Get any player's relation to the nearby antenna's owner.
        /// </summary>
        /// <param name="antennaId">The ID of the nearby antenna.</param>
        /// <param name="playerId">The ID of the player to check.</param>
        /// <returns><code>null</code> if the nearby antenna is unreachable.</returns>
        MyRelationsBetweenPlayerAndBlock? GetNearbyAntennaUserRelationToOwner(long antennaId, long playerId);
        
        /// <summary>
        /// Get the nearby antenna's cube size if it is within
        /// this antenna's detail scan range.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if the nearby antenna is out of detail scan range.</returns>
        MyCubeSize? GetNearbyAntennaCubeSize(long antennaId);
        
        /// <summary>
        /// Check whether the nearby antenna belongs to a station (static)
        /// or a ship, if it is within this antenna's detail scan range.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if out of detail scan range.</returns>
        bool? GetNearbyAntennaIsStatic(long antennaId);
        
        /// <summary>
        /// Get the nearby antenna's ship's mass, if it is within this
        /// antenna's detail scan range.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if out of detail scan range.</returns>
        float? GetNearbyAntennaMass(long antennaId);
        
        /// <summary>
        /// Gets the nearby antenna's ship's bounding sphere. Useful
        /// for collision avoidance.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if out of detail scan range.</returns>
        BoundingSphereD? GetNearbyAntennaWorldVolume(long antennaId);
        
        /// <summary>
        /// Gets the nearby antenna's ship's world AABB. Useful
        /// for collision avoidance.
        /// </summary>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns><code>null</code> if out of detail scan range.</returns>
        BoundingBoxD? GetNearbyAntennaWorldAABB(long antennaId);
        
        /// <summary>
        /// Counts all the blocks on the nearby antenna's ship of
        /// a specific type. Does not check whether these blocks
        /// are functional or active. The nearby antenna has to be
        /// within your detail scan range.
        /// </summary>
        /// <typeparam name="T">The block type, e.g. <code>IMyProgrammableBlock</code>.</typeparam>
        /// <param name="antennaId">The nearby antenna's ID.</param>
        /// <returns>The number of blocks of the given type or <code>null</code>.</returns>
        int? CountNearbyBlocksOfType<T>(long antennaId) where T : IMyCubeBlock;

        #endregion // Nearby Antenna Patch
    }
}
