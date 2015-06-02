using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyEntities
    {
        bool TryGetEntityById(long id, out IMyEntity entity);
        bool TryGetEntityByName(string name, out IMyEntity entity);
        bool EntityExists(string name);
        void AddEntity(IMyEntity entity, bool insertIntoScene = true);
        IMyEntity CreateFromObjectBuilder(MyObjectBuilder_EntityBase objectBuilder);
        IMyEntity CreateFromObjectBuilderAndAdd(MyObjectBuilder_EntityBase objectBuilder);
        void RemoveEntity(IMyEntity entity);

        event Action<IMyEntity> OnEntityRemove;
        event Action<IMyEntity> OnEntityAdd;
        event Action OnCloseAll;
        event Action<IMyEntity, string, string> OnEntityNameSet;

        bool IsSpherePenetrating(ref BoundingSphereD bs);
        Vector3D? FindFreePlace(Vector3D basePos, float radius, int maxTestCount = 20, int testsPerDistance = 5, float stepSize = 1);
        void GetInflatedPlayerBoundingBox(ref BoundingBox playerBox, float inflation);
        bool IsInsideVoxel(Vector3 pos, Vector3 hintPosition, out Vector3 lastOutsidePos);
        bool IsWorldLimited();
        float WorldHalfExtent();
        float WorldSafeHalfExtent();
        bool IsInsideWorld(Vector3D pos);
        bool IsRaycastBlocked(Vector3D pos, Vector3D target);
        void SetEntityName(IMyEntity IMyEntity, bool possibleRename = true);
        bool IsNameExists(IMyEntity entity, string name);
        void RemoveFromClosedEntities(IMyEntity entity);
        void RemoveName(IMyEntity entity);
        bool Exist(IMyEntity entity);
        void MarkForClose(IMyEntity entity);
        void RegisterForUpdate(IMyEntity entity);
        void RegisterForDraw(IMyEntity entity);
        void UnregisterForUpdate(IMyEntity entity, bool immediate = false);
        void UnregisterForDraw(IMyEntity entity);
        IMyEntity GetIntersectionWithSphere(ref BoundingSphereD sphere);
        IMyEntity GetIntersectionWithSphere(ref BoundingSphereD sphere, IMyEntity ignoreEntity0, IMyEntity ignoreEntity1);
        IMyEntity GetIntersectionWithSphere(ref BoundingSphereD sphere, IMyEntity ignoreEntity0, IMyEntity ignoreEntity1, bool ignoreVoxelMaps, bool volumetricTest, bool excludeEntitiesWithDisabledPhysics = false, bool ignoreFloatingObjects = true, bool ignoreHandWeapons = true);
        IMyEntity GetEntityById(long entityId);
        bool EntityExists(long entityId);
        IMyEntity GetEntityByName(string name);
        void SetTypeHidden(Type type, bool hidden);
        bool IsTypeHidden(Type type);
        bool IsVisible(IMyEntity entity);
        void UnhideAllTypes();
        void RemapObjectBuilderCollection(IEnumerable<MyObjectBuilder_EntityBase> objectBuilders);
        void RemapObjectBuilder(MyObjectBuilder_EntityBase objectBuilder);
        IMyEntity CreateFromObjectBuilderNoinit(MyObjectBuilder_EntityBase objectBuilder);
        
        void EnableEntityBoundingBoxDraw(IMyEntity entity, bool enable, Vector4? color = null, float lineWidth = 0.01f, Vector3? inflateAmount = null);

        IMyEntity GetEntity(Func<IMyEntity, bool> match);
        void GetEntities(HashSet<IMyEntity> entities, Func<IMyEntity, bool> collect = null);
        List<IMyEntity> GetIntersectionWithSphere(ref VRageMath.BoundingSphereD sphere, IMyEntity ignoreEntity0, IMyEntity ignoreEntity1, bool ignoreVoxelMaps, bool volumetricTest);
        List<IMyEntity> GetEntitiesInAABB(ref BoundingBoxD boundingBox);
        List<IMyEntity> GetEntitiesInSphere(ref BoundingSphereD boundingSphere);
        List<IMyEntity> GetElementsInBox(ref BoundingBoxD boundingBox);

        //Missing dependencies
        //void OverlapAllLineSegment(ref Line line, IEnumerable<MyLineSegmentOverlapResult<IMyEntity>> resultList);
        //bool IsShapePenetrating(HkShape shape, ref Vector3 position, ref Quaternion rotation, int filter = MyPhysics.DefaultCollisionLayer);

        //Not for scripters?
        //bool SafeAreasHidden, SafeAreasSelectable;
        //bool DetectorsHidden, DetectorsSelectable;
        //bool ParticleEffectsHidden, ParticleEffectsSelectable;
        //bool ShowDebugDrawStatistics = false;
        //void DebugDrawStatistics();
        //IMyEntity GetEntityFromRenderObjectID(uint renderObjectID);
        //void DebugDraw();
        //IMyEntity CreateFromObjectBuilderAndAdd(MyObjectBuilder_EntityBase objectBuilder);
        //bool MemoryLimitReached;
        //bool MemoryLimitReachedReport;
        //bool MemoryLimitAddFailure;
        //void MemoryLimitAddFailureReset();
        //bool Load(List<MyObjectBuilder_EntityBase> objectBuilders);
        //public Vector4 Color;
        //public float LineWidth;
        //public Vector3 InflateAmount;
        //bool UpdateInProgress = false;
        //bool CloseAllowed = false;
        //void UpdateBeforeSimulation();
        //void UpdateAfterSimulation();
        //void UpdatingStopped();
        //void Draw();
        //bool TryGetEntityById<T>(long entityId, out T entity); //they dont have our types
        //void RaiseEntityRemove(IMyEntity entity);
        //FastResourceLock EntityCloseLock = new FastResourceLock();
        //FastResourceLock EntityMarkForCloseLock = new FastResourceLock();
        //FastResourceLock UnloadDataLock = new FastResourceLock();
        //void CloseAll();
        //void Link();
        //bool IsLoaded;
        //void LoadData();
        //void UnloadData();
        //void AddRenderObjectToMap(uint id, IMyEntity entity);
        //void RemoveRenderObjectFromMap(uint id);
    }
}
