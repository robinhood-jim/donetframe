using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Frameset.Core.Security
{
    public class RsaUtils
    {
        private static readonly string BEGINPRIVATEKEY = "-----BEGIN PRIVATE KEY-----";
        private static readonly string ENDPRIVATEKEY = "-----END PRIVATE KEY-----";
        private static readonly string BEGINRSAPUBLICEKEY = "-----BEGIN RSA PUBLIC KEY-----";
        private static readonly string ENDRSAPUBLICKEY = "-----END RSA PUBLIC KEY-----";
        private static readonly string BEGINPUBLICEKEY = "-----BEGIN PUBLIC KEY-----";
        private static readonly string ENDPUBLICKEY = "-----END PUBLIC KEY-----";
        private static readonly string BEGINOPENSSLKEY = "-----BEGIN OPENSSH PRIVATE KEY-----";
        private static readonly string ENDOPENSSLKEY = "-----END OPENSSH PRIVATE KEY-----";
        private static readonly List<string> ignoreInputs = [BEGINOPENSSLKEY, ENDOPENSSLKEY, BEGINPRIVATEKEY, ENDPRIVATEKEY];
        private static readonly string SIGNSIGNATURE = "SHA256";

        public static RSACryptoServiceProvider LoadDefaultPrivateKey()
        {
            string userPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName;
            string privateKeyPath = userPath + Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar + ".ssh" + Path.DirectorySeparatorChar + "id_rsa";

            if (File.Exists(privateKeyPath))
            {
                RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
                bool parseOk = false;
                try
                {
                    using Stream stream = File.OpenRead(privateKeyPath);
                    string content = FormatNoHeadFooter(stream);
                    RSAParameters parameters = PaserFromOpenSSHPrivate(Convert.FromBase64String(content));
                    provider.ImportParameters(parameters);
                    parseOk = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (!parseOk)
                {
                    using RSA rsa = RSA.Create();
                    rsa.ImportFromPem(File.ReadAllText(privateKeyPath));
                    provider.ImportParameters(rsa.ExportParameters(true));
                }
                return provider;
                //provider.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(Convert.FromBase64String(content)), out _);
            }
            return null;
        }
        public static RSACryptoServiceProvider LoadDefaultPublicKey()
        {
            string userPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName;
            string publicKeyPath = userPath + Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar + ".ssh" + Path.DirectorySeparatorChar + "id_rsa.pub";


            if (File.Exists(publicKeyPath))
            {
                RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
                string fileContent = File.ReadAllText(publicKeyPath).Trim();
                string[] parts = fileContent.Split(' ');

                if (parts.Length < 2)
                    throw new FormatException("Invalid OpenSSH public key format.");

                // Identify the algorithm identifier type and the base64 body payload
                string keyType = parts[0];
                string base64Data = parts[1];

                if (keyType != "ssh-rsa")
                    throw new NotSupportedException($"Unsupported public key algorithm: {keyType}");

                // 2. Decode the raw wire format byte payload
                byte[] binaryData = Convert.FromBase64String(base64Data);
                bool parseOk = false;
                try
                {
                    RSAParameters parameters = ParseFromOpenSSHPublic(binaryData);
                    provider.ImportParameters(parameters);
                    parseOk = true;
                }
                catch (Exception ex)
                {

                }

                return provider;
            }
            return null;
        }
        public static RSACryptoServiceProvider LoadPrivateByPem(Stream stream)
        {
            using StreamReader reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            return LoadPrivateByPem(content);
        }
        public static RSACryptoServiceProvider LoadPublicByPem(Stream stream)
        {
            using StreamReader reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            return LoadPublicByPem(content);
        }
        public static RSACryptoServiceProvider LoadPrivateFromPemContent(string base64Content)
        {
            StringBuilder builder = new StringBuilder();
            if (!base64Content.StartsWith(BEGINPRIVATEKEY))
            {
                builder.Append(BEGINPRIVATEKEY).Append(base64Content).Append(ENDPRIVATEKEY);
            }
            else
            {
                builder.Append(base64Content);
            }
            return LoadPrivateByPem(builder.ToString());
        }
        public static RSACryptoServiceProvider LoadPublicFromPemContent(string base64Content)
        {
            StringBuilder builder = new StringBuilder();
            if (!base64Content.StartsWith(BEGINPUBLICEKEY))
            {
                builder.Append(BEGINPUBLICEKEY).Append(base64Content).Append(ENDPUBLICKEY);
            }
            else
            {
                builder.Append(base64Content);
            }
            return LoadPrivateByPem(builder.ToString());
        }
        public static RSACryptoServiceProvider LoadPrivateByPem(string content)
        {
            using RSA rsa = RSA.Create();
            try
            {
                RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
                rsa.ImportFromPem(content);
                provider.ImportParameters(rsa.ExportParameters(true));
                return provider;
            }
            catch (Exception ex)
            {

            }
            return null;
        }
        public static RSACryptoServiceProvider LoadPublicByPem(string content)
        {
            using RSA rsa = RSA.Create();
            try
            {
                RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
                rsa.ImportFromPem(content);
                provider.ImportParameters(rsa.ExportParameters(false));
                return provider;
            }
            catch (Exception ex)
            {

            }
            return null;
        }
        public static byte[] Encrypt(RSACryptoServiceProvider provider, byte[] content)
        {
            Trace.Assert(provider != null, "provider should not be null!");
            return provider.Encrypt(content, true);
        }
        public static byte[] Decrypt(RSACryptoServiceProvider provider, byte[] encryptBytes)
        {
            Trace.Assert(provider != null, "provider should not be null!");
            return provider.Decrypt(encryptBytes, true);
        }
        public static byte[] SignData(RSACryptoServiceProvider provider, byte[] signBytes)
        {

            Trace.Assert(provider != null, "provider should not be null!");
            return provider.SignData(signBytes, SIGNSIGNATURE);
        }
        public static bool VerfiyData(RSACryptoServiceProvider provide, byte[] data, byte[] signBytes)
        {
            return provide.VerifyData(data, CryptoConfig.MapNameToOID(SIGNSIGNATURE), signBytes);
        }
        public static Tuple<RSACryptoServiceProvider, string, string> GenerateKeys()
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            string privateKey = ExtractPrivate(provider.ExportPkcs8PrivateKeyPem());
            string publicKey = ExtractPublic(provider.ExportRSAPublicKeyPem());
            return Tuple.Create(provider, privateKey, publicKey);
        }
        private static string ExtractPrivate(string privateKey)
        {
            using StringReader reader = new(privateKey);
            string lineStr = null;
            StringBuilder builder = new StringBuilder();
            while ((lineStr = reader.ReadLine()) != null)
            {
                if (lineStr.StartsWith(BEGINPRIVATEKEY) || lineStr.StartsWith(ENDPRIVATEKEY))
                {
                    continue;
                }
                builder.Append(lineStr);
            }
            return builder.ToString();
        }
        private static string ExtractPublic(string publicKey)
        {
            using StringReader reader = new(publicKey);
            string lineStr = null;
            StringBuilder builder = new StringBuilder();
            while ((lineStr = reader.ReadLine()) != null)
            {
                if (lineStr.StartsWith(BEGINRSAPUBLICEKEY) || lineStr.StartsWith(ENDRSAPUBLICKEY))
                {
                    continue;
                }
                builder.Append(lineStr.Replace("\n", ""));
            }
            return builder.ToString();
        }

        private static string FormatNoHeadFooter(Stream input)
        {
            using StreamReader reader = new StreamReader(input);
            string lineStr = string.Empty;
            StringBuilder builder = new StringBuilder();
            while ((lineStr = reader.ReadLine()) != null)
            {
                if (ignoreInputs.IndexOf(lineStr) == -1 && !string.Equals(lineStr.Trim(), "\n"))
                {
                    builder.Append(lineStr);
                }
            }
            return builder.ToString();
        }

        private static RSAParameters PaserFromOpenSSHPrivate(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms))
            {
                byte[] magicBytes = reader.ReadBytes(15);
                if (Encoding.ASCII.GetString(magicBytes) != "openssh-key-v1\0")
                    throw new FormatException("Not a valid unencrypted OpenSSH private key file.");

                ReadSshStringBlock(reader); // Cipher name
                ReadSshStringBlock(reader); // KDF name
                ReadSshStringBlock(reader); // KDF details

                int keyCount = ReadInt32BigEndian(reader);
                if (keyCount != 1) throw new NotSupportedException("Only single-key containers are supported.");

                ReadSshStringBlock(reader); // Skip Public Key payload metadata
                byte[] privateKeyBlock = ReadSshStringBlock(reader);

                using (var innerMs = new MemoryStream(privateKeyBlock))
                using (var innerReader = new BinaryReader(innerMs))
                {
                    int check1 = ReadInt32BigEndian(innerReader);
                    int check2 = ReadInt32BigEndian(innerReader);
                    if (check1 != check2)
                        throw new CryptographicException("Key payload verification mismatch (corrupted or encrypted).");

                    string keyType = Encoding.ASCII.GetString(ReadSshStringBlock(innerReader));
                    if (keyType != "ssh-rsa")
                        throw new NotSupportedException($"Unsupported key type: {keyType}");

                    // 1. Read the raw unaligned arrays from OpenSSH
                    byte[] rawModulus = StripLeadingZeroByte(ReadSshStringBlock(innerReader));
                    byte[] rawExponent = StripLeadingZeroByte(ReadSshStringBlock(innerReader));
                    byte[] rawD = StripLeadingZeroByte(ReadSshStringBlock(innerReader));
                    byte[] rawInverseQ = StripLeadingZeroByte(ReadSshStringBlock(innerReader));
                    byte[] rawP = StripLeadingZeroByte(ReadSshStringBlock(innerReader));
                    byte[] rawQ = StripLeadingZeroByte(ReadSshStringBlock(innerReader));

                    // 2. CRITICAL FIX: Define targets exactly matching .NET strict requirements
                    int modulusLength = rawModulus.Length;
                    int primeLength = (modulusLength + 1) / 2; // Half size

                    // 3. Mathematically calculate DP and DQ using aligned byte ordering
                    var pBig = new System.Numerics.BigInteger(AlignBigEndianToLittleEndian(rawP));
                    var qBig = new System.Numerics.BigInteger(AlignBigEndianToLittleEndian(rawQ));
                    var dBig = new System.Numerics.BigInteger(AlignBigEndianToLittleEndian(rawD));

                    var dpBig = dBig % (pBig - 1);
                    var dqBig = dBig % (qBig - 1);

                    byte[] rawDp = StripLeadingZeroByte(ConvertBigIntegerToBigEndian(dpBig));
                    byte[] rawDq = StripLeadingZeroByte(ConvertBigIntegerToBigEndian(dqBig));

                    // 4. Force strict size restrictions by padding or trimming
                    var rsaParams = new RSAParameters
                    {
                        Modulus = rawModulus, // Modulus sets baseline length
                        Exponent = rawExponent,
                        D = ForceLength(rawD, modulusLength), // Must match Modulus length
                        P = ForceLength(rawP, primeLength),   // Must match half size
                        Q = ForceLength(rawQ, primeLength),   // Must match half size
                        DP = ForceLength(rawDp, primeLength),  // Must match half size
                        DQ = ForceLength(rawDq, primeLength),  // Must match half size
                        InverseQ = ForceLength(rawInverseQ, primeLength) // Must match half size
                    };

                    return rsaParams;
                }
            }
        }
        private static RSAParameters ParseFromOpenSSHPublic(byte[] binaryData)
        {
            using (var ms = new MemoryStream(binaryData))
            {
                using (var reader = new BinaryReader(ms))
                {
                    // 3. Verify internal protocol identifier block string matches "ssh-rsa"
                    string innerKeyType = Encoding.ASCII.GetString(ReadSshStringBlock(reader));
                    if (innerKeyType != "ssh-rsa")
                        throw new FormatException("The nested wire format layout is invalid.");

                    // 4. Sequentially extract the Exponent (E) and Modulus (N)
                    byte[] rawExponent = ReadSshStringBlock(reader);
                    byte[] rawModulus = ReadSshStringBlock(reader);

                    // 5. Clean up any OpenSSH padding anomalies 
                    byte[] exponent = StripLeadingZeroByte(rawExponent);
                    byte[] modulus = StripLeadingZeroByte(rawModulus);

                    // 6. Bind elements to native .NET cryptographic parameters container
                    var rsaParams = new RSAParameters
                    {
                        Exponent = exponent,
                        Modulus = modulus
                    };
                    return rsaParams;
                }
            }
        }
        private static byte[] ForceLength(byte[] buffer, int targetLength)
        {
            if (buffer.Length == targetLength) return buffer;

            byte[] aligned = new byte[targetLength];
            if (buffer.Length < targetLength)
            {
                // Pad with leading zeros if it is too small
                Buffer.BlockCopy(buffer, 0, aligned, targetLength - buffer.Length, buffer.Length);
            }
            else
            {
                // Strip excess padding if it is too big
                Buffer.BlockCopy(buffer, buffer.Length - targetLength, aligned, 0, targetLength);
            }
            return aligned;
        }

        private static int ReadInt32BigEndian(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return System.BitConverter.ToInt32(bytes, 0);
        }

        private static byte[] ReadSshStringBlock(BinaryReader reader)
        {
            int length = ReadInt32BigEndian(reader);
            return reader.ReadBytes(length);
        }

        private static byte[] StripLeadingZeroByte(byte[] buffer)
        {
            while (buffer.Length > 1 && buffer[0] == 0x00)
            {
                byte[] trimmed = new byte[buffer.Length - 1];
                Buffer.BlockCopy(buffer, 1, trimmed, 0, trimmed.Length);
                buffer = trimmed;
            }
            return buffer;
        }

        private static byte[] AlignBigEndianToLittleEndian(byte[] bigEndianBytes)
        {
            byte[] copy = new byte[bigEndianBytes.Length + 1];
            Buffer.BlockCopy(bigEndianBytes, 0, copy, 0, bigEndianBytes.Length);
            Array.Reverse(copy);
            return copy;
        }

        private static byte[] ConvertBigIntegerToBigEndian(System.Numerics.BigInteger value)
        {
            byte[] bytes = value.ToByteArray();
            Array.Reverse(bytes);
            return bytes;
        }

    }
}
