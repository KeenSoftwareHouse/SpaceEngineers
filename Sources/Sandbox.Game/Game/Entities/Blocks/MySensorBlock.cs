using Havok;
using ProtoBuf;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using VRage;
using VRage.Utils;
using VRage.Trace;
using VRageMath;
using Sandbox.Game.Screens.Terminal.Controls;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    [Flags]
    public enum MySensorFilterFlags : ushort
    {
        Players           = 1 << 0,
        FloatingObjects   = 1 << 1,
        SmallShips        = 1 << 2,
        LargeShips        = 1 << 3,
        Stations          = 1 << 4,
        Asteroids         = 1 << 5,

        Owner             = 1 << 8,
        Friendly          = 1 << 9,
        Neutral           = 1 << 10,
        Enemy             = 1 << 11,
    }


    [MyCubeBlockType(typeof(MyObjectBuilder_SensorBlock))]
    class MySensorBlock : MyFunctionalBlock, IMyPowerConsumer, Sandbox.ModAPI.IMySensorBlock, IMyGizmoDrawableObject
    {
        private new MySensorBlockDefinition BlockDefinition
        {
            get { return (MySensorBlockDefinition)base.BlockDefinition; }
        }

        private Color m_gizmoColor = new Vector4(0.1f, 0, 0, 0.1f);
        private const float m_maxGizmoDrawDistance = 200.0f;
        private BoundingBox m_gizmoBoundingBox = new BoundingBox();

        private bool m_playProximitySound = true;

        private bool m_active = false;
        public bool IsActive
        {
            get { return m_active; }
            set
            {
                if (m_active != value)
                {
                    m_active = value;
                    if (m_active)
                        OnFirstEnter();
                    else
                        OnLastLeave();

                    UpdateEmissivity();

                    (SyncObject as MySyncSensorBlock).SendSensorIsActiveChangedRequest(m_active);

                    var handle = StateChanged;
                    if (handle != null) handle(m_active);
                }
            }
        }

        private List<ToolbarItem> m_items;
        static private List<MyEntity> m_potentialPenetrations = new List<MyEntity>();

        public MyEntity LastDetectedEntity
        {
            get;
            private set;
        }

        protected HkShape m_fieldShape;
        private bool m_recreateField;

        public MyToolbar Toolbar { get; set; }

        private Vector3 m_fieldMin = new Vector3(-5f);
        public Vector3 FieldMin
        {
            get { return m_fieldMin; }
            set
            {
                if (m_fieldMin != value)
                {
                    m_fieldMin = value;
                    UpdateField();
                }
            }
        }

        private Vector3 m_fieldMax = new Vector3(5f);
        public Vector3 FieldMax
        {
            get { return m_fieldMax; }
            set
            {
                if (m_fieldMax != value)
                {
                    m_fieldMax = value;
                    UpdateField();
                }
            }
        }

        public float MaxRange
        {
            get { return BlockDefinition.MaxRange; }
        }

        public MySensorFilterFlags Filters
        {
            get;
            set;
        }

        public bool PlayProximitySound
        {
            get
            {
                return m_playProximitySound;
            }
            set
            {                
                m_playProximitySound = value;                
            }
        }

        public bool DetectPlayers
        {
            get
            {
                return (Filters & MySensorFilterFlags.Players) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.Players;
                else
                    Filters &= ~MySensorFilterFlags.Players;
            }
        }

        public bool DetectFloatingObjects
        {
            get
            {
                return (Filters & MySensorFilterFlags.FloatingObjects) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.FloatingObjects;
                else
                    Filters &= ~MySensorFilterFlags.FloatingObjects;
            }
        }

        public bool DetectSmallShips
        {
            get
            {
                return (Filters & MySensorFilterFlags.SmallShips) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.SmallShips;
                else
                    Filters &= ~MySensorFilterFlags.SmallShips;
            }
        }

        public bool DetectLargeShips
        {
            get
            {
                return (Filters & MySensorFilterFlags.LargeShips) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.LargeShips;
                else
                    Filters &= ~MySensorFilterFlags.LargeShips;
            }
        }

        public bool DetectStations
        {
            get
            {
                return (Filters & MySensorFilterFlags.Stations) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.Stations;
                else
                    Filters &= ~MySensorFilterFlags.Stations;
            }
        }

        public bool DetectAsteroids
        {
            get
            {
                return (Filters & MySensorFilterFlags.Asteroids) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.Asteroids;
                else
                    Filters &= ~MySensorFilterFlags.Asteroids;
            }
        }

        public bool DetectOwner
        {
            get
            {
                return (Filters & MySensorFilterFlags.Owner) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.Owner;
                else
                    Filters &= ~MySensorFilterFlags.Owner;
            }
        }

        public bool DetectFriendly
        {
            get
            {
                return (Filters & MySensorFilterFlags.Friendly) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.Friendly;
                else
                    Filters &= ~MySensorFilterFlags.Friendly;
            }
        }

        public bool DetectNeutral
        {
            get
            {
                return (Filters & MySensorFilterFlags.Neutral) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.Neutral;
                else
                    Filters &= ~MySensorFilterFlags.Neutral;
            }
        }

        public bool DetectEnemy
        {
            get
            {
                return (Filters & MySensorFilterFlags.Enemy) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.Enemy;
                else
                    Filters &= ~MySensorFilterFlags.Enemy;
            }
        }

        private static List<MyToolbar> m_openedToolbars;
        private static bool m_shouldSetOtherToolbars;

        static MySensorBlock()
        {
            m_openedToolbars = new List<MyToolbar>();

            var toolbarButton = new MyTerminalControlButton<MySensorBlock>("Open Toolbar", MySpaceTexts.BlockPropertyTitle_SensorToolbarOpen, MySpaceTexts.BlockPropertyDescription_SensorToolbarOpen,
                delegate(MySensorBlock self)
                {
                    m_openedToolbars.Add(self.Toolbar);
                    if (MyGuiScreenCubeBuilder.Static == null)
                    {
                        m_shouldSetOtherToolbars = true;
                        MyToolbarComponent.CurrentToolbar = self.Toolbar;
                        MyGuiScreenBase screen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.ToolbarConfigScreen, 0, self);
                        MyToolbarComponent.AutoUpdate = false;
                        screen.Closed += (source) =>
                            {
                                MyToolbarComponent.AutoUpdate = true;
                                m_openedToolbars.Clear();
                            };
                        MyGuiSandbox.AddScreen(screen);
                    }
                });
            MyTerminalControlFactory.AddControl(toolbarButton);

            var fieldWidthMin = new MyTerminalControlSlider<MySensorBlock>("Left", MySpaceTexts.BlockPropertyTitle_SensorFieldWidthMin, MySpaceTexts.BlockPropertyDescription_SensorFieldLeft);
            fieldWidthMin.SetLimits(block => 1, block => block.MaxRange);
            fieldWidthMin.DefaultValue = 5;
            fieldWidthMin.Getter = (x) => -x.m_fieldMin.X;
            fieldWidthMin.Setter = (x, v) =>
            {
                var fieldMin = x.FieldMin;
                fieldMin.X = -v;
                x.FieldMin = fieldMin;
                (x.SyncObject as MySyncSensorBlock).SendChangeSensorMinRequest(ref fieldMin);
            };
            fieldWidthMin.Writer = (x, result) => result.AppendInt32((int)-x.m_fieldMin.X).Append(" m");
            fieldWidthMin.EnableActions();
            MyTerminalControlFactory.AddControl(fieldWidthMin);

            var fieldWidthMax = new MyTerminalControlSlider<MySensorBlock>("Right", MySpaceTexts.BlockPropertyTitle_SensorFieldWidthMax, MySpaceTexts.BlockPropertyDescription_SensorFieldRight);
            fieldWidthMax.SetLimits(block => 1, block => block.MaxRange);
            fieldWidthMax.DefaultValue = 5;
            fieldWidthMax.Getter = (x) => x.m_fieldMax.X;
            fieldWidthMax.Setter = (x, v) =>
            {
                var fieldMax = x.FieldMax;
                fieldMax.X = v;
                x.FieldMax = fieldMax;
                (x.SyncObject as MySyncSensorBlock).SendChangeSensorMaxRequest(ref fieldMax);
            };
            fieldWidthMax.Writer = (x, result) => result.AppendInt32((int)x.m_fieldMax.X).Append(" m");
            fieldWidthMax.EnableActions();
            MyTerminalControlFactory.AddControl(fieldWidthMax);


            var fieldHeightMin = new MyTerminalControlSlider<MySensorBlock>("Bottom", MySpaceTexts.BlockPropertyTitle_SensorFieldHeightMin, MySpaceTexts.BlockPropertyDescription_SensorFieldBottom);
            fieldHeightMin.SetLimits(block => 1, block => block.MaxRange);
            fieldHeightMin.DefaultValue = 5;
            fieldHeightMin.Getter = (x) => -x.m_fieldMin.Y;
            fieldHeightMin.Setter = (x, v) =>
            {
                var fieldMin = x.FieldMin;
                fieldMin.Y = -v;
                x.FieldMin = fieldMin;
                (x.SyncObject as MySyncSensorBlock).SendChangeSensorMinRequest(ref fieldMin);
            };
            fieldHeightMin.Writer = (x, result) => result.AppendInt32((int)-x.m_fieldMin.Y).Append(" m");
            fieldHeightMin.EnableActions();
            MyTerminalControlFactory.AddControl(fieldHeightMin);

            var fieldHeightMax = new MyTerminalControlSlider<MySensorBlock>("Top", MySpaceTexts.BlockPropertyTitle_SensorFieldHeightMax, MySpaceTexts.BlockPropertyDescription_SensorFieldTop);
            fieldHeightMax.SetLimits(block => 1, block => block.MaxRange);
            fieldHeightMax.DefaultValue = 5;
            fieldHeightMax.Getter = (x) => x.m_fieldMax.Y;
            fieldHeightMax.Setter = (x, v) =>
            {
                var fieldMax = x.FieldMax;
                fieldMax.Y = v;
                x.FieldMax = fieldMax;
                (x.SyncObject as MySyncSensorBlock).SendChangeSensorMaxRequest(ref fieldMax);
            };
            fieldHeightMax.Writer = (x, result) => result.AppendInt32((int)x.m_fieldMax.Y).Append(" m");
            fieldHeightMax.EnableActions();
            MyTerminalControlFactory.AddControl(fieldHeightMax);

            var fieldDepthMax = new MyTerminalControlSlider<MySensorBlock>("Back", MySpaceTexts.BlockPropertyTitle_SensorFieldDepthMax, MySpaceTexts.BlockPropertyDescription_SensorFieldBack);
            fieldDepthMax.SetLimits(block => 1, block => block.MaxRange);
            fieldDepthMax.DefaultValue = 5;
            fieldDepthMax.Getter = (x) => x.m_fieldMax.Z;
            fieldDepthMax.Setter = (x, v) =>
            {
                var fieldMax = x.FieldMax;
                fieldMax.Z = v;
                x.FieldMax = fieldMax;
                (x.SyncObject as MySyncSensorBlock).SendChangeSensorMaxRequest(ref fieldMax);
            };
            fieldDepthMax.Writer = (x, result) => result.AppendInt32((int)x.m_fieldMax.Z).Append(" m");
            fieldDepthMax.EnableActions();
            MyTerminalControlFactory.AddControl(fieldDepthMax);

            var fieldDepthMin = new MyTerminalControlSlider<MySensorBlock>("Front", MySpaceTexts.BlockPropertyTitle_SensorFieldDepthMin, MySpaceTexts.BlockPropertyDescription_SensorFieldFront);
            fieldDepthMin.SetLimits(block => 1, block => block.MaxRange);
            fieldDepthMin.DefaultValue = 5;
            fieldDepthMin.Getter = (x) => -x.m_fieldMin.Z;
            fieldDepthMin.Setter = (x, v) =>
            {
                var fieldMin = x.FieldMin;
                fieldMin.Z = -v;
                x.FieldMin = fieldMin;
                (x.SyncObject as MySyncSensorBlock).SendChangeSensorMinRequest(ref fieldMin);
            };
            fieldDepthMin.Writer = (x, result) => result.AppendInt32((int)-x.m_fieldMin.Z).Append(" m");
            fieldDepthMin.EnableActions();
            MyTerminalControlFactory.AddControl(fieldDepthMin);

            var separatorFilters = new MyTerminalControlSeparator<MySensorBlock>();
            MyTerminalControlFactory.AddControl(separatorFilters);

            var detectPlayProximitySoundSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Audible Proximity Alert", MySpaceTexts.BlockPropertyTitle_SensorPlaySound, MySpaceTexts.BlockPropertyTitle_SensorPlaySound);
            detectPlayProximitySoundSwitch.Getter = (x) => x.PlayProximitySound;
            detectPlayProximitySoundSwitch.Setter = (x, v) =>
            {
                x.PlayProximitySound = v;
                (x.SyncObject as MySyncSensorBlock).SendChangeSensorPlaySoundRequest(x.PlayProximitySound);
            };                   
            MyTerminalControlFactory.AddControl(detectPlayProximitySoundSwitch);

            var detectPlayersSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Players", MySpaceTexts.BlockPropertyTitle_SensorDetectPlayers, MySpaceTexts.BlockPropertyTitle_SensorDetectPlayers);
            detectPlayersSwitch.Getter = (x) => x.DetectPlayers;
            detectPlayersSwitch.Setter = (x, v) =>
            {
                x.DetectPlayers = v;
                (x.SyncObject as MySyncSensorBlock).SendFiltersChangedRequest(x.Filters);
            };
            detectPlayersSwitch.EnableToggleAction(MyTerminalActionIcons.CHARACTER_TOGGLE);
            detectPlayersSwitch.EnableOnOffActions(MyTerminalActionIcons.CHARACTER_ON, MyTerminalActionIcons.CHARACTER_OFF);
            MyTerminalControlFactory.AddControl(detectPlayersSwitch);

            var detectFloatingObjectsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Floating Objects", MySpaceTexts.BlockPropertyTitle_SensorDetectFloatingObjects, MySpaceTexts.BlockPropertyTitle_SensorDetectFloatingObjects);
            detectFloatingObjectsSwitch.Getter = (x) => x.DetectFloatingObjects;
            detectFloatingObjectsSwitch.Setter = (x, v) =>
            {
                x.DetectFloatingObjects = v;
                (x.SyncObject as MySyncSensorBlock).SendFiltersChangedRequest(x.Filters);
            };
            detectFloatingObjectsSwitch.EnableToggleAction(MyTerminalActionIcons.MOVING_OBJECT_TOGGLE);
            detectFloatingObjectsSwitch.EnableOnOffActions(MyTerminalActionIcons.MOVING_OBJECT_ON, MyTerminalActionIcons.MOVING_OBJECT_OFF);
            MyTerminalControlFactory.AddControl(detectFloatingObjectsSwitch);

            var detectSmallShipsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Small Ships", MySpaceTexts.BlockPropertyTitle_SensorDetectSmallShips, MySpaceTexts.BlockPropertyTitle_SensorDetectSmallShips);
            detectSmallShipsSwitch.Getter = (x) => x.DetectSmallShips;
            detectSmallShipsSwitch.Setter = (x, v) =>
            {
                x.DetectSmallShips = v;
                (x.SyncObject as MySyncSensorBlock).SendFiltersChangedRequest(x.Filters);
            };
            detectSmallShipsSwitch.EnableToggleAction(MyTerminalActionIcons.SMALLSHIP_TOGGLE);
            detectSmallShipsSwitch.EnableOnOffActions(MyTerminalActionIcons.SMALLSHIP_ON, MyTerminalActionIcons.SMALLSHIP_OFF);
            MyTerminalControlFactory.AddControl(detectSmallShipsSwitch);

            var detectLargeShipsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Large Ships", MySpaceTexts.BlockPropertyTitle_SensorDetectLargeShips, MySpaceTexts.BlockPropertyTitle_SensorDetectLargeShips);
            detectLargeShipsSwitch.Getter = (x) => x.DetectLargeShips;
            detectLargeShipsSwitch.Setter = (x, v) =>
            {
                x.DetectLargeShips = v;
                (x.SyncObject as MySyncSensorBlock).SendFiltersChangedRequest(x.Filters);
            };
            detectLargeShipsSwitch.EnableToggleAction(MyTerminalActionIcons.LARGESHIP_TOGGLE);
            detectLargeShipsSwitch.EnableOnOffActions(MyTerminalActionIcons.LARGESHIP_ON, MyTerminalActionIcons.LARGESHIP_OFF);
            MyTerminalControlFactory.AddControl(detectLargeShipsSwitch);

            var detectStationsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Stations", MySpaceTexts.BlockPropertyTitle_SensorDetectStations, MySpaceTexts.BlockPropertyTitle_SensorDetectStations);
            detectStationsSwitch.Getter = (x) => x.DetectStations;
            detectStationsSwitch.Setter = (x, v) =>
            {
                x.DetectStations = v;
                (x.SyncObject as MySyncSensorBlock).SendFiltersChangedRequest(x.Filters);
            };
            detectStationsSwitch.EnableToggleAction(MyTerminalActionIcons.STATION_TOGGLE);
            detectStationsSwitch.EnableOnOffActions(MyTerminalActionIcons.STATION_ON, MyTerminalActionIcons.STATION_OFF);
            MyTerminalControlFactory.AddControl(detectStationsSwitch);

            var detectAsteroidsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Asteroids", MySpaceTexts.BlockPropertyTitle_SensorDetectAsteroids, MySpaceTexts.BlockPropertyTitle_SensorDetectAsteroids);
            detectAsteroidsSwitch.Getter = (x) => x.DetectAsteroids;
            detectAsteroidsSwitch.Setter = (x, v) =>
            {
                x.DetectAsteroids = v;
                (x.SyncObject as MySyncSensorBlock).SendFiltersChangedRequest(x.Filters);
            };
            detectAsteroidsSwitch.EnableToggleAction();
            detectAsteroidsSwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(detectAsteroidsSwitch);

            var separatorFactionFilters = new MyTerminalControlSeparator<MySensorBlock>();
            MyTerminalControlFactory.AddControl(separatorFactionFilters);

            var detectOwnerSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Owner", MySpaceTexts.BlockPropertyTitle_SensorDetectOwner, MySpaceTexts.BlockPropertyTitle_SensorDetectOwner);
            detectOwnerSwitch.Getter = (x) => x.DetectOwner;
            detectOwnerSwitch.Setter = (x, v) =>
            {
                x.DetectOwner = v;
                (x.SyncObject as MySyncSensorBlock).SendFiltersChangedRequest(x.Filters);
            };
            detectOwnerSwitch.EnableToggleAction();
            detectOwnerSwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(detectOwnerSwitch);

            var detectFriendlySwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Friendly", MySpaceTexts.BlockPropertyTitle_SensorDetectFriendly, MySpaceTexts.BlockPropertyTitle_SensorDetectFriendly);
            detectFriendlySwitch.Getter = (x) => x.DetectFriendly;
            detectFriendlySwitch.Setter = (x, v) =>
            {
                x.DetectFriendly = v;
                (x.SyncObject as MySyncSensorBlock).SendFiltersChangedRequest(x.Filters);
            };
            detectFriendlySwitch.EnableToggleAction();
            detectFriendlySwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(detectFriendlySwitch);

            var detectNeutralSwitch = new  MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Neutral", MySpaceTexts.BlockPropertyTitle_SensorDetectNeutral, MySpaceTexts.BlockPropertyTitle_SensorDetectNeutral);
            detectNeutralSwitch.Getter = (x) => x.DetectNeutral;
            detectNeutralSwitch.Setter = (x, v) =>
            {
                x.DetectNeutral = v;
                (x.SyncObject as MySyncSensorBlock).SendFiltersChangedRequest(x.Filters);
            };
            detectNeutralSwitch.EnableToggleAction();
            detectNeutralSwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(detectNeutralSwitch);

            var detectEnemySwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Enemy", MySpaceTexts.BlockPropertyTitle_SensorDetectEnemy, MySpaceTexts.BlockPropertyTitle_SensorDetectEnemy);
            detectEnemySwitch.Getter = (x) => x.DetectEnemy;
            detectEnemySwitch.Setter = (x, v) =>
            {
                x.DetectEnemy = v;
                (x.SyncObject as MySyncSensorBlock).SendFiltersChangedRequest(x.Filters);
            };
            detectEnemySwitch.EnableToggleAction();
            detectEnemySwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(detectEnemySwitch);
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(objectBuilder, cubeGrid);

            m_items = new List<ToolbarItem>(2);
            for (int i = 0; i < 2; i++)
            {
                m_items.Add(new ToolbarItem() { EntityID = 0 });
            }
            Toolbar = new MyToolbar(MyToolbarType.ButtonPanel, 2, 1);
            Toolbar.DrawNumbers = false;

            var builder = (MyObjectBuilder_SensorBlock)objectBuilder;
            
            m_fieldMin = Vector3.Clamp(builder.FieldMin, new Vector3(-MaxRange), -Vector3.One);
            m_fieldMax = Vector3.Clamp(builder.FieldMax, Vector3.One, new Vector3(MaxRange));

            PlayProximitySound = builder.PlaySound;
            DetectPlayers = builder.DetectPlayers;
            DetectFloatingObjects = builder.DetectFloatingObjects;
            DetectSmallShips = builder.DetectSmallShips;
            DetectLargeShips = builder.DetectLargeShips;
            DetectStations = builder.DetectStations;
            DetectAsteroids = builder.DetectAsteroids;
            DetectOwner = builder.DetectOwner;
            DetectFriendly = builder.DetectFriendly;
            DetectNeutral = builder.DetectNeutral;
            DetectEnemy = builder.DetectEnemy;
            m_active = builder.IsActive;

            Toolbar.Init(builder.Toolbar, this);

            for (int i = 0; i < 2; i++)
            {
                var item = Toolbar.GetItemAtIndex(i);
                if (item == null)
                    continue;
                m_items.RemoveAt(i);
                m_items.Insert(i, ToolbarItem.FromItem(item));
            }
            Toolbar.ItemChanged += Toolbar_ItemChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                BlockDefinition.RequiredPowerInput,
                this.CalculateRequiredPowerInput);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.RequiredInputChanged += Receiver_RequiredInputChanged;
            PowerReceiver.Update();

            m_fieldShape = GetHkShape();

            OnClose += delegate(MyEntity self)
            {
                m_fieldShape.RemoveReference();
            };

        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
            PowerReceiver.Update();
        }

        public override void OnBuildSuccess(long builtBy)
        {
            PowerReceiver.Update();
            base.OnBuildSuccess(builtBy);
        }

        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncSensorBlock(this);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            if (InScene)
                UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (!IsWorking || !PowerReceiver.IsPowered)
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
                return;
            }

            if (IsActive)
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Blue, Color.White);
        }

        protected void UpdateField()
        {
            m_recreateField = true;
        }

        protected HkShape GetHkShape()
        {
            return new HkBoxShape((m_fieldMax - m_fieldMin) * 0.5f);
        }

        protected override void OnEnabledChanged()
        {
            PowerReceiver.Update();
            UpdateEmissivity();
            base.OnEnabledChanged();
        }

        protected float CalculateRequiredPowerInput()
        {
            if (Enabled && IsFunctional)
                return 0.0003f * (float)Math.Pow((m_fieldMax - m_fieldMin).Volume, 1f / 3f);
            else
                return 0.0f;
        }

        protected void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            PowerReceiver.Update();

            UpdateText();
            UpdateEmissivity();
        }

        protected void Receiver_RequiredInputChanged(MyPowerReceiver receiver, float oldRequirement, float newRequirement)
        {
            UpdateText();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
            UpdateEmissivity();
        }

        void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.MaxRequiredInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.IsPowered ? PowerReceiver.RequiredInput : 0, DetailedInfo);
            RaisePropertiesChanged();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_SensorBlock;
            ob.FieldMin = FieldMin;
            ob.FieldMax = FieldMax;

            ob.PlaySound = PlayProximitySound;
            ob.DetectPlayers = DetectPlayers;
            ob.DetectFloatingObjects = DetectFloatingObjects;
            ob.DetectSmallShips = DetectSmallShips;
            ob.DetectLargeShips = DetectLargeShips;
            ob.DetectStations = DetectStations;
            ob.DetectAsteroids = DetectAsteroids;
            ob.DetectOwner = DetectOwner;
            ob.DetectFriendly = DetectFriendly;
            ob.DetectNeutral = DetectNeutral;
            ob.DetectEnemy = DetectEnemy;
            ob.IsActive = IsActive;

            ob.Toolbar = Toolbar.GetObjectBuilder();
            return ob;
        }

        void Toolbar_ItemChanged(MyToolbar self, MyToolbar.IndexArgs index)
        {
            Debug.Assert(self == Toolbar);

            var tItem = ToolbarItem.FromItem(self.GetItemAtIndex(index.ItemIndex));
            var oldItem = m_items[index.ItemIndex];
            if ((tItem.EntityID == 0 && oldItem.EntityID == 0 || (tItem.EntityID != 0 && oldItem.EntityID != 0 && tItem.Equals(oldItem))))
                return;
            m_items.RemoveAt(index.ItemIndex);
            m_items.Insert(index.ItemIndex, tItem);
            (SyncObject as MySyncSensorBlock).SendToolbarItemChanged(tItem, index.ItemIndex);

            if (m_shouldSetOtherToolbars)
            {
                m_shouldSetOtherToolbars = false;
                if (!(SyncObject as MySyncSensorBlock).IsSyncing)
                {
                    foreach (var toolbar in m_openedToolbars)
                    {
                        if (toolbar != self)
                        {
                            toolbar.SetItemAtIndex(index.ItemIndex, self.GetItemAtIndex(index.ItemIndex));
                        }
                    }
                }
                m_shouldSetOtherToolbars = true;
            }
        }

        private void OnFirstEnter()
        {
            UpdateEmissivity();
            Toolbar.UpdateItem(0);
            if (Sync.IsServer)
                Toolbar.ActivateItemAtSlot(0, false, PlayProximitySound);
        }

        private void OnLastLeave()
        {
            UpdateEmissivity();
            Toolbar.UpdateItem(1);
            if (Sync.IsServer)
                Toolbar.ActivateItemAtSlot(1, false, PlayProximitySound);
        }

        public bool ShouldDetectRelation(MyRelationsBetweenPlayerAndBlock relation)
        {
            switch (relation)
            {
                case MyRelationsBetweenPlayerAndBlock.Owner:
                    return DetectOwner;
                    break;
                case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    return DetectFriendly;
                    break;
                case MyRelationsBetweenPlayerAndBlock.Neutral:
                    return DetectNeutral;
                    break;
                case MyRelationsBetweenPlayerAndBlock.Enemies:
                    return DetectEnemy;
                    break;
                default:
                    throw new InvalidBranchException();
                    break;
            }
            return false;
        }

        public bool ShouldDetectGrid(MyCubeGrid grid)
        {
            bool noRelation = true;
            foreach (var playerId in grid.BigOwners)
            {
                var relation = MyPlayer.GetRelationBetweenPlayers(OwnerId, playerId);
                if (ShouldDetectRelation(relation))
                {
                    return true;
                }
                noRelation = false;
            }

            if (noRelation)
            {
                return ShouldDetectRelation(MyRelationsBetweenPlayerAndBlock.Enemies);
            }

            return false;
        }

        private bool ShouldDetect(MyEntity entity)
        {
            if (entity == null)
                return false;

            if (entity == CubeGrid)
                return false;

            if (DetectPlayers)
                if (entity is Character.MyCharacter)
                    return ShouldDetectRelation((entity as Character.MyCharacter).GetRelationTo(OwnerId));
            if (DetectFloatingObjects)
                if (entity is MyFloatingObject)
                    return true;
            var grid = entity as MyCubeGrid;
            if (DetectSmallShips)
                if (grid != null && grid.GridSizeEnum == MyCubeSize.Small)
                    return ShouldDetectGrid(grid);
            if (DetectLargeShips)
                if (grid != null && grid.GridSizeEnum == MyCubeSize.Large && !grid.IsStatic)
                    return ShouldDetectGrid(grid);
            if (DetectStations)
                if (grid != null && grid.GridSizeEnum == MyCubeSize.Large && grid.IsStatic)
                    return ShouldDetectGrid(grid);
            if (DetectAsteroids)
                if (entity is MyVoxelMap)
                    return true;

            return false;
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (!Sync.IsServer || !IsWorking || !PowerReceiver.IsPowered)
                return;

            var rotation1 = Quaternion.CreateFromForwardUp(WorldMatrix.Forward, WorldMatrix.Up);
            var position1 = PositionComp.GetPosition() + Vector3D.Transform(PositionComp.LocalVolume.Center + (m_fieldMax + m_fieldMin) * 0.5f, rotation1);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Recreate Field");
            if (m_recreateField)
            {
                m_recreateField = false;
                m_fieldShape.RemoveReference();
                m_fieldShape = GetHkShape();
                PowerReceiver.Update();
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            var boundingBox = new BoundingBoxD(m_fieldMin, m_fieldMax).Translate(PositionComp.LocalVolume.Center).Transform(WorldMatrix.GetOrientation()).Translate(PositionComp.GetPosition());

            m_potentialPenetrations.Clear();
            MyGamePruningStructure.GetAllTopMostEntitiesInBox<MyEntity>(ref boundingBox, m_potentialPenetrations);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Sensor Physics");
            LastDetectedEntity = null;
            if (IsActive)
            {
                bool empty = true;
                foreach (var entity in m_potentialPenetrations)
                {
                    if (ShouldDetect(entity))
                    {
                        if (entity.Physics == null || !entity.Physics.Enabled)
                            continue;

                        Quaternion rotation2;
                        Vector3 posDiff;
                        HkShape? shape2;

                        if (entity.Physics.RigidBody != null)
                        {
                            shape2 = entity.Physics.RigidBody.GetShape();

                            var worldMatrix = entity.WorldMatrix;
                            rotation2 = Quaternion.CreateFromForwardUp(worldMatrix.Forward, worldMatrix.Up);
                            posDiff = entity.PositionComp.GetPosition() - position1;
                            if (entity is MyVoxelMap)
                            {
                                var voxel = entity as MyVoxelMap;
                                posDiff -= voxel.Storage.Size / 2;
                            }
                        }
                        else if (entity.Physics.CharacterProxy != null)
                        {
                            shape2 = entity.Physics.CharacterProxy.GetShape();
                            var worldMatrix = entity.WorldMatrix;
                            rotation2 = Quaternion.CreateFromForwardUp(worldMatrix.Forward, worldMatrix.Up);
                            posDiff = entity.PositionComp.GetPosition() - position1;
                        }
                        else
                        {
                            continue;
                        }

                        if (entity.Physics.HavokWorld.IsPenetratingShapeShape(m_fieldShape, ref Vector3.Zero, ref rotation1, shape2.Value, ref posDiff, ref rotation2))
                        {
                            LastDetectedEntity = entity;
                            empty = false;
                            break;
                        }
                    }
                }

                if (empty)
                {
                    IsActive = false;
                }
            }
            else
            {
                foreach (var entity in m_potentialPenetrations)
                {
                    if (ShouldDetect(entity))
                    {
                        if (entity.Physics == null || !entity.Physics.Enabled)
                            continue;

                        Quaternion rotation2;
                        Vector3 posDiff;
                        HkShape? shape2;

                        if (entity.Physics.RigidBody != null)
                        {
                            shape2 = entity.Physics.RigidBody.GetShape();

                            var worldMatrix = entity.WorldMatrix;
                            rotation2 = Quaternion.CreateFromForwardUp(worldMatrix.Forward, worldMatrix.Up);
                            posDiff = entity.PositionComp.GetPosition() - position1;
                            if (entity is MyVoxelMap)
                            {
                                var voxel = entity as MyVoxelMap;
                                posDiff -= voxel.Storage.Size / 2;
                            }
                        }
                        else if (entity.Physics.CharacterProxy != null)
                        {
                            shape2 = entity.Physics.CharacterProxy.GetShape();
                            var worldMatrix = entity.WorldMatrix;
                            rotation2 = Quaternion.CreateFromForwardUp(worldMatrix.Forward, worldMatrix.Up);
                            posDiff = entity.PositionComp.GetPosition() - position1;
                        }
                        else
                        {
                            continue;
                        }

                        if (entity.Physics.HavokWorld.IsPenetratingShapeShape(m_fieldShape, ref Vector3.Zero, ref rotation1, shape2.Value, ref posDiff, ref rotation2))
                        {
                            LastDetectedEntity = entity;
                            IsActive = true;
                            break;
                        }
                    }
                }
            }
            m_potentialPenetrations.Clear();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        event Action<bool> StateChanged;
        event Action<bool> Sandbox.ModAPI.IMySensorBlock.StateChanged
        {
            add { StateChanged += value; }
            remove { StateChanged -= value; }
        }

        public Color GetGizmoColor()
        {
            return m_gizmoColor;
        }

        public bool CanBeDrawed()
        {
            if (false == MyCubeGrid.ShowSenzorGizmos || false == ShowOnHUD || false == IsWorking || false == HasLocalPlayerAccess() ||
               GetDistanceBetweenCameraAndBoundingSphere() > m_maxGizmoDrawDistance)
            {
                return false;
            }
            return Entities.Cube.MyRadioAntenna.IsRecievedByPlayer(this);
        }

        public BoundingBox? GetBoundingBox()
        {
            m_gizmoBoundingBox.Min = PositionComp.LocalVolume.Center + this.FieldMin;
            m_gizmoBoundingBox.Max = PositionComp.LocalVolume.Center + this.FieldMax;

            return m_gizmoBoundingBox;
        }

        public float GetRadius()
        {
            return -1;
        }

        public MatrixD GetWorldMatrix()
        {
            return WorldMatrix;
        }
        public Vector3 GetPositionInGrid()
        {
            return Position;
        }

        public bool EnableLongDrawDistance()
        {
            return false;
        }

        float ModAPI.Ingame.IMySensorBlock.LeftExtend { get { return -m_fieldMin.X; } }
        float ModAPI.Ingame.IMySensorBlock.RightExtend { get { return m_fieldMax.X; } }
        float ModAPI.Ingame.IMySensorBlock.TopExtend { get { return m_fieldMax.Y; } }
        float ModAPI.Ingame.IMySensorBlock.BottomExtend { get { return -m_fieldMin.Y; } }
        float ModAPI.Ingame.IMySensorBlock.FrontExtend { get { return -m_fieldMin.Z; } }
        float ModAPI.Ingame.IMySensorBlock.BackExtend { get { return m_fieldMax.Z; } }
        bool ModAPI.Ingame.IMySensorBlock.PlayProximitySound { get { return PlayProximitySound; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectPlayers { get { return DetectPlayers; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectFloatingObjects { get { return DetectFloatingObjects; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectSmallShips { get { return DetectSmallShips; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectLargeShips { get { return DetectLargeShips; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectStations { get { return DetectStations; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectAsteroids { get { return DetectAsteroids; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectOwner { get { return DetectOwner; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectFriendly { get { return DetectFriendly; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectNeutral { get { return DetectNeutral; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectEnemy { get { return DetectEnemy; } }
        bool ModAPI.Ingame.IMySensorBlock.IsActive { get { return IsActive; } }
        IMyEntity ModAPI.Ingame.IMySensorBlock.LastDetectedEntity { get { return LastDetectedEntity; } }
    }
}
