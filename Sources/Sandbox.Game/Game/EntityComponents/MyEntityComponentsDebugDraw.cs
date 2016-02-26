using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace VRage.Game.Components
{
    [PreloadRequired]
    public class MyEntityComponentsDebugDraw
    {
        public static void DebugDraw()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_ENTITY_COMPONENTS && MySector.MainCamera != null)
            {
                double fontSize = 1.5;
                double lineSize = fontSize * 0.045;
                double hoffset = 0.5f;

                Vector3D playerPos = MySector.MainCamera.Position;
                Vector3D upVector = MySector.MainCamera.WorldMatrix.Up;
                Vector3D rightVector = MySector.MainCamera.WorldMatrix.Right;
                Vector3D fwVector = MySector.MainCamera.ForwardVector;

                BoundingSphereD bSphere = new BoundingSphereD(playerPos, 5.0f);
                var entities = MyEntities.GetEntitiesInSphere(ref bSphere);
                Vector3D lastEntityPos = Vector3D.Zero;
                Vector3D offset = Vector3D.Zero;

                var mat = MySector.MainCamera.ViewProjectionMatrix;

                var fullscreenRect = Sandbox.Graphics.MyGuiManager.GetSafeGuiRectangle();
                float aspect = (float)fullscreenRect.Height / fullscreenRect.Width;
                float scaleX = 600;
                float scaleY = scaleX * aspect;

                Vector3D worldAxisPos = playerPos + 1.0f * fwVector;

                // Draw the world-space axis in the middle of the screen
                /*MyRenderProxy.DebugDrawArrow3D(worldAxisPos, worldAxisPos + Vector3D.Right * 0.1f, Color.Red, Color.Red, false, text: "World X");
                MyRenderProxy.DebugDrawArrow3D(worldAxisPos, worldAxisPos + Vector3D.Up * 0.1f, Color.Green, Color.Green, false, text: "World Y");
                MyRenderProxy.DebugDrawArrow3D(worldAxisPos, worldAxisPos + Vector3D.Backward * 0.1f, Color.Blue, Color.Blue, false, text: "World Z");*/

                Vector3D posView = Vector3D.Transform(worldAxisPos, mat);
                Vector3D rtView = Vector3D.Transform(worldAxisPos + Vector3D.Right * 0.1f, mat);
                Vector3D upView = Vector3D.Transform(worldAxisPos + Vector3D.Up * 0.1f, mat);
                Vector3D bwView = Vector3D.Transform(worldAxisPos + Vector3D.Backward * 0.1f, mat);
                var center2D = new Vector2((float)posView.X * scaleX, (float)posView.Y * -scaleY * aspect);
                var right2D = new Vector2((float)rtView.X * scaleX, (float)rtView.Y * -scaleY * aspect) - center2D;
                var up2D = new Vector2((float)upView.X * scaleX, (float)upView.Y * -scaleY * aspect) - center2D;
                var backward2D = new Vector2((float)bwView.X * scaleX, (float)bwView.Y * -scaleY * aspect) - center2D;

                var frameSize = 150.0f;
                var frameBR = Sandbox.Graphics.MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(new Vector2(1.0f, 1.0f));
                var frameBL = frameBR + new Vector2(-frameSize, 0.0f);
                var frameTR = frameBR + new Vector2(0.0f, -frameSize);
                var frameTL = frameBR + new Vector2(-frameSize, -frameSize);
                var frameCenter = (frameBR + frameTL) * 0.5f;

                // Draw frame around the world-space axis
                /*MyRenderProxy.DebugDrawLine2D(frameTL, frameTR, Color.White, Color.White);
                MyRenderProxy.DebugDrawLine2D(frameTR, frameBR, Color.White, Color.White);
                MyRenderProxy.DebugDrawLine2D(frameBR, frameBL, Color.White, Color.White);
                MyRenderProxy.DebugDrawLine2D(frameBL, frameTL, Color.White, Color.White);*/

                // Draw the world-space axis in the corner
                MyRenderProxy.DebugDrawLine2D(frameCenter, frameCenter + right2D, Color.Red, Color.Red);
                MyRenderProxy.DebugDrawLine2D(frameCenter, frameCenter + up2D, Color.Green, Color.Green);
                MyRenderProxy.DebugDrawLine2D(frameCenter, frameCenter + backward2D, Color.Blue, Color.Blue);
                MyRenderProxy.DebugDrawText2D(frameCenter + right2D, "World X", Color.Red, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                MyRenderProxy.DebugDrawText2D(frameCenter + up2D, "World Y", Color.Green, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                MyRenderProxy.DebugDrawText2D(frameCenter + backward2D, "World Z", Color.Blue, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

                MyComponentsDebugInputComponent.DetectedEntities.Clear();

                foreach (var entity in entities)
                {
                    if (entity.PositionComp == null) continue;

                    Vector3D originalPos = entity.PositionComp.GetPosition();
                    Vector3D pos2 = originalPos + upVector * 0.1f;
                    Vector3D pos = pos2 - rightVector * hoffset;
                    Vector3D viewVector = Vector3D.Normalize(originalPos - playerPos);

                    double dot = Vector3D.Dot(viewVector, fwVector);
                    if (dot < 0.9995)
                    {
                        Vector3D rightObject = entity.PositionComp.WorldMatrix.Right * 0.3f;
                        Vector3D upObject = entity.PositionComp.WorldMatrix.Up * 0.3f;
                        Vector3D backwardObject = entity.PositionComp.WorldMatrix.Backward * 0.3f;

                        MyRenderProxy.DebugDrawSphere(originalPos, 0.01f, Color.White, 1.0f, false);
                        MyRenderProxy.DebugDrawArrow3D(originalPos, originalPos + rightObject, Color.Red, Color.Red, false, text: "X");
                        MyRenderProxy.DebugDrawArrow3D(originalPos, originalPos + upObject, Color.Green, Color.Green, false, text: "Y");
                        MyRenderProxy.DebugDrawArrow3D(originalPos, originalPos + backwardObject, Color.Blue, Color.Blue, false, text: "Z");
                        continue;
                    }

                    if (Vector3D.Distance(originalPos, lastEntityPos) < 0.01)
                    {
                        offset += rightVector * 0.3f;
                        upVector = -upVector;
                        pos2 = originalPos + upVector * 0.1f;
                        pos = pos2 - rightVector * hoffset;
                    }
                    lastEntityPos = originalPos;
                    

                    double dist = Vector3D.Distance(pos, playerPos);
                    double textSize = Math.Atan(fontSize / Math.Max(dist, 0.001));

                    float n = 0;
                    {
                        var enumerator = entity.Components.GetEnumerator();
                        MyComponentBase component = null;
                        while (enumerator.MoveNext())
                        {
                            component = enumerator.Current;
                            n += GetComponentLines(component);
                        }
                        n += 1;
                        n -= GetComponentLines(component); // The last component should not make the line longer
                        enumerator.Dispose();
                    }

                    Vector3D topPos = pos + (n + 0.5f) * upVector * lineSize;
                    Vector3D currentPos = pos + (n + 1) * upVector * lineSize + 0.01f * rightVector;

                    MyRenderProxy.DebugDrawLine3D(originalPos, pos2, Color.White, Color.White, false);
                    MyRenderProxy.DebugDrawLine3D(pos, pos2, Color.White, Color.White, false);
                    MyRenderProxy.DebugDrawLine3D(pos, topPos, Color.White, Color.White, false);
                    MyRenderProxy.DebugDrawLine3D(topPos, topPos + rightVector * 1.0f, Color.White, Color.White, false);
                    MyRenderProxy.DebugDrawText3D(currentPos, entity.GetType().ToString() + " - " + entity.DisplayName, Color.Orange, (float)textSize, false, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);

                    MyComponentsDebugInputComponent.DetectedEntities.Add(entity);

                    foreach (var component in entity.Components)
                    {
                        currentPos = pos + n * upVector * lineSize;
                        DebugDrawComponent(component, currentPos, rightVector, upVector, lineSize, (float)textSize);
                        var entityComponent = component as MyEntityComponentBase;
                        string compType = entityComponent == null ? "" : entityComponent.ComponentTypeDebugString;
                        MyRenderProxy.DebugDrawText3D(currentPos - 0.02f * rightVector, compType, Color.Yellow, (float)textSize, false, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
                        n -= GetComponentLines(component);
                    }
                }
                entities.Clear();
            }
        }

        private static int GetComponentLines(MyComponentBase component, bool countAll = true)
        {
            int n = 1;
            if (component is IMyComponentAggregate)
            {
                int count = (component as IMyComponentAggregate).ChildList.Reader.Count;
                int i = 0;
                foreach (var childComponent in (component as IMyComponentAggregate).ChildList.Reader)
                {
                    i++;
                    if (i < count || countAll)
                        n += GetComponentLines(childComponent);
                    else
                        n += 1;
                }
            }
            return n;
        }

        private static void DebugDrawComponent(MyComponentBase component, Vector3D origin, Vector3D rightVector, Vector3D upVector, double lineSize, float textSize)
        {
            Vector3D offset = rightVector * 0.025f;
            Vector3D offsetOrigin = origin + offset * 3.5f;
            Vector3D textOrigin = origin + 2.0f * offset + rightVector * 0.015f;

            MyRenderProxy.DebugDrawLine3D(origin, origin + 2.0f * offset, Color.White, Color.White, false);
            MyRenderProxy.DebugDrawText3D(textOrigin, component.ToString(), Color.White, textSize, false, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);

            if (component is IMyComponentAggregate && (component as IMyComponentAggregate).ChildList.Reader.Count != 0)
            {
                int lines = GetComponentLines(component, false) - 1;
                MyRenderProxy.DebugDrawLine3D(offsetOrigin - 0.5f * lineSize * upVector, offsetOrigin - lines * lineSize * upVector, Color.White, Color.White, false);

                offsetOrigin -= 1 * lineSize * upVector;
                foreach (var childComponent in (component as IMyComponentAggregate).ChildList.Reader)
                {
                    int n = GetComponentLines(childComponent);
                    DebugDrawComponent(childComponent, offsetOrigin, rightVector, upVector, lineSize, textSize);
                    offsetOrigin -= n * lineSize * upVector;
                }
            }
        }
    }
}
