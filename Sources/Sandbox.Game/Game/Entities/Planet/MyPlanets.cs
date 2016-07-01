using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sandbox.Game.World;
using VRage.Game.Components;

namespace Sandbox.Game.Entities.Planet
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, 500)]
    public class MyPlanets : MySessionComponentBase
    {
        private readonly List<MyPlanet> m_planets = new List<MyPlanet>();

        private static MyPlanets Get()
        {
            return MySession.Static.GetComponent<MyPlanets>();
        }


        public static void Register(MyPlanet myPlanet)
        {
            Get().m_planets.Add(myPlanet);
        }

        public static void UnRegister(MyPlanet myPlanet)
        {
            Get().m_planets.Remove(myPlanet);
        }

        public static List<MyPlanet> GetPlanets()
        {
            var planets = Get();
            return planets != null ? planets.m_planets : null;
        }
    }
}
