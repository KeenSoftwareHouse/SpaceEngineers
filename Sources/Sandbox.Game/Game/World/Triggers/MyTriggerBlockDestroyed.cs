using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Triggers;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Utils;
using VRage.Library;

namespace Sandbox.Game.World.Triggers
{
    [TriggerType(typeof(MyObjectBuilder_TriggerBlockDestroyed))]
    class MyTriggerBlockDestroyed : MyTrigger,ICloneable
    {    
        public enum BlockState
        {
            Ok = 0,
            Destroyed = 1,
            MessageShown = 2
        }
        private Dictionary<MyTerminalBlock, BlockState> m_blocks = new Dictionary<MyTerminalBlock, BlockState>();
        public Dictionary<MyTerminalBlock, BlockState> Blocks { get { return m_blocks; } private set { m_blocks = value; } }

        public string SingleMessage;

        public MyTriggerBlockDestroyed(){ }

        public MyTriggerBlockDestroyed(MyTriggerBlockDestroyed trg)
            : base(trg) 
        {
            SingleMessage = trg.SingleMessage;
            //needs to be deep copy because we are using this to copy from default template to each user
            m_blocks.Clear();
            foreach(var entry in trg.m_blocks)
                m_blocks.Add(entry.Key,entry.Value);
        }
        public override object Clone()
        {
            MyTriggerBlockDestroyed trigger = new MyTriggerBlockDestroyed(this);
            return trigger;
        }

        public override void DisplayHints(MyPlayer player, MyEntity me)
        {
            foreach (var item in m_blocks)
            {
                if (item.Value == BlockState.MessageShown)
                    continue;
                if (item.Key.SlimBlock.IsDestroyed)// already processed into BlockState.Destroyed on server as update runs before hints
                    m_blocksHelper.Add(item.Key);
            }
            foreach (var block in m_blocksHelper)
            {
                if (SingleMessage != null)
                    MyAPIGateway.Utilities.ShowNotification(string.Format(SingleMessage, block.CustomName), 20000, MyFontEnum.Blue);
                m_blocks[block] = BlockState.MessageShown;
            }
            m_blocksHelper.Clear();
            
            base.DisplayHints(player,me);
        }

        private static List<MyTerminalBlock> m_blocksHelper = new List<MyTerminalBlock>();
        public override bool Update(MyPlayer player, MyEntity me)
        {
            bool isSomethingAlive=false;
            foreach(var item in m_blocks)
            {
                if (item.Value == BlockState.MessageShown)
                    continue;
                if (item.Key.SlimBlock.IsDestroyed)
                {
                    m_blocksHelper.Add(item.Key);
                    continue;
                }
                isSomethingAlive = true;
            }
            if (!isSomethingAlive)
                m_IsTrue = true;
            if (m_blocksHelper.Count>0)
            {
                foreach(var block in m_blocksHelper)
                    m_blocks[block] = BlockState.Destroyed;
                m_blocksHelper.Clear();
            }
            return m_IsTrue;
        }

        private StringBuilder m_progress = new StringBuilder();
        public override StringBuilder GetProgress()
        {
            m_progress.Clear().Append(MyTexts.Get(MySpaceTexts.ScenarioProgressDestroyBlocks));
            foreach (var block in m_blocks)
                if (block.Value == BlockState.Ok)
                    m_progress.Append(MyEnvironment.NewLine).Append("   ").Append(block.Key.CustomName);
            return m_progress;
        }


        //OB:
        public override void Init(MyObjectBuilder_Trigger builder)
        {
            base.Init(builder);
            MyObjectBuilder_TriggerBlockDestroyed ob = (MyObjectBuilder_TriggerBlockDestroyed)builder;
            MyTerminalBlock block;
            foreach (var id in ob.BlockIds)
            {
                if (MyEntities.TryGetEntityById<MyTerminalBlock>(id, out block))
                    m_blocks.Add(block, BlockState.Ok);
                else
                    Debug.Fail("Bad entity ID in MyObjectBuilder_TriggerBlockDestroyed");
            }
            SingleMessage = ob.SingleMessage;
        }
        public override MyObjectBuilder_Trigger GetObjectBuilder()
        {
            MyObjectBuilder_TriggerBlockDestroyed ob = (MyObjectBuilder_TriggerBlockDestroyed)base.GetObjectBuilder();
            ob.BlockIds = new List<long>();
            foreach (var block in m_blocks)
                if (!block.Key.SlimBlock.IsDestroyed)
                    ob.BlockIds.Add(block.Key.EntityId);
            ob.SingleMessage = SingleMessage;
            return ob;
        }

        //GUI
        public override void DisplayGUI()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenTriggerBlockDestroyed(this));
        }
        public new static MyStringId GetCaption()
        {
            return MySpaceTexts.GuiTriggerCaptionBlockDestroyed;
        }
    }
}
