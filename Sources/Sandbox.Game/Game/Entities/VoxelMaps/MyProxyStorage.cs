using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Game.Voxels;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Common.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.VoxelMaps
{
    sealed class MyProxyStorage : IMyStorage
    {
        private MyStorageBase m_trueStorage;

        public bool IsMutable { get; private set; }

        public MyProxyStorage(bool isMutable, MyStorageBase trueStorage)
        {
            m_trueStorage = trueStorage;
            IsMutable = isMutable;
        }

        private void EnsureMutable()
        {
            if (!IsMutable)
            {
                MyStorageBase existingStorage = null;
                string newName;
                do
                {
                    newName = string.Format("{0}-{1}", m_trueStorage.Name, MyVRageUtils.GetRandomInt(int.MaxValue).ToString("########"));
                    MySession.Static.VoxelMaps.TryGetStorage(newName, out existingStorage);
                }
                while(existingStorage != null);

                m_trueStorage = m_trueStorage.DeepCopy(newName);
                IsMutable = true;
            }
        }

        MyStringId IMyStorage.NameId
        {
            get { return m_trueStorage.NameId; }
        }

        string IMyStorage.Name
        {
            get { return m_trueStorage.Name; }
        }

        Vector3I IMyStorage.Size
        {
            get { return m_trueStorage.Size; }
        }

        DictionaryValuesReader<int, IMyDepositCell> IMyStorage.OreDeposits
        {
            get { return m_trueStorage.OreDeposits; }
        }

        void IMyStorage.Close()
        {
            m_trueStorage.Close();
            m_trueStorage = null;
        }

        void IMyStorage.OverwriteAllMaterials(MyVoxelMaterialDefinition material)
        {
            EnsureMutable();
            m_trueStorage.OverwriteAllMaterials(material);
        }

        void IMyStorage.SetSurfaceMaterial(MyVoxelMaterialDefinition material, int cellThickness)
        {
            EnsureMutable();
            m_trueStorage.SetSurfaceMaterial(material, cellThickness);
        }

        void IMyStorage.Save(out byte[] outCompressedData)
        {
            m_trueStorage.Save(out outCompressedData);
        }

        void IMyStorage.MergeVoxelMaterials(MyMwcVoxelFilesEnum voxelFile, Vector3I voxelPosition, MyVoxelMaterialDefinition materialToSet)
        {
            EnsureMutable();
            m_trueStorage.MergeVoxelMaterials(voxelFile, voxelPosition, materialToSet);
        }

        void IMyStorage.GetAllMaterialsPresent(HashSet<MyVoxelMaterialDefinition> outputMaterialSet)
        {
            m_trueStorage.GetAllMaterialsPresent(outputMaterialSet);
        }

        void IMyStorage.RecomputeOreDeposits()
        {
            m_trueStorage.RecomputeOreDeposits();
        }

        void IMyStorage.ReadRange(MyStorageDataCache target, bool readContent, bool readMaterials, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax)
        {
            m_trueStorage.ReadRange(target, readContent, readMaterials, lodIndex, ref lodVoxelRangeMin, ref lodVoxelRangeMax);
        }

        void IMyStorage.WriteRange(MyStorageDataCache source, bool writeContent, bool writeMaterials, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax)
        {
            EnsureMutable();
            m_trueStorage.WriteRange(source, writeContent, writeMaterials, ref voxelRangeMin, ref voxelRangeMax);
        }

        MyVoxelRangeType IMyStorage.GetRangeType(int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax)
        {
            return m_trueStorage.GetRangeType(lodIndex, ref lodVoxelRangeMin, ref lodVoxelRangeMax);
        }

        void IMyStorage.DebugDraw(MyVoxelDebugDrawMode mode, int modeArg)
        {
            m_trueStorage.DebugDraw(mode, modeArg);
        }

    }
}
