using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
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
                MySession.LocalCharacter.SuitOxygenLevel = 0.35f;
            }
            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad2))
            {
                MySession.LocalCharacter.SuitOxygenLevel = 0f;
            }
            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad3))
            {
                MySession.LocalCharacter.SuitOxygenLevel -= 0.05f;
            }
            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad4))
            {
                MySession.LocalCharacter.SuitBattery.DebugDepleteBattery();
            }

            return handled;
        }

        private void ModifyOxygenBottleAmount(float amount)
        {
            var inventory = MySession.LocalCharacter.GetInventory();
            var items = inventory.GetItems();
            foreach (var item in items)
            {
                var oxygenContainer = item.Content as MyObjectBuilder_OxygenContainerObject;
                if (oxygenContainer != null)
                {
                    var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(oxygenContainer) as MyOxygenContainerDefinition;

                    if (amount > 0f && oxygenContainer.OxygenLevel == 1f)
                        continue;

                    if (amount < 0f && oxygenContainer.OxygenLevel == 0f)
                        continue;

                    oxygenContainer.OxygenLevel += amount / physicalItem.Capacity;
                    if (oxygenContainer.OxygenLevel < 0f)
                    {
                        oxygenContainer.OxygenLevel = 0f;
                    }
                    if (oxygenContainer.OxygenLevel > 1f)
                    {
                        oxygenContainer.OxygenLevel = 1f;
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
        }
    }
}