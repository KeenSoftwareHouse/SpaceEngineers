using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

namespace VRage.Game
{
    public enum MySimpleObjectRasterizer
    {
        Solid,
        Wireframe,
        SolidAndWireframe
    }

    public static class MySimpleObjectDraw
    {
        private static List<LineD> m_lineBuffer = new List<LineD>(2000);   //max capacity of rendered lines  
        private static readonly List<Vector3D> m_verticesBuffer = new List<Vector3D>(2000);   //max capacity of rendered lines

        // Empty function, but forces static variable preload during game load
        static MySimpleObjectDraw() { }

        public static void DrawTransparentBox(ref MatrixD worldMatrix, ref BoundingBoxD localbox, ref Color color, MySimpleObjectRasterizer rasterization, 
            int wireDivideRatio, float lineWidth = 1, string faceMaterial = null, string lineMaterial = null, bool onlyFrontFaces = false, 
            int customViewProjection = -1)
        {
            DrawTransparentBox(ref worldMatrix, ref localbox, ref color, ref color, rasterization, new Vector3I(wireDivideRatio), lineWidth, faceMaterial, 
                lineMaterial, onlyFrontFaces, customViewProjection);
        }

        public static void DrawTransparentBox(ref MatrixD worldMatrix, ref BoundingBoxD localbox, ref Color color, ref Color frontFaceColor, 
            MySimpleObjectRasterizer rasterization, int wireDivideRatio, float lineWidth = 1, string faceMaterial = null, string lineMaterial = null, 
            bool onlyFrontFaces = false, int customViewProjection = -1)
        {
            DrawTransparentBox(ref worldMatrix, ref localbox, ref color, ref frontFaceColor, rasterization, new Vector3I(wireDivideRatio), lineWidth, faceMaterial, lineMaterial, onlyFrontFaces, customViewProjection);
        }

        public static void DrawAttachedTransparentBox(ref MatrixD worldMatrix, ref BoundingBoxD localbox, ref Color color,
            int renderObjectID, ref MatrixD worldToLocal,
            MySimpleObjectRasterizer rasterization, int wireDivideRatio, float lineWidth = 1, string faceMaterial = null, string lineMaterial = null, bool onlyFrontFaces = false)
        {
            DrawAttachedTransparentBox(ref worldMatrix, ref localbox, ref color, renderObjectID, ref worldToLocal, rasterization, new Vector3I(wireDivideRatio), lineWidth, faceMaterial, lineMaterial, onlyFrontFaces);
        }

        public static bool FaceVisible(Vector3D center, Vector3D normal)
        {
            var viewDir = Vector3D.Normalize(center - MyTransparentGeometry.Camera.Translation);
            return Vector3D.Dot(viewDir, normal) < 0;
        }

        /// <summary>
        /// DrawTransparentBox
        /// </summary>
        public static void DrawTransparentBox(ref MatrixD worldMatrix, ref BoundingBoxD localbox, ref Color color, ref Color frontFaceColor, MySimpleObjectRasterizer rasterization, Vector3I wireDivideRatio, float lineWidth = 1, string faceMaterial = null, string lineMaterial = null, bool onlyFrontFaces = false, int customViewProjection = -1)
        {
            if (faceMaterial == null)
            {
                faceMaterial = "ContainerBorder";
            }

            if (rasterization == MySimpleObjectRasterizer.Solid || rasterization == MySimpleObjectRasterizer.SolidAndWireframe)
            {
                Vector3 vctMin = localbox.Min;
                Vector3 vctMax = localbox.Max;

                //@ CreateQuads
                
                MyQuadD quad;

                MatrixD orientation = MatrixD.Identity;
                orientation.Forward = worldMatrix.Forward;
                orientation.Up = worldMatrix.Up;
                orientation.Right = worldMatrix.Right;

                Vector3D translation = worldMatrix.Translation + Vector3D.Transform(localbox.Center, orientation);// +(vctMin + vctMax) / 2;

                float halfWidth = (float)(localbox.Max.X - localbox.Min.X) / 2f;
                float halfHeight = (float)(localbox.Max.Y - localbox.Min.Y) / 2f;
                float halfDeep = (float)(localbox.Max.Z - localbox.Min.Z) / 2f;

                //@ Front side
                Vector3D faceNorm = Vector3D.TransformNormal(Vector3.Forward, orientation);
                faceNorm *= halfDeep;
                Vector3D vctPos = translation + faceNorm;
                if (!onlyFrontFaces || FaceVisible(vctPos, faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfHeight, ref worldMatrix);
                    MyTransparentGeometry.AddQuad(faceMaterial, ref quad, frontFaceColor, ref vctPos, customViewProjection);
                }

                //@ Back side
                vctPos = translation - faceNorm;
                if (!onlyFrontFaces || FaceVisible(vctPos, -faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfHeight, ref worldMatrix);
                    MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos,  customViewProjection);
                }

                //@ Left side
                MatrixD rotMat = MatrixD.CreateRotationY(MathHelper.ToRadians(90f));
                MatrixD rotated = rotMat * worldMatrix;
                faceNorm = Vector3.TransformNormal(Vector3.Left, worldMatrix);
                faceNorm *= halfWidth;
                vctPos = translation + faceNorm;
                if (!onlyFrontFaces || FaceVisible(vctPos, faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfDeep, halfHeight, ref rotated);
                    MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);
                }

                //@ Right side
                vctPos = translation - faceNorm;
                if (!onlyFrontFaces || FaceVisible(vctPos, -faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfDeep, halfHeight, ref rotated);
                    MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);
                }

                //@ Top side
                rotMat = Matrix.CreateRotationX(MathHelper.ToRadians(90f));
                rotated = rotMat * worldMatrix;
                faceNorm = Vector3.TransformNormal(Vector3.Up, worldMatrix);
                faceNorm *= ((localbox.Max.Y - localbox.Min.Y) / 2f);
                vctPos = translation + faceNorm;
                if (!onlyFrontFaces || FaceVisible(vctPos, faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfDeep, ref rotated);
                    MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);
                }

                //@ Bottom side
                vctPos = translation - faceNorm;
                if (!onlyFrontFaces || FaceVisible(vctPos, -faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfDeep, ref rotated);
                    MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);
                }
            }

            if (rasterization == MySimpleObjectRasterizer.Wireframe || rasterization == MySimpleObjectRasterizer.SolidAndWireframe)
            {
                Color wireColor = color;
                wireColor *= 1.3f;
                DrawWireFramedBox(ref worldMatrix, ref localbox, ref wireColor, lineWidth, wireDivideRatio, lineMaterial, onlyFrontFaces, customViewProjection);
            }
        }

        public static void DrawTransparentRamp(ref MatrixD worldMatrix, ref BoundingBoxD localbox, ref Color color, string faceMaterial = null, bool onlyFrontFaces = false, int customViewProjection = -1)
        {
            if (faceMaterial == null)
            {
                faceMaterial = "ContainerBorder";
            }

            //@ CreateQuads
            MyQuadD quad;

            MatrixD orientation = MatrixD.Identity;
            orientation.Forward = worldMatrix.Forward;
            orientation.Up = worldMatrix.Up;
            orientation.Right = worldMatrix.Right;

            Vector3D translation = worldMatrix.Translation + Vector3D.Transform(localbox.Center, orientation);// +(vctMin + vctMax) / 2;

            float halfWidth  = (float)(localbox.Max.X - localbox.Min.X) / 2f;
            float halfHeight = (float)(localbox.Max.Y - localbox.Min.Y) / 2f;
            float halfDeep = (float)(localbox.Max.Z - localbox.Min.Z) / 2f;

            //@ Back side
            var faceNorm = Vector3D.TransformNormal(Vector3D.Forward, orientation) * halfDeep;
            var vctPos = translation - (Vector3D)faceNorm;
            if (!onlyFrontFaces || FaceVisible(vctPos, -faceNorm))
            {
                MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfHeight, ref worldMatrix);
                MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);
            }

            //@ Left side
            MatrixD rotMat = MatrixD.CreateRotationY(MathHelper.ToRadians(90f));
            MatrixD rotated = rotMat * worldMatrix;
            faceNorm = Vector3.TransformNormal(Vector3.Left, worldMatrix);
            faceNorm *= halfWidth;
            vctPos = translation + faceNorm;
            if (!onlyFrontFaces || FaceVisible(vctPos, faceNorm))
            {
                MyUtils.GenerateQuad(out quad, ref vctPos, halfDeep, halfHeight, ref rotated);
                quad.Point3 = quad.Point0;
                MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);
            }

            //@ Right side
            vctPos = translation - faceNorm;
            if (!onlyFrontFaces || FaceVisible(vctPos, -faceNorm))
            {
                MyUtils.GenerateQuad(out quad, ref vctPos, halfDeep, halfHeight, ref rotated);
                quad.Point3 = quad.Point0;
                MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);
            }

            Vector3 p1 = Vector3.One;
            Vector3 p2 = Vector3.One;

            //@ Bottom side
            rotMat = Matrix.CreateRotationX(MathHelper.ToRadians(90f));
            rotated = rotMat * worldMatrix;
            faceNorm = Vector3.TransformNormal(Vector3.Up, worldMatrix);
            faceNorm *= ((localbox.Max.Y - localbox.Min.Y) / 2f);
            vctPos = translation - faceNorm;
            if (!onlyFrontFaces || FaceVisible(vctPos, -faceNorm))
            {
                MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfDeep, ref rotated);
                p1 = quad.Point1;
                p2 = quad.Point2;
                MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);
            }

            //@ Top side
            vctPos = translation + faceNorm;
            if (!onlyFrontFaces || FaceVisible(vctPos, faceNorm))
            {
                MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfDeep, ref rotated);
                quad.Point1 = p1;
                quad.Point2 = p2;
                MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);
            }
        }

        public static void DrawTransparentRoundedCorner(ref MatrixD worldMatrix, ref BoundingBoxD localbox, ref Color color, string faceMaterial = null, int customViewProjection = -1)
        {
            if (faceMaterial == null)
            {
                faceMaterial = "ContainerBorder";
            }

            MyQuadD quad;

            // Back side
            quad.Point0   = localbox.Min;
            quad.Point0.Z = localbox.Max.Z;
            quad.Point1   = localbox.Max;
            quad.Point1.Y = localbox.Min.Y;
            quad.Point2   = localbox.Max;
            quad.Point3   = localbox.Max;
            quad.Point3.X = localbox.Min.X;

            quad.Point0 = Vector3.Transform(quad.Point0, worldMatrix);
            quad.Point1 = Vector3.Transform(quad.Point1, worldMatrix);
            quad.Point2 = Vector3.Transform(quad.Point2, worldMatrix);
            quad.Point3 = Vector3.Transform(quad.Point3, worldMatrix);

            Vector3D vctPos = (quad.Point0 + quad.Point1 + quad.Point2 + quad.Point3) * 0.25;
            MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);

            // Right side
            quad.Point0   = localbox.Min;
            quad.Point0.X = localbox.Max.X;
            quad.Point1   = localbox.Max;
            quad.Point1.Z = localbox.Min.Z;
            quad.Point2   = localbox.Max;
            quad.Point3   = localbox.Max;
            quad.Point3.Y = localbox.Min.Y;

            quad.Point0 = Vector3.Transform(quad.Point0, worldMatrix);
            quad.Point1 = Vector3.Transform(quad.Point1, worldMatrix);
            quad.Point2 = Vector3.Transform(quad.Point2, worldMatrix);
            quad.Point3 = Vector3.Transform(quad.Point3, worldMatrix);

            vctPos = (quad.Point0 + quad.Point1 + quad.Point2 + quad.Point3) * 0.25f;
            MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);

            // Rounded Side
            float angleStep = MathHelper.TwoPi / 40;
            float alpha  = 0;
            float radius = (float)(localbox.Max.X - localbox.Min.X);
            float radiusHalf = radius * 0.5f;

            // transalte the position to center of the right side
            var center = (quad.Point2 + quad.Point3) * 0.5f;
            var backup = worldMatrix.Translation;
            worldMatrix.Translation = center;

            for (int i = 20; i < 30; i++)
            {
                alpha = i * angleStep;
                var ca = (float)(radius * Math.Cos(alpha));
                var sa = (float)(radius * Math.Sin(alpha));

                quad.Point0.X = ca;
                quad.Point0.Z = sa;
                quad.Point3.X = ca;
                quad.Point3.Z = sa;

                alpha = (i + 1) * angleStep;
                ca = (float)(radius * Math.Cos(alpha));
                sa = (float)(radius * Math.Sin(alpha));

                quad.Point1.X = ca;
                quad.Point1.Z = sa;
                quad.Point2.X = ca;
                quad.Point2.Z = sa;

                quad.Point0.Y = -radiusHalf;
                quad.Point1.Y = -radiusHalf;
                quad.Point2.Y =  radiusHalf;
                quad.Point3.Y =  radiusHalf;

                quad.Point0 = Vector3.Transform(quad.Point0, worldMatrix);
                quad.Point1 = Vector3.Transform(quad.Point1, worldMatrix);
                quad.Point2 = Vector3.Transform(quad.Point2, worldMatrix);
                quad.Point3 = Vector3.Transform(quad.Point3, worldMatrix);

                vctPos = (quad.Point0 + quad.Point1 + quad.Point2 + quad.Point3) * 0.25f;
                MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjection);
            }
            worldMatrix.Translation = backup;
        }

        public static void DrawAttachedTransparentBox(ref MatrixD worldMatrix, ref BoundingBoxD localbox, ref Color color, 
            int renderObjectID, ref MatrixD worldToLocal,
            MySimpleObjectRasterizer rasterization, Vector3I wireDivideRatio, float lineWidth = 1, string faceMaterial = null, string lineMaterial = null, bool onlyFrontFaces = false)
        {
            if (faceMaterial == null)
            {
                faceMaterial = "ContainerBorder";
            }

            if (rasterization == MySimpleObjectRasterizer.Solid || rasterization == MySimpleObjectRasterizer.SolidAndWireframe)
            {
                Vector3 vctMin = localbox.Min;
                Vector3 vctMax = localbox.Max;

                //@ CreateQuads

                MyQuadD quad;

                MatrixD orientation = MatrixD.Identity;
                orientation.Forward = worldMatrix.Forward;
                orientation.Up = worldMatrix.Up;
                orientation.Right = worldMatrix.Right;

                Vector3D translation = worldMatrix.Translation + Vector3D.Transform(localbox.Center, orientation);// +(vctMin + vctMax) / 2;

                float halfWidth = (float)(localbox.Max.X - localbox.Min.X) / 2f;
                float halfHeight = (float)(localbox.Max.Y - localbox.Min.Y) / 2f;
                float halfDeep = (float)(localbox.Max.Z - localbox.Min.Z) / 2f;

                //@ Front side
                Vector3D faceNorm = Vector3D.TransformNormal(Vector3D.Forward, orientation);
                Vector3D vctPos = translation + faceNorm * halfDeep;
                if (!onlyFrontFaces || FaceVisible(vctPos, faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfHeight, ref worldMatrix);
                    Vector3D.Transform(ref quad.Point0, ref worldToLocal, out quad.Point0);
                    Vector3D.Transform(ref quad.Point1, ref worldToLocal, out quad.Point1);
                    Vector3D.Transform(ref quad.Point2, ref worldToLocal, out quad.Point2);
                    Vector3D.Transform(ref quad.Point3, ref worldToLocal, out quad.Point3);
                    MyTransparentGeometry.AddAttachedQuad(faceMaterial, ref quad, color, ref vctPos, renderObjectID);
                }

                //@ Back side
                vctPos = translation - faceNorm * halfDeep;
                if (!onlyFrontFaces || FaceVisible(vctPos, -faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfHeight, ref worldMatrix);
                    Vector3D.Transform(ref quad.Point0, ref worldToLocal, out quad.Point0);
                    Vector3D.Transform(ref quad.Point1, ref worldToLocal, out quad.Point1);
                    Vector3D.Transform(ref quad.Point2, ref worldToLocal, out quad.Point2);
                    Vector3D.Transform(ref quad.Point3, ref worldToLocal, out quad.Point3);
                    MyTransparentGeometry.AddAttachedQuad(faceMaterial, ref quad, color, ref vctPos, renderObjectID);
                }

                //@ Left side
                MatrixD rotMat = Matrix.CreateRotationY(MathHelper.ToRadians(90f));
                MatrixD rotated = rotMat * worldMatrix;
                faceNorm = Vector3D.TransformNormal(Vector3D.Left, worldMatrix);
                vctPos = translation + faceNorm * halfWidth;
                if (!onlyFrontFaces || FaceVisible(vctPos, faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfDeep, halfHeight, ref rotated);
                    Vector3D.Transform(ref quad.Point0, ref worldToLocal, out quad.Point0);
                    Vector3D.Transform(ref quad.Point1, ref worldToLocal, out quad.Point1);
                    Vector3D.Transform(ref quad.Point2, ref worldToLocal, out quad.Point2);
                    Vector3D.Transform(ref quad.Point3, ref worldToLocal, out quad.Point3);
                    MyTransparentGeometry.AddAttachedQuad(faceMaterial, ref quad, color, ref vctPos, renderObjectID);
                }

                //@ Right side
                vctPos = translation - faceNorm * halfWidth;
                if (!onlyFrontFaces || FaceVisible(vctPos, -faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfDeep, halfHeight, ref rotated);
                    Vector3D.Transform(ref quad.Point0, ref worldToLocal, out quad.Point0);
                    Vector3D.Transform(ref quad.Point1, ref worldToLocal, out quad.Point1);
                    Vector3D.Transform(ref quad.Point2, ref worldToLocal, out quad.Point2);
                    Vector3D.Transform(ref quad.Point3, ref worldToLocal, out quad.Point3);
                    MyTransparentGeometry.AddAttachedQuad(faceMaterial, ref quad, color, ref vctPos, renderObjectID);
                }

                //@ Top side
                rotMat = MatrixD.CreateRotationX(MathHelper.ToRadians(90f));
                rotated = rotMat * worldMatrix;
                faceNorm = Vector3D.TransformNormal(Vector3D.Up, worldMatrix);
                vctPos = translation + faceNorm * halfHeight;
                if (!onlyFrontFaces || FaceVisible(vctPos, faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfDeep, ref rotated);
                    Vector3D.Transform(ref quad.Point0, ref worldToLocal, out quad.Point0);
                    Vector3D.Transform(ref quad.Point1, ref worldToLocal, out quad.Point1);
                    Vector3D.Transform(ref quad.Point2, ref worldToLocal, out quad.Point2);
                    Vector3D.Transform(ref quad.Point3, ref worldToLocal, out quad.Point3);
                    MyTransparentGeometry.AddAttachedQuad(faceMaterial, ref quad, color, ref vctPos, renderObjectID);
                }

                //@ Bottom side
                vctPos = translation - faceNorm * halfHeight;
                if (!onlyFrontFaces || FaceVisible(vctPos, -faceNorm))
                {
                    MyUtils.GenerateQuad(out quad, ref vctPos, halfWidth, halfDeep, ref rotated);
                    Vector3D.Transform(ref quad.Point0, ref worldToLocal, out quad.Point0);
                    Vector3D.Transform(ref quad.Point1, ref worldToLocal, out quad.Point1);
                    Vector3D.Transform(ref quad.Point2, ref worldToLocal, out quad.Point2);
                    Vector3D.Transform(ref quad.Point3, ref worldToLocal, out quad.Point3);
                    MyTransparentGeometry.AddAttachedQuad(faceMaterial, ref quad, color, ref vctPos, renderObjectID);
                }

            }

            if (rasterization == MySimpleObjectRasterizer.Wireframe || rasterization == MySimpleObjectRasterizer.SolidAndWireframe)
            {
                Vector4 vctWireColor = color;
                vctWireColor *= 1.3f;
                DrawAttachedWireFramedBox(ref worldMatrix, ref localbox, 
                    renderObjectID, ref worldToLocal,
                    ref vctWireColor, lineWidth, wireDivideRatio, lineMaterial, onlyFrontFaces);
            }
        }

        /// <summary>
        /// DrawWireFramedBox
        /// </summary>
        /// <param name="worldMatrix"></param>
        /// <param name="localbox"></param>
        /// <param name="color"></param>
        /// <param name="bWireFramed"></param>
        /// <param name="wireDivideRatio"></param>
        /// <param name="wireDivideRatio"></param>
        private static void DrawWireFramedBox(ref MatrixD worldMatrix, ref BoundingBoxD localbox, ref Color color, float fThickRatio, Vector3I wireDivideRatio, string lineMaterial = null, bool onlyFrontFaces = false, int customViewProjection = -1)
        {
            if (lineMaterial == null)
            {
                lineMaterial = "GizmoDrawLine";
            }

            m_lineBuffer.Clear();

            //@ generate linnes for Front Side

            MatrixD orientation = MatrixD.Identity;
            orientation.Forward = worldMatrix.Forward;
            orientation.Up = worldMatrix.Up;
            orientation.Right = worldMatrix.Right;

            bool front = Vector3D.Dot(orientation.Forward, MyTransparentGeometry.Camera.Forward) > 0;
            bool right = Vector3D.Dot(orientation.Right, MyTransparentGeometry.Camera.Forward) > 0;
            bool top = Vector3D.Dot(orientation.Up, MyTransparentGeometry.Camera.Forward) > 0;

            var forwardNormal = orientation.Forward;
            var rightNormal = orientation.Right;
            var upNormal = orientation.Up;

            float width = (float)localbox.Size.X;
            float height = (float)localbox.Size.Y;
            float deep = (float)localbox.Size.Z;

            Vector3D globalBoxCenter = Vector3D.Transform(localbox.Center, worldMatrix);

            Vector3D faceCenter = globalBoxCenter + forwardNormal * (deep * 0.5f); // Front side
            Vector3D faceCenter2 = globalBoxCenter - forwardNormal * (deep * 0.5f); // Back side

            //@ FrontSide
            Vector3D vctStart = localbox.Min;
            Vector3D vctEnd = vctStart + Vector3.Up * height;
            Vector3D vctSideStep = Vector3.Right * (width / wireDivideRatio.X);
            if (!onlyFrontFaces || FaceVisible(faceCenter, forwardNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.X);
            }
            // BackSide
            vctStart += Vector3.Backward * deep;
            vctEnd = vctStart + Vector3.Up * height;
            if (!onlyFrontFaces || FaceVisible(faceCenter2, -forwardNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.X);
            }

            //@ FrontSide
            vctStart = localbox.Min;
            vctEnd = vctStart + Vector3.Right * width;
            vctSideStep = Vector3.Up * (height / wireDivideRatio.Y);
            if (!onlyFrontFaces || FaceVisible(faceCenter, forwardNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Y);
            }
            //@ BackSide
            vctStart += Vector3.Backward * deep;
            vctEnd += Vector3.Backward * deep;
            if (!onlyFrontFaces || FaceVisible(faceCenter2, -forwardNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Y);
            }

            Matrix rotMat = Matrix.CreateRotationY(MathHelper.ToRadians(90f));
            Matrix rotated = rotMat * worldMatrix;

            faceCenter = globalBoxCenter - rightNormal * (width * 0.5f); // Left side
            faceCenter2 = globalBoxCenter + rightNormal * (width * 0.5f); // Right side

            //@ LeftSide
            vctStart = localbox.Min;
            vctEnd = vctStart + Vector3.Backward * deep;
            vctSideStep = Vector3.Up * (height / wireDivideRatio.Y);
            if (!onlyFrontFaces || FaceVisible(faceCenter, -rightNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Y);
            }
            // RightSide
            vctStart = localbox.Min;
            vctStart += Vector3.Right * width;
            vctEnd = vctStart + Vector3.Backward * deep;
            if (!onlyFrontFaces || FaceVisible(faceCenter2, rightNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Y);
            }

            //@ LeftSide
            vctStart = localbox.Min;
            vctEnd = vctStart + Vector3.Up * height;
            vctSideStep = Vector3.Backward * (deep / wireDivideRatio.Z);
            if (!onlyFrontFaces || FaceVisible(faceCenter, -rightNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Z);
            }
            // RightSide
            vctStart += Vector3.Right * width;
            vctEnd += Vector3.Right * width;
            if (!onlyFrontFaces || FaceVisible(faceCenter2, rightNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Z);
            }


            //if (wireDivideRatio.Y > 1)
            {
                faceCenter = globalBoxCenter - upNormal * (height * 0.5f); // Top side
                faceCenter2 = globalBoxCenter + upNormal * (height * 0.5f); // Bottom side

                //@ TopSide
                vctStart = localbox.Min;
                vctEnd = vctStart + Vector3.Right * width;
                vctSideStep = Vector3.Backward * (deep / wireDivideRatio.Z);
                if (!onlyFrontFaces || FaceVisible(faceCenter, -upNormal))
                {
                    GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Z);
                }
                // BottomSide
                vctStart += Vector3.Up * height;
                vctEnd += Vector3.Up * height;
                if (!onlyFrontFaces || FaceVisible(faceCenter2, upNormal))
                {
                    GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Z);
                }

                //@ TopSide
                vctStart = localbox.Min;
                vctEnd = vctStart + Vector3.Backward * deep;
                vctSideStep = Vector3.Right * (width / wireDivideRatio.X);
                if (!onlyFrontFaces || FaceVisible(faceCenter, -upNormal))
                {
                    GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.X);
                }
                // BottomSide
                vctStart += Vector3.Up * height;
                vctEnd += Vector3.Up * height;
                if (!onlyFrontFaces || FaceVisible(faceCenter2, upNormal))
                {
                    GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.X);
                }
            }


            Vector3 size = new Vector3D(localbox.Max.X - localbox.Min.X, localbox.Max.Y - localbox.Min.Y, localbox.Max.Z - localbox.Min.Z);
            float thickness = MathHelper.Max(1, MathHelper.Min(MathHelper.Min(size.X, size.Y), size.Z));
            thickness *= fThickRatio;
            //billboard
            foreach (LineD line in m_lineBuffer)
            {
                //@ 16 - lifespan for 1 update in 60FPS
                MyTransparentGeometry.AddLineBillboard(lineMaterial, color, line.From, line.Direction, (float)line.Length, thickness, 
                    VRageRender.MyBillboard.BlenType.Standard, customViewProjection);
            }
        }

        private static void DrawAttachedWireFramedBox(ref MatrixD worldMatrix, ref BoundingBoxD localbox, 
            int renderObjectID, ref MatrixD worldToLocal,
            ref Vector4 vctColor, float fThickRatio, Vector3I wireDivideRatio, string lineMaterial = null, bool onlyFrontFaces = false)
        {
            if (lineMaterial == null)
            {
                lineMaterial = "GizmoDrawLine";
            }

            m_lineBuffer.Clear();

            //@ generate linnes for Front Side

            MatrixD orientation = MatrixD.Identity;
            orientation.Forward = worldMatrix.Forward;
            orientation.Up = worldMatrix.Up;
            orientation.Right = worldMatrix.Right;

            bool front = Vector3D.Dot(orientation.Forward, MyTransparentGeometry.Camera.Forward) > 0;
            bool right = Vector3D.Dot(orientation.Right, MyTransparentGeometry.Camera.Forward) > 0;
            bool top = Vector3D.Dot(orientation.Up, MyTransparentGeometry.Camera.Forward) > 0;

            var forwardNormal = orientation.Forward;
            var rightNormal = orientation.Right;
            var upNormal = orientation.Up;

            float width = (float)localbox.Size.X;
            float height = (float)localbox.Size.Y;
            float deep = (float)localbox.Size.Z;

            Vector3 globalBoxCenter = Vector3.Transform(localbox.Center, worldMatrix);

            Vector3 faceCenter = globalBoxCenter + forwardNormal * (deep * 0.5f); // Front side
            Vector3 faceCenter2 = globalBoxCenter - forwardNormal * (deep * 0.5f); // Back side

            //@ FrontSide
            Vector3D vctStart = localbox.Min;
            Vector3D vctEnd = vctStart + Vector3.Up * height;
            Vector3D vctSideStep = Vector3.Right * (width / wireDivideRatio.X);
            if (!onlyFrontFaces || FaceVisible(faceCenter, forwardNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.X);
            }
            // BackSide
            vctStart += Vector3.Backward * deep;
            vctEnd = vctStart + Vector3.Up * height;
            if (!onlyFrontFaces || FaceVisible(faceCenter2, -forwardNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.X);
            }

            //@ FrontSide
            vctStart = localbox.Min;
            vctEnd = vctStart + Vector3.Right * width;
            vctSideStep = Vector3.Up * (height / wireDivideRatio.Y);
            if (!onlyFrontFaces || FaceVisible(faceCenter, forwardNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Y);
            }
            //@ BackSide
            vctStart += Vector3.Backward * deep;
            vctEnd += Vector3.Backward * deep;
            if (!onlyFrontFaces || FaceVisible(faceCenter2, -forwardNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Y);
            }

            Matrix rotMat = Matrix.CreateRotationY(MathHelper.ToRadians(90f));
            Matrix rotated = rotMat * worldMatrix;

            faceCenter = globalBoxCenter - rightNormal * (width * 0.5f); // Left side
            faceCenter2 = globalBoxCenter + rightNormal * (width * 0.5f); // Right side

            //@ LeftSide
            vctStart = localbox.Min;
            vctEnd = vctStart + Vector3.Backward * deep;
            vctSideStep = Vector3.Up * (height / wireDivideRatio.Y);
            if (!onlyFrontFaces || FaceVisible(faceCenter, -rightNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Y);
            }
            // RightSide
            vctStart = localbox.Min;
            vctStart += Vector3.Right * width;
            vctEnd = vctStart + Vector3.Backward * deep;
            if (!onlyFrontFaces || FaceVisible(faceCenter2, rightNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Y);
            }

            //@ LeftSide
            vctStart = localbox.Min;
            vctEnd = vctStart + Vector3.Up * height;
            vctSideStep = Vector3.Backward * (deep / wireDivideRatio.Z);
            if (!onlyFrontFaces || FaceVisible(faceCenter, -rightNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Z);
            }
            // RightSide
            vctStart += Vector3.Right * width;
            vctEnd += Vector3.Right * width;
            if (!onlyFrontFaces || FaceVisible(faceCenter2, rightNormal))
            {
                GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Z);
            }


            //if (wireDivideRatio.Y > 1)
            {
                faceCenter = globalBoxCenter - upNormal * (height * 0.5f); // Top side
                faceCenter2 = globalBoxCenter + upNormal * (height * 0.5f); // Bottom side

                //@ TopSide
                vctStart = localbox.Min;
                vctEnd = vctStart + Vector3.Right * width;
                vctSideStep = Vector3.Backward * (deep / wireDivideRatio.Z);
                if (!onlyFrontFaces || FaceVisible(faceCenter, -upNormal))
                {
                    GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Z);
                }
                // BottomSide
                vctStart += Vector3.Up * height;
                vctEnd += Vector3.Up * height;
                if (!onlyFrontFaces || FaceVisible(faceCenter2, upNormal))
                {
                    GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.Z);
                }

                //@ TopSide
                vctStart = localbox.Min;
                vctEnd = vctStart + Vector3.Backward * deep;
                vctSideStep = Vector3.Right * (width / wireDivideRatio.X);
                if (!onlyFrontFaces || FaceVisible(faceCenter, -upNormal))
                {
                    GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.X);
                }
                // BottomSide
                vctStart += Vector3.Up * height;
                vctEnd += Vector3.Up * height;
                if (!onlyFrontFaces || FaceVisible(faceCenter2, upNormal))
                {
                    GenerateLines(vctStart, vctEnd, ref vctSideStep, ref worldMatrix, ref m_lineBuffer, wireDivideRatio.X);
                }
            }


            Vector3 size = new Vector3D(localbox.Max.X - localbox.Min.X, localbox.Max.Y - localbox.Min.Y, localbox.Max.Z - localbox.Min.Z);
            float thickness = MathHelper.Max(1, MathHelper.Min(MathHelper.Min(size.X, size.Y), size.Z));
            thickness *= fThickRatio;
            //billboard
            foreach (LineD line in m_lineBuffer)
            {
                //@ 16 - lifespan for 1 update in 60FPS
                MyTransparentGeometry.AddLineBillboard(lineMaterial, vctColor, line.From,
                    renderObjectID, ref worldToLocal,
                    line.Direction, (float)line.Length, thickness);
            }
        }

        /// <summary>
        /// DrawTransparentSphere
        /// </summary>
        /// <param name="vctPos"></param>
        /// <param name="radius"></param>
        /// <param name="color"></param>
        /// <param name="bWireFramed"></param>
        /// <param name="wireDivideRatio"></param>
        public static void DrawTransparentSphere(ref MatrixD worldMatrix, float radius, ref Color color, MySimpleObjectRasterizer rasterization, int wireDivideRatio, string faceMaterial = null, string lineMaterial = null, float lineThickness = -1, int customViewProjectionMatrix = -1)
        {
            if (lineMaterial == null)
            {
                lineMaterial = "GizmoDrawLine";
            }

            m_verticesBuffer.Clear();
            MyMeshHelper.GenerateSphere(ref worldMatrix, radius, wireDivideRatio, m_verticesBuffer);
            Vector3D vctZero = Vector3D.Zero;

            float thickness = radius * 0.01f;
            if (lineThickness > -1.0f)
            {
                thickness = lineThickness;
            }
            int i = 0;
            for (i = 0; i < m_verticesBuffer.Count; i += 4)
            {
                MyQuadD quad;
                quad.Point0 = m_verticesBuffer[i + 1];
                quad.Point1 = m_verticesBuffer[i + 3];
                quad.Point2 = m_verticesBuffer[i + 2];
                quad.Point3 = m_verticesBuffer[i];

                if (rasterization == MySimpleObjectRasterizer.Solid || rasterization == MySimpleObjectRasterizer.SolidAndWireframe)
                {
                    MyTransparentGeometry.AddQuad(faceMaterial ?? "ContainerBorder", ref quad, color, ref vctZero, customViewProjectionMatrix);
                }

                if (rasterization == MySimpleObjectRasterizer.Wireframe || rasterization == MySimpleObjectRasterizer.SolidAndWireframe)
                {

                    //@ 20 - lifespan for 1 update in 60FPPS
                    Vector3D start = quad.Point0;
                    Vector3D dir = quad.Point1 - start;
                    float len = (float)dir.Length();
                    if (len > 0.1f)
                    {
                        dir = MyUtils.Normalize(dir);

                        MyTransparentGeometry.AddLineBillboard(lineMaterial, color, start, dir, len, thickness, 
                            VRageRender.MyBillboard.BlenType.Standard, customViewProjectionMatrix);
                    }

                    start = quad.Point1;
                    dir = quad.Point2 - start;
                    len = (float)dir.Length();
                    if (len > 0.1f)
                    {
                        dir = MyUtils.Normalize(dir);

                        MyTransparentGeometry.AddLineBillboard(lineMaterial, color, start, dir, len, thickness, 
                            VRageRender.MyBillboard.BlenType.Standard, customViewProjectionMatrix);
                    }

                }
            }
        }

        public static void DrawTransparentCapsule(ref MatrixD worldMatrix, float radius, float height, ref Color color, int wireDivideRatio, string faceMaterial = null, int customViewProjectionMatrix = -1)
        {
            if (faceMaterial == null)
                faceMaterial = "ContainerBorder";

            var heightHalf = height * 0.5f;
            var center = worldMatrix.Translation;

            MatrixD upperSphere = MatrixD.CreateRotationX(-MathHelper.PiOver2);
            upperSphere.Translation = new Vector3D(0, heightHalf, 0);
            upperSphere *= worldMatrix;

            m_verticesBuffer.Clear();
            MyMeshHelper.GenerateSphere(ref upperSphere, radius, wireDivideRatio, m_verticesBuffer);
            Vector3D vctZero = Vector3D.Zero;

            var countHalf = m_verticesBuffer.Count / 2;
            for (int i = 0; i < countHalf; i += 4)
            {
                MyQuadD quad;
                quad.Point0 = m_verticesBuffer[i + 1];
                quad.Point1 = m_verticesBuffer[i + 3];
                quad.Point2 = m_verticesBuffer[i + 2];
                quad.Point3 = m_verticesBuffer[i];
                MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctZero, customViewProjectionMatrix);
            }

            MatrixD lowerSphere = MatrixD.CreateRotationX(-MathHelper.PiOver2);
            lowerSphere.Translation = new Vector3D(0, -heightHalf, 0);
            lowerSphere *= worldMatrix;

            m_verticesBuffer.Clear();
            MyMeshHelper.GenerateSphere(ref lowerSphere, radius, wireDivideRatio, m_verticesBuffer);

            for (int i = countHalf; i < m_verticesBuffer.Count; i += 4)
            {
                MyQuadD quad;
                quad.Point0 = m_verticesBuffer[i + 1];
                quad.Point1 = m_verticesBuffer[i + 3];
                quad.Point2 = m_verticesBuffer[i + 2];
                quad.Point3 = m_verticesBuffer[i];
                MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctZero, customViewProjectionMatrix);
            }

            // Cylinder
            var angleStep = MathHelper.TwoPi / wireDivideRatio;
            var alpha = 0f;
            for (int i = 0; i < wireDivideRatio; i++)
            {
                MyQuadD quad;
                alpha = i * angleStep;
                var ca = (float)(radius * Math.Cos(alpha));
                var sa = (float)(radius * Math.Sin(alpha));

                quad.Point0.X = ca;
                quad.Point0.Z = sa;
                quad.Point3.X = ca;
                quad.Point3.Z = sa;

                alpha = (i + 1) * angleStep;
                ca = (float)(radius * Math.Cos(alpha));
                sa = (float)(radius * Math.Sin(alpha));

                quad.Point1.X = ca;
                quad.Point1.Z = sa;
                quad.Point2.X = ca;
                quad.Point2.Z = sa;

                quad.Point0.Y = -heightHalf;
                quad.Point1.Y = -heightHalf;
                quad.Point2.Y = heightHalf;
                quad.Point3.Y = heightHalf;

                quad.Point0 = Vector3D.Transform(quad.Point0, worldMatrix);
                quad.Point1 = Vector3D.Transform(quad.Point1, worldMatrix);
                quad.Point2 = Vector3D.Transform(quad.Point2, worldMatrix);
                quad.Point3 = Vector3D.Transform(quad.Point3, worldMatrix);

                var vctPos = (quad.Point0 + quad.Point1 + quad.Point2 + quad.Point3) * 0.25f;
                MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref vctPos, customViewProjectionMatrix);
            }
        }

        public static void DrawTransparentCone(ref MatrixD worldMatrix, float radius, float height, ref Color color, int wireDivideRatio, string faceMaterial = null, int customViewProjectionMatrix = -1)
        {
            DrawTransparentCone(worldMatrix.Translation, worldMatrix.Forward * height, worldMatrix.Up * radius, color, wireDivideRatio, faceMaterial, customViewProjectionMatrix);
        }

        private static void DrawTransparentCone(Vector3D apexPosition, Vector3 directionVector, Vector3 baseVector, Color color, int wireDivideRatio, string faceMaterial = null, int customViewProjectionMatrix = -1)
        {
            faceMaterial = faceMaterial ?? "ContainerBorder";

            var axis = directionVector;
            axis.Normalize();

            var apex = apexPosition;

            var stepsRcp = (float)(Math.PI * 2 / wireDivideRatio);
            MyQuadD quad;
            for (int i = 0; i < wireDivideRatio; i++)
            {
                float a0 = i * stepsRcp;
                float a1 = (i + 1) * stepsRcp;

                var A = apexPosition + directionVector + Vector3.Transform(baseVector, Matrix.CreateFromAxisAngle(axis, a0));
                var B = apexPosition + directionVector + Vector3.Transform(baseVector, Matrix.CreateFromAxisAngle(axis, a1));

                quad.Point0 = A;
                quad.Point1 = B;
                quad.Point2 = apex;
                quad.Point3 = apex;

                MyTransparentGeometry.AddQuad(faceMaterial, ref quad, color, ref Vector3D.Zero);
            }
        }

        public static void DrawTransparentCuboid(ref MatrixD worldMatrix, MyCuboid cuboid, ref Vector4 vctColor, bool bWireFramed, float thickness, string lineMaterial = null)
        {
            foreach (Line line in cuboid.UniqueLines)
            {
                Vector3D from = Vector3D.Transform(line.From, worldMatrix);
                Vector3D to = Vector3D.Transform(line.To, worldMatrix);
                DrawLine(from, to, lineMaterial ?? "GizmoDrawLine", ref vctColor, thickness);
            }
        }

        public static void DrawLine(Vector3D start, Vector3D end, string material, ref Vector4 color, float thickness)
        {
            Vector3 dir = (Vector3)(end - start);
            float len = dir.Length();
            if (len > 0.1f)
            {
                dir = MyUtils.Normalize(dir);

                MyTransparentGeometry.AddLineBillboard(material ?? "GizmoDrawLine", color, start, dir, len, thickness);
            }
        }

        public static void DrawTransparentCylinder(ref MatrixD worldMatrix, float radius1, float radius2, float length, ref Vector4 vctColor, bool bWireFramed, int wireDivideRatio, float thickness, string lineMaterial = null)
        {
            Vector3 vertexEnd = Vector3.Zero;
            Vector3 vertexStart = Vector3.Zero;

            Vector3 previousEnd = Vector3.Zero;
            Vector3 previousStart = Vector3.Zero;

            float angleStep = 360.0f / (float)wireDivideRatio;
            float alpha = 0;

            for (int i = 0; i <= wireDivideRatio; i++)
            {
                alpha = (float)i * angleStep;

                vertexEnd.X = (float)(radius1 * Math.Cos(MathHelper.ToRadians(alpha)));
                vertexEnd.Y = length / 2;
                vertexEnd.Z = (float)(radius1 * Math.Sin(MathHelper.ToRadians(alpha)));

                vertexStart.X = (float)(radius2 * Math.Cos(MathHelper.ToRadians(alpha)));
                vertexStart.Y = -length / 2;
                vertexStart.Z = (float)(radius2 * Math.Sin(MathHelper.ToRadians(alpha)));

                vertexEnd = Vector3D.Transform(vertexEnd, worldMatrix);
                vertexStart = Vector3D.Transform(vertexStart, worldMatrix);

                DrawLine(vertexStart, vertexEnd, lineMaterial ?? "GizmoDrawLine", ref vctColor, thickness);

                if (i > 0)
                {
                    DrawLine(previousStart, vertexStart, lineMaterial ?? "GizmoDrawLine", ref vctColor, thickness);
                    DrawLine(previousEnd, vertexEnd, lineMaterial ?? "GizmoDrawLine", ref vctColor, thickness);
                }

                previousStart = vertexStart;
                previousEnd = vertexEnd;
            }
        }

        public static void DrawTransparentPyramid(ref Vector3D start, ref MyQuad backQuad, ref Vector4 vctColor, int divideRatio, float thickness, string lineMaterial = null)
        {
            Vector3 vctZero = Vector3.Zero;
            m_lineBuffer.Clear();
            GenerateLines(start, backQuad.Point0, backQuad.Point1, ref m_lineBuffer, divideRatio);
            GenerateLines(start, backQuad.Point1, backQuad.Point2, ref m_lineBuffer, divideRatio);
            GenerateLines(start, backQuad.Point2, backQuad.Point3, ref m_lineBuffer, divideRatio);
            GenerateLines(start, backQuad.Point3, backQuad.Point0, ref m_lineBuffer, divideRatio);

            foreach (LineD line in m_lineBuffer)
            {
                Vector3 dir = line.To - line.From;
                float len = dir.Length();
                if (len > 0.1f)
                {
                    dir = MyUtils.Normalize(dir);

                    MyTransparentGeometry.AddLineBillboard(lineMaterial ?? "GizmoDrawLine", vctColor, line.From, dir, len, thickness);
                }
            }
        }

        private static void GenerateLines(Vector3D start, Vector3D end1, Vector3D end2, ref List<VRageMath.LineD> lineBuffer, int divideRatio)
        {
            Vector3D dirStep = (end2 - end1) / (double)divideRatio;
            for (int i = 0; i < divideRatio; i++)
            {
                VRageMath.LineD line = new VRageMath.LineD(start, end1 + (double)i * dirStep);
                lineBuffer.Add(line);
            }
        }

        /// <summary>
        /// GenerateLines
        /// </summary>
        /// <param name="vctStart"></param>
        /// <param name="vctEnd"></param>
        /// <param name="vctSideStep"></param>
        /// <param name="worldMatrix"></param>
        /// <param name="lineBuffer"></param>
        /// <param name="divideRatio"></param>
        private static void GenerateLines(Vector3D vctStart, Vector3D vctEnd, ref Vector3D vctSideStep, ref MatrixD worldMatrix, ref List<LineD> lineBuffer, int divideRatio)
        {
            for (int i = 0; i <= divideRatio; ++i)
            {
                Vector3D transformedStart = Vector3D.Transform(vctStart, worldMatrix);
                Vector3D transformedEnd = Vector3D.Transform(vctEnd, worldMatrix);

                if (lineBuffer.Count < lineBuffer.Capacity)
                {
                    LineD line = new LineD(transformedStart, transformedEnd);
                    //@ generate Line
                    lineBuffer.Add(line);

                    vctStart += vctSideStep;
                    vctEnd += vctSideStep;
                }
            }
        }

    }
}
