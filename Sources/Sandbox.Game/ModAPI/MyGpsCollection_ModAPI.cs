using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Serialization;
using PlayerId = Sandbox.Game.World.MyPlayer.PlayerId;

using Sandbox.Definitions;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using VRageMath;
using Sandbox.Engine.Networking;
using Sandbox.Game.Gui;

using Sandbox.Engine.Utils;
using VRage;
using Sandbox.Game.Localization;

using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sandbox.Game.Multiplayer
{
    partial class MyGpsCollection : IMyGpsCollection
    {
        private static List<IMyGps> reusableList = new List<IMyGps>();

        IMyGps IMyGpsCollection.Create(string name, string description, Vector3D coords, bool showOnHud, bool temporary)
        {
            var gps = new MyGps();
            gps.Name = name;
            gps.Description = description;
            gps.Coords = coords;
            gps.ShowOnHud = showOnHud;
            gps.GPSColor = new Color(117, 201, 241);
            if (temporary)
                gps.SetDiscardAt();
            else
                gps.DiscardAt = null;
            gps.UpdateHash();
            return gps;
        }

        List<IMyGps> IMyGpsCollection.GetGpsList(long identityId)
        {
            reusableList.Clear();
            GetGpsList(identityId, reusableList);
            return reusableList;
        }

        public void GetGpsList(long identityId, List<IMyGps> list)
        {
            Dictionary<int, MyGps> gpsList;
            if (!m_playerGpss.TryGetValue(identityId, out gpsList))
                return;
            foreach (var internalGps in gpsList.Values)
            {
                list.Add(internalGps as IMyGps);
            }
        }

        public IMyGps GetGpsByName(long identityId, string gpsName)
        {
            Dictionary<int, MyGps> gpsList;
            if (!m_playerGpss.TryGetValue(identityId, out gpsList))
                return null;
            foreach (var internalGps in gpsList.Values)
            {
                if (internalGps.Name == gpsName)
                    return internalGps;
            }
            return null;
        }

        void IMyGpsCollection.AddGps(long identityId, IMyGps gps)
        {
            var internalGps = (MyGps)gps;
            SendAddGps(identityId, ref internalGps);
        }

        void IMyGpsCollection.RemoveGps(long identityId, IMyGps gps)
        {
            SendDelete(identityId, (gps as MyGps).Hash);
        }

        void IMyGpsCollection.RemoveGps(long identityId, int gpsHash)
        {
            SendDelete(identityId, gpsHash);
        }

        void IMyGpsCollection.ModifyGps(long identityId, IMyGps gps)
        {
            var internalGps = (MyGps)gps;
            SendModifyGps(identityId, internalGps);
        }

        void IMyGpsCollection.SetShowOnHud(long identityId, int gpsHash, bool show)
        {
            SendChangeShowOnHud(identityId, gpsHash, show);
        }

        void IMyGpsCollection.SetShowOnHud(long identityId, IMyGps gps, bool show)
        {
            SendChangeShowOnHud(identityId, (gps as MyGps).Hash, show);
        }

        void IMyGpsCollection.AddLocalGps(IMyGps gps)
        {
            var internalGps = (MyGps)gps;
            internalGps.IsLocal = true;
            if (AddPlayerGps(MySession.Static.LocalPlayerId, ref internalGps) && gps.ShowOnHud)
                MyHud.GpsMarkers.RegisterMarker(internalGps);
        }

        void IMyGpsCollection.RemoveLocalGps(IMyGps gps)
        {
            RemovePlayerGps(gps.Hash);
        }

        void IMyGpsCollection.RemoveLocalGps(int gpsHash)
        {
            RemovePlayerGps(gpsHash);
        }

        private void RemovePlayerGps(int gpsHash)
        {
            Dictionary<int, MyGps> gpsList;
            if (MySession.Static.Gpss.m_playerGpss.TryGetValue(MySession.Static.LocalPlayerId, out gpsList))
            {
                MyGps gps;
                if (gpsList.TryGetValue(gpsHash, out gps))
                {
                    if (gps.ShowOnHud)
                        MyHud.GpsMarkers.UnregisterMarker(gps);
                    gpsList.Remove(gpsHash);
                    var handler = MySession.Static.Gpss.ListChanged;
                    if (handler != null)
                        handler(MySession.Static.LocalPlayerId);
                }
            }
        }
    }
}