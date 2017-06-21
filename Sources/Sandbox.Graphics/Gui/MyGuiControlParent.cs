using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlParent))]
    public class MyGuiControlParent : MyGuiControlBase, IMyGuiControlsParent
    {
        private MyGuiControls m_controls;

        public MyGuiControlParent() : this(position: null) { }

        public MyGuiControlParent(
            Vector2? position = null,
            Vector2? size = null,
            Vector4? backgroundColor = null,
            String toolTip = null)
            : base( position: position,
                    size: size,
                    colorMask: backgroundColor,
                    toolTip: toolTip,
                    isActiveControl: true,
                    canHaveFocus: true)
        {
            m_controls = new MyGuiControls(this);
        }

        public override void Init(MyObjectBuilder_GuiControlBase builder)
        {
            base.Init(builder);
            
            MyDebug.AssertDebug(builder is MyObjectBuilder_GuiControlParent);
            var ob = builder as MyObjectBuilder_GuiControlParent;
            if (ob.Controls != null)
                m_controls.Init(ob.Controls);
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_GuiControlParent;
            MyDebug.AssertDebug(ob != null);

            ob.Controls = Controls.GetObjectBuilder();

            return ob;
        }

        public MyGuiControls Controls
        {
            get { return m_controls; }
        }

        public override void Clear()
        {
            Controls.Clear();
        }

        #region Overriden methods

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            foreach (MyGuiControlBase control in Controls.GetVisibleControls())
            {
                if (control.GetExclusiveInputHandler() == control)
                {
                    continue;
                }

                if (!(control is MyGuiControlGridDragAndDrop))
                    control.Draw(transitionAlpha * control.Alpha, backgroundTransitionAlpha * control.Alpha);
            }
        }

        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase captured = null;
            
            captured = base.HandleInput();

            foreach (var control in Controls.GetVisibleControls())
            {
                captured = control.HandleInput();
                if (captured != null)
                    break;
            }

            return captured;
        }

        public override MyGuiControlBase GetExclusiveInputHandler()
        {
            var exclusive = GetExclusiveInputHandler(Controls);
            if (exclusive == null)
                exclusive = base.GetExclusiveInputHandler();
            return exclusive;
        }

        //  Returns true or false to let child implementation know if it has to run its own version of draw.
        public override bool IsMouseOverAnyControl()
        {
            //  Update screen controls
            for (int i = Controls.GetVisibleControls().Count - 1; i >= 0; i--)
            {
                if (Controls.GetVisibleControls()[i].IsMouseOver) return true;
            }

            return false;
        }

        // Returns first control, which has mouse over
        public override MyGuiControlBase GetMouseOverControl()
        {
            //  Update screen controls
            for (int i = Controls.GetVisibleControls().Count - 1; i >= 0; i--)
            {
                if (Controls.GetVisibleControls()[i].IsMouseOver)
                    return Controls.GetVisibleControls()[i];
            }

            return null;
        }


        public override MyGuiControlGridDragAndDrop GetDragAndDropHandlingNow()
        {
            for (int i = 0; i < Controls.GetVisibleControls().Count; i++)
            {
                MyGuiControlBase control = Controls.GetVisibleControls()[i];

                if (control is MyGuiControlGridDragAndDrop)
                {
                    MyGuiControlGridDragAndDrop tempDragAndDrop = (MyGuiControlGridDragAndDrop)control;
                    if (tempDragAndDrop.IsActive())
                    {
                        return tempDragAndDrop;
                    }
                }
            }

            //  Not found
            return null;
        }

        public override void HideToolTip()
        {
            foreach (var control in Controls)
            {
                control.HideToolTip();
            }
        }

        public override void ShowToolTip()
        {
            foreach (var control in Controls.GetVisibleControls())
            {
                control.ShowToolTip();
            }
        }

        public override void Update()
        {
            foreach (var control in Controls.GetVisibleControls())
            {
                control.Update();
            }
            base.Update();
        }

        public override void OnRemoving()
        {
            Controls.Clear();
            base.OnRemoving();
        }

        #endregion

        internal override MyGuiControlBase GetFocusControl(bool forwardMovement)
        {
            return GetNextFocusControl(this, forwardMovement);
        }

        public override MyGuiControlBase GetNextFocusControl(MyGuiControlBase currentFocusControl, bool forwardMovement)
        {
            Debug.Assert(currentFocusControl != null);

            var visibleControls = Controls.GetVisibleControls();

            // Pretend that both Controls and Elements are in single array.
            int totalCount = visibleControls.Count + Elements.Count;

            int idxCurrent = visibleControls.IndexOf(currentFocusControl);
            if (idxCurrent == -1)
            {
                idxCurrent = Elements.IndexOf(currentFocusControl);
                if (idxCurrent != -1)
                    idxCurrent += visibleControls.Count;
            }

            if (!forwardMovement && idxCurrent == -1)
                idxCurrent = totalCount;

            // Single loop for both forward and backward movement, but terminating condition would be pain to write in for loop.
            int i = (forwardMovement) ? (idxCurrent + 1)
                                      : (idxCurrent - 1);
            int step = (forwardMovement) ? +1 : -1;
            while((forwardMovement && i < totalCount) ||
                  (!forwardMovement && i >= 0))
            {
                int idx = i;
                if (idx < visibleControls.Count)
                {
                    var visibleControl = visibleControls[idx];
                    if (MyGuiScreenBase.CanHaveFocusRightNow(visibleControl))
                    {
                        if (visibleControl is MyGuiControlParent || !visibleControl.IsActiveControl)
                            return visibleControl.GetFocusControl(forwardMovement);
                        else
                            return visibleControl;
                    }
                }
                else
                {
                    idx -= visibleControls.Count;
                    if (MyGuiScreenBase.CanHaveFocusRightNow(Elements[idx]))
                        return Elements[idx];
                }

                i += step;
            }

            return Owner.GetNextFocusControl(this, forwardMovement);
        }
    }
}
