using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Extensions;
using Sandbox.Game.World;


namespace Sandbox.Game.Gui
{
    /// <summary>
    /// Terminal control for specified block type.
    /// E.g. Torque slider for stator
    /// </summary>
    public abstract class MyTerminalControl<TBlock> : ITerminalControl
        where TBlock : MyTerminalBlock
    {
        public delegate void WriterDelegate(TBlock block, StringBuilder writeTo);

        // TODO: Make it differently, this is crappy
        public static readonly float PREFERRED_CONTROL_WIDTH = 400f / MyGuiConstants.GUI_OPTIMAL_SIZE.X;
        public static readonly MyTerminalBlock[] Empty = new MyTerminalBlock[0];

        public readonly string Id;

        public Func<TBlock, bool> Enabled = (b) => true;
        public Func<TBlock, bool> Visible = (b) => true;

        MyTerminalBlock[] ITerminalControl.TargetBlocks { get; set; }

        private MyGuiControlBase m_control;

        protected ArrayOfTypeEnumerator<MyTerminalBlock, ArrayEnumerator<MyTerminalBlock>, TBlock> TargetBlocks
        {
            get
            {
                return ((ITerminalControl)this).TargetBlocks.OfTypeFast<MyTerminalBlock, TBlock>();
            }
        }

        protected TBlock FirstBlock
        {
            get
            {
                foreach (var item in TargetBlocks)
                {
                    if (item.HasLocalPlayerAccess())
                    {
                        return item;
                    }
                }
                foreach (var item in TargetBlocks)
                {
                    return item;
                }
                return null;
            }
        }

        public MyGuiControlBase GetGuiControl()
        {
            if (m_control == null)
            {
                m_control = CreateGui();
            }
            return m_control;
        }

        public bool SupportsMultipleBlocks { get; set; }

        public MyTerminalControl(string id)
        {
            Id = id;
            SupportsMultipleBlocks = true;
            ((ITerminalControl)this).TargetBlocks = Empty;
        }

        /// <summary>
        /// Called when app needs GUI (not on DS)
        /// </summary>
        protected abstract MyGuiControlBase CreateGui();

        /// <summary>
        /// Called when GUI needs update
        /// </summary>
        protected virtual void OnUpdateVisual()
        {
            m_control.Enabled = false;
            foreach (var item in TargetBlocks)
                m_control.Enabled |= item.HasLocalPlayerAccess() && Enabled(item);
        }

        public void UpdateVisual()
        {
            if (m_control != null)
            {
                OnUpdateVisual();
            }
        }

        bool ITerminalControl.IsVisible(MyTerminalBlock block)
        {
            return Visible((TBlock)block);
        }

        public MyTerminalAction<TBlock>[] Actions { get; protected set; }

        ITerminalAction[] ITerminalControl.Actions { get { return Actions; } }
        string ITerminalControl.Id { get { return Id; } }
    }
}
