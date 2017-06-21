using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{

    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlTabPage))]
    public class MyGuiControlTabPage : MyGuiControlParent
    {
        private MyStringId m_textEnum;
        private StringBuilder m_text;
        private int m_pageKey;

        public float TextScale;

        public int PageKey
        {
            get { return m_pageKey; }
        }
        public MyStringId TextEnum
        {
            get { return m_textEnum; }
            set
            {
                m_textEnum = value;
                m_text = MyTexts.Get(m_textEnum);
            }
        }
        public StringBuilder Text
        {
            get { return m_text; }
            set { m_text = value; }
        }

        public MyGuiControlTabPage() : this(pageKey: 0) { }

        public MyGuiControlTabPage(
            int pageKey,
            Vector2? position = null,
            Vector2? size = null,
            Vector4? color = null,
            float captionTextScale = 1.0f)
            : base( position: position,
                    size: size,
                    backgroundColor: color)
        {
            Name = "TabPage";
            m_pageKey = pageKey;
            TextScale = captionTextScale;
        }

        public override void Init(MyObjectBuilder_GuiControlBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_GuiControlTabPage;
            MyDebug.AssertDebug(ob != null);

            m_pageKey = ob.PageKey;
            TextEnum  = MyStringId.GetOrCompute(ob.TextEnum);
            TextScale = ob.TextScale;
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_GuiControlTabPage;
            MyDebug.AssertDebug(ob != null);

            ob.PageKey   = PageKey;
            ob.TextEnum  = TextEnum.ToString();
            ob.TextScale = TextScale;
            return ob;
        }
    }

    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlTabControl))]
    public class MyGuiControlTabControl : MyGuiControlParent
    {
        public event Action OnPageChanged;

        Dictionary<int, MyGuiControlTabPage> m_pages = new Dictionary<int, MyGuiControlTabPage>();

        string m_selectedTexture;
        string m_unSelectedTexture;

        private int m_selectedPage;
        public int SelectedPage
        {
            get { return m_selectedPage; }
            set
            {
                if (m_pages.ContainsKey(m_selectedPage))
                    m_pages[m_selectedPage].Visible = false;
                m_selectedPage = value;
                if ( OnPageChanged != null)
                    OnPageChanged();
                if (m_pages.ContainsKey(m_selectedPage))
                    m_pages[m_selectedPage].Visible = true;
            }
        }

        private Vector2 m_tabButtonSize;
        public Vector2 TabButtonSize
        {
            get { return m_tabButtonSize; }
            private set { m_tabButtonSize = value; }
        }

        private float m_tabButtonScale = 1.0f;
        public float TabButtonScale
        {
            get { return m_tabButtonScale; }
            set 
            { 
                m_tabButtonScale = value; 
                RefreshInternals(); 
            }
        }

        public MyGuiControlTabControl() :
            this(null)
        { }

        public MyGuiControlTabControl(
            Vector2? position  = null,
            Vector2? size      = null,
            Vector4? colorMask = null)
            : base( position: position,
                    size: size,
                    backgroundColor: colorMask)
        {
            RefreshInternals();
        }

        public override void Init(MyObjectBuilder_GuiControlBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_GuiControlTabControl)builder;

            RecreatePages();

            HideTabs();
            SelectedPage = 0;
        }

        public void RecreatePages()
        {
            m_pages.Clear();
            foreach (var control in Controls)
            {
                var page = (MyGuiControlTabPage)control;
                page.Visible = false;
                m_pages.Add(page.PageKey, page);
            }
            RefreshInternals();
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var ob = (MyObjectBuilder_GuiControlTabControl)base.GetObjectBuilder();

            return ob;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            //draw buttons on head with texts
            var normalTexture = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL;
            var highlightTexture = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_HIGHLIGHT;

            int count = m_pages.Count;
            int pos = 0;
            var currentPos = GetPositionAbsoluteTopLeft();

            foreach (int i in m_pages.Keys)
            {
                bool isHighlight = (GetMouseOverTab() == i) || (SelectedPage == i);

                var currentTab = GetTabSubControl(i);

                bool isEnabled = Enabled && currentTab.Enabled;
                bool isNotImplementedForbidenOrDisabled = !isEnabled;

                var colorMaskModified = ApplyColorMaskModifiers(ColorMask, isEnabled, transitionAlpha);
                var texture           = (isEnabled && isHighlight) ? highlightTexture : normalTexture;
                var font              = (isEnabled && isHighlight) ? MyFontEnum.White : MyFontEnum.Blue;

                // Draw background texture
                texture.Draw(currentPos, TabButtonSize, ApplyColorMaskModifiers(ColorMask, isEnabled, transitionAlpha), m_tabButtonScale);
                StringBuilder text = currentTab.Text;
                if (text != null)
                {
                    Vector2 textPosition;
                    MyGuiDrawAlignEnum textDrawAlign;

                    textPosition = currentPos + TabButtonSize / 2;
                    textDrawAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;

                    MyGuiManager.DrawString(font, text, textPosition, currentTab.TextScale, colorMaskModified, textDrawAlign);
                }

                currentPos.X += TabButtonSize.X;
                pos++;
            }

            base.Draw(transitionAlpha, backgroundTransitionAlpha);
        }
      
        public override MyGuiControlBase HandleInput()
        {
            int tab = GetMouseOverTab();
            if (tab != -1 && GetTabSubControl(tab).Enabled && MyInput.Static.IsNewPrimaryButtonPressed())
            {
                MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                SelectedPage = tab;
                return this;
            }

            return base.HandleInput();
        }

        public override void ShowToolTip()
        {
            var mouseOverTabKey = GetMouseOverTab();
            foreach (var entry in m_pages)
            {
                if (entry.Key == mouseOverTabKey && entry.Value.m_toolTip != null)
                {
                    entry.Value.m_toolTip.Draw(MyGuiManager.MouseCursorPosition);
                    return;
                }
            }

            base.ShowToolTip();
        }

        public MyGuiControlTabPage GetTabSubControl(int key)
        {
            if (!m_pages.ContainsKey(key))
            {
                m_pages[key] = new MyGuiControlTabPage(
                    position: TabPosition,
                    size: TabSize,
                    color: ColorMask,
                    pageKey: key) {
                    Visible = false,
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
                };
                Controls.Add(m_pages[key]);
            }
            return m_pages[key];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Mouse over tab index or -1 when none of them.</returns>
        private int GetMouseOverTab()
        {
            int count = m_pages.Keys.Count;
            int pos = 0;
            var currentPos = GetPositionAbsoluteTopLeft();

            foreach (int i in m_pages.Keys)
            {
                Vector2 min = currentPos;
                Vector2 max = min + TabButtonSize;

                if ((MyGuiManager.MouseCursorPosition.X >= min.X) &&
                    (MyGuiManager.MouseCursorPosition.X <= max.X) &&
                    (MyGuiManager.MouseCursorPosition.Y >= min.Y) &&
                    (MyGuiManager.MouseCursorPosition.Y <= max.Y))
                    return i;

                currentPos.X += TabButtonSize.X;
                pos++;
            }
            return -1;
        }

        private void RefreshInternals()
        {
            var buttonTextureSize = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL.MinSizeGui;
            buttonTextureSize *= m_tabButtonScale;
            TabButtonSize = new Vector2(Math.Min(Size.X / m_pages.Count, buttonTextureSize.X),
                                        buttonTextureSize.Y);
            TabPosition = Size * -0.5f + new Vector2(0, TabButtonSize.Y);
            TabSize = Size - new Vector2(0, TabButtonSize.Y);
            RefreshPageParameters();
        }

        private void RefreshPageParameters()
        {
            //this.BorderEnabled = true;
            foreach (var pages in m_pages.Values)
            {
                pages.Position    = TabPosition;
                pages.Size        = TabSize;
                pages.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            }
        }

        private void HideTabs()
        {
            foreach (var entry in m_pages)
                entry.Value.Visible = false;
        }


        public Vector2 TabPosition { get; private set; }
        public Vector2 TabSize { get; private set; }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            RefreshInternals();
        }

        public void MoveToNextTab()
        {
            if (m_pages.Count == 0)
                return;

            int currentPage = SelectedPage;
            int nextPage = int.MaxValue;
            int firstPage = int.MaxValue;

            foreach (var pair in m_pages)
            {
                firstPage = Math.Min(firstPage, pair.Key);
                if (pair.Key > currentPage && pair.Key < nextPage)
                    nextPage = pair.Key;
            }

            SelectedPage = nextPage != int.MaxValue ? nextPage : firstPage;
        }

        public void MoveToPreviousTab()
        {
            if (m_pages.Count == 0)
                return;

            int currentPage = SelectedPage;
            int previousPage = int.MinValue;
            int lastPage = int.MinValue;

            foreach (var pair in m_pages)
            {
                lastPage = Math.Max(lastPage, pair.Key);
                if (pair.Key < currentPage && pair.Key > previousPage)
                    previousPage = pair.Key;
            }

            SelectedPage = previousPage != int.MinValue ? previousPage : lastPage;
        }

        public override MyGuiControlGridDragAndDrop GetDragAndDropHandlingNow()
        {
            if (m_selectedPage > -1)
                return m_pages[m_selectedPage].GetDragAndDropHandlingNow();

            return null;
        }
    }
}

