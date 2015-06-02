using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI
{
    public abstract class MyAiTargetBase : IMyAiTarget
    {
        protected MyAiTargetEnum m_currentTarget;
        protected IMyEntityBot m_user;
        protected MyAgentBot m_bot;

        protected MyEntity m_targetEntity = null;
        protected Vector3I m_targetCube = Vector3I.Zero;
        protected Vector3D m_targetPosition = Vector3D.Zero;
        protected Vector3I m_targetInVoxelCoord = Vector3I.Zero;

        protected static List<MyEntity> m_tmpEntities = new List<MyEntity>();
        protected static List<MyPhysics.HitInfo> m_tmpHits = new List<MyPhysics.HitInfo>();

        public MyAiTargetEnum TargetType
        {
            get { return m_currentTarget; }
        }

        public bool HasTarget()
        {
            return m_currentTarget != MyAiTargetEnum.NO_TARGET;
        }

        public MyCubeGrid TargetGrid
        {
            get
            {
                Debug.Assert(IsTargetGridOrBlock(m_currentTarget) && m_targetEntity is MyCubeGrid);
                return m_targetEntity as MyCubeGrid;
            }
        }

        public MyEntity TargetEntity
        {
            get { return m_targetEntity; }
        }

        public Vector3D TargetPosition
        {
            get
            {
                return m_targetPosition;
            }
        }

        public bool IsTargetGridOrBlock(MyAiTargetEnum type)
        {
            return type == MyAiTargetEnum.CUBE || type == MyAiTargetEnum.GRID;
        }

        public MyAiTargetBase(IMyEntityBot bot)
        {
            m_user = bot;
            m_bot = bot as MyAgentBot;
            m_currentTarget = MyAiTargetEnum.NO_TARGET;

            MyAiTargetManager.Static.AddAiTarget(this);
        }

        public abstract void Init(MyObjectBuilder_AiTarget builder);
        public abstract MyObjectBuilder_AiTarget GetObjectBuilder();

        public virtual void UnsetTarget()
        {
        }

        public virtual void DebugDraw()
        {
        }

        public virtual void Cleanup()
        {
			MyAiTargetManager.Static.RemoveAiTarget(this);
        }

        public virtual void Update()
        {
        }

        public bool PositionIsNearTarget(Vector3D position, float radius)
        {
            Debug.Assert(HasTarget());
            if (!HasTarget()) return false;

            Vector3D gotoPosition;
            float gotoRadius;
            GetGotoPosition(position, out gotoPosition, out gotoRadius);

            return Vector3D.Distance(position, gotoPosition) <= radius + gotoRadius;
        }

        public abstract void GetGotoPosition(Vector3D startingPosition, out Vector3D gotoPosition, out float radius);
        public abstract void GotoTarget();
        public abstract void AimAtTarget();
        public abstract void GotoFailed();
        public abstract bool SetTargetFromMemory(MyBBMemoryTarget inTarget);
    }
}
