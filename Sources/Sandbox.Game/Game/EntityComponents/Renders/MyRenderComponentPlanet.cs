using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Library.Utils;
using VRageMath;
using VRageRender;
using VRage.Game.Entity;
using VRage.Voxels;
using VRageRender.Import;
using VRageRender.Messages;

namespace Sandbox.Game.Components
{
    class MyRenderComponentPlanet : MyRenderComponentVoxelMap
    {
        MyPlanet m_planet = null;

        private int m_shadowHelperRenderObjectIndex = -1;
        private int m_atmosphereRenderIndex = -1;
        readonly List<int> m_cloudLayerRenderObjectIndexList = new List<int>();

		int m_fogUpdateCounter = 0;
		static bool lastSentFogFlag = true;
		bool m_oldNeedsDraw = false;

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_planet = Entity as MyPlanet;
			m_oldNeedsDraw = NeedsDraw;
			NeedsDraw = true;
        }

		public override void OnBeforeRemovedFromContainer()
		{
			base.OnBeforeRemovedFromContainer();
			NeedsDraw = m_oldNeedsDraw;
            m_planet = null;
        }

        public ContainmentType IntersectStorage(ref BoundingBox bb, bool lazy)
        {
            if (Entity != null)
            {
                var voxel = (MyVoxelBase) Entity;
                bb.Translate(voxel.StorageMin);
                if (voxel.Storage != null)
                    return voxel.Storage.Intersect(ref bb, lazy);
            }
            return ContainmentType.Disjoint;
        }

		public override void AddRenderObjects()
		{
			var minCorner = m_planet.PositionLeftBottomCorner;

			m_renderObjectIDs = new uint[16];

			for (int index = 0; index < 16; ++index)
				m_renderObjectIDs[index] = MyRenderProxy.RENDER_ID_UNASSIGNED;

		    int runningRenderObjectIndex = 0;

				Debug.Assert((m_planet.Size % MyVoxelCoordSystems.RenderCellSizeInLodVoxels(0)) == Vector3I.Zero);
			var clipmapSizeLod0 = m_planet.Size / MyVoxelCoordSystems.RenderCellSizeInLodVoxels(0);

			Vector3 atmosphereWavelengths = new Vector3();

			atmosphereWavelengths.X = 1.0f / (float)Math.Pow(m_planet.AtmosphereWavelengths.X, 4.0);
			atmosphereWavelengths.Y = 1.0f / (float)Math.Pow(m_planet.AtmosphereWavelengths.Y, 4.0);
			atmosphereWavelengths.Z = 1.0f / (float)Math.Pow(m_planet.AtmosphereWavelengths.Z, 4.0);

            var voxel = (Entity as MyVoxelBase);
            
			SetRenderObjectID(runningRenderObjectIndex++,
				MyRenderProxy.CreateClipmap(
					MatrixD.CreateTranslation(minCorner),
					clipmapSizeLod0,
					m_planet.ScaleGroup,
					m_planet.PositionComp.GetPosition(),
					m_planet.AtmosphereRadius,
					m_planet.AverageRadius,
					m_planet.HasAtmosphere,
					atmosphereWavelengths,
					m_planet.SpherizeWithDistance,
					RenderFlags.Visible | RenderFlags.DrawOutsideViewDistance | RenderFlags.CastShadows,
                    IntersectStorage));

			if (m_planet.HasAtmosphere)
			{
				MatrixD matrix = MatrixD.Identity * m_planet.AtmosphereRadius;
				matrix.M44 = 1;
				matrix.Translation = Entity.PositionComp.GetPosition();

			    m_atmosphereRenderIndex = runningRenderObjectIndex;

				SetRenderObjectID(runningRenderObjectIndex++, MyRenderProxy.CreateRenderEntityAtmosphere(this.Entity.GetFriendlyName() + " " + this.Entity.EntityId.ToString(),
					  "Models\\Environment\\Atmosphere_sphere.mwm",
					  matrix,
					 MyMeshDrawTechnique.ATMOSPHERE,
					 RenderFlags.Visible | RenderFlags.DrawOutsideViewDistance,
					 GetRenderCullingOptions(),
					 m_planet.AtmosphereRadius,
					 m_planet.AverageRadius,
					 atmosphereWavelengths));

				SetRenderObjectID(runningRenderObjectIndex++, MyRenderProxy.CreateRenderEntityAtmosphere(this.Entity.GetFriendlyName() + " " + this.Entity.EntityId.ToString(),
					"Models\\Environment\\Atmosphere_sphere.mwm",
					matrix,
				   MyMeshDrawTechnique.PLANET_SURFACE,
				   RenderFlags.Visible | RenderFlags.DrawOutsideViewDistance,
				   GetRenderCullingOptions(),
				   m_planet.AtmosphereRadius,
				   m_planet.AverageRadius,
				   atmosphereWavelengths));

				UpdateAtmosphereSettings(m_planet.AtmosphereSettings);
			}

		    m_shadowHelperRenderObjectIndex = runningRenderObjectIndex;
		    MatrixD shadowHelperWorldMatrix = MatrixD.CreateScale(m_planet.MinimumRadius);
		    shadowHelperWorldMatrix.Translation = m_planet.WorldMatrix.Translation;
            SetRenderObjectID(runningRenderObjectIndex++, MyRenderProxy.CreateRenderEntity("Shadow helper", "Models\\Environment\\Sky\\ShadowHelperSphere.mwm",
		        shadowHelperWorldMatrix,
		        MyMeshDrawTechnique.MESH,
                RenderFlags.Visible | RenderFlags.CastShadows | RenderFlags.DrawOutsideViewDistance | RenderFlags.NoBackFaceCulling | RenderFlags.SkipInMainView,
		        CullingOptions.Default,
		        Color.White, new Vector3(1, 1, 1)));

			MyPlanetGeneratorDefinition definition = m_planet.Generator;
			if (!MyFakes.ENABLE_PLANETARY_CLOUDS || definition == null || definition.CloudLayers == null)
				return;

			foreach (var cloudLayer in definition.CloudLayers)
			{
				double minScaledAltitude = (m_planet.AverageRadius + m_planet.MaximumRadius)/2.0;
				double layerAltitude = minScaledAltitude + (m_planet.MaximumRadius - minScaledAltitude) * cloudLayer.RelativeAltitude;
				Vector3D rotationAxis = Vector3D.Normalize(cloudLayer.RotationAxis == Vector3D.Zero ? Vector3D.Up : cloudLayer.RotationAxis);

                int index = runningRenderObjectIndex + m_cloudLayerRenderObjectIndexList.Count;
				SetRenderObjectID(index,
					MyRenderProxy.CreateRenderEntityCloudLayer(this.Entity.GetFriendlyName() + " " + this.Entity.EntityId.ToString(),
					cloudLayer.Model,
                    cloudLayer.Textures,
					Entity.PositionComp.GetPosition(),
					layerAltitude,
					minScaledAltitude,
					cloudLayer.ScalingEnabled,
					cloudLayer.FadeOutRelativeAltitudeStart,
					cloudLayer.FadeOutRelativeAltitudeEnd,
					cloudLayer.ApplyFogRelativeDistance,
					m_planet.MaximumRadius,
					MyMeshDrawTechnique.CLOUD_LAYER,
					RenderFlags.Visible | RenderFlags.DrawOutsideViewDistance,
					GetRenderCullingOptions(),
					rotationAxis,
					cloudLayer.AngularVelocity,
					cloudLayer.InitialRotation));
				m_cloudLayerRenderObjectIndexList.Add(index);
			}
		    runningRenderObjectIndex += definition.CloudLayers.Count;
		}

		public override void Draw()
		{
			if (m_oldNeedsDraw)
				base.Draw();

		    float cameraDistanceFromCenter = Vector3.Distance(MySector.MainCamera.Position, m_planet.WorldMatrix.Translation);
            MatrixD shadowHelperWorldMatrix = MatrixD.CreateScale(m_planet.MinimumRadius * Math.Min(cameraDistanceFromCenter / m_planet.MinimumRadius, 1f)*0.997f);
		    shadowHelperWorldMatrix.Translation = m_planet.PositionComp.WorldMatrix.Translation;
            MyRenderProxy.UpdateRenderObject(m_renderObjectIDs[m_shadowHelperRenderObjectIndex], ref shadowHelperWorldMatrix, false);

            

		    DrawFog();
		}

        private void DrawFog()
        {
            if (!MyFakes.ENABLE_CLOUD_FOG)
                return;

            if (m_fogUpdateCounter-- <= 0)
            {
                m_fogUpdateCounter = (int)(100 * (0.8f + MyRandom.Instance.NextFloat() * 0.4f));
                Vector3D cameraPosition = MySector.MainCamera.Position;
                Vector3D planetPosition = m_planet.PositionComp.GetPosition();
                double comparisonRadius = m_planet.AtmosphereRadius * 2;

                if ((cameraPosition - planetPosition).LengthSquared() > comparisonRadius * comparisonRadius)
                    return;

                m_fogUpdateCounter = (int)(m_fogUpdateCounter * 0.67f);	// Update more often when we're actually near a planet

                bool shouldDrawFog = !IsPointInAirtightSpace(cameraPosition);

                if (lastSentFogFlag == shouldDrawFog)
                    return;

                lastSentFogFlag = shouldDrawFog;
                MyRenderProxy.UpdateCloudLayerFogFlag(shouldDrawFog);
            }
        }

        public void UpdateAtmosphereSettings(MyAtmosphereSettings settings)
        {
            MyRenderProxy.UpdateAtmosphereSettings(m_renderObjectIDs[m_atmosphereRenderIndex], settings);
        }

		private bool IsPointInAirtightSpace(Vector3D worldPosition)
		{
			if (!MySession.Static.Settings.EnableOxygen)
				return true;

			bool cameraInAirtightSpace = false;
			var sphere = new BoundingSphereD(worldPosition, 0.1);
			List<MyEntity> entityList = null;
			try
			{
				entityList = MyEntities.GetEntitiesInSphere(ref sphere);

				foreach(var entity in entityList)
				{
					var grid = entity as MyCubeGrid;

					if (grid == null || grid.GridSystems.GasSystem == null)
						continue;

					var safeOxygenBlock = grid.GridSystems.GasSystem.GetSafeOxygenBlock(worldPosition);
					
					if(safeOxygenBlock.Room == null || !safeOxygenBlock.Room.IsPressurized)
						continue;

					cameraInAirtightSpace = true;
					break;
				}
			}
			finally
			{
				if (entityList != null)
					entityList.Clear();
			}

			return cameraInAirtightSpace;
		}
    }
}
