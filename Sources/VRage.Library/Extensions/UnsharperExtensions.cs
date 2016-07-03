using System;
namespace Unsharper
{
	[UnsharperDisableReflectionAttribute]
	public class UnsharperDisableReflectionAttribute : Attribute
	{
		public UnsharperDisableReflectionAttribute()
		{
		}
	}

    [UnsharperDisableReflectionAttribute]
    public class UnsharperExclude : Attribute
    {
        public UnsharperExclude()
        {
        }
    }

    [UnsharperDisableReflectionAttribute]
    public class UnsharperStaticInitializersPriority : Attribute
    {
        public UnsharperStaticInitializersPriority(int i)
        {
        }
    }
}
