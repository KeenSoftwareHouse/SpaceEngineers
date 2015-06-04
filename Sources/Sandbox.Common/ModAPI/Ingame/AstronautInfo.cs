using System;

namespace Sandbox.ModAPI.Ingame
{
    public struct AstronautInfo
    {
        public readonly string FactionTag;
        public readonly string Name;

        public AstronautInfo(string name, string factionTag = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null", "name");
            this.Name = name;
            this.FactionTag = string.IsNullOrEmpty(factionTag)? null : factionTag;
        }

        public bool IsEmpty()
        {
            return Name != null;
        }

        public override string ToString()
        {
            if (Name == null)
                return "";
            if (FactionTag != null)
                return FactionTag + "." + Name;
            return Name;
        }
    }
}