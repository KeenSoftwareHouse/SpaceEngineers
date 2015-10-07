using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Components;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace VRage.ModAPI
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

		DrawOutsideViewDistance = 1 << 18,
    }

    [Flags]
    public enum MyEntityUpdateEnum
    {
        NONE = 0,  //no update
        EACH_FRAME = 1,  //each 0.016s, 60 FPS    
        EACH_10TH_FRAME = 2,  //each 0.166s, 6 FPS
        EACH_100TH_FRAME = 4,  //each 1.666s, 0.6 FPS

        /// <summary>
        /// Separate update performed once before any other updates are called.
        /// </summary>
        BEFORE_NEXT_FRAME = 8,
    }
    #endregion

    public interface IMyEntity
    {
        //Components
        MyEntityComponentContainer Components { get; }
        MyPhysicsComponentBase Physics { get; set; }
        MyPositionComponentBase PositionComp { get; set; }
        MyRenderComponentBase Render { get; set; }
        MyEntityComponentBase GameLogic { get; set; }
        MyHierarchyComponentBase Hierarchy { get; set; }
        MySyncComponentBase SyncObject { get; }


        //Entity core
        EntityFlags Flags { get; set; }
        long EntityId { get; set; }
        string Name { get; set; }        
        string GetFriendlyName();
        void Close();
        bool MarkedForClose { get; }
        void Delete();
        bool Closed { get; }
        bool DebugAsyncLoading { get; } // Will be eventually removed
        MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false);
        bool Save { get; set; }
        MyPersistentEntityFlags2 PersistentFlags { get; set; }
        event Action<IMyEntity> OnClose;
        event Action<IMyEntity> OnClosing;
        event Action<IMyEntity> OnMarkForClose;
        void BeforeSave();


        //Updating
        MyEntityUpdateEnum NeedsUpdate { get; set; }

        //Hierarchy
        IMyEntity GetTopMostParent(Type type = null);
        IMyEntity Parent { get; }
        Matrix LocalMatrix { get; set; }
        void SetLocalMatrix(VRageMath.Matrix localMatrix, object source = null);
        void GetChildren(List<IMyEntity> children, Func<IMyEntity, bool> collect = null);



        //Render
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
        bool IsVisible();
        void DebugDraw();
        void DebugDrawInvalidTriangles();
        void EnableColorMaskForSubparts(bool enable);
        void SetColorMaskForSubparts(VRageMath.Vector3 colorMaskHsv);  


        //Scene 
        float GetDistanceBetweenCameraAndBoundingSphere();
        float GetDistanceBetweenCameraAndPosition();
        float GetLargestDistanceBetweenCameraAndBoundingSphere();
        float GetSmallestDistanceBetweenCameraAndBoundingSphere();
        bool InScene { get; set; }
        void OnRemovedFromScene(object source);
        void OnAddedToScene(object source);
        bool InvalidateOnMove { get; }
        MatrixD GetViewMatrix();
        MatrixD GetWorldMatrixNormalizedInv();
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



        //Model 
        Vector3? GetIntersectionWithLineAndBoundingSphere(ref LineD line, float boundingSphereRadiusMultiplier);
        bool GetIntersectionWithSphere(ref BoundingSphereD sphere);
        void GetTrianglesIntersectingSphere(ref BoundingSphereD sphere, Vector3? referenceNormalVector, float? maxAngle, System.Collections.Generic.List<MyTriangle_Vertex_Normals> retTriangles, int maxNeighbourTriangles);
        bool DoOverlapSphereTest(float sphereRadius, Vector3D spherePos);
        bool IsVolumetric { get; }        
        BoundingBox LocalAABB { get; set; }
        BoundingBox LocalAABBHr { get; }
        BoundingSphere LocalVolume { get; set; }
        Vector3 LocalVolumeOffset { get; set; }
        

        //Physics
        event Action<IMyEntity> OnPhysicsChanged;
       
        
        //Game related - remove asap
        Vector3 LocationForHudMarker { get; }
        bool IsCCDForProjectiles { get; }
        void AddToGamePruningStructure();
        void RemoveFromGamePruningStructure();
        void UpdateGamePruningStructure();
        string DisplayName { get; set; }
     

    }
}
