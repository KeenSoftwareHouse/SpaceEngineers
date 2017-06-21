using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace VRage.Game.ModAPI.Ingame
{
    /// <summary>
    /// Ingame (Programmable Block) interface for all entities.
    /// </summary>
    // This must only contain SAFE members; No references, No modifications
    public interface IMyEntity
    {
        MyEntityComponentContainer Components { get; }  // Needed for Power/resources; still subject to whitelist

        // Entity Core
        long EntityId { get; }
        
        // Scene
        VRageMath.BoundingBoxD WorldAABB { get; }
        VRageMath.BoundingBoxD WorldAABBHr { get; }
        VRageMath.MatrixD WorldMatrix { get; }
        VRageMath.BoundingSphereD WorldVolume { get; }
        VRageMath.BoundingSphereD WorldVolumeHr { get; }
        VRageMath.Vector3D GetPosition();
    }
}
