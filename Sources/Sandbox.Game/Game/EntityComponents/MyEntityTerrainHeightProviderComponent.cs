using System.Collections.Generic;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using VRageRender.Animations;
using VRage.Game.Components;
using VRage.ModAPI;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.EntityComponents
{
    /// <summary>
    /// Entity component providing terrain height when asked.
    /// </summary>
    internal class MyEntityTerrainHeightProviderComponent : MyEntityComponentBase, IMyTerrainHeightProvider
    {
        private readonly List<MyPhysics.HitInfo> m_raycastHits = new List<MyPhysics.HitInfo>(32);

        public override string ComponentTypeDebugString
        {
            get { return "SkinnedEntityTerrainHeightProvider"; }
        }

        bool IMyTerrainHeightProvider.GetTerrainHeight(Vector3 bonePosition, out float terrainHeight, out Vector3 terrainNormal)
        {
            MatrixD worldMatrix = Entity.PositionComp.WorldMatrix;
            Vector3D downDirection = worldMatrix.Down;
            Vector3D bonePositionWorld = Vector3D.Transform(new Vector3(bonePosition.X, Entity.PositionComp.LocalAABB.Min.Y, bonePosition.Z), ref worldMatrix);

            m_raycastHits.Clear();
            MyPhysics.CastRay(bonePositionWorld - downDirection, bonePositionWorld + downDirection, m_raycastHits, MyPhysics.CollisionLayers.CharacterCollisionLayer);
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                MyRenderProxy.DebugDrawLine3D(bonePositionWorld - downDirection,
                    bonePositionWorld + downDirection, Color.Red, Color.Yellow, false);
            }
            foreach (var hit in m_raycastHits)
            {
                IMyEntity hitEntity = hit.HkHitInfo.GetHitEntity();
                if (hitEntity != Entity && !(hitEntity is Entities.Character.MyCharacter))
                {
                    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                        MyRenderProxy.DebugDrawSphere(hit.Position, 0.05f, Color.Red, 1, false);
            
                    Vector3D localPosition = Vector3D.Transform(hit.Position, Entity.PositionComp.WorldMatrixInvScaled);
                    terrainHeight = (float)localPosition.Y - Entity.PositionComp.LocalAABB.Min.Y;
                    float convexRadius = hit.HkHitInfo.GetConvexRadius();
                    const float maxConvexRadius = 0.06f; // todo: this is a hard limit, but can we somehow determine why the convexradius is so big sometimes?
                    terrainHeight -= convexRadius < maxConvexRadius ? convexRadius : maxConvexRadius;
                    terrainNormal = Vector3D.Transform(hit.HkHitInfo.Normal, Entity.WorldMatrixNormalizedInv.GetOrientation());
                    return true;
                }
            }
            
            terrainHeight = Entity.PositionComp.LocalAABB.Min.Y;
            terrainNormal = Vector3.Zero;
            return false;
        }

        float IMyTerrainHeightProvider.GetReferenceTerrainHeight()
        {
            return Entity.PositionComp.LocalAABB.Min.Y;
        }
    }
}
