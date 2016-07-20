using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GpsCollectionDefinition))]
    public class MyGpsCollectionDefinition : MyDefinitionBase
    {
        public struct MyGpsAction 
        {
            public string BlockName;
            public string ActionId;
        }

        public struct MyGpsCoordinate
        {
            public string Name;
            public Vector3D Coords;
            public List<MyGpsAction> Actions;
        }
        public List<MyGpsCoordinate> Positions;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var gpsBuilder = builder as MyObjectBuilder_GpsCollectionDefinition;

            Positions = new List<MyGpsCoordinate>();
            if (gpsBuilder.Positions != null && gpsBuilder.Positions.Length > 0)
            {
                StringBuilder name = new StringBuilder();
                Vector3D coords = Vector3D.Zero;

                StringBuilder additionalData = new StringBuilder();

                foreach (var gpsString in gpsBuilder.Positions)
                {
                    if (MyGpsCollection.ParseOneGPSExtended(gpsString, name, ref coords, additionalData))
                    {
                        MyGpsCoordinate newGps = new MyGpsCoordinate()
                        {
                            Name = name.ToString(),
                            Coords = coords
                        };

                        var additionalString = additionalData.ToString();
                        if (!string.IsNullOrWhiteSpace(additionalString))
                        {
                            string[] split = additionalString.Split(':');
                            for (int i = 0; i < split.Length / 2; ++i)
                            {
                                string first = split[2 * i + 0];
                                string second = split[2 * i + 1];

                                if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(second))
                                {
                                    if (newGps.Actions == null)
                                        newGps.Actions = new List<MyGpsAction>();
                                    newGps.Actions.Add(new MyGpsAction() { BlockName = first, ActionId = second });
                                }
                            }
                        }

                        Positions.Add(newGps);
                    }
                    else
                    {
                        Debug.Fail("Invalid GPS: " + gpsString);
                    }
                }
            }
        }

    }
}
