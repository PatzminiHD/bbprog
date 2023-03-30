using System;
using System.Diagnostics;
using static System.Console;

namespace bbprog
{
    static class Program
    {
        private static string rsyncArgs = "-azrt --info=progress2";
        public static void Main(string[] args)
        {
            List<(string name, string source, string destination, bool delete)> backupEntries;
            if (args.Length < 1)
            {
                Exit("Not enough arguments specified");
            }
            backupEntries = ReadFile(args[0]);
            foreach (var backupEntry in backupEntries)
            {
                WriteLine($"{backupEntry.name}:{backupEntry.source} {backupEntry.destination}; {backupEntry.delete}");
            }
            WriteLine($"Found {backupEntries.Count} entries for backup");
            RunRsync(backupEntries);
        }
        private static void RunRsync(List<(string name, string source, string destination, bool delete)> backupEntries)
        {
            System.Diagnostics.Process rsync;
            System.Diagnostics.ProcessStartInfo procStartInfo = new();
            procStartInfo.UseShellExecute = false;
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.FileName = "rsync";
            for (int i = 0; i < backupEntries.Count; i++)
            {
                WriteLine($"Backing up: {backupEntries[i].name}");
                if(backupEntries[i].delete)
                    procStartInfo.Arguments = $"{rsyncArgs} {backupEntries[i].source} {backupEntries[i].destination} --delete";
                else
                    procStartInfo.Arguments = $"{rsyncArgs} {backupEntries[i].source} {backupEntries[i].destination}";
                rsync = new();
                rsync.StartInfo = procStartInfo;
                rsync.Start();
                
                StreamPipe pout = new StreamPipe(rsync.StandardOutput.BaseStream, Console.OpenStandardOutput());
                pout.Connect();
                rsync.WaitForExit();
            }
            WriteLine("Backups Finished!");
        }
        private static List<(string name, string source, string destination, bool delete)> ReadFile(string filePath)
        {
            List<(string name, string source, string destination, bool delete)> pathsList = new();
            string[] fileLines = Array.Empty<string>();
            try
            {
                if (!File.Exists(filePath))
                {
                    Exit("Specified file does not exist");
                }

                fileLines = File.ReadAllLines(filePath);
            }
            catch (Exception e)
            {
                Exit("Error reading File:\n" + e);
            }

            for (int i = 0; i < fileLines.Length; i++)
            {
                if (!fileLines[i].StartsWith('"'))
                    continue;
                List<string> split = new();
                string name, source, destination;
                bool delete = false;
                int argBeginn = -1, argLength = 0;
                for (int j = 0; j < fileLines[i].Length; j++)
                {
                    if (argBeginn == -1 && fileLines[i][j] == '"')
                    {
                        argBeginn = j + 1;
                        argLength = 0;
                        continue;
                    }
                    if (argBeginn != -1 && fileLines[i][j] == '"')
                    {
                        split.Add(fileLines[i].Substring(argBeginn, argLength));
                        argBeginn = -1;
                        argLength = 0;
                    }
                    argLength++;
                }
                if(split.Count != 4 && split.Count != 3)
                    Exit($"File line {i + 1} does not have the right number of arguments");

                name = split[0];
                source = split[1];
                destination = split[2];
                
                if(split.Count == 4)
                    if (split[3].ToLower() == "delete")
                        delete = true;
                    
                pathsList.Add((name, source, destination, delete));
            }
            return pathsList;
        }

        private static void Exit(string exitMessage)
        {
            WriteLine(exitMessage);
            Environment.Exit(0);
        }
    }
    class StreamPipe
    {
        private const Int32 BufferSize = 4096;

        public Stream Source { get; protected set; }
        public Stream Destination { get; protected set; }

        private CancellationTokenSource _cancellationToken;
        private Task _worker;

        public StreamPipe(Stream source, Stream destination)
        {
            Source = source;
            Destination = destination;
        }

        public StreamPipe Connect()
        {
            _cancellationToken = new CancellationTokenSource();
            _worker = Task.Run(async () =>
            {
                byte[] buffer = new byte[BufferSize];
                while (true)
                {
                    _cancellationToken.Token.ThrowIfCancellationRequested();
                    var count = await Source.ReadAsync(buffer, 0, BufferSize, _cancellationToken.Token);
                    if (count <= 0)
                        break;
                    await Destination.WriteAsync(buffer, 0, count, _cancellationToken.Token);
                    await Destination.FlushAsync(_cancellationToken.Token);
                }
            }, _cancellationToken.Token);
            return this;
        }

        public void Disconnect()
        {
            _cancellationToken.Cancel();
        }
    }
}