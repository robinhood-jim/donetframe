using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;

namespace Frameset.Core.Security
{
    public class CiperUtils
    {
        private static string DEFAULT_CIPHER_ALGORITHM = "AES/ECB/PKCS7Padding";
        private static string DEFAULTALGORITHM = "AES";
        public static byte[] Encrypt(string input, byte[] key)
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
        public static byte[] Decrypt(byte[] enryptBytes, byte[] key)
        {
            IBlockCipher symmetricBlockCipher = new AesEngine();
            IBlockCipherMode symmetricBlockMode = new EcbBlockCipher(symmetricBlockCipher);
            IBlockCipherPadding padding = new Pkcs7Padding();

            PaddedBufferedBlockCipher ecbCipher = new PaddedBufferedBlockCipher(symmetricBlockMode, padding);
            ecbCipher.Init(false, new KeyParameter(key));

            return ecbCipher.DoFinal(enryptBytes);
        }

    }
}
