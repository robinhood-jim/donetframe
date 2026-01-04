using Frameset.Core.Exceptions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Frameset.Core.Utils
{
    public static class CommandExecutor
    {
        public static string ExcuteCommand(string[] cmdArgs)
        {
            StringBuilder builder = new StringBuilder();
            var psi = new ProcessStartInfo(cmdArgs[0], cmdArgs.Skip(1)) { RedirectStandardOutput = true };
            var proc = Process.Start(psi);
            if (proc == null)
            {
                throw new OperationFailedException("process execute error!");
            }
            using (var reader = proc.StandardOutput)
            {
                while (!reader.EndOfStream)
                {
                    builder.Append(reader.ReadLine());
                }
                if (!proc.HasExited)
                {
                    proc.Kill();
                }

            }
            return builder.ToString();
        }
        public static string ExecuteCommandReturnAfterRow(string[] cmdArgs, int afterRows)
        {
            StringBuilder builder = new StringBuilder();
            var psi = new ProcessStartInfo(cmdArgs[0], cmdArgs.Skip(1)) { RedirectStandardOutput = true };
            var proc = Process.Start(psi);
            if (proc == null)
            {
                throw new OperationFailedException("process execute error!");
            }
            Tuple<bool, string> tuple = ReadOutput(proc, afterRows);
            if (tuple.Item1)
            {
                return tuple.Item2;
            }
            else
            {
                throw new OperationFailedException("Execute failed");
            }
        }
        public static string ExecuteCommandMeetSpecifyKey(string[] cmdArgs, string specifyKey)
        {
            StringBuilder builder = new StringBuilder();
            var psi = new ProcessStartInfo(cmdArgs[0], cmdArgs.Skip(1)) { RedirectStandardOutput = true };
            var proc = Process.Start(psi);
            if (proc == null)
            {
                throw new OperationFailedException("process execute error!");
            }
            Tuple<bool, string> tuple = ReadOutputMeetSpecifyKey(proc, specifyKey);
            if (tuple.Item1)
            {
                return tuple.Item2;
            }
            else
            {
                throw new OperationFailedException("Execute failed");
            }
        }
        private static Tuple<bool, string> ReadOutputMeetSpecifyKey(Process process, string specifyKey)
        {
            StringBuilder builder = new StringBuilder();
            bool executeOk = false;
            using (var reader = process.StandardOutput)
            {
                while (!reader.EndOfStream)
                {
                    string readLine = reader.ReadLine();
                    if (readLine.Contains(specifyKey))
                    {
                        if (builder.Length > 0)
                        {
                            builder.Append("\n");
                        }
                        int pos = readLine.IndexOf(specifyKey);
                        builder.Append(readLine.Substring(pos + specifyKey.Length, builder.Length - pos - specifyKey.Length));
                    }
                }
            }
            if (builder.Length > 0)
            {
                return Tuple.Create(true, builder.ToString());
            }
            else
            {
                return Tuple.Create(false, string.Empty);
            }
        }
        private static Tuple<bool, string> ReadOutput(Process process, int skipRows)
        {
            StringBuilder builder = new StringBuilder();
            int pos = 0;
            bool executeOk = false;
            using (var reader = process.StandardOutput)
            {
                while (!reader.EndOfStream)
                {
                    string readLine = reader.ReadLine();
                    if (!string.IsNullOrWhiteSpace(readLine))
                    {
                        pos++;
                        if (pos > skipRows)
                        {
                            if (builder.Length > 0)
                            {
                                builder.Append("\n");
                            }
                            builder.Append(readLine);
                        }
                    }
                }

                if (!process.HasExited)
                {
                    process.Kill();
                }

            }
            if (builder.Length > 0)
            {
                return Tuple.Create(true, builder.ToString());
            }
            else
            {
                return Tuple.Create(false, string.Empty);
            }
        }
    }
}
