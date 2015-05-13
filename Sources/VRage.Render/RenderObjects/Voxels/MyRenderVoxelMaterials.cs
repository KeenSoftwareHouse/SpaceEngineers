using System;
using System.Collections.Generic;
using VRage;
using VRage.Utils;

namespace VRageRender
{
    class MyRenderVoxelMaterials : MyRenderComponentBase
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.RenderVoxelMaterials;
        }

        static Dictionary<byte, MyRenderVoxelMaterial> m_materials;

        static MyRenderVoxelMaterials()
        {
            m_materials = new Dictionary<byte, MyRenderVoxelMaterial>();
        }

        public static void MarkAllAsUnused()
        {
            if (m_materials != null)
            {
                foreach (var mat in m_materials)
                {
                    mat.Value.UseFlag = false;
                }
            }
        }

        public static void UnloadUnused()
        {
            if (m_materials != null)
            {
                foreach (var mat in m_materials)
                {
                    if (mat.Value.UseFlag == false)
                    {
                        mat.Value.UnloadContent();
                    }
                }
            }
        }

        //  Here we load only textures, effects, etc, no voxel-maps.
        public override void LoadContent()
        {
            MyRender.Log.WriteLine("MyVoxelMaterials.LoadContent() - START");
            MyRender.Log.IncreaseIndent();
            MyRender.GetRenderProfiler().StartProfilingBlock("MyVoxelMaterials::LoadContent");

            if (m_materials != null)
            {
                foreach (var mat in m_materials)
                {
                    mat.Value.LoadContent();
                }
            }

            MyRender.GetRenderProfiler().EndProfilingBlock();
            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyVoxelMaterials.LoadContent() - END");
        }

        public override void UnloadContent()
        {
            MyRender.Log.WriteLine("MyVoxelMaterials.UnloadContent - START");
            MyRender.Log.IncreaseIndent();

            Unload();

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyVoxelMaterials.UnloadContent - END");
        }

        static internal void Unload()
        {
            if (m_materials != null)
            {
                foreach (var m in m_materials)
                {
                    m.Value.UnloadContent();
                }
            }
        }

        static internal void Clear()
        {
            Unload();
            m_materials.Clear();
        }

        internal static void Add(ref MyRenderVoxelMaterialData materialData)
        {
            //  Check if not yet assigned
            MyDebug.AssertRelease(!m_materials.ContainsKey(materialData.Index));

            //  Create and add into array
            MyRenderVoxelMaterial voxelMaterial = new MyRenderVoxelMaterial(materialData);
            m_materials[materialData.Index] = voxelMaterial;
        }

        internal static MyRenderVoxelMaterial Get(byte materialIndex)
        {
            return m_materials[materialIndex];
        }

        public static int GetMaterialsCount()
        {
            return (m_materials != null) ?m_materials.Count : 0;
        }

    }
}
