using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Game.Entity;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GUI.DebugInputComponents
{
    public partial class MyVoxelDebugInputComponent
    {
        public class PhysicsComponent : MyDebugComponent
        {
            private MyVoxelDebugInputComponent m_comp;

            private bool m_debugDraw;

            private class PredictionInfo
            {
                public MyVoxelBase Body;
                public Vector4I Id;
                public MyOrientedBoundingBoxD Bounds;
            }

            private ConcurrentCachingList<PredictionInfo> m_list = new ConcurrentCachingList<PredictionInfo>();

            public static PhysicsComponent Static;

            [Conditional("DEBUG")]
            public void Add(MatrixD worldMatrix, BoundingBox box, Vector4I id, MyVoxelBase voxel)
            {
                if (m_list.Count > 1900)
                    m_list.ClearList();

                voxel = voxel.RootVoxel;
                box.Translate(-voxel.SizeInMetresHalf);
                //box.Translate(voxel.StorageMin);


                m_list.Add(new PredictionInfo
                {
                    Id = id,
                    Bounds = MyOrientedBoundingBoxD.Create((BoundingBoxD)box, voxel.WorldMatrix),
                    Body = voxel
                });
            }

            public PhysicsComponent(MyVoxelDebugInputComponent comp)
            {
                m_comp = comp;

                Static = this;

                AddShortcut(MyKeys.NumPad8, true, false, false, false, () => "Clear boxes", () =>
                {
                    m_list.ClearList();
                    return false;
                });
            }

            public override void Draw()
            {
                base.Draw();

                if (MySession.Static == null) m_list.ClearList();

                m_list.ApplyChanges();

                Text("Queried Out Areas: {0}", m_list.Count);

                foreach (var info in m_list)
                {
                    MyRenderProxy.DebugDrawOBB(info.Bounds, Color.Cyan, .2f, true, true);

                    //MyRenderProxy.DebugDrawText3D(info.Bounds.Center, string.Format("{0}: {1}", info.Body.StorageName, info.Id), Color.Cyan, .8f, true);
                }
            }

            public override string GetName()
            {
                return "Physics";
            }
        }
    }
}
