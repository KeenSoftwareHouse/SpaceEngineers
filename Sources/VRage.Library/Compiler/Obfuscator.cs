using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Reflection
{
    public static class Obfuscator
    {
        public static readonly bool EnableAttributeCheck = true;
        public const string NoRename = "cw symbol renaming";

        public static bool CheckAttribute(this MemberInfo member)
        {
            if(!EnableAttributeCheck)
                return true;

            var attr = member.GetCustomAttributes(typeof(ObfuscationAttribute), false);
            foreach (var a in attr.OfType<ObfuscationAttribute>())
            {
                if (a.Feature == NoRename && a.Exclude)
                    return true;
            }
            return false;
        }
    }
}
