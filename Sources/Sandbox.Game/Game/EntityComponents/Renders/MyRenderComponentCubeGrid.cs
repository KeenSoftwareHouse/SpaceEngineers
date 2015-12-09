using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Cube;
using System.Diagnostics;
using ProtoBuf;
using System.Reflection;
using VRage.Plugins;
using VRage.Import;
using VRageRender;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Engine.Physics;
using Sandbox.Common.Components;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using VRage.Components;
using Sandbox.Game.GameSystems;
using Sandbox.Graphics.TransparentGeometry.Particles;
using VRage;

namespace Sandbox.Game.Components
{
    public class MyRenderComponentCubeGrid: MyRenderComponent
    {
        public MyRenderComponentCubeGrid()
        {
            m_renderData = new MyCubeGridRenderData(this);
        }

        private static readonly List<MyPhysics.HitInfo> m_tmpHitList = new List<MyPhysics.HitInfo>();
        MyCubeGrid m_grid = null;

        #region cube grid properties

        private MyCubeGridRenderData m_renderData;

        public MyCubeGridRenderData RenderData { get { return m_renderData; } }

		private MyParticleEffect m_atmosphericEffect = null;
		const float m_atmosphericEffectMinSpeed = 75.0f;
		const float m_atmosphericEffectMinFade = 0.85f;
        const int m_atmosphericEffectVoxelContactDelay = 5000;
        private int m_lastVoxelContactTime = 0;

		float m_lastWorkingIntersectDistance = 0.0f;
		static List<Vector3> m_tmpCornerList = new List<Vector3>();

        private List<IMyBlockAdditionalModelGenerator> m_additionalModelGenerators = new List<IMyBlockAdditionalModelGenerator>();
        public List<IMyBlockAdditionalModelGenerator> AdditionalModelGenerators { get { return m_additionalModelGenerators; } }

        public uint[] AdditionalRenderObjects = new uint[0];

        // Create additional model generators from plugins using reflection.
        public void CreateAdditionalModelGenerators(MyCubeSize gridSizeEnum)
        {
            Assembly[] assemblies = new Assembly[] {
                Assembly.GetExecutingAssembly(),
                MyPlugins.GameAssembly,
                MyPlugins.SandboxAssembly,
                MyPlugins.UserAssembly,
            };

            foreach (var assembly in assemblies)
            {
                if (assembly == null)
                    continue;

                // Lookup
                Type lookupType = typeof(IMyBlockAdditionalModelGenerator);
                IEnumerable<Type> lookupTypes = assembly.GetTypes().Where(
                        t => lookupType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

                // Create instances
                foreach (var type in lookupTypes)
                {
                    IMyBlockAdditionalModelGenerator generator = Activator.CreateInstance(type) as IMyBlockAdditionalModelGenerator;
                    if (generator.Initialize(m_grid, gridSizeEnum))
                        AdditionalModelGenerators.Add(generator);
                    else
                        generator.Close();
                }
            }
        }

        public MyCubeSize GridSizeEnum
        {
            get
            {
                return m_grid.GridSizeEnum;
            }
        }
        public float GridSize
        {
            get
            {
                return m_grid.GridSize;
            }
        }
        public bool IsStatic 
        { 
            get 
            {
                return m_grid.IsStatic;
            } 
        }

        public void CloseModelGenerators()
        {
             foreach (var modelGenerator in AdditionalModelGenerators)
             {
                modelGenerator.Close();
             }
             AdditionalModelGenerators.Clear();
        }

        #endregion

        #region renderData properties
        public void RebuildDirtyCells()
        {
            m_renderData.RebuildDirtyCells(GetRenderFlags());
        }
        #endregion

        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_grid = Container.Entity as MyCubeGrid;
        }

		public override void OnBeforeRemovedFromContainer()
		{
			base.OnBeforeRemovedFromContainer();

			if(m_atmosphericEffect != null)
			{
				MyParticlesManager.RemoveParticleEffect(m_atmosphericEffect);
				m_atmosphericEffect = null;
			}
		}

        public override void Draw()
        {
            base.Draw();

            foreach (var block in m_grid.BlocksForDraw)
            {
                if (MyRenderProxy.VisibleObjectsRead.Contains(block.Render.RenderObjectIDs[0]))
                {
                    block.Render.Draw();
                }
            }

            if (MyCubeGrid.ShowCenterOfMass && !IsStatic && Container.Entity.Physics != null && Container.Entity.Physics.HasRigidBody)
            {
                var matrix = Container.Entity.Physics.GetWorldMatrix();
                var center = Container.Entity.Physics.CenterOfMassWorld;
                var cam = MySector.MainCamera.Position;
                var dist = Vector3.Distance(cam, center);
                bool draw = false;
                if (dist < 30)
                    draw = true;
                else if (dist < 200)
                {
                    draw = true;
                    MyPhysics.CastRay(cam, center, m_tmpHitList, MyPhysics.DynamicDoubledCollisionLayer);
                    foreach (var hit in m_tmpHitList)
                    {
                        if (hit.HkHitInfo.GetHitEntity() != this)
                        {
                            draw = false;
                            break;
                        }
                    }
                    m_tmpHitList.Clear();
                }
                if (draw)
                {
                    float size = MathHelper.Lerp(1, 9, dist / 200);
                    var mat = "WeaponLaserIgnoreDepth";
                    var color = Color.Yellow.ToVector4();
                    var thickness = 0.02f * size;
                    MySimpleObjectDraw.DrawLine(center - matrix.Up * 0.5f * size, center + matrix.Up * 0.5f * size, mat, ref color, thickness);
                    MySimpleObjectDraw.DrawLine(center - matrix.Forward * 0.5f * size, center + matrix.Forward * 0.5f * size, mat, ref color, thickness);
                    MySimpleObjectDraw.DrawLine(center - matrix.Right * 0.5f * size, center + matrix.Right * 0.5f * size, mat, ref color, thickness);
                    Sandbox.Graphics.TransparentGeometry.MyTransparentGeometry.AddBillboardOriented("RedDotIgnoreDepth", Color.White.ToVector4(), center, MySector.MainCamera.LeftVector, MySector.MainCamera.UpVector, 0.1f * size, priority: 1);
                }
            }
            if (MyCubeGrid.ShowGridPivot)
            {
                var matrix = Container.Entity.WorldMatrix;
                var pos = matrix.Translation;
                var cam = MySector.MainCamera.Position;
                var dist = Vector3.Distance(cam, pos);
                bool draw = false;
                if (dist < 30)
                    draw = true;
                else if (dist < 200)
                {
                    draw = true;
                    MyPhysics.CastRay(cam, pos, m_tmpHitList, MyPhysics.DynamicDoubledCollisionLayer);
                    foreach (var hit in m_tmpHitList)
                    {
                        if (hit.HkHitInfo.GetHitEntity() != this)
                        {
                            draw = false;
                            break;
                        }
                    }
                    m_tmpHitList.Clear();
                }
                if (draw)
                {
                    float size = MathHelper.Lerp(1, 9, dist / 200);
                    var mat = "WeaponLaserIgnoreDepth";
                    var thickness = 0.02f * size;
                    var color = Color.Blue.ToVector4();
                    MySimpleObjectDraw.DrawLine(pos, pos + matrix.Up * 0.5f * size, mat, ref color, thickness);
                    color = Color.Green.ToVector4();
                    MySimpleObjectDraw.DrawLine(pos, pos + matrix.Forward * 0.5f * size, mat, ref color, thickness);
                    color = Color.Red.ToVector4();
                    MySimpleObjectDraw.DrawLine(pos, pos + matrix.Right * 0.5f * size, mat, ref color, thickness);
                    Sandbox.Graphics.TransparentGeometry.MyTransparentGeometry.AddBillboardOriented("RedDotIgnoreDepth", Color.White.ToVector4(), pos, MySector.MainCamera.LeftVector, MySector.MainCamera.UpVector, 0.1f * size, priority: 1);
                }
            }

            if (MyCubeGrid.ShowStructuralIntegrity)
            {
                if (m_grid.StructuralIntegrity == null)
                {
                    if (MyFakes.ENABLE_STRUCTURAL_INTEGRITY)
                    {
                        m_grid.CreateStructuralIntegrity();
                        
                        if (m_grid.StructuralIntegrity != null)
                        {
                            m_grid.StructuralIntegrity.EnabledOnlyForDraw = true;
                        }
                    }
                }
                else
                    m_grid.StructuralIntegrity.Draw();
            }
            else
                if (m_grid.StructuralIntegrity != null && m_grid.StructuralIntegrity.EnabledOnlyForDraw)
                {
                    m_grid.CloseStructuralIntegrity();
                }

			if (MyFakes.ENABLE_ATMOSPHERIC_ENTRYEFFECT)
			{
				ProfilerShort.Begin("DrawAtmosphericEntryEffect");
				DrawAtmosphericEntryEffect();
				ProfilerShort.End();
			}
        }

		private void DrawAtmosphericEntryEffect()
		{
			var naturalGravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(m_grid.PositionComp.GetPosition());
			var naturalGravityMagnitude = naturalGravity.Length();

            bool noPhysics = m_grid.Physics == null;
            bool recentlyHitVoxel = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastVoxelContactTime < m_atmosphericEffectVoxelContactDelay;
            bool tooLowSpeed = !noPhysics && (m_grid.Physics.LinearVelocity.Length() < m_atmosphericEffectMinSpeed);
			if (noPhysics || recentlyHitVoxel || tooLowSpeed)
			{
				if (m_atmosphericEffect != null)
				{
					MyParticlesManager.RemoveParticleEffect(m_atmosphericEffect);
					m_atmosphericEffect = null;
				}

				return;
			}
			var currentVelocity = m_grid.Physics.LinearVelocity;
			var currentVelocityDirection = Vector3.Normalize(currentVelocity);
			var currentSpeed = currentVelocity.Length();

			if (m_atmosphericEffect == null)
			{
				if(!MyParticlesManager.TryCreateParticleEffect(52, out m_atmosphericEffect))
					return;
			}

			BoundingBox worldAABB = (BoundingBox)m_grid.PositionComp.WorldAABB;
			var aabbCenter = worldAABB.Center;
			var directionFaceCenter = new Vector3();
			Debug.Assert(m_tmpCornerList.Count == 0);
			foreach (var corner in worldAABB.GetCorners())
			{
				var centerToCorner = corner - aabbCenter;
				if (centerToCorner.Dot(currentVelocityDirection) > 0.01f)
				{
					m_tmpCornerList.Add(corner);
					directionFaceCenter += corner;
					if (m_tmpCornerList.Count == 4)
						break;
				}
			}
			if(m_tmpCornerList.Count > 0)
				directionFaceCenter /= m_tmpCornerList.Count;

			Plane plane = new Plane(directionFaceCenter, -currentVelocityDirection);

			m_tmpCornerList.Clear();
			var startPosition = m_grid.Physics.CenterOfMassWorld;
			float? intersectDistance = new Ray(startPosition, currentVelocityDirection).Intersects(plane);
			m_lastWorkingIntersectDistance = intersectDistance ?? m_lastWorkingIntersectDistance;
			var intersectPoint = startPosition + 0.875f * currentVelocityDirection * m_lastWorkingIntersectDistance;
	
			Matrix worldMatrix = Matrix.Identity;
			worldMatrix.Translation = intersectPoint;
			worldMatrix.Forward = currentVelocityDirection;
			var forwardPerpendicular = Vector3.Transform(currentVelocityDirection, Quaternion.CreateFromAxisAngle(m_grid.PositionComp.WorldMatrix.Left, (float)Math.PI / 2.0f));
			worldMatrix.Up = Vector3.Normalize(Vector3.Reject(m_grid.PositionComp.WorldMatrix.Left, forwardPerpendicular));
			worldMatrix.Left = worldMatrix.Up.Cross(worldMatrix.Forward);

			var atmosphericDensityMultiplier = MyGravityProviderSystem.CalculateHighestNaturalGravityMultiplierInPoint(m_grid.PositionComp.GetPosition());

			m_atmosphericEffect.UserScale = (float)worldAABB.ProjectedArea(currentVelocityDirection) / (float)Math.Pow(38.0 * m_grid.GridSize, 2.0);
			m_atmosphericEffect.UserAxisScale = Vector3.Normalize(new Vector3(1.0f, 1.0f, 1.0f + 1.5f*(m_grid.Physics.LinearVelocity.Length() - m_atmosphericEffectMinSpeed) / (MyGridPhysics.ShipMaxLinearVelocity() - m_atmosphericEffectMinSpeed)));
			m_atmosphericEffect.WorldMatrix = worldMatrix;
			m_atmosphericEffect.UserColorMultiplier = new Vector4(MathHelper.Clamp((currentSpeed - m_atmosphericEffectMinSpeed) / (m_atmosphericEffectMinSpeed * 0.5f) * (float)Math.Pow(atmosphericDensityMultiplier, 1.5), 0.0f, m_atmosphericEffectMinFade));
		}

        public void ResetLastVoxelContactTimer()
        {
            m_lastVoxelContactTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public override void AddRenderObjects()
        {
            MyCubeGrid grid = Container.Entity as MyCubeGrid;
            if (m_renderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                return;

            if (grid.IsDirty())
            {
                grid.UpdateInstanceData();
            }
        }
        public override void RemoveRenderObjects()
        {
            for (int index = 0; index < m_renderObjectIDs.Length; index++)
            {
                if (m_renderObjectIDs[index] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                    ReleaseRenderObjectID(index);
            }

            for (int i = 0; i < AdditionalRenderObjects.Length; i++)
            {
                if (AdditionalRenderObjects[i] != MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    MyEntities.RemoveRenderObjectFromMap(AdditionalRenderObjects[i]);
                    VRageRender.MyRenderProxy.RemoveRenderObject(AdditionalRenderObjects[i]);
                    AdditionalRenderObjects[i] = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
                }
            }
        }

        protected override void UpdateRenderObjectVisibility(bool visible)
        {
            base.UpdateRenderObjectVisibility(visible);
            for (int i = 0; i < AdditionalRenderObjects.Length; i++)
            {
                if (AdditionalRenderObjects[i] != MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    VRageRender.MyRenderProxy.UpdateRenderObjectVisibility(AdditionalRenderObjects[i], visible, Container.Entity.NearFlag);
                }
            }
        }
        #endregion
    }
}
