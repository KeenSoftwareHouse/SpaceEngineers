using System;

namespace VRage.Data
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ModdableContentFileAttribute : Attribute
    {
        public string FileExtension;

        public ModdableContentFileAttribute(string fileExtension)
        {
            if (fileExtension[0] == '.')
            {
                FileExtension = fileExtension.Substring(1);
            }
            else
            {
                FileExtension = fileExtension;
            }
        }
    }
}
