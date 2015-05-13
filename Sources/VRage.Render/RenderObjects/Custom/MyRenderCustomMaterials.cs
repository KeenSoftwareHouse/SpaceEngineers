using System;
using System.Collections.Generic;
using SysUtils.Utils;
using VRage.Common;
using VRage.Common.Utils;

namespace VRageRender
{
    class MyRenderCustomMaterials : MyRenderComponentBase
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.CustomMaterial;
        }

        static Dictionary<int, MyRenderCustomMaterial> m_materials;

        static MyRenderCustomMaterials()
        {
            m_materials = new Dictionary<int, MyRenderCustomMaterial>();
        }

        //  Here we load only textures
        public override void LoadContent()
        {
            /*MyRender.Log.WriteLine("MyRenderCustomMaterials.LoadContent() - START");
            MyRender.Log.IncreaseIndent();
            MyRender.GetRenderProfiler().StartProfilingBlock("MyRenderCustomMaterials::LoadContent");

            if (m_materials != null)
            {
                foreach (var mat in m_materials)
                    mat.Value.LoadContent();
            }

            MyRender.GetRenderProfiler().EndProfilingBlock();
            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyRenderCustomMaterials.LoadContent() - END");*/
        }

        public override void UnloadContent()
        {
            /*MyRender.Log.WriteLine("MyRenderCustomMaterials.UnloadContent - START");
            MyRender.Log.IncreaseIndent();

            Unload();

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyRenderCustomMaterials.UnloadContent - END");*/
        }

        internal static void Unload()
        {
            if (m_materials != null)
            {
                foreach (var m in m_materials)
                    m.Value.UnloadContent();
            }
        }

        internal static void Clear()
        {
            Unload();
            m_materials.Clear();
        }

        internal static void Add(ref MyRenderCustomMaterialData materialData)
        {
            //  Check if not yet assigned
            MyDebug.AssertRelease(!m_materials.ContainsKey(materialData.Index));

            //  Create and add into array
            var voxelMaterial = new MyRenderCustomMaterial(materialData);
            m_materials[materialData.Index] = voxelMaterial;
        }

        internal static MyRenderCustomMaterial Get(int materialIndex)
        {
            return m_materials[materialIndex];
        }

        public static int GetMaterialsCount()
        {
            return (m_materials != null) ? m_materials.Count : 0;
        }

    }
}
