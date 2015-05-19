using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using Sandbox.Game.World;
using VRage;
using Sandbox.Engine.Physics;
using Havok;
using System.Diagnostics;
using System;
using Medieval.ObjectBuilders;
using VRage.Library.Utils;
using VRageMath;
using Sandbox.Game.Multiplayer;

namespace Sandbox.Game.GameSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyFracturedPiecesManager : MySessionComponentBase
    {
        public const int FakePieceLayer = MyPhysics.CollideWithStaticLayer;
        public static MyFracturedPiecesManager Static;
        static float LIFE_OF_CUBIC_PIECE = 300; //1m3 will live for 300secs

        Queue<MyFracturedPiece> m_piecesPool = new Queue<MyFracturedPiece>();
        Dictionary<MyFracturedPiece, MyTimeSpan> m_piecesTimesOfDeath = new Dictionary<MyFracturedPiece, MyTimeSpan>();
        HashSet<MyFracturedPiece> m_blendingPieces = new HashSet<MyFracturedPiece>();
        HashSet<MyFracturedPiece> m_inactivePieces = new HashSet<MyFracturedPiece>();

        const int MAX_ALLOC_PER_FRAME = 50; //somehow allocations here are superfast compared to callback 10alloc ~0.03ms
        public static float BLEND_TIME = 2; //sec
        private int m_allocatedThisFrame = 0;

        HashSet<MyFracturedPiece> m_tmpToRemove = new HashSet<MyFracturedPiece>();
        HashSet<HkdBreakableBody> m_tmpToReturn = new HashSet<HkdBreakableBody>();

        HashSet<long> m_dbgCreated = new HashSet<long>();
        HashSet<long> m_dbgRemoved = new HashSet<long>();

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

            if (Sync.IsServer)
            {
                MyEntities.OnEntityAdd += MyEntities_OnEntityAdd;
                MyEntities.OnEntityRemove += MyEntities_OnEntityRemove;
            }

            InitPools();
            
            Static = this;
        }

        private MyFracturedPiece AllocatePiece()
        {
            ProfilerShort.Begin("AllocCounter");
            m_allocatedThisFrame++;

            var fp = new MyFracturedPiece();
            fp.Physics = new MyPhysicsBody(fp, RigidBodyFlag.RBF_DEBRIS);
            ProfilerShort.End();
            return fp;
        }

        protected override void UnloadData()
        {
            MyEntities.OnEntityAdd -= MyEntities_OnEntityAdd;
            MyEntities.OnEntityRemove -= MyEntities_OnEntityRemove;

            m_piecesTimesOfDeath.Clear();
            foreach (var bodies in m_bodyPool)
            {
                bodies.Breakable.ClearListener();
            }
            m_bodyPool.Clear();
            foreach (var fp in m_piecesPool)
                fp.Close();
            m_piecesPool.Clear();

            base.UnloadData();
        }

        int m_addedThisFrame = 0;
        void MyEntities_OnEntityAdd(MyEntity obj)
        {
            var fp = obj as MyFracturedPiece;
            if (fp != null)
            {
                MyTimeSpan age = GetPieceAgeLength(obj as MyFracturedPiece);
                m_piecesTimesOfDeath[fp] = MySandboxGame.Static.UpdateTime + age;
                m_addedThisFrame++;
                if (!fp.Physics.RigidBody.IsActive)
                    m_inactivePieces.Add(fp);
                fp.Physics.RigidBody.Activated += RigidBody_Activated;
                fp.Physics.RigidBody.Deactivated += RigidBody_Deactivated;
            }
        }

        void RigidBody_Deactivated(HkEntity entity)
        {
            Debug.Assert(entity.GetEntity() is MyFracturedPiece);
            var fp = entity.GetEntity() as MyFracturedPiece;
            if (fp == null || m_blendingPieces.Contains(fp))
                return;
            m_inactivePieces.Add(fp);
        }

        void RigidBody_Activated(HkEntity entity)
        {
            Debug.Assert(entity.GetEntity() is MyFracturedPiece);
            var fp = entity.GetEntity() as MyFracturedPiece;
            if (fp == null || m_blendingPieces.Contains(fp))
                return;
            m_inactivePieces.Remove(fp);
        }

        void MyEntities_OnEntityRemove(MyEntity obj)
        {
            //var fp = obj as MyFracturedPiece;
            //if (fp != null)
            //{
            //    if (m_piecesTimesOfBirth.ContainsKey(fp))
            //        m_activePieces.Remove(m_piecesTimesOfBirth[fp].Miliseconds);
            //    m_piecesTimesOfDeath.Remove(fp);
            //    m_piecesTimesOfBirth.Remove(fp);
            //    m_blendingPieces.Remove(fp);
            //}
        }

        MyTimeSpan GetPieceAgeLength(MyFracturedPiece piece)
        {
            if (piece.Physics == null || piece.Physics.BreakableBody == null)
                return MyTimeSpan.Zero;

            if (piece.Physics.RigidBody.Layer == FakePieceLayer)
                return MyTimeSpan.FromSeconds(8 + MyRandom.Instance.NextFloat(0, 4));

            float volume = piece.Physics.BreakableBody.BreakableShape.Volume;
            float proposedAgeInSecs = volume * LIFE_OF_CUBIC_PIECE;

            return MyTimeSpan.FromSeconds(proposedAgeInSecs);
        }

        public override void UpdateAfterSimulation()
        {
            CheckConsistency();

            m_addedThisFrame = 0;
            base.UpdateAfterSimulation();

            foreach(var body in m_tmpToReturn)
            {
                ReturnToPoolInternal(body);
            }
            m_tmpToReturn.Clear();

            if (Sync.IsServer)
            {
                foreach (var piece in m_piecesTimesOfDeath)
                {
                    Debug.Assert(piece.Key.Physics == null || !piece.Key.Physics.RigidBody.IsDisposed, "Disposed piece rigid body!!");
                    if (piece.Value <= MySandboxGame.Static.UpdateTime + MyTimeSpan.FromSeconds(BLEND_TIME))
                    {
                        RemoveFracturePiece(piece.Key, BLEND_TIME);
                    }
                }

                int i = m_piecesTimesOfDeath.Count - m_blendingPieces.Count;
                var maxFracturePieces = ((MyObjectBuilder_MedievalSessionSettings)MySession.Static.Settings).MaxActiveFracturePieces;
                if (i > maxFracturePieces)
                {
                    foreach (var piece in m_inactivePieces)
                    {
                        if (i <= maxFracturePieces)
                            break;
                        if (m_blendingPieces.Contains(piece))
                            continue;
                        RemoveFracturePiece(piece, BLEND_TIME);
                        i--;
                    }

                    foreach (var piece in m_piecesTimesOfDeath.Keys)
                    {
                        if (i <= maxFracturePieces)
                            break;
                        if (m_blendingPieces.Contains(piece) || m_inactivePieces.Contains(piece))
                            continue;
                        m_tmpToRemove.Add(piece);
                        i--;
                    }

                    foreach (var piece in m_tmpToRemove)
                    {
                        RemoveFracturePiece(piece, BLEND_TIME);
                    }
                    m_tmpToRemove.Clear();
                }
            }

            foreach (var piece in m_blendingPieces)
            {
                float blend = (float)(m_piecesTimesOfDeath[piece] - MySandboxGame.Static.UpdateTime).Seconds / BLEND_TIME;

                foreach (var id in piece.Render.RenderObjectIDs)
                {
                    VRageRender.MyRenderProxy.UpdateRenderEntity(
                        id,
                        null,
                        null,
                        1 - blend);
                }
                if (Sync.IsServer && m_piecesTimesOfDeath[piece] <= MySandboxGame.Static.UpdateTime)
                {
                    m_tmpToRemove.Add(piece);
                }
            }

            foreach (var fp in m_tmpToRemove)
            {
                Debug.Assert(Sync.IsServer);
                MySyncDestructions.RemoveFracturePiece(fp.EntityId, 0);
                RemoveInternal(fp);
            }
            m_tmpToRemove.Clear();

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

        [Conditional("DEBUG")]
        private void CheckConsistency()
        {
            foreach (var piece in m_inactivePieces)
            {
                Debug.Assert(m_piecesTimesOfDeath.ContainsKey(piece));
            }

            foreach (var piece in m_blendingPieces)
            {
                Debug.Assert(m_piecesTimesOfDeath.ContainsKey(piece));
            }
        }

        private void RemoveInternal(MyFracturedPiece fp)
        {
            if (fp.Physics != null && fp.Physics.RigidBody != null)
            {
                Debug.Assert(!fp.Physics.RigidBody.IsDisposed, "Disposed piece rigid body!!");
                if (fp.Physics.RigidBody.IsDisposed)
                {
                    var rb = fp.Physics.BreakableBody.GetRigidBody();
                    fp.Physics.BreakableBody = fp.Physics.BreakableBody;
                }
            }
            bool a = m_piecesTimesOfDeath.Remove(fp);
            bool b = m_blendingPieces.Remove(fp);
            bool c = m_inactivePieces.Remove(fp);

            if (fp.Physics == null || fp.Physics.RigidBody == null || fp.Physics.RigidBody.IsDisposed)
            {
                Debug.Fail("Should not get here!");
                MyEntities.Remove(fp);
                return;
            }

            fp.Physics.RigidBody.Activated -= RigidBody_Activated;
            fp.Physics.RigidBody.Deactivated -= RigidBody_Deactivated;

            //Let objects staying on this fp to fall
            if (!fp.Physics.RigidBody.IsActive)
                fp.Physics.RigidBody.Activate();

            MyPhysics.RemoveDestructions(fp.Physics.RigidBody);


            var bb = fp.Physics.BreakableBody;
            bb.AfterReplaceBody -= fp.Physics.FracturedBody_AfterReplaceBody;

            MyEntities.Remove(fp);
            fp.Physics.Enabled = false;
            fp.Physics.BreakableBody = null;
            //fp.Shape.RemoveReference();
            //System.Diagnostics.Debug.Assert(bb.ReferenceCount == 1);//not true anymore, since FP can be removed from callback immediately

            MyFracturedPiecesManager.Static.ReturnToPool(bb);
            fp.Render.ClearModels();
            fp.OriginalBlocks.Clear();
            if (Sync.IsServer)
                Debug.Assert(m_dbgRemoved.Add(fp.EntityId));
            else
                MySyncDestructions.FPManagerDbgMessage(0, fp.EntityId);
            fp.EntityId = 0;
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

		public List<MyFracturedPiece> GetFracturesInSphere(ref BoundingSphereD searchSphere)
		{
			var fracturesInRadius = new List<MyFracturedPiece>();
			var activeFractures = m_piecesTimesOfDeath.Keys;

			double radiusSq = searchSphere.Radius * searchSphere.Radius;
			foreach(var fracture in activeFractures)
			{
				double distanceSq = Vector3D.DistanceSquared(searchSphere.Center, fracture.PositionComp.GetPosition());
				if(distanceSq < radiusSq)
				{
					fracturesInRadius.Add(fracture);
				}
			}
			
			return fracturesInRadius;
		}

		public bool TryGetFractureById(long entityId, out MyFracturedPiece outFracture)
		{
			outFracture = null;
			var activeFractures = m_piecesTimesOfDeath.Keys;

			foreach(var fracture in activeFractures)
			{
				if (fracture.EntityId == entityId)
				{
					outFracture = fracture;
					return true;
				}
			}


			return false;
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
                if (sync)
                {
                    Debug.Assert(m_piecesTimesOfDeath.ContainsKey(piece), "Double removing Fracture Piece!");
                    MySyncDestructions.RemoveFracturePiece(piece.EntityId, blendTimeSeconds);
                }
                RemoveInternal(piece);
                return;
            }

            MyTimeSpan newDeath = MySandboxGame.Static.UpdateTime + MyTimeSpan.FromSeconds(blendTimeSeconds);
            if (m_blendingPieces.Add(piece))
            {
                if (sync)
                    MySyncDestructions.RemoveFracturePiece(piece.EntityId, blendTimeSeconds);

                if (!m_piecesTimesOfDeath.ContainsKey(piece))
                {
                    Debug.Assert(fromServer, "Fracture piece missing time of death on server!");
                    m_piecesTimesOfDeath.Add(piece, newDeath);
                }

                MyTimeSpan currentDeath;
                if (m_piecesTimesOfDeath.TryGetValue(piece, out currentDeath))
                {
                    if (currentDeath > newDeath)
                    {
                        m_piecesTimesOfDeath[piece] = newDeath;
                    }
                }
                else
                    Debug.Fail("Fracture Piece missing time of death!");
            }
            else
            {
                MyTimeSpan currentDeath;
                if (m_piecesTimesOfDeath.TryGetValue(piece, out currentDeath))
                {
                    if (currentDeath > newDeath)
                    {
                        m_piecesTimesOfDeath[piece] = newDeath;

                        if (sync)
                            MySyncDestructions.RemoveFracturePiece(piece.EntityId, blendTimeSeconds);
                    }
                }
                else
                {
                    Debug.Assert(false, "Shouldnt get here");
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
            //Debug.Assert(m_givenRBs.Remove(rb), "New body from outside in pool!");
            m_givenRBs.Remove(rb);
            foreach(var b0 in m_bodyPool)
            {
                if (body == b0.Breakable || rb == b0.Rigid)
                    Debug.Fail("Body already in pool!");
            }
            // body.BreakableShape.AddReference();
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