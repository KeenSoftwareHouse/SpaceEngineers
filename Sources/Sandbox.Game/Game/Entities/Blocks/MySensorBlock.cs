using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Screens.Terminal.Controls;
using VRage.ModAPI;
using VRage.Game.Entity;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI.Ingame;
using VRage.Collections;
using VRage.Game;
using VRage.Sync;
using VRage.Voxels;
using Parallel = ParallelTasks.Parallel;

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
        Subgrids          = 1 << 6,

        Owner             = 1 << 8,
        Friendly          = 1 << 9,
        Neutral           = 1 << 10,
        Enemy             = 1 << 11,
    }


    [MyCubeBlockType(typeof(MyObjectBuilder_SensorBlock))]
    public class MySensorBlock : MyFunctionalBlock, Sandbox.ModAPI.IMySensorBlock, IMyGizmoDrawableObject
    {
        private new MySensorBlockDefinition BlockDefinition
        {
            get { return (MySensorBlockDefinition)base.BlockDefinition; }
        }

        private Color m_gizmoColor;
        private const float m_maxGizmoDrawDistance = 400.0f;
        private BoundingBox m_gizmoBoundingBox = new BoundingBox();

        private readonly Sync<bool> m_playProximitySound;

        private MyConcurrentHashSet<MyDetectedEntityInfo> m_detectedEntities = new MyConcurrentHashSet<MyDetectedEntityInfo>();

        private Sync<bool> m_active;
        public bool IsActive
        {
            get { return m_active; }
            set
            {
                m_active.Value = value;
            } 
        }

        private List<ToolbarItem> m_items;
        static private readonly List<MyEntity> m_potentialPenetrations = new List<MyEntity>();
        static private readonly List<MyVoxelBase> m_potentialVoxelPenetrations = new List<MyVoxelBase>();

        public MyEntity LastDetectedEntity
        {
            get;
            private set;
        }

        protected HkShape m_fieldShape;
        private bool m_recreateField;

        public MyToolbar Toolbar { get; set; }

        private readonly Sync<Vector3> m_fieldMin;
        public Vector3 FieldMin
        {
            get { return m_fieldMin; }
            set
            {
                m_fieldMin.Value = value; ;
            }
        }

        private readonly Sync<Vector3> m_fieldMax;
        public Vector3 FieldMax
        {
            get { return m_fieldMax; }
            set
            {
                m_fieldMax.Value = value;
            }
        }

        public float MaxRange
        {
            get { return BlockDefinition.MaxRange; }
        }

        readonly Sync<MySensorFilterFlags> m_flags;
        public MySensorFilterFlags Filters
        {
            get
            {
              return  m_flags;
            }
            set
            {
                m_flags.Value = value;
            }
        }

        public bool PlayProximitySound
        {
            get
            {
                return m_playProximitySound;
            }
            set
            {                
                m_playProximitySound.Value = value;                
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

        public bool DetectSubgrids
        {
            get
            {
                return (Filters & MySensorFilterFlags.Subgrids) != 0;
            }
            set
            {
                if (value)
                    Filters |= MySensorFilterFlags.Subgrids;
                else
                    Filters &= ~MySensorFilterFlags.Subgrids;
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
        bool m_syncing = false;

        public MySensorBlock()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_playProximitySound = SyncType.CreateAndAddProp<bool>();
            m_active = SyncType.CreateAndAddProp<bool>();
            m_fieldMin = SyncType.CreateAndAddProp<Vector3>();
            m_fieldMax = SyncType.CreateAndAddProp<Vector3>();
            m_flags = SyncType.CreateAndAddProp<MySensorFilterFlags>();
#endif // XB1
            CreateTerminalControls();

            m_active.ValueChanged += (x) => IsActiveChanged();
            m_fieldMax.ValueChanged += (x) => UpdateField();
            m_fieldMin.ValueChanged +=(x) => UpdateField();
        }

        protected void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MySensorBlock>())
                return;
            base.CreateTerminalControls();
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
            fieldWidthMin.Getter = (x) => -x.m_fieldMin.Value.X;
            fieldWidthMin.Setter = (x, v) =>
            {
                var fieldMin = x.FieldMin;
                if (fieldMin.X == -v)
                    return;
                fieldMin.X = -v;
                x.FieldMin = fieldMin;
            };
            fieldWidthMin.Writer = (x, result) => result.AppendInt32((int)-x.m_fieldMin.Value.X).Append(" m");
            fieldWidthMin.EnableActions();
            MyTerminalControlFactory.AddControl(fieldWidthMin);

            var fieldWidthMax = new MyTerminalControlSlider<MySensorBlock>("Right", MySpaceTexts.BlockPropertyTitle_SensorFieldWidthMax, MySpaceTexts.BlockPropertyDescription_SensorFieldRight);
            fieldWidthMax.SetLimits(block => 1, block => block.MaxRange);
            fieldWidthMax.DefaultValue = 5;
            fieldWidthMax.Getter = (x) => x.m_fieldMax.Value.X;
            fieldWidthMax.Setter = (x, v) =>
            {
                var fieldMax = x.FieldMax;
                if (fieldMax.X == v)
                    return;
                fieldMax.X = v;
                x.FieldMax = fieldMax;
            };
            fieldWidthMax.Writer = (x, result) => result.AppendInt32((int)x.m_fieldMax.Value.X).Append(" m");
            fieldWidthMax.EnableActions();
            MyTerminalControlFactory.AddControl(fieldWidthMax);


            var fieldHeightMin = new MyTerminalControlSlider<MySensorBlock>("Bottom", MySpaceTexts.BlockPropertyTitle_SensorFieldHeightMin, MySpaceTexts.BlockPropertyDescription_SensorFieldBottom);
            fieldHeightMin.SetLimits(block => 1, block => block.MaxRange);
            fieldHeightMin.DefaultValue = 5;
            fieldHeightMin.Getter = (x) => -x.m_fieldMin.Value.Y;
            fieldHeightMin.Setter = (x, v) =>
            {
                var fieldMin = x.FieldMin;
                if (fieldMin.Y == -v)
                    return;
                fieldMin.Y = -v;
                x.FieldMin = fieldMin;
            };
            fieldHeightMin.Writer = (x, result) => result.AppendInt32((int)-x.m_fieldMin.Value.Y).Append(" m");
            fieldHeightMin.EnableActions();
            MyTerminalControlFactory.AddControl(fieldHeightMin);

            var fieldHeightMax = new MyTerminalControlSlider<MySensorBlock>("Top", MySpaceTexts.BlockPropertyTitle_SensorFieldHeightMax, MySpaceTexts.BlockPropertyDescription_SensorFieldTop);
            fieldHeightMax.SetLimits(block => 1, block => block.MaxRange);
            fieldHeightMax.DefaultValue = 5;
            fieldHeightMax.Getter = (x) => x.m_fieldMax.Value.Y;
            fieldHeightMax.Setter = (x, v) =>
            {
                var fieldMax = x.FieldMax;
                if (fieldMax.Y == v)
                    return;
                fieldMax.Y = v;
                x.FieldMax = fieldMax;
            };
            fieldHeightMax.Writer = (x, result) => result.AppendInt32((int)x.m_fieldMax.Value.Y).Append(" m");
            fieldHeightMax.EnableActions();
            MyTerminalControlFactory.AddControl(fieldHeightMax);

            var fieldDepthMax = new MyTerminalControlSlider<MySensorBlock>("Back", MySpaceTexts.BlockPropertyTitle_SensorFieldDepthMax, MySpaceTexts.BlockPropertyDescription_SensorFieldBack);
            fieldDepthMax.SetLimits(block => 1, block => block.MaxRange);
            fieldDepthMax.DefaultValue = 5;
            fieldDepthMax.Getter = (x) => x.m_fieldMax.Value.Z;
            fieldDepthMax.Setter = (x, v) =>
            {
                var fieldMax = x.FieldMax;
                if (fieldMax.Z == v)
                    return;
                fieldMax.Z = v;
                x.FieldMax = fieldMax;
            };
            fieldDepthMax.Writer = (x, result) => result.AppendInt32((int)x.m_fieldMax.Value.Z).Append(" m");
            fieldDepthMax.EnableActions();
            MyTerminalControlFactory.AddControl(fieldDepthMax);

            var fieldDepthMin = new MyTerminalControlSlider<MySensorBlock>("Front", MySpaceTexts.BlockPropertyTitle_SensorFieldDepthMin, MySpaceTexts.BlockPropertyDescription_SensorFieldFront);
            fieldDepthMin.SetLimits(block => 1, block => block.MaxRange);
            fieldDepthMin.DefaultValue = 5;
            fieldDepthMin.Getter = (x) => -x.m_fieldMin.Value.Z;
            fieldDepthMin.Setter = (x, v) =>
            {
                var fieldMin = x.FieldMin;
                if (fieldMin.Z == -v)
                    return;
                fieldMin.Z = -v;
                x.FieldMin = fieldMin;
            };
            fieldDepthMin.Writer = (x, result) => result.AppendInt32((int)-x.m_fieldMin.Value.Z).Append(" m");
            fieldDepthMin.EnableActions();
            MyTerminalControlFactory.AddControl(fieldDepthMin);

            var separatorFilters = new MyTerminalControlSeparator<MySensorBlock>();
            MyTerminalControlFactory.AddControl(separatorFilters);

            var detectPlayProximitySoundSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Audible Proximity Alert", MySpaceTexts.BlockPropertyTitle_SensorPlaySound, MySpaceTexts.BlockPropertyTitle_SensorPlaySound);
            detectPlayProximitySoundSwitch.Getter = (x) => x.PlayProximitySound;
            detectPlayProximitySoundSwitch.Setter = (x, v) =>
            {
                x.PlayProximitySound = v;
            };                   
            MyTerminalControlFactory.AddControl(detectPlayProximitySoundSwitch);

            var detectPlayersSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Players", MySpaceTexts.BlockPropertyTitle_SensorDetectPlayers, MySpaceTexts.BlockPropertyTitle_SensorDetectPlayers);
            detectPlayersSwitch.Getter = (x) => x.DetectPlayers;
            detectPlayersSwitch.Setter = (x, v) =>
            {
                x.DetectPlayers = v;
            };
            detectPlayersSwitch.EnableToggleAction(MyTerminalActionIcons.CHARACTER_TOGGLE);
            detectPlayersSwitch.EnableOnOffActions(MyTerminalActionIcons.CHARACTER_ON, MyTerminalActionIcons.CHARACTER_OFF);
            MyTerminalControlFactory.AddControl(detectPlayersSwitch);

            var detectFloatingObjectsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Floating Objects", MySpaceTexts.BlockPropertyTitle_SensorDetectFloatingObjects, MySpaceTexts.BlockPropertyTitle_SensorDetectFloatingObjects);
            detectFloatingObjectsSwitch.Getter = (x) => x.DetectFloatingObjects;
            detectFloatingObjectsSwitch.Setter = (x, v) =>
            {
                x.DetectFloatingObjects = v;
            };
            detectFloatingObjectsSwitch.EnableToggleAction(MyTerminalActionIcons.MOVING_OBJECT_TOGGLE);
            detectFloatingObjectsSwitch.EnableOnOffActions(MyTerminalActionIcons.MOVING_OBJECT_ON, MyTerminalActionIcons.MOVING_OBJECT_OFF);
            MyTerminalControlFactory.AddControl(detectFloatingObjectsSwitch);

            var detectSmallShipsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Small Ships", MySpaceTexts.BlockPropertyTitle_SensorDetectSmallShips, MySpaceTexts.BlockPropertyTitle_SensorDetectSmallShips);
            detectSmallShipsSwitch.Getter = (x) => x.DetectSmallShips;
            detectSmallShipsSwitch.Setter = (x, v) =>
            {
                x.DetectSmallShips = v;
            };
            detectSmallShipsSwitch.EnableToggleAction(MyTerminalActionIcons.SMALLSHIP_TOGGLE);
            detectSmallShipsSwitch.EnableOnOffActions(MyTerminalActionIcons.SMALLSHIP_ON, MyTerminalActionIcons.SMALLSHIP_OFF);
            MyTerminalControlFactory.AddControl(detectSmallShipsSwitch);

            var detectLargeShipsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Large Ships", MySpaceTexts.BlockPropertyTitle_SensorDetectLargeShips, MySpaceTexts.BlockPropertyTitle_SensorDetectLargeShips);
            detectLargeShipsSwitch.Getter = (x) => x.DetectLargeShips;
            detectLargeShipsSwitch.Setter = (x, v) =>
            {
                x.DetectLargeShips = v;
            };
            detectLargeShipsSwitch.EnableToggleAction(MyTerminalActionIcons.LARGESHIP_TOGGLE);
            detectLargeShipsSwitch.EnableOnOffActions(MyTerminalActionIcons.LARGESHIP_ON, MyTerminalActionIcons.LARGESHIP_OFF);
            MyTerminalControlFactory.AddControl(detectLargeShipsSwitch);

            var detectStationsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Stations", MySpaceTexts.BlockPropertyTitle_SensorDetectStations, MySpaceTexts.BlockPropertyTitle_SensorDetectStations);
            detectStationsSwitch.Getter = (x) => x.DetectStations;
            detectStationsSwitch.Setter = (x, v) =>
            {
                x.DetectStations = v;
            };
            detectStationsSwitch.EnableToggleAction(MyTerminalActionIcons.STATION_TOGGLE);
            detectStationsSwitch.EnableOnOffActions(MyTerminalActionIcons.STATION_ON, MyTerminalActionIcons.STATION_OFF);
            MyTerminalControlFactory.AddControl(detectStationsSwitch);

            var detectSubgridsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Subgrids", MySpaceTexts.BlockPropertyTitle_SensorDetectSubgrids, MySpaceTexts.BlockPropertyTitle_SensorDetectSubgrids);
            detectSubgridsSwitch.Getter = (x) => x.DetectSubgrids;
            detectSubgridsSwitch.Setter = (x, v) =>
            {
                x.DetectSubgrids = v;
            };
            detectSubgridsSwitch.EnableToggleAction(MyTerminalActionIcons.SUBGRID_TOGGLE);
            detectSubgridsSwitch.EnableOnOffActions(MyTerminalActionIcons.SUBGRID_ON, MyTerminalActionIcons.SUBGRID_OFF);
            MyTerminalControlFactory.AddControl(detectSubgridsSwitch);

            var detectAsteroidsSwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Asteroids", MySpaceTexts.BlockPropertyTitle_SensorDetectAsteroids, MySpaceTexts.BlockPropertyTitle_SensorDetectAsteroids);
            detectAsteroidsSwitch.Getter = (x) => x.DetectAsteroids;
            detectAsteroidsSwitch.Setter = (x, v) =>
            {
                x.DetectAsteroids = v;
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
            };
            detectOwnerSwitch.EnableToggleAction();
            detectOwnerSwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(detectOwnerSwitch);

            var detectFriendlySwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Friendly", MySpaceTexts.BlockPropertyTitle_SensorDetectFriendly, MySpaceTexts.BlockPropertyTitle_SensorDetectFriendly);
            detectFriendlySwitch.Getter = (x) => x.DetectFriendly;
            detectFriendlySwitch.Setter = (x, v) =>
            {
                x.DetectFriendly = v;
            };
            detectFriendlySwitch.EnableToggleAction();
            detectFriendlySwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(detectFriendlySwitch);

            var detectNeutralSwitch = new  MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Neutral", MySpaceTexts.BlockPropertyTitle_SensorDetectNeutral, MySpaceTexts.BlockPropertyTitle_SensorDetectNeutral);
            detectNeutralSwitch.Getter = (x) => x.DetectNeutral;
            detectNeutralSwitch.Setter = (x, v) =>
            {
                x.DetectNeutral = v;
            };
            detectNeutralSwitch.EnableToggleAction();
            detectNeutralSwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(detectNeutralSwitch);

            var detectEnemySwitch = new MyTerminalControlOnOffSwitch<MySensorBlock>("Detect Enemy", MySpaceTexts.BlockPropertyTitle_SensorDetectEnemy, MySpaceTexts.BlockPropertyTitle_SensorDetectEnemy);
            detectEnemySwitch.Getter = (x) => x.DetectEnemy;
            detectEnemySwitch.Setter = (x, v) =>
            {
                x.DetectEnemy = v;
            };
            detectEnemySwitch.EnableToggleAction();
            detectEnemySwitch.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(detectEnemySwitch);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                BlockDefinition.ResourceSinkGroup,
                BlockDefinition.RequiredPowerInput,
                this.CalculateRequiredPowerInput);
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            m_items = new List<ToolbarItem>(2);
            for (int i = 0; i < 2; i++)
            {
                m_items.Add(new ToolbarItem() { EntityID = 0 });
            }
            Toolbar = new MyToolbar(MyToolbarType.ButtonPanel, 2, 1);
            Toolbar.DrawNumbers = false;

            var builder = (MyObjectBuilder_SensorBlock)objectBuilder;
            
            m_fieldMin.Value = Vector3.Clamp(builder.FieldMin, new Vector3(-MaxRange), -Vector3.One);
            m_fieldMax.Value = Vector3.Clamp(builder.FieldMax, Vector3.One, new Vector3(MaxRange));

            PlayProximitySound = builder.PlaySound;
            DetectPlayers = builder.DetectPlayers;
            DetectFloatingObjects = builder.DetectFloatingObjects;
            DetectSmallShips = builder.DetectSmallShips;
            DetectLargeShips = builder.DetectLargeShips;
            DetectStations = builder.DetectStations;
            DetectSubgrids = builder.DetectSubgrids;
            DetectAsteroids = builder.DetectAsteroids;
            DetectOwner = builder.DetectOwner;
            DetectFriendly = builder.DetectFriendly;
            DetectNeutral = builder.DetectNeutral;
            DetectEnemy = builder.DetectEnemy;
            m_active.Value = builder.IsActive;

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

			
			ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
			ResourceSink.RequiredInputChanged += Receiver_RequiredInputChanged;
			ResourceSink.Update();

            m_fieldShape = GetHkShape();

            OnClose += delegate(MyEntity self)
            {
                m_fieldShape.RemoveReference();
            };

            m_gizmoColor = new Vector4(0.35f, 0, 0, 0.5f);

        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
			ResourceSink.Update();
        }

        public override void OnBuildSuccess(long builtBy)
        {
			ResourceSink.Update();
            base.OnBuildSuccess(builtBy);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            if (InScene)
                UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (!IsWorking || !ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
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
            return new HkBoxShape((m_fieldMax.Value - m_fieldMin.Value) * 0.5f);
        }

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
            UpdateEmissivity();
            base.OnEnabledChanged();
        }

        protected float CalculateRequiredPowerInput()
        {
            if (Enabled && IsFunctional)
                return 0.0003f * (float)Math.Pow((m_fieldMax.Value - m_fieldMin.Value).Volume, 1f / 3f);
            else
                return 0.0f;
        }

        protected void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
			ResourceSink.Update();

            UpdateText();
            UpdateEmissivity();
        }

        protected void Receiver_RequiredInputChanged(MyDefinitionId resourceTypeId, MyResourceSinkComponent receiver, float oldRequirement, float newRequirement)
        {
            UpdateText();
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
            UpdateEmissivity();
        }

        void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) ? ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0, DetailedInfo);
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
            ob.DetectSubgrids = DetectSubgrids;
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
            if(m_syncing)
            {
                return;
            }
            Debug.Assert(self == Toolbar);

            var tItem = ToolbarItem.FromItem(self.GetItemAtIndex(index.ItemIndex));
            var oldItem = m_items[index.ItemIndex];
            if ((tItem.EntityID == 0 && oldItem.EntityID == 0 || (tItem.EntityID != 0 && oldItem.EntityID != 0 && tItem.Equals(oldItem))))
                return;
            m_items.RemoveAt(index.ItemIndex);
            m_items.Insert(index.ItemIndex, tItem);
            MyMultiplayer.RaiseEvent(this, x => x.SendToolbarItemChanged, tItem, index.ItemIndex);

            if (m_shouldSetOtherToolbars)
            {
                m_shouldSetOtherToolbars = false;

                foreach (var toolbar in m_openedToolbars)
                {
                    if (toolbar != self)
                    {
                        toolbar.SetItemAtIndex(index.ItemIndex, self.GetItemAtIndex(index.ItemIndex));
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

        public bool ShouldDetectRelation(VRage.Game.MyRelationsBetweenPlayerAndBlock relation)
        {
            switch (relation)
            {
                case VRage.Game.MyRelationsBetweenPlayerAndBlock.Owner:
                    return DetectOwner;
                    break;
                case VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership:
                case VRage.Game.MyRelationsBetweenPlayerAndBlock.FactionShare:
                    return DetectFriendly;
                    break;
                case VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral:
                    return DetectNeutral;
                    break;
                case VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies:
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
                return ShouldDetectRelation(VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies);
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
            {
                if (entity is Character.MyCharacter)
                    return ShouldDetectRelation((entity as Character.MyCharacter).GetRelationTo(OwnerId));
                if (entity is MyGhostCharacter)
                    return ShouldDetectRelation((entity as IMyControllableEntity).ControllerInfo.Controller.Player.GetRelationTo(OwnerId));
            }
            if (DetectFloatingObjects)
                if (entity is MyFloatingObject)
                    return true;
            
            var grid = entity as MyCubeGrid;
            
            if (DetectSubgrids)
                if (grid != null && MyCubeGridGroups.Static.Logical.HasSameGroup(grid, CubeGrid))
                    return ShouldDetectGrid(grid);

            if (grid != null && MyCubeGridGroups.Static.Logical.HasSameGroup(grid, CubeGrid))
                return false;

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
                if (entity is MyVoxelBase)
                    return true;

            return false;
        }

        bool GetPropertiesFromEntity(MyEntity entity,ref Vector3D position1, out Quaternion rotation2, out Vector3 posDiff, out HkShape? shape2)
        {
            rotation2 = new Quaternion();
            posDiff = Vector3.Zero;
            shape2 = null;
            if (entity.Physics == null || !entity.Physics.Enabled)
            {
                return false;
            }

            if (entity.Physics.RigidBody != null)
            {
                shape2 = entity.Physics.RigidBody.GetShape();

                var worldMatrix = entity.WorldMatrix;
                rotation2 = Quaternion.CreateFromForwardUp(worldMatrix.Forward, worldMatrix.Up);
                posDiff = entity.PositionComp.GetPosition() - position1;
                if (entity is MyVoxelBase)
                {
                    var voxel = entity as MyVoxelBase;
                    posDiff -= voxel.Size / 2;
                }
            }
            else if (entity.GetPhysicsBody().CharacterProxy != null)
            {
                shape2 = entity.GetPhysicsBody().CharacterProxy.GetShape();
                var worldMatrix = entity.WorldMatrix;
                rotation2 = Quaternion.CreateFromForwardUp(worldMatrix.Forward, worldMatrix.Up);
                posDiff = entity.PositionComp.GetPosition() - position1;
            }
            else
            {
                return false;
            }

            return true;
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (!Sync.IsServer || !IsWorking)
                return;

            if (!ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                if(ResourceSink.IsPowerAvailable(MyResourceDistributorComponent.ElectricityId, BlockDefinition.RequiredPowerInput))
                {
                    float origInput = ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId);
                    ResourceSink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, 0);
                    ResourceSink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, origInput);
                }
                else
                {
                    return;
                }
            }

            var rotation1 = Quaternion.CreateFromForwardUp(WorldMatrix.Forward, WorldMatrix.Up);
            var position1 = PositionComp.GetPosition() + Vector3D.Transform(PositionComp.LocalVolume.Center + (m_fieldMax.Value + m_fieldMin.Value) * 0.5f, rotation1);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Recreate Field");
            if (m_recreateField)
            {
                m_recreateField = false;
                m_fieldShape.RemoveReference();
                m_fieldShape = GetHkShape();
				ResourceSink.Update();
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            var boundingBox = new BoundingBoxD(m_fieldMin.Value, m_fieldMax.Value).Translate(PositionComp.LocalVolume.Center).TransformFast(WorldMatrix.GetOrientation()).Translate(PositionComp.GetPosition());
             
            m_potentialPenetrations.Clear();
            MyGamePruningStructure.GetTopMostEntitiesInBox(ref boundingBox, m_potentialPenetrations);

            m_potentialVoxelPenetrations.Clear();
            MyGamePruningStructure.GetAllVoxelMapsInBox(ref boundingBox, m_potentialVoxelPenetrations);//disabled until heightmap queries are finished

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Sensor Physics");
            LastDetectedEntity = null;
            bool empty = true;
            m_detectedEntities.Clear();
            //foreach (var entity in m_potentialPenetrations)
            Parallel.ForEach(m_potentialPenetrations, entity =>
                                                      {
                                                          if (entity is MyVoxelBase)
                                                          {
                                                              //voxels are handled in different loop (becaose of planets)
                                                              return;
                                                          }
                                                          if (ShouldDetect(entity))
                                                          {
                                                              Quaternion rotation2;
                                                              Vector3 posDiff;
                                                              HkShape? shape2;
                                                              if (GetPropertiesFromEntity(entity, ref position1, out rotation2, out posDiff, out shape2))
                                                              {
                                                                  if (entity.GetPhysicsBody().HavokWorld.IsPenetratingShapeShape(m_fieldShape, ref Vector3.Zero, ref rotation1, shape2.Value, ref posDiff, ref rotation2))
                                                                  {
                                                                      if (LastDetectedEntity == null)
                                                                          LastDetectedEntity = entity;
                                                                      empty = false;
                                                                      //entities.Add(entity);
                                                                      var inf = MyDetectedEntityInfoHelper.Create(entity, this.OwnerId);
                                                                      m_detectedEntities.Add(inf);
                                                                  }
                                                              }
                                                          }
                                                      });

            if (DetectAsteroids)
            {
                //foreach (var entity in m_potentialVoxelPenetrations)
                Parallel.ForEach(m_potentialVoxelPenetrations, entity =>
                                                               {
                                                                   var voxel = entity as MyVoxelPhysics;
                                                                   if (voxel != null)
                                                                   {
                                                                       Vector3D localPositionMin, localPositionMax;

                                                                       MyVoxelCoordSystems.WorldPositionToLocalPosition(boundingBox.Min, voxel.PositionComp.WorldMatrix, voxel.PositionComp.WorldMatrixInvScaled, voxel.SizeInMetresHalf, out localPositionMin);
                                                                       MyVoxelCoordSystems.WorldPositionToLocalPosition(boundingBox.Max, voxel.PositionComp.WorldMatrix, voxel.PositionComp.WorldMatrixInvScaled, voxel.SizeInMetresHalf, out localPositionMax);
                                                                       var aabb = new BoundingBox(localPositionMin, localPositionMax);
                                                                       aabb.Translate(voxel.StorageMin);
                                                                       if (voxel.Storage.Intersect(ref aabb) != ContainmentType.Disjoint)
                                                                       {
                                                                           if (LastDetectedEntity == null)
                                                                               LastDetectedEntity = entity;
                                                                           empty = false;
                                                                           //entities.Add(entity);
                                                                           var inf = MyDetectedEntityInfoHelper.Create(entity, this.OwnerId);
                                                                           m_detectedEntities.Add(inf);
                                                                       }
                                                                   }
                                                                   else
                                                                   {
                                                                       Quaternion rotation2;
                                                                       Vector3 posDiff;
                                                                       HkShape? shape2;
                                                                       if (GetPropertiesFromEntity(entity, ref position1, out rotation2, out posDiff, out shape2))
                                                                       {
                                                                           if (entity.GetPhysicsBody().HavokWorld.IsPenetratingShapeShape(m_fieldShape, ref Vector3.Zero, ref rotation1, shape2.Value, ref posDiff, ref rotation2))
                                                                           {
                                                                               if (LastDetectedEntity == null)
                                                                                   LastDetectedEntity = entity;
                                                                               empty = false;
                                                                               //entities.Add(entity);
                                                                               var inf = MyDetectedEntityInfoHelper.Create(entity, this.OwnerId);
                                                                               m_detectedEntities.Add(inf);
                                                                           }
                                                                       }
                                                                   }
                                                               });
            }
            
            IsActive = !empty;
            m_potentialPenetrations.Clear();
            m_potentialVoxelPenetrations.Clear();
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
               GetDistanceBetweenPlayerPositionAndBoundingSphere() > m_maxGizmoDrawDistance)
            {
                return false;
            }
            return true;
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

        void IsActiveChanged()
        {
            if (m_active)
                OnFirstEnter();
            else
                OnLastLeave();

            UpdateEmissivity();

            var handle = StateChanged;
            if (handle != null) handle(m_active);
        }

        [Event,Reliable,Server,Broadcast]
        void SendToolbarItemChanged(ToolbarItem sentItem, int index)
        {
            m_syncing = true;
            MyToolbarItem item = null;
            if (sentItem.EntityID != 0)
            {
                item = ToolbarItem.ToItem(sentItem);
            }
            Toolbar.SetItemAtIndex(index, item);
            m_syncing = false;
        }

        float ModAPI.Ingame.IMySensorBlock.LeftExtend { get { return -m_fieldMin.Value.X; } }
        float ModAPI.Ingame.IMySensorBlock.RightExtend { get { return m_fieldMax.Value.X; } }
        float ModAPI.Ingame.IMySensorBlock.TopExtend { get { return m_fieldMax.Value.Y; } }
        float ModAPI.Ingame.IMySensorBlock.BottomExtend { get { return -m_fieldMin.Value.Y; } }
        float ModAPI.Ingame.IMySensorBlock.FrontExtend { get { return -m_fieldMin.Value.Z; } }
        float ModAPI.Ingame.IMySensorBlock.BackExtend { get { return m_fieldMax.Value.Z; } }
        bool ModAPI.Ingame.IMySensorBlock.PlayProximitySound { get { return PlayProximitySound; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectPlayers { get { return DetectPlayers; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectFloatingObjects { get { return DetectFloatingObjects; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectSmallShips { get { return DetectSmallShips; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectLargeShips { get { return DetectLargeShips; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectStations { get { return DetectStations; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectSubgrids { get { return DetectSubgrids; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectAsteroids { get { return DetectAsteroids; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectOwner { get { return DetectOwner; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectFriendly { get { return DetectFriendly; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectNeutral { get { return DetectNeutral; } }
        bool ModAPI.Ingame.IMySensorBlock.DetectEnemy { get { return DetectEnemy; } }
        bool ModAPI.Ingame.IMySensorBlock.IsActive { get { return IsActive; } }
        MyDetectedEntityInfo ModAPI.Ingame.IMySensorBlock.LastDetectedEntity { get { return MyDetectedEntityInfoHelper.Create(LastDetectedEntity, this.OwnerId); } }
        
        void ModAPI.Ingame.IMySensorBlock.DetectedEntities(List<MyDetectedEntityInfo> result)
        {
            result.AddRange(m_detectedEntities);
    }
    }
}
