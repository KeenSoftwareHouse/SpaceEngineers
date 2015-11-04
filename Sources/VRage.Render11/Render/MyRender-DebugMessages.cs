using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;

namespace VRageRender
{
    partial class MyRender11
    {
        static void ProcessDebugMessages()
        {
            var linesBatch = MyLinesRenderer.CreateBatch();
            var noDepthLinesBatch = MyLinesRenderer.CreateBatch();
            noDepthLinesBatch.IgnoreDepth = true;
            var lines2D = MyLinesRenderer.CreateBatch();
            lines2D.IgnoreDepth = true;
            

            while (m_debugDrawMessages.Count > 0)
            {
                IMyRenderMessage debugDrawMessage = m_debugDrawMessages.Dequeue();

                MyRenderMessageEnum messageType = debugDrawMessage.MessageType;

                switch (messageType)
                {
                    case MyRenderMessageEnum.DebugDrawLine3D:
                        {
                            MyRenderMessageDebugDrawLine3D message = (MyRenderMessageDebugDrawLine3D)debugDrawMessage;

                            if(message.DepthRead)
                            {
                                linesBatch.Add(message.PointFrom - MyEnvironment.CameraPosition, message.PointTo - MyEnvironment.CameraPosition, message.ColorFrom, message.ColorTo);
                            }
                            else
                            {
                                noDepthLinesBatch.Add(message.PointFrom - MyEnvironment.CameraPosition, message.PointTo - MyEnvironment.CameraPosition, message.ColorFrom, message.ColorTo);
                            }

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawLine2D:
                        {
                            MyRenderMessageDebugDrawLine2D message = (MyRenderMessageDebugDrawLine2D)debugDrawMessage;

                            var matrix = message.Projection ?? Matrix.CreateOrthographicOffCenter(0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y, 0, 0, -1);

                            if (!lines2D.CustomViewProjection.HasValue || (lines2D.CustomViewProjection.HasValue && lines2D.CustomViewProjection.Value != matrix))
                            {
                                lines2D.Commit();
                                lines2D = MyLinesRenderer.CreateBatch();
                                lines2D.IgnoreDepth = true;
                                lines2D.CustomViewProjection = matrix;
                            }

                            var p0 = new Vector3(message.PointFrom.X, message.PointFrom.Y, 0);
                            var p1 = new Vector3(message.PointTo.X, message.PointTo.Y, 0);
                            lines2D.Add(p0, p1, message.ColorFrom, message.ColorTo);   

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawPoint:
                        {
                            MyRenderMessageDebugDrawPoint message = (MyRenderMessageDebugDrawPoint)debugDrawMessage;

                            var batch = message.DepthRead ? linesBatch : noDepthLinesBatch;

                            var scale = 0.125f;

                            var borderDepth = MyRender11.UseComplementaryDepthBuffer ? 0.0f : 1.0f;
                            borderDepth = message.ClipDistance.HasValue ? Vector3.Transform(new Vector3(0, 0, -message.ClipDistance.Value), MyEnvironment.Projection).Z : borderDepth;

                            var clipPosition = Vector3D.Transform(message.Position, MyEnvironment.ViewProjectionAt0);
                            clipPosition.X = clipPosition.X * 0.5f + 0.5f;
                            clipPosition.Y = clipPosition.Y * -0.5f + 0.5f;

                            //Debug.Assert(MyRender11.UseComplementaryDepthBuffer);

                            Vector3 position = (Vector3)(message.Position - MyEnvironment.CameraPosition);

                            bool drawCondition = 
                                MyRender11.UseComplementaryDepthBuffer 
                                ? clipPosition.Z > borderDepth && clipPosition.Z < 1
                                : clipPosition.Z < borderDepth && clipPosition.Z > 0;

                            if (drawCondition)
                            {
                                batch.Add(position + Vector3.UnitX * scale, position - Vector3.UnitX * scale, message.Color);
                                batch.Add(position + Vector3.UnitY * scale, position - Vector3.UnitY * scale, message.Color);
                                batch.Add(position + Vector3.UnitZ * scale, position - Vector3.UnitZ * scale, message.Color);
                            }

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawSphere:
                        {
                            MyRenderMessageDebugDrawSphere message = (MyRenderMessageDebugDrawSphere)debugDrawMessage;

                            var borderDepth = MyRender11.UseComplementaryDepthBuffer ? 0.0f : 1.0f;
                            borderDepth = message.ClipDistance.HasValue ? Vector3.Transform(new Vector3(0, 0, -message.ClipDistance.Value), MyEnvironment.Projection).Z : borderDepth;

                            Vector3D position = message.Position - MyEnvironment.CameraPosition;

                            var clipPosition = Vector3D.Transform(position, MyEnvironment.ViewProjectionAt0);
                            clipPosition.X = clipPosition.X * 0.5f + 0.5f;
                            clipPosition.Y = clipPosition.Y * -0.5f + 0.5f;

                            bool drawCondition =
                                MyRender11.UseComplementaryDepthBuffer
                                ? clipPosition.Z > borderDepth && clipPosition.Z < 1
                                : clipPosition.Z < borderDepth && clipPosition.Z > 0;

                            if (drawCondition)
                            {
                                var batch = message.DepthRead ? linesBatch : noDepthLinesBatch;

                                batch.AddSphereRing(new BoundingSphere(position, message.Radius), message.Color, Matrix.Identity);
                                batch.AddSphereRing(new BoundingSphere(position, message.Radius), message.Color, Matrix.CreateRotationX(MathHelper.PiOver2));
                                batch.AddSphereRing(new BoundingSphere(position, message.Radius), message.Color, Matrix.CreateRotationZ(MathHelper.PiOver2));
                            }

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawAABB:
                        {
                            MyRenderMessageDebugDrawAABB message = (MyRenderMessageDebugDrawAABB)debugDrawMessage;

                            BoundingBoxD aabb = message.AABB;
                            aabb.Translate(-MyEnvironment.CameraPosition);

                            if (message.DepthRead)
                            {
                                linesBatch.AddBoundingBox((BoundingBox)aabb, message.Color);
                            }
                            else
                            {
                                noDepthLinesBatch.AddBoundingBox((BoundingBox)aabb, message.Color);
                            }

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawCone:
                        {
                            MyRenderMessageDebugDrawCone message = (MyRenderMessageDebugDrawCone)debugDrawMessage;

                            var batch = message.DepthRead ? linesBatch : noDepthLinesBatch;

                            var axis = message.DirectionVector;
                            axis.Normalize();

                            var apex = message.Translation + message.DirectionVector;

                            var steps = 32;
                            var stepsRcp = (float)(Math.PI * 2 / steps);
                            for (int i = 0; i < 32; i++)
                            {
                                float a0 = i * stepsRcp;
                                float a1 = (i + 1) * stepsRcp;

                                var A = message.Translation + Vector3D.Transform(message.BaseVector, MatrixD.CreateFromAxisAngle(axis, a0)) - MyEnvironment.CameraPosition;
                                var B = message.Translation + Vector3D.Transform(message.BaseVector, MatrixD.CreateFromAxisAngle(axis, a1)) - MyEnvironment.CameraPosition;

                                batch.Add(A, B, message.Color);
                                batch.Add(A, apex, message.Color);
                            }

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawAxis:
                        {
                            MyRenderMessageDebugDrawAxis message = (MyRenderMessageDebugDrawAxis)debugDrawMessage;

                            var batch = message.DepthRead ? linesBatch : noDepthLinesBatch;

                            Vector3 position = message.Matrix.Translation - MyEnvironment.CameraPosition;

                            batch.Add(position, position + message.Matrix.Right * message.AxisLength, Color.Red);
                            batch.Add(position, position + message.Matrix.Up * message.AxisLength, Color.Green);
                            batch.Add(position, position + message.Matrix.Forward * message.AxisLength, Color.Blue);
                            
                            break;
                        }  

                    case MyRenderMessageEnum.DebugDrawOBB:
                        {
                            MyRenderMessageDebugDrawOBB message = (MyRenderMessageDebugDrawOBB)debugDrawMessage;

                            Vector3D [] cornersD = new Vector3D[8];
                            MatrixD matrix = (MatrixD)message.Matrix;
                            new MyOrientedBoundingBoxD(matrix).GetCorners(cornersD, 0);

                            Vector3[] corners = new Vector3[8];
                            for (int i = 0; i < 8; i++)
                            {
                                corners[i] = (Vector3)(cornersD[i] - MyEnvironment.CameraPosition);
                            }

                            if(message.DepthRead)
                            {
                                linesBatch.Add6FacedConvex(corners, message.Color);
                            }
                            else
                            {
                                noDepthLinesBatch.Add6FacedConvex(corners, message.Color);
                            }

                            MyPrimitivesRenderer.Draw6FacedConvex(corners, message.Color, message.Alpha);
                           
                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawCylinder:
                        {
                            MyRenderMessageDebugDrawCylinder message = (MyRenderMessageDebugDrawCylinder)debugDrawMessage;

                            var steps = 32;
                            var stepsRcp = (float)(Math.PI * 2 / steps);
                            for (int i = 0; i < 32; i++ )
                            {
                                float a0 = i * stepsRcp;
                                float a1 = (i+1) * stepsRcp;

                                Vector3D A = new Vector3D(Math.Cos(a0), 1.0f, Math.Sin(a0)) * 0.5f;
                                Vector3D B = new Vector3D(Math.Cos(a1), 1.0f, Math.Sin(a1)) * 0.5f;
                                Vector3D C = A - Vector3D.UnitY;
                                Vector3D D = B - Vector3D.UnitY;

                                A = Vector3D.Transform(A, message.Matrix);
                                B = Vector3D.Transform(B, message.Matrix);
                                C = Vector3D.Transform(C, message.Matrix);
                                D = Vector3D.Transform(D, message.Matrix);

                                A -= MyEnvironment.CameraPosition;
                                B -= MyEnvironment.CameraPosition;
                                C -= MyEnvironment.CameraPosition;
                                D -= MyEnvironment.CameraPosition;

                                linesBatch.Add(A, B, message.Color);
                                linesBatch.Add(A, C, message.Color);
                                linesBatch.Add(C, D, message.Color);
                            }

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawTriangle:
                        {
                            MyRenderMessageDebugDrawTriangle message = (MyRenderMessageDebugDrawTriangle)debugDrawMessage;

                            MyPrimitivesRenderer.DrawTriangle(message.Vertex0 - MyEnvironment.CameraPosition, message.Vertex1 - MyEnvironment.CameraPosition, message.Vertex2 - MyEnvironment.CameraPosition, message.Color);

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawTriangles:
                        {
                            MyRenderMessageDebugDrawTriangles message = (MyRenderMessageDebugDrawTriangles)debugDrawMessage;

                            for (int i = 0; i < message.Indices.Count; i+=3 )
                            {
                                
                                var v0 = Vector3D.Transform(message.Vertices[message.Indices[i + 0]], message.WorldMatrix) - MyEnvironment.CameraPosition;
                                var v1 = Vector3D.Transform(message.Vertices[message.Indices[i + 1]], message.WorldMatrix) - MyEnvironment.CameraPosition;
                                var v2 = Vector3D.Transform(message.Vertices[message.Indices[i + 2]], message.WorldMatrix) - MyEnvironment.CameraPosition;

                                MyPrimitivesRenderer.DrawTriangle(v0, v1, v2, message.Color);
                            }

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawCapsule:
                        {
                            MyRenderMessageDebugDrawCapsule message = (MyRenderMessageDebugDrawCapsule)debugDrawMessage;

                            var batch = message.DepthRead ? linesBatch : noDepthLinesBatch;

                            batch.AddSphereRing(new BoundingSphere(message.P0 - MyEnvironment.CameraPosition, message.Radius), message.Color, Matrix.Identity);
                            batch.AddSphereRing(new BoundingSphere(message.P0 - MyEnvironment.CameraPosition, message.Radius), message.Color, Matrix.CreateRotationX(MathHelper.PiOver2));
                            batch.AddSphereRing(new BoundingSphere(message.P1 - MyEnvironment.CameraPosition, message.Radius), message.Color, Matrix.Identity);
                            batch.AddSphereRing(new BoundingSphere(message.P1 - MyEnvironment.CameraPosition, message.Radius), message.Color, Matrix.CreateRotationX(MathHelper.PiOver2));

                            batch.Add(message.P0 - MyEnvironment.CameraPosition, message.P1 - MyEnvironment.CameraPosition, message.Color);


                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawText2D:
                        {
                            MyRenderMessageDebugDrawText2D message = (MyRenderMessageDebugDrawText2D)debugDrawMessage;

                            var text = new StringBuilder(message.Text);

                            MySpritesRenderer.DrawText(message.Coord, text, message.Color, message.Scale, message.Align);

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawText3D:
                        {
                            MyRenderMessageDebugDrawText3D message = (MyRenderMessageDebugDrawText3D)debugDrawMessage;

                            Vector3D position = message.Coord;

                            var worldToClip = MyEnvironment.ViewProjectionD;
                            if (message.CustomViewProjection != -1)
                            {
                                if (!MyRenderProxy.BillboardsViewProjectionRead.ContainsKey(message.CustomViewProjection))
                                {
                                    break;
                                }

                                var i = message.CustomViewProjection;

                                var scaleX = MyRenderProxy.BillboardsViewProjectionRead[i].Viewport.Width / (float)MyRender11.ViewportResolution.X;
                                var scaleY = MyRenderProxy.BillboardsViewProjectionRead[i].Viewport.Height / (float)MyRender11.ViewportResolution.Y;
                                var offsetX = MyRenderProxy.BillboardsViewProjectionRead[i].Viewport.OffsetX / (float)MyRender11.ViewportResolution.X;
                                var offsetY = (MyRender11.ViewportResolution.Y - MyRenderProxy.BillboardsViewProjectionRead[i].Viewport.OffsetY - MyRenderProxy.BillboardsViewProjectionRead[i].Viewport.Height)
                                    / (float)MyRender11.ViewportResolution.Y;

                                var viewportTransformation = new Matrix(
                                    scaleX, 0, 0, 0,
                                    0, scaleY, 0, 0,
                                    0, 0, 1, 0,
                                    offsetX, offsetY, 0, 1
                                    );

                                worldToClip = MyRenderProxy.BillboardsViewProjectionRead[message.CustomViewProjection].View * MyRenderProxy.BillboardsViewProjectionRead[message.CustomViewProjection].Projection * viewportTransformation;
                            }

                            var clipPosition = Vector3D.Transform(position, ref worldToClip);
                            clipPosition.X = clipPosition.X * 0.5f + 0.5f;
                            clipPosition.Y = clipPosition.Y * -0.5f + 0.5f;

                            var borderDepth = MyRender11.UseComplementaryDepthBuffer ? 0.0f : 1.0f;
                            borderDepth = message.ClipDistance.HasValue ? Vector3.Transform(new Vector3(0, 0, -message.ClipDistance.Value), MyEnvironment.Projection).Z : borderDepth;

                            bool drawCondition =
                                MyRender11.UseComplementaryDepthBuffer
                                ? clipPosition.Z > borderDepth && clipPosition.Z < 1
                                : clipPosition.Z < borderDepth && clipPosition.Z > 0;

                            if (drawCondition)
                            {
                                MySpritesRenderer.DrawText(new Vector2((float)clipPosition.X, (float)clipPosition.Y) * MyRender11.ViewportResolution,
                                    new StringBuilder(message.Text), message.Color, message.Scale, message.Align);
                            }

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawModel:
                        {
                            MyRenderMessageDebugDrawModel message = (MyRenderMessageDebugDrawModel)debugDrawMessage;


                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawPlane:
                        {
                            MyRenderMessageDebugDrawPlane message = (MyRenderMessageDebugDrawPlane)debugDrawMessage;


                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
            }

            linesBatch.Commit();
            noDepthLinesBatch.Commit();
            lines2D.Commit();
        }
    }
}
