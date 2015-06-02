using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Threading;

using Sandbox.Game.Entities;
using Sandbox.Common.Components;
using VRage.Import;
using VRage.Components;
using VRage.ModAPI;

namespace Sandbox.Game.Components
{
    public class MyRenderComponent : MyRenderComponentBase
    {
        public override void AddRenderObjects()
        {
            if (m_model == null)
                return;

            if (m_renderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                return;

            SetRenderObjectID(0, VRageRender.MyRenderProxy.CreateRenderEntity
                (
                 Container.Entity.GetFriendlyName() + " " + Container.Entity.EntityId.ToString(),
                 m_model.AssetName,
                 Container.Entity.PositionComp.WorldMatrix,
                 MyMeshDrawTechnique.MESH,
                 GetRenderFlags(),
                 GetRenderCullingOptions(),
                 m_diffuseColor,
                 m_colorMaskHsv,
                 Transparency
                ));
        }

        protected MyModel m_model;                       //  LOD0 main model, used for rendering and also physics / col-det

        public MyModel Model
        {
            get { return m_model; }
            set { m_model = value; }
        }

        public override object ModelStorage
        {
            get
            {
                return Model;
            }
            set
            {
                Debug.Assert(value is MyModel, "Model storage can store only MyModel");
                Model = (MyModel)value;
            }
        }


        public override void SetRenderObjectID(int index, uint ID)
        {
            System.Diagnostics.Debug.Assert(m_renderObjectIDs[index] == VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED);
            m_renderObjectIDs[index] = ID;
            MyEntities.AddRenderObjectToMap(ID, Container.Entity);
        }

        public override void ReleaseRenderObjectID(int index)
        {
            if (m_renderObjectIDs[index] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                MyEntities.RemoveRenderObjectFromMap(m_renderObjectIDs[index]);
                VRageRender.MyRenderProxy.RemoveRenderObject(m_renderObjectIDs[index]);
                m_renderObjectIDs[index] = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
            }
        }

        /// <summary>
        /// Method is called defacto from Update, but only the last one in sequence, where 
        /// creating render messages makes sense
        /// </summary>
        public override void Draw()
        {
            var objToCameraSq = Vector3.DistanceSquared(MySector.MainCamera.Position, Container.Entity.PositionComp.GetPosition());

            //Disable glass for holograms (transparency < 0)
            if (m_model != null && m_model.GlassData != null && objToCameraSq < Container.Entity.MaxGlassDistSq && Transparency >= 0f)
            {
                string mat;
                var world = (Matrix)Container.Entity.PositionComp.WorldMatrix;

                for (int i = 0; i < m_model.GlassData.TriCount; i++)
                {
                    var tri = m_model.GetTriangle(m_model.GlassData.TriStart + i);
                    Vector3 p0 = m_model.GetVertex(tri.I0);
                    Vector3 p1 = m_model.GetVertex(tri.I1);
                    Vector3 p2 = m_model.GetVertex(tri.I2);

                    Vector3 worldP0 = new Vector3();
                    Vector3 worldP1 = new Vector3();
                    Vector3 worldP2 = new Vector3();

                    Vector3.Transform(ref p0, ref world, out worldP0);
                    Vector3.Transform(ref p1, ref world, out worldP1);
                    Vector3.Transform(ref p2, ref world, out worldP2);

                    var uv0 = m_model.GlassTexCoords[i * 3 + 0];
                    var uv1 = m_model.GlassTexCoords[i * 3 + 2];
                    var uv2 = m_model.GlassTexCoords[i * 3 + 1];

                    var normal = Vector3.Cross(worldP0 - worldP1, worldP0 - worldP2);

                    float dot = Vector3.Dot(normal, worldP0 - MySector.MainCamera.Position);
                    if (dot > 0)
                    {
                        mat = string.IsNullOrEmpty(m_model.GlassData.Material.GlassCW) ? "GlassCW" : m_model.GlassData.Material.GlassCW;
                    }
                    else
                    {
                        mat = string.IsNullOrEmpty(m_model.GlassData.Material.GlassCCW) ? "GlassCCW" : m_model.GlassData.Material.GlassCCW;
                    }

                    Vector3 n0 = Vector3.Zero;
                    Vector3 n1 = Vector3.Zero;
                    Vector3 n2 = Vector3.Zero;

                    bool smooth = m_model.GlassData.Material.GlassSmooth;

                    if (smooth)
                    {
                        n0 = m_model.GetVertexNormal(tri.I0);
                        n1 = m_model.GetVertexNormal(tri.I1);
                        n2 = m_model.GetVertexNormal(tri.I2);
                    }
                    else
                    {
                        n0 = n1 = n2 = Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));
                    }

                    var renderID = m_renderObjectIDs[0];

                    Vector3 center; // Old way: (p0 + p1 + p2) / 3
                    //Vector3 center = (p0 + p1 + p2) / 3;

                    float worldP0P1 = (worldP1 - worldP0).LengthSquared();
                    float worldP1P2 = (worldP2 - worldP1).LengthSquared();
                    float worldP2P0 = (worldP0 - worldP2).LengthSquared();

                    if (worldP0P1 > worldP1P2 && worldP0P1 > worldP2P0)
                    {
                        center = (worldP0 + worldP1) / 2;
                    }
                    else if (worldP1P2 > worldP2P0 && worldP1P2 > worldP0P1)
                    {
                        center = (worldP1 + worldP2) / 2;
                    }
                    else
                    {
                        center = (worldP2 + worldP0) / 2;
                    }

                    Sandbox.Graphics.TransparentGeometry.MyTransparentGeometry.AddTriangleBillboard(
                          p0, p1, p2,
                          n0, n1, n2,
                          uv0.ToVector2(), uv1.ToVector2(), uv2.ToVector2(),
                          mat, (int)renderID, center,
                          useNormals: smooth);
                }
            }
        }

        public override bool IsVisible()
        {
            if (!MyEntities.IsVisible(Container.Entity))
            {
                return false;
            }

            if (!this.Visible)
            {
                return false;
            }

            if (!Container.Entity.InScene)
            {
                return false;
            }

            return true;
        }

        public override bool NeedsDraw
        {
            get
            {
                return ((Container.Entity.Flags & EntityFlags.NeedsDraw) != 0);
            }
            set
            {
                bool hasChanged = value != NeedsDraw;

                if (hasChanged)                
                {
                    MyEntities.UnregisterForDraw(Container.Entity);
                    Container.Entity.Flags &= ~EntityFlags.NeedsDraw;

                    if (value)
                        Container.Entity.Flags |= EntityFlags.NeedsDraw;

                    if (Container.Entity.InScene)
                    {
                        MyEntities.RegisterForDraw(Container.Entity);
                    }
                }
            }
        }
    }
}
