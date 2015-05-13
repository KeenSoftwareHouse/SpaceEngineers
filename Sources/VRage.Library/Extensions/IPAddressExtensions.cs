using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Net
{
    public static class IPAddressExtensions
    {
        public static uint ToIPv4NetworkOrder(this IPAddress ip)
        {
            // Ignore obsolete flag, we're using this to get IPv4 only
#pragma warning disable 618
            return (uint)IPAddress.HostToNetworkOrder((int)(uint)ip.Address);
#pragma warning restore 618
        }

        public static IPAddress FromIPv4NetworkOrder(uint ip)
        {
            return new IPAddress((uint)IPAddress.NetworkToHostOrder((int)ip));
        }

        public static IPAddress ParseOrAny(string ip)
        {
            IPAddress result;
            if (!IPAddress.TryParse(ip, out result))
            {
                return IPAddress.Any;
            }
            return result;
        }

        /// <summary>
        /// Parses IP Endpoint from string in format x.x.x.x:port
        /// </summary>
        public static bool TryParseEndpoint(string ipAndPort, out IPEndPoint result)
        {
            try
            {
                int port;
                System.Net.IPAddress address;

                string[] ipPort = ipAndPort.Replace(" ", String.Empty).Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (ipPort.Length == 2 && IPAddress.TryParse(ipPort[0], out address) && int.TryParse(ipPort[1], out port))
                {
                    result = new IPEndPoint(address, port);
                    return true;
                }
            }
            catch
            {
            }
            result = default(IPEndPoint);
            return false;
        }
    }
}
