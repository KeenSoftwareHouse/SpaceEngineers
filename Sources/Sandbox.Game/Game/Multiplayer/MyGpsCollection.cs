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
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Network;


namespace Sandbox.Game.Multiplayer
{
    [StaticEventOwner]
    partial class MyGpsCollection
    {
        public Dictionary<int, MyGps> this[long id]
        {
            get { return m_playerGpss[id]; }
        }
        public bool ExistsForPlayer(long id)
        {
            Dictionary<int, MyGps> var;
            return m_playerGpss.TryGetValue(id, out var);
        }
        #region network

        struct AddMsg
        {
            public long IdentityId;
            [Serialize(MyObjectFlags.Nullable)]
            public string Name;
            [Serialize(MyObjectFlags.Nullable)]
            public string Description;
            public Vector3D Coords;
            public bool ShowOnHud;
            public bool IsFinal;
            public bool AlwaysVisible;
            public long EntityId;
            public Color GPSColor;
        }

        struct ModifyMsg
        {
            public long IdentityId;
            public int Hash;
            [Serialize(MyObjectFlags.Nullable)]
            public string Name;
            [Serialize(MyObjectFlags.Nullable)]
            public string Description;
            public Vector3D Coords;
            public Color GPSColor;
        }

        #region add_GPS
        //ADD:
        public void SendAddGps(long identityId, ref MyGps gps, long entityId = -1)
        {
            var msg = new AddMsg();
            msg.IdentityId = identityId;
            msg.Name = gps.Name;
            msg.Description = gps.Description;
            msg.Coords = gps.Coords;
            msg.ShowOnHud = gps.ShowOnHud;
            msg.IsFinal = (gps.DiscardAt == null ? true : false);
            msg.AlwaysVisible = gps.AlwaysVisible;
            msg.EntityId = entityId;
            msg.GPSColor = gps.GPSColor;

            MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.OnAddGps, msg);
        }

        [Event, Reliable, Server, Broadcast]
        static void OnAddGps(AddMsg msg)
        {
            MyGps gps = new MyGps();
            gps.Name = msg.Name;
            gps.Description = msg.Description;
            gps.Coords = msg.Coords;
            gps.ShowOnHud = msg.ShowOnHud;
            gps.AlwaysVisible = msg.AlwaysVisible;
            gps.DiscardAt = null;
            gps.GPSColor = msg.GPSColor;
            if (!msg.IsFinal)
                gps.SetDiscardAt();
            gps.UpdateHash();
            if(msg.EntityId > 0)
                gps.SetEntity(MyEntities.GetEntityById(msg.EntityId));
            if (MySession.Static.Gpss.AddPlayerGps(msg.IdentityId, ref gps))
            {//new entry succesfully added
                if (gps.ShowOnHud && msg.IdentityId == MySession.Static.LocalPlayerId)
                    MyHud.GpsMarkers.RegisterMarker(gps);
            }

            var handler = MySession.Static.Gpss.ListChanged;
            if (handler != null)
            {
                handler(msg.IdentityId);
            }
        }

        #endregion add_GPS

        #region delete_GPS
        //DELETE:
        public void SendDelete(long identityId, int gpsHash)
        {
            MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.DeleteRequest, identityId, gpsHash);
        }

        [Event, Reliable, Server]
        static void DeleteRequest(long identityId, int gpsHash)
        {
            Dictionary<int, MyGps> gpsList;
            var result = MySession.Static.Gpss.m_playerGpss.TryGetValue(identityId, out gpsList);

            if (result)
                if (gpsList.ContainsKey(gpsHash))
                    MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.DeleteSuccess, identityId, gpsHash);
        }

        [Event, Reliable, Server, Broadcast]
        static void DeleteSuccess(long identityId, int gpsHash)
        {
            Dictionary<int, MyGps> gpsList;
            var result = MySession.Static.Gpss.m_playerGpss.TryGetValue(identityId, out gpsList);
            if (result)
            {
                MyGps gps;
                result = gpsList.TryGetValue(gpsHash, out gps);
                if (result)
                {
                    if (gps.ShowOnHud)
                        MyHud.GpsMarkers.UnregisterMarker(gps);
                    gpsList.Remove(gpsHash);
                    gps.Close();
                    var handler = MySession.Static.Gpss.ListChanged;
                    if (handler != null)
                        handler(identityId);
                }

            }
        }
        #endregion delete_GPS

        #region MODIFY_GPS
        //MODIFY:
        public void SendModifyGps(long identityId, MyGps gps)
        {//beware: gps must still contain original hash. Recompute during/after success.
            var msg = new ModifyMsg();
            msg.IdentityId = identityId;
            msg.Name = gps.Name;
            msg.Description = gps.Description;
            msg.Coords = gps.Coords;
            msg.Hash = gps.Hash;
            msg.GPSColor = gps.GPSColor;

            MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.ModifyRequest, msg);
        }

        [Event, Reliable, Server]
        static void ModifyRequest(ModifyMsg msg)
        {
            Dictionary<int, MyGps> gpsList;
            var result = MySession.Static.Gpss.m_playerGpss.TryGetValue(msg.IdentityId, out gpsList);

            if (result)
                if (gpsList.ContainsKey(msg.Hash))
                    MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.ModifySuccess, msg);
        }

        [Event, Reliable, Server, Broadcast]
        static void ModifySuccess(ModifyMsg msg)
        {
            Dictionary<int, MyGps> gpsList;
            var result = MySession.Static.Gpss.m_playerGpss.TryGetValue(msg.IdentityId, out gpsList);

            if (result)
            {
                MyGps gps;
                if (gpsList.TryGetValue(msg.Hash, out gps))
                {
                    gps.Name = msg.Name;
                    gps.Description = msg.Description;
                    gps.Coords = msg.Coords;
                    gps.DiscardAt = null;//finalize at edit
                    gps.GPSColor = msg.GPSColor;
                    var handler = MySession.Static.Gpss.GpsChanged;//because of name in table
                    if (handler != null)
                    {
                        handler(msg.IdentityId, gps.Hash);
                    }
                    gpsList.Remove(gps.Hash);
                    MyHud.GpsMarkers.UnregisterMarker(gps);
                    gps.UpdateHash();
                    //if an entry exists, we will remove it, then isert this => user overwrited old entry:
                    if (gpsList.ContainsKey(gps.Hash))
                    {
                        MyGps oldGps;
                        gpsList.TryGetValue(gps.Hash, out oldGps);
                        MyHud.GpsMarkers.UnregisterMarker(oldGps);
                        gpsList.Remove(gps.Hash);
                        gpsList.Add(gps.Hash, gps);//new key
                        var handlerList = MySession.Static.Gpss.ListChanged;//we have merged two entries...
                        if (handlerList != null)
                        {
                            handlerList(msg.IdentityId);
                        }
                    }
                    else
                        gpsList.Add(gps.Hash, gps);//new key

                    if (msg.IdentityId == MySession.Static.LocalPlayerId && gps.ShowOnHud)
                        MyHud.GpsMarkers.RegisterMarker(gps);
                }
            }
        }
        #endregion add_GPS


        public event Action<long> ListChanged;//something added or deleted  <identity id>
        public event Action<long, int> GpsChanged;//right side changed  <identity id, hash of gps>
        #region showOnHUD
        //SHOW ON HUD:
        public void ChangeShowOnHud(long identityId, int gpsHash, bool show)
        {
            SendChangeShowOnHud(identityId, gpsHash, show);
        }
        void SendChangeShowOnHud(long identityId, int gpsHash, bool show)
        {
            MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.ShowOnHudRequest, identityId, gpsHash, show);
        }

        [Event, Reliable, Server]
        static void ShowOnHudRequest(long identityId, int gpsHash, bool show)
        {
            Dictionary<int, MyGps> gpsList;
            var found = MySession.Static.Gpss.m_playerGpss.TryGetValue(identityId, out gpsList);

            if (found)
                MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.ShowOnHudSuccess, identityId, gpsHash, show);
        }

        [Event, Reliable, Server, Broadcast]
        static void ShowOnHudSuccess(long identityId, int gpsHash, bool show)
        {
            Dictionary<int, MyGps> gpsList;
            var result = MySession.Static.Gpss.m_playerGpss.TryGetValue(identityId, out gpsList);
            if (result)
            {
                MyGps gps;
                result = gpsList.TryGetValue(gpsHash, out gps);
                if (result)
                {
                    gps.ShowOnHud = show;
                    if (!show) gps.AlwaysVisible = false;
                    gps.DiscardAt = null;//finalize

                    var handler = MySession.Static.Gpss.GpsChanged;
                    if (handler != null)
                        handler(identityId, gpsHash);

                    if (identityId == MySession.Static.LocalPlayerId)
                    {
                        if (gps.ShowOnHud)
                            MyHud.GpsMarkers.RegisterMarker(gps);
                        else
                            MyHud.GpsMarkers.UnregisterMarker(gps);
                    }
                }

            }
        }
        #endregion showOnHUD

        #region alwaysVisible
        //SHOW ON HUD:
        public void ChangeAlwaysVisible(long identityId, int gpsHash, bool alwaysVisible)
        {
            SendChangeAlwaysVisible(identityId, gpsHash, alwaysVisible);
        }
        void SendChangeAlwaysVisible(long identityId, int gpsHash, bool alwaysVisible)
        {
            MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.AlwaysVisibleRequest, identityId, gpsHash, alwaysVisible);
        }

        [Event, Reliable, Server]
        static void AlwaysVisibleRequest(long identityId, int gpsHash, bool alwaysVisible)
        {
            Dictionary<int, MyGps> gpsList;
            var found = MySession.Static.Gpss.m_playerGpss.TryGetValue(identityId, out gpsList);

            if (found)
                MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.AlwaysVisibleSuccess, identityId, gpsHash, alwaysVisible);
        }

        [Event, Reliable, Server, Broadcast]
        static void AlwaysVisibleSuccess(long identityId, int gpsHash, bool alwaysVisible)
        {
            Dictionary<int, MyGps> gpsList;
            var result = MySession.Static.Gpss.m_playerGpss.TryGetValue(identityId, out gpsList);
            if (result)
            {
                MyGps gps;
                result = gpsList.TryGetValue(gpsHash, out gps);
                if (result)
                {
                    gps.AlwaysVisible = alwaysVisible;
                    gps.ShowOnHud = (gps.ShowOnHud || alwaysVisible);
                    gps.DiscardAt = null;//finalize

                    var handler = MySession.Static.Gpss.GpsChanged;
                    if (handler != null)
                        handler(identityId, gpsHash);

                    if (identityId == MySession.Static.LocalPlayerId)
                    {
                        if (gps.ShowOnHud)
                            MyHud.GpsMarkers.RegisterMarker(gps);
                        else
                            MyHud.GpsMarkers.UnregisterMarker(gps);
                    }
                }

            }
        }
        #endregion

        #region color
        //SHOW ON HUD:
        public void ChangeColor(long identityId, int gpsHash, Color color)
        {
            SendChangeColor(identityId, gpsHash, color);
        }
        void SendChangeColor(long identityId, int gpsHash, Color color)
        {
            MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.ColorRequest, identityId, gpsHash, color);
        }

        [Event, Reliable, Server]
        static void ColorRequest(long identityId, int gpsHash, Color color)
        {
            Dictionary<int, MyGps> gpsList;
            var found = MySession.Static.Gpss.m_playerGpss.TryGetValue(identityId, out gpsList);

            if (found)
                MyMultiplayer.RaiseStaticEvent(s => MyGpsCollection.ColorSuccess, identityId, gpsHash, color);
        }

        [Event, Reliable, Server, Broadcast]
        static void ColorSuccess(long identityId, int gpsHash, Color color)
        {
            Dictionary<int, MyGps> gpsList;
            var result = MySession.Static.Gpss.m_playerGpss.TryGetValue(identityId, out gpsList);
            if (result)
            {
                MyGps gps;
                result = gpsList.TryGetValue(gpsHash, out gps);
                if (result)
                {
                    gps.GPSColor = color;
                    gps.DiscardAt = null;//finalize

                    var handler = MySession.Static.Gpss.GpsChanged;
                    if (handler != null)
                        handler(identityId, gpsHash);
                }

            }
        }
        #endregion

        #endregion network

        //<IdentityId < hash of gps, gps > >
        private Dictionary<long, Dictionary<int, MyGps>> m_playerGpss = new Dictionary<long, Dictionary<int, MyGps>>();

        public bool AddPlayerGps(long identityId, ref MyGps gps)
        {

            if (gps == null)
                return false;
            Dictionary<int, MyGps> result;
            var success = m_playerGpss.TryGetValue(identityId, out result);
            if (!success)
            {
                result = new Dictionary<int, MyGps>();
                m_playerGpss.Add(identityId, result);
            }
            if (result.ContainsKey(gps.Hash))
            {
                //Request to add existing. We update timestamp:
                MyGps mGps;
                result.TryGetValue(gps.Hash, out mGps);
                if (mGps.DiscardAt != null)//not final
                    mGps.SetDiscardAt();
                return false;
            }
            result.Add(gps.Hash, gps);

            return true;
        }

        public MyGps GetGps(int hash)
        {
            foreach (var gpss in MySession.Static.Gpss.m_playerGpss.Values)
            {
                MyGps gps;
                if (gpss.TryGetValue(hash, out gps))
                {
                    return gps;
                }
            }

            return null;
        }

        private StringBuilder m_NamingSearch = new StringBuilder();
        public void GetNameForNewCurrent(StringBuilder name)
        {//makes next entry name of coordinate from my current position - playername #xx
            Dictionary<int, MyGps> result;
            int number = 0;
            name.Clear()
                .Append(MySession.Static.LocalHumanPlayer.DisplayName)
                .Append(" #");
            if (m_playerGpss.TryGetValue(MySession.Static.LocalPlayerId, out result))
            {
                foreach (var gpsList in result)
                {
                    if (gpsList.Value.Name.StartsWith(name.ToString()))
                    {
                        m_NamingSearch.Clear().Append(gpsList.Value.Name, name.Length, gpsList.Value.Name.Length - name.Length);
                        int i;
                        try
                        {
                            i = int.Parse(m_NamingSearch.ToString());
                        }
                        catch (SystemException)
                        {
                            continue;
                        }
                        if (i > number)
                            number = i;
                    }
                }
            }
            number++;
            name.Append(number);
        }

        #region build_and_save
        private long lastPlayerId = 0;
        public void LoadGpss(MyObjectBuilder_Checkpoint checkpoint)
        {
            if (MyFakes.ENABLE_GPS && checkpoint.Gps != null)
                foreach (var entry in checkpoint.Gps.Dictionary)//identity
                {
                    foreach (var gpsEntry in entry.Value.Entries)
                    {
                        MyGps gps = new MyGps(gpsEntry);
                        Dictionary<int, MyGps> playersGpss;
                        if (!m_playerGpss.TryGetValue(entry.Key, out playersGpss))
                        {
                            playersGpss = new Dictionary<int, MyGps>();
                            m_playerGpss.Add(entry.Key, playersGpss);
                        }
                        if (!playersGpss.ContainsKey(gps.GetHashCode()))
                        {
                            playersGpss.Add(gps.GetHashCode(), gps);

                            if (gps.ShowOnHud && entry.Key == MySession.Static.LocalPlayerId && MySession.Static.LocalPlayerId != 0)// LocalPlayerId=0 => loading MP game and not yet initialized. Or server, which does not matter
                                MyHud.GpsMarkers.RegisterMarker(gps);
                        }
                    }
                }
        }
        public void updateForHud()
        {//unfortunately, when loading MP game, local identity is not initialized, we need register hud markers later. =now
            if (lastPlayerId != MySession.Static.LocalPlayerId)
            {
                Dictionary<int, MyGps> playersGpss;
                if (m_playerGpss.TryGetValue(lastPlayerId, out playersGpss))
                    foreach (var gps in playersGpss)
                        MyHud.GpsMarkers.UnregisterMarker(gps.Value);
                lastPlayerId = MySession.Static.LocalPlayerId;
                if (m_playerGpss.TryGetValue(lastPlayerId, out playersGpss))
                    foreach (var gps in playersGpss)
                        if (gps.Value.ShowOnHud)
                            MyHud.GpsMarkers.RegisterMarker(gps.Value);
            }

        }

        public void SaveGpss(MyObjectBuilder_Checkpoint checkpoint)
        {
            if (MyFakes.ENABLE_GPS)
            {
                DiscardOld();
                if (checkpoint.Gps == null)
                    checkpoint.Gps = new SerializableDictionary<long, MyObjectBuilder_Gps>();
                foreach (var item in m_playerGpss)
                {
                    MyObjectBuilder_Gps bGps;
                    if (!checkpoint.Gps.Dictionary.TryGetValue(item.Key, out bGps))
                        bGps = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Gps>();
                    if (bGps.Entries == null)
                        bGps.Entries = new List<MyObjectBuilder_Gps.Entry>();
                    foreach (var gps in item.Value)
                    {
                        if(!gps.Value.IsLocal)
                            bGps.Entries.Add(GetObjectBuilderEntry(gps.Value));
                    }
                    checkpoint.Gps.Dictionary.Add(item.Key, bGps);
                }
            }
        }
        public MyObjectBuilder_Gps.Entry GetObjectBuilderEntry(MyGps gps)
        {
            return new MyObjectBuilder_Gps.Entry()
            {
                name = gps.Name,
                description = gps.Description,
                coords = gps.Coords,
                isFinal = (gps.DiscardAt == null ? true : false),
                showOnHud = gps.ShowOnHud,
                alwaysVisible = gps.AlwaysVisible,
                color = gps.GPSColor
            };

        }
        //drops nonfinal which are older than MyGps.DROP_NONFINAL_AFTER_SEC
        public void DiscardOld()
        {
            List<int> toRemove = new List<int>();
            foreach (var plist in m_playerGpss)
            {
                foreach (var gpsList in plist.Value)
                {
                    if (gpsList.Value.DiscardAt != null)//nonfinal
                        if (TimeSpan.Compare(MySession.Static.ElapsedPlayTime, (TimeSpan)gpsList.Value.DiscardAt) > 0)
                            toRemove.Add(gpsList.Value.GetHashCode());
                }
                foreach (var hash in toRemove)
                {
                    plist.Value.Remove(hash);//drop
                }
                toRemove.Clear();
            }
        }

        #endregion build_and_save

        #region scan texts for GPS coordinates
        internal void RegisterChat(MyMultiplayerBase multiplayer)
        {
            if (MyFakes.ENABLE_GPS)
                multiplayer.ChatMessageReceived += ParseChat;
        }

        internal void UnregisterChat(MyMultiplayerBase multiplayer)
        {
            if (MyFakes.ENABLE_GPS)
                multiplayer.ChatMessageReceived -= ParseChat;
        }

        private void ParseChat(ulong steamUserId, string messageText, ChatEntryTypeEnum chatEntryType)
        {
            //string userName = MyMultiplayer.Static.GetMemberName(steamUserId);
            StringBuilder description = new StringBuilder();
            description.Append(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_FromChatDescPrefix)).Append(MyMultiplayer.Static.GetMemberName(steamUserId));
            ScanText(messageText, description);
        }

        //parses input string, searches for only one valid coords
        public static bool ParseOneGPS(string input, StringBuilder name, ref Vector3D coords)
        {
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
            foreach (Match match in Regex.Matches(input, m_ScanPattern))
            {
                double x, y, z;
                try
                {
                    x = double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    x = Math.Round(x, 2);
                    y = double.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    y = Math.Round(y, 2);
                    z = double.Parse(match.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
                    z = Math.Round(z, 2);
                }
                catch (SystemException)
                {
                    continue;//search for next GPS in the input
                }
                //parsed successfully
                name.Clear().Append(match.Groups[1].Value);
                coords.X = x; coords.Y = y; coords.Z = z;
                return true;
            }
#endif // !XB1
            return false;
        }

        //parses input string, searches for only one valid coords
        public static bool ParseOneGPSExtended(string input, StringBuilder name, ref Vector3D coords, StringBuilder additionalData)
        {
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
            foreach (Match match in Regex.Matches(input, m_ScanPatternExtended))
            {
                double x, y, z;
                try
                {
                    x = double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    x = Math.Round(x, 2);
                    y = double.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    y = Math.Round(y, 2);
                    z = double.Parse(match.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
                    z = Math.Round(z, 2);
                }
                catch (SystemException)
                {
                    continue;//search for next GPS in the input
                }
                //parsed successfully
                name.Clear().Append(match.Groups[1].Value);
                coords.X = x; coords.Y = y; coords.Z = z;

                additionalData.Clear();

                if (match.Groups.Count == 6 && !string.IsNullOrWhiteSpace(match.Groups[5].Value))
                    additionalData.Append(match.Groups[5].Value);

                return true;
            }
#endif // !XB1
            return false;
        }

        //this is all you have to call if you have text with possible GPS coordinates and want to add them
        //drop string in question into input parameter, if you want you can provide text into GPS description field in second parameter
        //if a point already exists (same name&xyz) it will not be added again
        //returns number of coordinates found
        public int ScanText(string input, StringBuilder desc)
        {
            return ScanText(input, desc.ToString());
        }
        private static readonly int PARSE_MAX_COUNT = 20;
        private static readonly string m_ScanPattern = @"GPS:([^:]{0,32}):([\d\.-]*):([\d\.-]*):([\d\.-]*):";
        private static readonly string m_ScanPatternExtended = @"GPS:([^:]{0,32}):([\d\.-]*):([\d\.-]*):([\d\.-]*):(.*)";
        public int ScanText(string input, string desc = null)
        {//scans given text and adds all as uncorfirmed
            int count = 0;
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
            // GPS:name without doublecolons:123.4:234.5:3421.6:
            foreach (Match match in Regex.Matches(input, m_ScanPattern))
            {
                String name = match.Groups[1].Value;
                double x, y, z;
                try
                {
                    x = double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    x = Math.Round(x, 2);
                    y = double.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    y = Math.Round(y, 2);
                    z = double.Parse(match.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
                    z = Math.Round(z, 2);
                }
                catch (SystemException)
                {
                    continue;//search for next GPS in the input
                }

                MyGps newGps = new MyGps()
                {
                    Name = name,
                    Description = desc,
                    Coords = new Vector3D(x, y, z),
                    ShowOnHud = false
                };
                newGps.UpdateHash();
                MySession.Static.Gpss.SendAddGps(MySession.Static.LocalPlayerId, ref newGps);
                ++count;
                if (count == PARSE_MAX_COUNT)
                    break;
            }
#endif // !XB1

            return count;
        }
        #endregion
    }
}
