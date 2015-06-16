using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Triggers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;


namespace Sandbox.Game.World.Triggers
{
    [TriggerType(typeof(MyObjectBuilder_TriggerPositionLeft))]
    public class MyTriggerPositionLeft : MyTrigger, ICloneable
    {
        public Vector3D TargetPos=new Vector3D(0,0,0);
        protected double m_maxDistance2 = 10000;
        public double Radius { get { return Math.Sqrt(m_maxDistance2); } set { m_maxDistance2 = value * value; } }

        public MyTriggerPositionLeft() { }
        public MyTriggerPositionLeft(MyTriggerPositionLeft pos) : base(pos)
        {
            TargetPos = new Vector3D(pos.TargetPos);
            m_maxDistance2 = pos.m_maxDistance2;
        }
        public override object Clone()
        {
            return new MyTriggerPositionLeft(this);
        }

        public override bool Update(MyEntity me)
        {
            if (Vector3D.DistanceSquared(me.PositionComp.GetPosition(), TargetPos) > m_maxDistance2)
                m_IsTrue = true;
            return IsTrue;
        }

        public override void Init(MyObjectBuilder_Trigger ob)
        {
            base.Init(ob);
            TargetPos = ((MyObjectBuilder_TriggerPositionLeft)ob).Pos;
            m_maxDistance2 = ((MyObjectBuilder_TriggerPositionLeft)ob).Distance2;
        }
        public override MyObjectBuilder_Trigger GetObjectBuilder()
        {
            MyObjectBuilder_TriggerPositionLeft ob = (MyObjectBuilder_TriggerPositionLeft)base.GetObjectBuilder();
            ob.Pos = TargetPos;
            ob.Distance2 = m_maxDistance2;
            return ob;
        }

        //GUI
        public override void DisplayGUI()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenTriggerPositionLeft(this));
        }
        public new static MyStringId GetCaption()
        {
            return MySpaceTexts.GuiTriggerCaptionPositionLeft;
        }

    }
}
