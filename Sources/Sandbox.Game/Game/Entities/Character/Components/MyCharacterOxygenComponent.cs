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
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using VRage.Components;

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

            public override string ToString() { return string.Format("Subtype: {0}, FillLevel: {1}, CurrentCapacity: {2}, MaxCapacity: {3}", Id.SubtypeName, FillLevel, FillLevel*MaxCapacity, MaxCapacity); }
        }

        public static readonly float LOW_OXYGEN_RATIO = 0.2f;

        private Dictionary<MyDefinitionId, int> m_gasIdToIndex; 
        private GasData[] m_storedGases;

        public float EnvironmentOxygenLevel;

        private float m_oldSuitOxygenLevel;
        private float m_suitOxygenAmount;
        private bool m_needsOxygen;

        private float m_gasOutputTime = 0f;

        private MyResourceSinkComponent m_characterGasSink;
        private MyResourceSourceComponent m_characterGasSource;
        public MyResourceSinkComponent CharacterGasSink { get { return m_characterGasSink; } set { SetGasSink(value); } }
        public MyResourceSourceComponent CharacterGasSource { get { return m_characterGasSource; } set { SetGasSource(value); } }

		public static readonly MyDefinitionId OxygenId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");
        public static readonly MyDefinitionId HydrogenId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Hydrogen");

        MyHudNotification m_lowOxygenNotification;
        MyHudNotification m_criticalOxygenNotification;
        MyHudNotification m_oxygenBottleRefillNotification;
        MyHudNotification m_helmetToggleNotification;

        #region Properties
        private MyCharacterDefinition Definition { get { return Character.Definition; } }

        public float SuitOxygenAmount
        {
            get { return m_suitOxygenAmount; }
            set
            {
                m_suitOxygenAmount = value;
                if (m_suitOxygenAmount > Definition.OxygenCapacity)
                {
                    m_suitOxygenAmount = Definition.OxygenCapacity;
                }
            }
        }

        public float SuitOxygenAmountMissing {  get { return Definition.OxygenCapacity - SuitOxygenAmount; } }

        public float SuitOxygenLevel
        {
            get
            {
                if (Definition.OxygenCapacity == 0)
                {
                    return 0;
                }
                return m_suitOxygenAmount / Definition.OxygenCapacity;
            }
            set
            {
                m_suitOxygenAmount = value * Definition.OxygenCapacity;
            }
        }

        public bool IsOxygenLevelLow { get { return MyHud.CharacterInfo.OxygenLevel < LOW_OXYGEN_RATIO; } }

        public bool EnabledHelmet { get { return !m_needsOxygen; } }
        #endregion

        public override string ComponentTypeDebugString { get { return "Oxygen Component"; } }

        public virtual void Init(MyObjectBuilder_Character characterOb)
        {
            if (MySession.Static.SurvivalMode)
            {
                m_suitOxygenAmount = characterOb.OxygenLevel * Definition.OxygenCapacity;
            }
            else
            {
                m_suitOxygenAmount = Definition.OxygenCapacity;
            }
            m_oldSuitOxygenLevel = SuitOxygenLevel;

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
                        Throughput = gasInfo.Throughput
                    };
                    m_gasIdToIndex.Add(gasInfo.Id, gasIndex);
                }

                if (characterOb.StoredGases != null)
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
            if(m_storedGases == null)
                m_storedGases = new GasData[0];

            m_oxygenBottleRefillNotification = new MyHudNotification(text: MySpaceTexts.NotificationBottleRefill, level: MyNotificationLevel.Important);
            m_lowOxygenNotification = new MyHudNotification(text: MySpaceTexts.NotificationOxygenLow, font: MyFontEnum.Red, level: MyNotificationLevel.Important);
            m_criticalOxygenNotification = new MyHudNotification(text: MySpaceTexts.NotificationOxygenCritical, font: MyFontEnum.Red, level: MyNotificationLevel.Important);
            m_helmetToggleNotification = m_helmetToggleNotification ?? new MyHudNotification(); // Init() is called when toggling helmet so this check is required

            m_needsOxygen = Definition.NeedsOxygen;

            NeedsUpdateBeforeSimulation100 = true;
        }

        public virtual void GetObjectBuilder(MyObjectBuilder_Character objectBuilder)
        {
            objectBuilder.OxygenLevel = SuitOxygenLevel;

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
            MyGamePruningStructure.GetAllTopMostEntitiesInBox(ref aabb, entities);
            bool lowOxygenDamage = MySession.Static.Settings.EnableOxygen;
            bool noOxygenDamage = MySession.Static.Settings.EnableOxygen;
            bool isInEnvironment = true;

            EnvironmentOxygenLevel = MyOxygenProviderSystem.GetOxygenInPoint(Character.PositionComp.GetPosition());

            bool oxygenReplenished = false;

            if (Sync.IsServer)
            {
                if (MySession.Static.Settings.EnableOxygen)
                {
                    float oxygenInput = CharacterGasSink.CurrentInputByType(OxygenId);
                    if (oxygenInput > 0 && !Definition.NeedsOxygen)
                    {
                        var oxygenInputPer100Frames = oxygenInput * 100f / MyEngineConstants.UPDATE_STEPS_PER_SECOND;
                        SuitOxygenAmount += oxygenInputPer100Frames;

                        if (oxygenInputPer100Frames >= Definition.OxygenConsumption)
                        {
                            oxygenReplenished = true;
                            noOxygenDamage = false;
                            lowOxygenDamage = false;
                        }
                    }
                }

                foreach (GasData gasInfo in m_storedGases)
                {
                    float gasInputPer100Frames = Math.Min(gasInfo.Throughput, CharacterGasSink.CurrentInputByType(gasInfo.Id))*100f/MyEngineConstants.UPDATE_STEPS_PER_SECOND;
                    float gasOutputPer100Frames = Math.Min(gasInfo.Throughput, CharacterGasSource.CurrentOutputByType(gasInfo.Id))*100f/MyEngineConstants.UPDATE_STEPS_PER_SECOND;
                    TransferSuitGas(gasInfo.Id, gasInputPer100Frames, gasOutputPer100Frames);
                    m_gasOutputTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                }
            }

            if (MySession.Static.Settings.EnableOxygen)
            {
                var cockpit = Character.Parent as MyCockpit;
                if (cockpit != null && cockpit.BlockDefinition.IsPressurized)
                {
                    if (Sync.IsServer && MySession.Static.SurvivalMode && !oxygenReplenished)
                    {
                        if (!Definition.NeedsOxygen)
                        {
                            if (cockpit.OxygenAmount >= Definition.OxygenConsumption)
                            {
                                cockpit.OxygenAmount -= Definition.OxygenConsumption;

                                noOxygenDamage = false;
                                lowOxygenDamage = false;
                            }
                            else if (m_suitOxygenAmount >= Definition.OxygenConsumption)
                            {
                                m_suitOxygenAmount -= Definition.OxygenConsumption;
                                noOxygenDamage = false;
                                lowOxygenDamage = false;
                            }
                        }
                        else if (Definition.NeedsOxygen)
                        {
                            if (cockpit.OxygenFillLevel > 0f)
                            {
                                if (cockpit.OxygenAmount >= Definition.OxygenConsumption)
                                {
                                    cockpit.OxygenAmount -= Definition.OxygenConsumption;

                                    noOxygenDamage = false;
                                    lowOxygenDamage = false;
                                }
                            }
                        }
                    }
                    EnvironmentOxygenLevel = cockpit.OxygenFillLevel;
                    isInEnvironment = false;
                }
                else
                {
                    Vector3D pos = Character.GetHeadMatrix(true, true, false, true).Translation;
                    foreach (var entity in entities)
                    {
                        var grid = entity as MyCubeGrid;
                        // Oxygen can be present on small grids as well because of mods
                        if (grid != null)
                        {
                            var oxygenBlock = grid.GridSystems.GasSystem.GetSafeOxygenBlock(pos);
                            if (oxygenBlock.Room != null)
                            {
                                if (oxygenBlock.Room.OxygenLevel(grid.GridSize) > Definition.PressureLevelForLowDamage)
                                {
                                    if (Definition.NeedsOxygen)
                                    {
                                        lowOxygenDamage = false;
                                    }
                                }

                                if (oxygenBlock.Room.IsPressurized)
                                {
                                    EnvironmentOxygenLevel = oxygenBlock.Room.OxygenLevel(grid.GridSize);
                                    if (oxygenBlock.Room.OxygenAmount > Definition.OxygenConsumption)
                                    {
                                        if (Definition.NeedsOxygen)
                                        {
                                            noOxygenDamage = false;
                                            oxygenBlock.PreviousOxygenAmount = oxygenBlock.OxygenAmount() - Definition.OxygenConsumption;
                                            oxygenBlock.OxygenChangeTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                                            if (!oxygenReplenished)
                                                oxygenBlock.Room.OxygenAmount -= Definition.OxygenConsumption;
                                        }
                                        break;
                                    }
                                }
                                else
                                {
                                    EnvironmentOxygenLevel = oxygenBlock.Room.EnvironmentOxygen;
                                    if (EnvironmentOxygenLevel > Definition.OxygenConsumption)
                                    {
                                        if (Definition.NeedsOxygen)
                                        {
                                            noOxygenDamage = false;
                                        }
                                        break;
                                    }
                                }

                                isInEnvironment = false;
                            }
                        }
                    }
                }


                if (MySession.LocalCharacter == Character)
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
            if (!Sync.IsServer || MySession.Static.CreativeMode || !MySession.Static.Settings.EnableOxygen)
                return;

            //Try to refill the suit from bottles in inventory
            if (SuitOxygenLevel < 0.3f && !Definition.NeedsOxygen)
            {
                var items = Character.Inventory.GetItems();
                bool bottlesUsed = false;
                foreach (var item in items)
                {
                    var oxygenContainer = item.Content as MyObjectBuilder_GasContainerObject;
                    if (oxygenContainer != null)
                    {
						if (oxygenContainer.GasLevel == 0f)
                            continue;

                        var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(oxygenContainer) as MyOxygenContainerDefinition;
	                    if (physicalItem.StoredGasId != OxygenId)
		                    continue;
						float oxygenAmount = oxygenContainer.GasLevel * physicalItem.Capacity;

                        float transferredAmount = Math.Min(oxygenAmount, SuitOxygenAmountMissing);
						oxygenContainer.GasLevel = (oxygenAmount - transferredAmount) / physicalItem.Capacity;

						if (oxygenContainer.GasLevel < 0f)
                        {
							oxygenContainer.GasLevel = 0f;
                        }

						if (oxygenContainer.GasLevel > 1f)
                        {
                            Debug.Fail("Incorrect value");
                        }

                        Character.Inventory.UpdateGasAmount();

                        bottlesUsed = true;

                        SuitOxygenAmount += transferredAmount;
                        if (SuitOxygenLevel == 1f)
                        {
                            break;
                        }
                    }
                }
                if (bottlesUsed)
                {
                    if (MySession.LocalCharacter == Character)
                    {
                        ShowRefillFromBottleNotification();
                    }
                    else
                    {
                        Character.SyncObject.SendRefillFromBottle();
                    }
                }
            }

            foreach (var gasInfo in m_storedGases)
            {
                if (gasInfo.FillLevel < 0.3f) // Get rid of the specific oxygen version of this 
                {
                    var items = Character.Inventory.GetItems();
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
                            float gasAmount = gasContainer.GasLevel*physicalItem.Capacity;

                            float transferredAmount = Math.Min(gasAmount, (1f - gasInfo.FillLevel)*gasInfo.MaxCapacity);
                            gasContainer.GasLevel = Math.Max((gasAmount - transferredAmount)/physicalItem.Capacity, 0f);

                            if (gasContainer.GasLevel > 1f)
                                Debug.Fail("Incorrect value");

                            Character.Inventory.UpdateGasAmount();

                            bottlesUsed = true;

                            gasInfo.FillLevel = Math.Min(gasInfo.FillLevel + transferredAmount/gasInfo.MaxCapacity, 1f);
                            if (gasInfo.FillLevel == 1f)
                                break;
                        }
                    }
                    if (bottlesUsed)
                    {
                        if (MySession.LocalCharacter == Character)
                            ShowRefillFromBottleNotification();
                        else
                            Character.SyncObject.SendRefillFromBottle();
                    }
                }
            }

            // No oxygen found in room, try to get it from suit
            if (noOxygenDamage || lowOxygenDamage)
            {
                if (!Definition.NeedsOxygen && m_suitOxygenAmount > Definition.OxygenConsumption)
                {
                    if (!oxygenReplenished)
                        m_suitOxygenAmount -= Definition.OxygenConsumption;
                    if (m_suitOxygenAmount < 0f)
                    {
                        m_suitOxygenAmount = 0f;
                    }
                    noOxygenDamage = false;
                    lowOxygenDamage = false;
                }

                if (isInEnvironment)
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

            if (noOxygenDamage)
            {
                Character.DoDamage(Definition.DamageAmountAtZeroPressure, MyDamageType.LowPressure, true);
            }
            else if (lowOxygenDamage)
            {
                Character.DoDamage(1f, MyDamageType.Asphyxia, true);
            }

            Character.SyncObject.UpdateOxygen(SuitOxygenAmount);
        }

        public void SwitchHelmet()
        {
            if (Character.IsDead)
            {
                return;
            }

            bool hasHelmetVariation = Definition.HelmetVariation != null;
            if (hasHelmetVariation)
            {
                bool variationExists = false;
                var characters = MyDefinitionManager.Static.Characters;
                foreach (var character in characters)
                {
                    if (character.Name == Definition.HelmetVariation)
                    {
                        variationExists = true;
                        break;
                    }
                }

                if (!variationExists)
                {
                    hasHelmetVariation = false;
                }
            }

            if (hasHelmetVariation)
            {
                Character.ChangeModelAndColor(Definition.HelmetVariation, Character.ColorMask);
                m_needsOxygen = !Definition.NeedsOxygen;
                m_helmetToggleNotification.Text = (Definition.NeedsOxygen ? MySpaceTexts.NotificationHelmetOn : MySpaceTexts.NotificationHelmetOff);
            }
            else
            {
                m_helmetToggleNotification.Text = MySpaceTexts.NotificationNoHelmetVariation;
            }

            MyHud.Notifications.Add(m_helmetToggleNotification);
        }

        public void ShowRefillFromBottleNotification()
        {
            MyHud.Notifications.Add(m_oxygenBottleRefillNotification);
        }

        public bool ContainsGasStorage(MyDefinitionId gasId)
        {
            return m_gasIdToIndex.ContainsKey(gasId);
        }

        public float GetGasFillLevel(MyDefinitionId gasId)
        {
            int gasIndex = -1;
            if (!m_gasIdToIndex.TryGetValue(gasId, out gasIndex))
                return 0f;

            return m_storedGases[gasIndex].FillLevel;
        }

        public void UpdateStoredGasLevel(MyDefinitionId gasId, float fillLevel)
        {
            int gasIndex = -1;
            if (!m_gasIdToIndex.TryGetValue(gasId, out gasIndex))
                return;

            m_storedGases[gasIndex].FillLevel = fillLevel;
        }

        private void TransferSuitGas(MyDefinitionId gasId, float gasInput, float gasOutput)
        {
            int gasIndex = -1;
            if (!m_gasIdToIndex.TryGetValue(gasId, out gasIndex))
                return;

            float gasTransfer = gasInput - gasOutput;

            if (MySession.Static.CreativeMode)
                gasTransfer = Math.Max(gasTransfer, 0f);

            if (gasTransfer == 0f)
                return;

            var gasInfo = m_storedGases[gasIndex];
            gasInfo.FillLevel = MathHelper.Clamp(gasInfo.FillLevel + gasTransfer / gasInfo.MaxCapacity, 0f, 1f);
            CharacterGasSource.SetRemainingCapacityByType(gasInfo.Id, gasInfo.FillLevel * gasInfo.MaxCapacity);

            if(gasOutput != 0f)
                m_gasOutputTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        private void Source_CurrentOutputChanged(MyDefinitionId changedResourceId, float oldOutput, MyResourceSourceComponent source)
        {
            float timeSinceLastOutputSeconds = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_gasOutputTime)/1000f;
            float outputAmount = oldOutput*timeSinceLastOutputSeconds;
            TransferSuitGas(changedResourceId, 0f, outputAmount);
        }

        private void SetGasSink(MyResourceSinkComponent characterSinkComponent)
        {
            m_characterGasSink = characterSinkComponent;
        }

        private void SetGasSource(MyResourceSourceComponent characterSourceComponent)
        {
            m_gasOutputTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            foreach (var gasInfo in m_storedGases)
            {
                if (m_characterGasSource != null)
                {
                    m_characterGasSource.SetRemainingCapacityByType(gasInfo.Id, 0);
                    m_characterGasSource.OutputChanged -= Source_CurrentOutputChanged;
                }

                if (characterSourceComponent != null)
                {
                    characterSourceComponent.SetRemainingCapacityByType(gasInfo.Id, gasInfo.FillLevel*gasInfo.MaxCapacity);
                    characterSourceComponent.SetProductionEnabledByType(gasInfo.Id, gasInfo.FillLevel > 0);
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
                int tmpIndex = gasIndex;
                sinkData.Add(new MyResourceSinkInfo
                {
                    ResourceTypeId = m_storedGases[gasIndex].Id,
                    MaxRequiredInput = m_storedGases[gasIndex].Throughput,
                    RequiredInputFunc = () => Math.Min((1 - m_storedGases[tmpIndex].FillLevel) * m_storedGases[tmpIndex].MaxCapacity, m_storedGases[tmpIndex].Throughput),
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
    }
}
