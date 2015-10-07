﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public class MyRakNetConnectionException : Exception
    {
        public readonly ConnectionAttemptResultEnum ConnectionResult;

        public MyRakNetConnectionException(string message, ConnectionAttemptResultEnum connectionResult, Exception innerException = null)
            : base(message, innerException)
        {
            ConnectionResult = connectionResult;
        }
    }
}
