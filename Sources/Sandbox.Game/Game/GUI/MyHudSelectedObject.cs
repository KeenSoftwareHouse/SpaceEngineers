#region Using

using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System.Diagnostics;
using VRage.Game.Entity.UseObject;
using VRage.Game.Gui;
using VRage.Utils;
using VRageMath;
using System.Collections.Generic;
using VRage.Import;
using VRage.Game.Models;
using VRage.Render.Models;
using VRage.Game.Components;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyHudSelectedObject
    {
        private bool m_visible;
        private int m_visibleRenderID = -1;
        private string m_highlightAttribute;
        internal MyHudSelectedObjectStatus CurrentObject;
        internal MyHudSelectedObjectStatus PreviousObject;
        private Vector2 m_halfSize = Vector2.One * 0.02f;
        private Color m_color = MyHudConstants.HUD_COLOR_LIGHT;

        public MyHudSelectedObject()
        {
        }

        public bool KeepObjectReference
        {
            get;
            set;
        }

        internal MyHudSelectedObjectState State
        {
            get;
            private set;
        }

        public string HighlightAttribute
        {
            get { return m_highlightAttribute; }
            internal set
            {
                if (m_highlightAttribute == value)
                    return;

                CheckForTransition();
                m_highlightAttribute = value;
                CurrentObject.SectionIndices = null;
            }
        }

        public MyHudObjectHighlightStyle HighlightStyle
        {
            get { return CurrentObject.Style; }
            set
            {
                if (CurrentObject.Style == value)
                    return;

                CheckForTransition();
                CurrentObject.Style = value;
            }
        }

        public Vector2 HalfSize
        {
            get { return m_halfSize; }
            set
            {
                if (m_halfSize == value)
                    return;

                CheckForTransition();
                m_halfSize = value;
            }
        }

        public Color Color
        {
            get { return m_color; }
            set
            {
                if (m_color == value)
                    return;

                CheckForTransition();
                m_color = value;
            }
        }

        public bool Visible
        {
            get { return m_visible; }
            internal set
            {
                if (value)
                    m_visibleRenderID = CurrentObject.Instance.RenderObjectID;
                else
                    m_visibleRenderID = -1;

                m_visible = value;
                State = MyHudSelectedObjectState.VisibleStateSet;
            }
        }

        public int VisibleRenderID
        {
            get { return m_visibleRenderID; }
        }

        public IMyUseObject InteractiveObject
        {
            get { return CurrentObject.Instance; }
            internal set
            {
                if (CurrentObject.Instance == value)
                    return;

                CheckForTransition();
                CurrentObject.Instance = value;
            }
        }

        internal int[] SectionIndices
        {
            get
            {
                if (CurrentObject.SectionIndices != null)
                    return CurrentObject.SectionIndices;

                if (m_highlightAttribute == null)
                    return null;

                string[] sections = m_highlightAttribute.Split(MyModelDummy.ATTRIBUTE_HIGHLIGHT_SEPARATOR[0]);
                CurrentObject.SectionIndices = new int[sections.Length];
                MyModel model = CurrentObject.Instance.Owner.Render.GetModel();
                for (int idx = 0; idx < sections.Length; idx++)
                {
                    MyMeshSection section;
                    bool found = model.TryGetMeshSection(sections[idx], out section);
                    if (found)
                    {
                        CurrentObject.SectionIndices[idx] = section.Index;
                    }
                    else
                    {
                        // Returns a sentinel empty array to signal a problem
                        CurrentObject.SectionIndices = new int[0];
                        break;
                    }
                }

                return CurrentObject.SectionIndices;
            }
        }

        internal void Highlight(IMyUseObject obj = null)
        {
            if (obj == null)
                obj = CurrentObject.Instance;

            bool transition = SetObjectInternal(obj);
            if (transition)
                return;

            if (m_visible)
            {
                if (State == MyHudSelectedObjectState.MarkedForNotVisible)
                    State = MyHudSelectedObjectState.VisibleStateSet;
            }
            else
            {
                State = MyHudSelectedObjectState.MarkedForVisible;
            }
        }

        internal void RemoveHighlight()
        {
            if (m_visible)
            {
                State = MyHudSelectedObjectState.MarkedForNotVisible;
            }
            else
            {
                if (State == MyHudSelectedObjectState.MarkedForVisible)
                    State = MyHudSelectedObjectState.VisibleStateSet;
            }
        }

        private bool SetObjectInternal(IMyUseObject obj)
        {
            if (CurrentObject.Instance == obj)
                return false;

            bool transition = CheckForTransition();
            CurrentObject.Instance = obj;
            return transition;
        }

        private bool CheckForTransition()
        {
            if (CurrentObject.Instance == null || !m_visible)
                return false;

            DoTransition();
            return true;
        }

        private void DoTransition()
        {
            PreviousObject = CurrentObject;
            State = MyHudSelectedObjectState.MarkedForVisible;
        }
    }

    public struct MyHudObjectHighlightStyleData
    {
        public string AtlasTexture;
        public MyAtlasTextureCoordinate TextureCoord;
    }

    public enum MyHudObjectHighlightStyle
    {
        /// <summary>
        /// Old block highlight style
        /// </summary>
        HighlightStyle1 = 0,

        /// <summary>
        /// Contour highlight style
        /// </summary>
        HighlightStyle2 = 1
    }

    struct MyHudSelectedObjectStatus
    {
        public IMyUseObject Instance;
        public int[] SectionIndices;
        public MyHudObjectHighlightStyle Style;

        public void Reset()
        {
            Instance = null;
            SectionIndices = null;
            Style = MyHudObjectHighlightStyle.HighlightStyle1;
        }
    }

    enum MyHudSelectedObjectState
    {
        VisibleStateSet = 0,
        MarkedForVisible = 1,
        MarkedForNotVisible = 2
    }
}
