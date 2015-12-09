#region Using

using SharpDX;
using System.Collections.Generic;
using System.Text;
using System;

using SharpDX.Direct3D9;
using VRageRender.Graphics;
using VRageRender.Profiler;

#endregion

namespace VRageRender
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Rectangle = VRageMath.Rectangle;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingSphere = VRageMath.BoundingSphere;
    using BoundingFrustum = VRageMath.BoundingFrustum;
    using MathHelper = VRageMath.MathHelper;
    using VRageRender.Effects;
    using VRage.Utils;
    using VRage;
    using VRageMath;


    class MyDebugDraw : MyRenderComponentBase
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.DebugDraw;
        }

        static MyVertexFormatPositionColor[] m_verticesLine = null;
        static MyVertexFormatPositionColor[] m_triangleVertices = null;

        static MyLineBatch m_lineBatch;
        private static Vector3D[] m_frustumCorners;

        private static List<Vector3> m_vertices;
        private static List<short> m_indices;

        private static Vector3[] m_triVertices = new Vector3[0];
        private static int[] m_triIndices = new int[0];

        static MyRenderModel m_modelBoxLowRes;
        static MyRenderModel m_modelBoxHiRes;
        static MyRenderModel m_modelSphere;
        static MyRenderModel m_modelLightSphere;
        static MyRenderModel m_modelCone;
        static MyRenderModel m_modelHemisphere;
        static MyRenderModel m_modelHemisphereLowRes;
        static MyRenderModel m_modelCapsule;
        static MyRenderModel m_modelCylinderLow;

        public override void LoadContent()
        {
            MyRender.Log.WriteLine("MyDebugDraw.LoadContent() - START");
            MyRender.Log.IncreaseIndent();

            //  Line
            m_verticesLine = new MyVertexFormatPositionColor[2];
            m_verticesLine[0] = new MyVertexFormatPositionColor();
            m_verticesLine[1] = new MyVertexFormatPositionColor();

            //  Triangle
            m_triangleVertices = new MyVertexFormatPositionColor[3];
            m_triangleVertices[0] = new MyVertexFormatPositionColor();
            m_triangleVertices[1] = new MyVertexFormatPositionColor();
            m_triangleVertices[2] = new MyVertexFormatPositionColor();

            m_lineBatch = new MyLineBatch(Matrix.Identity, Matrix.Identity, 1024);

            m_frustumCorners = new Vector3D[8];
            m_vertices = new List<Vector3>(32);
            m_indices = new List<short>(128);

            m_modelBoxHiRes = MyRenderModels.GetModel("Models\\Debug\\BoxHiRes.mwm");
            m_modelBoxLowRes = MyRenderModels.GetModel("Models\\Debug\\BoxLowRes.mwm");

            m_modelSphere = MyRenderModels.GetModel("Models\\Debug\\Sphere_low.mwm");
            //m_modelSphere = MyModels.GetModel("Models2\\Debug\\Sphere");
            m_modelLightSphere = MyRenderModels.GetModel("Models\\Debug\\Sphere_low.mwm");
            m_modelCone = MyRenderModels.GetModel("Models\\Debug\\Cone.mwm");
            m_modelHemisphere = MyRenderModels.GetModel("Models\\Debug\\Hemisphere.mwm");
            m_modelHemisphereLowRes = MyRenderModels.GetModel("Models\\Debug\\Hemisphere_low.mwm");
            m_modelCapsule = MyRenderModels.GetModel("Models\\Debug\\Capsule.mwm");
            m_modelCylinderLow = MyRenderModels.GetModel("Models\\Debug\\Cylinder_Low.mwm");


            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyDebugDraw.LoadContent() - END");
        }

        public override void UnloadContent()
        {
        }

        public static void DrawLine3D(Vector3D pointFrom, Vector3D pointTo, Color colorFrom, Color colorTo, bool depthRead)
        {
            DrawLine3D(ref pointFrom, ref pointTo, ref colorFrom, ref colorTo, depthRead);
        }

        public static void DrawLine3D(ref Vector3D pointFrom, ref Vector3D pointTo, ref Color colorFrom, ref Color colorTo, bool depthRead)
        {
            Device graphicsDevice = MyRender.GraphicsDevice;

            RasterizerState.CullNone.Apply();

            if (depthRead)
                DepthStencilState.DepthRead.Apply();
            else
                DepthStencilState.None.Apply();

            //  Create the line vertices
            m_verticesLine[0].Position = (Vector3)(pointFrom - MyRenderCamera.Position);
            m_verticesLine[0].Color = colorFrom.ToVector4();
            m_verticesLine[1].Position = (Vector3)(pointTo - MyRenderCamera.Position);
            m_verticesLine[1].Color = colorTo.ToVector4();

            // Lower the Z-value of the lines to avoid Z-fighting in case they're drawn on geometry
            if (depthRead == true)
            {
                m_verticesLine[0].Position = m_verticesLine[0].Position * 0.99f;
                m_verticesLine[1].Position = m_verticesLine[1].Position * 0.99f;
            }

            var effect = (MyEffectModelsDiffuse)MyRender.GetEffect(MyEffects.ModelDiffuse);

            effect.SetDiffuseColor(Color.White.ToVector3());
            effect.SetProjectionMatrix(MyRenderCamera.ProjectionMatrix);
            var viewMatrix = MyRenderCamera.ViewMatrixAtZero;
            effect.SetViewMatrix(viewMatrix);
            effect.SetWorldMatrix(Matrix.Identity);
            effect.SetTechnique(MyEffectModelsDiffuse.Technique.PositionColor);

            graphicsDevice.VertexDeclaration = MyVertexFormatPositionColor.VertexDeclaration;
            //  Draw the line
            effect.Begin();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, 0, 1, m_verticesLine);
            effect.End();
        }

        public static void DrawLine2D(Vector2 pointFrom, Vector2 pointTo, Color colorFrom, Color colorTo, Matrix? projection = null)
        {
            Device graphicsDevice = MyRender.GraphicsDevice;
            RasterizerState.CullNone.Apply();
            DepthStencilState.None.Apply();
            BlendState.NonPremultiplied.Apply();

            //  Create the line vertices
            m_verticesLine[0].Position = new Vector3(pointFrom, 0);
            m_verticesLine[0].Color = colorFrom.ToVector4();
            m_verticesLine[1].Position = new Vector3(pointTo, 0);
            m_verticesLine[1].Color = colorTo.ToVector4();

            var effect = (MyEffectModelsDiffuse)MyRender.GetEffect(MyEffects.ModelDiffuse);

            effect.SetProjectionMatrix(projection ?? Matrix.CreateOrthographicOffCenter(0.0F, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0.0F, 0.0F, -1.0F));
            effect.SetViewMatrix(Matrix.Identity);
            effect.SetWorldMatrix(Matrix.Identity);
            effect.SetTechnique(MyEffectModelsDiffuse.Technique.PositionColor);
            effect.SetDiffuseColor4(Vector4.One);

            graphicsDevice.VertexDeclaration = MyVertexFormatPositionColor.VertexDeclaration;

            //  Draw the line
            effect.Begin();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, 0, 1, m_verticesLine);
            effect.End();
        }

        public static void DrawTriangles(MatrixD worldMatrix, List<Vector3D> vertices, List<int> indices, Color color, bool depthRead, bool shaded = false)
        {
            if (vertices.Count == 0 || indices.Count == 0)
                return;

            if (depthRead)
                DepthStencilState.DepthRead.Apply();
            else
                DepthStencilState.None.Apply();


            if (m_triVertices.Length < vertices.Count)
                m_triVertices = new Vector3[vertices.Count];

            if (m_triIndices.Length < indices.Count)
                m_triIndices = new int[indices.Count];

            for (int i = 0; i < vertices.Count; i++)
            {
                m_triVertices[i] = (Vector3)vertices[i];
            }

            indices.CopyTo(m_triIndices);

            Device graphicsDevice = MyRender.GraphicsDevice;

            var effect = (MyEffectModelsDiffuse)MyRender.GetEffect(MyEffects.ModelDiffuse);

            effect.SetDiffuseColor4(color.ToVector4());
            effect.SetProjectionMatrix(MyRenderCamera.ProjectionMatrix);
            effect.SetViewMatrix(MyRenderCamera.ViewMatrixAtZero);

            Matrix worldMatrixF = worldMatrix;
            worldMatrixF.Translation = (Vector3)(worldMatrix.Translation - MyRenderCamera.Position);
            effect.SetWorldMatrix(worldMatrixF);
            effect.SetTechnique(MyEffectModelsDiffuse.Technique.Position);

            graphicsDevice.VertexDeclaration = MyVertexFormatPositionFull.VertexDeclaration;
            if (!shaded)
            {
                MyStateObjects.WireframeClockwiseRasterizerState.Apply();
            }
            else
            {
                MyStateObjects.BiasedRasterizerCullNone_DebugDraw.Apply();
            }

            BlendState.NonPremultiplied.Apply();

            effect.Begin();
            graphicsDevice.DrawIndexedUserPrimitives<int, Vector3>(PrimitiveType.TriangleList, 0, vertices.Count, indices.Count / 3, m_triIndices, Format.Index32, m_triVertices);
            effect.End();
        }

        public static void DrawTriangle(Vector3D vertex1, Vector3D vertex2, Vector3D vertex3, Color color1, Color color2, Color color3, bool smooth, bool depthRead)
        {
            if (!smooth)
            {
                MyDebugDraw.DrawLine3D(ref vertex1, ref vertex2, ref color1, ref color2, depthRead);
                MyDebugDraw.DrawLine3D(ref vertex2, ref vertex3, ref color2, ref color3, depthRead);
                MyDebugDraw.DrawLine3D(ref vertex3, ref vertex1, ref color3, ref color1, depthRead);
            }
            else
            {
                Device graphicsDevice = MyRender.GraphicsDevice;

                if (depthRead)
                    DepthStencilState.DepthRead.Apply();
                else
                    DepthStencilState.None.Apply();

                BlendState.NonPremultiplied.Apply();
                MyStateObjects.BiasedRasterizerCullNone_DebugDraw.Apply();

                //  Create triangleVertexes vertices
                m_triangleVertices[0] = new MyVertexFormatPositionColor((Vector3)vertex1, color1.ToVector4());
                m_triangleVertices[1] = new MyVertexFormatPositionColor((Vector3)vertex2, color2.ToVector4());
                m_triangleVertices[2] = new MyVertexFormatPositionColor((Vector3)vertex3, color3.ToVector4());

                var effect = (MyEffectModelsDiffuse)MyRender.GetEffect(MyEffects.ModelDiffuse);

                // Initialise the effect
                effect.SetWorldMatrix(Matrix.Identity);
                effect.SetProjectionMatrix(MyRenderCamera.ProjectionMatrix);
                effect.SetViewMatrix((Matrix)MyRenderCamera.ViewMatrix);
                effect.SetDiffuseColor(Vector3.One);
                effect.SetTechnique(MyEffectModelsDiffuse.Technique.PositionColor);
                graphicsDevice.VertexDeclaration = MyVertexFormatPositionColor.VertexDeclaration;

                // Draw the line
                effect.Begin();
                graphicsDevice.VertexDeclaration = MyVertexFormatPositionColor.VertexDeclaration;
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, 0, 1, m_triangleVertices);
                effect.End();
            }
        }

        public static void DrawSphere(Vector3D position, float radius, Color diffuseColor, float alpha, bool depthRead, bool smooth, bool cull = true)
        {
            if (smooth)
            {
                MyStateObjects.SolidRasterizerState.Apply();
            }
            else
            {
                if (cull)
                {
                    MyStateObjects.WireframeCounterClockwiseRasterizerState.Apply();
                }
                else
                {
                    MyStateObjects.WireframeClockwiseRasterizerState.Apply();
                }
            }
            MatrixD m = MatrixD.Identity * radius;
            m.M44 = 1;
            m.Translation = position;
            DrawModel(ModelSphere, m, new Color(diffuseColor, alpha), depthRead);
        }

        public static void DrawSphereWireframe(Vector3D position, float radius, Color diffuseColor, float alpha, bool depthRead)
        {
            MyStateObjects.WireframeClockwiseRasterizerState.Apply();
            MatrixD m = MatrixD.Identity * radius;
            m.M44 = 1;
            m.Translation = position;
            DrawModel(ModelSphere, m, new Color(diffuseColor, alpha), depthRead);
        }

        public static void DrawSphereWireframe(MatrixD worldMatrix, Color diffuseColor, float alpha)
        {
            MyStateObjects.WireframeClockwiseRasterizerState.Apply();
            DrawModel(LightSphere, worldMatrix, new Color(diffuseColor, alpha), false);
        }

        public static void DrawHemisphereWireframe(MatrixD worldMatrix, Color diffuseColor, float alpha)
        {
            MyStateObjects.WireframeClockwiseRasterizerState.Apply();
            DrawModel(ModelHemisphereLowRes, worldMatrix, new Color(diffuseColor, alpha), false);
        }

        public static void DrawHemisphereShaded(MatrixD worldMatrix, Vector3 diffuseColor, float alpha, bool depthRead)
        {
            RasterizerState.CullNone.Apply();
            DrawModel(ModelHemisphereLowRes, worldMatrix, new Color(diffuseColor, alpha), depthRead);
        }

        public static void DrawSphereSmooth(MatrixD worldMatrix, Color diffuseColor, float alpha, bool depthRead)
        {
            RasterizerState.CullNone.Apply();
            DrawModel(ModelSphere, worldMatrix, new Color(diffuseColor, alpha), depthRead);
        }


        public static void GenerateCylinder(float height, float radius1, float radius2, int tessellation, List<Vector3> vertices, List<short> indices)
        {
            height /= 2;

            Vector3 up = new Vector3(0, 1, 0);
            Vector3 down = new Vector3(0, -1, 0);

            // Create a ring of triangles around the outside of the cylinder.
            for (int i = 0; i < tessellation; i++)
            {
                Vector3 normal = GetCircleVector(i, tessellation);

                Vector3 vtx = new Vector3();
                vtx = (normal * radius1) + (up * height);
                vertices.Add(vtx);

                vtx = new Vector3();
                vtx = (normal * radius2) + (down * height);
                vertices.Add(vtx);


                indices.Add((short)(i * 2));
                indices.Add((short)(((i * 2) + 2) % (tessellation * 2)));
                //indices.Add((short)((i * 2) + 1));

                //indices.Add((short)((i * 2) + 1));
                indices.Add((short)(((i * 2) + 2) % (tessellation * 2)));
                indices.Add((short)(((i * 2) + 3) % (tessellation * 2)));

                indices.Add((short)((i * 2) + 1));
                indices.Add((short)(((i * 2) + 3) % (tessellation * 2)));
            }
        }

        /// <summary>
        /// Helper method computes a point on a circle.
        /// </summary>
        private static Vector3 GetCircleVector(int i, int tessellation)
        {
            float angle = i * (float)System.Math.PI * 2 / tessellation;

            float dx = (float)Math.Cos(angle);
            float dz = (float)Math.Sin(angle);

            return new Vector3(dx, 0, dz);
        }

        public static void GenerateSphere(int stacks, int slices, float radius, bool halfSphere, List<Vector3> vertices, List<short> indices)
        {
            int n_Vertices = (stacks + 1) * (slices + 1);
            int dwIndices = (3 * stacks * (slices + 1)) * 2;

            float StackAngle = (float)Math.PI / (float)stacks * (halfSphere ? 0.5f : 1);
            float SliceAngle = (float)(Math.PI * 2.0) / (float)slices;

            Vector3 unitUpVector = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 unitRightVector = new Vector3(1.0f, 0.0f, 0.0f);

            int wVertexIndex = 0;
            //Generate the group of Stacks for the sphere
            for (int stack = 0; stack < (stacks + 1); stack++)
            {
                float r = (float)Math.Sin((float)stack * StackAngle);
                float y = (float)Math.Cos((float)stack * StackAngle);                //Generate the group of segments for the current Stack

                for (int slice = 0; slice < (slices + 1); slice++)
                {
                    float x = r * (float)Math.Sin((float)slice * SliceAngle);
                    float z = r * (float)Math.Cos((float)slice * SliceAngle);

                    Vector3 vtx = new Vector3();

                    vtx = new Vector3(x * radius, y * radius, z * radius); //normalized                    
                    vertices.Add(vtx);

                    if (!(stack == (stacks - 1)))
                    {
                        indices.Add((short)(wVertexIndex + (slices + 1)));
                        //indices.Add((short)(wVertexIndex + 1));
                        indices.Add((short)(wVertexIndex));

                        indices.Add((short)(wVertexIndex + (slices)));
                        indices.Add((short)(wVertexIndex + (slices + 1)));
                        //indices.Add((short)(wVertexIndex));

                        wVertexIndex++;
                    }

                }
            }
        }


        public static void DrawCylinder(Vector3 p0, Vector3 p1, float radius0, float radius1, Color color, bool depthRead)
        {
            RasterizerState.CullNone.Apply();
            MyStateObjects.SolidRasterizerState.Apply();
            VRageRender.Graphics.BlendState.Opaque.Apply();



            if (depthRead)
                DepthStencilState.DepthRead.Apply();
            else
                DepthStencilState.None.Apply();

            int slices = 12;

            m_vertices.Clear();
            m_indices.Clear();

            GenerateCylinder(Vector3.Distance(p0, p1), radius1, radius0, slices, m_vertices, m_indices);

            Vector3 originalOrientaion = Vector3.Up;
            Vector3 targetOrientation = Vector3.Normalize(p1 - p0);

            var t = Vector3.Cross(originalOrientaion, targetOrientation);
            Vector3 normal = Vector3.Normalize(Vector3.Cross(originalOrientaion, targetOrientation));

            if (MyUtils.IsZero(targetOrientation - Vector3.Up))
                normal = Vector3.Up;

            if (MyUtils.IsZero(targetOrientation + Vector3.Up))
                normal = Vector3.Forward;

            float alpha = (float)Math.Acos(Vector3.Dot(originalOrientaion, targetOrientation));

            Matrix transformMatrix = Matrix.CreateFromAxisAngle(normal, alpha);
            transformMatrix.Translation = (p0 + p1) * 0.5f;
            TransformDebugVertices(ref transformMatrix);
            DrawDebugLines(color);
        }

        public static void DrawCapsule(Vector3 p0, Vector3 p1, float radius, Color color, bool depthRead)
        {
            RasterizerState.CullNone.Apply();
            MyStateObjects.SolidRasterizerState.Apply();
            VRageRender.Graphics.BlendState.Opaque.Apply();

            if (depthRead)
                DepthStencilState.DepthRead.Apply();
            else
                DepthStencilState.None.Apply();

            int slices = 12;
            int stacks = 8;

            m_vertices.Clear();
            m_indices.Clear();

            GenerateCylinder(Vector3.Distance(p0, p1), radius, radius, slices, m_vertices, m_indices);

            Vector3 originalOrientaion = Vector3.Up;
            Vector3 targetOrientation = Vector3.Normalize(p1 - p0);

            var t = Vector3.Cross(originalOrientaion, targetOrientation);
            Vector3 normal = Vector3.Normalize(Vector3.Cross(originalOrientaion, targetOrientation));

            if (MyUtils.IsZero(targetOrientation - Vector3.Up))
                normal = Vector3.Up;

            if (MyUtils.IsZero(targetOrientation + Vector3.Up))
                normal = Vector3.Forward;

            float alpha = (float)Math.Acos(Vector3.Dot(originalOrientaion, targetOrientation));

            Matrix transformMatrix = Matrix.CreateFromAxisAngle(normal, alpha);
            transformMatrix.Translation = (p0 + p1) * 0.5f;
            TransformDebugVertices(ref transformMatrix);
            DrawDebugLines(color);

            m_vertices.Clear();
            m_indices.Clear();
            transformMatrix.Translation = p1;
            GenerateSphere(stacks, slices, radius, true, m_vertices, m_indices);
            TransformDebugVertices(ref transformMatrix);
            DrawDebugLines(color);

            m_vertices.Clear();
            m_indices.Clear();
            transformMatrix = Matrix.CreateRotationX(MathHelper.Pi) * transformMatrix;
            transformMatrix.Translation = p0;
            GenerateSphere(stacks, slices, radius, true, m_vertices, m_indices);
            TransformDebugVertices(ref transformMatrix);
            DrawDebugLines(color);
        }

        static void TransformDebugVertices(ref Matrix matrix)
        {
            for (int i = 0; i < m_vertices.Count; i++)
            {
                m_vertices[i] = Vector3.Transform(m_vertices[i], matrix);
            }
        }

        static void DrawDebugTriangles(Color color)
        {
            m_lineBatch.Begin();
            // draw lines from start to ends
            for (int i = 0; i < m_indices.Count; i += 3)
            {
                Vector3 v0 = m_vertices[m_indices[i + 0]];
                Vector3 v1 = m_vertices[m_indices[i + 1]];
                Vector3 v2 = m_vertices[m_indices[i + 2]];

                m_lineBatch.DrawLine(v0, v1, color);
                m_lineBatch.DrawLine(v1, v2, color);
                m_lineBatch.DrawLine(v2, v0, color);
            }
            m_lineBatch.End();
        }

        static void DrawDebugLines(Color color)
        {
            m_lineBatch.Begin();
            // draw lines from start to ends
            for (int i = 0; i < m_indices.Count; i += 2)
            {
                Vector3 v0 = m_vertices[m_indices[i + 0]];
                Vector3 v1 = m_vertices[m_indices[i + 1]];

                m_lineBatch.DrawLine(v0, v1, color);
                m_lineBatch.DrawLine(v1, v0, color);
            }
            m_lineBatch.End();
        }

        public static void DrawCapsuleShaded(Vector3D p0, Vector3D p1, float radius, Color color, bool depthRead)
        {
            //DrawSphereSmooth(p0, radius, color.ToVector3(), color.ToVector4().W, depthRead);
            //DrawSphereSmooth(p1, radius, color.ToVector3(), color.ToVector4().W, depthRead);

            var length = (p1 - p0).Length();
            var up = (p1 - p0) / length;
            MatrixD world = MatrixD.CreateWorld((p0 + p1) * 0.5, Vector3D.CalculatePerpendicularVector(up), up);
            DrawModel(ModelCylinderLow, MatrixD.CreateScale(radius * 2, length, radius * 2) * world, color.ToVector4(), depthRead);

            var tmp = world.Forward;
            world.Forward = world.Right;
            world.Right = tmp;
            world.Translation = p0;
            DrawModel(ModelHemisphereLowRes, MatrixD.CreateScale(radius * 0.955f) * world, color.ToVector4(), depthRead);
            world.Translation = p1;
            world.Up = -world.Up;
            DrawModel(ModelHemisphereLowRes, MatrixD.CreateScale(radius * 0.955f) * world, color.ToVector4(), depthRead);
        }

        public static void DrawCylinderShaded(Vector3D p0, Vector3D p1, float radius, Color color, bool depthRead)
        {
            var length = (p1 - p0).Length();
            var up = (p1 - p0) / length;
            MatrixD world = MatrixD.CreateWorld((p0 + p1) * 0.5f, Vector3D.CalculatePerpendicularVector(up), up);
            DrawModel(ModelCylinderLow, MatrixD.CreateScale(radius * 2, length, radius * 2) * world, color.ToVector4(), depthRead);
        }

        public static void DrawSphereSmooth(MatrixD worldMatrix, Matrix viewMatrixAtZero, Matrix projectionMatrix, Vector3 diffuseColor, float alpha)
        {
            RasterizerState.CullNone.Apply();
            Matrix relativeMatrix = (Matrix)worldMatrix;
            relativeMatrix.Translation = (Vector3)(worldMatrix.Translation - MyRenderCamera.Position);
            DrawModel(ModelSphere, relativeMatrix, viewMatrixAtZero, projectionMatrix, new Vector4(diffuseColor, alpha), false);
        }

        public static void DrawSphereSmooth(Vector3D position, float radius, Vector3 diffuseColor, float alpha, bool depthRead)
        {
            DrawSphereSmooth(MatrixD.CreateScale(radius) * MatrixD.CreateTranslation(position), diffuseColor, alpha, depthRead);
        }

        //  Draws hi-res wireframe box (its surface is split into many small triangles)
        public static void DrawHiresBoxWireframe(MatrixD worldMatrix, Vector3 diffuseColor, float alpha, bool depthRead)
        {
            MyStateObjects.WireframeClockwiseRasterizerState.Apply();
            DrawModel(ModelBoxHiRes, worldMatrix, new Vector4(diffuseColor, alpha), depthRead);
        }

        //  Draws hi-res smooth box (its surface is split into many small triangles)
        public static void DrawHiresBoxSmooth(MatrixD worldMatrix, Vector3 diffuseColor, float alpha)
        {
            RasterizerState.CullNone.Apply();
            DrawModel(ModelBoxHiRes, worldMatrix, new Vector4(diffuseColor, alpha), false);
        }

        //@ Draw world debug aabb
        public static void DrawAABB(ref BoundingBoxD worldAABB, ref Vector4 color, float fScale, bool depthRead)
        {
            var size = worldAABB.Max - worldAABB.Min;
            var center = size / 2f;
            center = center + worldAABB.Min;
            MatrixD mat = MatrixD.CreateWorld(center, new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
            MyDebugDraw.DrawHiresBoxWireframe(MatrixD.CreateScale(size * fScale) * mat, new Vector3(color.X, color.Y, color.Z), color.W, depthRead);
        }

        //@ Draw world debug aabb
        public static void DrawAABBLowRes(ref BoundingBoxD worldAABB, ref Vector4 color, float fScale)
        {
            var size = worldAABB.Max - worldAABB.Min;
            var center = size / 2f;
            center = center + worldAABB.Min;
            MatrixD mat = MatrixD.CreateWorld(center, new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
            MyDebugDraw.DrawLowresBoxWireframe(MatrixD.CreateScale(size * fScale) * mat, new Vector3(color.X, color.Y, color.Z), color.W, false);
        }

        public static void DrawAABBLine(ref BoundingBoxD worldAABB, ref Color color, float fScale, bool depthRead)
        {
            Vector3D center = worldAABB.Center;
            Vector3D halfSize = worldAABB.Size * fScale * 0.5f;

            Vector3D v0 = new Vector3D(center.X - halfSize.X, center.Y - halfSize.Y, center.Z - halfSize.Z);
            Vector3D v1 = new Vector3D(center.X + halfSize.X, center.Y - halfSize.Y, center.Z - halfSize.Z);
            Vector3D v2 = new Vector3D(center.X - halfSize.X, center.Y + halfSize.Y, center.Z - halfSize.Z);
            Vector3D v3 = new Vector3D(center.X + halfSize.X, center.Y + halfSize.Y, center.Z - halfSize.Z);
            Vector3D v4 = new Vector3D(center.X - halfSize.X, center.Y - halfSize.Y, center.Z + halfSize.Z);
            Vector3D v5 = new Vector3D(center.X + halfSize.X, center.Y - halfSize.Y, center.Z + halfSize.Z);
            Vector3D v6 = new Vector3D(center.X - halfSize.X, center.Y + halfSize.Y, center.Z + halfSize.Z);
            Vector3D v7 = new Vector3D(center.X + halfSize.X, center.Y + halfSize.Y, center.Z + halfSize.Z);

            MyDebugDraw.DrawLine3D(ref v0, ref v1, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v0, ref v2, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v2, ref v3, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v3, ref v1, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v4, ref v5, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v4, ref v6, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v6, ref v7, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v5, ref v7, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v0, ref v4, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v1, ref v5, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v2, ref v6, ref color, ref color, depthRead);
            MyDebugDraw.DrawLine3D(ref v3, ref v7, ref color, ref color, depthRead);
        }

        static Vector3D[] m_obbCorners = new Vector3D[8];

        public static void DrawOBBLine(MyOrientedBoundingBoxD worldOBB, Color color, float alpha, bool depthRead)
        {
            worldOBB.GetCorners(m_obbCorners, 0);

            m_lineBatch.Begin();

            for (int i = 0; i < MyOrientedBoundingBoxD.StartVertices.Length; i++)
            {
                Vector3D vs = m_obbCorners[MyOrientedBoundingBoxD.StartVertices[i]];
                Vector3D ve = m_obbCorners[MyOrientedBoundingBoxD.EndVertices[i]];

                m_lineBatch.DrawLine(vs, ve, color);
            }

            m_lineBatch.End();
        }

        //@ Draw world debug aabb
        public static void DrawAABBSolidLowRes(BoundingBoxD worldAABB, Vector4 color, float fScale)
        {
            Vector3D size = worldAABB.Max - worldAABB.Min;
            Vector3D center = size / 2f;
            center = center + worldAABB.Min;
            MatrixD mat = MatrixD.CreateWorld(center, new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
            MyDebugDraw.DrawLowresBoxSmooth(MatrixD.CreateScale(size * fScale) * mat, new Vector3(color.X, color.Y, color.Z), color.W, false);
        }

        //  Draws low-res wireframe box (12 triangles)
        public static void DrawLowresBoxWireframe(MatrixD worldMatrix, Vector3 diffuseColor, float alpha, bool depthRead, bool cull = true)
        {
            if (cull)
            {
                MyStateObjects.WireframeCounterClockwiseRasterizerState.Apply();
            }
            else
            {
                MyStateObjects.WireframeClockwiseRasterizerState.Apply();
            }
            DrawModel(ModelBoxLowRes, worldMatrix, new Vector4(diffuseColor, alpha), depthRead);
        }

        //  Draws low-res smooth box (12 triangles)
        public static void DrawLowresBoxSmooth(MatrixD worldMatrix, Vector3 diffuseColor, float alpha, bool depthRead, bool cull = true)
        {
            if (cull)
            {
                MyStateObjects.BiasedRasterizerCounterclockwise_DebugDraw.Apply();
            }
            else
            {
                MyStateObjects.BiasedRasterizerCullNone_DebugDraw.Apply();
            }
            DrawModel(ModelBoxLowRes, worldMatrix, new Vector4(diffuseColor, alpha), depthRead);
        }

        public static void DrawLowresCylinderWireframe(MatrixD worldMatrix, Vector3 diffuseColor, float alpha, bool depthRead)
        {
            MyStateObjects.WireframeCounterClockwiseRasterizerState.Apply();
            DrawModel(ModelCylinderLow, worldMatrix, new Vector4(diffuseColor, alpha), depthRead);
        }

        public static void DrawLowresCylinderSmooth(MatrixD worldMatrix, Vector3 diffuseColor, float alpha, bool depthRead)
        {
            MyStateObjects.BiasedRasterizerCounterclockwise_DebugDraw.Apply();
            DrawModel(ModelCylinderLow, worldMatrix, new Vector4(diffuseColor, alpha), depthRead);
        }

        //  Draws low-res smooth box (12 triangles)
        public static void DrawLowresBoxSmooth(Vector3D position, Vector3 scale, Vector3 diffuseColor, float alpha, bool depthRead)
        {
            RasterizerState.CullCounterClockwise.Apply();
            DrawLowresBoxSmooth(MatrixD.CreateScale((Vector3D)scale) * MatrixD.CreateTranslation(position), diffuseColor, alpha, depthRead);
        }

        public static void DrawModel(MyRenderModel model, MatrixD worldMatrix, Color diffuseColor, bool depthRead)
        {
            MatrixD relativeMatrix = worldMatrix;
            relativeMatrix.Translation = worldMatrix.Translation - MyRenderCamera.Position;
            DrawModel(model, relativeMatrix, MyRenderCamera.ViewMatrixAtZero, MyRenderCamera.ProjectionMatrix, diffuseColor, depthRead);
        }

        public static void DrawModel(MyRenderModel model, Matrix relativeMatrix, Matrix viewMatrixAtZero, Matrix projectionMatrix, Color diffuseColor, bool depthRead)
        {
            if (depthRead)
                DepthStencilState.DepthRead.Apply();
            else
                DepthStencilState.None.Apply();

            BlendState.NonPremultiplied.Apply();

            var effect = (MyEffectModelsDiffuse)MyRender.GetEffect(MyEffects.ModelDiffuse);

            effect.SetWorldMatrix(relativeMatrix);
            effect.SetViewMatrix(ref viewMatrixAtZero);
            effect.SetProjectionMatrix(ref projectionMatrix);
            effect.SetDiffuseColor4(diffuseColor.ToVector4());
            effect.SetTechnique(MyEffectModelsDiffuse.Technique.Position);

            effect.Begin();
            model.Render();
            effect.End();
        }

        public static void DrawAxis(MatrixD matrix, float axisLength, float alpha, bool depthRead)
        {
            var pos = matrix.Translation;

            MyDebugDraw.DrawLine3D(pos, pos + (matrix.Right * axisLength), Color.Red, Color.Red, depthRead);
            MyDebugDraw.DrawLine3D(pos, pos + (matrix.Up * axisLength), Color.Green, Color.Green, depthRead);
            MyDebugDraw.DrawLine3D(pos, pos + (matrix.Forward * axisLength), Color.Blue, Color.Blue, depthRead);
        }


        public static void DrawPlane(Vector3D position, Vector3 normal, Color color, bool depthRead)
        {
            //Anx + Bny + Cnz + D = 0

            var d = -Vector3D.Dot(position, (Vector3D)normal);

            Vector3D randomPoint = Vector3D.Cross(Vector3D.Normalize(new Vector3D(1, 2, 3)), (Vector3D)normal) + position;

            Vector3D dir = Vector3D.Normalize(randomPoint - position);

            for (int i = 0; i < 130; i++)
            {
                Matrix t = Matrix.CreateFromAxisAngle(normal, i * 0.05f);

                Vector3D rightOr = Vector3D.TransformNormal(dir, t);
                DrawLine3D(position, position + rightOr, color, color, depthRead);
            }
        }

        /// <summary>
        /// Draw debug text
        /// </summary>
        /// <param name="screenCoord"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="scale"></param>
        public static float DrawText(Vector2 screenCoord, StringBuilder text, Color color, float scale, bool depthRead, MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            return DrawText(screenCoord, text, color, scale, depthRead, MyStateObjects.GuiDefault_BlendState, align);
        }

        public static float DrawText(Vector2 screenCoord, StringBuilder text, Color color, float scale, bool depthRead, BlendState blendState, MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            if (depthRead)
                DepthStencilState.DepthRead.Apply();
            else
                DepthStencilState.None.Apply();

            MyRender.BeginSpriteBatch(blendState);

            Vector2 textSize = MyRender.GetDebugFont().MeasureString(text, scale);
            screenCoord = MyUtils.GetCoordAligned(screenCoord, textSize, align);
            float textLength = MyRender.GetDebugFont().DrawString(screenCoord, color, text, scale);

            MyRender.EndSpriteBatch();

            return textLength;
        }
        public static void DrawText(Vector3D worldCoord, StringBuilder text, Color color, float scale, bool depthRead, MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, int customViewProjection = -1)
        {
            System.Diagnostics.Debug.Assert(customViewProjection == -1 || MyRenderProxy.BillboardsViewProjectionRead.ContainsKey(customViewProjection));

            if (customViewProjection != -1 && !MyRenderProxy.BillboardsViewProjectionRead.ContainsKey(customViewProjection))
                return;

            //customViewProjection
            Vector4D screenCoord = Vector4D.Transform(worldCoord, customViewProjection == -1 ? MyRenderCamera.ViewProjectionMatrix : MyRenderProxy.BillboardsViewProjectionRead[customViewProjection].View * MyRenderProxy.BillboardsViewProjectionRead[customViewProjection].Projection);

            if (screenCoord.Z > 0)
            {
                Vector2 projectedPoint2D = new Vector2((float)(screenCoord.X / screenCoord.W) / 2.0f + 0.5f, (float)(-screenCoord.Y / screenCoord.W) / 2.0f + 0.5f);

                if (customViewProjection == -1)
                    projectedPoint2D = MyRender.GetHudPixelCoordFromNormalizedCoord(projectedPoint2D);
                else
                {
                    var myviewport = MyRenderProxy.BillboardsViewProjectionRead[customViewProjection].Viewport;
                    SharpDX.Viewport viewport = new Viewport((int)myviewport.OffsetX, (int)myviewport.OffsetY, (int)myviewport.Width, (int)myviewport.Height);

                    projectedPoint2D = new Vector2(
                        projectedPoint2D.X * viewport.Width + viewport.X,
                        projectedPoint2D.Y * viewport.Height + viewport.Y);
                }

                DrawText(projectedPoint2D, text, color, scale, depthRead, align);
            }
        }

        public static void DrawBoundingFrustum(BoundingFrustumD boundingFrustum, Color color)
        {
            boundingFrustum.GetCorners(m_frustumCorners);
            m_lineBatch.Begin();

            // near face            
            m_lineBatch.DrawLine(m_frustumCorners[0], m_frustumCorners[1], color);
            m_lineBatch.DrawLine(m_frustumCorners[1], m_frustumCorners[2], color);
            m_lineBatch.DrawLine(m_frustumCorners[2], m_frustumCorners[3], color);
            m_lineBatch.DrawLine(m_frustumCorners[3], m_frustumCorners[0], color);

            // far face            
            m_lineBatch.DrawLine(m_frustumCorners[4], m_frustumCorners[5], color);
            m_lineBatch.DrawLine(m_frustumCorners[5], m_frustumCorners[6], color);
            m_lineBatch.DrawLine(m_frustumCorners[6], m_frustumCorners[7], color);
            m_lineBatch.DrawLine(m_frustumCorners[7], m_frustumCorners[4], color);

            // top,right,bottom,left face            
            m_lineBatch.DrawLine(m_frustumCorners[0], m_frustumCorners[4], color);
            m_lineBatch.DrawLine(m_frustumCorners[1], m_frustumCorners[5], color);
            m_lineBatch.DrawLine(m_frustumCorners[2], m_frustumCorners[6], color);
            m_lineBatch.DrawLine(m_frustumCorners[3], m_frustumCorners[7], color);

            m_lineBatch.End();
        }

        public static void DrawCone(Vector3 start, Vector3 end, float radius, Color color)
        {
            m_vertices.Clear();
            Vector3 forward = Vector3.Normalize(end - start);
            Vector3 up = Vector3.Cross(forward, Vector3.Right);
            float length = Vector3.Distance(end, start);
            Matrix world = Matrix.CreateWorld(start, forward, up);

            float angleStep = MathHelper.TwoPi / 32;

            for (int i = 0; i < 32; i++)
            {
                Vector3 vertex = new Vector3();
                vertex.X = (float)Math.Cos(i * angleStep) * radius;
                vertex.Y = (float)Math.Sin(i * angleStep) * radius;
                vertex.Z = length;
                vertex = Vector3.Transform(vertex, world);
                m_vertices.Add(vertex);
            }

            m_lineBatch.Begin();
            // draw lines from start to ends
            for (int i = 0; i < m_vertices.Count; i++)
            {
                m_lineBatch.DrawLine(start, m_vertices[i], color);
            }
            m_lineBatch.End();
        }

        /// <summary>
        /// Draw occlusion bounding box method with our premade effect and box.
        /// </summary>
        /// <param name="bbox"></param>
        /// <param name="scale"></param>
        /// <param name="enableDepthTesting"></param>
        /// <param name="billboardLike">Indicates whether the occlusion object (box) is rotated to face the camera or not.</param>
        public static void DrawOcclusionBoundingBox(BoundingBoxD bbox, float scale, bool enableDepthTesting, bool billboardLike = false, bool useDepthTarget = true)
        {
            var cameraToBBox = bbox.Center - MyRenderCamera.Position;
            MatrixD worldMatrix = billboardLike ? MatrixD.CreateWorld(Vector3D.Zero, MyUtils.Normalize(cameraToBBox), (Vector3D)MyUtils.Normalize(MyRenderCamera.UpVector + MyRenderCamera.LeftVector)) : MatrixD.Identity;

            Vector3D scaleV = (bbox.Max - bbox.Min) * scale;
            worldMatrix *= MatrixD.CreateScale(scaleV);
            worldMatrix.Translation = cameraToBBox;



            MyEffectOcclusionQueryDraw effectOQ = MyRender.GetEffect(MyEffects.OcclusionQueryDrawMRT) as MyEffectOcclusionQueryDraw;

            if (enableDepthTesting)
                effectOQ.SetTechnique(MyEffectOcclusionQueryDraw.Technique.DepthTestEnabled);
            else
                effectOQ.SetTechnique(MyEffectOcclusionQueryDraw.Technique.DepthTestDisabled);


            effectOQ.SetWorldMatrix((Matrix)worldMatrix);
            effectOQ.SetViewMatrix(MyRenderCamera.ViewMatrixAtZero);
            effectOQ.SetProjectionMatrix(MyRenderCamera.ProjectionMatrix);

            if (useDepthTarget)
            {
                var depthRenderTarget = MyRender.GetRenderTarget(MyRenderTargets.Depth);
                effectOQ.SetDepthRT(depthRenderTarget);
                effectOQ.SetScale(MyRender.GetScaleForViewport(depthRenderTarget));
            }

            effectOQ.Begin();

            //draw
            ModelBoxLowRes.Render();

            effectOQ.End();
        }

        /// <summary>
        /// Only to be called with FastOcclusionBoundingBoxDraw
        /// </summary>
        public static void PrepareFastOcclusionBoundingBoxDraw()
        {
            MyEffectOcclusionQueryDraw effectOQ = MyRender.GetEffect(MyEffects.OcclusionQueryDrawMRT) as MyEffectOcclusionQueryDraw;
            effectOQ.SetTechnique(MyEffectOcclusionQueryDraw.Technique.DepthTestEnabled);

            effectOQ.SetViewMatrix(MyRenderCamera.ViewMatrixAtZero);
            effectOQ.SetProjectionMatrix(MyRenderCamera.ProjectionMatrix);

            var depthRenderTarget = MyRender.GetRenderTarget(MyRenderTargets.Depth);
            effectOQ.SetDepthRT(depthRenderTarget);
            effectOQ.SetScale(MyRender.GetScaleForViewport(depthRenderTarget));
        }

        // Vertices must be in triangle strip order
        // 0--1
        // | /|
        // |/ |
        // 2--3
        public static void OcclusionPlaneDraw(Vector3[] quad)
        {
            MatrixD worldMatrix = MatrixD.Identity;
            worldMatrix.Translation = -MyRenderCamera.Position;

            MyEffectOcclusionQueryDraw effectOQ = MyRender.GetEffect(MyEffects.OcclusionQueryDrawMRT) as MyEffectOcclusionQueryDraw;
            effectOQ.SetWorldMatrix((Matrix)worldMatrix);
            effectOQ.SetViewMatrix(MyRenderCamera.ViewMatrixAtZero);
            effectOQ.SetProjectionMatrix(MyRenderCamera.ProjectionMatrix);

            var depthRenderTarget = MyRender.GetRenderTarget(MyRenderTargets.Depth);
            effectOQ.SetDepthRT(depthRenderTarget);
            effectOQ.SetScale(MyRender.GetScaleForViewport(depthRenderTarget));
            effectOQ.SetTechnique(MyEffectOcclusionQueryDraw.Technique.DepthTestEnabledNonMRT);
        }

        /// <summary>
        /// Needs to have PrepareFastOcclusionBoundingBoxDraw() called first
        /// </summary>
        public static void FastOcclusionBoundingBoxDraw(BoundingBoxD bbox, float scale)
        {
            Vector3D scaleV = (bbox.Max - bbox.Min) * scale;
            MatrixD worldMatrix = MatrixD.CreateScale(scaleV);
            worldMatrix.Translation = bbox.Center - MyRenderCamera.Position;

            MyEffectOcclusionQueryDraw effectOQ = MyRender.GetEffect(MyEffects.OcclusionQueryDrawMRT) as MyEffectOcclusionQueryDraw;
            effectOQ.SetWorldMatrix((Matrix)worldMatrix);
            effectOQ.SetTechnique(MyEffectOcclusionQueryDraw.Technique.DepthTestEnabledNonMRT);

            effectOQ.Begin();

            //draw
            ModelBoxLowRes.Render();
            MyPerformanceCounter.PerCameraDrawWrite["Fast occlusion box draw calls"]++;

            effectOQ.End();
        }


        public static void DrawSphereForLight(MyEffectPointLight effect, ref MatrixD worldMatrix, ref Vector3 diffuseColor, float alpha)
        {
            MatrixD worldViewProjection;
            MatrixD.Multiply(ref worldMatrix, ref MyRenderCamera.ViewProjectionMatrixAtZero, out worldViewProjection);

            Matrix worldViewProjMatrix2 = (Matrix)worldViewProjection;
            Matrix worldMatrix2 = (Matrix)worldMatrix;


            effect.SetWorldViewProjMatrix(ref worldViewProjMatrix2);
            effect.SetWorldMatrix(ref worldMatrix2);

            effect.Begin();

            MyPerformanceCounter.PerCameraDrawWrite["Light draw calls"]++;
            LightSphere.Render();

            effect.End();
        }

        public static void DrawSphereForLight(MyEffectPointLight effect, Vector3D position, float radius, ref Vector3 diffuseColor, float alpha)
        {
            MatrixD scaleMatrix;
            MatrixD.CreateScale(radius, out scaleMatrix);
            MatrixD positionMatrix;
            position = position - MyRenderCamera.Position;
            MatrixD.CreateTranslation(ref position, out positionMatrix);
            MatrixD lightMatrix;
            MatrixD.Multiply(ref scaleMatrix, ref positionMatrix, out lightMatrix);

            DrawSphereForLight(effect, ref lightMatrix, ref diffuseColor, alpha);
        }

        public static void DrawHemisphereForLight(MyEffectPointLight effect, ref MatrixD worldMatrix, ref Vector3 diffuseColor, float alpha)
        {
            MatrixD worldViewProjMatrix;
            MatrixD.Multiply(ref worldMatrix, ref MyRenderCamera.ViewProjectionMatrixAtZero, out worldViewProjMatrix);

            Matrix worldViewProjMatrix2 = (Matrix)worldViewProjMatrix;
            Matrix worldMatrix2 = (Matrix)worldMatrix;
            effect.SetWorldViewProjMatrix(ref worldViewProjMatrix2);
            effect.SetWorldMatrix(ref worldMatrix2);

            effect.Begin();

            MyPerformanceCounter.PerCameraDrawWrite["Light draw calls"]++;
            ModelHemisphereLowRes.Render();

            effect.End();
        }

        public static void DrawHemisphereForLight(MyEffectPointLight effect, ref Vector3D position, float radius, ref Vector3 diffuseColor, float alpha)
        {
            MatrixD scaleMatrix;
            MatrixD.CreateScale(radius, out scaleMatrix);
            MatrixD positionMatrix;
            position = position - MyRenderCamera.Position;
            MatrixD.CreateTranslation(ref position, out positionMatrix);
            MatrixD lightMatrix;
            MatrixD.Multiply(ref scaleMatrix, ref positionMatrix, out lightMatrix);

            DrawHemisphereForLight(effect, ref lightMatrix, ref diffuseColor, alpha);
        }

        public static void DrawConeForLight(MyEffectPointLight effect, MatrixD worldMatrix)
        {
            MatrixD worldViewProjMatrix;
            MatrixD.Multiply(ref worldMatrix, ref MyRenderCamera.ViewProjectionMatrixAtZero, out worldViewProjMatrix);

            Matrix worldViewProjMatrix2 = (Matrix)worldViewProjMatrix;
            effect.SetWorldViewProjMatrix(ref worldViewProjMatrix2);
            Matrix worldMatrix2 = (Matrix)worldMatrix;
            effect.SetWorldMatrix(ref worldMatrix2);

            effect.Begin();

            MyPerformanceCounter.PerCameraDrawWrite["Light draw calls"]++;
            ModelCone.Render();

            effect.End();
        }

        public static void DrawConeForLight(MyEffectPointLight effect, Vector3 position, Vector3 direction, Vector3 upVector, float coneLength, float coneCosAngle)
        {
            // Cone is oriented backwards
            float scaleZ = -coneLength;

            // Calculate cone side (hypotenuse of triangle)
            float side = coneLength / coneCosAngle;

            // Calculate cone bottom scale (Pythagoras theorem)
            float scaleXY = (float)System.Math.Sqrt(side * side - coneLength * coneLength);

            // Calculate world matrix as scale * light world matrix
            MatrixD world = MatrixD.CreateScale(scaleXY, scaleXY, scaleZ) * Matrix.CreateWorld(position - MyRenderCamera.Position, direction, upVector);
            DrawConeForLight(effect, world);
        }

        /// <summary>
        /// GenerateLines
        /// </summary>
        /// <param name="vctStart"></param>
        /// <param name="vctEnd"></param>
        /// <param name="vctSideStep"></param>
        /// <param name="worldMatrix"></param>
        /// <param name="m_lineBuffer"></param>
        /// <param name="divideRatio"></param>
        private static void GenerateLines(Vector3 vctStart, Vector3 vctEnd, ref Vector3 vctSideStep, ref Matrix worldMatrix, ref List<VRageMath.Line> m_lineBuffer, int divideRatio)
        {
            for (int i = 0; i <= divideRatio; ++i)
            {
                Vector3 transformedStart = Vector3.Transform(vctStart, worldMatrix);
                Vector3 transformedEnd = Vector3.Transform(vctEnd, worldMatrix);

                if (m_lineBuffer.Count < m_lineBuffer.Capacity)
                {
                    VRageMath.Line line = new VRageMath.Line(transformedStart, transformedEnd, false);
                    //@ generate Line
                    m_lineBuffer.Add(line);

                    vctStart += vctSideStep;
                    vctEnd += vctSideStep;
                }
            }
        }


        public static MyRenderModel ModelCone
        {
            get { m_modelCone.LoadData(); m_modelCone.LoadInDraw(); return m_modelCone; }
        }
        public static MyRenderModel ModelHemisphereLowRes
        {
            get { m_modelHemisphereLowRes.LoadData(); m_modelHemisphereLowRes.LoadInDraw(); return m_modelHemisphereLowRes; }
        }
        public static MyRenderModel ModelSphere
        {
            get { m_modelSphere.LoadData(); m_modelSphere.LoadInDraw(); return m_modelSphere; }
        }
        public static MyRenderModel LightSphere
        {
            get { m_modelLightSphere.LoadData(); m_modelLightSphere.LoadInDraw(); return m_modelLightSphere; }
        }
        public static MyRenderModel ModelBoxLowRes
        {
            get { m_modelBoxLowRes.LoadData(); m_modelBoxLowRes.LoadInDraw(); return m_modelBoxLowRes; }
        }
        public static MyRenderModel ModelBoxHiRes
        {
            get { m_modelBoxHiRes.LoadData(); m_modelBoxHiRes.LoadInDraw(); return m_modelBoxHiRes; }
        }
        public static MyRenderModel Capsule
        {
            get { m_modelCapsule.LoadData(); m_modelCapsule.LoadInDraw(); return m_modelCapsule; }
        }
        public static MyRenderModel ModelCylinderLow
        {
            get { m_modelCylinderLow.LoadData(); m_modelCylinderLow.LoadInDraw(); return m_modelCylinderLow; }
        }

        static public class TextBatch
        {
            struct TextData
            {
                public TextData(Vector2 screenCoord, StringBuilder text, Color color, float scale)
                {
                    this.screenCoord = screenCoord;
                    this.color = color;
                    this.text = text;
                    this.scale = scale;
                }

                public Vector2 screenCoord;
                public Color color;
                public StringBuilder text;
                public float scale;
            }

            static List<TextData> m_data = new List<TextData>();

            static public void AddText(Vector2 screenCoord, StringBuilder text, Color color, float scale)
            {
                m_data.Add(new TextData(screenCoord, text, color, scale));
            }
        }
    }
}
