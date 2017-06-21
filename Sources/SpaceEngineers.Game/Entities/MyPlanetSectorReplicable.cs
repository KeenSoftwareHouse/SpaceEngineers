using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Multiplayer;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using VRage.Library.Collections;
using VRage.Network;

namespace Sandbox.Game.Entities.Planet
{
    /*class MyPlanetSectorReplicable : MyExternalReplicableEvent<MyPlanetEnvironmentSector>
    {
        public override IMyReplicable GetDependency()
        {
            return FindByObject(Instance.Planet);
        }

        public override float GetPriority(MyClientInfo client)
        {
            var spaceClient = client.State as MySpaceClientState;

            Debug.Assert(spaceClient != null);

            long planetId = Instance.Planet.EntityId;

            HashSet<MyPlanetSectorId> sectors;
            if (spaceClient.KnownSectors.TryGetValue(planetId, out sectors))
            {
                if (sectors.Contains(Instance.SectorId)) return 1.0f;
            }

            return 0f;
        }

        public override bool OnSave(BitStream stream)
        {
            stream.WriteInt64(Instance.Planet.EntityId);
            stream.WriteInt64(Instance.SectorId.Pack());

            var items = Instance.SavedItems;
            if (items != null)
            {
                stream.WriteInt32(items.Count);

                for (int i = 0; i < items.Count; i++)
                {
                    stream.WriteInt32(items[i]);
                }
            }
            else
            {
                stream.WriteInt32(0);
            }

            return true;
        }

        // Called on client
        public override void OnDestroy()
        {
            Instance.ServerOwned = false;
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
        }

        protected override void OnLoad(BitStream stream, Action<MyPlanetEnvironmentSector> loadingDoneHandler)
        {
            var planetEntityId = stream.ReadInt64();
            var packedSectorId = stream.ReadInt64();

            var changedItems = stream.ReadInt32();

            var planet = MyEntities.GetEntityById(planetEntityId) as MyPlanet;

            MyPlanetSectorId id;
            MyPlanetSectorId.Unpack(packedSectorId, out id);

            MyPlanetEnvironmentSector sector = (MyPlanetEnvironmentSector) planet.GetSector(ref id);

            List<int> items = new List<int>(changedItems);

            for (int i = 0; i < changedItems; i++)
            {
                items.Add(stream.ReadInt32());
            }

            if (items.Count != 0 && (!planet.SavedSectors.ContainsKey(id) || planet.SavedSectors[id].Count != items.Count))
            {
                planet.SavedSectors[id] = items;
                if (sector != null)
                    sector.Reset();
            }
            else if(sector != null)
            {
                sector.HasPhysics = false;
            }

            Debug.Assert(sector == null || !sector.ServerOwned);

            loadingDoneHandler(sector != null && sector.ServerOwned ? null : sector);
        }

        protected override void OnHook()
        {
            base.OnHook();

            if (Sync.IsServer)
            {
                Instance.OnPhysicsClose += Sector_OnPhysicsClose;
            }
            else
                Instance.ServerOwned = true;
        }

        private void Sector_OnPhysicsClose()
        {
            Instance.OnPhysicsClose -= Sector_OnPhysicsClose;

            // Needed when server is host client
            Instance.ServerOwned = false;

            RaiseDestroyed();
        }

        protected override void OnLoadBegin(BitStream stream, Action<MyPlanetEnvironmentSector> loadingDoneHandler)
        {
            //throw new NotImplementedException();
        }
    }*/
}
