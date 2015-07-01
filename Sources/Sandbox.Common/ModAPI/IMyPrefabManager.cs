using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI
{
    [Flags]
    public enum SpawningOptions
    {
        None = 1 << 0,
        RotateFirstCockpitTowardsDirection = 1 << 1,
        SpawnRandomCargo = 1 << 2,
        DisableDampeners = 1 << 3,
        SetNeutralOwner = 1 << 4,
        TurnOffReactors = 1 << 5,
        DisableSave = 1 << 6,
        UseGridOrigin = 1 << 7,
        HostileEncounter = 1 << 8,
        AlmostNew = 1 << 9,
        LightlyDamaged = 1 << 10,
        Damaged = 1 << 11,
        HeavilyDamaged = 1 << 12,        
        TurnOnReactors = 1 << 13,
        AntennaOn = 1 << 14,
        AntennaOff = 1 << 15,
        AntennaMaxed = 1 << 16,                
    }
    
    public interface IMyPrefabManager
    {

        void SpawnPrefab(
           List<IMyCubeGrid> resultList,
           String prefabName,
           Vector3D position,
           Vector3 forward,
           Vector3 up,
           Vector3 initialLinearVelocity = default(Vector3),
           Vector3 initialAngularVelocity = default(Vector3),
           String beaconName = null,
           SpawningOptions spawningOptions = SpawningOptions.None,
           bool updateSync = false);

        bool IsPathClear(Vector3D from, Vector3D to);
        bool IsPathClear(Vector3D from, Vector3D to, double halfSize);
    }
}
