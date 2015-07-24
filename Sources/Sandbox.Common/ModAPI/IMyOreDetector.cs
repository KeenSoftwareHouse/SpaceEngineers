using System;
using VRage.Collections;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyOreDetector :  IMyCubeBlock, IMyFunctionalBlock, Sandbox.ModAPI.Ingame.IMyOreDetector
    {
        /// <summary>
        /// <para>Provides access to all the ores detected by an Ore Detector.</para>
        /// <para>ReadOnlyDictionary contains ore world location as key and materialIndex as value.</para>
        /// <para>Material information can be obtained via Sandbox.Definitions.MyVoxelMaterialDefinition GetVoxelMaterialDefinition(byte materialIndex).</para>
        /// </summary>
        /// <remarks>
        /// <para>There are often many more ores than an Ore Detector would display.</para>
        /// <para>This event triggers every 100 updates.</para>
        /// </remarks>
        event Action<ReadOnlyDictionary<Vector3D, byte>> OnOresUpdated;
    }
}
