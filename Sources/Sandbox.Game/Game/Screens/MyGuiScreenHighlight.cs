using System;
using System.Collections.Generic;
using Sandbox.Graphics.GUI;
using VRage.Input;
using VRageMath;

namespace Sandbox.Game.Screens
{
    /// <summary>
    /// This screen serves the highlighting purposes. Instantiate it through the static methods only.
    /// Should directly overlay the screen of the referenced controls.
    /// </summary>
    public class MyGuiScreenHighlight : MyGuiScreenBase
    {
        public struct MyHighlightControl
        {
            public MyGuiControlBase Control;
            public int[] Indices;
            public Color? Color;
            public MyToolTips CustomToolTips;
        }
        
        private uint m_closeInFrames = uint.MaxValue;

        private readonly MyGuiControls m_highlightedControls;
        private readonly MyHighlightControl[] m_highlightedControlsData;

        private static readonly Vector2 HIGHLIGHT_TEXTURE_SIZE = new Vector2(   MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.LeftCenter.SizeGui.X + MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.RightCenter.SizeGui.X,
                                                                                MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.CenterTop.SizeGui.Y + MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.CenterBottom.SizeGui.Y);

        private static readonly Vector2 HIGHLIGHT_TEXTURE_OFFSET = new Vector2( MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.LeftCenter.SizeGui.X,
                                                                                MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.CenterTop.SizeGui.Y);

        public override string GetFriendlyName()
        {
            return "HighlightScreen";
        }

        public override int GetTransitionOpeningTime()
        {
            return 500;
        }

        public override int GetTransitionClosingTime()
        {
            return 500;
        }

        /// <summary>
        /// Use this method to highlight the set of provided controls and their tooltips.
        /// </summary>
        /// <param name="controls">Array of controls to be highlighted.</param>
        /// <param name="color">Color of the highlight.</param>
        public static void HighlightControls(MyHighlightControl[] controlsData)
        {
            var screen = new MyGuiScreenHighlight(controlsData);
            MyScreenManager.AddScreen(screen);
        }

        /// <summary>
        /// This this method to highlight a single control and its tooltip.
        /// </summary>
        /// <param name="control">Control to be highlighted.</param>
        /// <param name="color">Color of the highlight.</param>
        public static void HighlightControl(MyHighlightControl control)
        {
            HighlightControls(new[] { control });
        }

        // Private because this should be used only through the static methods.
        private MyGuiScreenHighlight(MyHighlightControl[] controlsData)
            : base(Vector2.Zero, size: Vector2.One * 2.5f)
        {
            m_highlightedControlsData = controlsData;
            m_highlightedControls = new MyGuiControls(this);

            foreach (var controlData in m_highlightedControlsData)
            {
                if (controlData.CustomToolTips != null)
                {
                    controlData.CustomToolTips.Highlight = true;
                    controlData.CustomToolTips.HighlightColor = controlData.Color ?? Color.Yellow;
                }

                m_highlightedControls.AddWeak(controlData.Control);
            }

            m_backgroundColor = Color.Black;
            m_backgroundFadeColor = Color.Black;
            CanBeHidden = false;
            CanHaveFocus = true;
            m_canShareInput = false;
            CanHideOthers = false;
            EnabledBackgroundFade = true;
            DrawMouseCursor = true;
            CloseButtonEnabled = false;
        }

        public override MyGuiControls Controls
        {
            get { return m_highlightedControls; }
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            UniversalInputHandling();

            foreach (var control in m_highlightedControls)
            {
                control.IsMouseOver = MyGuiControlBase.CheckMouseOver(control.Size, control.GetPositionAbsolute(), control.OriginAlign);

                var owner = control.Owner as MyGuiControlBase;
                while (owner != null)
                {
                    owner.IsMouseOver = MyGuiControlBase.CheckMouseOver(owner.Size, owner.GetPositionAbsolute(), owner.OriginAlign);
                    owner = owner.Owner as MyGuiControlBase;
                }

                if (m_closeInFrames == uint.MaxValue && control.IsMouseOver && MyInput.Static.IsNewLeftMousePressed())
                {
                    m_closeInFrames = 10;
                }
            }

            base.HandleInput(receivedFocusInThisUpdate);

            if (m_closeInFrames == 0)
            {
                CloseScreen();
            }
            else if (m_closeInFrames < uint.MaxValue)
            {
                m_closeInFrames--;
            }
        }

        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            UniversalInputHandling();
            base.HandleUnhandledInput(receivedFocusInThisUpdate);
        }

        public override bool Draw()
        {
            foreach (var controlData in m_highlightedControlsData)
            {
                var cgrid = controlData.Control as MyGuiControlGrid;
                if (cgrid != null)
                {
                    if(cgrid.ModalItems == null)
                    {
                        cgrid.ModalItems = new Dictionary<int, Color>();
                    }
                    else
                    {
                        cgrid.ModalItems.Clear();
                    }

                    if (controlData.Indices != null)
                    {
                        foreach (var index in controlData.Indices)
                        {
                            cgrid.ModalItems.Add(index, controlData.Color.HasValue ? controlData.Color.Value : Color.Yellow);
                        }
                    }
                }
            }

            base.Draw();

            foreach (var controlData in m_highlightedControlsData)
            {
                var cgrid = controlData.Control as MyGuiControlGrid;
                if (cgrid != null && cgrid.ModalItems != null)
                {
                    cgrid.ModalItems.Clear();
                }

                foreach (var element in controlData.Control.Elements)
                {
                    var grid = element as MyGuiControlGrid;
                    if (grid != null && grid.ModalItems != null)
                    {
                        grid.ModalItems.Clear();
                    }
                }
            }

          //  return true;

            foreach (var controlData in m_highlightedControlsData)
            {
                if (State == MyGuiScreenState.OPENED && controlData.CustomToolTips != null)
                {
                    // Position the toolip above the controls right top corner
                    var toolTipPosition = controlData.Control.GetPositionAbsoluteTopRight();
                    toolTipPosition.Y -= controlData.CustomToolTips.Size.Y + 0.045f;
                    toolTipPosition.X -= 0.01f;
                    controlData.CustomToolTips.Draw(toolTipPosition);
                }

                if (controlData.Control is MyGuiControlGrid)
                    continue;
                if (controlData.Control is MyGuiControlGridDragAndDrop)
                    continue;

                var control = controlData.Control;
                var size = control.Size + HIGHLIGHT_TEXTURE_SIZE;
                var position = control.GetPositionAbsoluteTopLeft() - HIGHLIGHT_TEXTURE_OFFSET;
                var color = controlData.Color.HasValue ? controlData.Color.Value : Color.Yellow;
                color.A = (byte)(color.A * m_transitionAlpha);

                // 1. Draw highlight box.
                MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.Draw(position, size, color);

                // 2. Draw control.
                control.Draw(m_transitionAlpha, m_backgroundTransition);
            }

            return true;
        }

        // Close the screen when user clicks one of the controls of presses ESC.
        private void UniversalInputHandling()
        {
            if(MyInput.Static.IsNewKeyPressed(MyKeys.Escape))
            {
                CloseScreen();
            }
        }

        public override bool CloseScreen()
        {
            m_highlightedControls.ClearWeaks();

            return base.CloseScreen();
        }
    }
}
