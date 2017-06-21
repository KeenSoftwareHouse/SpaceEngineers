using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using VRage.Cryptography;
using VRage.Serialization;

namespace VRage.Common.Utils
{
    public class MyRSA
	{
#if XB1
#else
        private HashAlgorithm m_hasher;

        public HashAlgorithm HashObject { get { return m_hasher; } }

        public MyRSA()
        {
            m_hasher = MySHA256.Create();
            m_hasher.Initialize();
        }

        public void GenerateKeys(string publicKeyFileName, string privateKeyFileName)
        {
            byte[] publicKey;
            byte[] privateKey;

            GenerateKeys(out publicKey, out privateKey);

            if (publicKey != null && privateKey != null)
            {
                // Export public key only
                File.WriteAllText(publicKeyFileName, Convert.ToBase64String(publicKey));
                // Export private/public key pair
                File.WriteAllText(privateKeyFileName, Convert.ToBase64String(privateKey));
            }
        }

        /// <summary>
        /// Generate keys into specified files.
        /// </summary>
        /// <param name="publicKeyFileName">Name of the file that will contain public key</param>
        /// <param name="privateKeyFileName">Name of the file that will contain private key</param>
        public void GenerateKeys(out byte[] publicKey, out byte[] privateKey)
        {
            // Variables
            CspParameters cspParams = null;
            RSACryptoServiceProvider rsaProvider = null;

            try
            {
                // Create a new key pair on target CSP
                cspParams = new CspParameters()
                {
                    ProviderType = 1,                          // PROV_RSA_FULL
                    Flags = CspProviderFlags.UseArchivableKey, // can be exported
                    KeyNumber = (int)KeyNumber.Exchange        // can be safely stored and exchanged
                };

                rsaProvider = new RSACryptoServiceProvider(cspParams);
                rsaProvider.PersistKeyInCsp = false;

                // Export public key only
                publicKey = rsaProvider.ExportCspBlob(false);
                privateKey = rsaProvider.ExportCspBlob(true);
            }
            catch (Exception ex)
            {
                Debug.Fail(string.Format("Exception occured while generating keys: {0}", ex.Message));
                publicKey = null;
                privateKey = null;
            }
            finally
            {
                if (rsaProvider != null) rsaProvider.PersistKeyInCsp = false;
            }
        }

        /// <summary>
        /// Signs given data with provided key.
        /// </summary>
        /// <param name="data">data to sign (in base64 form)</param>
        /// <param name="privateKey">private key (in base64 form)</param>
        /// <returns>Signed data (string in base64 form)</returns>
        public string SignData(string data, string privateKey)
        {
            byte[] signedBytes;

            using (var RSA = new RSACryptoServiceProvider())
            {
                // do not store key info into persistent storage of CSP
                RSA.PersistKeyInCsp = false;

                var encoder = new UTF8Encoding();
                var original = encoder.GetBytes(data);

                try
                {
                    RSA.ImportCspBlob(Convert.FromBase64String(privateKey));
                    signedBytes = RSA.SignData(original, m_hasher);
                }
                catch (CryptographicException e)
                {
                    Debug.Fail(e.Message);
                    return null;
                }
                finally
                {
                    RSA.PersistKeyInCsp = false;
                }
            }
            return Convert.ToBase64String(signedBytes);
        }

        /// <summary>
        /// Signs given hash with provided key.
        /// </summary>
        /// <param name="hash">hash to sign</param>
        /// <param name="privateKey">private key</param>
        /// <returns>Signed hash (string in base64 form)</returns>
        public string SignHash(byte[] hash, byte[] privateKey)
        {
            byte[] signedBytes;

            using (var RSA = new RSACryptoServiceProvider())
            {
                // do not store key info into persistent storage of CSP
                RSA.PersistKeyInCsp = false;

                try
                {
                    RSA.ImportCspBlob(privateKey);
                    signedBytes = RSA.SignHash(hash, CryptoConfig.MapNameToOID("SHA256"));
                }
                catch (CryptographicException e)
                {
                    Debug.Fail(e.Message);
                    return null;
                }
                finally
                {
                    RSA.PersistKeyInCsp = false;
                }
            }
            return Convert.ToBase64String(signedBytes);
        }

        /// <summary>
        /// Signs given hash with provided key.
        /// </summary>
        /// <param name="hash">hash to sign (in base64 form)</param>
        /// <param name="privateKey">private key (in base64 form)</param>
        /// <returns>Signed hash (string in base64 form)</returns>
        public string SignHash(string hash, string privateKey)
        {
            byte[] signedBytes;

            using (var RSA = new RSACryptoServiceProvider())
            {
                // do not store key info into persistent storage of CSP
                RSA.PersistKeyInCsp = false;

                var encoder = new UTF8Encoding();
                var original = encoder.GetBytes(hash);

                try
                {
                    RSA.ImportCspBlob(Convert.FromBase64String(privateKey));
                    signedBytes = RSA.SignHash(original, CryptoConfig.MapNameToOID("SHA256"));
                }
                catch (CryptographicException e)
                {
                    Debug.Fail(e.Message);
                    return null;
                }
                finally
                {
                    RSA.PersistKeyInCsp = false;
                }
            }
            return Convert.ToBase64String(signedBytes);
        }

        /// <summary>
        /// Verifies that a digital signature is valid by determining the hash value
        /// in the signature using the provided public key and comparing it to the provided hash value.
        /// </summary>
        /// <param name="hash">hash to test</param>
        /// <param name="signedHash">already signed hash</param>
        /// <param name="publicKey">signature</param>
        /// <returns>true if the signature is valid; otherwise, false.</returns>
        public bool VerifyHash(byte[] hash, byte[] signedHash, byte[] publicKey)
        {
            using (var RSA = new RSACryptoServiceProvider())
            {
                try
                {
                    RSA.ImportCspBlob(publicKey);
                    return RSA.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA256"), signedHash);
                }
                catch (CryptographicException e)
                {
                    Debug.Fail(e.Message);
                    return false;
                }
                finally
                {
                    RSA.PersistKeyInCsp = false;
                }
            }
        }

        /// <summary>
        /// Verifies that a digital signature is valid by determining the hash value
        /// in the signature using the provided public key and comparing it to the provided hash value.
        /// </summary>
        /// <param name="hash">hash to test</param>
        /// <param name="signedHash">already signed hash (in base64 form)</param>
        /// <param name="publicKey">signature (in base64 form)</param>
        /// <returns>true if the signature is valid; otherwise, false.</returns>
        public bool VerifyHash(string hash, string signedHash, string publicKey)
        {
            using (var RSA = new RSACryptoServiceProvider())
            {
                var encoder = new UTF8Encoding();
                var toVerify = encoder.GetBytes(hash);
                var signed = Convert.FromBase64String(signedHash);

                try
                {
                    RSA.ImportCspBlob(Convert.FromBase64String(publicKey));
                    return RSA.VerifyHash(toVerify, CryptoConfig.MapNameToOID("SHA256"), signed);
                }
                catch (CryptographicException e)
                {
                    Debug.Fail(e.Message);
                    return false;
                }
                finally
                {
                    RSA.PersistKeyInCsp = false;
                }
            }
        }

        /// <summary>
        /// Verifies that a digital signature is valid by determining the hash value
        /// in the signature using the provided public key and comparing it to the hash value of the provided data.
        /// </summary>
        /// <param name="originalMessage">original data</param>
        /// <param name="signedMessage">signed message (in base64 form)</param>
        /// <param name="publicKey">signature (in base64 form)</param>
        /// <returns>true if the signature is valid; otherwise, false.</returns>
        public bool VerifyData(string originalMessage, string signedMessage, string publicKey)
        {
            using (var RSA = new RSACryptoServiceProvider())
            {
                var encoder = new UTF8Encoding();
                var toVerify = encoder.GetBytes(originalMessage);
                var signed = Convert.FromBase64String(signedMessage);

                try
                {
                    RSA.ImportCspBlob(Convert.FromBase64String(publicKey));
                    return RSA.VerifyData(toVerify, m_hasher, signed);
                }
                catch (CryptographicException e)
                {
                    Debug.Fail(e.Message);
                    return false;
                }
                finally
                {
                    RSA.PersistKeyInCsp = false;
                }
            }
        }
#endif
	}
}
