//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;

//namespace VRage.Network
//{
//    public class MyClientEntry
//    {
//        public readonly RakNetGUID GUID;
//        public readonly int ClientIndex;

//        public EndpointId EndpointId
//        {
//            get { return GUID.ToEndpoint(); }
//        }

//        public MyClientEntry(RakNetGUID guid, int clientIndex)
//        {
//            Debug.Assert(guid != RakNetGUID.UNASSIGNED_RAKNET_GUID);
//            GUID = guid;
//            ClientIndex = clientIndex;
//        }
//    }
//}
