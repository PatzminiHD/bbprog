using System;
using System.Diagnostics;
using System.Security.Authentication;
using static System.Console;

namespace bbprog
{
    static class Program
    {
     
        private static readonly string rsyncArgs = "-azrt --info=progress2";
        private const string Version = "2.0.0";

        private const string RsyncIdentifierLine = "[rsync]";
        private const string BorgIdentifierLine = "[borg]";
        
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Exit("Not enough arguments specified");
            }

            if (args[0] == "--help" || args[0] == "-h")
            {
                ShowHelpAndExit();
            }
            var backupEntries = ReadFile(args[0]);
            
            WriteLine($"Found {backupEntries.rsyncBackupList.Count} entries for rsync backup");
            WriteLine($"Found {backupEntries.borgBackupList.Count} entries for borg backup");

            Stopwatch sw_all = new();
            Stopwatch sw_rsync = new();
            Stopwatch sw_borg = new();
            sw_all.Start();
            sw_rsync.Start();
            RsyncInterface.RunBackup(backupEntries.rsyncBackupList, rsyncArgs);
            sw_rsync.Stop();
            WriteLine($"Rsync backups finished!");
            sw_borg.Start();
            BorgInterface.RunBackup(backupEntries.borgBackupList);
            WriteLine("Borg backups finished!");
            WriteLine($"\nRsync backups took {sw_rsync.Elapsed}");
            WriteLine($"Borg backups took  {sw_borg.Elapsed}");
            WriteLine($"All backups took   {sw_all.Elapsed}");
        }

        private static void ShowHelpAndExit()
        {
            WriteLine($"bbrog version {Version} by PatzminiHD\n");
            WriteLine("Usage: bbprog [options] <file>");
            WriteLine("Options:");
            WriteLine("-h, --help  Show this help page and exit\n");
            WriteLine("Under normal circumstances bbprog is only run using 'bbprog /path/to/config/file'");
            WriteLine("An example config file can be found on https://github.com/PatzminiHD/bbprog/blob/master/bbprog/ExampleConfig.txt");
            Environment.Exit(0);
        }
        private static (List<RsyncBackupEntry> rsyncBackupList, List<BorgBackupEntry> borgBackupList) ReadFile(string filePath)
        {
            List<RsyncBackupEntry>? rsyncBackupList = new();
            List<BorgBackupEntry>? borgBackupList = new();
            string identifier = "";
            
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
                if (identifier == null)
                    continue;
                
                switch(fileLines[i].Trim())
                {
                    case RsyncIdentifierLine:
                        identifier = RsyncIdentifierLine;
                        continue;
                    case BorgIdentifierLine:
                        identifier = BorgIdentifierLine;
                        continue;
                }
                
                if(!fileLines[i].StartsWith('"'))
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

                if (identifier == RsyncIdentifierLine)
                {
                    if(split.Count != 4 && split.Count != 3)
                        Exit($"File line {i + 1} does not have the right number of arguments");

                    name = split[0];
                    source = split[1];
                    destination = split[2];
                
                    if(split.Count == 4)
                        if (split[3].ToLower() == "delete")
                            delete = true;
                
                    rsyncBackupList.Add(new RsyncBackupEntry(name, source, destination, delete));
                }
                else if (identifier == BorgIdentifierLine)
                {
                    string compression, encryption, pruning;
                    //Name
                    //Destination (Repo path)
                    //Source
                    //Compression
                    //Encryption
                    //Pruning
                    if(split.Count != 6)
                        Exit($"File line {i + 1} does not have the right number of arguments");

                    name = split[0];
                    source = split[1];
                    destination = split[2];
                    compression = split[3];
                    encryption = split[4];
                    pruning = split[5];

                    borgBackupList.Add(new BorgBackupEntry(name, source, destination, compression, encryption, pruning));
                }
                
            }
            return (rsyncBackupList, borgBackupList);
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