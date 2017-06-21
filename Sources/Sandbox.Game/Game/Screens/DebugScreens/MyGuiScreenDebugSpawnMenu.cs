using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Network;
using VRage.Serialization;
using System.Diagnostics;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using Sandbox.Game.SessionComponents.Clipboard;
using VRage.Game.Entity;
using VRage.Voxels;
using VRageRender.Messages;
using Sandbox.Game.Multiplayer;

namespace Sandbox.Game.Gui
{
    [StaticEventOwner]
    class MyGuiScreenDebugSpawnMenu : MyGuiScreenDebugBase
    {
        public struct SpawnAsteroidInfo
        {
            [Serialize(MyObjectFlags.Nullable)]
            public string Asteroid;
            public int RandomSeed;
            public Vector3D Position;
            public bool IsProcedural;
            public float ProceduralRadius;
        }

        private static readonly Vector2 SCREEN_SIZE = new Vector2(0.40f, 1.2f);
        private static readonly float HIDDEN_PART_RIGHT = 0.04f;

        private MyGuiControlCombobox m_asteroidCombobox;
        private MyGuiControlCombobox m_planetCombobox;
        private string m_selectedCoreVoxelFile;
        private MyGuiControlCombobox m_physicalObjectCombobox;
        private static int m_lastSelectedFloatingObjectIndex = 0;
        private static int m_lastSelectedAsteroidIndex = 0;
        private List<MyPhysicalItemDefinition> m_physicalItemDefinitions = new List<MyPhysicalItemDefinition>();
        private MyGuiControlTextbox m_amountTextbox;
        private MyGuiControlLabel m_errorLabel;
        private static long m_amount = 1;
        private static int m_asteroidCounter = 0;

        private static float m_procAsteroidSizeValue = 64.0f;

        private static String m_procAsteroidSeedValue = "12345";
        private MyGuiControlSlider m_procAsteroidSize;
        private MyGuiControlTextbox m_procAsteroidSeed;

        private static string m_selectedPlanetName;

        private MyVoxelBase m_currentVoxel = null;

        private static int m_selectedScreen;

        private delegate void CreateScreen(float space, float width);

        private struct Screen
        {
            public string Name;
            public CreateScreen Creator;
        }

        private Screen[] Screens;

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugSpawnMenu";
        }

        public static SpawnAsteroidInfo m_lastAsteroidInfo;

        static MyGuiScreenDebugSpawnMenu()
        {
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

            Screens = new Screen[]{
                new Screen() {
                    Name = MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_Asteroids),
                    Creator = CreateAsteroidsSpawnMenu
                },
                new Screen() {
                    Name = MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_ProceduralAsteroids),
                    Creator = CreateProceduralAsteroidsSpawnMenu
                },
                new Screen() {
                    Name = MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_Planets),
                    Creator = CreatePlanetsSpawnMenu
                },
                new Screen() {
                    Name = MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_EmptyVoxelMap),
                    Creator = CreateEmptyVoxelMapSpawnMenu
                }
            };

            RecreateControls(true);
        }

        private void CreateScreenSelector()
        {
            var combo = AddCombo();
            combo.AddItem(0, MySpaceTexts.ScreenDebugSpawnMenu_PredefinedAsteroids);

            if (MyFakes.ENABLE_SPAWN_MENU_PROCEDURAL_ASTEROIDS)
                combo.AddItem(1, MySpaceTexts.ScreenDebugSpawnMenu_ProceduralAsteroids);

            if (MyFakes.ENABLE_PLANETS)
                combo.AddItem(2, MySpaceTexts.ScreenDebugSpawnMenu_Planets);

            if (MyFakes.ENABLE_SPAWN_MENU_EMPTY_VOXEL_MAPS)
                combo.AddItem(3, MySpaceTexts.ScreenDebugSpawnMenu_EmptyVoxelMap);

            combo.SelectItemByKey(m_selectedScreen);
            combo.ItemSelected += () => { m_selectedScreen = (int)combo.GetSelectedKey(); RecreateControls(false); };
        }


        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

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

            CreateMenu(separatorSize, usableWidth);

            CreateObjectsSpawnMenu(separatorSize, usableWidth);
        }

        private void CreateMenu(float separatorSize, float usableWidth)
        {
            AddSubcaption(Screens[m_selectedScreen].Name, Color.White.ToVector4(), new Vector2(-HIDDEN_PART_RIGHT, 0.0f));

            CreateScreenSelector();

            Screens[m_selectedScreen].Creator(separatorSize, usableWidth);
        }

        private void CreateAsteroidsSpawnMenu(float separatorSize, float usableWidth)
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

        private void CreateProceduralAsteroidsSpawnMenu(float separatorSize, float usableWidth)
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
                    if (physicalItemDef == null || physicalItemDef.CanSpawnFromScreen == false)
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

            MyCharacterDetectorComponent comp;
            MyTerminalBlock detected = null;
            bool enableButton = false;

            if (MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.Components.TryGet(out comp) && comp.UseObject != null)
                detected = comp.DetectedEntity as MyTerminalBlock;
           
            string name = "-";
            if (detected != null && detected.HasInventory && detected.HasLocalPlayerAccess())
            {
                name = detected.CustomName.ToString();
                enableButton = true;
            }

            AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_CurrentTarget) + name, Color.White.ToVector4(), m_scale);
            CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_SpawnTargeted, OnSpawnIntoContainer, enableButton);

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

        private void UpdateLayerSlider(MyGuiControlSlider slider, float minValue, float maxValue)
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

        private void OnPlanetCombobox_ItemSelected()
        {
            m_selectedPlanetName = m_planetCombobox.GetSelectedValue().ToString();
        }

        private void OnPhysicalObjectCombobox_ItemSelected()
        {
            m_lastSelectedFloatingObjectIndex = m_physicalObjectCombobox.GetSelectedIndex();
        }

        private void SpawnFloatingObjectPreview()
        {
            var itemId = m_physicalItemDefinitions[(int)m_physicalObjectCombobox.GetSelectedKey()].Id;

            MyFixedPoint amount = (MyFixedPoint)(decimal)m_amount;

            var builder = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemId);

            if (builder is MyObjectBuilder_PhysicalGunObject || builder is MyObjectBuilder_OxygenContainerObject || builder is MyObjectBuilder_GasContainerObject)
                amount = 1;

            var obj = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_FloatingObject>();
            obj.PositionAndOrientation = MyPositionAndOrientation.Default;
            obj.Item = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_InventoryItem>();
            obj.Item.Amount = amount;
            obj.Item.PhysicalContent = builder;

            MyClipboardComponent.Static.ActivateFloatingObjectClipboard(obj, Vector3.Zero, 1f);
        }

        private MyGuiControlButton CreateDebugButton(float usableWidth, MyStringId text, Action<MyGuiControlButton> onClick, bool enabled = true, MyStringId? tooltip = null)
        {
            m_currentPosition.Y += .01f;
            var button = AddButton(MyTexts.Get(text), onClick);
            button.VisualStyle = MyGuiControlButtonStyleEnum.Rectangular;
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
            if (MySession.Static.ControlledEntity != null &&
                (MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Entity || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator))
            {
                return MySession.Static.ControlledEntity.GetHeadMatrix(true);
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

        private void OnSpawnIntoContainer(MyGuiControlButton myGuiControlButton)
        {
            if (!IsValidAmount())
            {
                m_errorLabel.Visible = true;
                return;
            }

            MyCharacterDetectorComponent comp;

            if (MySession.Static.LocalCharacter == null || !MySession.Static.LocalCharacter.Components.TryGet(out comp))
                return;

            MyTerminalBlock entity = comp.DetectedEntity as MyTerminalBlock;

            if (entity == null || !entity.HasInventory)
                return;

            SerializableDefinitionId itemId = m_physicalItemDefinitions[(int)m_physicalObjectCombobox.GetSelectedKey()].Id;

            if (Sync.IsServer)
                SpawnIntoContainer_Implementation(m_amount, itemId, entity.EntityId, MySession.Static.LocalPlayerId);
            else
                MyMultiplayer.RaiseStaticEvent(x => SpawnIntoContainer_Implementation, m_amount, itemId, entity.EntityId, MySession.Static.LocalPlayerId);
        }

        [Event, Reliable, Server]
        private static void SpawnIntoContainer_Implementation(long amount, SerializableDefinitionId item, long entityId, long playerId)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }

            MyEntity entity;
            if (!MyEntities.TryGetEntityById(entityId, out entity))
                return;

            if(!entity.HasInventory || !((MyTerminalBlock)entity).HasPlayerAccess(playerId))
                return;

            MyInventory inventory = entity.GetInventory();

            if(!inventory.CheckConstraint(item))
                return;

            MyFixedPoint itemAmt = (MyFixedPoint)Math.Min(amount, (decimal)inventory.ComputeAmountThatFits(item));

            inventory.AddItems(itemAmt, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(item));
        }

        private void OnLoadAsteroid(MyGuiControlButton obj)
        {
            SpawnVoxelPreview();
            CloseScreenNow();
        }

        private void OnSpawnProceduralAsteroid(MyGuiControlButton obj)
        {
            int seed = GetProceduralAsteroidSeed(m_procAsteroidSeed);
            SpawnProceduralAsteroid(seed, m_procAsteroidSize.Value);
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
            return new MyOctreeStorage(MyCompositeShapeProvider.CreateAsteroidShape(seed, radius, 0), MyVoxelCoordSystems.FindBestOctreeSize(radius));
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

                m_lastAsteroidInfo = new SpawnAsteroidInfo()
                {
                    Asteroid = m_selectedCoreVoxelFile.ToString(),
                    RandomSeed = randomSeed,
                    Position = Vector3D.Zero,
                    IsProcedural = false
                };

                MyClipboardComponent.Static.ActivateVoxelClipboard(builder, storage, MySector.MainCamera.ForwardVector, (storage.Size * 0.5f).Length());
            }
        }

        public static MyStorageBase CreateProceduralAsteroidStorage(int seed, float radius)
        {
            return new MyOctreeStorage(MyCompositeShapeProvider.CreateAsteroidShape(seed, radius, 2), MyVoxelCoordSystems.FindBestOctreeSize(radius));
        }

        private void SpawnProceduralAsteroid(int seed, float radius)
        {
            var storageNameBase = "ProcAsteroid" + "-" + seed + "r" + radius;
            var storageName = MakeStorageName(storageNameBase);

            var storage = CreateProceduralAsteroidStorage(seed, radius);
            var builder = CreateAsteroidObjectBuilder(storageName);

            m_lastAsteroidInfo = new SpawnAsteroidInfo()
            {
                Asteroid = null,
                RandomSeed = seed,
                Position = Vector3D.Zero,
                IsProcedural = true,
                ProceduralRadius = radius,
            };

            MyClipboardComponent.Static.ActivateVoxelClipboard(builder, storage, MySector.MainCamera.ForwardVector, (storage.Size * 0.5f).Length());
        }

        public static void RecreateAsteroidBeforePaste(float dragVectorLength){
            var seed = m_lastAsteroidInfo.RandomSeed;
            var radius = m_lastAsteroidInfo.ProceduralRadius;
            var storageNameBase = "ProcAsteroid" + "-" + seed + "r" + radius;
            var storageName = MyGuiScreenDebugSpawnMenu.MakeStorageName(storageNameBase);

            MyStorageBase storage = null;
            if (m_lastAsteroidInfo.IsProcedural)
                storage = CreateProceduralAsteroidStorage(seed, radius);
            else
            {
                bool oldValue = MyStorageBase.UseStorageCache;
                MyStorageBase.UseStorageCache = false;
                storage = CreateAsteroidStorage(m_lastAsteroidInfo.Asteroid, seed);
                MyStorageBase.UseStorageCache = oldValue;
            }
            var builder = CreateAsteroidObjectBuilder(storageName);

            MyClipboardComponent.Static.ActivateVoxelClipboard(builder, storage, MySector.MainCamera.ForwardVector, dragVectorLength);
        }

        public static String MakeStorageName(String storageNameBase)
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

        private MyGuiControlTextbox CreateSeedButton(string seedValue, float usableWidth)
        {
            var label = AddLabel(MyTexts.GetString(MySpaceTexts.ScreenDebugSpawnMenu_ProceduralSeed), Color.White.ToVector4(), m_scale);

            var textBox = new MyGuiControlTextbox(m_currentPosition, seedValue, 20, Color.White.ToVector4(), m_scale, MyGuiControlTextboxType.Normal);
            textBox.TextChanged += (MyGuiControlTextbox t) => { seedValue = t.Text; };
            textBox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            Controls.Add(textBox);

            m_currentPosition.Y += textBox.Size.Y;

            var button = CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_GenerateSeed, (MyGuiControlButton buttonClicked) => { textBox.Text = MyRandom.Instance.Next().ToString(); });
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

        private MyGuiControlLabel CreateSliderWithDescription(float usableWidth, float min, float max, string description, ref MyGuiControlSlider slider)
        {
            var label = AddLabel(description, Vector4.One, m_scale);

            CreateSlider(usableWidth, min, max, ref slider);

            var labelNoise = AddLabel("", Vector4.One, m_scale);
            return labelNoise;
        }

        private void CreateSlider(float usableWidth, float min, float max, ref MyGuiControlSlider slider)
        {
            slider = AddSlider(String.Empty, 5, min, max, null);
            slider.Size = new Vector2(400f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, slider.Size.Y);
            slider.LabelDecimalPlaces = 4;

            slider.DebugScale = m_sliderDebugScale;
            slider.ColorMask = Color.White.ToVector4();
        }

        public static string GetAsteroidName()
        {
            return m_lastAsteroidInfo.Asteroid;
        }

        static public void SpawnAsteroid(Vector3D pos)
        {
            m_lastAsteroidInfo.Position = pos;
            if (MySession.Static.HasCreativeRights || MySession.Static.CreativeMode)
            {
                MyMultiplayer.RaiseStaticEvent(s => SpawnAsteroid, m_lastAsteroidInfo);
            }
        }

        [Event, Reliable, Server]
        static void SpawnAsteroid(SpawnAsteroidInfo asteroidInfo)
        {
            if (MySession.Static.CreativeMode || MyEventContext.Current.IsLocallyInvoked || MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyStorageBase storage;
                string storageName;
                using (MyRandom.Instance.PushSeed(asteroidInfo.RandomSeed))
                {
                    if (!asteroidInfo.IsProcedural)
                    {
                        var storageNameBase = asteroidInfo.Asteroid + "-" + asteroidInfo.RandomSeed;
                        storageName = MakeStorageName(storageNameBase);
                        storage = CreateAsteroidStorage(asteroidInfo.Asteroid, asteroidInfo.RandomSeed);
                    }
                    else
                    {
                        var storageNameBase = "ProcAsteroid" + "-" + asteroidInfo.RandomSeed + "r" + asteroidInfo.ProceduralRadius;
                        storageName = MakeStorageName(storageNameBase);
                        storage = CreateProceduralAsteroidStorage(asteroidInfo.RandomSeed, asteroidInfo.ProceduralRadius, 0.03f);
                    }
                }

                var pastedVoxelMap = new MyVoxelMap();
                pastedVoxelMap.CreatedByUser = true;
                pastedVoxelMap.Save = true;
                pastedVoxelMap.AsteroidName = asteroidInfo.Asteroid;
                pastedVoxelMap.Init(storageName, storage, asteroidInfo.Position - storage.Size * 0.5f);
                MyEntities.Add(pastedVoxelMap);
                MyEntities.RaiseEntityCreated(pastedVoxelMap);
                pastedVoxelMap.IsReadyForReplication = true;
            }
        }

        #region Planet

        private void CreatePlanetsSpawnMenu(float separatorSize, float usableWidth)
        {
            float min = MyFakes.ENABLE_EXTENDED_PLANET_OPTIONS ? 100 : 19000;
            float max = /*MyFakes.ENABLE_EXTENDED_PLANET_OPTIONS ? (6378.1f * 1000 * 2) :*/ 120000f;
            MyGuiControlSlider slider = null;
            slider = new MyGuiControlSlider(
                position: m_currentPosition,
                width: 400f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                minValue: min,
                maxValue: max,
                labelText: String.Empty,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                labelFont: MyFontEnum.Debug,
                intValue: true);
            slider.DebugScale = m_sliderDebugScale;
            slider.ColorMask = Color.White.ToVector4();
            Controls.Add(slider);

            var label = new MyGuiControlLabel(
                position: m_currentPosition + new Vector2(slider.Size.X + 0.005f, slider.Size.Y / 2),
                text: String.Empty,
                colorMask: Color.White.ToVector4(),
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.8f * m_scale,
                font: MyFontEnum.Debug);
            label.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            Controls.Add(label);

            m_currentPosition.Y += slider.Size.Y;
            m_currentPosition.Y += separatorSize;

            slider.ValueChanged += (MyGuiControlSlider s) =>
            {
                StringBuilder sb = new StringBuilder();
                MyValueFormatter.AppendDistanceInBestUnit(s.Value, sb);
                label.Text = sb.ToString();
                m_procAsteroidSizeValue = s.Value;
            };
            slider.Value = 8000;

            m_procAsteroidSeed = CreateSeedButton(m_procAsteroidSeedValue, usableWidth);
            m_planetCombobox = AddCombo();
            {
                foreach (var definition in MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions())
                {
                    m_planetCombobox.AddItem((int)definition.Id.SubtypeId, definition.Id.SubtypeId.ToString());
                }
                m_planetCombobox.ItemSelected += OnPlanetCombobox_ItemSelected;
                m_planetCombobox.SortItemsByValueText();
                m_planetCombobox.SelectItemByIndex(0);
            }

            CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_SpawnAsteroid, x =>
            {
                int seed = GetProceduralAsteroidSeed(m_procAsteroidSeed);
                CreatePlanet(seed, slider.Value);
                CloseScreenNow();
            });
        }

        private void CreatePlanet(int seed, float size)
        {
            Vector3D pos = MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * size * 3 - new Vector3D(size);

            MyPlanetGeneratorDefinition planetDefinition = MyDefinitionManager.Static.GetDefinition<MyPlanetGeneratorDefinition>(MyStringHash.GetOrCompute(m_selectedPlanetName));
            MyPlanetStorageProvider provider = new MyPlanetStorageProvider();

            provider.Init(seed, planetDefinition, size / 2f);

            IMyStorage storage = new MyOctreeStorage(provider, provider.StorageSize);

            float minHillSize = provider.Radius * planetDefinition.HillParams.Min;
            float maxHillSize = provider.Radius * planetDefinition.HillParams.Max;

            float averagePlanetRadius = provider.Radius;

            float outerRadius = averagePlanetRadius + maxHillSize;
            float innerRadius = averagePlanetRadius + minHillSize;

            float atmosphereRadius = planetDefinition.AtmosphereSettings.HasValue && planetDefinition.AtmosphereSettings.Value.Scale > 1f ? 1 + planetDefinition.AtmosphereSettings.Value.Scale : 1.75f;
            atmosphereRadius *= provider.Radius;

            var planet = new MyPlanet();
            planet.EntityId = MyRandom.Instance.NextLong();

            MyPlanetInitArguments planetInitArguments;
            planetInitArguments.StorageName = "test";
            planetInitArguments.Storage = storage;
            planetInitArguments.PositionMinCorner = pos;
            planetInitArguments.Radius = provider.Radius;
            planetInitArguments.AtmosphereRadius = atmosphereRadius;
            planetInitArguments.MaxRadius = outerRadius;
            planetInitArguments.MinRadius = innerRadius;
            planetInitArguments.HasAtmosphere = planetDefinition.HasAtmosphere;
            planetInitArguments.AtmosphereWavelengths = Vector3.Zero;
            planetInitArguments.GravityFalloff = planetDefinition.GravityFalloffPower;
            planetInitArguments.MarkAreaEmpty = true;
            planetInitArguments.AtmosphereSettings = planetDefinition.AtmosphereSettings.HasValue ? planetDefinition.AtmosphereSettings.Value : MyAtmosphereSettings.Defaults();
            planetInitArguments.SurfaceGravity = planetDefinition.SurfaceGravity;
            planetInitArguments.AddGps = false;
            planetInitArguments.SpherizeWithDistance = true;
            planetInitArguments.Generator = planetDefinition;
            planetInitArguments.UserCreated = true;
            planetInitArguments.InitializeComponents = true;

            planet.Init(planetInitArguments);

            m_lastAsteroidInfo = new SpawnAsteroidInfo()
            {
                Asteroid = null,
                RandomSeed = seed,
                Position = Vector3D.Zero,
                IsProcedural = true,
                ProceduralRadius = size,
            };

            MyClipboardComponent.Static.ActivateVoxelClipboard(planet.GetObjectBuilder(), storage, MySector.MainCamera.ForwardVector, (storage.Size * 0.5f).Length());
        }

        public static void SpawnPlanet(Vector3D pos)
        {
            MyMultiplayer.RaiseStaticEvent(s => SpawnPlanet_Server, m_selectedPlanetName, m_lastAsteroidInfo.ProceduralRadius, m_lastAsteroidInfo.RandomSeed, pos);
        }

        [Event, Reliable, Server]
        static void SpawnPlanet_Server(string planetName, float size, int seed, Vector3D pos)
        {
            if (MySession.Static.CreativeMode || MyEventContext.Current.IsLocallyInvoked || MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                var storageNameBase = planetName + "-" + seed + "d" + size;

                var storageName = MakeStorageName(storageNameBase);

                MyWorldGenerator.AddPlanet(storageNameBase, planetName, planetName, pos, seed, size, MyRandom.Instance.NextLong(), userCreated: true);

                if (MySession.Static.RequiresDX < 11)
                    MySession.Static.RequiresDX = 11;
            }
        }

        #endregion

        #region Empty Voxel Map

        private void CreateEmptyVoxelMapSpawnMenu(float separatorSize, float usableWidth)
        {
            int min = 2;
            int max = 10;

            var label = AddLabel("Voxel Size: ", Vector4.One, m_scale);

            MyGuiControlSlider slider = null;
            slider = new MyGuiControlSlider(
                position: m_currentPosition,
                width: 400f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                minValue: min,
                maxValue: max,
                labelText: String.Empty,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                labelFont: MyFontEnum.Debug,
                intValue: true);
            slider.DebugScale = m_sliderDebugScale;
            slider.ColorMask = Color.White.ToVector4();
            Controls.Add(slider);

            label = new MyGuiControlLabel(
                position: m_currentPosition + new Vector2(slider.Size.X + 0.005f, slider.Size.Y / 2),
                text: String.Empty,
                colorMask: Color.White.ToVector4(),
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.8f * m_scale,
                font: MyFontEnum.Debug);
            label.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            Controls.Add(label);

            m_currentPosition.Y += slider.Size.Y;
            m_currentPosition.Y += separatorSize;

            slider.ValueChanged += (MyGuiControlSlider s) =>
            {
                int size = 1 << ((int)s.Value);
                label.Text = size + "m";
                m_procAsteroidSizeValue = size;
            };
            slider.Value = 5;

            CreateDebugButton(usableWidth, MySpaceTexts.ScreenDebugSpawnMenu_SpawnAsteroid, x =>
            {
                int size = (int)m_procAsteroidSizeValue;
                Debug.Assert(MathHelper.IsPowerOfTwo(size));

                MyStorageBase storage = new MyOctreeStorage(null, new Vector3I(size));

                string name = MakeStorageName("MyEmptyVoxelMap");

                var builder = CreateAsteroidObjectBuilder(name);
                MyClipboardComponent.Static.ActivateVoxelClipboard(builder, storage, MySector.MainCamera.ForwardVector, (storage.Size * 0.5f).Length());

                CloseScreenNow();
            });
        }

        #endregion
    }
}
