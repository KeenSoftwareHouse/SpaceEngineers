using Havok;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Engine.Physics
{
    static class MyPhysicsDebugDraw
    {
        public static bool DebugDrawFlattenHierarchy = false;
        public static bool HkGridShapeCellDebugDraw = false;

        public static HkGeometry DebugGeometry;

        static Color[] boxColors = MyUtils.GenerateBoxColors();
        static List<HkShape> m_tmpShapeList = new List<HkShape>();

        static Color GetShapeColor(HkShapeType shapeType, ref int shapeIndex, bool isPhantom)
        {
            if (isPhantom)
                return Color.LightGreen;

            switch (shapeType)
            {
                case HkShapeType.Sphere:
                    return Color.White;
                case HkShapeType.Capsule:
                    return Color.Yellow;
                case HkShapeType.Cylinder:
                    return Color.Orange;
                case HkShapeType.ConvexVertices:
                    return Color.Red;

                default:
                case HkShapeType.Box:
                    return boxColors[(++shapeIndex) % (boxColors.Length - 1)];
            }
        }

        public static void DrawCollisionShape(HkShape shape, MatrixD worldMatrix, float alpha, ref int shapeIndex, string customText = null, bool isPhantom = false)
        {
            var color = GetShapeColor(shape.ShapeType, ref shapeIndex, isPhantom);
            if (isPhantom) alpha *= alpha;
            color.A = (byte)(alpha * 255);

            bool shaded = true;

            float expandSize = 0.02f;
            float expandRatio = 1.035f;

            bool drawCustomText = false;

            switch (shape.ShapeType)
            {
                case HkShapeType.Sphere:
                    {
                        var sphere = (HkSphereShape)shape;
                        float radius = sphere.Radius;

                        VRageRender.MyRenderProxy.DebugDrawSphere(worldMatrix.Translation, radius, color, alpha, true, shaded);

                        if (isPhantom)
                        {
                            VRageRender.MyRenderProxy.DebugDrawSphere(worldMatrix.Translation, radius, color, 1.0f, true, false);
                            VRageRender.MyRenderProxy.DebugDrawSphere(worldMatrix.Translation, radius, color, 1.0f, true, false, false);
                        }

                        drawCustomText = true;
                        break;
                    }

                case HkShapeType.Capsule:
                    {
                        // Sphere and OBB to show cylinder space
                        var capsule = (HkCapsuleShape)shape;
                        Vector3D vertexA = Vector3.Transform(capsule.VertexA, worldMatrix);
                        Vector3D vertexB = Vector3.Transform(capsule.VertexB, worldMatrix);
                        VRageRender.MyRenderProxy.DebugDrawCapsule(vertexA, vertexB, capsule.Radius, color, true, shaded);
                        drawCustomText = true;
                        break;
                    }

                case HkShapeType.Cylinder:
                    {
                        // Sphere and OBB to show cylinder space
                        var cylinder = (HkCylinderShape)shape;
                        VRageRender.MyRenderProxy.DebugDrawCylinder(worldMatrix, cylinder.VertexA, cylinder.VertexB, cylinder.Radius, color, alpha, true, shaded);
                        drawCustomText = true;
                        break;
                    }


                case HkShapeType.Box:
                    {
                        var box = (HkBoxShape)shape;

                        VRageRender.MyRenderProxy.DebugDrawOBB(MatrixD.CreateScale(box.HalfExtents * 2 + new Vector3(expandSize)) * worldMatrix, color, alpha, true, shaded);
                        if (isPhantom)
                        {
                            VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(box.HalfExtents * 2 + new Vector3(expandSize)) * worldMatrix, color, 1.0f, true, false);
                            VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(box.HalfExtents * 2 + new Vector3(expandSize)) * worldMatrix, color, 1.0f, true, false, false);
                        }
                        drawCustomText = true;
                        break;
                    }

                case HkShapeType.ConvexVertices:
                    {
                        var convexShape = (HkConvexVerticesShape)shape;
                        Vector3 center;
                        convexShape.GetGeometry(DebugGeometry, out center);
                        Vector3D transformedCenter = Vector3D.Transform(center, worldMatrix.GetOrientation());

                        var matrix = worldMatrix;
                        matrix = MatrixD.CreateScale(expandRatio) * matrix;
                        matrix.Translation -= transformedCenter * (expandRatio - 1);

                        //matrix.Translation += transformedCenter;
                        DrawGeometry(DebugGeometry, matrix, color, true, true);

                        drawCustomText = true;
                        break;
                    }

                case HkShapeType.ConvexTranslate:
                    {
                        var translateShape = (HkConvexTranslateShape)shape;
                        DrawCollisionShape((HkShape)translateShape.ChildShape, Matrix.CreateTranslation(translateShape.Translation) * worldMatrix, alpha, ref shapeIndex, customText);
                        break;
                    }

                case HkShapeType.ConvexTransform:
                    {
                        var transformShape = (HkConvexTransformShape)shape;
                        DrawCollisionShape(transformShape.ChildShape, transformShape.Transform * worldMatrix, alpha, ref shapeIndex, customText);
                        break;
                    }

                case HkShapeType.Mopp:
                    {
                        var compoundShape = (HkMoppBvTreeShape)shape;
                        DrawCollisionShape(compoundShape.ShapeCollection, worldMatrix, alpha, ref shapeIndex, customText);
                        break;
                    }

                case HkShapeType.List:
                    {
                        var listShape = (HkListShape)shape;
                        var iterator = listShape.GetIterator();
                        while (iterator.IsValid)
                        {
                            //string text = (customText ?? string.Empty) + "[" + iterator.CurrentShapeKey + "]";
                            DrawCollisionShape(iterator.CurrentValue, worldMatrix, alpha, ref shapeIndex, customText);
                            iterator.Next();
                        }
                        break;
                    }

                case HkShapeType.StaticCompound:
                    {
                        var compoundShape = (HkStaticCompoundShape)shape;

                        if (DebugDrawFlattenHierarchy)
                        {
                            var it = compoundShape.GetIterator();
                            while (it.IsValid)
                            {
                                if (compoundShape.IsShapeKeyEnabled(it.CurrentShapeKey))
                                {
                                    string text = (customText ?? string.Empty) + "-" + it.CurrentShapeKey + "-";
                                    DrawCollisionShape(it.CurrentValue, worldMatrix, alpha, ref shapeIndex, text);
                                }
                                it.Next();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < compoundShape.InstanceCount; i++)
                            {
                                bool enabled = compoundShape.IsInstanceEnabled(i);
                                string text;
                                if (enabled)
                                    text = (customText ?? string.Empty) + "<" + i + ">";
                                else
                                    text = (customText ?? string.Empty) + "(" + i + ")";

                                if (enabled)
                                    DrawCollisionShape(compoundShape.GetInstance(i), compoundShape.GetInstanceTransform(i) * worldMatrix, alpha, ref shapeIndex, text);
                            }
                        }

                        break;
                    }

                case HkShapeType.Triangle:
                    {
                        HkTriangleShape tri = (HkTriangleShape)shape;
                        VRageRender.MyRenderProxy.DebugDrawTriangle(tri.Pt0, tri.Pt1, tri.Pt2, Color.Green, false, false);
                        break;
                    }

                case HkShapeType.BvTree:
                    {
                        var gridShape = (HkGridShape)shape;
                        if (HkGridShapeCellDebugDraw && !gridShape.Base.IsZero)
                        {
                            Vector3S min, max;
                            var cellSize = gridShape.CellSize;

                            int count = gridShape.GetShapeInfoCount();
                            for (int i = 0; i < count; i++)
                            {
                                try
                                {
                                    gridShape.GetShapeInfo(i, out min, out max, m_tmpShapeList);
                                    Vector3 size = max * cellSize - min * cellSize;
                                    Vector3 center = (max * cellSize + min * cellSize) / 2.0f;
                                    size += Vector3.One * cellSize;
                                    var clr = color;
                                    if (min == max)
                                    {
                                        clr = new Color(1.0f, 0.2f, 0.1f);
                                    }
                                    VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(size + new Vector3(expandSize)) * Matrix.CreateTranslation(center) * worldMatrix, clr, alpha, true, shaded);
                                }
                                finally
                                {
                                    m_tmpShapeList.Clear();
                                }
                            }
                        }
                        else
                        {
                            var msg = MyRenderProxy.PrepareDebugDrawTriangles();

                            try
                            {
                                using (HkShapeBuffer buf = new HkShapeBuffer())
                                {
                                    var treeShape = (HkBvTreeShape)shape;
                                    for (var i = treeShape.GetIterator(buf); i.IsValid; i.Next())
                                    {
                                        var child = i.CurrentValue;
                                        if (child.ShapeType == HkShapeType.Triangle)
                                        {
                                            var tri = (HkTriangleShape)child;
                                            msg.AddTriangle(tri.Pt0, tri.Pt1, tri.Pt2);
                                        }
                                        else
                                        {
                                            DrawCollisionShape(child, worldMatrix, alpha, ref shapeIndex);
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                MyRenderProxy.DebugDrawTriangles(msg, worldMatrix, color, false, false);
                            }
                        }
                        break;
                    }

                case HkShapeType.BvCompressedMesh:
                    {
                        if (MyDebugDrawSettings.DEBUG_DRAW_TRIANGLE_PHYSICS)
                        {
                            var meshShape = (HkBvCompressedMeshShape)shape;
                            meshShape.GetGeometry(DebugGeometry);
                            DrawGeometry(DebugGeometry, worldMatrix, Color.Green, false, false);
                            drawCustomText = true;
                        }
                        break;
                    }

                case HkShapeType.Bv:
                    {
                        var bvShape = (HkBvShape)shape;
                        DrawCollisionShape(bvShape.BoundingVolumeShape, worldMatrix, alpha, ref shapeIndex, null, true);
                        DrawCollisionShape(bvShape.ChildShape, worldMatrix, alpha, ref shapeIndex);
                        break;
                    }

                case HkShapeType.PhantomCallback:
                    {
                        // Nothing to draw, it's just shape with events
                        MyRenderProxy.DebugDrawText3D(worldMatrix.Translation, "Phantom", Color.Green, 0.75f, false);
                        break;
                    }

                default:
                    break;
            }

            if (drawCustomText && customText != null)
            {
                color.A = 255;
                MyRenderProxy.DebugDrawText3D(worldMatrix.Translation, customText, color, 0.8f, false);
            }
        }

        public static void DrawGeometry(HkGeometry geometry, MatrixD worldMatrix, Color color, bool depthRead = false, bool shaded = false)
        {
            var msg = MyRenderProxy.PrepareDebugDrawTriangles();

            try
            {
                for (int i = 0; i < geometry.TriangleCount; i++)
                {
                    int a, b, c, m;
                    geometry.GetTriangle(i, out a, out b, out c, out m);
                    msg.AddIndex(a);
                    msg.AddIndex(b);
                    msg.AddIndex(c);
                }

                for (int i = 0; i < geometry.VertexCount; i++)
                {
                    msg.AddVertex(geometry.GetVertex(i));
                }
            }
            finally
            {
                MyRenderProxy.DebugDrawTriangles(msg, worldMatrix, color, depthRead, shaded);
            }
        }

        static Dictionary<string, Vector3D> DebugShapesPositions = new Dictionary<string, Vector3D>();
        public static void DebugDrawBreakable(HkdBreakableBody bb, Vector3 offset)
        {
            const float alpha = 0.3f;

            //var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

            DebugShapesPositions.Clear();

            if (bb != null)
            {
                int index = 0;
                Matrix rbMatrix = bb.GetRigidBody().GetRigidBodyMatrix();
                MatrixD worldMatrix = MatrixD.CreateWorld(rbMatrix.Translation + offset, rbMatrix.Forward, rbMatrix.Up);

                DrawBreakableShape(bb.BreakableShape, worldMatrix, alpha, ref index);
                DrawConnections(bb.BreakableShape, worldMatrix, alpha, ref index);
            }

        }

        private static void DrawBreakableShape(HkdBreakableShape breakableShape, MatrixD worldMatrix, float alpha, ref int shapeIndex, string customText = null, bool isPhantom = false)
        {
            //VRageRender.MyRenderProxy.DebugDrawText3D(worldMatrix.Translation, , Color.White, 1, false);
            DrawCollisionShape(breakableShape.GetShape(), worldMatrix, alpha, ref shapeIndex, breakableShape.Name + " Strength: " + breakableShape.GetStrenght() + " Static:" + breakableShape.IsFixed());

            if (!string.IsNullOrEmpty(breakableShape.Name) && breakableShape.Name != "PineTree175m_v2_001" && breakableShape.IsFixed())
            {
            }

            DebugShapesPositions[breakableShape.Name] = worldMatrix.Translation;

            List<HkdShapeInstanceInfo> children = new List<HkdShapeInstanceInfo>();
            breakableShape.GetChildren(children);

            Vector3 parentCom = breakableShape.CoM;

            foreach (var shapeInst in children)
            {
                Matrix transform = shapeInst.GetTransform();
                //  transform.Translation += (shapeInst.Shape.CoM - parentCom);
                Matrix trWorld = transform * worldMatrix * Matrix.CreateTranslation(Vector3.Right * 2);
                DrawBreakableShape(shapeInst.Shape, trWorld, alpha, ref shapeIndex);
            }
        }

        private static void DrawConnections(HkdBreakableShape breakableShape, MatrixD worldMatrix, float alpha, ref int shapeIndex, string customText = null, bool isPhantom = false)
        {
            List<HkdConnection> connections = new List<HkdConnection>();
            breakableShape.GetConnectionList(connections);

            List<HkdShapeInstanceInfo> children = new List<HkdShapeInstanceInfo>();
            breakableShape.GetChildren(children);

            foreach (var conn in connections)
            {
                var posA = DebugShapesPositions[conn.ShapeAName];
                var posB = DebugShapesPositions[conn.ShapeBName];

                bool cont = false;
                foreach (var child in children)
                {
                    if ((child.ShapeName == conn.ShapeAName) || (child.ShapeName == conn.ShapeBName))
                        cont = true;
                }

                if (cont)
                    VRageRender.MyRenderProxy.DebugDrawLine3D(posA, posB, Color.White, Color.White, false);
            }
        }

        public static void DebugDrawAddForce(MyPhysicsBody physics, MyPhysicsForceType type, Vector3? force, Vector3D? position, Vector3? torque)
        {
            Matrix transform;

            const float scale = 0.1f;
            switch (type)
            {
                case MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE:
                    {
                        if (physics.RigidBody != null)
                        {
                            transform = physics.RigidBody.GetRigidBodyMatrix();
                            Vector3D p = physics.CenterOfMassWorld + physics.LinearVelocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;// ClusterToWorld(transform.Translation);//ClusterToWorld(transform.Translation);

                            if (force.HasValue)
                            {
                                Vector3 f = Vector3.TransformNormal(force.Value, transform) * scale;
                                MyRenderProxy.DebugDrawArrow3D(p, p + f, Color.Blue, Color.Red, false);
                            }
                            if (torque.HasValue)
                            {
                                Vector3 f = Vector3.TransformNormal(torque.Value, transform) * scale;
                                MyRenderProxy.DebugDrawArrow3D(p, p + f, Color.Blue, Color.Purple, false);
                            }
                        }
                    }
                    break;
                case MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE:
                    {
                        Vector3D p = position.Value + physics.LinearVelocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                        if (force.HasValue)
                        {
                            MyRenderProxy.DebugDrawArrow3D(p, p + force.Value * scale, Color.Blue, Color.Red, false);
                        }
                        if (torque.HasValue)
                        {
                            MyRenderProxy.DebugDrawArrow3D(p, p + torque.Value * scale, Color.Blue, Color.Purple, false);
                        }
                    }
                    break;
                case MyPhysicsForceType.APPLY_WORLD_FORCE:
                    {
                        if (position.HasValue)
                        {
                            Vector3D p = position.Value + physics.LinearVelocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                            if (force.HasValue)
                            {
                                MyRenderProxy.DebugDrawArrow3D(p, p + force.Value * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * scale, Color.Blue, Color.Red, false);
                            }
                        }
                    }

                    break;
                default:
                    {
                        Debug.Fail("Unhandled enum!");
                    }
                    break;
            }
        }

        public static void DebugDrawCoordinateSystem(Vector3? position, Vector3? forward, Vector3? side, Vector3? up, float scale = 1)
        {
            if (position.HasValue)
            {
                Vector3D p = position.Value;

                if (forward.HasValue)
                {
                    Vector3 f = forward.Value * scale;
                    MyRenderProxy.DebugDrawArrow3D(p, p + f, Color.Blue, Color.Red, false);
                }

                if (side.HasValue)
                {
                    Vector3 s = side.Value * scale;
                    MyRenderProxy.DebugDrawArrow3D(p, p + s, Color.Blue, Color.Green, false);
                }

                if (up.HasValue)
                {
                    Vector3 u = up.Value * scale;
                    MyRenderProxy.DebugDrawArrow3D(p, p + u, Color.Blue, Color.Blue, false);
                }
            }
        }

        public static void DebugDrawVector3(Vector3? position, Vector3? vector, Color color, float scale = 0.01f)
        {
            if (position.HasValue)
            {
                Vector3D p = position.Value;

                if (vector.HasValue)
                {
                    Vector3 v = vector.Value * scale;
                    MyRenderProxy.DebugDrawArrow3D(p, p + v, color, color, false);
                }
            }
        }
    }
}
