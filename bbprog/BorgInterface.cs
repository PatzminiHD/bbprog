using System.Diagnostics;
using static System.Console;

namespace bbprog;

public class BorgInterface
{
    public static void RunBackup(List<BorgBackupEntry> backupEntries)
    {
        System.Diagnostics.ProcessStartInfo procStartInfo = new();
        procStartInfo.UseShellExecute = false;
        procStartInfo.RedirectStandardOutput = true;
        procStartInfo.FileName = "borg";
        System.Diagnostics.Process borg = new();
        StreamPipe pout;

        for (int i = 0; i < backupEntries.Count; i++)
        {
            
            WriteLine($"Backing up: {backupEntries[i].Name}");

            if (!Directory.Exists(backupEntries[i].Destination))
            {
                ForegroundColor = ConsoleColor.Yellow;
                WriteLine($"Repository for backup {backupEntries[i].Name} does not exist, initiating...");
                ResetColor();
                procStartInfo.Arguments = $"init --encryption={backupEntries[i].Encryption} {backupEntries[i].Destination}";
                
                borg = new Process();
                borg.StartInfo = procStartInfo;
                borg.Start();
                pout = new StreamPipe(borg.StandardOutput.BaseStream, Console.OpenStandardOutput());
                pout.Connect();
                borg.WaitForExit();
                if (borg.ExitCode != 0)
                {
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine($"ERROR initiating repository {backupEntries[i].Name}, exiting...");
                    ResetColor();
                    Environment.Exit(1);
                }
                WriteLine($"Initiated Repository!");
            }

            //Creating Backup
            borg = new Process();

            procStartInfo.Arguments = $"create " +
                                      $"--compression {backupEntries[i].Compression} " +
                                      $"--exclude-caches " +
                                      $"--one-file-system " +
                                      $"-v --stats --progress " +
                                      $"\"{backupEntries[i].Destination}::bbprog_'{backupEntries[i].Name}'_{DateTime.Now.ToString("yyMMddHHmmss")}\" " +
                                      $"{backupEntries[i].Source}";
                
            borg.StartInfo = procStartInfo;
            borg.Start();
            pout = new StreamPipe(borg.StandardOutput.BaseStream, Console.OpenStandardOutput());
            pout.Connect();
            borg.WaitForExit();
            if (borg.ExitCode != 0)
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"ERROR backing up {backupEntries[i].Name}. See previous errors");
                ResetColor();
            }
            
            //Pruning old backups
            borg = new Process();

            procStartInfo.Arguments = $"prune -v --list {backupEntries[i].Destination} --glob-archives bbprog_* {backupEntries[i].Pruning}";
            
            borg.StartInfo = procStartInfo;
            borg.Start();
            pout = new StreamPipe(borg.StandardOutput.BaseStream, Console.OpenStandardOutput());
            pout.Connect();
            borg.WaitForExit();
            
            if (borg.ExitCode != 0)
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine("ERROR pruning old backups. See previous errors");
                ResetColor();
            }
        }
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