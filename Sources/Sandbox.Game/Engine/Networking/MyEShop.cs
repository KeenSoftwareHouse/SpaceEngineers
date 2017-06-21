using Sandbox.Engine.Utils;
using System;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using Sandbox.Game;
using LitJson;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Net.Http.Headers;

namespace Sandbox.Engine.Networking
{
    public class MyEShop
    {
        #region NLFeedback class
        public class NLFeedback
        {
            public string Email { get; private set; }
            public bool SteamRefusalFlag { get; private set; }

            public NLFeedback(string email, bool steamRefusalFlag)
            {
                Email = email;
                SteamRefusalFlag = steamRefusalFlag;
            }
        }
        #endregion

        #region Fields
        // Necessary because the server may still see the ticket as invalid
        private const int SERVER_REQUEST_DELAY_MILLISEC = 100;
        private const int TOKEN_EXPIRATION_MINUTES = 5;
        private const string JWT_STEAM_TICKET_ATTRIBUTE = "ticketSteam";
        private const string JWT_SYMMETRIC_CIPHER = "N1F5Kn7yqWx3RQa9U29Iu1WpMOE04EKxyd6CHueSVb19Ot1C7us7cEt0D6yPLLAM";
        private const string HTTP_OP_GET = "rest/user/status/";
        private const string HTTP_OP_POST = "rest/user/";

        private static byte[] m_steamAuthTicketBuffer = new byte[1024];
        private static HttpClient m_client = new HttpClient();
        private static Uri m_UriPOST = new Uri(MyPerGameSettings.EShopUrl + HTTP_OP_POST);
        private static Uri m_UriGET = new Uri(MyPerGameSettings.EShopUrl + HTTP_OP_GET);
        #endregion

        #region Properties
        public static bool ShowNewsletterScreenAtStartup
        { get { return MySandboxGame.Config.NewsletterCurrentStatus == MyConfig.NewsletterStatus.NoFeedback;  } }
        #endregion

        static MyEShop()
        {
            if (!MySandboxGame.IsDedicated &&
                (MyFakes.FORCE_UPDATE_NEWSLETTER_STATUS ||
                 MySandboxGame.Config.NewsletterCurrentStatus == MyConfig.NewsletterStatus.NoFeedback ||
                 MySandboxGame.Config.NewsletterCurrentStatus == MyConfig.NewsletterStatus.Unknown ||
                 MySandboxGame.Config.NewsletterCurrentStatus == MyConfig.NewsletterStatus.EmailNotConfirmed))
                CheckServerAndUpdateStatus();
        }

        #region Public Methods
        public async static void SendInfo(string email)
        {
            string ticket = GetAuthenticatedTicket();
            if (string.IsNullOrEmpty(ticket))
                return;

            string tokenTicket = GenerateToken(ticket);
            string json_feedback = JsonMapper.ToJson(new NLFeedback(email, string.IsNullOrEmpty(email)));

            using (HttpRequestMessage request = GetPOSTRequestMessage(tokenTicket, json_feedback))
            {
                try
                {
                    await Task.Delay(SERVER_REQUEST_DELAY_MILLISEC);
                    var result = await m_client.SendAsync(request);
                }
                catch
                {
                    // TODO write in the log
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Checks the server in order to update the Newsletter status
        /// </summary>
        private async static void CheckServerAndUpdateStatus()
        {
            string ticket = GetAuthenticatedTicket();
            if (string.IsNullOrEmpty(ticket))
                return;

            string tokenTicket = GenerateToken(ticket);

            string response = string.Empty;
            using (HttpRequestMessage request = GetGETRequestMessage(tokenTicket))
            {
                try
                {
                    await Task.Delay(SERVER_REQUEST_DELAY_MILLISEC);
                    var result = await m_client.SendAsync(request);
                    response = result.Content.ReadAsStringAsync().Result;
                }
                catch
                {
                    // TODO write in the log
                }
            }

            ReadPlayerStatus(response);
        }

        /// <summary>
        /// Consume Server response about player newsletter status
        /// </summary>
        /// <param name="jsonString"></param>
        private static void ReadPlayerStatus(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return;

            JsonData res;
            try
            {
                JsonData data = JsonMapper.ToObject(jsonString);
                res = data["status"];
            }
            catch // No key
            {
                return;
            }

            if (res != null && res.IsString)
            {
                switch (res.ToString())
                {
                    case "UNKNOWN":
                        MySandboxGame.Config.NewsletterCurrentStatus = Engine.Utils.MyConfig.NewsletterStatus.NoFeedback;
                        break;
                    case "REFUSED":
                        MySandboxGame.Config.NewsletterCurrentStatus = Engine.Utils.MyConfig.NewsletterStatus.NotInterested;
                        break;
                    case "UNCONFIRMED":
                        MySandboxGame.Config.NewsletterCurrentStatus = Engine.Utils.MyConfig.NewsletterStatus.EmailNotConfirmed;
                        break;
                    case "AGREED":
                        MySandboxGame.Config.NewsletterCurrentStatus = Engine.Utils.MyConfig.NewsletterStatus.EmailConfirmed;
                        break;
                    default: 
                        return;
                }

                MySandboxGame.Config.Save();
            }
        }

        private static string GetAuthenticatedTicket()
        {
            try
            {
                uint length, handle;
                if (MySteam.API.GetAuthSessionTicket(out handle, m_steamAuthTicketBuffer, out length))
                    return BitConverter.ToString(m_steamAuthTicketBuffer, 0, (int)length).Replace("-", "").ToLowerInvariant();
            }
            catch // There may be some problems when trying MySteam.API.GetAuthSessionTicket() -> ParticlesEditorSE
            { }
            return null;
        }

        private static string GenerateToken(string steamTicket)
        {
            // symmetricKey, value that only server knows
            var symmetricKey = Convert.FromBase64String(JWT_SYMMETRIC_CIPHER);
            var tokenHandler = new JwtSecurityTokenHandler();

            var now = DateTime.UtcNow;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                // Payload part of JWT (Data)
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JWT_STEAM_TICKET_ATTRIBUTE, steamTicket)
                }),
                Expires = now.AddMinutes(Convert.ToInt32(TOKEN_EXPIRATION_MINUTES)),
                // Header part of JWT (Algorithm and token type), and secret key
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(symmetricKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var stoken = tokenHandler.CreateToken(tokenDescriptor);
            var token = tokenHandler.WriteToken(stoken);

            return token;
        }

        private static HttpRequestMessage GetPOSTRequestMessage(string tokenTicket, string jsonString)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = m_UriPOST,
                Method = HttpMethod.Post
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            request.Headers.Add("Authorization", "Bearer " + tokenTicket);
            request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            return request;
        }

        private static HttpRequestMessage GetGETRequestMessage(string tokenTicket)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = m_UriGET,
                Method = HttpMethod.Get
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            request.Headers.Add("Authorization", "Bearer " + tokenTicket);

            return request;
        }
        #endregion
    }
}
