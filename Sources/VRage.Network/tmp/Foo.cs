using VRage.Network;

namespace Server
{
    [Synchronized]
    public class Foo : Bar
    {
        [StateData]
        public MySyncedString Name;

        public Foo()
            : base()
        {
            Name = new MySyncedString();
            Name.Set("Foo");
        }
    }
}
