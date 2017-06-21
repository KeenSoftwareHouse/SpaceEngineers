using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Threading;

using Sandbox.Game.Entities;

using VRage;
using VRage.Import;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Game.Models;
using VRage.Game;
using VRage.Profiler;
using VRageRender.Import;

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
                 Transparency,
                 maxViewDistance: float.MaxValue,
                 depthBias: DepthBias,
                 rescale: m_model.ScaleFactor
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

        public override void Draw() { }

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
                    Container.Entity.Flags &= ~EntityFlags.NeedsDraw;

                    if (value)
                        Container.Entity.Flags |= EntityFlags.NeedsDraw;
                    if (Container.Entity.InScene)
                    {
                        if (value)
                        {
                            MyEntities.RegisterForDraw(Container.Entity);
                        }
                        else
                        {
                            MyEntities.UnregisterForDraw(Container.Entity);
                        }
                    }
                }
            }
        }
    }
}
