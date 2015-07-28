using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Noise;
using VRageMath;
using VRage;
using System.IO.MemoryMappedFiles;
using System.IO;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.World.Generator
{
    class MyCompositeShapeOreDeposit
    {
        public readonly MyCsgShapeBase Shape;
        readonly MyVoxelMaterialDefinition m_material;

        virtual public void DebugDraw(ref Vector3D translation, ref Color materialColor)
        {
            Shape.DebugDraw(ref translation, materialColor);
            VRageRender.MyRenderProxy.DebugDrawText3D(Shape.Center() + translation, m_material.Id.SubtypeName, Color.White, 1f, false);
        }

        public MyCompositeShapeOreDeposit(MyCsgShapeBase shape, MyVoxelMaterialDefinition material)
        {
            Shape = shape;
            m_material = material;
        }

        public virtual MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos,float lodSize)
        {
            return m_material;
        }

        public virtual bool SpawnsFlora()
        {
            return m_material.SpawnsFlora;
        }

        public virtual void ReleaseMaps()
        { 
        }
    }

    class MyCompositePrecomputedOreDeposit : MyCompositeShapeOreDeposit
    {
        int m_resolution;
        MyCsgShapePrecomputed m_planetShape;
        MyCompositeOrePlanetDeposit m_oreDeposits = null;

        MemoryMappedFile[] m_file;
        MemoryMappedViewAccessor[] m_reader;

        public override void DebugDraw(ref Vector3D translation, ref Color materialColor)
        {
            Shape.DebugDraw(ref translation, materialColor);
            VRageRender.MyRenderProxy.DebugDrawText3D(Shape.Center() + translation, "layered", Color.White, 1f, false);

            m_oreDeposits.DebugDraw(ref translation, ref materialColor);
        }

        public MyCompositePrecomputedOreDeposit(MyCsgShapeBase shape, string path, MyCompositeOrePlanetDeposit oresDeposits,MyCsgShapePrecomputed planetShape) :
            base(shape, null)
        {
            m_planetShape = planetShape;
            m_oreDeposits = oresDeposits;
            m_file = new MemoryMappedFile[MyCsgPrecomputedHelpres.NUM_MAPS];
            m_reader = new MemoryMappedViewAccessor[MyCsgPrecomputedHelpres.NUM_MAPS];

            for (int i = 0; i < MyCsgPrecomputedHelpres.NUM_MAPS; ++i)
            {
                string name = null;
                MyCsgPrecomputedHelpres.GetNameForFace(i, ref name);

                name = Path.Combine(path, name + "_material.bin");
                FileInfo fi = new FileInfo(name);
                int length = (int)fi.Length;
                m_file[i] = MemoryMappedFile.CreateFromFile(name, FileMode.Open);
                m_reader[i] = m_file[i].CreateViewAccessor(0, length);
                m_reader[i].Read(0,out m_resolution);
            }
        }

        public override MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos, float lodSize)
        {
            Vector3 localPosition = pos - Shape.Center();
            float lenghtToCenter = localPosition.Length();
            if (lenghtToCenter < 0.01)
            {
                return null;
            }

            float distance = m_planetShape.SampleField(ref pos);
            if (distance <= MyCsgPrecomputedHelpres.FROZEN_OCEAN_LEVEL && MyFakes.ENABLE_PLANET_FROZEN_SEA)
            {
                return MyDefinitionManager.Static.GetVoxelMaterialDefinition("Ice_01");
            }

            float originalDistance = m_planetShape.SignedDistanceUnchecked(ref pos,lodSize,null,null);
        

            if (lenghtToCenter <= m_oreDeposits.MinDepth)
            {
                MyVoxelMaterialDefinition definiton = m_oreDeposits.GetMaterialForPosition(ref pos, lodSize);
                if (definiton != null)
                {
                    return definiton;
                }
            }

            if (originalDistance < -2.0f)
            {
                return MyDefinitionManager.Static.GetVoxelMaterialDefinition("Stone_01");
            } 

            Vector3I samplePos;
            Vector2 positon = Vector2.Zero;
            MyCsgPrecomputedHelpres.CalculateSamplePosition(ref localPosition, out samplePos, ref positon, m_resolution);

            byte materialData;
            m_reader[samplePos.X].Read(sizeof(int) + samplePos.Z * m_resolution + samplePos.Y, out materialData);

            byte material = (byte)(materialData & 127);
            byte spawns = (byte)(materialData & 128);     
            var voxelMaterial = MyDefinitionManager.Static.GetVoxelMaterialDefinition(material);
            if(voxelMaterial != null)
            {
                voxelMaterial.SpawnsFlora = spawns == 0 ? false : true;  
            }
            return voxelMaterial;
        }

        public override bool SpawnsFlora()
        {          
            return true;
        }

        public override void ReleaseMaps()
        {
            for (int i = 0; i < MyCsgPrecomputedHelpres.NUM_MAPS; ++i)
            {
                m_reader[i].Dispose();
                m_file[i].Dispose();
            }
        }
    }
}
