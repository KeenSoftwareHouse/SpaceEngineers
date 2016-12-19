using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sandbox.ModAPI
{
    public class MyEntitiesHelper_ModAPI : IMyEntities
    {
        private List<MyEntity> m_entityList = new List<MyEntity>();

        void IMyEntities.GetEntities(HashSet<IMyEntity> entities, Func<IMyEntity, bool> collect)
        {
            foreach (var entity in MyEntities.GetEntities())
                if (collect == null || collect(entity))
                    entities.Add(entity);
        }

        bool IMyEntities.TryGetEntityById(long id, out IMyEntity entity)
        {
            MyEntity baseEntity;
            var retVal = MyEntities.TryGetEntityById(id, out baseEntity);
            entity = baseEntity;
            return retVal;
        }

        bool IMyEntities.TryGetEntityById(long? id, out IMyEntity entity)
        {
            entity = null;
            bool retVal = false;
            if (id.HasValue)
            {
                MyEntity baseEntity;
                retVal = MyEntities.TryGetEntityById(id.Value, out baseEntity);
                entity = baseEntity;
            }
            return retVal;
        }

        bool IMyEntities.TryGetEntityByName(string name, out IMyEntity entity)
        {
            MyEntity baseEntity;
            var retVal = MyEntities.TryGetEntityByName(name, out baseEntity);
            entity = baseEntity;
            return retVal;
        }

        bool IMyEntities.EntityExists(string name)
        {
            return MyEntities.EntityExists(name);
        }

        void IMyEntities.AddEntity(IMyEntity entity, bool insertIntoScene)
        {
            if (entity is MyEntity)
                MyEntities.Add(entity as MyEntity, insertIntoScene);
        }

        IMyEntity IMyEntities.CreateFromObjectBuilder(MyObjectBuilder_EntityBase objectBuilder)
        {
            return (IMyEntity)MyEntities.CreateFromObjectBuilder(objectBuilder);
        }

        IMyEntity IMyEntities.CreateFromObjectBuilderAndAdd(MyObjectBuilder_EntityBase objectBuilder)
        {
            return (IMyEntity)MyEntities.CreateFromObjectBuilderAndAdd(objectBuilder);
        }


        void IMyEntities.RemoveEntity(IMyEntity entity)
        {
            MyEntities.Remove(entity as MyEntity);
        }

        Action<MyEntity> GetDelegate(Action<IMyEntity> value)
        {
            return (Action<MyEntity>)Delegate.CreateDelegate(typeof(Action<MyEntity>), value.Target, value.Method);
        }

        Action<MyEntity, string, string> GetDelegate(Action<IMyEntity, string, string> value)
        {
            return (Action<MyEntity, string, string>)Delegate.CreateDelegate(typeof(Action<MyEntity, string, string>), value.Target, value.Method);
        }
        
        event Action<IMyEntity> IMyEntities.OnEntityRemove
        {
            add { MyEntities.OnEntityRemove += GetDelegate(value); }
            remove { MyEntities.OnEntityRemove -= GetDelegate(value); }
        }

        event Action<IMyEntity> IMyEntities.OnEntityAdd
        {
            add { MyEntities.OnEntityAdd += GetDelegate(value); }
            remove { MyEntities.OnEntityAdd -= GetDelegate(value); }
        }

        event Action IMyEntities.OnCloseAll
        {
            add { MyEntities.OnCloseAll += value; }
            remove { MyEntities.OnCloseAll -= value; }
        }

        event Action<IMyEntity, string, string> IMyEntities.OnEntityNameSet
        {
            add { MyEntities.OnEntityNameSet += GetDelegate(value); }
            remove { MyEntities.OnEntityNameSet -= GetDelegate(value); }
        }

        bool IMyEntities.IsSpherePenetrating(ref VRageMath.BoundingSphereD bs)
        {
            return MyEntities.IsSpherePenetrating(ref bs);
        }

        VRageMath.Vector3D? IMyEntities.FindFreePlace(VRageMath.Vector3D basePos, float radius, int maxTestCount, int testsPerDistance, float stepSize)
        {
            return MyEntities.FindFreePlace(basePos, radius, maxTestCount, testsPerDistance, stepSize);
        }

        void IMyEntities.GetInflatedPlayerBoundingBox(ref VRageMath.BoundingBox playerBox, float inflation)
        {
            MyEntities.GetInflatedPlayerBoundingBox(ref playerBox, inflation);
        }

        bool IMyEntities.IsInsideVoxel(VRageMath.Vector3 pos, VRageMath.Vector3 hintPosition, out VRageMath.Vector3 lastOutsidePos)
        {
            return MyEntities.IsInsideVoxel(pos, hintPosition, out lastOutsidePos);
        }

        bool IMyEntities.IsWorldLimited()
        {
            return MyEntities.IsWorldLimited();
        }

        float IMyEntities.WorldHalfExtent()
        {
            return MyEntities.WorldHalfExtent();
        }

        float IMyEntities.WorldSafeHalfExtent()
        {
            return MyEntities.WorldSafeHalfExtent();
        }

        bool IMyEntities.IsInsideWorld(VRageMath.Vector3D pos)
        {
            return MyEntities.IsInsideWorld(pos);
        }

        bool IMyEntities.IsRaycastBlocked(VRageMath.Vector3D pos, VRageMath.Vector3D target)
        {
            return MyEntities.IsRaycastBlocked(pos, target);
        }

        List<IMyEntity> IMyEntities.GetEntitiesInAABB(ref VRageMath.BoundingBoxD boundingBox)
        {
            var lst = MyEntities.GetEntitiesInAABB(ref boundingBox);
            var result = new List<IMyEntity>(lst.Count);
            foreach (var entity in lst)
                result.Add(entity);
			lst.Clear();
            return result;
        }

        List<IMyEntity> IMyEntities.GetEntitiesInSphere(ref VRageMath.BoundingSphereD boundingSphere)
        {
            var lst = MyEntities.GetEntitiesInSphere(ref boundingSphere);
            var result = new List<IMyEntity>(lst.Count);
            foreach (var entity in lst)
                result.Add(entity);
            lst.Clear();
            return result;
        }

        List<IMyEntity> IMyEntities.GetTopMostEntitiesInSphere(ref VRageMath.BoundingSphereD boundingSphere)
        {
            var lst = MyEntities.GetTopMostEntitiesInSphere( ref boundingSphere );
            var result = new List<IMyEntity>(lst.Count);
            foreach (var entity in lst)
                result.Add(entity);
            lst.Clear();
            return result;
        }

        List<IMyEntity> IMyEntities.GetElementsInBox(ref VRageMath.BoundingBoxD boundingBox)
        {
            m_entityList.Clear();
            MyEntities.GetElementsInBox(ref boundingBox, m_entityList);
            var result = new List<IMyEntity>(m_entityList.Count);
            foreach (var entity in m_entityList)
                result.Add(entity);
            return result;
        }

        List<IMyEntity> IMyEntities.GetTopMostEntitiesInBox(ref VRageMath.BoundingBoxD boundingBox)
        {
            m_entityList.Clear();
            MyEntities.GetTopMostEntitiesInBox(ref boundingBox, m_entityList);
            var result = new List<IMyEntity>(m_entityList.Count);
            foreach (var entity in m_entityList)
                result.Add(entity);
            return result;
        }

        void IMyEntities.SetEntityName(IMyEntity entity, bool possibleRename)
        {
            if (entity is MyEntity)
                MyEntities.SetEntityName(entity as MyEntity, possibleRename);
        }

        bool IMyEntities.IsNameExists(IMyEntity entity, string name)
        {
            if (entity is MyEntity)
                return MyEntities.IsNameExists(entity as MyEntity, name);
            return false;
        }

        void IMyEntities.RemoveFromClosedEntities(IMyEntity entity)
        {
            if (entity is MyEntity)
                MyEntities.RemoveFromClosedEntities(entity as MyEntity);
        }

        void IMyEntities.RemoveName(IMyEntity entity)
        {
            if (!string.IsNullOrEmpty(entity.Name))
            {
                MyEntities.m_entityNameDictionary.Remove(entity.Name);
            }
        }

        bool IMyEntities.Exist(IMyEntity entity)
        {
            if (entity is MyEntity)
                return MyEntities.Exist(entity as MyEntity);
            return false;
        }

        void IMyEntities.MarkForClose(IMyEntity entity)
        {
            if (entity is MyEntity)
                MyEntities.Close(entity as MyEntity);
        }

        void IMyEntities.RegisterForUpdate(IMyEntity entity)
        {
            var e = entity as MyEntity;
            if (e != null)
                MyEntities.RegisterForUpdate(e);
        }

        void IMyEntities.RegisterForDraw(IMyEntity entity)
        {
            var e = entity as MyEntity;
            if (e != null)
                MyEntities.RegisterForDraw(e);
        }

        void IMyEntities.UnregisterForUpdate(IMyEntity entity, bool immediate)
        {
            var e = entity as MyEntity;
            if (e != null)
                MyEntities.UnregisterForUpdate(e, immediate);
        }

        void IMyEntities.UnregisterForDraw(IMyEntity entity)
        {
            var e = entity as MyEntity;
            if (e != null)
                MyEntities.UnregisterForDraw(e);
        }

        IMyEntity IMyEntities.GetIntersectionWithSphere(ref VRageMath.BoundingSphereD sphere)
        {
            return MyEntities.GetIntersectionWithSphere(ref sphere);
        }

        IMyEntity IMyEntities.GetIntersectionWithSphere(ref VRageMath.BoundingSphereD sphere, IMyEntity ignoreEntity0, IMyEntity ignoreEntity1)
        {
            return MyEntities.GetIntersectionWithSphere(ref sphere, ignoreEntity0 as MyEntity, ignoreEntity1 as MyEntity);
        }

        List<IMyEntity> IMyEntities.GetIntersectionWithSphere(ref VRageMath.BoundingSphereD sphere, IMyEntity ignoreEntity0, IMyEntity ignoreEntity1, bool ignoreVoxelMaps, bool volumetricTest)
        {
            m_entityList.Clear();
            MyEntities.GetIntersectionWithSphere(ref sphere, ignoreEntity0 as MyEntity, ignoreEntity1 as MyEntity, ignoreVoxelMaps, volumetricTest, ref m_entityList);
            var result = new List<IMyEntity>(m_entityList.Count);
            foreach (var entity in m_entityList)
                result.Add(entity);
            return result;
        }

        IMyEntity IMyEntities.GetIntersectionWithSphere(ref VRageMath.BoundingSphereD sphere, IMyEntity ignoreEntity0, IMyEntity ignoreEntity1, bool ignoreVoxelMaps, bool volumetricTest, bool excludeEntitiesWithDisabledPhysics, bool ignoreFloatingObjects, bool ignoreHandWeapons)
        {
            return MyEntities.GetIntersectionWithSphere(ref sphere, ignoreEntity0 as MyEntity, ignoreEntity1 as MyEntity, ignoreVoxelMaps, volumetricTest, excludeEntitiesWithDisabledPhysics, ignoreFloatingObjects, ignoreHandWeapons);
        }

        IMyEntity IMyEntities.GetEntityById(long entityId)
        {
            return MyEntities.EntityExists(entityId) ? MyEntities.GetEntityById(entityId) : null;
        }

        IMyEntity IMyEntities.GetEntityById(long? entityId)
        {
            return entityId.HasValue ? MyEntities.GetEntityById(entityId.Value) : null;
        }

        bool IMyEntities.EntityExists(long entityId)
        {
            return MyEntities.EntityExists(entityId);
        }

        bool IMyEntities.EntityExists(long? entityId)
        {
            return entityId.HasValue && MyEntities.EntityExists(entityId.Value);
        }

        //bool TryGetEntityById<T>(long entityId, out T entity)
        //{
        //    return TryGetEntityById<T>(entityId, out entity);
        //}

        IMyEntity IMyEntities.GetEntityByName(string name)
        {
            return MyEntities.GetEntityByName(name);
        }

        void IMyEntities.SetTypeHidden(Type type, bool hidden)
        {
            MyEntities.SetTypeHidden(type, hidden);
        }

        bool IMyEntities.IsTypeHidden(Type type)
        {
            return MyEntities.IsTypeHidden(type);
        }

        bool IMyEntities.IsVisible(IMyEntity entity)
        {
            return (this as IMyEntities).IsTypeHidden(entity.GetType());
        }

        void IMyEntities.UnhideAllTypes()
        {
            MyEntities.UnhideAllTypes();
        }

        void IMyEntities.RemapObjectBuilderCollection(IEnumerable<MyObjectBuilder_EntityBase> objectBuilders)
        {
            MyEntities.RemapObjectBuilderCollection(objectBuilders);
        }

        void IMyEntities.RemapObjectBuilder(MyObjectBuilder_EntityBase objectBuilder)
        {
            MyEntities.RemapObjectBuilder(objectBuilder);
        }

        IMyEntity IMyEntities.CreateFromObjectBuilderNoinit(MyObjectBuilder_EntityBase objectBuilder)
        {
            return MyEntities.CreateFromObjectBuilderNoinit(objectBuilder);
        }

        void IMyEntities.EnableEntityBoundingBoxDraw(IMyEntity entity, bool enable, VRageMath.Vector4? color, float lineWidth, VRageMath.Vector3? inflateAmount)
        {
            if (entity is MyEntity)
                MyEntities.EnableEntityBoundingBoxDraw(entity as MyEntity, enable, color, lineWidth, inflateAmount);
        }


        IMyEntity IMyEntities.GetEntity(Func<IMyEntity, bool> match)
        {
            foreach (var ent in MyEntities.GetEntities())
                if (match(ent))
                    return ent;
            return null;
        }
    }
}
