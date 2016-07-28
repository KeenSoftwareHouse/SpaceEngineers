using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;
using System;
using System.Text;
using System.Threading;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    public partial class MyGps
    {
        internal static readonly int DROP_NONFINAL_AFTER_SEC = 180;

        //GPS entry may be confirmed or uncorfirmed. Uncorfirmed has valid DiscardAt.
        public string Name;
        public string Description;
        public Vector3D Coords;
        public bool ShowOnHud;
        public bool AlwaysVisible;
        public TimeSpan? DiscardAt;//final=null. Not final=time at which we should drop it from the list, relative to ElapsedPlayTime
        public int Hash
        {
            get;
            private set;
        }
        public void UpdateHash()
        {
            var newHash = MyUtils.GetHash(Name);
            newHash = MyUtils.GetHash(Coords.X, newHash);
            newHash = MyUtils.GetHash(Coords.Y, newHash);
            newHash = MyUtils.GetHash(Coords.Z, newHash);
            Hash = newHash;
        }
        public override int GetHashCode()
        {
            return Hash;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("GPS:", 256);
            sb.Append(Name); sb.Append(":");
            sb.Append(Coords.X.ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
            sb.Append(Coords.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
            sb.Append(Coords.Z.ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
            return sb.ToString();
        }

        public void ToClipboard()
        {
#if !XB1
            Thread thread = new Thread(() => System.Windows.Forms.Clipboard.SetText(this.ToString()));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
#else
            System.Diagnostics.Debug.Assert(false, "Not Clipboard support on XB1!");
#endif
        }

        public MyGps(MyObjectBuilder_Gps.Entry builder)
        {
            Name = builder.name;
            Description = builder.description;
            Coords = builder.coords;
            ShowOnHud = builder.showOnHud;
            AlwaysVisible = builder.alwaysVisible;
            if (!builder.isFinal)
                SetDiscardAt();
            UpdateHash();
        }
        public MyGps()
        {
            SetDiscardAt();
        }

        public void SetDiscardAt()
        {
            DiscardAt = TimeSpan.FromSeconds(MySession.Static.ElapsedPlayTime.TotalSeconds + DROP_NONFINAL_AFTER_SEC);
        }

    }
}
