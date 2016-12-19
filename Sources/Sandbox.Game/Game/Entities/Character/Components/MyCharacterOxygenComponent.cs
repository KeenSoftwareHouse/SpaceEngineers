using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel.Syndication;
using Sandbox.Engine.Utils;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.VisualScripting;
using Sandbox.Engine.Multiplayer;

namespace Sandbox.Game.Entities.Character.Components
{
    public class MyCharacterOxygenComponent : MyCharacterComponent
    {
        private class GasData
        {
            public MyDefinitionId Id;
            public float FillLevel;
            public float MaxCapacity;
            public float Throughput;

            public float NextGasTransfer;

            public int LastOutputTime;
            public int LastInputTime;
            public int NextGasRefill = -1;

            public override string ToString() { return string.Format("Subtype: {0}, FillLevel: {1}, CurrentCapacity: {2}, MaxCapacity: {3}", Id.SubtypeName, FillLevel, FillLevel*MaxCapacity, MaxCapacity); }
        }

        public static readonly float LOW_OXYGEN_RATIO = 0.2f;
        public static readonly float GAS_REFILL_RATION = 0.3f;

        private Dictionary<MyDefinitionId, int> m_gasIdToIndex; 
        private GasData[] m_storedGases;

        public float EnvironmentOxygenLevel;
        public float OxygenLevelAtCharacterLocation;
        public MyCubeGrid OxygenSourceGrid = null;

        private float m_oldSuitOxygenLevel;

        private const int m_gasRefillInterval = 5;

        private int m_lastOxygenUpdateTime;

        private const int m_updateInterval = 100;

        private MyResourceSinkComponent m_characterGasSink;
        private MyResourceSourceComponent m_characterGasSource;
        public MyResourceSinkComponent CharacterGasSink { get { return m_characterGasSink; } set { SetGasSink(value); } }
        public MyResourceSourceComponent CharacterGasSource { get { return m_characterGasSource; } set { SetGasSource(value); } }

		public static readonly MyDefinitionId OxygenId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");
        public static readonly MyDefinitionId HydrogenId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Hydrogen");

        MyEntity3DSoundEmitter m_soundEmitter = null;
        MySoundPair m_helmetOpenSound = new MySoundPair("PlayHelmetOn");
        MySoundPair m_helmetCloseSound = new MySoundPair("PlayHelmetOff");
        MySoundPair m_helmetAirEscapeSound = new MySoundPair("PlayChokeInitiate");

        MyHudNotification m_lowOxygenNotification;
        MyHudNotification m_criticalOxygenNotification;
        MyHudNotification m_oxygenBottleRefillNotification;
        MyHudNotification m_gasBottleRefillNotification;
        MyHudNotification m_helmetToggleNotification;

        #region Properties
        private MyCharacterDefinition Definition { get { return Character.Definition; } }

        public float OxygenCapacity { get {
            int gasIndex = -1;
            MyDefinitionId oxygenID = OxygenId;
            if (!TryGetTypeIndex(ref oxygenID, out gasIndex)) return 0f;
            return m_storedGases[gasIndex].MaxCapacity;
        } }

        public float SuitOxygenAmount
        {
            get { return GetGasFillLevel(OxygenId) * OxygenCapacity; }
            set {
                MyDefinitionId oxygenID = OxygenId;
                UpdateStoredGasLevel(ref oxygenID, MyMath.Clamp(value / OxygenCapacity, 0f, 1f)); 
            }
        }

        public float SuitOxygenAmountMissing { get { return OxygenCapacity - GetGasFillLevel(OxygenId) * OxygenCapacity; } }

        public float SuitOxygenLevel
        {
            get
            {
                if (OxygenCapacity == 0)
                {
                    return 0;
                }
                return GetGasFillLevel(OxygenId);
            }
            set
            {
                MyDefinitionId oxygenID = OxygenId;
                UpdateStoredGasLevel(ref oxygenID, value);
            }
        }

        public bool IsOxygenLevelLow { get { return MyHud.CharacterInfo.OxygenLevel < LOW_OXYGEN_RATIO; } }

        public bool HelmetEnabled { get { return !NeedsOxygenFromSuit; } }
        public bool NeedsOxygenFromSuit { get; set; }
        #endregion

        public override string ComponentTypeDebugString { get { return "Oxygen Component"; } }

        public virtual void Init(MyObjectBuilder_Character characterOb)
        {
            m_lastOxygenUpdateTime = MySession.Static.GameplayFrameCounter;

            m_gasIdToIndex = new Dictionary<MyDefinitionId, int>(); 
            if (MyFakes.ENABLE_HYDROGEN_FUEL && Definition.SuitResourceStorage != null)
            {
                m_storedGases = new GasData[Definition.SuitResourceStorage.Count];
                for(int gasIndex = 0; gasIndex < m_storedGases.Length; ++gasIndex)
                {
                    var gasInfo = Definition.SuitResourceStorage[gasIndex];
                    m_storedGases[gasIndex] = new GasData
                    {
                        Id = gasInfo.Id,
                        FillLevel = 1f,
                        MaxCapacity = gasInfo.MaxCapacity,
                        Throughput = gasInfo.Throughput,
                        LastOutputTime = MySession.Static.GameplayFrameCounter,
                        LastInputTime = MySession.Static.GameplayFrameCounter
                    };
                    m_gasIdToIndex.Add(gasInfo.Id, gasIndex);
                }

                if (characterOb.StoredGases != null)
                {
                    if (!MySession.Static.CreativeMode)
                    {
                        foreach (var gasInfo in characterOb.StoredGases)
                        {
                            int gasIndex;
                            if (!m_gasIdToIndex.TryGetValue(gasInfo.Id, out gasIndex))
                                continue;

                            m_storedGases[gasIndex].FillLevel = gasInfo.FillLevel;
                        }
                    }
                }
            }
            if(m_storedGases == null)
                m_storedGases = new GasData[0];

            Debug.Assert(ContainsGasStorage(OxygenId), characterOb.SubtypeName + " is missing Oxygen resource.");
            Debug.Assert(ContainsGasStorage(HydrogenId), characterOb.SubtypeName + " is missing Hydrogen resource.");


            if (MySession.Static.Settings.EnableOxygen)
            {
                float oxygenFillLevel = GetGasFillLevel(OxygenId);
                m_oldSuitOxygenLevel = oxygenFillLevel == 0f ? OxygenCapacity : oxygenFillLevel;
            }

            EnvironmentOxygenLevel = characterOb.EnvironmentOxygenLevel;
            OxygenLevelAtCharacterLocation = 0f;

            m_oxygenBottleRefillNotification = new MyHudNotification(text: MySpaceTexts.NotificationBottleRefill, level: MyNotificationLevel.Important);
            m_gasBottleRefillNotification = new MyHudNotification(text: MySpaceTexts.NotificationGasBottleRefill, level: MyNotificationLevel.Important);
            m_lowOxygenNotification = new MyHudNotification(text: MySpaceTexts.NotificationOxygenLow, font: MyFontEnum.Red, level: MyNotificationLevel.Important);
            m_criticalOxygenNotification = new MyHudNotification(text: MySpaceTexts.NotificationOxygenCritical, font: MyFontEnum.Red, level: MyNotificationLevel.Important);
            m_helmetToggleNotification = m_helmetToggleNotification ?? new MyHudNotification(); // Init() is called when toggling helmet so this check is required

            // todo: this should be independent on animation/layer names
            string animationOpenHelmet;
            string animationCloseHelmet;
            Character.Definition.AnimationNameToSubtypeName.TryGetValue("HelmetOpen", out animationOpenHelmet);
            Character.Definition.AnimationNameToSubtypeName.TryGetValue("HelmetClose", out animationCloseHelmet);
            if ((animationOpenHelmet != null && animationCloseHelmet != null)
                || (Character.UseNewAnimationSystem && Character.AnimationController.Controller.GetLayerByName("Helmet") != null))
            {
                NeedsOxygenFromSuit = characterOb.NeedsOxygenFromSuit;
            }
            else
            {
                NeedsOxygenFromSuit = Definition.NeedsOxygen; // compatibility
            }

            NeedsUpdateBeforeSimulation = true;
            NeedsUpdateBeforeSimulation100 = true;

            if (m_soundEmitter == null)
                m_soundEmitter = new MyEntity3DSoundEmitter(Character);

            if (!HelmetEnabled)   // default state == helmet is enabled
                AnimateHelmet();
        }

        public virtual void GetObjectBuilder(MyObjectBuilder_Character objectBuilder)
        {
            objectBuilder.OxygenLevel = SuitOxygenLevel;
            objectBuilder.EnvironmentOxygenLevel = EnvironmentOxygenLevel;
            objectBuilder.NeedsOxygenFromSuit = NeedsOxygenFromSuit;

            if (m_storedGases != null && m_storedGases.Length > 0)
            {
                if(objectBuilder.StoredGases == null)
                    objectBuilder.StoredGases = new List<MyObjectBuilder_Character.StoredGas>();

                foreach (var storedGas in m_storedGases)
                {
                    if (!objectBuilder.StoredGases.TrueForAll((obGas) => obGas.Id != storedGas.Id))
                        continue;

                    objectBuilder.StoredGases.Add(new MyObjectBuilder_Character.StoredGas { Id = storedGas.Id, FillLevel = storedGas.FillLevel });
                }
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            UpdateOxygen();
        }

        private void UpdateOxygen()
        {
            // Try to find grids that might contain oxygen
            var entities = new List<MyEntity>();
            var aabb = Character.PositionComp.WorldAABB;

            bool lowOxygenDamage = MySession.Static.Settings.EnableOxygen;
            bool noOxygenDamage = MySession.Static.Settings.EnableOxygen;
            bool isInEnvironment = true;
            bool oxygenReplenished = false;

            EnvironmentOxygenLevel = MyOxygenProviderSystem.GetOxygenInPoint(Character.PositionComp.GetPosition());
            OxygenLevelAtCharacterLocation = EnvironmentOxygenLevel;

            if (Sync.IsServer)
            {
                // Check for possibility that we are replenishing oxygen
                if (MySession.Static.Settings.EnableOxygen)
                {
                    GasData oxygenData;
                    if (TryGetGasData(OxygenId, out oxygenData))
                    {
                        float timeSinceLastUpdateSeconds = (MySession.Static.GameplayFrameCounter - oxygenData.LastOutputTime) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                        oxygenReplenished = CharacterGasSink.CurrentInputByType(OxygenId) * timeSinceLastUpdateSeconds > Definition.OxygenConsumption;

                        if (oxygenReplenished)
                        {
                            noOxygenDamage = false;
                            lowOxygenDamage = false;
                        } 
                    } 
                }

                // Update Gases fill levels and capacity amounts
                foreach (GasData gasInfo in m_storedGases)
                {
                    var timeSinceLastOutputSeconds = (MySession.Static.GameplayFrameCounter - gasInfo.LastOutputTime) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    var timeSinceLastInputSeconds = (MySession.Static.GameplayFrameCounter - gasInfo.LastInputTime) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                    gasInfo.LastOutputTime = MySession.Static.GameplayFrameCounter;
                    gasInfo.LastInputTime = MySession.Static.GameplayFrameCounter;

                    float gasOutputAmount = CharacterGasSource.CurrentOutputByType(gasInfo.Id) * timeSinceLastOutputSeconds;
                    float gasInputAmount = CharacterGasSink.CurrentInputByType(gasInfo.Id) * timeSinceLastInputSeconds;

                    // Values that are not in distribution system yet and happend to inc/dec between updates
                    float outTransfer = -MathHelper.Clamp(gasInfo.NextGasTransfer, float.NegativeInfinity, 0f); 
                    float inTransfer = MathHelper.Clamp(gasInfo.NextGasTransfer, 0f, float.PositiveInfinity);
                    gasInfo.NextGasTransfer = 0f;

                    TransferSuitGas(ref gasInfo.Id, gasInputAmount + inTransfer, gasOutputAmount + outTransfer);
                }
            }

            if (MySession.Static.Settings.EnableOxygen)
            {
                var cockpit = Character.Parent as MyCockpit;
                bool OxygenFromCockpit = false;
                if (cockpit != null && cockpit.BlockDefinition.IsPressurized)
                {
                    if (Sync.IsServer && MySession.Static.SurvivalMode && !oxygenReplenished)
                    {
                        // Character is in pressurized room
                        if (!HelmetEnabled)
                        {
                            if (cockpit.OxygenFillLevel > 0f)
                            {
                                if (cockpit.OxygenAmount >= Definition.OxygenConsumption * Definition.OxygenConsumptionMultiplier)
                                {
                                    cockpit.OxygenAmount -= Definition.OxygenConsumption * Definition.OxygenConsumptionMultiplier;

                                    noOxygenDamage = false;
                                    lowOxygenDamage = false;
                                }
                            }
                        }
                    }
                    EnvironmentOxygenLevel = cockpit.OxygenFillLevel;
                    isInEnvironment = false;
                    OxygenFromCockpit = true;
                }

                if(OxygenFromCockpit == false || (MyFakes.ENABLE_NEW_SOUNDS && MySession.Static.Settings.RealisticSound))
                {
                    OxygenSourceGrid = null;
                    Vector3D pos = Character.GetHeadMatrix(true, true, false, true).Translation;

                    MyGamePruningStructure.GetTopMostEntitiesInBox(ref aabb, entities);
                    foreach (var entity in entities)
                    {
                        var grid = entity as MyCubeGrid;
                        // Oxygen can be present on small grids as well because of mods
                        if (grid != null && grid.GridSystems.GasSystem != null)
                        {
                            var oxygenBlock = grid.GridSystems.GasSystem.GetSafeOxygenBlock(pos);
                            if (oxygenBlock.Room != null)
                            {
                                if (oxygenBlock.Room.OxygenLevel(grid.GridSize) > Definition.PressureLevelForLowDamage)
                                {
                                    if (!HelmetEnabled)
                                    {
                                        lowOxygenDamage = false;
                                    }
                                }

                                if (oxygenBlock.Room.IsPressurized)
                                {
                                    float oxygen = oxygenBlock.Room.OxygenLevel(grid.GridSize);
                                    if (!OxygenFromCockpit)
                                        EnvironmentOxygenLevel = oxygen;
                                    OxygenLevelAtCharacterLocation = oxygen;
                                    OxygenSourceGrid = grid;
                                    if (oxygenBlock.Room.OxygenAmount > Definition.OxygenConsumption * Definition.OxygenConsumptionMultiplier)
                                    {
                                        if (!HelmetEnabled)
                                        {
                                            noOxygenDamage = false;
                                            oxygenBlock.PreviousOxygenAmount = oxygenBlock.OxygenAmount() - Definition.OxygenConsumption * Definition.OxygenConsumptionMultiplier;
                                            oxygenBlock.OxygenChangeTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                                            if (!oxygenReplenished)
                                                oxygenBlock.Room.OxygenAmount -= Definition.OxygenConsumption * Definition.OxygenConsumptionMultiplier;
                                        }
                                        break;
                                    }
                                }
                                else
                                {
                                    float oxygen = oxygenBlock.Room.EnvironmentOxygen;
                                    OxygenLevelAtCharacterLocation = oxygen;

                                    if (!OxygenFromCockpit)
                                    {
                                        EnvironmentOxygenLevel = oxygen;

                                        if (!HelmetEnabled && EnvironmentOxygenLevel > Definition.OxygenConsumption * Definition.OxygenConsumptionMultiplier)
                                        {
                                            noOxygenDamage = false;
                                            break;
                                        }
                                    }
                                }

                                isInEnvironment = false;
                            }
                        }
                    }
                }


                if (MySession.Static.LocalCharacter == Character)
                {
                    if (m_oldSuitOxygenLevel >= 0.25f && SuitOxygenLevel < 0.25f)
                    {
                        MyHud.Notifications.Add(m_lowOxygenNotification);
                    }
                    else if (m_oldSuitOxygenLevel >= 0.05f && SuitOxygenLevel < 0.05f)
                    {
                        MyHud.Notifications.Add(m_criticalOxygenNotification);
                    }
                }
                m_oldSuitOxygenLevel = SuitOxygenLevel;
            }
            CharacterGasSink.Update();

            // Cannot early exit before calculations because of UI
            if (!Sync.IsServer || MySession.Static.CreativeMode)
                return;

            foreach (var gasInfo in m_storedGases)
            {
                if (gasInfo.FillLevel < GAS_REFILL_RATION) // Get rid of the specific oxygen version of this 
                {
                    if (gasInfo.NextGasRefill == -1)
                        gasInfo.NextGasRefill = MySandboxGame.TotalGamePlayTimeInMilliseconds + m_gasRefillInterval * 1000;
                    if (MySandboxGame.TotalGamePlayTimeInMilliseconds < gasInfo.NextGasRefill)
                        continue;

                    gasInfo.NextGasRefill = -1;

                    var items = Character.GetInventory().GetItems();
                    bool bottlesUsed = false;
                    foreach (var item in items)
                    {
                        var gasContainer = item.Content as MyObjectBuilder_GasContainerObject;
                        if (gasContainer != null)
                        {
                            if (gasContainer.GasLevel == 0f)
                                continue;

                            var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(gasContainer) as MyOxygenContainerDefinition;
                            if (physicalItem.StoredGasId != gasInfo.Id)
                                continue;
                            float gasAmount = gasContainer.GasLevel * physicalItem.Capacity;

                            float transferredAmount = Math.Min(gasAmount, (1f - gasInfo.FillLevel) * gasInfo.MaxCapacity);
                            gasContainer.GasLevel = Math.Max((gasAmount - transferredAmount) / physicalItem.Capacity, 0f);

                            if (gasContainer.GasLevel > 1f)
                                Debug.Fail("Incorrect value");

                            Character.GetInventory().UpdateGasAmount();

                            bottlesUsed = true;

                            TransferSuitGas(ref gasInfo.Id, transferredAmount, 0);
                            if (gasInfo.FillLevel == 1f)
                                break;
                        }
                    }
                    if (bottlesUsed)
                    {
                        if (MySession.Static.LocalCharacter == Character)
                            ShowRefillFromBottleNotification(gasInfo.Id);
                        else
                            Character.SendRefillFromBottle(gasInfo.Id);
                    }

                    var jetpack = Character.JetpackComp;
                    if (jetpack != null && jetpack.TurnedOn && jetpack.FuelDefinition != null && jetpack.FuelDefinition.Id == gasInfo.Id
                        && gasInfo.FillLevel <= 0 
                        && (Character.ControllerInfo.Controller != null && MySession.Static.CreativeToolsEnabled(Character.ControllerInfo.Controller.Player.Id.SteamId) == false || (MySession.Static.LocalCharacter != Character && Sync.IsServer == false)))
                    {
                        if (Sync.IsServer && MySession.Static.LocalCharacter != Character)
                        {
                            MyMultiplayer.RaiseEvent(Character, x => x.SwitchJetpack, new VRage.Network.EndpointId(Character.ControllerInfo.Controller.Player.Id.SteamId));
                        }

                        jetpack.SwitchThrusts();
                    }
                }
                else
                    gasInfo.NextGasRefill = -1;
            }

            // No oxygen or low oxygen found in room, try to get it from suit
            if (MySession.Static.Settings.EnableOxygen)
            {
                if (noOxygenDamage || lowOxygenDamage)
                {
                    if (HelmetEnabled && SuitOxygenAmount > Definition.OxygenConsumption * Definition.OxygenConsumptionMultiplier)
                    {
                        noOxygenDamage = false;
                        lowOxygenDamage = false;
                    }

                    if (isInEnvironment && !HelmetEnabled)
                    {
                        if (EnvironmentOxygenLevel > Definition.PressureLevelForLowDamage)
                        {
                            lowOxygenDamage = false;
                        }
                        if (EnvironmentOxygenLevel > 0f)
                        {
                            noOxygenDamage = false;
                        }
                    }
                }

                m_oldSuitOxygenLevel = SuitOxygenLevel;

                if (noOxygenDamage)
                {
                    Character.DoDamage(Definition.DamageAmountAtZeroPressure, MyDamageType.LowPressure, true);
                }
                else if (lowOxygenDamage)
                {
                    Character.DoDamage(1f, MyDamageType.Asphyxia, true);
                }
            }

            if (MySession.Static.Settings.EnableOxygen)
                Character.UpdateOxygen(SuitOxygenAmount);

            foreach (var gasInfo in m_storedGases)
            {
                Character.UpdateStoredGas(gasInfo.Id, gasInfo.FillLevel);
            }
        }

        public void SwitchHelmet()
        {
            if (Character.IsDead)
            {
                return;
            }

            // old system - switching models
            //bool hasHelmetVariation = Definition.HelmetVariation != null;
            //if (hasHelmetVariation)
            //{
            //    bool variationExists = false;
            //    var characters = MyDefinitionManager.Static.Characters;
            //    if (Definition.Name != Definition.HelmetVariation)
            //    {
            //        foreach (var character in characters)
            //        {
            //            if (character.Name == Definition.HelmetVariation)
            //            {
            //                variationExists = true;
            //                break;
            //            }
            //        }
            //    }

            //    if (!variationExists)
            //    {
            //        hasHelmetVariation = false;
            //    }
            //}

            // todo: this should be independent on animation/layer names
            string animationOpenHelmet, animationCloseHelmet;
            Character.Definition.AnimationNameToSubtypeName.TryGetValue("HelmetOpen", out animationOpenHelmet);
            Character.Definition.AnimationNameToSubtypeName.TryGetValue("HelmetClose", out animationCloseHelmet);
            if ((animationOpenHelmet != null && animationCloseHelmet != null)
                || (Character.UseNewAnimationSystem && Character.AnimationController.Controller.GetLayerByName("Helmet") != null))
            {
                // old system
                //Character.ChangeModelAndColor(Definition.HelmetVariation, Character.ColorMask);
                NeedsOxygenFromSuit = !NeedsOxygenFromSuit;
                AnimateHelmet();
                if (MySession.Static.LocalCharacter == Character)
                {
                    m_helmetToggleNotification.Text = (!NeedsOxygenFromSuit ? MySpaceTexts.NotificationHelmetOn : MySpaceTexts.NotificationHelmetOff);
                }
            }
            else
            {
                m_helmetToggleNotification.Text = MySpaceTexts.NotificationNoHelmetVariation;
            }

            Character.SinkComp.Update();

            MyHud.Notifications.Add(m_helmetToggleNotification);

            if (m_soundEmitter != null)
            {
                bool force2D = false;
                if (Character == MySession.Static.LocalCharacter)
                    force2D = true;
                if (NeedsOxygenFromSuit)
                    m_soundEmitter.PlaySound(m_helmetOpenSound, true, force2D: force2D);
                else
                    m_soundEmitter.PlaySound(m_helmetCloseSound, true, force2D: force2D);
                if(MySession.Static.CreativeMode == false && NeedsOxygenFromSuit && Character.AtmosphereDetectorComp != null && Character.AtmosphereDetectorComp.InAtmosphere == false
                    && Character.AtmosphereDetectorComp.InShipOrStation == false && SuitOxygenAmount >= 0.5f)
                    m_soundEmitter.PlaySound(m_helmetAirEscapeSound, false, force2D: force2D);
            }
            if (MyFakes.ENABLE_NEW_SOUNDS && MyFakes.ENABLE_NEW_SOUNDS_QUICK_UPDATE && MySession.Static.Settings.RealisticSound)
                MyEntity3DSoundEmitter.UpdateEntityEmitters(true, true, false);
        }

        private void AnimateHelmet()
        {
            // old animation system, will be changed to new anim system in the future
            // todo: this should be independent on animation/layer names
            string animationOpenHelmet, animationCloseHelmet;
            Character.Definition.AnimationNameToSubtypeName.TryGetValue("HelmetOpen", out animationOpenHelmet);
            Character.Definition.AnimationNameToSubtypeName.TryGetValue("HelmetClose", out animationCloseHelmet);
            if (Character.Definition != null)
            {
                if (NeedsOxygenFromSuit && animationOpenHelmet != null)
                {
                    Character.PlayCharacterAnimation(animationOpenHelmet,
                        MyBlendOption.Immediate,
                        MyFrameOption.StayOnLastFrame, 0.2f, 1.0f, true);
                }
                else if (!NeedsOxygenFromSuit && animationCloseHelmet != null)
                {
                    Character.PlayCharacterAnimation(animationCloseHelmet,
                        MyBlendOption.Immediate,
                        MyFrameOption.StayOnLastFrame, 0.2f, 1.0f, true);
                }
            }
        }

        public void ShowRefillFromBottleNotification(MyDefinitionId gasType)
        {
            if (gasType == OxygenId)
                MyHud.Notifications.Add(m_oxygenBottleRefillNotification);
            else
                MyHud.Notifications.Add(m_gasBottleRefillNotification);
        }

        public bool ContainsGasStorage(MyDefinitionId gasId)
        {
            return m_gasIdToIndex.ContainsKey(gasId);
        }

        private bool TryGetGasData(MyDefinitionId gasId, out GasData data)
        {
            int index = -1;
            data = null;

            if (TryGetTypeIndex(ref gasId, out index))
            {
                data = m_storedGases[index];
                return true;
            }

            return false;
        }

        public float GetGasFillLevel(MyDefinitionId gasId)
        {
            int gasIndex = -1;
            if (!TryGetTypeIndex(ref gasId, out gasIndex))
                return 0f;

            return m_storedGases[gasIndex].FillLevel;
        }

        public void UpdateStoredGasLevel(ref MyDefinitionId gasId, float fillLevel)
        {
            int gasIndex = -1;
            if (!TryGetTypeIndex(ref gasId, out gasIndex))
                return;

            m_storedGases[gasIndex].FillLevel = fillLevel;
            CharacterGasSource.SetRemainingCapacityByType(gasId, fillLevel * m_storedGases[gasIndex].MaxCapacity);
            CharacterGasSource.SetProductionEnabledByType(gasId, fillLevel > 0);
        }

        private void TransferSuitGas(ref MyDefinitionId gasId, float gasInput, float gasOutput)
        {
            int gasIndex = GetTypeIndex(ref gasId);

            float gasTransfer = gasInput - gasOutput;

            if (MySession.Static.CreativeMode)
                gasTransfer = Math.Max(gasTransfer, 0f);

            if (gasTransfer == 0f)
                return;

            var gasInfo = m_storedGases[gasIndex];
            gasInfo.FillLevel = MathHelper.Clamp(gasInfo.FillLevel + gasTransfer / gasInfo.MaxCapacity, 0f, 1f);
            CharacterGasSource.SetRemainingCapacityByType(gasInfo.Id, gasInfo.FillLevel * gasInfo.MaxCapacity);
            CharacterGasSource.SetProductionEnabledByType(gasInfo.Id, gasInfo.FillLevel > 0);
        }

        private void Source_CurrentOutputChanged(MyDefinitionId changedResourceId, float oldOutput, MyResourceSourceComponent source)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref changedResourceId, out typeIndex))
                return;

            float timeSinceLastOutputSeconds = (MySession.Static.GameplayFrameCounter - m_storedGases[typeIndex].LastOutputTime) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_storedGases[typeIndex].LastOutputTime = MySession.Static.GameplayFrameCounter;
            float outputAmount = oldOutput*timeSinceLastOutputSeconds;

            m_storedGases[typeIndex].NextGasTransfer -= outputAmount;
        }

        private void Sink_CurrentInputChanged(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref resourceTypeId, out typeIndex))
                return;

            float timeSinceLastInputSeconds = (MySession.Static.GameplayFrameCounter - m_storedGases[typeIndex].LastInputTime) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_storedGases[typeIndex].LastInputTime = MySession.Static.GameplayFrameCounter;
            float inputAmount = oldInput * timeSinceLastInputSeconds;

            m_storedGases[typeIndex].NextGasTransfer += inputAmount;
        }

        private void SetGasSink(MyResourceSinkComponent characterSinkComponent)
        {
            foreach(var gasInfo in m_storedGases)
            {
                gasInfo.LastInputTime = MySession.Static.GameplayFrameCounter;
                if (!Sync.IsServer)
                    continue;

                if( m_characterGasSink != null )
                {
                    m_characterGasSink.CurrentInputChanged -= Sink_CurrentInputChanged;
                }

                if(characterSinkComponent != null)
                {
                    characterSinkComponent.CurrentInputChanged += Sink_CurrentInputChanged;
                }
            }
            m_characterGasSink = characterSinkComponent;
        }

        private void SetGasSource(MyResourceSourceComponent characterSourceComponent)
        {
            foreach (var gasInfo in m_storedGases)
            {
                gasInfo.LastOutputTime = MySession.Static.GameplayFrameCounter;
                if (m_characterGasSource != null)
                {
                    m_characterGasSource.SetRemainingCapacityByType(gasInfo.Id, 0);

                    if(Sync.IsServer)
                        m_characterGasSource.OutputChanged -= Source_CurrentOutputChanged;
                }

                if (characterSourceComponent != null)
                {
                    characterSourceComponent.SetRemainingCapacityByType(gasInfo.Id, gasInfo.FillLevel*gasInfo.MaxCapacity);
                    characterSourceComponent.SetProductionEnabledByType(gasInfo.Id, gasInfo.FillLevel > 0);
                    if(Sync.IsServer)
                        characterSourceComponent.OutputChanged += Source_CurrentOutputChanged;
                }
            }
            m_characterGasSource = characterSourceComponent;
        }

        public void AppendSinkData(List<MyResourceSinkInfo> sinkData)
        {
            Debug.Assert(sinkData != null, "AppendSinkData called with null list!");
            for(int gasIndex = 0; gasIndex < m_storedGases.Length; ++gasIndex)
            {
                int captureIndex = gasIndex;
                sinkData.Add(new MyResourceSinkInfo
                {
                    ResourceTypeId = m_storedGases[gasIndex].Id,
                    MaxRequiredInput = m_storedGases[gasIndex].Throughput,
                    RequiredInputFunc = () => Sink_ComputeRequiredGas(m_storedGases[captureIndex]),
                });
            }
        }

        public void AppendSourceData(List<MyResourceSourceInfo> sourceData)
        {
            Debug.Assert(sourceData != null, "AppendSourceData called with null list!");
            for (int gasIndex = 0; gasIndex < m_storedGases.Length; ++gasIndex)
            {
                sourceData.Add(new MyResourceSourceInfo
                {
                    ResourceTypeId = m_storedGases[gasIndex].Id,
                    DefinedOutput = m_storedGases[gasIndex].Throughput,
                    ProductionToCapacityMultiplier = 1f,
                    IsInfiniteCapacity = false,
                });
            }
        }

        private float Sink_ComputeRequiredGas(GasData gas)
        {
            float inputToFillInUpdateInterval = ((1 - gas.FillLevel) * gas.MaxCapacity + (gas.Id == OxygenId ? Definition.OxygenConsumption * Definition.OxygenConsumptionMultiplier : 0f)) / VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND * m_updateInterval;
            return Math.Min(inputToFillInUpdateInterval, gas.Throughput);
        }

        private int GetTypeIndex(ref MyDefinitionId gasId)
        {
            int typeIndex = 0;
            if (m_gasIdToIndex.Count > 1)
                typeIndex = m_gasIdToIndex[gasId];
            return typeIndex;
        }

        private bool TryGetTypeIndex(ref MyDefinitionId gasId, out int typeIndex)
        {
            return m_gasIdToIndex.TryGetValue(gasId, out typeIndex);
        }
    }
}
