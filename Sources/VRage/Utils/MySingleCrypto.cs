//  This class is used for single encryption and decryption of byte arrays.
//  It's symmetric and doesn't change number of bytes. Just changed the values.
//  These passwords are stored in source code and never sent through network.
//  These class can't be used to verify if encrypted values were encrypted with some password. It can just decrypt and hope it will be OK.

namespace VRage.Utils
{
    public class MySingleCrypto
    {
        readonly byte[] m_password;

        private MySingleCrypto()
        {
        }

        public MySingleCrypto(byte[] password)
        {
            m_password = (byte[])password.Clone();
        }

        //  Encrypts specified byte array, thus changing values, but not size of the array
        public void Encrypt(byte[] data, int length)
        {
            int passwordPosition = 0;
            for (int i = 0; i < length; i++)
            {
                //  This is the "encoding" part
                data[i] = (byte)(data[i] + m_password[passwordPosition]);

                passwordPosition++;
                passwordPosition = passwordPosition % m_password.Length;
            }
        }

        //  Decrypts specified byte array, thus changing values, but not size of the array
        public void Decrypt(byte[] data, int length)
        {
            int passwordPosition = 0;
            for (int i = 0; i < length; i++)
            {
                //  This is the "decoding" part
                data[i] = (byte)(data[i] - m_password[passwordPosition]);

                passwordPosition++;
                passwordPosition = passwordPosition % m_password.Length;
            }
        }
    }
}
