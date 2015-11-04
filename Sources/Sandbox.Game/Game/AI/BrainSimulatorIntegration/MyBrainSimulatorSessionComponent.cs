using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Input;
using VRageMath;

namespace Sandbox.Game.AI.BrainSimulatorIntegration
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    public class MyBrainSimulatorSessionComponent : MySessionComponentBase
    {
        private MyPlayer m_botPlayer = null;
        private bool m_playerRequestSent = false;
        private bool m_respawnRequestSent = false;

        public override bool IsRequiredByGame
        {
            get
            {
                return MyFakes.ENABLE_BRAIN_SIMULATOR;
            }
        }

        public override void LoadData()
        {
            base.LoadData();

            Sync.Players.NewPlayerRequestSucceeded += Players_NewPlayerRequestSucceeded;
            Sync.Players.NewPlayerRequestFailed += Players_NewPlayerRequestFailed;
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            m_botPlayer = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(Sync.MyId, 1));
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            Sync.Players.NewPlayerRequestSucceeded -= Players_NewPlayerRequestSucceeded;
            Sync.Players.NewPlayerRequestFailed -= Players_NewPlayerRequestFailed;
        }

        void Players_NewPlayerRequestSucceeded(int playerSerialId)
        {
            m_botPlayer = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(Sync.MyId, 1));
        }

        void Players_NewPlayerRequestFailed(int playerSerialId)
        {
            MyGuiSandbox.CreateMessageBox(messageText: new StringBuilder("Could not create a new player for the bot!"));
            m_playerRequestSent = false;
        }

        public override void Simulate()
        {
            base.Simulate();

            if (m_botPlayer != null)
            {
                if (m_botPlayer.Identity.IsDead)
                {
                    if (!m_respawnRequestSent)
                    {
                        MyPlayerCollection.RespawnRequest(false, false, 0, null, m_botPlayer.Id.SerialId);
                        m_respawnRequestSent = true;
                    }
                }
                else
                {
                    m_respawnRequestSent = false;

                    // TODO: Add your bot logic here
                    if (m_botPlayer.Character != null)
                    {
                        MyCharacter character = m_botPlayer.Character;

                        // Moving and/or the character (run the game an you'll see what this code does...)
                        character.MoveAndRotate(Vector3.Zero, new Vector2(0.0f, 10.0f), 0.0f);

                        /*
                        // Enabling/disabling the jetpack
                        character.TurnOnJetpack(true);
                        character.TurnOnJetpack(false);

                        // Querying position and orientation of the character:
                        Vector3D positionInWorld = character.PositionComp.GetPosition();
                        Vector3D forwardUnitVector = character.PositionComp.WorldMatrix.Forward;

                        // These can be found in KeenSWH\Sandbox\Sources\SpaceEngineers\Content\Data\PhysicalItems.sbc ...
                        MyDefinitionId welderDefId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "WelderItem");
                        MyDefinitionId grinderDefId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "AngleGrinderItem");
                        MyDefinitionId rifleDefId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "AutomaticRifleItem");

                        // Switching to rifle:
                        if (character.CanSwitchToWeapon(rifleDefId))
                        {
                            character.SwitchToWeapon(rifleDefId);
                        }
                        else
                        {
                            // Switching failed, probably because the object was not in the inventory
                        }

                        // Shooting with whatever you have in the hand
                        // (Note that after switching to a gun, you'll have to wait until it really is in your hand, which might take a while
                        // if you're the client and you're connected to a server.)
                        character.BeginShoot(MyShootActionEnum.PrimaryAction);
                        // Several frames later:
                        character.EndShoot(MyShootActionEnum.PrimaryAction);

                        // Getting entities in an area of 10m around the character:
                        BoundingSphereD sphere = new BoundingSphereD(positionInWorld, 10.0f);
                        List<MyEntity> result = MyEntities.GetEntitiesInSphere(ref sphere);
                        foreach (MyEntity entity in result)
                        {
                            // We only want to check CubeGrids, for example (i.e. stations and ships)
                            MyCubeGrid grid = entity as MyCubeGrid;
                            if (grid != null)
                            {
                                HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(); // Consider preallocating this
                                grid.GetBlocksInsideSphere(ref sphere, blocks);

                                foreach (MySlimBlock block in blocks)
                                {
                                    // You can query the block here:

                                    // Type of the block (as found in KeenSWH\Sandbox\Sources\SpaceEngineers\Content\Data\CubeBlocks.sbc)
                                    var blockDefinitionId = block.BlockDefinition.Id;
                                    // Position in world (probably center, but I don't remember precisely now)
                                    Vector3D worldPosition = grid.GridIntegerToWorld(block.Position);

                                    // Min and max positions in world.
                                    // This is important if the block is larger than 1x1x1 (again, probably centers of those min and max cubes)
                                    Vector3D minWorldPosition = grid.GridIntegerToWorld(block.Min);
                                    Vector3D maxWorldPosition = grid.GridIntegerToWorld(block.Max);

                                    // This will contain dimensions of the block. Beware: it does not have to be the same as block.Min - block.Max (because of rotations)
                                    Vector3I dims = block.BlockDefinition.Size;

                                    // Etc... VisualStudio IntelliSense is your friend ;-)
                                }
                            }

                            // How large is one cube in the grid
                            float blockSizeInMeters = grid.GridSize;
                        }*/
                    }
                }
            }
        }

        public override void HandleInput()
        {
            base.HandleInput();

            if (MyInput.Static.IsAnyCtrlKeyPressed() &&
                MyInput.Static.IsAnyAltKeyPressed() &&
                MyInput.Static.IsAnyShiftKeyPressed() &&
                MyInput.Static.IsLeftMousePressed())
            {
                if (m_playerRequestSent == false &&
                    m_botPlayer == null)
                {
                    Sync.Players.RequestNewPlayer(1, "BrainSimulator", "Default_Astronaut");
                    m_playerRequestSent = true;
                }
            }
        }
    }
}
