using System.Diagnostics;
using static System.Console;

namespace bbprog;

public class BorgInterface
{
    public static void RunBackup(List<BorgBackupEntry> backupEntries, int maxThreads)
    {
        ProcessStartInfo procStartInfo = new()
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            FileName = "borg"
        };
        List<Process> borgProcesses = new List<Process>();
        List<ProcessStartInfo> borgProcessStartInfos = new List<ProcessStartInfo>();

        for (int i = 0; i < backupEntries.Count; i++)
        {
            Process borg = new();
            if (!Directory.Exists(backupEntries[i].Destination))
            {
                ForegroundColor = ConsoleColor.Yellow;
                WriteLine($"Repository for backup {backupEntries[i].Name} does not exist, initiating...");
                ResetColor();
                procStartInfo.Arguments = $"init --encryption={backupEntries[i].Encryption} {backupEntries[i].Destination}";
                
                borg = new Process();
                borg.StartInfo = procStartInfo;
                borg.Start();
                StreamPipe pout = new StreamPipe(borg.StandardOutput.BaseStream, Console.OpenStandardOutput());
                pout.Connect();
                borg.WaitForExit();
                if (borg.ExitCode != 0)
                {
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine($"ERROR initiating repository {backupEntries[i].Name}, exiting...");
                    ResetColor();
                    Environment.Exit(1);
                }
                WriteLine($"\nInitiated Repository!");
            }
            
            borg = new Process();
            
            procStartInfo.Arguments = $"create " +
                                      $"--compression {backupEntries[i].Compression} " +
                                      $"--exclude-caches " +
                                      $"--one-file-system " +
                                      $"-v --stats --progress " +
                                      $"\"{backupEntries[i].Destination}::bbprog_'{backupEntries[i].Name}'_{DateTime.Now.ToString("yyMMddHHmmss")}\" " +
                                      $"{backupEntries[i].Source}";
            
            borg.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = procStartInfo.UseShellExecute,
                RedirectStandardOutput = procStartInfo.RedirectStandardOutput,
                FileName = procStartInfo.FileName,
                Arguments = procStartInfo.Arguments,
            };
            borgProcesses.Add(borg);
        }
        
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxThreads
        };

        Parallel.ForEach(borgProcesses, parallelOptions, (n) =>
        {
            //Creating Backup
            WriteLine($"\nBacking up: {backupEntries[borgProcesses.IndexOf(n)].Name}");
            n.Start();
            StreamPipe pout = new StreamPipe(n.StandardOutput.BaseStream, Console.OpenStandardOutput());
            pout.Connect();
            n.WaitForExit();
            if (n.ExitCode != 0)
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"\nERROR backing up {backupEntries[borgProcesses.IndexOf(n)].Name}. See previous errors\n");
                ResetColor();
            }
            
            //Pruning old backups
            Process borgPrune = new Process();

            procStartInfo.Arguments = $"prune -v --list {backupEntries[borgProcesses.IndexOf(n)].Destination} --glob-archives bbprog_* {backupEntries[borgProcesses.IndexOf(n)].Pruning}";
            
            borgPrune.StartInfo = procStartInfo;
            borgPrune.Start();
            pout = new StreamPipe(borgPrune.StandardOutput.BaseStream, Console.OpenStandardOutput());
            pout.Connect();
            borgPrune.WaitForExit();
            
            if (borgPrune.ExitCode != 0)
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"\nERROR pruning old backups for {backupEntries[borgProcesses.IndexOf(n)].Name}. See previous errors\n");
                ResetColor();
            }
        });
    }
}

public class BorgBackupEntry
{
    public readonly string Name;
    public readonly string Source;
    public readonly string Destination;
    public readonly string Compression;
    public readonly string Encryption;
    public readonly string Pruning;

    public BorgBackupEntry(string name, string source, string destination, string compression, string encryption, string pruning)
    {
        this.Name = name;
        this.Source = source;
        this.Destination = destination;
        this.Compression = compression;
        this.Encryption = encryption;
        this.Pruning = pruning;
    }
}