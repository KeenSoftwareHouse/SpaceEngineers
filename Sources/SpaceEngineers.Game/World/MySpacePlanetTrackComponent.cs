using Sandbox;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Game.Components;
using VRage.Data.Audio;
using VRage.Game.ObjectBuilders.Components;
using VRage.Utils;
using VRageMath;
using VRageMath.Spatial;
using VRageRender;
using VRage.Game.Entity;

namespace SpaceEngineers
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 503, typeof(MyObjectBuilder_SpacePlanetTrackComponent))]
    public class MySpacePlanetTrackComponent : MySessionComponentBase
    {
        private static readonly int UPDATE_DELAY = 120;
        private int m_waitForUpdate = 0;

        private Dictionary<long, MyPlanet> m_planets = new Dictionary<long, MyPlanet>();

        MyPlanet m_closestPlanet;


        public override bool IsRequiredByGame
        {
            get
            {
                return true;
            }
        }

        static MySpacePlanetTrackComponent()
        {
        }

        public override void LoadData()
        {
            base.LoadData();

            MyEntities.OnEntityAdd += EntityAdded;
            MyEntities.OnEntityRemove += EntityRemoved;
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            MyEntities.OnEntityAdd -= EntityAdded;
            MyEntities.OnEntityRemove -= EntityRemoved;

            m_planets.Clear();
        }

        private void EntityAdded(MyEntity entity)
        {
            var planet = entity as MyPlanet;
            if (planet != null)
            {
                m_planets.Add(entity.EntityId, planet);
            }
        }

        private void EntityRemoved(MyEntity entity)
        {
            var planet = entity as MyPlanet;
            if (planet != null)
            {
                m_planets.Remove(entity.EntityId);
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            m_waitForUpdate--;
            if (m_waitForUpdate > 0)
            {
                return;
            }
            m_waitForUpdate = UPDATE_DELAY;


            Vector3D position = MySector.MainCamera.Position;


            MyPlanet planet = MyGravityProviderSystem.GetNearestPlanet(position);

            if (planet != null)
            {
                var distanceFromPlanet = Vector3D.Distance(planet.PositionComp.GetPosition(), position);
                if (distanceFromPlanet > planet.MaximumRadius + Math.Max(100, planet.MaximumRadius)) ;
                planet = null; //too far planet
            }

            if (m_closestPlanet != planet)
            {
                m_closestPlanet = planet;

                if (m_closestPlanet != null)
                {
                    MyStringId category = MyStringId.GetOrCompute(m_closestPlanet.Generator.Id.SubtypeId.ToString());
                    MyStringId transition = MyStringId.GetOrCompute(m_closestPlanet.Generator.Id.SubtypeId.ToString());

                    if (!MyAudio.Static.IsValidTransitionCategory(transition, category))
                    {
                        category = MyStringId.GetOrCompute("OtherPlanet");
                        transition = MyStringId.GetOrCompute("OtherPlanet");
                    }

                    MyMusicTrack mt = new MyMusicTrack()
                    {
                        MusicCategory = category,
                        TransitionCategory = transition
                    };

                    MyAudio.Static.PlayMusic(mt);
                }
                else
                {
                    //random
                    MyMusicTrack mt = new MyMusicTrack()
                    {
                    };
                    MyAudio.Static.PlayMusic(mt, 1);
                }
            }

        }
    }
}
