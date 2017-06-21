using System;
using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IMyGps
    {
        /// <summary>
        /// The GPS entry id hash which is generated using the GPS name and coordinates.
        /// </summary>
        int Hash { get; }

        /// <summary>
        /// Updates the hash id if you've changed the name or the coordinates.
        /// NOTE: Do not use this if you plan on using this object to update existing GPS entries.
        /// </summary>
        void UpdateHash();

        string Name { get; set; }

        string Description { get; set; }

        Vector3D Coords { get; set; }

        bool ShowOnHud { get; set; }

        /// <summary>
        /// If it's null then the GPS is confirmed (does not expire automatically).
        /// Otherwise, time at which we should drop it from the list, relative to ElapsedPlayTime
        /// </summary>
        TimeSpan? DiscardAt { get; set; }

        string ToString();
    }
}
