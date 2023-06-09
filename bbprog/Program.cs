﻿using System;
using System.Diagnostics;
using static System.Console;

namespace bbprog
{
    static class Program
    {
        private static string rsyncArgs = "-azrt --info=progress2";
        private static readonly string Version = "1.1.1";
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
            List<(string name, string source, string destination, bool delete)> backupEntries = ReadFile(args[0]);
            WriteLine($"Found {backupEntries.Count} entries for backup");
            RunRsync(backupEntries);
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
        private static void RunRsync(List<(string name, string source, string destination, bool delete)> backupEntries)
        {
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
                
                System.Diagnostics.Process rsync = new();
                rsync.StartInfo = procStartInfo;
                rsync.Start();
                
                StreamPipe pout = new StreamPipe(rsync.StandardOutput.BaseStream, Console.OpenStandardOutput());
                pout.Connect();
                rsync.WaitForExit();
                if (rsync.ExitCode != 0)
                {
                    ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ERROR backing up {backupEntries[i].name}. See previous errors");
                    ResetColor();
                }
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