#region Using

using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Game.Components;
using VRage.FileSystem;
using VRage.Game.Entity.UseObject;
using VRage.Game.ObjectBuilders;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using IMyModdingControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;

#endregion

namespace Sandbox.Game.Entities.Character
{
    public abstract class MyCharacterComponent : MyEntityComponentBase
    {

        private bool m_needsUpdateAfterSimulation;

        /// <summary>
        /// This set's flag for update. Set it after add to container!
        /// </summary>
        public bool NeedsUpdateAfterSimulation
        {
            get { return m_needsUpdateAfterSimulation; }
            set { m_needsUpdateAfterSimulation = value; Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME; }
        }

        private bool m_needsUpdateAfterSimulation10;

        /// <summary>
        /// This set's flag for update. Set it after add to container!
        /// </summary>
        public bool NeedsUpdateAfterSimulation10
        {
            get { return m_needsUpdateAfterSimulation10; }
            set { m_needsUpdateAfterSimulation10 = value; Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME; }
        }

        private bool m_needsUpdateBeforeSimulation100;

        /// <summary>
        /// This set's flag for update. Set it after add to container!
        /// </summary>
        public bool NeedsUpdateBeforeSimulation100
        {
            get { return m_needsUpdateBeforeSimulation100; }
            set { m_needsUpdateBeforeSimulation100 = value; Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME; }
        }        

        private bool m_needsUpdateBeforeSimulation;

        /// <summary>
        /// This set's flag for update. Set it after add to container!
        /// </summary>
        public bool NeedsUpdateBeforeSimulation
        {
            get { return m_needsUpdateBeforeSimulation; }
            set { m_needsUpdateBeforeSimulation = value; Entity.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME; }
        }               

        public MyCharacter Character { get { return (MyCharacter)Entity; } }
               

        public virtual void UpdateAfterSimulation10()
        {

        }

        public virtual void UpdateBeforeSimulation()
        {

        }

        public virtual void UpdateAfterSimulation()
        {

        }

        public virtual void UpdateBeforeSimulation100()
        {

        }

        public override string ComponentTypeDebugString
        {
            get { return "Character Component"; }
        }

        public virtual void OnCharacterDead()
        {

        }
    }
}
