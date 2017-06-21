using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using Sandbox.Game.World;
using VRage;
using Havok;
using System.Diagnostics;
using System;
using VRage.Library.Utils;
using VRageMath;
using Sandbox.Game.Multiplayer;
using VRage.Game.Components;
using VRage.Game.Entity;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Profiler;
using VRage.Utils;

namespace Sandbox.Game.GameSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyFracturedPiecesManager : MySessionComponentBase
    {
        public const int FakePieceLayer = MyPhysics.CollisionLayers.CollideWithStaticLayer;
        public static MyFracturedPiecesManager Static;
        static float LIFE_OF_CUBIC_PIECE = 300; //1m3 will live for 300secs

        Queue<MyFracturedPiece> m_piecesPool = new Queue<MyFracturedPiece>();
        
        const int MAX_ALLOC_PER_FRAME = 50; //somehow allocations here are superfast compared to callback 10alloc ~0.03ms
        private int m_allocatedThisFrame = 0;

        HashSet<HkdBreakableBody> m_tmpToReturn = new HashSet<HkdBreakableBody>();

        HashSet<long> m_dbgCreated = new HashSet<long>();
        HashSet<long> m_dbgRemoved = new HashSet<long>();

        List<HkBodyCollision> m_rigidList = new List<HkBodyCollision>();

        public override bool IsRequiredByGame
        {
            get
            {
                return MyPerGameSettings.Destruction;
            }
        }
        public override void LoadData()
        {
            base.LoadData();

            InitPools();
            
            Static = this;
        }

        private MyFracturedPiece AllocatePiece()
        {
            ProfilerShort.Begin("AllocCounter");
            m_allocatedThisFrame++;

            var fp = MyEntities.CreateEntity(new MyDefinitionId(typeof(MyObjectBuilder_FracturedPiece))) as MyFracturedPiece;
            fp.Physics = new MyPhysicsBody(fp, RigidBodyFlag.RBF_DEBRIS);
            fp.Physics.CanUpdateAccelerations = true;
            ProfilerShort.End();
            return fp;
        }

        protected override void UnloadData()
        {
            foreach (var bodies in m_bodyPool)
            {
                bodies.Breakable.ClearListener();
            }
            m_bodyPool.Clear();
            m_piecesPool.Clear();

            base.UnloadData();
        }

        int m_addedThisFrame = 0;
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            foreach (var body in m_tmpToReturn)
            {
                ReturnToPoolInternal(body);
            }
            m_tmpToReturn.Clear();

            ProfilerShort.Begin("Allocate");
            while (m_bodyPool.Count < PREALLOCATE_BODIES && m_allocatedThisFrame < MAX_ALLOC_PER_FRAME)
            {
                m_bodyPool.Enqueue(AllocateBodies());
            }

            while (m_piecesPool.Count < PREALLOCATE_PIECES && m_allocatedThisFrame < MAX_ALLOC_PER_FRAME)
            {
                m_piecesPool.Enqueue(AllocatePiece());
            }
            ProfilerShort.End();
            m_allocatedThisFrame = 0;
        }

        private void RemoveInternal(MyFracturedPiece fp,bool fromServer = false)
        {
            if (fp.Physics != null && fp.Physics.RigidBody != null)
            {
                Debug.Assert(!fp.Physics.RigidBody.IsDisposed, "Disposed piece rigid body!!");
                if (fp.Physics.RigidBody.IsDisposed)
                {
                    fp.Physics.BreakableBody = fp.Physics.BreakableBody;
                }
            }

            if (fp.Physics == null || fp.Physics.RigidBody == null || fp.Physics.RigidBody.IsDisposed)
            {
                Debug.Fail("Should not get here!");
                MyEntities.Remove(fp);
                return;
            }

            //Let objects staying on this fp to fall
            if (!fp.Physics.RigidBody.IsActive)
                fp.Physics.RigidBody.Activate();

            MyPhysics.RemoveDestructions(fp.Physics.RigidBody);


            var bb = fp.Physics.BreakableBody;
            bb.AfterReplaceBody -= fp.Physics.FracturedBody_AfterReplaceBody;
            this.ReturnToPool(bb);
            
            fp.Physics.Enabled = false;
            MyEntities.Remove(fp);
            fp.Physics.BreakableBody = null;
            fp.Render.ClearModels();
            fp.OriginalBlocks.Clear();
            if (Sync.IsServer)
                Debug.Assert(m_dbgRemoved.Add(fp.EntityId));
            else
                MySyncDestructions.FPManagerDbgMessage(0, fp.EntityId);
            fp.EntityId = 0;
            fp.Physics.BreakableBody = null;
            m_piecesPool.Enqueue(fp);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityId">server = 0 (it allocates new), client - should recieve from server</param>
        /// <param name="fromServer"></param>
        /// <returns></returns>
        public MyFracturedPiece GetPieceFromPool(long entityId, bool fromServer = false)
        {
            Debug.Assert(!fromServer || entityId != 0);
            if (!Sync.IsServer)
                MySyncDestructions.FPManagerDbgMessage(entityId, 0);
            System.Diagnostics.Debug.Assert(Sync.IsServer || fromServer == true);
            MyFracturedPiece fp;
            if (m_piecesPool.Count == 0)
                fp = AllocatePiece();
            else
                fp = m_piecesPool.Dequeue();
            if (Sync.IsServer)
            {
                fp.EntityId = MyEntityIdentifier.AllocateId();
                Debug.Assert(m_dbgCreated.Add(fp.EntityId));
            }
            return fp;
        }

		public void GetFracturesInSphere(ref BoundingSphereD searchSphere, ref List<MyFracturedPiece> output)
		{
			HkShape shape = new HkSphereShape((float)searchSphere.Radius);
			try
			{
                MyPhysics.GetPenetrationsShape(shape, ref searchSphere.Center, ref Quaternion.Identity, m_rigidList, MyPhysics.CollisionLayers.NotCollideWithStaticLayer);
			
				foreach(var rigidBody in m_rigidList)
				{
					var fracture = rigidBody.GetCollisionEntity() as MyFracturedPiece;
					if (fracture != null)
						output.Add(fracture);
				}
			}
			finally
			{
				m_rigidList.Clear();
				shape.RemoveReference();
			}
		}

        public void GetFracturesInBox(ref BoundingBoxD searchBox, List<MyFracturedPiece> output)
        {
            Debug.Assert(m_rigidList.Count == 0);
            m_rigidList.Clear();

            HkShape shape = new HkBoxShape(searchBox.HalfExtents);
            try
            {
                var center = searchBox.Center;
                MyPhysics.GetPenetrationsShape(shape, ref center, ref Quaternion.Identity, m_rigidList, MyPhysics.CollisionLayers.NotCollideWithStaticLayer);

                foreach (var rigidBody in m_rigidList)
                {
                    var fracture = rigidBody.GetCollisionEntity() as MyFracturedPiece;
                    if (fracture != null /*&& m_piecesTimesOfDeath.ContainsKey(fracture)*/)
                        output.Add(fracture);
                }
            }
            finally
            {
                m_rigidList.Clear();
                shape.RemoveReference();
            }
        }

        //jn: TODO move to some more general position 
        private Queue<Bodies> m_bodyPool = new Queue<Bodies>();
        struct Bodies
        {
            public HkRigidBody Rigid;
            public HkdBreakableBody Breakable;
        }

        private Bodies AllocateBodies()
        {
            ProfilerShort.Begin("AllocCounter");
            m_allocatedThisFrame++;

            Bodies b;
            b.Rigid = HkRigidBody.Allocate();
            b.Breakable = HkdBreakableBody.Allocate();
            ProfilerShort.End();
            return b;
        }

        const int PREALLOCATE_PIECES = 400;
        const int PREALLOCATE_BODIES = 400;
        public void InitPools()
        {
            for (int i = 0; i < PREALLOCATE_PIECES; i++)
            {
                m_piecesPool.Enqueue(AllocatePiece());
            }

            for (int i = 0; i < PREALLOCATE_BODIES; i++)
            {
                m_bodyPool.Enqueue(AllocateBodies());
            }
        }

        public HashSet<HkRigidBody> m_givenRBs = new HashSet<HkRigidBody>(InstanceComparer<HkRigidBody>.Default);
        public HkdBreakableBody GetBreakableBody(HkdBreakableBodyInfo bodyInfo)
        {
            Bodies bodies;
            if (m_bodyPool.Count == 0)
            {
                bodies = AllocateBodies();
            }
            else
            {
                bodies = m_bodyPool.Dequeue();
            }
            Debug.Assert(m_givenRBs.Add(bodies.Rigid), "Same body in pool twice!");
            ProfilerShort.Begin("Init");
            bodies.Breakable.Initialize(bodyInfo, bodies.Rigid);
            ProfilerShort.End();
            return bodies.Breakable;
        }

        public void RemoveFracturePiece(MyFracturedPiece piece, float blendTimeSeconds, bool fromServer = false, bool sync = true)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer || fromServer, "Clients cannot remove pieces by themselves");
            System.Diagnostics.Debug.Assert((sync && Sync.IsServer) ^ (fromServer && !sync), "Sync must be called on server.");
            if (blendTimeSeconds == 0)
            {
                Debug.Assert((Sync.IsServer && sync) || fromServer, "Server must sync Fracture Piece removal!");

                RemoveInternal(piece, fromServer);
                return;
            }
        }

        public void RemoveFracturesInBox(ref BoundingBoxD box, float blendTimeSeconds)
        {
            Debug.Assert(Sync.IsServer);
            if (!Sync.IsServer)
                return;

            List<MyFracturedPiece> fracturesInBox = new List<MyFracturedPiece>();
            GetFracturesInBox(ref box, fracturesInBox);

            foreach (var fracture in fracturesInBox)
            {
                RemoveFracturePiece(fracture, blendTimeSeconds);
            }
        }

        public void RemoveFracturesInSphere(Vector3D center, float radius)
        {
            var radiusSq = radius * radius;
            foreach (var entity in Sandbox.Game.Entities.MyEntities.GetEntities())
            {
                if (entity is Sandbox.Game.Entities.MyFracturedPiece)
                {
                    if (radius <= 0 || (center - entity.Physics.CenterOfMassWorld).LengthSquared() < radiusSq)
                        MyFracturedPiecesManager.Static.RemoveFracturePiece(entity as MyFracturedPiece, 2);
                }
            }
        }

        public void ReturnToPool(HkdBreakableBody body)
        {
            m_tmpToReturn.Add(body);
        }

        private void ReturnToPoolInternal(HkdBreakableBody body)
        {
            var rb = body.GetRigidBody();
            if(rb == null)
            {
                return;
            }
            rb.ContactPointCallbackEnabled = false;
            m_givenRBs.Remove(rb);
            foreach(var b0 in m_bodyPool)
            {
                if (body == b0.Breakable || rb == b0.Rigid)
                    Debug.Fail("Body already in pool!");
            }
            var bs = body.BreakableShape;
            bs.ClearConnections();
            body.Clear();
            Bodies b;
            b.Rigid = rb;
            b.Breakable = body;
            body.InitListener();
            m_bodyPool.Enqueue(b);
        }

        internal void DbgCheck(long createdId, long removedId)
        {
            Debug.Assert(Sync.IsServer);
            if (createdId != 0)
                Debug.Assert(m_dbgCreated.Contains(createdId));
            if(removedId != 0)
                Debug.Assert(m_dbgRemoved.Contains(removedId));
        }
    }
}