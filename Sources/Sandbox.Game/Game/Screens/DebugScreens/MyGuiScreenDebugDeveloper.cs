using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics.TransparentGeometry.Particles;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Plugins;
using VRage.Utils;
using VRageMath;


namespace Sandbox.Game.Gui
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MyDebugScreenAttribute : System.Attribute
    {
        public readonly string Group;
        public readonly string Name;

        public MyDebugScreenAttribute(string group, string name)
        {
            Group = group;
            Name = name;
        }
    }

    public class MyGuiScreenDebugDeveloper : MyGuiScreenDebugBase
    {
        // Regex to replace member expressions by getter and setter

        // Static:
        // Search: null, MemberHelper.GetMember\(\(\) => ([^\)]*)\)
        // Replace: () => $1, (x) => $1 = x

        // With instance:
        // Search: ([^,]*), MemberHelper.GetMember\(\(\) => ([^\)]*)\)
        // Replace: () => $2, (x) => $2 = x

        static MyGuiScreenBase s_activeScreen;

        static List<MyGuiControlCheckbox> s_groupList = new List<MyGuiControlCheckbox>();
        static List<MyGuiControlCheckbox> s_inputList = new List<MyGuiControlCheckbox>();

        class MyDevelopGroup
        {
            public MyDevelopGroup(string name)
            {
                Name = name;
                ControlList = new List<MyGuiControlBase>();
            }
            public string Name;
            public MyGuiControlBase GroupControl;
            public List<MyGuiControlBase> ControlList;
        };

        //Main groups
        static MyDevelopGroup s_debugDrawGroup = new MyDevelopGroup("Debug draw");
        static MyDevelopGroup s_performanceGroup = new MyDevelopGroup("Performance");
        static List<MyDevelopGroup> s_mainGroups = new List<MyDevelopGroup>()
        {
            s_debugDrawGroup,
            s_performanceGroup,
        };
        static MyDevelopGroup s_activeMainGroup = s_debugDrawGroup;

        //Develop groups
        static MyDevelopGroup s_debugInputGroup = new MyDevelopGroup("Debug Input");
        static MyDevelopGroup s_activeDevelopGroup;
        static Dictionary<string, MyDevelopGroup> s_developGroups = new Dictionary<string, MyDevelopGroup>();
        static Dictionary<string, SortedDictionary<string, Type>> s_developScreenTypes = new Dictionary<string, SortedDictionary<string, Type>>();

        static bool m_profilerEnabled = false;

        static bool EnableProfiler
        {
            get { return VRageRender.Profiler.MyRenderProfiler.ProfilerVisible; }
            set
            {
                if(VRageRender.Profiler.MyRenderProfiler.ProfilerVisible != value)
                {
                    VRageRender.MyRenderProxy.RenderProfilerInput(VRageRender.RenderProfilerCommand.Enable, 0);
                }
            }
        }

        private static void RegisterScreensFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return;

            var baseScreen = typeof(MyGuiScreenBase);
            foreach (var type in assembly.GetTypes())
            {
                if (!baseScreen.IsAssignableFrom(type))
                    continue;

                var attributes = type.GetCustomAttributes(typeof(MyDebugScreenAttribute), false);
                if (attributes.Length == 0)
                    continue;

                var attribute = (MyDebugScreenAttribute)attributes[0];
                SortedDictionary<string, Type> typesInGroup;
                if (!s_developScreenTypes.TryGetValue(attribute.Group, out typesInGroup))
                {
                    typesInGroup = new SortedDictionary<string, Type>();
                    s_developScreenTypes.Add(attribute.Group, typesInGroup);
                    s_developGroups.Add(attribute.Group, new MyDevelopGroup(attribute.Group));
                }

                typesInGroup.Add(attribute.Name, type);
            }
        }

        static MyGuiScreenDebugDeveloper()
        {
            RegisterScreensFromAssembly(Assembly.GetExecutingAssembly());
            RegisterScreensFromAssembly(MyPlugins.GameAssembly);
            RegisterScreensFromAssembly(MyPlugins.SandboxAssembly);
            RegisterScreensFromAssembly(MyPlugins.UserAssembly);

            s_developGroups.Add(s_debugInputGroup.Name, s_debugInputGroup);

            var e = s_developGroups.Values.GetEnumerator();
            e.MoveNext();
            s_activeDevelopGroup = e.Current;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MyGuiScreenDebugDeveloper"/> class.
        /// </summary>
        public MyGuiScreenDebugDeveloper()
            : base(new Vector2(.5f, .5f), new Vector2(0.35f, 1.0f), 0.35f * Color.Yellow.ToVector4(), true)
        {
            // This disable drawing of the background image as well:
            m_backgroundColor = null;

            EnabledBackgroundFade = true;
            m_backgroundFadeColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            foreach (MyDevelopGroup developerGroup in s_developGroups.Values)
            {
                if (developerGroup.ControlList.Count > 0)
                {
                    EnableGroup(developerGroup, false);
                    developerGroup.ControlList.Clear();
                }
            }
            foreach (MyDevelopGroup mainGroup in s_mainGroups)
            {
                if (mainGroup.ControlList.Count > 0)
                {
                    EnableGroup(mainGroup, false);
                    mainGroup.ControlList.Clear();
                }
            }

            float heightOffset = -0.02f;
            AddCaption("Developer screen", captionTextColor: Color.Yellow.ToVector4(), captionOffset: new Vector2(0, heightOffset));

            m_scale = 0.9f;
            m_closeOnEsc = true;

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.03f, 0.1f);
            m_currentPosition.Y += heightOffset;

            // First row of buttons.
            float buttonOffset = 0;
            var buttonSize = new Vector2(0.09f, 0.03f);
            foreach (MyDevelopGroup mainGroup in s_mainGroups)
            {
                var position = new Vector2(-0.03f + m_currentPosition.X + buttonOffset, m_currentPosition.Y);
                mainGroup.GroupControl = new MyGuiControlButton(
                    position: position,
                    colorMask: new Vector4(1, 1, 0.5f, 1),
                    text: new StringBuilder(mainGroup.Name),
                    textScale: MyGuiConstants.DEBUG_BUTTON_TEXT_SCALE * MyGuiConstants.DEBUG_LABEL_TEXT_SCALE * m_scale * 1.2f,
                    onButtonClick: OnClickMainGroup,
                    visualStyle: MyGuiControlButtonStyleEnum.Debug,
                    originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                buttonOffset += mainGroup.GroupControl.Size.X * 1.1f;
                Controls.Add(mainGroup.GroupControl);
            }
            m_currentPosition.Y += buttonSize.Y * 1.1f;

            // Create tabs; each of them has checkboxes starting from the same vertical position.
            // We need to store greatest height as well (for remaining sections).
            float verticalStart = m_currentPosition.Y;
            float verticalMax = verticalStart;
            CreateDebugDrawControls();
            verticalMax = MathHelper.Max(verticalMax, m_currentPosition.Y);
            m_currentPosition.Y = verticalStart;
            CreatePerformanceControls();
            m_currentPosition.Y = MathHelper.Max(verticalMax, m_currentPosition.Y);

            foreach (MyDevelopGroup mainGroup in s_mainGroups)
            {
                EnableGroup(mainGroup, false);
            }

            EnableGroup(s_activeMainGroup, true);

            //Screens
            AddLabel("Debug screens", Color.Yellow.ToVector4(), 1.2f);

            m_currentPosition.Y += 0.02f;

            buttonOffset = 0;
            foreach (MyDevelopGroup developerGroup in s_developGroups.Values)
            {
                var position = new Vector2(-0.03f + m_currentPosition.X + buttonOffset, m_currentPosition.Y);
                developerGroup.GroupControl = new MyGuiControlButton(
                    position: position,
                    colorMask: new Vector4(1, 1, 0.5f, 1),
                    text: new StringBuilder(developerGroup.Name),
                    textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * MyGuiConstants.DEBUG_BUTTON_TEXT_SCALE * m_scale * 1.2f,
                    onButtonClick: OnClickGroup,
                    visualStyle: MyGuiControlButtonStyleEnum.Debug,
                    originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                buttonOffset += developerGroup.GroupControl.Size.X * 1.1f;
                Controls.Add(developerGroup.GroupControl);
            }

            m_currentPosition.Y += buttonSize.Y * 1.1f;

            float groupStartPosition = m_currentPosition.Y;

            foreach (var groupEntry in s_developScreenTypes)
            {
                var group = s_developGroups[groupEntry.Key];
                foreach (var typeEntry in groupEntry.Value)
                {
                    AddGroupBox(typeEntry.Key, typeEntry.Value, group.ControlList);
                }
                m_currentPosition.Y = groupStartPosition;
            }

            if (MyGuiSandbox.Gui is MyDX9Gui)
            {
                for (int i = 0; i < (MyGuiSandbox.Gui as MyDX9Gui).UserDebugInputComponents.Count; ++i)
                {
                    AddGroupInput(string.Format("{0} (Ctrl + numPad{1})", (MyGuiSandbox.Gui as MyDX9Gui).UserDebugInputComponents[i].GetName(), i),
                                  (MyGuiSandbox.Gui as MyDX9Gui).UserDebugInputComponents[i], s_debugInputGroup.ControlList);
                }
            }

            m_currentPosition.Y = groupStartPosition;

            foreach (MyDevelopGroup developerGroup in s_developGroups.Values)
            {
                EnableGroup(developerGroup, false);
            }
            EnableGroup(s_activeDevelopGroup, true);
        }

        //Because of edit and continue
        void CreateDebugDrawControls()
        {
            //Debug draw
            AddCheckBox("Debug draw", null, MemberHelper.GetMember(() => MyDebugDrawSettings.ENABLE_DEBUG_DRAW), true, s_debugDrawGroup.ControlList);
            AddCheckBox("Draw physics", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_PHYSICS), true, s_debugDrawGroup.ControlList);
            AddCheckBox("Audio debug draw", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_AUDIO), true, s_debugDrawGroup.ControlList);
            AddCheckBox("Profiler", () => EnableProfiler, (v) => EnableProfiler = v, true, s_debugDrawGroup.ControlList);
            // AddCheckBox(new StringBuilder("Flatten primitive hierarchy"), null, MemberHelper.GetMember(() => MyPhysicsBody.DebugDrawFlattenHierarchy), true, s_debugDrawGroup.Item2);

            m_currentPosition.Y += 0.01f;
        }

        //Because of edit and continue
        void CreatePerformanceControls()
        {
            AddCheckBox("Particles", null, MemberHelper.GetMember(() => MyParticlesManager.Enabled), true, s_performanceGroup.ControlList);
            m_currentPosition.Y += 0.01f;
        }

        protected void AddGroupInput(String text, MyDebugComponent component, List<MyGuiControlBase> controlGroup = null)
        {
            MyGuiControlCheckbox checkBox = AddCheckBox(text, component, controlGroup);
            s_inputList.Add(checkBox);
        }

        private void AddGroupBox(String text, Type screenType, List<MyGuiControlBase> controlGroup)
        {
            MyGuiControlCheckbox checkBox = AddCheckBox(text, true, null, controlGroup: controlGroup);

            checkBox.IsChecked = s_activeScreen != null && s_activeScreen.GetType() == screenType;
            checkBox.UserData = screenType;

            s_groupList.Add(checkBox);

            checkBox.IsCheckedChanged += delegate(MyGuiControlCheckbox sender)
            {
                var senderScreenType = sender.UserData as Type;
                if (sender.IsChecked)
                {
                    foreach (MyGuiControlCheckbox chb in s_groupList)
                    {
                        if (chb != sender)
                        {
                            chb.IsChecked = false;
                        }
                    }
                    var newScreen = (MyGuiScreenBase)Activator.CreateInstance(senderScreenType);
                    newScreen.Closed += (source) =>
                    {
                        if (source == s_activeScreen) s_activeScreen = null;
                    };
                    MyGuiSandbox.AddScreen(newScreen);
                    s_activeScreen = newScreen;
                }
                else if (s_activeScreen != null && s_activeScreen.GetType() == senderScreenType)
                {
                    s_activeScreen.CloseScreen();
                }
            };
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugDeveloper";
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.F12))
            {
                this.CloseScreen();
            }
        }

        void OnClickGroup(MyGuiControlButton sender)
        {
            EnableGroup(s_activeDevelopGroup, false);

            foreach (MyDevelopGroup developerGroup in s_developGroups.Values)
            {
                if (developerGroup.GroupControl == sender)
                {
                    s_activeDevelopGroup = developerGroup;
                    break;
                }
            }

            EnableGroup(s_activeDevelopGroup, true);
        }

        void OnClickMainGroup(MyGuiControlButton sender)
        {
            EnableGroup(s_activeMainGroup, false);

            foreach (MyDevelopGroup mainGroup in s_mainGroups)
            {
                if (mainGroup.GroupControl == sender)
                {
                    s_activeMainGroup = mainGroup;
                    break;
                }
            }

            EnableGroup(s_activeMainGroup, true);
        }

        void EnableGroup(MyDevelopGroup group, bool enable)
        {
            foreach (var control in group.ControlList)
                control.Visible = enable;
        }
    }
}
