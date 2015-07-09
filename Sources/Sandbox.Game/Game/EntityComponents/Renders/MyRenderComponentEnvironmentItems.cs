using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.EnvironmentItems;

namespace Sandbox.Game.Components
{
    //[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    //class MyMedievalDebugDrawHelper : MySessionComponentBase
    //{
    //    public static MyMedievalDebugDrawHelper Static;

    //    private List<BoundingBox> m_boxes = new List<BoundingBox>();


    //    public override void LoadData()
    //    {
    //        base.LoadData();

    //        Static = this;
    //    }

    //    public override void Draw()
    //    {
    //        base.Draw();
    //        foreach (var box in m_boxes) 
    //        {
    //            MyRenderProxy.DebugDrawAABB(box, Vector3.One, 1f, 1f, true);
    //        }
    //    }

    //    public void AddAabb(BoundingBox aabb)
    //    {
    //        m_boxes.Add(aabb);
    //    }

    //    public void Clear()
    //    {
    //        m_boxes.Clear();
    //    }
    //}

    class MyRenderComponentEnvironmentItems : MyRenderComponent
    {
        internal readonly MyEnvironmentItems EnvironmentItems;

        internal MyRenderComponentEnvironmentItems(MyEnvironmentItems environmentItems)
        {
            EnvironmentItems = environmentItems;
        }

        public override void AddRenderObjects()
        {
            //if (m_itemsData.Count > 0)
            //{
            //    m_renderObjectIDs = new uint[m_itemsData.Count];

            //    for (int i = 0; i < m_renderObjectIDs.Length; ++i)
            //        m_renderObjectIDs[i] = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;

            //    for (int i = 0; i < m_renderObjectIDs.Length; ++i)
            //    {
            //        MyMedievalEnvironmentItemData data = m_itemsData[i];
            //        uint renderObjectId = VRageRender.MyRenderProxy.CreateRenderEntity(
            //            "Environment item part: " + MyModel.GetId(data.Model),
            //            data.Model,
            //            data.WorldMatrix,
            //            MyMeshDrawTechnique.MESH,
            //            RenderFlags.CastShadows | RenderFlags.Visible,
            //            CullingOptions.Default,
            //            Vector3.One,
            //            ColorMaskHsv,
            //            0
            //        );

            //        SetRenderObjectID(i, renderObjectId);
            //    }
            //}
            //else
            //{
            //    base.AddRenderObjects();
            //}
        }

        public override void RemoveRenderObjects()
        {
            foreach (var pair in EnvironmentItems.Sectors)
            {
                pair.Value.UnloadRenderObjects();
            }

            foreach (var pair in EnvironmentItems.Sectors)
            {
                pair.Value.ClearInstanceData();
            }

            //MyMedievalDebugDrawHelper.Static.Clear();
        }
    }
}
