using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Planet;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRageMath;

namespace Multiplayer
{
    class MySpaceClientState : MyClientState
    {
        public readonly Dictionary<long, HashSet<MyPlanetSectorId>> KnownSectors = new Dictionary<long, HashSet<MyPlanetSectorId>>();

        static MyContextKind GetContextByPage(MyTerminalPageEnum page)
        {
            switch (page)
            {
                case MyTerminalPageEnum.Inventory: return MyContextKind.Inventory;
                case MyTerminalPageEnum.ControlPanel: return MyContextKind.Terminal;
                case MyTerminalPageEnum.Production: return MyContextKind.Production;
                default: return MyContextKind.None;
            }
        }

        protected override void WriteInternal(BitStream stream, MyEntity controlledEntity)
        {
            MyContextKind context = GetContextByPage(MyGuiScreenTerminal.GetCurrentScreen());

            stream.WriteInt32((int)context, 2);
            if (context != MyContextKind.None)
            {
                var entityId = MyGuiScreenTerminal.InteractedEntity != null ? MyGuiScreenTerminal.InteractedEntity.EntityId : 0;
                stream.WriteInt64(entityId);
            }

            WritePlanetSectors(stream);
        }

        protected override void ReadInternal(BitStream stream, MyNetworkClient sender, MyEntity controlledEntity)
        {
            Context = (MyContextKind)stream.ReadInt32(2);
            if (Context != MyContextKind.None)
            {
                long entityId = stream.ReadInt64();
                ContextEntity = MyEntities.GetEntityByIdOrDefault(entityId);
            }
            else
            {
                ContextEntity = null;
            }

            KnownSectors.Clear();
            ReadPlanetSectors(stream);
        }

        private void WritePlanetSectors(BitStream stream)
        {
            stream.WriteInt32(0x42424242);

            var planets = MyPlanets.GetPlanets();

            stream.WriteInt32(planets.Count);

            foreach (var planet in planets)
            {
                stream.WriteInt64(planet.EntityId);

                foreach (var sector in planet.EnvironmentSectors.Values)
                {
                    if (sector.HasPhysics || sector.ServerOwned)
                    {
                        stream.WriteInt64(sector.SectorId.Pack64());
                    }
                }

                // don't know how many in advance so I will use -1 termination instead of count.
                stream.WriteInt64(-1);
            }
        }

        private void ReadPlanetSectors(BitStream stream)
        {
            if (stream.ReadInt32() != 0x42424242)
            {
                throw new BitStreamException("Wrong magic when reading planet sectors from client state.");
            }

            int count = stream.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                long p = stream.ReadInt64();

                long sectorid;

                HashSet<MyPlanetSectorId> sectids = new HashSet<MyPlanetSectorId>();

                KnownSectors.Add(p, sectids);

                while(true)
                {
                    sectorid = stream.ReadInt64();
                    if (sectorid == -1) break;

                    MyPlanetSectorId id;
                    MyPlanetSectorId.Unpack64(sectorid, out id);
                    sectids.Add(id);
                }

            }
        }
    }
}
