using System.IdentityModel.Metadata;
using VRage.Library.Collections;
using VRageMath;

namespace Sandbox.Game.Replication
{
    public struct MyGridNetState
    {
        public bool Valid;
        public Vector3 Move;
        public Vector2 Rotation;
        public float Roll;

        public MyGridNetState(BitStream stream)
        {
            Rotation = new Vector2
            {
                X = stream.ReadFloat(),
                Y = stream.ReadFloat()
            };

            Roll = stream.ReadHalf();

            Move = new Vector3
            {
                X = stream.ReadHalf(),
                Y = stream.ReadHalf(),
                Z = stream.ReadHalf()
            };

            Valid = true;
        }

        public void Serialize(BitStream stream)
        {
            stream.WriteFloat(Rotation.X);
            stream.WriteFloat(Rotation.Y);

            stream.WriteHalf(Roll);

            stream.WriteHalf(Move.X);
            stream.WriteHalf(Move.Y);
            stream.WriteHalf(Move.Z);
        }
    }
}
