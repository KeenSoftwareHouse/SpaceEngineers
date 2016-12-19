#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Game.Localization;
using Sandbox.Game.Gui;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;


#endregion

namespace Sandbox.Game.Entities.Cube
{
    /// <summary>
    /// Calculates and draws rotation hints.
    /// </summary>
    public class MyBlockBuilderRotationHints
    {
        private Vector3D[] m_cubeVertices = new Vector3D[8];

        public int RotationRightAxis { get; private set; }
        public int RotationRightDirection { get; private set; }
        public int RotationUpAxis { get; private set; }
        public int RotationUpDirection { get; private set; }
        public int RotationForwardAxis { get; private set; }
        public int RotationForwardDirection { get; private set; }

        private struct BoxEdge
        {
            public int Axis;
            public LineD Edge;
        }
        private List<BoxEdge> m_cubeEdges = new List<BoxEdge>(3);

        private MyBillboardViewProjection m_viewProjection = new MyBillboardViewProjection();

        private MyHudNotification m_mountpointNotification = new MyHudNotification(MySpaceTexts.NotificationHint_CubeDefaultMountpoint);

        public MyBlockBuilderRotationHints()
        {
            Clear();
        }

        private static int GetBestAxis(List<BoxEdge> edgeList, Vector3D fitVector, out int direction)
        {
            double closestDot = double.MaxValue;
            int closestIndex = -1;
            direction = 0;

            for (int i = 0; i < edgeList.Count; i++)
            {
                double dot = Vector3D.Dot(fitVector, edgeList[i].Edge.Direction);
                int edgeDirection = Math.Sign(dot);
                dot = 1.0f - Math.Abs(dot);

                if (dot < closestDot)
                {
                    closestDot = dot;
                    closestIndex = i;
                    direction = edgeDirection;
                }
            }

            int closestAxis = edgeList[closestIndex].Axis;
            edgeList.RemoveAt(closestIndex);

            return closestAxis;
        }

        private static void GetClosestCubeEdge(Vector3D[] vertices, Vector3D cameraPosition, int[] startIndices, int[] endIndices, out int edgeIndex, out int edgeIndex2)
        {
            int startIndex = -1;
            int endIndex = -1;
            edgeIndex = -1;
            edgeIndex2 = -1;
            float closestDistance = float.MaxValue;
            float closestDistance2 = float.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                Vector3D edgeCenter = (vertices[startIndices[i]] + vertices[endIndices[i]]) * 0.5f;
                float distance = (float)Vector3D.Distance(cameraPosition, edgeCenter);

                if (distance < closestDistance)
                {
                    startIndex = startIndices[i];
                    endIndex = endIndices[i];
                    edgeIndex2 = edgeIndex;
                    edgeIndex = i;
                    closestDistance2 = closestDistance;
                    closestDistance = distance;
                }
                else if (distance < closestDistance2)
                {
                    edgeIndex2 = i;
                    closestDistance2 = distance;
                }
            }
        }

        public void Clear()
        {
            RotationRightAxis = -1;
            RotationRightDirection = -1;
            RotationUpAxis = -1;
            RotationUpDirection = -1;
            RotationForwardAxis = -1;
            RotationForwardDirection = -1;
        }

        public void ReleaseRenderData()
        {
            VRageRender.MyRenderProxy.RemoveBillboardViewProjection(0);
        }

        public void CalculateRotationHints(MatrixD drawMatrix, BoundingBoxD worldBox, bool draw, bool fixedAxes = false, bool hideForwardAndUpArrows = false)
        {
            Matrix cameraView = MySector.MainCamera.ViewMatrix;
            MatrixD camWorld = MatrixD.Invert(cameraView);

            camWorld.Translation = drawMatrix.Translation - 6 * camWorld.Forward;
            cameraView = MatrixD.Invert(camWorld);

            m_viewProjection.View = cameraView;


            drawMatrix.Translation -= camWorld.Translation;
            m_viewProjection.CameraPosition = camWorld.Translation;
            camWorld.Translation = Vector3D.Zero;
            Matrix cameraViewAtZero = MatrixD.Transpose(camWorld);


            m_viewProjection.ViewAtZero = cameraViewAtZero;



            Vector2 screenSize = MyGuiManager.GetScreenSizeFromNormalizedSize(Vector2.One);
            float previewRatio = 2.75f;
            int hintsWidth = (int)(screenSize.X / previewRatio), hintsHeight = (int)(screenSize.Y / previewRatio), hintsXOffset = 0, hintsYOffset = 0;

            m_viewProjection.Viewport = new MyViewport(
                (int)MySector.MainCamera.Viewport.Width - hintsWidth - hintsXOffset,
                hintsYOffset,
                hintsWidth,
                hintsHeight);

            m_viewProjection.DepthRead = false;
            m_viewProjection.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, (float)hintsWidth / hintsHeight, 0.1f, 10);




            worldBox = new BoundingBoxD(-new Vector3(MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large) * 0.5f), new Vector3(MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large)) * 0.5f);


            //m_rotationHintsViewProjection.Projection = MySector.MainCamera.ProjectionMatrix;




            int projectionId = 0;
            VRageRender.MyRenderProxy.AddBillboardViewProjection(projectionId, m_viewProjection);

            if (draw)
            {
                var white = Color.White;
                var red = Color.Red;

                MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix, ref worldBox, ref white, ref red, MySimpleObjectRasterizer.Solid, 1, 0.04f, 
                    "SquareFullColor", null, false, projectionId);

                Vector2 hintTextPos = new Vector2((int)(MySector.MainCamera.Viewport.Width - hintsWidth - hintsXOffset + hintsWidth / 2), hintsYOffset + 0.9f * hintsHeight);
                m_mountpointNotification.SetTextFormatArguments(MyInput.Static.GetGameControl(MyControlsSpace.CUBE_DEFAULT_MOUNTPOINT).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard));
                VRageRender.MyRenderProxy.DebugDrawText2D(hintTextPos, m_mountpointNotification.GetText(), Color.White, 0.7f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
            }

            MyOrientedBoundingBoxD rotateHintsBox = new MyOrientedBoundingBoxD(Vector3D.Transform(worldBox.Center, drawMatrix), worldBox.HalfExtents, Quaternion.CreateFromRotationMatrix(drawMatrix));
            //VRageRender.MyRenderProxy.DebugDrawOBB(rotateHintsBox, Vector3.One, 1, false, false);


            rotateHintsBox.GetCorners(m_cubeVertices, 0);

            //for (int vi = 0; vi < 8; vi++)
            //{
            //    VRageRender.MyRenderProxy.DebugDrawText3D(m_cubeVertices[vi], vi.ToString(), Color.White, 0.7f, false);
            //}

            //for (int vi = 0; vi < 4; vi++)
            //{
            //    VRageRender.MyRenderProxy.DebugDrawText3D((m_cubeVertices[MyOrientedBoundingBox.StartXVertices[vi]] + m_cubeVertices[MyOrientedBoundingBox.EndXVertices[vi]]) * 0.5f, vi.ToString(), Color.Red, 0.7f, false);
            //    VRageRender.MyRenderProxy.DebugDrawText3D((m_cubeVertices[MyOrientedBoundingBox.StartYVertices[vi]] + m_cubeVertices[MyOrientedBoundingBox.EndYVertices[vi]]) * 0.5f, vi.ToString(), Color.Green, 0.7f, false);
            //    VRageRender.MyRenderProxy.DebugDrawText3D((m_cubeVertices[MyOrientedBoundingBox.StartZVertices[vi]] + m_cubeVertices[MyOrientedBoundingBox.EndZVertices[vi]]) * 0.5f, vi.ToString(), Color.Blue, 0.7f, false);
            //}

            int closestXAxis, closestXAxis2;
            GetClosestCubeEdge(m_cubeVertices, Vector3D.Zero, MyOrientedBoundingBox.StartXVertices, MyOrientedBoundingBox.EndXVertices, out closestXAxis, out closestXAxis2);
            Vector3D startXVertex = m_cubeVertices[MyOrientedBoundingBox.StartXVertices[closestXAxis]];
            Vector3D endXVertex = m_cubeVertices[MyOrientedBoundingBox.EndXVertices[closestXAxis]];
            Vector3D startXVertex2 = m_cubeVertices[MyOrientedBoundingBox.StartXVertices[closestXAxis2]];
            Vector3D endXVertex2 = m_cubeVertices[MyOrientedBoundingBox.EndXVertices[closestXAxis2]];

            int closestYAxis, closestYAxis2;
            GetClosestCubeEdge(m_cubeVertices, Vector3D.Zero, MyOrientedBoundingBox.StartYVertices, MyOrientedBoundingBox.EndYVertices, out closestYAxis, out closestYAxis2);
            Vector3D startYVertex = m_cubeVertices[MyOrientedBoundingBox.StartYVertices[closestYAxis]];
            Vector3D endYVertex = m_cubeVertices[MyOrientedBoundingBox.EndYVertices[closestYAxis]];
            Vector3D startYVertex2 = m_cubeVertices[MyOrientedBoundingBox.StartYVertices[closestYAxis2]];
            Vector3D endYVertex2 = m_cubeVertices[MyOrientedBoundingBox.EndYVertices[closestYAxis2]];

            int closestZAxis, closestZAxis2;
            GetClosestCubeEdge(m_cubeVertices, Vector3D.Zero, MyOrientedBoundingBox.StartZVertices, MyOrientedBoundingBox.EndZVertices, out closestZAxis, out closestZAxis2);
            Vector3D startZVertex = m_cubeVertices[MyOrientedBoundingBox.StartZVertices[closestZAxis]];
            Vector3D endZVertex = m_cubeVertices[MyOrientedBoundingBox.EndZVertices[closestZAxis]];
            Vector3D startZVertex2 = m_cubeVertices[MyOrientedBoundingBox.StartZVertices[closestZAxis2]];
            Vector3D endZVertex2 = m_cubeVertices[MyOrientedBoundingBox.EndZVertices[closestZAxis2]];

            m_cubeEdges.Clear();
            m_cubeEdges.Add(new BoxEdge() { Axis = 0, Edge = new LineD(startXVertex, endXVertex) });
            m_cubeEdges.Add(new BoxEdge() { Axis = 1, Edge = new LineD(startYVertex, endYVertex) });
            m_cubeEdges.Add(new BoxEdge() { Axis = 2, Edge = new LineD(startZVertex, endZVertex) });

            if (!fixedAxes)
            {
                int rotDirection;

                RotationRightAxis = GetBestAxis(m_cubeEdges, MySector.MainCamera.WorldMatrix.Right, out rotDirection);
                RotationRightDirection = rotDirection;

                RotationUpAxis = GetBestAxis(m_cubeEdges, MySector.MainCamera.WorldMatrix.Up, out rotDirection);
                RotationUpDirection = rotDirection;

                RotationForwardAxis = GetBestAxis(m_cubeEdges, MySector.MainCamera.WorldMatrix.Forward, out rotDirection);
                RotationForwardDirection = rotDirection;
            }

            string rightControlName1 = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard).ToString();
            string rightControlName2 = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard).ToString();
            string upControlName1 = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard).ToString();
            string upControlName2 = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard).ToString();
            string forwControlName1 = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard).ToString();
            string forwControlName2 = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard).ToString();

            if (MyInput.Static.IsJoystickConnected())
            {
                rightControlName1 = MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE).ToString();
                rightControlName2 = MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE).ToString();
                upControlName1 = MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE).ToString();
                upControlName2 = MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE).ToString();
                forwControlName1 = MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE).ToString();
                forwControlName2 = MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_BUILD_MODE, MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE).ToString();
            }

            Vector3D rightStart = Vector3D.Zero;
            Vector3D rightEnd = Vector3D.Zero;
            Vector3D upStart = Vector3D.Zero;
            Vector3D upEnd = Vector3D.Zero;
            Vector3D forwStart = Vector3D.Zero;
            Vector3D forwEnd = Vector3D.Zero;
            Vector3D rightStart2 = Vector3D.Zero;
            Vector3D rightEnd2 = Vector3D.Zero;
            Vector3D upStart2 = Vector3D.Zero;
            Vector3D upEnd2 = Vector3D.Zero;
            Vector3D forwStart2 = Vector3D.Zero;
            Vector3D forwEnd2 = Vector3D.Zero;
            int rightAxis = -1, upAxis = -1, forwAxis = -1;
            int closestRightEdge = -1, closestUpEdge = -1, closestForwEdge = -1;
            int closestRightEdge2 = -1, closestUpEdge2 = -1, closestForwEdge2 = -1;

            if (RotationRightAxis == 0)
            {
                rightStart = startXVertex;
                rightEnd = endXVertex;
                rightStart2 = startXVertex2;
                rightEnd2 = endXVertex2;
                rightAxis = 0;
                closestRightEdge = closestXAxis;
                closestRightEdge2 = closestXAxis2;
            }
            else
                if (RotationRightAxis == 1)
                {
                    rightStart = startYVertex;
                    rightEnd = endYVertex;
                    rightStart2 = startYVertex2;
                    rightEnd2 = endYVertex2;
                    rightAxis = 1;
                    closestRightEdge = closestYAxis;
                    closestRightEdge2 = closestYAxis2;
                }
                else
                    if (RotationRightAxis == 2)
                    {
                        rightStart = startZVertex;
                        rightEnd = endZVertex;
                        rightStart2 = startZVertex2;
                        rightEnd2 = endZVertex2;
                        rightAxis = 2;
                        closestRightEdge = closestZAxis;
                        closestRightEdge2 = closestZAxis2;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Not defined axis");
                    }

            if (RotationUpAxis == 0)
            {
                upStart = startXVertex;
                upEnd = endXVertex;
                upStart2 = startXVertex2;
                upEnd2 = endXVertex2;
                upAxis = 0;
                closestUpEdge = closestXAxis;
                closestUpEdge2 = closestXAxis2;
            }
            else
                if (RotationUpAxis == 1)
                {
                    upStart = startYVertex;
                    upEnd = endYVertex;
                    upStart2 = startYVertex2;
                    upEnd2 = endYVertex2;
                    upAxis = 1;
                    closestUpEdge = closestYAxis;
                    closestUpEdge2 = closestYAxis2;
                }
                else
                    if (RotationUpAxis == 2)
                    {
                        upStart = startZVertex;
                        upEnd = endZVertex;
                        upStart2 = startZVertex2;
                        upEnd2 = endZVertex2;
                        upAxis = 2;
                        closestUpEdge = closestZAxis;
                        closestUpEdge2 = closestZAxis2;
                    }

            if (RotationForwardAxis == 0)
            {
                forwStart = startXVertex;
                forwEnd = endXVertex;
                forwStart2 = startXVertex2;
                forwEnd2 = endXVertex2;
                forwAxis = 0;
                closestForwEdge = closestXAxis;
                closestForwEdge2 = closestXAxis2;
            }
            else
                if (RotationForwardAxis == 1)
                {
                    forwStart = startYVertex;
                    forwEnd = endYVertex;
                    forwStart2 = startYVertex2;
                    forwEnd2 = endYVertex2;
                    forwAxis = 1;
                    closestForwEdge = closestYAxis;
                    closestForwEdge2 = closestYAxis2;
                }
                else
                    if (RotationForwardAxis == 2)
                    {
                        forwStart = startZVertex;
                        forwEnd = endZVertex;
                        forwStart2 = startZVertex2;
                        forwEnd2 = endZVertex2;
                        forwAxis = 2;
                        closestForwEdge = closestZAxis;
                        closestForwEdge2 = closestZAxis2;
                    }

            float textScale = 0.7f;

            //Closest axis
            //VRageRender.MyRenderProxy.DebugDrawLine3D(rightStart, rightEnd, Color.Red, Color.Red, false);
            //VRageRender.MyRenderProxy.DebugDrawLine3D(upStart, upEnd, Color.Green, Color.Green, false);
            //VRageRender.MyRenderProxy.DebugDrawLine3D(forwStart, forwEnd, Color.Blue, Color.Blue, false);


            if (draw)
            {
                //if all axis are visible, all are shown on edges
                //if 1 axis is not visible, other 2 must be shown on faces
                //if 2 are not visible, they are shown on faces and the one is shown on face center
                //Vector3 camVector = Vector3.Normalize(rotateHintsBox.Center - MySector.MainCamera.Position);
                Vector3D camVector = MySector.MainCamera.ForwardVector;
                Vector3D rightDirection = Vector3.Normalize(rightEnd - rightStart);
                Vector3D upDirection = Vector3.Normalize(upEnd - upStart);
                Vector3D forwDirection = Vector3.Normalize(forwEnd - forwStart);
                float dotRight = Math.Abs(Vector3.Dot(camVector, rightDirection));
                float dotUp = Math.Abs(Vector3.Dot(camVector, upDirection));
                float dotForw = Math.Abs(Vector3.Dot(camVector, forwDirection));

                bool drawRightOnFace = false, drawUpOnFace = false, drawForwOnFace = false;
                bool drawRightOnFaceCenter = false, drawUpOnFaceCenter = false, drawForwOnFaceCenter = false;

                float dotAngle = 0.4f;

                if (dotRight < dotAngle)
                {
                    if (dotUp < dotAngle)
                    {
                        drawForwOnFaceCenter = true;
                        drawRightOnFace = true;
                        drawUpOnFace = true;

                        System.Diagnostics.Debug.Assert(dotForw >= dotAngle);
                    }
                    else if (dotForw < dotAngle)
                    {
                        drawUpOnFaceCenter = true;
                        drawRightOnFace = true;
                        drawForwOnFace = true;

                        System.Diagnostics.Debug.Assert(dotUp >= dotAngle);
                    }
                    else
                    {
                        drawUpOnFace = true;
                        drawForwOnFace = true;
                    }
                }
                else
                    if (dotUp < dotAngle)
                    {
                        if (dotRight < dotAngle)
                        {
                            drawForwOnFaceCenter = true;
                            drawRightOnFace = true;
                            drawUpOnFace = true;

                            System.Diagnostics.Debug.Assert(dotForw >= dotAngle);
                        }
                        else if (dotForw < dotAngle)
                        {
                            drawRightOnFaceCenter = true;
                            drawUpOnFace = true;
                            drawForwOnFace = true;

                            System.Diagnostics.Debug.Assert(dotRight >= dotAngle);
                        }
                        else
                        {
                            drawRightOnFace = true;
                            drawForwOnFace = true;
                        }
                    }
                    else
                        if (dotForw < dotAngle)
                        {
                            if (dotRight < dotAngle)
                            {
                                drawUpOnFaceCenter = true;
                                drawRightOnFace = true;
                                drawForwOnFace = true;

                                System.Diagnostics.Debug.Assert(dotUp >= dotAngle);
                            }
                            else if (dotUp < dotAngle)
                            {
                                drawUpOnFaceCenter = true;
                                drawRightOnFace = true;
                                drawForwOnFace = true;

                                System.Diagnostics.Debug.Assert(dotRight >= dotAngle);
                            }
                            else
                            {
                                drawUpOnFace = true;
                                drawRightOnFace = true;
                            }
                        }

                //Draw according to cube visual appearance

                if (!(hideForwardAndUpArrows && RotationRightAxis != 1))
                {
                    if (drawRightOnFaceCenter)
                    {
                        //                VRageRender.MyRenderProxy.DebugDrawSphere((forwStart + forwEnd + forwStart2 + forwEnd2) * 0.25f, 0.2f, Vector3.Right, 1.0f, false, false);
                        Vector3D faceCenter = (forwStart + forwEnd + forwStart2 + forwEnd2) * 0.25f;

                        MyTransparentGeometry.AddBillboardOriented(
                               "ArrowLeftGreen",
                               Vector4.One,
                               faceCenter - RotationForwardDirection * forwDirection * 0.2f - RotationRightDirection * rightDirection * 0.01f,
                               -RotationForwardDirection * forwDirection,
                               -RotationUpDirection * upDirection,
                               0.2f,
                               projectionId);

                        MyTransparentGeometry.AddBillboardOriented(
                               "ArrowRightGreen",
                               Vector4.One,
                               faceCenter + RotationForwardDirection * forwDirection * 0.2f - RotationRightDirection * rightDirection * 0.01f,
                               RotationForwardDirection * forwDirection,
                               RotationUpDirection * upDirection,
                               0.2f,
                               projectionId);

                        VRageRender.MyRenderProxy.DebugDrawText3D(faceCenter - RotationForwardDirection * forwDirection * 0.2f - RotationRightDirection * rightDirection * 0.01f, rightControlName2, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                        VRageRender.MyRenderProxy.DebugDrawText3D(faceCenter + RotationForwardDirection * forwDirection * 0.2f - RotationRightDirection * rightDirection * 0.01f, rightControlName1, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                    }
                    else
                        if (drawRightOnFace)
                        {
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(rightStart2, rightEnd2, Color.Red, Color.Red, false);

                            Vector3 normalRightBack, normalRightForw;
                            MyOrientedBoundingBox.GetNormalBetweenEdges(rightAxis, closestRightEdge, closestRightEdge2, out normalRightForw);
                            Vector3D rightCenter = (rightStart + rightEnd) * 0.5f;
                            Vector3D rightNormalForwWorld = Vector3D.TransformNormal(normalRightForw, drawMatrix);

                            MyOrientedBoundingBox.GetNormalBetweenEdges(rightAxis, closestRightEdge2, closestRightEdge, out normalRightBack);
                            Vector3D rightCenter2 = (rightStart2 + rightEnd2) * 0.5f;
                            Vector3D rightNormalBackWorld = Vector3D.TransformNormal(normalRightBack, drawMatrix);

                            //VRageRender.MyRenderProxy.DebugDrawLine3D(rightCenter, rightCenter + rightNormalForwWorld, Color.Red, Color.Red, false);
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(rightCenter2, rightCenter2 + rightNormalBackWorld, Color.Red, Color.Red, false);

                            int normalEdge = -1;
                            bool opposite = false;
                            if (closestRightEdge == 0 && closestRightEdge2 == 3)
                                normalEdge = closestRightEdge + 1;
                            else
                                if ((closestRightEdge < closestRightEdge2) || (closestRightEdge == 3 && closestRightEdge2 == 0))
                                {
                                    normalEdge = closestRightEdge - 1;
                                    opposite = true;
                                }
                                else
                                    normalEdge = closestRightEdge + 1;

                            if (RotationRightDirection < 0) opposite = !opposite;

                            Vector3 rightOffset;
                            MyOrientedBoundingBox.GetNormalBetweenEdges(rightAxis, closestRightEdge, normalEdge, out rightOffset);
                            Vector3D rightOffsetWorld = Vector3D.TransformNormal(rightOffset, drawMatrix);

                            //VRageRender.MyRenderProxy.DebugDrawText3D(rightCenter + rightNormalForwWorld * 0.6f - rightOffsetWorld * 0.01f, opposite ? rightControlName2 : rightControlName1, Color.White, textScale, false);
                            //VRageRender.MyRenderProxy.DebugDrawText3D(rightCenter2 + rightNormalBackWorld * 0.6f - rightOffsetWorld * 0.01f, opposite ? rightControlName1 : rightControlName2, Color.White, textScale, false);

                            MyTransparentGeometry.AddBillboardOriented(
                               "ArrowGreen",
                               Vector4.One,
                               rightCenter + rightNormalForwWorld * 0.4f - rightOffsetWorld * 0.01f,
                               rightNormalBackWorld,
                               rightDirection,
                               0.5f,
                               projectionId);

                            MyTransparentGeometry.AddBillboardOriented(
                               "ArrowGreen",
                               Vector4.One,
                               rightCenter2 + rightNormalBackWorld * 0.4f - rightOffsetWorld * 0.01f,
                               rightNormalForwWorld,
                               rightDirection,
                               0.5f,
                               projectionId);

                            VRageRender.MyRenderProxy.DebugDrawText3D(rightCenter + rightNormalForwWorld * 0.3f - rightOffsetWorld * 0.01f, opposite ? rightControlName1 : rightControlName2, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                            VRageRender.MyRenderProxy.DebugDrawText3D(rightCenter2 + rightNormalBackWorld * 0.3f - rightOffsetWorld * 0.01f, opposite ? rightControlName2 : rightControlName1, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);

                        }
                        else //draw on edge
                        {
                            Vector3 normalRightBack, normalRightForw;
                            MyOrientedBoundingBox.GetNormalBetweenEdges(rightAxis, closestRightEdge, closestRightEdge + 1, out normalRightForw);
                            MyOrientedBoundingBox.GetNormalBetweenEdges(rightAxis, closestRightEdge, closestRightEdge - 1, out normalRightBack);
                            Vector3D rightCenter = (rightStart + rightEnd) * 0.5f;
                            Vector3D rightNormalForwWorld = Vector3D.TransformNormal(normalRightForw, drawMatrix);
                            Vector3D rightNormalBackWorld = Vector3D.TransformNormal(normalRightBack, drawMatrix);
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(rightCenter, rightCenter + rightNormalForwWorld, Color.Red, Color.Red, false);
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(rightCenter, rightCenter + rightNormalBackWorld, Color.Red, Color.Red, false);

                            MyTransparentGeometry.AddBillboardOriented(
                                "ArrowGreen",
                                Vector4.One,
                                rightCenter + rightNormalForwWorld * 0.3f - rightNormalBackWorld * 0.01f,
                                rightNormalForwWorld,
                                rightDirection,
                                0.5f,
                                projectionId);

                            MyTransparentGeometry.AddBillboardOriented(
                               "ArrowGreen",
                               Vector4.One,
                               rightCenter + rightNormalBackWorld * 0.3f - rightNormalForwWorld * 0.01f,
                               rightNormalBackWorld,
                               rightDirection,
                               0.5f,
                               projectionId);

                            VRageRender.MyRenderProxy.DebugDrawText3D(rightCenter + rightNormalForwWorld * 0.3f - rightNormalBackWorld * 0.01f, RotationRightDirection < 0 ? rightControlName1 : rightControlName2, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                            VRageRender.MyRenderProxy.DebugDrawText3D(rightCenter + rightNormalBackWorld * 0.3f - rightNormalForwWorld * 0.01f, RotationRightDirection < 0 ? rightControlName2 : rightControlName1, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);

                        }
                }

                if (!(hideForwardAndUpArrows && RotationUpAxis != 1))
                {
                    if (drawUpOnFaceCenter)
                    {
                        //VRageRender.MyRenderProxy.DebugDrawSphere((forwStart + forwEnd + forwStart2 + forwEnd2) * 0.25f, 0.2f, Vector3.Up, 1.0f, false, false);

                        Vector3D faceCenter = (forwStart + forwEnd + forwStart2 + forwEnd2) * 0.25f;

                        MyTransparentGeometry.AddBillboardOriented(
                               "ArrowLeftRed",
                               Vector4.One,
                               faceCenter - RotationRightDirection * rightDirection * 0.2f - RotationUpDirection * upDirection * 0.01f,
                               -RotationRightDirection * rightDirection,
                               -RotationForwardDirection * forwDirection,
                               0.2f,
                               projectionId);

                        MyTransparentGeometry.AddBillboardOriented(
                               "ArrowRightRed",
                               Vector4.One,
                               faceCenter + RotationRightDirection * rightDirection * 0.2f - RotationUpDirection * upDirection * 0.01f,
                               RotationRightDirection * rightDirection,
                               RotationForwardDirection * forwDirection,
                               0.2f,
                               projectionId);

                        VRageRender.MyRenderProxy.DebugDrawText3D(faceCenter - RotationRightDirection * rightDirection * 0.2f - RotationUpDirection * upDirection * 0.01f, upControlName1, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                        VRageRender.MyRenderProxy.DebugDrawText3D(faceCenter + RotationRightDirection * rightDirection * 0.2f - RotationUpDirection * upDirection * 0.01f, upControlName2, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                    }
                    else
                        if (drawUpOnFace)
                        {
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(upStart2, upEnd2, Color.Green, Color.Green, false);

                            Vector3 normalUpBack, normalUpForw;
                            MyOrientedBoundingBox.GetNormalBetweenEdges(upAxis, closestUpEdge, closestUpEdge2, out normalUpForw);
                            Vector3D upCenter = (upStart + upEnd) * 0.5f;
                            Vector3 upNormalForwWorld = Vector3.TransformNormal(normalUpForw, drawMatrix);

                            MyOrientedBoundingBox.GetNormalBetweenEdges(upAxis, closestUpEdge2, closestUpEdge, out normalUpBack);
                            Vector3D upCenter2 = (upStart2 + upEnd2) * 0.5f;
                            Vector3 upNormalBackWorld = Vector3.TransformNormal(normalUpBack, drawMatrix);

                            //VRageRender.MyRenderProxy.DebugDrawLine3D(upCenter, upCenter + upNormalForwWorld, Color.Green, Color.Green, false);
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(upCenter2, upCenter2 + upNormalBackWorld, Color.Green, Color.Green, false);

                            int normalEdge = -1;
                            bool opposite = false;
                            if (closestUpEdge == 0 && closestUpEdge2 == 3)
                                normalEdge = closestUpEdge + 1;
                            else
                                if ((closestUpEdge < closestUpEdge2) || (closestUpEdge == 3 && closestUpEdge2 == 0))
                                {
                                    normalEdge = closestUpEdge - 1;
                                    opposite = true;
                                }
                                else
                                    normalEdge = closestUpEdge + 1;

                            if (RotationUpDirection < 0) opposite = !opposite;

                            Vector3 upOffset;
                            MyOrientedBoundingBox.GetNormalBetweenEdges(upAxis, closestUpEdge, normalEdge, out upOffset);
                            Vector3 upOffsetWorld = Vector3.TransformNormal(upOffset, drawMatrix);

                            //VRageRender.MyRenderProxy.DebugDrawText3D(upCenter + upNormalForwWorld * 0.6f - upOffsetWorld * 0.01f, opposite ? upControlName1 : upControlName2, Color.White, textScale, false);
                            //VRageRender.MyRenderProxy.DebugDrawText3D(upCenter2 + upNormalBackWorld * 0.6f - upOffsetWorld * 0.01f, opposite ? upControlName2 : upControlName1, Color.White, textScale, false);

                            MyTransparentGeometry.AddBillboardOriented(
                              "ArrowRed",
                              Vector4.One,
                              upCenter + upNormalForwWorld * 0.4f - upOffsetWorld * 0.01f,
                              upNormalBackWorld,
                              upDirection,
                              0.5f,
                              projectionId);

                            MyTransparentGeometry.AddBillboardOriented(
                               "ArrowRed",
                               Vector4.One,
                               upCenter2 + upNormalBackWorld * 0.4f - upOffsetWorld * 0.01f,
                               upNormalForwWorld,
                               upDirection,
                               0.5f,
                               projectionId);

                            VRageRender.MyRenderProxy.DebugDrawText3D(upCenter + upNormalForwWorld * 0.3f - upOffsetWorld * 0.01f, opposite ? upControlName2 : upControlName1, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                            VRageRender.MyRenderProxy.DebugDrawText3D(upCenter2 + upNormalBackWorld * 0.3f - upOffsetWorld * 0.01f, opposite ? upControlName1 : upControlName2, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);

                        }
                        else //draw on edge
                        {
                            Vector3 normalUpBack, normalUpForw;
                            MyOrientedBoundingBox.GetNormalBetweenEdges(upAxis, closestUpEdge, closestUpEdge + 1, out normalUpForw);
                            MyOrientedBoundingBox.GetNormalBetweenEdges(upAxis, closestUpEdge, closestUpEdge - 1, out normalUpBack);
                            Vector3D upCenter = (upStart + upEnd) * 0.5f;
                            Vector3 upNormalForwWorld = Vector3.TransformNormal(normalUpForw, drawMatrix);
                            Vector3 upNormalBackWorld = Vector3.TransformNormal(normalUpBack, drawMatrix);
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(upCenter, upCenter + upNormalForwWorld, Color.Green, Color.Green, false);
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(upCenter, upCenter + upNormalBackWorld, Color.Green, Color.Green, false);

                            MyTransparentGeometry.AddBillboardOriented(
                                "ArrowRed",
                                Vector4.One,
                                upCenter + upNormalForwWorld * 0.3f - upNormalBackWorld * 0.01f,
                                upNormalForwWorld,
                                upDirection,
                                0.5f,
                                projectionId);

                            MyTransparentGeometry.AddBillboardOriented(
                               "ArrowRed",
                               Vector4.One,
                               upCenter + upNormalBackWorld * 0.3f - upNormalForwWorld * 0.01f,
                               upNormalBackWorld,
                               upDirection,
                               0.5f,
                               projectionId);

                            VRageRender.MyRenderProxy.DebugDrawText3D(upCenter + upNormalForwWorld * 0.6f - upNormalBackWorld * 0.01f, RotationUpDirection > 0 ? upControlName1 : upControlName2, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                            VRageRender.MyRenderProxy.DebugDrawText3D(upCenter + upNormalBackWorld * 0.6f - upNormalForwWorld * 0.01f, RotationUpDirection > 0 ? upControlName2 : upControlName1, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                        }
                }

                if (!(hideForwardAndUpArrows && RotationForwardAxis != 1))
                {
                    if (drawForwOnFaceCenter)
                    {
                        //VRageRender.MyRenderProxy.DebugDrawSphere((rightStart + rightEnd + rightStart2 + rightEnd2) * 0.25f, 0.2f, Vector3.Backward, 1.0f, false, false);

                        Vector3D faceCenter = (rightStart + rightEnd + rightStart2 + rightEnd2) * 0.25f;

                        MyTransparentGeometry.AddBillboardOriented(
                               "ArrowLeftBlue",
                               Vector4.One,
                               faceCenter + RotationUpDirection * upDirection * 0.2f - RotationForwardDirection * forwDirection * 0.01f,
                               RotationUpDirection * upDirection,
                               -RotationRightDirection * rightDirection,
                               0.2f,
                               projectionId);

                        MyTransparentGeometry.AddBillboardOriented(
                               "ArrowRightBlue",
                               Vector4.One,
                               faceCenter - RotationUpDirection * upDirection * 0.2f - RotationForwardDirection * forwDirection * 0.01f,
                               -RotationUpDirection * upDirection,
                               RotationRightDirection * rightDirection,
                               0.2f,
                               projectionId);

                        VRageRender.MyRenderProxy.DebugDrawText3D(faceCenter + RotationUpDirection * upDirection * 0.2f - RotationForwardDirection * forwDirection * 0.01f, forwControlName1, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, projectionId);
                        VRageRender.MyRenderProxy.DebugDrawText3D(faceCenter - RotationUpDirection * upDirection * 0.2f - RotationForwardDirection * forwDirection * 0.01f, forwControlName2, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, projectionId);
                    }
                    else
                        if (drawForwOnFace)
                        {
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(forwStart2, forwEnd2, Color.Blue, Color.Blue, false);

                            Vector3 normalForwBack, normalForwForw;
                            MyOrientedBoundingBox.GetNormalBetweenEdges(forwAxis, closestForwEdge, closestForwEdge2, out normalForwForw);
                            Vector3D forwCenter = (forwStart + forwEnd) * 0.5f;
                            Vector3 forwNormalForwWorld = Vector3.TransformNormal(normalForwForw, drawMatrix);

                            MyOrientedBoundingBox.GetNormalBetweenEdges(forwAxis, closestForwEdge2, closestForwEdge, out normalForwBack);
                            Vector3D forwCenter2 = (forwStart2 + forwEnd2) * 0.5f;
                            Vector3 forwNormalBackWorld = Vector3.TransformNormal(normalForwBack, drawMatrix);

                            //VRageRender.MyRenderProxy.DebugDrawLine3D(forwCenter, forwCenter + forwNormalForwWorld, Color.Blue, Color.Blue, false);
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(forwCenter2, forwCenter2 + forwNormalBackWorld, Color.Blue, Color.Blue, false);

                            int normalEdge = -1;
                            bool opposite = false;
                            if (closestForwEdge == 0 && closestForwEdge2 == 3)
                                normalEdge = closestForwEdge + 1;
                            else
                                if ((closestForwEdge < closestForwEdge2) || (closestForwEdge == 3 && closestForwEdge2 == 0))
                                {
                                    normalEdge = closestForwEdge - 1;
                                    opposite = true;
                                }
                                else
                                    normalEdge = closestForwEdge + 1;

                            if (RotationForwardDirection < 0) opposite = !opposite;

                            Vector3 forwOffset;
                            MyOrientedBoundingBox.GetNormalBetweenEdges(forwAxis, closestForwEdge, normalEdge, out forwOffset);
                            Vector3 forwOffsetWorld = Vector3.TransformNormal(forwOffset, drawMatrix);

                            //VRageRender.MyRenderProxy.DebugDrawText3D(forwCenter + forwNormalForwWorld * 0.6f - forwOffsetWorld * 0.01f, opposite ? forwControlName2 : forwControlName1, Color.White, textScale, false);
                            //VRageRender.MyRenderProxy.DebugDrawText3D(forwCenter2 + forwNormalBackWorld * 0.6f - forwOffsetWorld * 0.01f, opposite ? forwControlName1 : forwControlName2, Color.White, textScale, false);

                            MyTransparentGeometry.AddBillboardOriented(
                               "ArrowBlue",
                               Vector4.One,
                               forwCenter + forwNormalForwWorld * 0.4f - forwOffsetWorld * 0.01f,
                               forwNormalBackWorld,
                               forwDirection,
                               0.5f,
                               projectionId);

                            MyTransparentGeometry.AddBillboardOriented(
                               "ArrowBlue",
                               Vector4.One,
                               forwCenter2 + forwNormalBackWorld * 0.4f - forwOffsetWorld * 0.01f,
                               forwNormalForwWorld,
                               forwDirection,
                               0.5f,
                               projectionId);

                            VRageRender.MyRenderProxy.DebugDrawText3D(forwCenter + forwNormalForwWorld * 0.3f - forwOffsetWorld * 0.01f, opposite ? forwControlName1 : forwControlName2, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                            VRageRender.MyRenderProxy.DebugDrawText3D(forwCenter2 + forwNormalBackWorld * 0.3f - forwOffsetWorld * 0.01f, opposite ? forwControlName2 : forwControlName1, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                        }
                        else //draw on edge
                        {
                            Vector3 normalForwBack, normalForwForw;
                            MyOrientedBoundingBox.GetNormalBetweenEdges(forwAxis, closestForwEdge, closestForwEdge + 1, out normalForwForw);
                            MyOrientedBoundingBox.GetNormalBetweenEdges(forwAxis, closestForwEdge, closestForwEdge - 1, out normalForwBack);
                            Vector3D forwCenter = (forwStart + forwEnd) * 0.5f;
                            Vector3 forwNormalForwWorld = Vector3.TransformNormal(normalForwForw, drawMatrix);
                            Vector3 forwNormalBackWorld = Vector3.TransformNormal(normalForwBack, drawMatrix);
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(forwCenter, forwCenter + forwNormalForwWorld, Color.Blue, Color.Blue, false);
                            //VRageRender.MyRenderProxy.DebugDrawLine3D(forwCenter, forwCenter + forwNormalBackWorld, Color.Blue, Color.Blue, false);

                            MyTransparentGeometry.AddBillboardOriented(
                                "ArrowBlue",
                                Vector4.One,
                                forwCenter + forwNormalForwWorld * 0.3f - forwNormalBackWorld * 0.01f,
                                forwNormalForwWorld,
                                forwDirection,
                                0.5f,
                                projectionId);

                            MyTransparentGeometry.AddBillboardOriented(
                               "ArrowBlue",
                               Vector4.One,
                               forwCenter + forwNormalBackWorld * 0.3f - forwNormalForwWorld * 0.01f,
                               forwNormalBackWorld,
                               forwDirection,
                               0.5f,
                               projectionId);

                            VRageRender.MyRenderProxy.DebugDrawText3D(forwCenter + forwNormalForwWorld * 0.3f - forwNormalBackWorld * 0.01f, RotationForwardDirection < 0 ? forwControlName1 : forwControlName2, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                            VRageRender.MyRenderProxy.DebugDrawText3D(forwCenter + forwNormalBackWorld * 0.3f - forwNormalForwWorld * 0.01f, RotationForwardDirection < 0 ? forwControlName2 : forwControlName1, Color.White, textScale, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, projectionId);
                        }
                }
            }
        }
    }
}
