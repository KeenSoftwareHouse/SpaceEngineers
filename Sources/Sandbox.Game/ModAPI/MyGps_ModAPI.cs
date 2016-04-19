using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    public partial class MyGps : IMyGps
    {
        string IMyGps.Name
        {
            get { return Name; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Value must not be null!");
                Name = value;
            }
        }

        string IMyGps.Description
        {
            get { return Description; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Value must not be null!");
                Description = value;
            }
        }

        Vector3D IMyGps.Coords
        {
            get { return Coords; }
            set { Coords = value; }
        }

        bool IMyGps.ShowOnHud
        {
            get { return ShowOnHud; }
            set { ShowOnHud = value; }
        }

        TimeSpan? IMyGps.DiscardAt
        {
            get { return DiscardAt; }
            set { DiscardAt = value; }
        }
    }
}
