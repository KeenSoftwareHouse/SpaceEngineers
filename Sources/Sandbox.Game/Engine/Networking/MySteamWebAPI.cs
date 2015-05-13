using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamSDK;

namespace Sandbox.Engine.Networking
{
    public class MySteamWebAPI
    {
        static public readonly string m_requestFormat = "https://api.steampowered.com/{0}/{1}/v{2:0000}/?format=xml";

        static private Dictionary<uint, Action<bool, string>> m_callbacks = new Dictionary<uint, Action<bool, string>>();

        static public bool GetPublishedFileDetails(IEnumerable<ulong> publishedFileIds, Action<bool, string> callback)
        {
            uint handle = HTTP.CreateHTTPRequest(HTTPMethod.POST, string.Format(m_requestFormat, "ISteamRemoteStorage", "GetPublishedFileDetails", 1));

            if (!HTTP.SetHTTPRequestGetOrPostParameter(handle, "itemcount", publishedFileIds.Count().ToString()))
            {
                MySandboxGame.Log.WriteLine(string.Format("HTTP: failed to set parameter '{0}' = '{1}'", "itemcount", publishedFileIds.Count().ToString()));
                return false;
            }

            int i = 0;
            foreach(var id in publishedFileIds)
            {
                if (!HTTP.SetHTTPRequestGetOrPostParameter(handle, string.Format("publishedfileids[{0}]", i), id.ToString()))
                {
                    MySandboxGame.Log.WriteLine(string.Format("HTTP: failed to set parameter '{0}' = '{1}'", string.Format("publishedfileids[{0}]", i), id.ToString()));
                    return false;
                }
                ++i;
            }

            if (!HTTP.SendHTTPRequest(handle, onRequestCompleted))
            {
                MySandboxGame.Log.WriteLine("HTTP: failed to send request");
                return false;
            }

            m_callbacks[handle] = callback;

            return true;
        }

        internal static void onRequestCompleted(bool ioFailure, HTTPRequestCompleted data)
        {
            uint handle = data.Request;
            uint dataSize;
            byte[] bodyData;
            string bodyString = null;
            bool success = false;

            if (data.StatusCode == HTTPStatusCode.OK)
            {
                if (HTTP.GetHTTPResponseBodySize(handle, out dataSize))
                {
                    bodyData = new byte[dataSize];
                    if (HTTP.GetHTTPResponseBodyData(handle, bodyData, dataSize))
                    {
                        bodyString = Encoding.UTF8.GetString(bodyData);
                        success = true;
                        MySandboxGame.Log.WriteLine(string.Format("HTTP: Downloaded {0} bytes", dataSize));
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLine(string.Format("HTTP: failed to read response body data, size = {0}", dataSize));
                    }
                }
                else
                {
                    MySandboxGame.Log.WriteLine("HTTP: failed to read response body size");
                }
            }
            else
            {
                MySandboxGame.Log.WriteLine(string.Format("HTTP: error {0}", data.StatusCode));
            }

            m_callbacks[handle](success, bodyString);
            m_callbacks.Remove(handle);
            HTTP.ReleaseHTTPRequest(handle);
        }
    }
}
