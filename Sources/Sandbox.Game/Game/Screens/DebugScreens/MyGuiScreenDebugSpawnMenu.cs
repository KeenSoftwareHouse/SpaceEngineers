using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
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
using VRage.ObjectBuilders;

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

        private static String m_procAsteroidSeedValue = "12345";
        private MyGuiControlSlider m_procAsteroidSize;
        private MyGuiControlTextbox m_procAsteroidSeed;

    
        private MyVoxelBase m_currentVoxel = null;

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
                        m_asteroidCombobox.AddItem((int)definition.Id.SubtypeId, definition.Id.SubtypeId.ToString());
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

        private void OnAsteroidCombobox_ItemSelected()
        {
            m_lastSelectedAsteroidIndex = m_asteroidCombobox.GetSelectedIndex();
            m_selectedCoreVoxelFile = m_asteroidCombobox.GetSelectedValue().ToString();
        }

        private void OnPhysicalObjectCombobox_ItemSelected()
        {
            m_lastSelectedFloatingObjectIndex = m_physicalObjectCombobox.GetSelectedIndex();
        }

        private void SpawnFloatingObjectPreview()
        {
            var itemId = m_physicalItemDefinitions[(int)m_physicalObjectCombobox.GetSelectedKey()].Id;

            MyFixedPoint amount = (MyFixedPoint)(float)m_amount;

            var builder = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemId);

            if (builder is MyObjectBuilder_PhysicalGunObject || builder is Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_OxygenContainerObject)
                amount = 1;

            var obj = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_FloatingObject>();
            obj.PositionAndOrientation = MyPositionAndOrientation.Default;
            obj.Item = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_InventoryItem>();
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

        private void OnSpawnProceduralAsteroid(MyGuiControlButton obj)
        {
            int seed = GetProceduralAsteroidSeed(m_procAsteroidSeed);
            if (m_asteroid_showPlanet)
            {
                SpawnPlanet(seed, m_procAsteroidSize.Value);
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

        public static MyStorageBase CreateProceduralAsteroidStorage(int seed, float radius, float deviationScale)
        {
            return new MyOctreeStorage(MyCompositeShapeProvider.CreateAsteroidShape(seed, radius, 0), FindBestOctreeSize(radius));
        }

        public static MyObjectBuilder_VoxelMap CreateAsteroidObjectBuilder(string storageName)
        {
            var builder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Planet>();
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

        private void SpawnPlanet(int seed,float size,Vector3D? pos = null)
        {
            var storageNameBase = "Planet" + "-" + seed + "d" + size;

            var storageName = MakeStorageName(storageNameBase);

            if (pos.HasValue == false)
            {
                pos = MySession.LocalHumanPlayer.GetPosition();
            }

            var previewVoxelMap = MyWorldGenerator.AddPlanet(storageNameBase, MySession.LocalHumanPlayer.GetPosition(), seed, size, MyRandom.Instance.NextLong());
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
  
            var button = CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_SpawnAsteroid, OnSpawnProceduralAsteroid); 
            Controls.Remove(button);
            list.Controls.Add(button);

            Controls.Add(list);
        }

        private void CreatePlanetControls(MyGuiControlList list, float usableWidth)
        {   
            var asteroidSizeLabel = CreateSliderWithDescription(list, usableWidth, 8000f, 50000f, MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_ProceduralSize), ref m_procAsteroidSize);

            m_procAsteroidSize.ValueChanged += (MyGuiControlSlider s) => { asteroidSizeLabel.Text = MyValueFormatter.GetFormatedFloat(s.Value, 0) + "m"; m_procAsteroidSizeValue = s.Value; };
            m_procAsteroidSize.Value = 8000.1f;

            m_procAsteroidSeed = CreateSeedButton(list, m_procAsteroidSeedValue, usableWidth);  
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
            list.Controls.Add(slider);
        }
    }
}
