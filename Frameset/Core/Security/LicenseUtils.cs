using Frameset.Core.Configuration;
using Frameset.Core.Hardware;
using Frameset.Core.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Frameset.Core.Security
{
    public sealed class LicenseUtils
    {
        protected static byte[] mzHeader = { 0x4D, 0x5A, 0x50, 0x00, 0x02, 0x00, 0x00, 0x00, 0x04, 0x00, 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        protected static byte[] PADDING = { 0X7f };
        protected static byte[] ENDING = { 0x00 };
        public static byte[] ACK = { 0x7E, 0x7F, 0x7E, 0x7F };
        internal static byte[] PERMIT = { 0x7E, 0x7F, 0x01, 0x01 };
        public static byte[] BANNED = { 0x7E, 0x7F, 0xFF, 0xFF };
        internal static byte[] EXPIRE = { 0x7E, 0x7F, 0x7F, 0x7F };
        internal static byte[] PUBLICKEYLIC = { 0x7E, 0x7F, 0x10, 0x10 };
        internal static byte[] PUBLICKEYLICACK = { 0x7E, 0x7F, 0xE0, 0xE0 };
        internal static byte[] LICENSEFILE = { 0x7E, 0x7F, 0x20, 0x20 };
        internal static byte[] LICENSEFILEACK = { 0x7E, 0x7F, 0xD0, 0xD0 };
        public static bool ValidateLicense()
        {
            string userPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName;
            string licensePath = userPath + Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar + ".robin" + Path.DirectorySeparatorChar + ".license";
            if (File.Exists(licensePath))
            {
                byte[] paddingByte = new byte[1];
                using Stream stream = File.OpenRead(licensePath);
                using BinaryReader binaryReader = new BinaryReader(stream);
                RSACryptoServiceProvider publicProvider = GetPublicKey();
                binaryReader.Read(mzHeader);

                int length = ReadToInt32(binaryReader);
                byte[] encryptBytes = new byte[length];
                binaryReader.Read(encryptBytes);
                string key = GetSerialNo();
                byte[] decryptBytes = CiperUtils.Decrypt(encryptBytes, Encoding.UTF8.GetBytes(key));
                string decryptStr = Encoding.UTF8.GetString(decryptBytes);
                string[] arr = decryptStr.Split(';');
                binaryReader.Read(paddingByte);
                length = ReadToInt32(binaryReader);
                byte[] signBytes = new byte[length];
                binaryReader.Read(signBytes);
                if (RsaUtils.VerfiyData(publicProvider, encryptBytes, signBytes))
                {
                    DateTime dateTime = new DateTime(long.Parse(arr[1]));
                    if (DateTime.Now.CompareTo(dateTime) > 0)
                    {
                        return true;
                    }
                    else
                    {
                        Trace.WriteLine("validate time expired!");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        internal static void GenerateTemporaryLicense(RSACryptoServiceProvider privateProvider, Stream fileStream, int days)
        {
            StackTrace trace = new StackTrace();
            StackFrame[] frames = trace.GetFrames();
            //prevent reflect call.only can call within LicenseUtils and ProjectConfiguration
            if (frames.Length > 1)
            {
                StackFrame callFrame = frames[1];
                string className = callFrame.GetFileName();
                MethodBase method = callFrame.GetMethod();
                Type type = method.DeclaringType;

                Trace.Assert(typeof(LicenseUtils).Equals(type) || typeof(ProjectConfiguration).Equals(type), "invalid call from outside class!");
            }

            DateTime dateTime = DateTime.Now;
            DateTime expireTime = dateTime.AddDays(days);

            using BinaryWriter writer = new(fileStream);
            writer.Write(mzHeader);
            string serialNo = MachineUtils.GetSystemTag();
            LogUtils.Debug($"-- current machine Tag {serialNo}");
            byte[] encryptBytes = CiperUtils.Encrypt(new string(serialNo + ";" + new DateTimeOffset(expireTime).ToUnixTimeMilliseconds()), Encoding.ASCII.GetBytes(GetSerialNo()));
            writer.Write(IntToBytes(encryptBytes.Length));
            writer.Write(encryptBytes);
            writer.Write(PADDING);
            byte[] signBytes = RsaUtils.SignData(privateProvider, encryptBytes);
            writer.Write(IntToBytes(signBytes.Length));
            writer.Write(signBytes);
            writer.Write(ENDING);

        }
        private static RSACryptoServiceProvider GetPublicKey()
        {
            RSACryptoServiceProvider publicProvider = RsaUtils.LoadDefaultPublicKey();
            if (publicProvider == null)
            {
                using Stream stream = FileUtil.ReadStreamInPackage("Frameset.Resources.public.pem");
                publicProvider = RsaUtils.LoadPublicByPem(stream);
            }
            return publicProvider;
        }
        private static RSACryptoServiceProvider GetPrivateKey()
        {
            RSACryptoServiceProvider publicProvider = RsaUtils.LoadDefaultPrivateKey();
            if (publicProvider == null)
            {
                using Stream stream = FileUtil.ReadStreamInPackage("Frameset.Resources.private.pem");
                publicProvider = RsaUtils.LoadPrivateByPem(stream);
            }
            return publicProvider;
        }
        private static int ReadToInt32(BinaryReader reader)
        {
            byte[] readBytes = new byte[4];
            reader.Read(readBytes);
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                int shift = (4 - 1 - i) * 8;
                value += (readBytes[i] & 0x000000FF) << shift;
            }
            return value;
        }
        private static byte[] IntToBytes(int value)
        {
            byte[] src = new byte[4];
            src[0] = (byte)((value >> 24) & 0xFF);
            src[1] = (byte)((value >> 16) & 0xFF);
            src[2] = (byte)((value >> 8) & 0xFF);
            src[3] = (byte)(value & 0xFF);
            return src;
        }
        public static void GenerateDefaultLicense(RSACryptoServiceProvider privateProvider, Stream fileStream, int days)
        {
            GenerateTemporaryLicense(privateProvider, fileStream, days);
        }
        private static string GetSerialNo()
        {
            StringBuilder builder = new StringBuilder();
            string machineId = MachineUtils.GetMachineId();
            if (!string.IsNullOrWhiteSpace(machineId))
            {
                builder.Append(machineId);
            }
            string systemSerial = MachineUtils.GetSystemSerial();
            if (!string.IsNullOrWhiteSpace(systemSerial))
            {
                builder.Append(systemSerial);
            }
            string sno = builder.ToString().Replace("-", "");
            if (sno.Length > 32)
            {
                return sno.Substring(0, 32);
            }
            else
            {
                return sno;
            }
        }
    }
}
