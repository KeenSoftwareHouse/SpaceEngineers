using Sandbox.Common;
using Sandbox.Common.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;

namespace Sandbox.ModAPI
{
    #region Enums

    /// <summary>
    /// Entity flags.
    /// </summary>
    [Flags]
    public enum EntityFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 1 << 0,

        /// <summary>
        /// Specifies whether draw this entity or not.
        /// </summary>
        Visible = 1 << 1,

        /// <summary>
        /// Specifies whether save entity when saving sector or not
        /// </summary>
        Save = 1 << 3,

        /// <summary>
        /// Specifies whether entity is "near", near entities are cockpit and weapons, these entities are rendered in special way
        /// </summary>
        Near = 1 << 4,

        /// <summary>
        /// On this entity and its children will be called UpdateBeforeSimulation and UpdateAfterSimulation each frame
        /// </summary>
        NeedsUpdate = 1 << 5,


        NeedsResolveCastShadow = 1 << 6,


        FastCastShadowResolve = 1 << 7,


        SkipIfTooSmall = 1 << 8,

        NeedsUpdate10 = 1 << 9,  //entity updated each 10th frame
        NeedsUpdate100 = 1 << 10,//entity updated each 100th frame

        /// <summary>
        /// Draw method of this entity will be called when suitable
        /// </summary>
        NeedsDraw = 1 << 11,

        /// <summary>
        /// If object is moved, invalidate its renderobjects (update render)
        /// </summary>
        InvalidateOnMove = 1 << 12,

        /// <summary>
        /// Synchronize object during multiplayer
        /// </summary>
        Sync = 1 << 13,

        /// <summary>
        /// Draw method of this entity will be called when suitable and only from parent
        /// </summary>
        NeedsDrawFromParent = 1 << 14,

        /// <summary>
        /// Draw LOD shadow as box
        /// </summary>
        ShadowBoxLod = 1 << 15,

        /// <summary>
        /// Render the entity using dithering to simulate transparency
        /// </summary>
        Transparent = 1 << 16,

        /// <summary>
        /// Entity updated once before first frame.
        /// </summary>
        NeedsUpdateBeforeNextFrame = 1 << 17,
    }

    #endregion

    public interface IMyEntity
    {
       
        //void AddChild(MyEntity child, bool preserveWorldPos = false, bool insertIntoSceneIfNeeded = true);
        //void AddChildWithMatrix(MyEntity child, ref VRageMath.Matrix childLocalMatrix, bool insertIntoSceneIfNeeded = true);

        EntityFlags Flags { get; set; }
        long EntityId { get; set; }
        string Name { get; set; }
        string DisplayName { get; set; }
        string GetFriendlyName();
        void Close();
        bool MarkedForClose { get; }
        void Delete();
        bool Closed { get; }

        MyEntityUpdateEnum NeedsUpdate { get; set; }

        IMyEntity GetTopMostParent(Type type = null);
        IMyEntity Parent { get; }

        bool NearFlag { get; set; }
        bool CastShadows { get; set; }
        bool FastCastShadowResolve { get; set; }
        bool NeedsResolveCastShadow { get; set; }
        VRageMath.Vector3 GetDiffuseColor();
        float MaxGlassDistSq { get; }
        bool NeedsDraw { get; set; }
        bool NeedsDrawFromParent { get; set; }
        bool Transparent { get; set; }
        bool ShadowBoxLod { get; set; }
        bool SkipIfTooSmall { get; set; }
        bool Visible { get; set; }

        float GetDistanceBetweenCameraAndBoundingSphere();
        float GetDistanceBetweenCameraAndPosition();
        float GetLargestDistanceBetweenCameraAndBoundingSphere();
        float GetSmallestDistanceBetweenCameraAndBoundingSphere();

        VRageMath.Vector3? GetIntersectionWithLineAndBoundingSphere(ref VRageMath.LineD line, float boundingSphereRadiusMultiplier);
        bool GetIntersectionWithSphere(ref VRageMath.BoundingSphereD sphere);
        void GetTrianglesIntersectingSphere(ref VRageMath.BoundingSphereD sphere, VRageMath.Vector3? referenceNormalVector, float? maxAngle, System.Collections.Generic.List<MyTriangle_Vertex_Normals> retTriangles, int maxNeighbourTriangles);
        bool DoOverlapSphereTest(float sphereRadius, VRageMath.Vector3D spherePos);

        Sandbox.Common.ObjectBuilders.MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false);
        bool Save { get; set; }
        Sandbox.Common.ObjectBuilders.MyPersistentEntityFlags2 PersistentFlags { get; set; }

        bool InScene { get; set; }
        bool InvalidateOnMove { get; }
        bool IsCCDForProjectiles { get; }
        bool IsVisible();
        bool IsVolumetric { get; }

        VRageMath.MatrixD GetViewMatrix();
        VRageMath.MatrixD GetWorldMatrixNormalizedInv();
        VRageMath.BoundingBox LocalAABB { get; set; }
        VRageMath.BoundingBox LocalAABBHr { get; }
        VRageMath.Matrix LocalMatrix { get; set; }
        VRageMath.BoundingSphere LocalVolume { get; set; }
        VRageMath.Vector3 LocalVolumeOffset { get; set; }
        VRageMath.Vector3 LocationForHudMarker { get; }
        void SetLocalMatrix(VRageMath.Matrix localMatrix, object source = null);
        void SetWorldMatrix(VRageMath.MatrixD worldMatrix, object source = null);
        VRageMath.BoundingBoxD WorldAABB { get; }
        VRageMath.BoundingBoxD WorldAABBHr { get; }
        VRageMath.MatrixD WorldMatrix { get; set; }
        VRageMath.MatrixD WorldMatrixInvScaled { get; }
        VRageMath.MatrixD WorldMatrixNormalizedInv { get; }
        VRageMath.BoundingSphereD WorldVolume { get; }
        VRageMath.BoundingSphereD WorldVolumeHr { get; }

        VRageMath.Vector3D GetPosition();
        void SetPosition(VRageMath.Vector3D pos);

        event Action<IMyEntity> OnClose;
        event Action<IMyEntity> OnClosing;
        event Action<IMyEntity> OnMarkForClose;
        event Action<IMyEntity> OnPhysicsChanged;

        void GetChildren(List<IMyEntity> children, Func<IMyEntity, bool> collect = null);

        //Components wip 
        MyComponentContainer Components { get; }
        MyPhysicsComponentBase Physics { get; set; }
        MyPositionComponentBase PositionComp { get; set; }
        MyRenderComponentBase Render { get; set; }
        MyGameLogicComponent GameLogic { get; set; }
        MyHierarchyComponentBase Hierarchy { get; set; }
        MySyncComponentBase SyncObject { get; }
        void OnRemovedFromScene(object source);
        void OnAddedToScene(object source);
        void BeforeSave();
        void AddToGamePruningStructure();
        void RemoveFromGamePruningStructure();
        void UpdateGamePruningStructure();
        void DebugDraw();
        void DebugDrawInvalidTriangles();
        void EnableColorMaskForSubparts(bool enable);
        void SetColorMaskForSubparts(VRageMath.Vector3 colorMaskHsv);

        //missing dependencies
        //MyEntityUpdateEnum NeedsUpdate { get; set; }
        //System.Collections.Generic.List<Sandbox.Game.Gui.MyHudEntityParams> GetHudParams(bool allowBlink);
        //bool GetIntersectionWithLine(ref VRageMath.Line line, out VRageMath.Vector3? v, bool useCollisionModel = true, Sandbox.Engine.Physics.IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES);

        //Not needed for scripters?
        //void Init(Sandbox.Common.ObjectBuilders.MyObjectBuilder_EntityBase objectBuilder);
        //void Init(System.Text.StringBuilder displayName, string model, MyEntity parentObject, float? scale, string modelCollision = null);
        //void InitBoxPhysics(Sandbox.Game.Utils.MyMaterialType materialType, Sandbox.Engine.Models.MyModel model, float mass, float angularDamping, ushort collisionLayer, Sandbox.Engine.Physics.RigidBodyFlag rbFlag);
        //void InitBoxPhysics(Sandbox.Game.Utils.MyMaterialType materialType, VRageMath.Vector3 center, VRageMath.Vector3 size, float mass, float linearDamping, float angularDamping, ushort collisionLayer, Sandbox.Engine.Physics.RigidBodyFlag rbFlag);
        //void InitCapsulePhysics(Sandbox.Game.Utils.MyMaterialType materialType, VRageMath.Vector3 vertexA, VRageMath.Vector3 vertexB, float radius, float mass, float linearDamping, float angularDamping, ushort collisionLayer, Sandbox.Engine.Physics.RigidBodyFlag rbFlag);
        //void InitCharacterPhysics(Sandbox.Game.Utils.MyMaterialType materialType, VRageMath.Vector3 center, float characterWidth, float characterHeight, float crouchHeight, float ladderHeight, float headSize, float linearDamping, float angularDamping, ushort collisionLayer, Sandbox.Engine.Physics.RigidBodyFlag rbFlag, float mass);
        //void InitDrawTechniques();
        //void InitSpherePhysics(Sandbox.Game.Utils.MyMaterialType materialType, Sandbox.Engine.Models.MyModel model, float mass, float linearDamping, float angularDamping, ushort collisionLayer, Sandbox.Engine.Physics.RigidBodyFlag rbFlag);
        //void InitSpherePhysics(Sandbox.Game.Utils.MyMaterialType materialType, VRageMath.Vector3 sphereCenter, float sphereRadius, float mass, float linearDamping, float angularDamping, ushort collisionLayer, Sandbox.Engine.Physics.RigidBodyFlag rbFlag);
        //System.Collections.Generic.Dictionary<string, MyEntitySubpart> Subparts { get; }
        //Sandbox.Game.Multiplayer.MySyncEntity SyncObject { get; }
        //void RemoveChild(MyEntity child, bool preserveWorldPos = false);
        //void OnMemberChanged(System.Reflection.MemberInfo memberInfo);
        //void UpdateAABBHr();
        //void UpdateAfterSimulation();
        //void UpdateAfterSimulation10();
        //void UpdateAfterSimulation100();
        //void UpdateBeforeSimulation();
        //void UpdateBeforeSimulation10();
        //void UpdateBeforeSimulation100();
        //void UpdateOnceBeforeFrame();
        //void UpdatingStopped();
        //Sandbox.Engine.Models.MyModel Model { get; }
        //Sandbox.Engine.Models.MyModel ModelCollision { get; }
        //void OnWorldPositionChanged(object source);
        //void UpdateWorldMatrix(ref VRageMath.Matrix parentWorldMatrix, object source = null);
        //void SetRenderObjectID(int index, uint ID);
        //void ReleaseRenderObjectID(int index);
        //uint[] RenderObjectIDs { get; }
        //void ResizeRenderObjectArray(int newSize);
        //void PrepareForDraw();
        //void Link();
        //bool IsRenderObjectAssigned(int index);
        //void CreateSync();
        //bool DebugDraw();
        //void DebugDrawDeactivated();
        //void DebugDrawInvalidTriangles();
        //void DebugDrawPhysics();
        //MyEntity.EntityFlags Flags { get; set; }
        //void GetAllChildren(System.Collections.Generic.List<MyEntity> collectedResources);
        //MyEntity GetBaseEntity();
        //void GetChildrenRecursive(System.Collections.Generic.HashSet<MyEntity> result);
        //void Draw();
        //int GetRenderObjectID();
        //void RaisePhysicsChanged();
        //void RefreshModels(string model, string modelCollision);
        //bool SyncFlag { get; set; }
    }
}
