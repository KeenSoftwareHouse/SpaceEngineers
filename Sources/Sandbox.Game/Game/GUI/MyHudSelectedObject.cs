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
using VRage.Game.Components;
using System;
using VRage.Game.Entity;
using Sandbox.Game.Entities.Cube;
using VRageRender.Import;
using VRageRender.Models;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyHudSelectedObject
    {
        [ThreadStatic]
        private static List<string> m_tmpSectionNames = new List<string>();

        [ThreadStatic]
        private static List<uint> m_tmpSubpartIds = new List<uint>();

        private bool m_highlightAttributeDirty;
        private bool m_visible;
        private int m_visibleRenderID = -1;
        private string m_highlightAttribute;
        internal MyHudSelectedObjectStatus CurrentObject;
        internal MyHudSelectedObjectStatus PreviousObject;
        private Vector2 m_halfSize = Vector2.One * 0.02f;
        private Color m_color = MyHudConstants.HUD_COLOR_LIGHT;
        private MyHudObjectHighlightStyle m_style;

        public MyHudSelectedObject() { }

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
                CurrentObject.SectionNames = null;
                CurrentObject.SubpartIndices = null;
                if (value != null)
                    m_highlightAttributeDirty = true;
            }
        }

        public MyHudObjectHighlightStyle HighlightStyle
        {
            get { return m_style; }
            set
            {
                if (m_style == value)
                    return;

                CheckForTransition();
                m_style = value;
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

                if (value)
                    CurrentObject.Style = m_style;
                else
                    CurrentObject.Style = MyHudObjectHighlightStyle.None;

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
        }

        internal uint[] SubpartIndices
        {
            get
            {
                ComputeHighlightIndices();
                return CurrentObject.SubpartIndices;
            }
        }

        internal string[] SectionNames
        {
            get
            {
                ComputeHighlightIndices();
                return CurrentObject.SectionNames;
            }
        }

        private void ComputeHighlightIndices()
        {
            if (!m_highlightAttributeDirty)
                return;

            if (m_highlightAttribute == null)
            {
                m_highlightAttributeDirty = false;
                return;
            }

            m_tmpSectionNames.Clear();
            m_tmpSubpartIds.Clear();
            string[] names = m_highlightAttribute.Split(MyModelDummy.ATTRIBUTE_HIGHLIGHT_SEPARATOR[0]);
            MyModel model = CurrentObject.Instance.Owner.Render.GetModel();
            bool found = true;
            for (int idx = 0; idx < names.Length; idx++)
            {
                string name = names[idx];
                if (name.StartsWith(MyModelDummy.SUBPART_PREFIX))
                {
                    string subpartName = name.Substring(MyModelDummy.SUBPART_PREFIX.Length);
                    MyEntitySubpart subpart;
                    found = CurrentObject.Instance.Owner.TryGetSubpart(subpartName, out subpart);
                    if (!found)
                        break;

                    int id = subpart.Render.GetRenderObjectID();
                    if (id != -1)
                        m_tmpSubpartIds.Add((uint)id);
                } else if (name.StartsWith(MyModelDummy.SUBBLOCK_PREFIX))
                {
                    // Subblocks are very similar in subparts as purpouse but they can be
                    // fully fledged blocks
                    MyCubeBlock cubeBlock = CurrentObject.Instance.Owner as MyCubeBlock;
                    if (cubeBlock == null)
                        break;

                    string subblockName = name.Substring(MyModelDummy.SUBBLOCK_PREFIX.Length);
                    MySlimBlock subblock;
                    found = cubeBlock.TryGetSubBlock(subblockName, out subblock);
                    if (!found)
                        break;

                    int id = subblock.FatBlock.Render.GetRenderObjectID();
                    if (id != -1)
                        m_tmpSubpartIds.Add((uint)id);
                }
                else
                {
                    MyMeshSection section;
                    found = model.TryGetMeshSection(names[idx], out section);
                    if (!found)
                        break;

                    m_tmpSectionNames.Add(section.Name);
                }
            }

            if (found)
            {
                CurrentObject.SectionNames = m_tmpSectionNames.ToArray();
                if (m_tmpSubpartIds.Count != 0)
                    CurrentObject.SubpartIndices = m_tmpSubpartIds.ToArray();
            }
            else
            {
                // Clear the lists to signal a problem
                CurrentObject.SectionNames = new string[0];
                CurrentObject.SubpartIndices = null;
            }

            m_highlightAttributeDirty = false;
        }

        internal void Highlight(IMyUseObject obj)
        {
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

        internal void ResetCurrent()
        {
            CurrentObject.Reset();
            m_highlightAttributeDirty = true;
        }

        /// <returns>Abort</returns>
        private bool SetObjectInternal(IMyUseObject obj)
        {
            if (CurrentObject.Instance == obj)
                return false;

            bool transition = CheckForTransition();
            ResetCurrent();
            CurrentObject.Instance = obj;
            return transition;
        }

        private bool CheckForTransition()
        {
            if (CurrentObject.Instance == null || !m_visible)
                return false;

            if (PreviousObject.Instance != null)
                return true;

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
        None = 0,

        /// <summary>
        /// Old block highlight style
        /// </summary>
        DummyHighlight = 1,

        /// <summary>
        /// Contour highlight style
        /// </summary>
        OutlineHighlight = 2
    }

    struct MyHudSelectedObjectStatus
    {
        public IMyUseObject Instance;
        public string[] SectionNames;
        public int InstanceId;
        public uint[] SubpartIndices;

        public MyHudObjectHighlightStyle Style;

        public void Reset()
        {
            Instance = null;
            SectionNames = null;
            InstanceId = -1;
            SubpartIndices = null;
            Style = MyHudObjectHighlightStyle.None;
        }
    }

    enum MyHudSelectedObjectState
    {
        VisibleStateSet = 0,
        MarkedForVisible = 1,
        MarkedForNotVisible = 2
    }
}
