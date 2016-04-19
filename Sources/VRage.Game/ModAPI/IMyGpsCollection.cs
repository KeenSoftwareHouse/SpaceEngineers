using System.Collections.Generic;
using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IMyGpsCollection
    {
        /// <summary>
        /// Gets the GPS entries from the specified identity (does not use network traffic).
        /// </summary>
        /// <param name="identityId"></param>
        /// <returns>The GPS entry list.</returns>
        List<IMyGps> GetGpsList(long identityId);

        /// <summary>
        /// Gets the GPS entries from the specified identity (does not use network traffic).
        /// </summary>
        /// <param name="identityId"></param>
        /// <param name="list">GPS entries will be added to this list. The list is not cleared internally.</param>
        void GetGpsList(long identityId, List<IMyGps> list);

        /// <summary>
        /// Creates a GPS entry object. Does not automatically add it, you need to use AddGps() or AddLocalGps().
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="coords"></param>
        /// <param name="showOnHud"></param>
        /// <param name="temporary">wether it automatically expires or not (DiscardAt field)</param>
        /// <returns></returns>
        IMyGps Create(string name, string description, Vector3D coords, bool showOnHud, bool temporary = false);

        /// <summary>
        /// Sends a network request to add the GPS entry for the said player, which will also save it to the server.
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="gps">Use the Create() method to get this object</param>
        void AddGps(long identityId, IMyGps gps);

        /// <summary>
        /// Sends a network request to modify the contents of an existing GPS entry.
        /// </summary>
        /// <param name="identityId"></param>
        /// <param name="gps">NOTE: it must contain the original hash id</param>
        void ModifyGps(long identityId, IMyGps gps);

        /// <summary>
        /// Sends a network request to remove the specified GPS entry.
        /// </summary>
        /// <param name="identityId"></param>
        /// <param name="gps"></param>
        void RemoveGps(long identityId, IMyGps gps);

        /// <summary>
        /// Sends a network request to remove the specified GPS entry.
        /// </summary>
        /// <param name="identityId"></param>
        /// <param name="gpsHash"></param>
        void RemoveGps(long identityId, int gpsHash);

        /// <summary>
        /// Sends a network request to set the GPS entry if it's shown on HUD or not.
        /// </summary>
        /// <param name="identityId"></param>
        /// <param name="gps"></param>
        /// <param name="show"></param>
        void SetShowOnHud(long identityId, IMyGps gps, bool show);

        /// <summary>
        /// Sends a network request to set the GPS entry if it's shown on HUD or not.
        /// </summary>
        /// <param name="identityId"></param>
        /// <param name="gpsHash"></param>
        /// <param name="show"></param>
        void SetShowOnHud(long identityId, int gpsHash, bool show);

        /// <summary>
        /// Adds a GPS entry only for this client which won't be synchronized or saved.
        /// </summary>
        /// <param name="gps"></param>
        void AddLocalGps(IMyGps gps);

        /// <summary>
        /// Remove a local GPS entry, no network updates sent.
        /// 
        /// NOTE: This can remove synchronized ones too.
        /// </summary>
        /// <param name="gps"></param>
        void RemoveLocalGps(IMyGps gps);

        /// <summary>
        /// Remove a local GPS entry, no network updates sent.
        /// 
        /// NOTE: This can remove synchronized ones too.
        /// </summary>
        /// <param name="gpsHash"></param>
        void RemoveLocalGps(int gpsHash);
    }
}
