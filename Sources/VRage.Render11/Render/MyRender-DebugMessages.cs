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
                                linesBatch.Add(message.PointFrom, message.PointTo, message.ColorFrom, message.ColorTo);
                            }
                            else
                            {
                                noDepthLinesBatch.Add(message.PointFrom, message.PointTo, message.ColorFrom, message.ColorTo);
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

                            var clipPosition = Vector3.Transform((Vector3)message.Position, ref MyEnvironment.ViewProjection);
                            clipPosition.X = clipPosition.X * 0.5f + 0.5f;
                            clipPosition.Y = clipPosition.Y * -0.5f + 0.5f;

                            //Debug.Assert(MyRender11.UseComplementaryDepthBuffer);

                            bool drawCondition = 
                                MyRender11.UseComplementaryDepthBuffer 
                                ? clipPosition.Z > borderDepth && clipPosition.Z < 1
                                : clipPosition.Z < borderDepth && clipPosition.Z > 0;

                            if (drawCondition)
                            {
                                batch.Add(message.Position + Vector3.UnitX * scale, message.Position - Vector3.UnitX * scale, message.Color);
                                batch.Add(message.Position + Vector3.UnitY * scale, message.Position - Vector3.UnitY * scale, message.Color);
                                batch.Add(message.Position + Vector3.UnitZ * scale, message.Position - Vector3.UnitZ * scale, message.Color);
                            }

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawSphere:
                        {
                            MyRenderMessageDebugDrawSphere message = (MyRenderMessageDebugDrawSphere)debugDrawMessage;

                            var borderDepth = MyRender11.UseComplementaryDepthBuffer ? 0.0f : 1.0f;
                            borderDepth = message.ClipDistance.HasValue ? Vector3.Transform(new Vector3(0, 0, -message.ClipDistance.Value), MyEnvironment.Projection).Z : borderDepth;

                            var clipPosition = Vector3.Transform((Vector3)message.Position, ref MyEnvironment.ViewProjection);
                            clipPosition.X = clipPosition.X * 0.5f + 0.5f;
                            clipPosition.Y = clipPosition.Y * -0.5f + 0.5f;

                            bool drawCondition =
                                MyRender11.UseComplementaryDepthBuffer
                                ? clipPosition.Z > borderDepth && clipPosition.Z < 1
                                : clipPosition.Z < borderDepth && clipPosition.Z > 0;

                            if (drawCondition)
                            {
                                var batch = message.DepthRead ? linesBatch : noDepthLinesBatch;

                                batch.AddSphereRing(new BoundingSphere(message.Position, message.Radius), message.Color, Matrix.Identity);
                                batch.AddSphereRing(new BoundingSphere(message.Position, message.Radius), message.Color, Matrix.CreateRotationX(MathHelper.PiOver2));
                                batch.AddSphereRing(new BoundingSphere(message.Position, message.Radius), message.Color, Matrix.CreateRotationZ(MathHelper.PiOver2));
                            }

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawAABB:
                        {
                            MyRenderMessageDebugDrawAABB message = (MyRenderMessageDebugDrawAABB)debugDrawMessage;

                            if (message.DepthRead)
                            {
                                linesBatch.AddBoundingBox((BoundingBox)message.AABB, message.Color);
                            }
                            else
                            {
                                noDepthLinesBatch.AddBoundingBox((BoundingBox)message.AABB, message.Color);
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

                                var A = message.Translation + Vector3.Transform(message.BaseVector, Matrix.CreateFromAxisAngle(axis, a0));
                                var B = message.Translation + Vector3.Transform(message.BaseVector, Matrix.CreateFromAxisAngle(axis, a1));

                                batch.Add(A, B, message.Color);
                                batch.Add(A, apex, message.Color);
                            }

                            break;
                        }
                    case MyRenderMessageEnum.DebugDrawAxis:
                        {
                            MyRenderMessageDebugDrawAxis message = (MyRenderMessageDebugDrawAxis)debugDrawMessage;

                            var batch = message.DepthRead ? linesBatch : noDepthLinesBatch;

                            batch.Add(message.Matrix.Translation, message.Matrix.Translation + message.Matrix.Right * message.AxisLength, Color.Red);
                            batch.Add(message.Matrix.Translation, message.Matrix.Translation + message.Matrix.Up * message.AxisLength, Color.Green);
                            batch.Add(message.Matrix.Translation, message.Matrix.Translation + message.Matrix.Forward * message.AxisLength, Color.Blue);
                            
                            break;
                        }  

                    case MyRenderMessageEnum.DebugDrawOBB:
                        {
                            MyRenderMessageDebugDrawOBB message = (MyRenderMessageDebugDrawOBB)debugDrawMessage;

                            Vector3 [] corners = new Vector3[8];
                            Matrix matrix = (Matrix)message.Matrix;
                            new MyOrientedBoundingBox(ref matrix).GetCorners(corners, 0);

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

                                Vector3 A = new Vector3(Math.Cos(a0), 1.0f, Math.Sin(a0)) * 0.5f;
                                Vector3 B = new Vector3(Math.Cos(a1), 1.0f, Math.Sin(a1)) * 0.5f;
                                Vector3 C = A - Vector3.UnitY;
                                Vector3 D = B - Vector3.UnitY;

                                A = Vector3.Transform(A, message.Matrix);
                                B = Vector3.Transform(B, message.Matrix);
                                C = Vector3.Transform(C, message.Matrix);
                                D = Vector3.Transform(D, message.Matrix);

                                linesBatch.Add(A, B, message.Color);
                                linesBatch.Add(A, C, message.Color);
                                linesBatch.Add(C, D, message.Color);
                            }

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawTriangle:
                        {
                            MyRenderMessageDebugDrawTriangle message = (MyRenderMessageDebugDrawTriangle)debugDrawMessage;

                            MyPrimitivesRenderer.DrawTriangle(message.Vertex0, message.Vertex1, message.Vertex2, message.Color);

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawTriangles:
                        {
                            MyRenderMessageDebugDrawTriangles message = (MyRenderMessageDebugDrawTriangles)debugDrawMessage;

                            for (int i = 0; i < message.Indices.Count; i+=3 )
                            {
                                
                                var v0 = Vector3.Transform(message.Vertices[message.Indices[i + 0]], message.WorldMatrix);
                                var v1 = Vector3.Transform(message.Vertices[message.Indices[i + 1]], message.WorldMatrix);
                                var v2 = Vector3.Transform(message.Vertices[message.Indices[i + 2]], message.WorldMatrix);

                                MyPrimitivesRenderer.DrawTriangle(v0, v1, v2, message.Color);
                            }

                            break;
                        }

                    case MyRenderMessageEnum.DebugDrawCapsule:
                        {
                            MyRenderMessageDebugDrawCapsule message = (MyRenderMessageDebugDrawCapsule)debugDrawMessage;

                            var batch = message.DepthRead ? linesBatch : noDepthLinesBatch;

                            batch.AddSphereRing(new BoundingSphere(message.P0, message.Radius), message.Color, Matrix.Identity);
                            batch.AddSphereRing(new BoundingSphere(message.P0, message.Radius), message.Color, Matrix.CreateRotationX(MathHelper.PiOver2));
                            batch.AddSphereRing(new BoundingSphere(message.P1, message.Radius), message.Color, Matrix.Identity);
                            batch.AddSphereRing(new BoundingSphere(message.P1, message.Radius), message.Color, Matrix.CreateRotationX(MathHelper.PiOver2));

                            batch.Add(message.P0, message.P1, message.Color);


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

                            Vector3 position = (Vector3)message.Coord;

                            var worldToClip = MyEnvironment.ViewProjection;
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

                            var clipPosition = Vector3.Transform(position, ref worldToClip);
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
                                MySpritesRenderer.DrawText(new Vector2(clipPosition.X, clipPosition.Y) * MyRender11.ViewportResolution,
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
