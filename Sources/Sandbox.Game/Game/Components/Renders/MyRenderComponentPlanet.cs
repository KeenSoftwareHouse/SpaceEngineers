using Sandbox.Common.Components;
using Sandbox.Engine.Voxels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Import;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using VRage.Components;
using Sandbox.Game.Entities;

namespace Sandbox.Game.Components
{
    class MyRenderComponentPlanet : MyRenderComponentVoxelMap
    {
        MyPlanet m_planet = null;

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_planet = Entity as MyPlanet;
        }

        public override void AddRenderObjects()
        {
            var minCorner = m_planet.PositionLeftBottomCorner;

            m_renderObjectIDs = new uint[] { MyRenderProxy.RENDER_ID_UNASSIGNED, MyRenderProxy.RENDER_ID_UNASSIGNED, MyRenderProxy.RENDER_ID_UNASSIGNED };
            Debug.Assert((m_planet.Size % MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS) == Vector3I.Zero);
            var clipmapSizeLod0 = m_planet.Size / MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS;

            Vector3 atmosphereWavelengths = new Vector3();

            atmosphereWavelengths.X = 1.0f / (float)Math.Pow(m_planet.AtmosphereWavelengths.X, 4.0);
            atmosphereWavelengths.Y = 1.0f / (float)Math.Pow(m_planet.AtmosphereWavelengths.Y, 4.0);
            atmosphereWavelengths.Z = 1.0f / (float)Math.Pow(m_planet.AtmosphereWavelengths.Z, 4.0);

            SetRenderObjectID(0,
                MyRenderProxy.CreateClipmap(
                    MatrixD.CreateTranslation(minCorner),                    
                    clipmapSizeLod0,
                    m_planet.ScaleGroup,
                    m_planet.PositionComp.GetPosition(),
                    m_planet.AtmosphereRadius,
                    m_planet.MinimumSurfaceRadius,
                    m_planet.HasAtmosphere,
                    atmosphereWavelengths));

            if (m_planet.HasAtmosphere)
            {
                MatrixD matrix = MatrixD.Identity * m_planet.AtmosphereRadius;
                matrix.M44 = 1;
                matrix.Translation = Entity.PositionComp.GetPosition();

                SetRenderObjectID(1, VRageRender.MyRenderProxy.CreateRenderEntityAtmosphere(this.Entity.GetFriendlyName() + " " + this.Entity.EntityId.ToString(),
                      "Models\\Environment\\Atmosphere_sphere.mwm",
                      matrix,
                     MyMeshDrawTechnique.ATMOSPHERE,
                     RenderFlags.Visible,
                     GetRenderCullingOptions(),
                     m_planet.AtmosphereRadius,
                     m_planet.MinimumSurfaceRadius,
                     atmosphereWavelengths));

                  SetRenderObjectID(2, VRageRender.MyRenderProxy.CreateRenderEntityAtmosphere(this.Entity.GetFriendlyName() + " " + this.Entity.EntityId.ToString(),
                      "Models\\Environment\\Atmosphere_sphere.mwm",
                      matrix,
                     MyMeshDrawTechnique.PLANET_SURFACE,
                     RenderFlags.Visible,
                     GetRenderCullingOptions(),
                     m_planet.AtmosphereRadius,
                     m_planet.MinimumSurfaceRadius,
                     atmosphereWavelengths));
            }
        }

    }
}
