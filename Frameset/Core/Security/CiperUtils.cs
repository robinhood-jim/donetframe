using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;

namespace Frameset.Core.Security
{
    public static class CiperUtils
    {
        private static string DEFAULT_CIPHER_ALGORITHM = "AES/ECB/PKCS7Padding";
        private static string DEFAULTALGORITHM = "AES";
        public static byte[] AesEncrypt(string input, byte[] key)
        {
            IBlockCipher symmetricBlockCipher = new AesEngine();

            // Next select the mode compatible with the "engine", in this case we use the simple ECB mode
            IBlockCipherMode symmetricBlockMode = new EcbBlockCipher(symmetricBlockCipher);

            // Finally select a compatible padding, PKCS7 which is the default
            IBlockCipherPadding padding = new Pkcs7Padding();

            // apply the mode and engine on the plainTextData
            PaddedBufferedBlockCipher ecbCipher = new PaddedBufferedBlockCipher(symmetricBlockMode, padding);
            ecbCipher.Init(true, new KeyParameter(key));

            //IBufferedCipher cipher = CipherUtilities.GetCipher(DEFAULT_CIPHER_ALGORITHM);
            //cipher.Init(true, new ParametersWithIV(ParameterUtilities.CreateKeyParameter(DEFAULTALGORITHM, key), iv));
            return ecbCipher.DoFinal(Encoding.UTF8.GetBytes(input));
        }
        public static byte[] AesDecrypt(byte[] enryptBytes, byte[] key)
        {
            IBlockCipher symmetricBlockCipher = new AesEngine();
            IBlockCipherMode symmetricBlockMode = new EcbBlockCipher(symmetricBlockCipher);
            IBlockCipherPadding padding = new Pkcs7Padding();

            PaddedBufferedBlockCipher ecbCipher = new PaddedBufferedBlockCipher(symmetricBlockMode, padding);
            ecbCipher.Init(false, new KeyParameter(key));

            return ecbCipher.DoFinal(enryptBytes);
        }

        public static byte[] Encrypt(this SymmetricAlgorithm algorithm, byte[] rawBytes, byte[] key,
            CipherMode cipherMode = CipherMode.CBC, PaddingMode paddingMode = PaddingMode.PKCS7)
        {
            if (key != null && key.Length > 0)
            {
                algorithm.Key = key;
                algorithm.Padding = paddingMode;
                algorithm.Mode = cipherMode;
            }

            using var outputStream = new MemoryStream(); 
            using var stream = new CryptoStream(outputStream, algorithm.CreateEncryptor(),
                CryptoStreamMode.Write);
            stream.Write(rawBytes, 0, rawBytes.Length);
            if (algorithm.Padding == PaddingMode.None)
            {
                var len = rawBytes.Length % 8;
                if (len > 0)
                {
                    var buf = new byte[8 - len];
                    stream.Write(buf, 0, buf.Length);
                }
            }
            stream.FlushFinalBlock();
            return outputStream.ToArray();
        }

        public static byte[] Decrypt(this SymmetricAlgorithm algorithm, byte[] encrypteBytes, byte[] key,CipherMode cipherMode = CipherMode.CBC, PaddingMode paddingMode = PaddingMode.PKCS7)
        {
            if (key != null && key.Length > 0)
            {
                algorithm.Key = key;
                algorithm.Padding = paddingMode;
                algorithm.Mode = cipherMode;
            }

            using var rawStream = new MemoryStream(encrypteBytes);
            using var cryptStream = new CryptoStream(rawStream, algorithm.CreateDecryptor(), CryptoStreamMode.Read);
            using var outStream = new MemoryStream();
            byte[] tempBytes = new byte[200];
            int i ;
            while ((i = cryptStream.Read(tempBytes, 0, tempBytes.Length)) > 0)
            {
                outStream.Write(tempBytes,0,i);
            }
            return outStream.ToArray();
        }
    }
}
