using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Game.GameSystems.Electricity;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Havok;
using System.Reflection;
using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens;
using Sandbox.Game.Screens.Terminal.Controls;
using VRage.Utils;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Localization;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Game.Entity;
using VRage;
using Sandbox.Engine.Multiplayer;
using VRage.Network;
using VRage.Game;
using VRage.Sync;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// GR: Added this class to be used as a base for all door classes. Added only very basic functionallity no new definitions or object builders.
    /// The main issue was that door actions (open / close) couldn't be used in groups because they were not inheriting from same class.
    /// Instead were inheriting directly from MyFunctionalBlock so this class is used in between for common attributes.
    /// </summary>
    public abstract class MyDoorBase : MyFunctionalBlock
    {
        public MyDoorBase()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_open = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();
        }

        protected readonly Sync<bool> m_open;
        
        public bool Open
        {
            get
            {
                return m_open;
            }
            set
            {
                if (m_open != value && Enabled && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
                {
                    m_open.Value = value;
                }
            }
        }
        
        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyDoorBase>())
                return;
            base.CreateTerminalControls();
            var open = new MyTerminalControlOnOffSwitch<MyDoorBase>("Open", MySpaceTexts.Blank, on: MySpaceTexts.BlockAction_DoorOpen, off: MySpaceTexts.BlockAction_DoorClosed);
            open.Getter = (x) => x.Open;
            open.Setter = (x, v) => x.SetOpenRequest(v, x.OwnerId);
            open.EnableToggleAction();
            open.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(open);
        }

        public void SetOpenRequest(bool open, long identityId)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OpenRequest, open, identityId);
        }

        [Event, Reliable, Server]
        void OpenRequest(bool open, long identityId)
        {
            VRage.Game.MyRelationsBetweenPlayerAndBlock relation = GetUserRelationToOwner(identityId);

            MyIdentity identity = MySession.Static.Players.TryGetIdentity(identityId);
            MyPlayer player = identity != null && identity.Character != null ? MyPlayer.GetPlayerFromCharacter(identity.Character) : null;
            if (relation.IsFriendly() ||
                (identity != null && identity.Character != null && player != null && MySession.Static.IsUserSpaceMaster(player.Client.SteamUserId)))
            {
                Open = open;
            }
        }
    }
}
