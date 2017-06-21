using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;
using System;
using System.Text;
using System.Threading;
using VRage.Game;
using VRage.ModAPI;
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
        public Color GPSColor;
        public bool ShowOnHud;
        public bool AlwaysVisible;
        public TimeSpan? DiscardAt;//final=null. Not final=time at which we should drop it from the list, relative to ElapsedPlayTime
        public bool IsLocal = false;
        private IMyEntity m_entity = null;
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
            if (builder.color != Color.Transparent && builder.color != Color.Black)
                GPSColor = builder.color;
            else
                GPSColor = new Color(117, 201, 241);
            if (!builder.isFinal)
                SetDiscardAt();
            UpdateHash();
        }
        public MyGps()
        {
            GPSColor = new Color(117, 201, 241);
            SetDiscardAt();
        }

        public void SetDiscardAt()
        {
            DiscardAt = TimeSpan.FromSeconds(MySession.Static.ElapsedPlayTime.TotalSeconds + DROP_NONFINAL_AFTER_SEC);
        }

        public void SetEntity(IMyEntity entity)
        {
            if (entity == null)
                return;
            m_entity = entity;
            m_entity.PositionComp.OnPositionChanged += PositionComp_OnPositionChanged;
            m_entity.OnClose += m_entity_OnClose;
            Coords = m_entity.PositionComp.GetPosition();
        }

        void m_entity_OnClose(IMyEntity obj)
        {
            m_entity.PositionComp.OnPositionChanged -= PositionComp_OnPositionChanged;
            m_entity.OnClose -= m_entity_OnClose;
        }

        void PositionComp_OnPositionChanged(VRage.Game.Components.MyPositionComponentBase obj)
        {
            if (m_entity != null)
                Coords = m_entity.PositionComp.GetPosition();
        }

        public void Close()
        {
            if (m_entity != null)
            {
                m_entity.PositionComp.OnPositionChanged -= PositionComp_OnPositionChanged;
                m_entity.OnClose -= m_entity_OnClose;
            }
        }
    }
}
