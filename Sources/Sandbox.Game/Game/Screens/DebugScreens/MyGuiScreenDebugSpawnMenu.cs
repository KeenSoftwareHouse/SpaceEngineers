using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Serializer;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using VRage;
using VRage.Input;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRage.Library.Utils;
using VRage.FileSystem;

namespace Sandbox.Game.Gui
{
    [PreloadRequiredAttribute]
    class MyGuiScreenDebugSpawnMenu : MyGuiScreenDebugBase
    {
        [ProtoContract]
        [MessageIdAttribute(3242, P2PMessageEnum.Reliable)]
        protected struct SpawnAsteroidMsg
        {
            [ProtoMember]
            public string Asteroid;
            [ProtoMember]
            public int RandomSeed;
            [ProtoMember]
            public Vector3D Position;
            [ProtoMember]
            public string StorageName;
            [ProtoMember]
            public bool IsProcedural;
            [ProtoMember]
            public float ProceduralRadius;

            public override string ToString()
            {
                return String.Format("{0}", this.GetType().Name);
            }
        }

        [ProtoContract]
        [MessageIdAttribute(3243, P2PMessageEnum.Reliable)]
        protected struct SpawnAsteroidConfirmedMsg
        {
            [ProtoMember]
            public SpawnAsteroidMsg AsteroidDetails;

            [ProtoMember]
            public long EntityId;
        }

        const float MIN_DEVIATION = 0.003f;
        const float MAX_DEVIATION = 0.5f;

        private static readonly Vector2 SCREEN_SIZE = new Vector2(0.40f, 1.2f);
        private static readonly float HIDDEN_PART_RIGHT = 0.04f;

        private MyGuiControlCombobox m_asteroidCombobox;
        private string m_selectedCoreVoxelFile;
        private MyGuiControlCombobox m_physicalObjectCombobox;
        private static int m_lastSelectedFloatingObjectIndex = 0;
        private static int m_lastSelectedAsteroidIndex = 0;
        private List<MyPhysicalItemDefinition> m_physicalItemDefinitions = new List<MyPhysicalItemDefinition>();
        private MyGuiControlTextbox m_amountTextbox;
        private MyGuiControlLabel m_errorLabel;
        private static long m_amount = 1;
        private static int m_asteroidCounter = 0;
        private static SpawnAsteroidMsg m_lastAsteroidMsg;

        private static bool m_asteroid_showPredefinedOrProcedural = true;
        private static bool m_asteroid_showPlanet = false;
        private static float m_procAsteroidSizeValue = 64.0f;
        private static float m_planetDeviationScaleValue = 0.003f;
        private static float m_planetHillRatioValue = 3.0f;
        private static float m_planetCanyonRatioValue = 1.0f;

        private static String m_procAsteroidSeedValue = "12345";
        private MyGuiControlSlider m_procAsteroidSize;
        private MyGuiControlTextbox m_procAsteroidSeed;
        private MyGuiControlSlider m_normalNoiseFrequency;
        private MyGuiControlSlider m_procAsteroidDeviationScale;

        private MyGuiControlCombobox m_oreCombobox;
        private MyGuiControlLabel m_oreComboboxLabel;    
        private MyGuiControlCombobox m_layerCombobox;
        private MyGuiControlSlider m_materialLayerDeviationNoise;
        private static String m_materialLayerDeviationSeedValue = "12345";
        private MyGuiControlTextbox m_materialLayerDeviationSeed;

        private MyGuiControlSlider m_materialLayerStart;
        private MyGuiControlSlider m_materialLayerEnd;
        private MyGuiControlSlider m_materialLayerStartHeigthDeviation;
        private MyGuiControlSlider m_materialLayerEndHeigthDeviation;
        private MyGuiControlSlider m_materialLayerAngleStart;
        private MyGuiControlSlider m_materialLayerAngleEnd;
        private MyGuiControlSlider m_materialLayerAngleStartDeviation;
        private MyGuiControlSlider m_materialLayerAngleEndDeviation;

        private MyGuiControlSlider m_planetStructureRatio;
        private MyGuiControlSlider m_planetHillTreshold;
        private MyGuiControlSlider m_planetHillBlendTreshold;
        private MyGuiControlSlider m_planetHillSizeRatio;
        private MyGuiControlSlider m_planetHillFrequency;
        private MyGuiControlSlider m_planetHillNumNoises;

        private MyGuiControlSlider m_planetCanyonTreshold;
        private MyGuiControlSlider m_planetCanyonBlendTreshold;
        private MyGuiControlSlider m_planetCanyonSizeRatio;
        private MyGuiControlSlider m_planetCanyonFrequency;
        private MyGuiControlSlider m_planetCanyonNumNoises;

        private MyGuiControlCheckbox m_planetAtmosphere;


        private MyVoxelBase m_currentVoxel = null;

        private List<MyMaterialLayer> m_materialLayers = new List<MyMaterialLayer>();

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugSpawnMenu";
        }

        static MyGuiScreenDebugSpawnMenu()
        {
            MySyncLayer.RegisterMessage<SpawnAsteroidMsg>(SpawnAsteroidSuccess, MyMessagePermissions.ToServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<SpawnAsteroidConfirmedMsg>(SpawnAsteroidConfirmedSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        static void SpawnAsteroidSuccess(ref SpawnAsteroidMsg msg, MyNetworkClient sender)
        {
            SpawnAsteroidConfirmedMsg response;
            response.AsteroidDetails = msg;
            response.EntityId = MyEntityIdentifier.AllocateId();
            Sync.Layer.SendMessageToAllAndSelf(ref response, MyTransportMessageEnum.Success);
        }

        static void SpawnAsteroidConfirmedSuccess(ref SpawnAsteroidConfirmedMsg msg, MyNetworkClient sender)
        {
            MyStorageBase storage;
            SpawnAsteroidMsg asteroid = msg.AsteroidDetails;

            string storageName;
            using (MyRandom.Instance.PushSeed(asteroid.RandomSeed))
            {
                if (!asteroid.IsProcedural)
                {
                    var storageNameBase = asteroid.StorageName ?? (asteroid.Asteroid + "-" + asteroid.RandomSeed);
                    storageName = MakeStorageName(storageNameBase);
                    storage = CreateAsteroidStorage(asteroid.Asteroid, asteroid.RandomSeed);
                }
                else
                {
                    var storageNameBase = asteroid.StorageName ?? "ProcAsteroid" + "-" + asteroid.RandomSeed + "r" + asteroid.ProceduralRadius;
                    storageName = MakeStorageName(storageNameBase);
                    storage = CreateProceduralAsteroidStorage(asteroid.RandomSeed, asteroid.ProceduralRadius, 0.03f);
                }
            }

            var pastedVoxelMap = new MyVoxelMap();
            pastedVoxelMap.EntityId = msg.EntityId;
            pastedVoxelMap.Init(storageName, storage, asteroid.Position - storage.Size * 0.5f);
            MyEntities.Add(pastedVoxelMap);
        }

        public static void SendAsteroid(Vector3D position)
        {
            m_lastAsteroidMsg.Position = position;
            Sync.Layer.SendMessageToServer(ref m_lastAsteroidMsg, MyTransportMessageEnum.Success);
        }

        public MyGuiScreenDebugSpawnMenu() :
            base(new Vector2(MyGuiManager.GetMaxMouseCoord().X - SCREEN_SIZE.X * 0.5f + HIDDEN_PART_RIGHT, 0.5f), SCREEN_SIZE, MyGuiConstants.SCREEN_BACKGROUND_COLOR, false)
        {
            CanBeHidden = true;
            CanHideOthers = false;
            m_canCloseInCloseAllScreenCalls = true;
            m_canShareInput = true;
            m_isTopScreen = false;
            m_isTopMostScreen = false;

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            if (m_asteroid_showPlanet)
            {
                CreatePlanetMenu();
                return;
            }

            Vector2 cbOffset = new Vector2(-0.05f, 0.0f);
            Vector2 controlPadding = new Vector2(0.02f, 0.02f); // X: Left & Right, Y: Bottom & Top

            float textScale = 0.8f;
            float separatorSize = 0.01f;
            float usableWidth = SCREEN_SIZE.X - HIDDEN_PART_RIGHT - controlPadding.X * 2;
            float hiddenPartTop = (SCREEN_SIZE.Y - 1.0f) / 2.0f;

            m_currentPosition = -m_size.Value / 2.0f;
            m_currentPosition += controlPadding;
            m_currentPosition.Y += hiddenPartTop;
            m_scale = textScale;

            var caption = AddCaption(MySpaceTexts.ScreenDebugSpawnMenu_Caption, Color.White.ToVector4(), controlPadding + new Vector2(-HIDDEN_PART_RIGHT, hiddenPartTop));
            m_currentPosition.Y += MyGuiConstants.SCREEN_CAPTION_DELTA_Y + separatorSize;

            if (MyFakes.ENABLE_SPAWN_MENU_ASTEROIDS || MyFakes.ENABLE_SPAWN_MENU_PROCEDURAL_ASTEROIDS)
            {
                AddSubcaption(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_Asteroids), Color.White.ToVector4(), new Vector2(-HIDDEN_PART_RIGHT, 0.0f));
            }

            if (MyFakes.ENABLE_SPAWN_MENU_ASTEROIDS && MyFakes.ENABLE_SPAWN_MENU_PROCEDURAL_ASTEROIDS)
            {
                AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_SelectAsteroidType), Vector4.One, m_scale);
                var combo = AddCombo();
                combo.AddItem(1, MySpaceTexts.ScreenDebugSpawnMenu_PredefinedAsteroids);
                combo.AddItem(2, MySpaceTexts.ScreenDebugSpawnMenu_ProceduralAsteroids);
                // DA: Remove from MySpaceTexts and just hardcode until release. Leave a todo so you don't forget about it before release of planets.
                combo.AddItem(3, MySpaceTexts.ScreenDebugSpawnMenu_Planets);

                combo.SelectItemByKey(m_asteroid_showPlanet ? 3 : m_asteroid_showPredefinedOrProcedural ? 1 : 2);
                combo.ItemSelected += () => { m_asteroid_showPredefinedOrProcedural = combo.GetSelectedKey() == 1; m_asteroid_showPlanet = combo.GetSelectedKey() == 3; RecreateControls(false); };
            }

            if (MyFakes.ENABLE_SPAWN_MENU_ASTEROIDS && m_asteroid_showPredefinedOrProcedural)
            {
                AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_Asteroid), Vector4.One, m_scale);
                m_asteroidCombobox = AddCombo();
                {
                    foreach (var definition in MyDefinitionManager.Static.GetVoxelMapStorageDefinitions())
                    {
                        m_asteroidCombobox.AddItem((int)definition.Id.SubtypeId, definition.Id.SubtypeId);
                    }
                    m_asteroidCombobox.ItemSelected += OnAsteroidCombobox_ItemSelected;
                    m_asteroidCombobox.SortItemsByValueText();
                    m_asteroidCombobox.SelectItemByIndex(m_lastSelectedAsteroidIndex);
                }

                m_currentPosition.Y += separatorSize;

                AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_AsteroidGenerationCanTakeLong), Color.Red.ToVector4(), m_scale);
                CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_SpawnAsteroid, OnLoadAsteroid);

                m_currentPosition.Y += separatorSize;
            }

            if (MyFakes.ENABLE_SPAWN_MENU_PROCEDURAL_ASTEROIDS && !m_asteroid_showPredefinedOrProcedural)
            {
                AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_ProceduralSize), Vector4.One, m_scale);


                m_procAsteroidSize = new MyGuiControlSlider(
                    position: m_currentPosition,
                    width: 400f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    minValue: 5.0f,
                    maxValue: 500f,
                    labelText: String.Empty,
                    labelDecimalPlaces: 2,
                    labelScale: 0.75f * m_scale,
                    originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    labelFont: MyFontEnum.Debug);
                m_procAsteroidSize.DebugScale = m_sliderDebugScale;
                m_procAsteroidSize.ColorMask = Color.White.ToVector4();
                Controls.Add(m_procAsteroidSize);

                MyGuiControlLabel label = new MyGuiControlLabel(
                    position: m_currentPosition + new Vector2(m_procAsteroidSize.Size.X + 0.005f, m_procAsteroidSize.Size.Y / 2),
                    text: String.Empty,
                    colorMask: Color.White.ToVector4(),
                    textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.8f * m_scale,
                    font: MyFontEnum.Debug);
                label.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                Controls.Add(label);
                m_procAsteroidSize.ValueChanged += (MyGuiControlSlider s) => { label.Text = MyValueFormatter.GetFormatedFloat(s.Value, 2) + "m"; m_procAsteroidSizeValue = s.Value; };

                m_procAsteroidSize.Value = m_procAsteroidSizeValue;

                m_currentPosition.Y += m_procAsteroidSize.Size.Y;
                m_currentPosition.Y += separatorSize;

                AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_ProceduralSeed), Color.White.ToVector4(), m_scale);

                m_procAsteroidSeed = new MyGuiControlTextbox(m_currentPosition, m_procAsteroidSeedValue, 20, Color.White.ToVector4(), m_scale, MyGuiControlTextboxType.Normal);
                m_procAsteroidSeed.TextChanged += (MyGuiControlTextbox t) => { m_procAsteroidSeedValue = t.Text; };
                m_procAsteroidSeed.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                Controls.Add(m_procAsteroidSeed);
                m_currentPosition.Y += m_procAsteroidSize.Size.Y + separatorSize;

                CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_GenerateSeed, generateSeedButton_OnButtonClick);
                AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_AsteroidGenerationCanTakeLong), Color.Red.ToVector4(), m_scale);
                CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_SpawnAsteroid, OnSpawnProceduralAsteroid);

                m_currentPosition.Y += separatorSize;

            }

            CreateObjectsSpawnMenu(separatorSize, usableWidth);
        }

        private void CreateObjectsSpawnMenu(float separatorSize, float usableWidth)
        {
            AddSubcaption(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_Items), Color.White.ToVector4(), new Vector2(-HIDDEN_PART_RIGHT, 0.0f));

            AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_ItemType), Vector4.One, m_scale);
            m_physicalObjectCombobox = AddCombo();
            {
                foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
                {
                    if (!definition.Public)
                        continue;
                    var physicalItemDef = definition as MyPhysicalItemDefinition;
                    if (physicalItemDef == null)
                        continue;

                    int key = m_physicalItemDefinitions.Count;
                    m_physicalItemDefinitions.Add(physicalItemDef);
                    m_physicalObjectCombobox.AddItem(key, definition.DisplayNameText);
                }
                m_physicalObjectCombobox.SortItemsByValueText();
                m_physicalObjectCombobox.SelectItemByIndex(m_lastSelectedFloatingObjectIndex);
                m_physicalObjectCombobox.ItemSelected += OnPhysicalObjectCombobox_ItemSelected;
            }

            m_currentPosition.Y += separatorSize;

            AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_ItemAmount), Vector4.One, m_scale);
            m_amountTextbox = new MyGuiControlTextbox(m_currentPosition, m_amount.ToString(), 6, null, m_scale, MyGuiControlTextboxType.DigitsOnly);
            m_amountTextbox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_amountTextbox.TextChanged += OnAmountTextChanged;
            Controls.Add(m_amountTextbox);

            m_currentPosition.Y += separatorSize + m_amountTextbox.Size.Y;
            m_errorLabel = AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_InvalidAmount), Color.Red.ToVector4(), m_scale);
            m_errorLabel.Visible = false;
            CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_SpawnObject, OnSpawnPhysicalObject);

            m_currentPosition.Y += separatorSize;
        }

        static private float NormalizeLog(float f, float min, float max)
        {
            return MathHelper.Clamp(MathHelper.InterpLogInv(f, min, max), 0, 1);
        }

        static private float DenormalizeLog(float f, float min, float max)
        {
            return MathHelper.Clamp(MathHelper.InterpLog(f, min, max), min, max);
        }

        private void UpdateLayerSlider(MyGuiControlSlider slider, float minValue,float maxValue)
        {
            slider.Value = MathHelper.Max(minValue, MathHelper.Min(slider.Value, maxValue));
            slider.MaxValue = maxValue;
            slider.MinValue = minValue;
        }

        private void OnPlanetDeviationChanged(MyGuiControlSlider slider)
        {
            float value = DenormalizeLog(slider.Value, MIN_DEVIATION, MAX_DEVIATION);

            float min = -m_procAsteroidSize.Value * value * m_planetCanyonRatioValue/2.0f;
            float max = m_procAsteroidSize.Value * value * m_planetHillRatioValue / 2.0f; 

            UpdateLayerSlider(m_materialLayerStart,min,max);
            UpdateLayerSlider(m_materialLayerEnd,min,max);          
        }

        private void OnPlanetSizeChanged(MyGuiControlSlider slider)
        {
            float min = -m_planetDeviationScaleValue * slider.Value * m_planetCanyonRatioValue / 2.0f;
            float max = m_planetDeviationScaleValue * slider.Value * m_planetHillRatioValue / 2.0f;
            UpdateLayerSlider(m_materialLayerStart,min,max);
            UpdateLayerSlider(m_materialLayerEnd,min,max);
        }

        private void OnPlanetHillRatioChanged(MyGuiControlSlider slider)
        {
            float min = -m_planetDeviationScaleValue * m_procAsteroidSize.Value * m_planetCanyonRatioValue / 2.0f;
            float max = m_planetDeviationScaleValue * m_procAsteroidSize.Value * slider.Value / 2.0f;    
            UpdateLayerSlider(m_materialLayerStart,min,max);
            UpdateLayerSlider(m_materialLayerEnd, min,max);
        }

        private void OnPlanetCanyonRatioChanged(MyGuiControlSlider slider)
        {
            float min = -m_planetDeviationScaleValue * m_procAsteroidSize.Value * slider.Value / 2.0f;
            float max = m_planetDeviationScaleValue * m_procAsteroidSize.Value * m_planetHillRatioValue / 2.0f;
            UpdateLayerSlider(m_materialLayerStart, min, max);
            UpdateLayerSlider(m_materialLayerEnd, min, max);
        }

        private void OnAmountTextChanged(MyGuiControlTextbox textbox)
        {
            m_errorLabel.Visible = false;
        }

        private bool IsValidAmount()
        {
            if (long.TryParse(m_amountTextbox.Text, out m_amount))
            {
                if (m_amount < 1)
                    return false;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnLayerCombobox_ItemSelected()
        {
            m_oreCombobox.Visible = true;
            m_oreComboboxLabel.Visible = true;

            m_materialLayerEnd.Visible = true;
            m_materialLayerStart.Visible = true;


            int currentLayer = m_layerCombobox.GetSelectedIndex();
            float temp = m_materialLayers[currentLayer].EndHeight;
            m_materialLayerStart.Value = m_materialLayers[currentLayer].StartHeight;
            m_materialLayerEnd.Value = temp;
            m_materialLayerStartHeigthDeviation.Value = m_materialLayers[currentLayer].HeightStartDeviation;
            m_materialLayerEndHeigthDeviation.Value = m_materialLayers[currentLayer].HeightEndDeviation;

            temp = m_materialLayers[currentLayer].EndAngle;
            m_materialLayerAngleStart.Value = m_materialLayers[currentLayer].StartAngle;
            m_materialLayerAngleEnd.Value = temp;
            m_materialLayerAngleStartDeviation.Value = m_materialLayers[currentLayer].AngleStartDeviation;
            m_materialLayerAngleEndDeviation.Value = m_materialLayers[currentLayer].AngleEndDeviation;

            int id = -1;
            foreach (var definition in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
            {
                if (definition.Id.SubtypeName == m_materialLayers[currentLayer].MaterialName)
                {
                    id = (int)definition.Id.SubtypeId;
                }
            }
            if (id != -1)
            {
                m_oreCombobox.SelectItemByKey(id);
            }
        }

        private void OnAsteroidCombobox_ItemSelected()
        {
            m_lastSelectedAsteroidIndex = m_asteroidCombobox.GetSelectedIndex();
            m_selectedCoreVoxelFile = m_asteroidCombobox.GetSelectedValue().ToString();
        }

        private void OnOreCombobox_ItemSelected()
        {
            int currentLayer = m_layerCombobox.GetSelectedIndex();
            m_materialLayers[currentLayer].MaterialName = m_oreCombobox.GetSelectedValue().ToString();
        }

        private void OnPhysicalObjectCombobox_ItemSelected()
        {
            m_lastSelectedFloatingObjectIndex = m_physicalObjectCombobox.GetSelectedIndex();
        }

        private void SpawnFloatingObjectPreview()
        {
            var itemId = m_physicalItemDefinitions[(int)m_physicalObjectCombobox.GetSelectedKey()].Id;

            MyFixedPoint amount = (MyFixedPoint)(float)m_amount;

            var builder = (MyObjectBuilder_PhysicalObject)Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject(itemId);

            if (builder is MyObjectBuilder_PhysicalGunObject || builder is Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_OxygenContainerObject)
                amount = 1;

            var obj = Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_FloatingObject>();
            obj.PositionAndOrientation = MyPositionAndOrientation.Default;
            obj.Item = Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_InventoryItem>();
            obj.Item.Amount = amount;
            obj.Item.Content = builder;

            MyCubeBuilder.Static.ActivateFloatingObjectClipboard(obj, Vector3.Zero, 1f);
        }

        private MyGuiControlButton CreateDebugButton(float usableWidth, MyStringId text, Action<MyGuiControlButton> onClick, bool enabled = true, MyStringId? tooltip = null)
        {
            var button = AddButton(MyTexts.Get(text), onClick);
            button.VisualStyle = Common.ObjectBuilders.Gui.MyGuiControlButtonStyleEnum.Rectangular;
            button.TextScale = m_scale;
            button.Size = new Vector2(usableWidth, button.Size.Y);
            button.Position = button.Position + new Vector2(-HIDDEN_PART_RIGHT / 2.0f, 0.0f);
            button.Enabled = enabled;
            if (tooltip != null)
            {
                button.SetToolTip(tooltip.Value);
            }
            return button;
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.F12) || MyInput.Static.IsNewKeyPressed(MyKeys.F11) || MyInput.Static.IsNewKeyPressed(MyKeys.F10))
            {
                this.CloseScreen();
            }
        }

        private static Matrix GetPasteMatrix()
        {
            if (MySession.ControlledEntity != null &&
                (MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Entity || MySession.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator))
            {
                return MySession.ControlledEntity.GetHeadMatrix(true);
            }
            else
            {
                return MySector.MainCamera.WorldMatrix;
            }
        }

        private void OnSpawnPhysicalObject(MyGuiControlButton obj)
        {
            if (!IsValidAmount())
            {
                m_errorLabel.Visible = true;
                return;
            }

            SpawnFloatingObjectPreview();
            CloseScreenNow();
        }

        private void OnLoadAsteroid(MyGuiControlButton obj)
        {
            SpawnVoxelPreview();
            CloseScreenNow();
        }

        private void OnCreateLayer(MyGuiControlButton obj)
        {
            m_materialLayers.Add(new MyMaterialLayer());

            m_layerCombobox.AddItem(m_materialLayers.Count, MyStringId.GetOrCompute("layer" + m_materialLayers.Count.ToString()));

            m_layerCombobox.SelectItemByIndex(m_materialLayers.Count - 1);
        }

        private void OnRemoveLayer(MyGuiControlButton obj)
        {
            int selectedIndex = m_layerCombobox.GetSelectedIndex();
            if (selectedIndex >= 0 && selectedIndex < m_materialLayers.Count)
            {
                m_materialLayers.RemoveAt(selectedIndex);
                m_layerCombobox.ClearItems();

                for (int i = 0; i < m_materialLayers.Count; ++i)
                {
                    m_layerCombobox.AddItem(i, MyStringId.GetOrCompute("layer" + i.ToString()));
                }
            }
        }

        private void OnSpawnProceduralAsteroid(MyGuiControlButton obj)
        {
            int seed = GetProceduralAsteroidSeed(m_procAsteroidSeed);
            if (m_asteroid_showPlanet)
            {
                MyCsgShapePlanetShapeAttributes planetShapeAttributes;
                MyCsgShapePlanetHillAttributes hillAttributes;
                MyCsgShapePlanetHillAttributes canyonAttributes;
                GetPlanetAttributes(out planetShapeAttributes, out hillAttributes, out canyonAttributes);

                SpawnPlanet(ref planetShapeAttributes, ref hillAttributes, ref canyonAttributes, m_materialLayers);
            }
            else
            {
                SpawnProceduralAsteroid(seed, m_procAsteroidSize.Value);
            }

            CloseScreenNow();
        }

        private void generateSeedButton_OnButtonClick(MyGuiControlButton sender)
        {
            m_procAsteroidSeed.Text = MyRandom.Instance.Next().ToString();
        }

        private int GetProceduralAsteroidSeed(MyGuiControlTextbox textbox)
        {
            int seed = 12345;
            if (!Int32.TryParse(textbox.Text, out seed))
            {
                //if user didn't passed right seed we will try to calculate it from string  : )
                String text = textbox.Text;

                HashAlgorithm algorithm = SHA1.Create();
                byte[] bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(text));
                //hash is too big to store it in int, so we take only first four bytes - it's more than fine, though
                int shift = 0;
                for (int i = 0; i < 4 && i < bytes.Length; ++i)
                {
                    seed |= bytes[i] << shift;
                    shift += 8;
                }
            }
            return seed;
        }

        public static MyStorageBase CreateAsteroidStorage(string asteroid, int randomSeed)
        {
            MyVoxelMapStorageDefinition definition;
            if (MyDefinitionManager.Static.TryGetVoxelMapStorageDefinition(asteroid, out definition))
            {
                if (definition.Context.IsBaseGame)
                {
                    return MyStorageBase.LoadFromFile(Path.Combine(MyFileSystem.ContentPath, definition.StorageFile));
                }
                else
                {
                    return MyStorageBase.LoadFromFile(definition.StorageFile);
                }
            }

            return null;
        }

        public static MyStorageBase CreatePlanetStorage(
            ref MyCsgShapePlanetShapeAttributes shapeAttributes,
            ref MyCsgShapePlanetHillAttributes hillAttributes,
            ref MyCsgShapePlanetHillAttributes canyonAttributes,
            MyMaterialLayer[] materialLayers)
        {
            //return new MyOctreeStorage(
            //    MyCompositeShapeProvider.CreatePlanetShape(0, ref shapeAttributes, ref hillAttributes, ref canyonAttributes, materialLayers),
            //    FindBestOctreeSize(shapeAttributes.Radius));
            return null;
        }

        public static MyStorageBase CreateProceduralAsteroidStorage(int seed, float radius, float deviationScale)
        {
            return new MyOctreeStorage(MyCompositeShapeProvider.CreateAsteroidShape(seed, radius, 0), FindBestOctreeSize(radius));
        }

        public static MyObjectBuilder_VoxelMap CreateAsteroidObjectBuilder(string storageName)
        {
            var builder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_VoxelMap>();
            builder.StorageName = storageName;
            builder.PersistentFlags = MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;
            builder.PositionAndOrientation = MyPositionAndOrientation.Default;
            builder.MutableStorage = false;
            return builder;
        }

        private void SpawnVoxelPreview()
        {
            var randomSeed = MyRandom.Instance.CreateRandomSeed();

            using (MyRandom.Instance.PushSeed(randomSeed))
            {
                var storageNameBase = m_selectedCoreVoxelFile + "-" + randomSeed;
                String storageName = MakeStorageName(storageNameBase);

                var storage = CreateAsteroidStorage(m_selectedCoreVoxelFile, randomSeed);
                var builder = CreateAsteroidObjectBuilder(storageName);

                m_lastAsteroidMsg = new SpawnAsteroidMsg()
                {
                    Asteroid = m_selectedCoreVoxelFile.ToString(),
                    RandomSeed = randomSeed,
                    Position = Vector3D.Zero,
                    StorageName = storageName,
                    IsProcedural = false
                };

                MyCubeBuilder.Static.ActivateVoxelClipboard(builder, storage, MySector.MainCamera.ForwardVector, (storage.Size * 0.5f).Length());
            }
        }

        public static MyStorageBase CreateProceduralAsteroidStorage(int seed, float radius)
        {
            return new MyOctreeStorage(MyCompositeShapeProvider.CreateAsteroidShape(seed, radius, 0), FindBestOctreeSize(radius));
        }

        private void SpawnProceduralAsteroid(int seed, float radius)
        {
            var storageNameBase = "ProcAsteroid" + "-" + seed + "r" + radius;
            var storageName = MakeStorageName(storageNameBase);

            var storage = CreateProceduralAsteroidStorage(seed, radius);
            var builder = CreateAsteroidObjectBuilder(storageName);

            m_lastAsteroidMsg = new SpawnAsteroidMsg()
            {
                RandomSeed = seed,
                Position = Vector3D.Zero,
                StorageName = storageName,
                IsProcedural = true,
                ProceduralRadius = radius
            };

            MyCubeBuilder.Static.ActivateVoxelClipboard(builder, storage, MySector.MainCamera.ForwardVector, (storage.Size * 0.5f).Length());
        }

        private void SpawnPlanet(ref MyCsgShapePlanetShapeAttributes planetShapeAttributes, ref MyCsgShapePlanetHillAttributes hillAttributes, ref MyCsgShapePlanetHillAttributes canyonAttributes, List<MyMaterialLayer> layers, Vector3D? pos = null)
        {
          
        }

        private static String MakeStorageName(String storageNameBase)
        {
            String storageName = storageNameBase;

            int i = 0;

            bool collision;
            do
            {
                collision = false;
                foreach (var voxelMap in MySession.Static.VoxelMaps.Instances)
                {
                    if (voxelMap.StorageName == storageName)
                    {
                        collision = true;
                        break;
                    }
                }

                if (collision)
                {
                    storageName = storageNameBase + "-" + i++;
                }
            }
            while (collision);

            return storageName;
        }

        private static Vector3I FindBestOctreeSize(float radius)
        {
            int nodeRadius = MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS;
            while (nodeRadius < radius)
                nodeRadius *= 2;
            //nodeRadius *= 2;
            return new Vector3I(nodeRadius, nodeRadius, nodeRadius);
        }

        private void CreatePlanetMenu()
        {
            m_currentVoxel = null;
            MyGuiControlList list = new MyGuiControlList(size: new Vector2(SCREEN_SIZE.X,1.0f));

            Vector2 controlPadding = new Vector2(0.02f, 0.02f); // X: Left & Right, Y: Bottom & Top

            float textScale = 0.8f;
            float usableWidth = SCREEN_SIZE.X - HIDDEN_PART_RIGHT - controlPadding.X * 2;

            m_currentPosition = Vector2.Zero;/* -m_size.Value / 2.0f;
            m_currentPosition += controlPadding;*/
            m_scale = textScale;

            var label = AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_SelectAsteroidType), Vector4.One, m_scale);
            Controls.Remove(label);
            list.Controls.Add(label);

            var combo = AddCombo();
            combo.AddItem(1, MySpaceTexts.ScreenDebugSpawnMenu_PredefinedAsteroids);
            combo.AddItem(2, MySpaceTexts.ScreenDebugSpawnMenu_ProceduralAsteroids);
            combo.AddItem(3, MySpaceTexts.ScreenDebugSpawnMenu_Planets);

            combo.SelectItemByKey(m_asteroid_showPlanet ? 3 : m_asteroid_showPredefinedOrProcedural ? 1 : 2);
            combo.ItemSelected += () => { m_asteroid_showPredefinedOrProcedural = combo.GetSelectedKey() == 1; m_asteroid_showPlanet = combo.GetSelectedKey() == 3; RecreateControls(false); };

            Controls.Remove(combo);
            list.Controls.Add(combo);

            CreatePlanetControls(list, usableWidth);

            AddSeparator(list);

            CreatePlanetHillControlls(list, usableWidth);

            AddSeparator(list);

            CreatePlanetCanyonControlls(list, usableWidth);

            AddSeparator(list);

            CreateLayersControls(list, usableWidth);

            AddSeparator(list);
  
            var button = CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_SpawnAsteroid, OnSpawnProceduralAsteroid); 
            Controls.Remove(button);
            list.Controls.Add(button);

            button = CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_PickPlanet, PickPlanet);
            Controls.Remove(button);
            list.Controls.Add(button);

            button = CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_UpdatePlanet, UpatePlanet);
            Controls.Remove(button);
            list.Controls.Add(button);
            Controls.Add(list);
        }

        private void CreatePlanetControls(MyGuiControlList list, float usableWidth)
        {
            var labelDeviation = CreateSliderWithDescription(list, usableWidth, 0f, 1f, "Planet deviation scale", ref m_procAsteroidDeviationScale);
               
            m_procAsteroidDeviationScale.ValueChanged += (MyGuiControlSlider s) =>
            {
                float value = DenormalizeLog(s.Value, MIN_DEVIATION, MAX_DEVIATION);
                labelDeviation.Text = MyValueFormatter.GetFormatedFloat(value * 100.0f, 3) + "%";
                m_planetDeviationScaleValue = value;
            };
            m_procAsteroidDeviationScale.Value = NormalizeLog(0.003f, MIN_DEVIATION, MAX_DEVIATION);
            labelDeviation.Text = MyValueFormatter.GetFormatedFloat(0.003f * 100.0f, 3) + "%";
            m_procAsteroidDeviationScale.ValueChanged += OnPlanetDeviationChanged;

            var asteroidSizeLabel = CreateSliderWithDescription(list, usableWidth, 8000f, 50000f, MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_ProceduralSize), ref m_procAsteroidSize);

            m_procAsteroidSize.ValueChanged += (MyGuiControlSlider s) => { asteroidSizeLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 0) + "m"; m_procAsteroidSizeValue = s.Value; };
            m_procAsteroidSize.Value = 8000.1f;
            m_procAsteroidSize.ValueChanged += OnPlanetSizeChanged;

            var labelNoise = CreateSliderWithDescription(list, usableWidth, 0.1f, 5f, "Planet structures ratio", ref m_planetStructureRatio);
            m_planetStructureRatio.ValueChanged += (MyGuiControlSlider s) =>
            {
                labelNoise.Text = MyValueFormatter.GetFormatedFloat(s.Value, 2);
            };
            m_planetStructureRatio.Value = 1f;

            var labelNormalNoise = CreateSliderWithDescription(list, usableWidth, 0.1f, 10f, "Planet normal noise ratio", ref m_normalNoiseFrequency);
            m_normalNoiseFrequency.ValueChanged += (MyGuiControlSlider s) =>
            {
                labelNormalNoise.Text = MyValueFormatter.GetFormatedFloat(s.Value, 2);
            };
            m_normalNoiseFrequency.Value = 1f;

            m_procAsteroidSeed = CreateSeedButton(list, m_procAsteroidSeedValue, usableWidth);
        
            var label = AddLabel("Atmosphere", Color.White.ToVector4(), m_scale);
            Controls.Remove(label);
            list.Controls.Add(label);

            m_planetAtmosphere = new MyGuiControlCheckbox(m_currentPosition);
            m_planetAtmosphere.IsChecked = true;
            list.Controls.Add(m_planetAtmosphere);
        }

        private MyGuiControlTextbox CreateSeedButton(MyGuiControlList list, string seedValue, float usableWidth)
        {
            var label = AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_ProceduralSeed), Color.White.ToVector4(), m_scale);
            Controls.Remove(label);
            list.Controls.Add(label);

           var textBox = new MyGuiControlTextbox(m_currentPosition, seedValue, 20, Color.White.ToVector4(), m_scale, MyGuiControlTextboxType.Normal);
            textBox.TextChanged += (MyGuiControlTextbox t) => { seedValue = t.Text; };
            textBox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            list.Controls.Add(textBox);

            var button = CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_GenerateSeed, (MyGuiControlButton buttonClicked) => { textBox.Text = MyRandom.Instance.Next().ToString(); });
            Controls.Remove(button);
            list.Controls.Add(button);
            return textBox;
        }

        private static void AddSeparator(MyGuiControlList list)
        {
            var separator = new MyGuiControlSeparatorList();
            separator.Size = new Vector2(1, 0.01f);
            separator.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            separator.AddHorizontal(Vector2.Zero, 1);

            list.Controls.Add(separator);
        }

        private void CreateLayersControls(MyGuiControlList list, float usableWidth)
        {
            var button = CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_CreateLayer, OnCreateLayer);
            Controls.Remove(button);
            list.Controls.Add(button);

            button = CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_RemoveLayer, OnRemoveLayer);
            Controls.Remove(button);
            list.Controls.Add(button);

            m_materialLayerDeviationSeed = CreateSeedButton(list, m_materialLayerDeviationSeedValue, usableWidth);

            var layerNoiseLabel = CreateSliderWithDescription(list, usableWidth, 10f, 200.0f, "Layer deviation noise frequency", ref m_materialLayerDeviationNoise);
            m_materialLayerDeviationNoise.ValueChanged += (MyGuiControlSlider s) =>
            {
                layerNoiseLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 2);
            };

            m_layerCombobox = AddCombo();
            m_layerCombobox.ItemSelected += OnLayerCombobox_ItemSelected;
            Controls.Remove(m_layerCombobox);
            list.Controls.Add(m_layerCombobox);

            m_oreComboboxLabel = AddLabel("Layer ore", Vector4.One, m_scale);
            Controls.Remove(m_oreComboboxLabel);
            list.Controls.Add(m_oreComboboxLabel);
            m_oreComboboxLabel.Visible = false;

            m_oreCombobox = AddCombo();
            {
                foreach (var definition in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
                {
                    m_oreCombobox.AddItem((int)definition.Id.SubtypeId, definition.Id.SubtypeId);
                }
                m_oreCombobox.ItemSelected += OnOreCombobox_ItemSelected;
                m_oreCombobox.SortItemsByValueText();
            }
            m_oreCombobox.Visible = false;
            list.Controls.Add(m_oreCombobox);


            var layerStartLabel = CreateSliderWithDescription(list, usableWidth, -m_procAsteroidSizeValue * m_planetDeviationScaleValue, m_procAsteroidSizeValue * m_planetDeviationScaleValue, "Layer start", ref m_materialLayerStart);
            m_materialLayerStart.ValueChanged += (MyGuiControlSlider s) =>
            {
                layerStartLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 0) + "m";
                int currentLayer = m_layerCombobox.GetSelectedIndex();
                if (currentLayer >= 0 && currentLayer < m_materialLayers.Count)
                {
                    m_materialLayers[currentLayer].StartHeight = s.Value;
                }

                if (s.Value > m_materialLayerEnd.Value)
                {
                    m_materialLayerEnd.Value = s.Value;
                }
            };

            var layerStartHeigthDeviationLabel = CreateSliderWithDescription(list, usableWidth, 0, 100.0f, "Layer start height deviation", ref m_materialLayerStartHeigthDeviation);
            m_materialLayerStartHeigthDeviation.ValueChanged += (MyGuiControlSlider s) =>
            {
                layerStartHeigthDeviationLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 0) + "m";
                int currentLayer = m_layerCombobox.GetSelectedIndex();
                if (currentLayer >= 0 && currentLayer < m_materialLayers.Count)
                {
                    m_materialLayers[currentLayer].HeightStartDeviation = s.Value;
                }
            };

            var layerEndLabel = CreateSliderWithDescription(list, usableWidth, -m_procAsteroidSizeValue * m_planetDeviationScaleValue, m_procAsteroidSizeValue * m_planetDeviationScaleValue, "Layer end", ref m_materialLayerEnd);   
            m_materialLayerEnd.ValueChanged += (MyGuiControlSlider s) =>
            {
                layerEndLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 0) + "m";
                int currentLayer = m_layerCombobox.GetSelectedIndex();
                if (currentLayer >= 0 && currentLayer < m_materialLayers.Count)
                {
                    m_materialLayers[currentLayer].EndHeight = s.Value;
                }
                if (s.Value < m_materialLayerStart.Value)
                {
                    m_materialLayerStart.Value = s.Value;
                }
            };

            var layerHeigthDeviationLabel = CreateSliderWithDescription(list, usableWidth,0, 100.0f, "Layer end height deviation", ref m_materialLayerEndHeigthDeviation);
            m_materialLayerEndHeigthDeviation.ValueChanged += (MyGuiControlSlider s) =>
            {
                layerHeigthDeviationLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 0) + "m";
                int currentLayer = m_layerCombobox.GetSelectedIndex();
                if (currentLayer >= 0 && currentLayer < m_materialLayers.Count)
                {
                    m_materialLayers[currentLayer].HeightEndDeviation = s.Value;
                }
            };

            var layerAngleStartLabel = CreateSliderWithDescription(list, usableWidth, -1, 1, "Layer angle start", ref m_materialLayerAngleStart);
            m_materialLayerAngleStart.ValueChanged += (MyGuiControlSlider s) =>
            {
                layerAngleStartLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 3);
                int currentLayer = m_layerCombobox.GetSelectedIndex();
                if (currentLayer >= 0 && currentLayer < m_materialLayers.Count)
                {
                    m_materialLayers[currentLayer].StartAngle = s.Value;
                }

                if (s.Value > m_materialLayerAngleEnd.Value)
                {
                    m_materialLayerAngleEnd.Value = s.Value;
                }
            };

            var layerStartAngleDeviationLabel = CreateSliderWithDescription(list, usableWidth, 0, 1.0f, "Layer start angle deviation", ref m_materialLayerAngleStartDeviation);
            m_materialLayerAngleStartDeviation.ValueChanged += (MyGuiControlSlider s) =>
            {
                layerStartAngleDeviationLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 3);
                int currentLayer = m_layerCombobox.GetSelectedIndex();
                if (currentLayer >= 0 && currentLayer < m_materialLayers.Count)
                {
                    m_materialLayers[currentLayer].AngleStartDeviation = s.Value;
                }
            };

            var layerAngleEndLabel = CreateSliderWithDescription(list, usableWidth, -1, 1, "Layer angle end", ref m_materialLayerAngleEnd);
            m_materialLayerAngleEnd.ValueChanged += (MyGuiControlSlider s) =>
            {
                layerAngleEndLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 3);
                int currentLayer = m_layerCombobox.GetSelectedIndex();
                if (currentLayer >= 0 && currentLayer < m_materialLayers.Count)
                {
                    m_materialLayers[currentLayer].EndAngle = s.Value;
                }
                if (s.Value < m_materialLayerAngleStart.Value)
                {
                    m_materialLayerAngleStart.Value = s.Value;
                }
            };

            var layerAngleDeviationLabel = CreateSliderWithDescription(list, usableWidth, 0, 1.0f, "Layer end angle deviation", ref m_materialLayerAngleEndDeviation);
            m_materialLayerAngleEndDeviation.ValueChanged += (MyGuiControlSlider s) =>
            {
                layerAngleDeviationLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 3);
                int currentLayer = m_layerCombobox.GetSelectedIndex();
                if (currentLayer >= 0 && currentLayer < m_materialLayers.Count)
                {
                    m_materialLayers[currentLayer].AngleEndDeviation = s.Value;
                }
            };

        }

        private void CreatePlanetHillControlls(MyGuiControlList list, float usableWidth)
        {
            var labelHillTreshold = CreateSliderWithDescription(list, usableWidth, 0f, 2f, "Planet hill treshold", ref m_planetHillTreshold);
            m_planetHillTreshold.ValueChanged += (MyGuiControlSlider s) =>
            {
                labelHillTreshold.Text = MyValueFormatter.GetFormatedFloat(s.Value, 3);
            };
            m_planetHillTreshold.Value = 0.5f;

            var labelHillBlendTreshold = CreateSliderWithDescription(list, usableWidth, 0f, 1f, "Planet hill blend size", ref m_planetHillBlendTreshold);
            m_planetHillBlendTreshold.ValueChanged += (MyGuiControlSlider s) =>
            {
                labelHillBlendTreshold.Text = MyValueFormatter.GetFormatedFloat(s.Value, 2);
            };
            m_planetHillBlendTreshold.Value = 0.4f;

            var labelHillSizeRatio = CreateSliderWithDescription(list, usableWidth, 1f, 5f, "Planet hill size ratio", ref m_planetHillSizeRatio);
            m_planetHillSizeRatio.ValueChanged += (MyGuiControlSlider s) =>
            {
                labelHillSizeRatio.Text = MyValueFormatter.GetFormatedFloat(s.Value, 3);
                m_planetHillRatioValue = s.Value;
            };
            m_planetHillSizeRatio.Value = m_planetHillRatioValue;
            m_planetHillSizeRatio.ValueChanged += OnPlanetHillRatioChanged;

            var labelHillFrequency = CreateSliderWithDescription(list, usableWidth, 0.1f, 4f, "Planet hill frequency", ref m_planetHillFrequency);
            m_planetHillFrequency.ValueChanged += (MyGuiControlSlider s) =>
            {
                labelHillFrequency.Text = MyValueFormatter.GetFormatedFloat(s.Value, 2);
            };
            m_planetHillFrequency.Value = 2f;

            var labelHillNumNoises = CreateSliderWithDescription(list, usableWidth, 1f, 4f, "Planet hill num noises", ref m_planetHillNumNoises);
            m_planetHillNumNoises.ValueChanged += (MyGuiControlSlider s) =>
            {
                int value = (int)Math.Ceiling(s.Value);
                labelHillNumNoises.Text = value.ToString();
            };
            m_planetHillNumNoises.Value = 2f;
        }

        private void CreatePlanetCanyonControlls(MyGuiControlList list, float usableWidth)
        {          
            var labelHillTreshold = CreateSliderWithDescription(list, usableWidth, -1.345f, -0.5f, "Planet canyon treshold", ref m_planetCanyonTreshold);
            m_planetCanyonTreshold.ValueChanged += (MyGuiControlSlider s) =>
            {
                labelHillTreshold.Text = MyValueFormatter.GetFormatedFloat(s.Value, 3);
            };
            m_planetCanyonTreshold.Value = -0.8f;

            var labelHillBlendTreshold = CreateSliderWithDescription(list, usableWidth, 0f, 1f, "Planet canyon blend size", ref m_planetCanyonBlendTreshold);
            m_planetCanyonBlendTreshold.ValueChanged += (MyGuiControlSlider s) =>
            {
                labelHillBlendTreshold.Text = MyValueFormatter.GetFormatedFloat(s.Value, 2);
            };
            m_planetCanyonBlendTreshold.Value = 0.1f;

            var labelHillSizeRatio = CreateSliderWithDescription(list, usableWidth, 1.0f, 5.0f, "Planet canyon size ratio", ref m_planetCanyonSizeRatio);
            m_planetCanyonSizeRatio.ValueChanged += (MyGuiControlSlider s) =>
            {
                labelHillSizeRatio.Text = MyValueFormatter.GetFormatedFloat(s.Value, 3);
                m_planetCanyonRatioValue = s.Value;
            };
            m_planetCanyonSizeRatio.Value = m_planetCanyonRatioValue;
            m_planetCanyonSizeRatio.ValueChanged += OnPlanetCanyonRatioChanged;

            var labelHillFrequency = CreateSliderWithDescription(list, usableWidth, 0.1f, 3f, "Planet canyon frequency", ref m_planetCanyonFrequency);
            m_planetCanyonFrequency.ValueChanged += (MyGuiControlSlider s) =>
            {
                labelHillFrequency.Text = MyValueFormatter.GetFormatedFloat(s.Value, 2);
            };
            m_planetCanyonFrequency.Value = 0.11f;

            var labelCanoynNumNoises = CreateSliderWithDescription(list, usableWidth, 1f, 2f, "Planet canyon num noises", ref m_planetCanyonNumNoises);
            m_planetCanyonNumNoises.ValueChanged += (MyGuiControlSlider s) =>
            {
                int value = (int)Math.Ceiling(s.Value);
                labelCanoynNumNoises.Text = value.ToString();
            };
            m_planetCanyonNumNoises.Value = 0.9f;
        }

        private MyGuiControlLabel CreateSliderWithDescription(MyGuiControlList list, float usableWidth,float min,float max, string description,ref MyGuiControlSlider slider)
        {
            var label = AddLabel(description, Vector4.One, m_scale);
            Controls.Remove(label);
            list.Controls.Add(label);

            CreateSlider(list, usableWidth, min, max, ref slider);

            var labelNoise = AddLabel("", Vector4.One, m_scale);
            Controls.Remove(labelNoise);
            list.Controls.Add(labelNoise);
            return labelNoise;
        }

        private void CreateSlider(MyGuiControlList list, float usableWidth, float min,float max,ref MyGuiControlSlider slider)
        {
            slider = new MyGuiControlSlider(
               position: m_currentPosition,
               width: 400f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
               minValue: min,
               maxValue: max,
               labelText: String.Empty,
               labelDecimalPlaces: 4,
               labelScale: 0.75f * m_scale,
               originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
               labelFont: MyFontEnum.Debug);

            slider.DebugScale = m_sliderDebugScale;
            slider.ColorMask = Color.White.ToVector4();
            slider.Size = new Vector2(usableWidth, m_procAsteroidDeviationScale.Size.Y);
            list.Controls.Add(slider);
        }

        private void PickPlanet(MyGuiControlButton obj)
        {
           
        }

        private void UpatePlanet(MyGuiControlButton obj)
        {
            if (m_currentVoxel == null)
            {
                return;
            }

            MyCsgShapePlanetShapeAttributes planetShapeAttributes;
            MyCsgShapePlanetHillAttributes hillAttributes;
            MyCsgShapePlanetHillAttributes canyonAttributes;
            GetPlanetAttributes(out planetShapeAttributes, out hillAttributes,out canyonAttributes);

            SpawnPlanet(ref planetShapeAttributes, ref hillAttributes, ref canyonAttributes, m_materialLayers, m_currentVoxel.PositionLeftBottomCorner);
            m_currentVoxel.Close();

            PickPlanet(null);
        }

        private void GetPlanetAttributes(out MyCsgShapePlanetShapeAttributes planetShapeAttributes, out MyCsgShapePlanetHillAttributes hillAttributes, out MyCsgShapePlanetHillAttributes canyonAttributes)
        {
            int seed = GetProceduralAsteroidSeed(m_procAsteroidSeed);
            planetShapeAttributes = new MyCsgShapePlanetShapeAttributes();
            planetShapeAttributes.DeviationScale = m_planetDeviationScaleValue;
            planetShapeAttributes.Radius = m_procAsteroidSize.Value;
            planetShapeAttributes.Seed = seed;
            planetShapeAttributes.LayerDeviationSeed = GetProceduralAsteroidSeed(m_materialLayerDeviationSeed);
            planetShapeAttributes.NoiseFrequency = m_planetStructureRatio.Value;
            planetShapeAttributes.NormalNoiseFrequency = m_normalNoiseFrequency.Value;
            planetShapeAttributes.LayerDeviationNoiseFreqeuncy = m_materialLayerDeviationNoise.Value;

            hillAttributes = new MyCsgShapePlanetHillAttributes();
            hillAttributes.SizeRatio = m_planetHillSizeRatio.Value;
            hillAttributes.Treshold = m_planetHillTreshold.Value;
            hillAttributes.BlendTreshold = m_planetHillBlendTreshold.Value;
            hillAttributes.Frequency = m_planetHillFrequency.Value;
            hillAttributes.NumNoises = (int)Math.Ceiling(m_planetHillNumNoises.Value);

            canyonAttributes = new MyCsgShapePlanetHillAttributes();
            canyonAttributes.SizeRatio = m_planetCanyonSizeRatio.Value;
            canyonAttributes.Treshold = m_planetCanyonTreshold.Value;
            canyonAttributes.BlendTreshold =  m_planetCanyonBlendTreshold.Value;
            canyonAttributes.Frequency = m_planetCanyonFrequency.Value;
            canyonAttributes.NumNoises = (int)Math.Ceiling(m_planetCanyonNumNoises.Value);
        }

    }
}
