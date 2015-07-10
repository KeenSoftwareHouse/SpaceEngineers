using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Localization;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using VRage.Utils;
using VRage.Audio;
using VRage.Library.Utils;
using Sandbox.Game.Entities;

namespace SpaceEngineers.Game.World
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    class MyApril2014Event : MySessionComponentBase
    {
        enum MyLemmingAction
        {
            None,
            Jump,
            Crouch,
            Sit
        }

        struct MyLemmingPosition
        {
            public Vector3 Position;
            public Vector2 Rotation;
            public MyLemmingAction Action;
        }

        class MyLemmingCharacter
        {
            static float LEMMING_SPEED = 0.1f;
            public static List<MyLemmingPosition> Positions = new List<MyLemmingPosition>();

            enum MyLemmingState
            {
                FindNextPosition,
                Go,
                Wait
            }

            public MyCharacter Character;
            public MyLemmingPosition CurrentPosition;
            MyLemmingState State = MyLemmingState.FindNextPosition;
            int m_positionIndex = 0;

            public void Update(bool changeState)
            {
                if (changeState || State == MyLemmingState.FindNextPosition)
                {
                    FindNextPosition();
                }
                else
                    Character.MoveAndRotate(CurrentPosition.Position, CurrentPosition.Rotation, 0);
            }

            public void FindNextPosition()
            {
                if (m_positionIndex > (Positions.Count - 1))
                {

                    Vector3 posM = new Vector3(
                        0,// MyVRageUtils.GetRandomFloat(0,1),
                        0,
                        MyUtils.GetRandomFloat(0, -1));
                    Vector2 rot = new Vector2(
                        0,
                        MyUtils.GetRandomFloat(-10.0f, 10.0f));

                    MyLemmingPosition pos = new MyLemmingPosition()
                    {
                        //Position = Character.GetPosition() + dir
                        Position = posM,
                        Rotation = rot
                    };

                    if (MyUtils.GetRandomFloat(0, 1) < 0.2f)
                    {
                        pos.Action = MyLemmingAction.Jump;
                    }
                    else
                        if (MyUtils.GetRandomFloat(0, 1) < 0.3f)
                        {
                            pos.Action = MyLemmingAction.Crouch;
                        }
                        else
                            if (MyUtils.GetRandomFloat(0, 1) < 0.2f)
                            {
                                pos.Action = MyLemmingAction.Sit;
                            }

                    Positions.Add(pos);
                    CurrentPosition = pos;
                    SetState(MyLemmingState.Go);
                }
                else
                {
                    CurrentPosition = Positions[m_positionIndex];
                    SetState(MyLemmingState.Go);
                }

                if (State != MyLemmingState.FindNextPosition)
                {
                    switch (Positions[m_positionIndex].Action)
                    {
                        case MyLemmingAction.Jump:
                            Character.Jump();
                            break;
                        case MyLemmingAction.Crouch:
                            Character.Crouch();
                            break;
                        case MyLemmingAction.Sit:
                            break;
                        case MyLemmingAction.None:
                            break;
                    }

                    m_positionIndex++;
                }
            }

            void SetState(MyLemmingState state)
            {
                State = state;
            }

            public void DebugDraw()
            {
               // VRageRender.MyRenderProxy.DebugDrawLine3D(Character.GetPosition(), NextPosition, Color.White, Color.Yellow, false);
            }
        }

        static List<MyEntity> m_getEntities = new List<MyEntity>();
        static List<MyLemmingCharacter> m_lemmings = new List<MyLemmingCharacter>();

        static MyMedicalRoom m_spawnMedical = null;
        static int SPAWN_TIME = 500;
        static int SPAWN_COUNT = 20;
        static int LIFE_TIME = 60 * 1000;
        static int m_spawnCount = SPAWN_COUNT;
        static int m_timeToNextSpawn = -1;
        static int m_timeToNextChange = -1;
        static int m_timeToEnd = LIFE_TIME;
        static bool m_started = false;
        static MyGlobalEventBase m_globalEvent;
        static MyHudNotification m_notification;


        public override void LoadData()
        {
            base.LoadData();
        }

        protected override void UnloadData()
        {
            m_getEntities.Clear();
            m_lemmings.Clear();
            m_timeToNextSpawn = -1;
            m_timeToNextChange = -1;
            m_spawnMedical = null;
            m_spawnCount = SPAWN_COUNT;
            MyLemmingCharacter.Positions.Clear();
            m_started = false;
            m_globalEvent = null;
            m_timeToEnd = LIFE_TIME;

            if (m_notification != null)
            {
                MyHud.Notifications.Remove(m_notification);
                m_notification = null;
            }

            base.UnloadData();
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            if (!Sync.IsServer) return;

            var april2014Event = MyGlobalEvents.GetEventById(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventDefinition), "April2014"));
            bool eventEnabled = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day) == new DateTime(2014, 4, 1) || MyFakes.APRIL2014_ENABLED;
            if (april2014Event == null && eventEnabled)
            {
                var globalEvent = MyGlobalEventFactory.CreateEvent<MyGlobalEventBase>(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventDefinition), "April2014"));
                MyGlobalEvents.AddGlobalEvent(globalEvent);
            }
            else if (april2014Event != null)
            {
                april2014Event.Enabled &= eventEnabled;
            }
        }

        private static MyStringId NO_RANDOM = MyStringId.GetOrCompute("NoRandom");
        private static MyStringId MUS_FUN = MyStringId.GetOrCompute("MustFun");
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (m_spawnMedical == null)
                return;

            if (m_started && m_lemmings.Count == 0)
            {
                if (Vector3.Distance((Vector3)((MyEntity)MySession.LocalHumanPlayer.Identity.Character).PositionComp.GetPosition(), (Vector3)m_spawnMedical.PositionComp.GetPosition()) < 6)
                {
                    SpawnLemming();
                    m_timeToNextSpawn = SPAWN_TIME;
                    m_timeToNextChange = SPAWN_TIME;

                    MyAudio.Static.ApplyTransition(NO_RANDOM, 0, MUS_FUN, false);
                    MyGlobalEvents.SyncObject.PlayMusic(NO_RANDOM, MUS_FUN, false);

                    m_globalEvent.Enabled = false;

                    m_notification = new MyHudNotification(MySpaceTexts.FirstApril2014, 60 * 1000, level: MyNotificationLevel.Important);
                    MyHud.Notifications.Add(m_notification);

                    MyGlobalEvents.SyncObject.ShowNotification(MySpaceTexts.FirstApril2014, 60 * 1000);
                }
            }

            if (m_timeToNextSpawn >= 0)
            {
                m_timeToNextSpawn -= MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
                if (m_timeToNextSpawn <= 0)
                {
                    m_timeToNextSpawn = SPAWN_TIME;
                    SpawnLemming();
                }
            }

            bool changeState = false;
            if (m_timeToNextChange >= 0)
            {
                m_timeToNextChange -= MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
                if (m_timeToNextChange <= 0)
                {
                    m_timeToNextChange = SPAWN_TIME;
                    changeState = true;
                }
            }

            foreach (var lemming in m_lemmings)
            {
                lemming.Update(changeState);                
            }

            if (m_lemmings.Count > 0)
            {
                m_timeToEnd -= MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
                if (m_timeToEnd <= 0)
                {
                    foreach (var lemming in m_lemmings)
                    {
                        lemming.Character.Close();
                    }
                    m_lemmings.Clear();
                    m_started = false;
                }
            }

        }

        private static void SpawnLemming()
        {
            m_spawnCount--;

            if (m_spawnCount <= 0)
            {
                m_timeToNextSpawn = -1;
                return;
            }

            Vector3D spawnPosition = m_spawnMedical.PositionComp.GetPosition() + (MyMedicalRoom.GetSafePlaceRelative() * (Matrix)m_spawnMedical.WorldMatrix).Translation;
            MatrixD matrix = m_spawnMedical.WorldMatrix;
            matrix.Translation = spawnPosition;

            var character = MyCharacter.CreateCharacter((Matrix)matrix, Vector3.Zero, "Lemming" + m_lemmings.Count, null, null, false, true);
            character.EnableJetpack(false, false, true, true);
            character.AIMode = true;
            MatrixD m = character.WorldMatrix;
            m = m * Matrix.CreateRotationY(-MathHelper.PiOver2);
            m.Translation = character.PositionComp.GetPosition();
            character.PositionComp.SetWorldMatrix(m);
            character.Save = false;
            MyLemmingCharacter lemming = new MyLemmingCharacter()
            {
                Character = character,
            };
            m_lemmings.Add(lemming);
        }

        private static void StartSurprise(object senderEvent)
        {
            BoundingSphereD sphere = new BoundingSphereD(new Vector3D(-18.75f, -2.5f, -1.25f), 2);
            m_getEntities.Clear();
            MyGamePruningStructure.GetAllEntitiesInSphere(ref sphere, m_getEntities);

            m_spawnMedical = null;
            foreach (var entity in m_getEntities)
            {
                m_spawnMedical = entity as MyMedicalRoom;
                if (m_spawnMedical != null)
                {
                    m_spawnMedical.OnClose += delegate { m_spawnMedical = null; };
                    break;
                }
            }

            m_started = true;
        }

        public override void Draw()
        {
            base.Draw();

            foreach (var lemming in m_lemmings)
            {
                lemming.DebugDraw();
            }
        }

        [MyGlobalEventHandler(typeof(MyObjectBuilder_GlobalEventDefinition), "April2014")]
        public static void Surprise(object senderEvent)
        {
            if (!m_started)
            {
                m_globalEvent = ((MyGlobalEventBase)senderEvent);
                StartSurprise(senderEvent);
            }
        }
    }
}
