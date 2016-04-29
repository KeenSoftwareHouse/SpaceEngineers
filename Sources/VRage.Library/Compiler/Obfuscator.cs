using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if BLIT
using System.Diagnostics;
#endif

#if true //!BLITCREMENTAL

namespace System.Reflection
{
#if BLIT
	[Unsharper.UnsharperDisableReflection()]
#endif
	public static class Obfuscator
    {
		public const string NoRename = "cw symbol renaming";
		public static readonly bool EnableAttributeCheck = true;
#if BLIT
		public static bool CheckAttribute(this MemberInfo member)
		{
			Debug.Assert(false);
			return false;
		}
#else

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
#endif
    }
}

#endif
