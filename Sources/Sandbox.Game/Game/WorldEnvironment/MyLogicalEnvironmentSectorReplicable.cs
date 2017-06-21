using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Game.WorldEnvironment
{
    internal class MyLogicalEnvironmentSectorReplicable : MyExternalReplicableEvent<MyLogicalEnvironmentSectorBase>
    {
        private static readonly MySerializeInfo serialInfo = new MySerializeInfo(MyObjectFlags.Dynamic | MyObjectFlags.Nullable, MyPrimitiveFlags.None, 0, MyObjectBuilderSerializer.SerializeDynamic, null, null);

        public override IMyReplicable GetParent()
        {
            return FindByObject(Instance.Owner.Entity);
        }

        public override float GetPriority(MyClientInfo client,bool cached)
        {
            var state = client.State as MyClientState;

            Debug.Assert(state != null);

            if (Instance.Owner.Entity == null)
                return 0f;

            long planetId = Instance.Owner.Entity.EntityId;

            HashSet<long> sectors;
            if (state.KnownSectors.TryGetValue(planetId, out sectors))
            {
                if (sectors.Contains(Instance.Id)) return 1.0f;
            }

            return 0f;
        }

        public override bool HasToBeChild
        {
            get { return false; } 
        }
          
        public override bool OnSave(BitStream stream)
        {
            stream.WriteInt64(Instance.Owner.Entity.EntityId);
            stream.WriteInt64(Instance.Id);

            var ob = Instance.GetObjectBuilder();

            MySerializer.Write(stream, ref ob, serialInfo);

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

        protected override void OnLoad(BitStream stream, Action<MyLogicalEnvironmentSectorBase> loadingDoneHandler)
        {
            var planetEntityId = stream.ReadInt64();
            var packedSectorId = stream.ReadInt64();

            var planet = MyEntities.GetEntityById(planetEntityId) as MyPlanet;

            Debug.Assert(planet != null, "planet == null!!!");

            var env = planet.Components.Get<MyPlanetEnvironmentComponent>();

            MyLogicalEnvironmentSectorBase sector = env.GetLogicalSector(packedSectorId);

            var ob = MySerializer.CreateAndRead<MyObjectBuilder_EnvironmentSector>(stream, serialInfo);

            if (sector != null)
            {
                sector.Init(ob);
            }

            Debug.Assert(sector == null || !sector.ServerOwned);

            loadingDoneHandler(sector != null && sector.ServerOwned ? null : sector);
        }

        protected override void OnHook()
        {
            base.OnHook();

            if (Sync.IsServer)
                Instance.OnClose += Sector_OnClose;
            else
                Instance.ServerOwned = true;
        }

        private void Sector_OnClose()
        {
            Instance.OnClose -= Sector_OnClose;

            // Needed when server is host client
            Instance.ServerOwned = false;

            RaiseDestroyed();
        }

        protected override void OnLoadBegin(BitStream stream, Action<MyLogicalEnvironmentSectorBase> loadingDoneHandler)
        {
            //throw new NotImplementedException();
        }

        public override VRageMath.BoundingBoxD GetAABB()
        {
            var aabb = VRageMath.BoundingBoxD.CreateInvalid();

            foreach (var bound in Instance.Bounds)
            {
                var bnd = bound;
                aabb = aabb.Include(Instance.WorldPos + bnd);
            }

            return aabb;
        }
        

    }
}
