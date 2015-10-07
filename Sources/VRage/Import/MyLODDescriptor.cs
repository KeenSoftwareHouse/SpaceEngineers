using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VRage.Import
{
    public enum MyFacingEnum : byte
    {
        None = 0,
        Vertical = 1,
        Full = 2,
        Impostor = 3
    }

	public class MyLODDescriptor
	{
        public float Distance; //In meters
        public string Model;
        public string RenderQuality;
        public List<int> RenderQualityList;

        public MyLODDescriptor() { }

		public bool Write(BinaryWriter writer)
		{
            writer.Write(Distance);
            writer.Write((Model != null) ? Model : "");
            writer.Write((RenderQuality != null) ? RenderQuality : "");
			return true;
		}

        public bool Read(BinaryReader reader)
        {
            Distance = reader.ReadSingle();
            Model = reader.ReadString();
            if (String.IsNullOrEmpty(Model))
                Model = null;
            RenderQuality = reader.ReadString();
            if (String.IsNullOrEmpty(RenderQuality))
                RenderQuality = null;
            return true;
        }
	}
}
