using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.World;
using System.Collections.Generic;
using VRage.Input;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    public class MyAlexDebugInputComponent : MyDebugComponent
    {
        public static MyAlexDebugInputComponent Static { get; private set; }

        private static bool ShowDebugDrawTests = false;

        public struct LineInfo
        {
            public LineInfo(Vector3 from, Vector3 to, Color colorFrom, Color colorTo, bool depthRead)
            {
                From = from;
                To = to;
                ColorFrom = colorFrom;
                ColorTo = colorTo;
                DepthRead = depthRead;
            }

            public LineInfo(Vector3 from, Vector3 to, Color colorFrom, bool depthRead)
                : this(from, to, colorFrom, colorFrom, depthRead)
            {
            }

            public Vector3 From;
            public Vector3 To;
            public Color ColorFrom;
            public Color ColorTo;
            public bool DepthRead;
        }

        public MyAlexDebugInputComponent()
        {
            Static = this;
        }

        private List<LineInfo> m_lines = new List<LineInfo>();
        public void AddDebugLine(LineInfo line)
        {
            m_lines.Add(line);
        }

        public override string GetName()
        {
            return "Alex";
        }

        public override bool HandleInput()
        {
            bool handled = false;
            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad0))
            {
                Clear();
            }
            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad1))
            {
                MySession.Static.LocalCharacter.OxygenComponent.SuitOxygenLevel = 0.35f;
            }
            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad2))
            {
                MySession.Static.LocalCharacter.OxygenComponent.SuitOxygenLevel = 0f;
            }
            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad3))
            {
                MySession.Static.LocalCharacter.OxygenComponent.SuitOxygenLevel -= 0.05f;
            }
            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad4))
            {
                MySession.Static.LocalCharacter.SuitBattery.DebugDepleteBattery();
            }
            if (MyInput.Static.IsKeyPress(MyKeys.Control))
            {
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Add))
                {
                    MySession.Static.Settings.SunRotationIntervalMinutes = 1;
                }
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Subtract))
                {
                    MySession.Static.Settings.SunRotationIntervalMinutes = -1;
                }
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Space))
                {
                    MySession.Static.Settings.EnableSunRotation = !MySession.Static.Settings.EnableSunRotation;
                }
            }
            if (MyInput.Static.IsKeyPress(MyKeys.Control))
            {
                if (MyInput.Static.IsNewKeyPressed(MyKeys.D))
                {
                    ShowDebugDrawTests = !ShowDebugDrawTests;
                }
            }

            return handled;
        }

        private void ModifyOxygenBottleAmount(float amount)
        {                                                   
            var inventory = MySession.Static.LocalCharacter.GetInventory();
            var items = inventory.GetItems();
            foreach (var item in items)
            {
                var oxygenContainer = item.Content as MyObjectBuilder_GasContainerObject;
                if (oxygenContainer != null)
                {
                    var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(oxygenContainer) as MyOxygenContainerDefinition;

					if (amount > 0f && oxygenContainer.GasLevel == 1f)
                        continue;

					if (amount < 0f && oxygenContainer.GasLevel == 0f)
                        continue;

					oxygenContainer.GasLevel += amount / physicalItem.Capacity;
					if (oxygenContainer.GasLevel < 0f)
                    {
						oxygenContainer.GasLevel = 0f;
                    }
					if (oxygenContainer.GasLevel > 1f)
                    {
						oxygenContainer.GasLevel = 1f;
                    }
                }
            }
        }

        public void Clear()
        {
            m_lines.Clear();
        }

        public override void Draw()
        {
            base.Draw();
            foreach (var line in m_lines)
            {
                MyRenderProxy.DebugDrawLine3D(line.From, line.To, line.ColorFrom, line.ColorTo, line.DepthRead);
            }

            if (ShowDebugDrawTests)
            {
                Vector3D position = new Vector3D(1000000000.0, 1000000000.0, 1000000000.0);
                MyRenderProxy.DebugDrawLine3D(position, position + Vector3D.Up, Color.Red, Color.Blue, true);
                position += Vector3D.Left;
                MyRenderProxy.DebugDrawLine3D(position, position + Vector3D.Up, Color.Red, Color.Blue, false);

                MyRenderProxy.DebugDrawLine2D(new Vector2(10, 10), new Vector2(50, 50), Color.Red, Color.Blue);

                position += Vector3D.Left;
                MyRenderProxy.DebugDrawPoint(position, Color.White, true);
                position += Vector3D.Left;
                MyRenderProxy.DebugDrawPoint(position, Color.White, false);

                position += Vector3D.Left;
                MyRenderProxy.DebugDrawSphere(position, 0.5f, Color.White, 1.0f, true);

                position += Vector3D.Left;
                MyRenderProxy.DebugDrawAABB(new BoundingBoxD(position - Vector3D.One * 0.5, position + Vector3D.One * 0.5), Color.White, 1.0f, 1.0f, true);

                position += Vector3D.Left;
                //MyRenderProxy.DebugDrawCone(position, Vector3D.Up, Vector3D.One, Color.Yellow, true);
                position += Vector3D.Left;
                MyRenderProxy.DebugDrawAxis(MatrixD.CreateFromTransformScale(Quaternion.Identity, position, Vector3D.One * 0.5), 1.0f, true);
                position += Vector3D.Left;
                MyRenderProxy.DebugDrawOBB(new MyOrientedBoundingBoxD(position, Vector3D.One * 0.5, Quaternion.Identity), Color.White, 1.0f, true, false);
                position += Vector3D.Left;
                MyRenderProxy.DebugDrawCylinder(MatrixD.CreateFromTransformScale(Quaternion.Identity, position, Vector3D.One * 0.5), Color.White, 1.0f, true, true);
                position += Vector3D.Left;
                MyRenderProxy.DebugDrawTriangle(position, position + Vector3D.Up, position + Vector3D.Left, Color.White, true, true);
                position += Vector3D.Left;
                var msg = MyRenderProxy.PrepareDebugDrawTriangles();
                msg.AddTriangle(position, position + Vector3D.Up, position + Vector3D.Left);
                msg.AddTriangle(position, position + Vector3D.Left, position - Vector3D.Up);
                MyRenderProxy.DebugDrawTriangles(msg, MatrixD.Identity, Color.White, true, true);
                position += Vector3D.Left;
                MyRenderProxy.DebugDrawCapsule(position, position + Vector3D.Up, 0.5f, Color.White, true);
                MyRenderProxy.DebugDrawText2D(new Vector2(100, 100), "text", Color.Green, 1.0f);
                position += Vector3D.Left;
                MyRenderProxy.DebugDrawText3D(position, "3D Text", Color.Blue, 1.0f, true);
            }
        }
    }
}