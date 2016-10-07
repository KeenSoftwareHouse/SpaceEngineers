﻿using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Replication
{
    public abstract class MyEntityReplicableBase<T> : MyExternalReplicable<T>
        where T : MyEntity
    {
        protected Dictionary<ulong, float> m_cachedPriorityForClient = null;
        protected float m_baseVisibility = 130; // 1m object is visible for 130m (9km for blue ship, 12km for red ship)
        protected IMyStateGroup m_physicsSync;
        protected Action<MyEntity> OnCloseAction;

        public IMyStateGroup PhysicsStateGroup
        {
            get { return m_physicsSync; }
        }

        public float GetBasePriority(Vector3 position, Vector3 size, MyClientInfo client)
        {
            float planeArea = Math.Max(size.X * size.Y, Math.Max(size.X * size.Z, size.Y * size.Z)) + 0.01f;
            float distSq = (float)Vector3D.DistanceSquared(((MyClientState)client.State).Position, position);

            // Anything bigger than this will behave like it's only this size, it will be hidden anyway by view distance limit
            //float planeAreaMaxSqrt = MyMultiplayer.ReplicationDistance / baseVisibility;
            //MathHelper.Clamp(planeArea, 0.1f, planeAreaMaxSqrt * planeAreaMaxSqrt);

            if (distSq > MyMultiplayer.ReplicationDistance * MyMultiplayer.ReplicationDistance)
                return 0;

            float relativeDistance = (float)Math.Sqrt(distSq / planeArea); // How far the object would be when recalculated to 1m size
            float ratio = relativeDistance / m_baseVisibility; // 0 very close; 1 at the edge of visibility; >1 too far
            return MathHelper.Clamp(1 - ratio, 0, 1);
        }
        
        protected override void OnLoad(BitStream stream, Action<T> loadingDoneHandler)
        {
            var builder = VRage.Serialization.MySerializer.CreateAndRead<MyObjectBuilder_EntityBase>(stream, MyObjectBuilderSerializer.Dynamic);
            T entity = (T)MyEntities.CreateFromObjectBuilder(builder);
            if (entity != null)
            {
                entity.DebugCreatedBy = DebugCreatedBy.FromServer;
                MyEntities.Add(entity);
            }
            loadingDoneHandler(entity);
        }

        protected override void OnLoadBegin(BitStream stream, Action<T> loadingDoneHandler)
        {
            OnLoad(stream, loadingDoneHandler);
        }

        protected override void OnHook()
        {
            m_physicsSync = CreatePhysicsGroup();
            OnCloseAction = OnClose;
            Instance.OnClose += OnCloseAction;
        }

        public void OnClose(MyEntity ent)
        {
            RaiseDestroyed();
        }

        protected virtual IMyStateGroup CreatePhysicsGroup()
        {
            return new MyEntityPhysicsStateGroup(Instance, this);
        }

        public override IMyReplicable GetDependency()
        {
            return null;
        }

        public override float GetPriority(MyClientInfo client, bool cached)
        {
            ulong clientEndpoint = client.EndpointId.Value;
            if (cached)
            {

                if (m_cachedPriorityForClient != null && m_cachedPriorityForClient.ContainsKey(clientEndpoint))
                {
                    return m_cachedPriorityForClient[clientEndpoint];
                }
            }


            if (m_cachedPriorityForClient == null)
            {
                m_cachedPriorityForClient = new Dictionary<ulong, float>();
            }
            m_cachedPriorityForClient[clientEndpoint] = GetBasePriority(Instance.PositionComp.GetPosition(), Instance.PositionComp.WorldAABB.Size, client);
            return m_cachedPriorityForClient[clientEndpoint];
        }

        public override bool OnSave(BitStream stream)
        {
            var builder = Instance.GetObjectBuilder();
            VRage.Serialization.MySerializer.Write(stream, ref builder, MyObjectBuilderSerializer.Dynamic);

            return true;
        }

        public override void OnDestroy()
        {
            if (Instance != null)
            {
                Instance.Close();
                while (MyEntities.HasEntitiesToDelete())
                {
                    MyEntities.DeleteRememberedEntities();
                }
            }
         
            m_physicsSync = null;
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
            if (m_physicsSync != null)
            {
                resultList.Add(m_physicsSync);
            }
        }

        public override bool IsChild
        {
            get { return false; }
        }
    }
}
