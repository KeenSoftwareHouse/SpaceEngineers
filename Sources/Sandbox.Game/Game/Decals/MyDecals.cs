using System.Collections.Generic;
using VRageMath;
using Sandbox;

using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;

using VRage.Utils;

using Sandbox.Game.Lights;
using System;
using VRage.Import;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.Render;
using Sandbox.Common;
using System.Diagnostics;
using VRage;

//  Decals manager. It holds lists of decal triangles, draws them, removes decals after explosion, etc.
//  I can't use texture atlas for holding all decal textures, because I need clamping, and if using atlas, 
//  texture sampler will get neighbour textures too.
//
//  We have two decal buffers. One for model instances, the other for voxels. Each one manages separate 
//  triangleVertexes buffers. One triangleVertexes buffer for one model/texture or voxel render cell and texture.

namespace Sandbox.Game.Decals
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class MyDecals : MySessionComponentBase
    {
        public MyDecals()
        {
            //  Reason is that if count of neighbour triangles is more then decal triangles buffer, we won't be able to add any triangleVertexes to the buffer.
            Debug.Assert(MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER > MyDecalsConstants.TEXTURE_LARGE_MAX_NEIGHBOUR_TRIANGLES);
            Debug.Assert(MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER_LARGE <= MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER);
            Debug.Assert(MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER_SMALL <= MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER);

            //  Reason is that if count of neighbour triangles is more then decal triangles buffer, we won't be able to add any triangleVertexes to the buffer.
            Debug.Assert(MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER > MyDecalsConstants.TEXTURE_SMALL_MAX_NEIGHBOUR_TRIANGLES);

            //  Reason is that if count of neighbour triangles is more then this fade limit, we won't be able to add decals that lay on more triangles, because buffer will be never released to us.
            Debug.Assert(MyDecalsConstants.TEXTURE_LARGE_MAX_NEIGHBOUR_TRIANGLES < (MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER * MyDecalsConstants.TEXTURE_LARGE_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT));

            //  Reason is that if count of neighbour triangles is more then this fade limit, we won't be able to add decals that lay on more triangles, because buffer will be never released to us.
            Debug.Assert(MyDecalsConstants.TEXTURE_SMALL_MAX_NEIGHBOUR_TRIANGLES < (MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER * MyDecalsConstants.TEXTURE_SMALL_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT));

            //  Large must be bigger than small
            Debug.Assert(MyDecalsConstants.TEXTURE_LARGE_MAX_NEIGHBOUR_TRIANGLES > MyDecalsConstants.TEXTURE_SMALL_MAX_NEIGHBOUR_TRIANGLES);
        }

        static float GetAlphaByAngleDiff(ref Vector3 referenceNormal, ref Vector3 vertexNormal)
        {
            //return (float)Math.Pow(Vector3.Dot(referenceNormal, vertexNormal), 5);
            float dot = Vector3.Dot(referenceNormal, vertexNormal);
            if (dot < MyMathConstants.EPSILON)
                return 0;
            float result = (float)Math.Pow(dot, 1f);
            return MathHelper.Clamp(result, 0, 1);
        }

        static Vector3 CalculateDominantNormal(List<MyTriangle_Vertex_Normals> triangleVertexNormals)
        {
            Vector3 normalSum = Vector3.Zero;
            for (int i = 0; i < triangleVertexNormals.Count; i++)
            {
                normalSum +=
                    triangleVertexNormals[i].Normals.Normal0 +
                    triangleVertexNormals[i].Normals.Normal1 +
                    triangleVertexNormals[i].Normals.Normal2;
            }

            return MyUtils.Normalize(normalSum);
        }


        //  Blends-out triangles affected by explosion (radius + some safe delta). Triangles there have zero alpha are flaged to not-draw at all.
        public static void HideTrianglesAfterExplosion(MyVoxelBase voxelMap, ref BoundingSphereD explosionSphere)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyDecals::HideTrianglesAfterExplosion");
            //MyMwcVector3Int renderCellCoord = voxelMap.GetVoxelRenderCellCoordinateFromMeters(ref explosionSphere.Center);
            //m_decalsForVoxels.HideTrianglesAfterExplosion(voxelMap.VoxelMapId, ref renderCellCoord, ref explosionSphere);

            foreach (uint id in voxelMap.Render.RenderObjectIDs)
            {
                VRageRender.MyRenderProxy.HideDecals(id, explosionSphere.Center, (float)explosionSphere.Radius);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        /// <summary>
        /// Removes decals from the specified entity (NOT voxel map).
        /// E.g. when the entity is destroyed (destructible prefab).
        /// </summary>
        /// <param name="renderObject">The entity from which we want to remove decals. NOT MyVoxelMap!</param>
        public static void RemoveModelDecals(MyEntity renderObject)
        {
            if (renderObject.Render.RenderObjectIDs.Length > 0)
                VRageRender.MyRenderProxy.HideDecals(renderObject.Render.RenderObjectIDs[0], Vector3.Zero, 0);

            foreach (var child in renderObject.Hierarchy.Children)
            {
                RemoveModelDecals(child.Container.Entity as MyEntity);
            }
        }

        public static float GetMaxDistanceForDrawingDecals()
        {
            float zoomLevel = MySector.MainCamera.Zoom.GetZoomLevel();
            zoomLevel = System.Math.Max(zoomLevel, MyConstants.FIELD_OF_VIEW_MIN / MySector.MainCamera.FieldOfView);
            return zoomLevel > 0 ? MyDecalsConstants.MAX_DISTANCE_FOR_DRAWING_DECALS / zoomLevel : MyDecalsConstants.MAX_DISTANCE_FOR_DRAWING_DECALS;
        }
    }
}
