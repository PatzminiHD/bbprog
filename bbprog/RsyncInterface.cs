using System.Diagnostics;
using static System.Console;


namespace bbprog;

public class RsyncInterface
{
    public static void RunBackup(List<RsyncBackupEntry> backupEntries, string rsyncArgs, int maxThreads)
    {
        ProcessStartInfo procStartInfo = new()
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            FileName = "rsync"
        };
        List<Process> rsyncProcesses = new List<Process>();

        for (int i = 0; i < backupEntries.Count; i++)
        {
            if(backupEntries[i].Delete)
                procStartInfo.Arguments = $"{rsyncArgs} {backupEntries[i].Source} {backupEntries[i].Destination} --delete";
            else
                procStartInfo.Arguments = $"{rsyncArgs} {backupEntries[i].Source} {backupEntries[i].Destination}";
            
            Process rsync = new();
            rsync.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = procStartInfo.UseShellExecute,
                RedirectStandardOutput = procStartInfo.RedirectStandardOutput,
                FileName = procStartInfo.FileName,
                Arguments = procStartInfo.Arguments,
            };
            rsyncProcesses.Add(rsync);
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxThreads
        };

        Parallel.ForEach(rsyncProcesses, parallelOptions, (n) =>
        {
            WriteLine($"\nBacking up: {backupEntries[rsyncProcesses.IndexOf(n)].Name}");
            n.Start();
            StreamPipe pout = new StreamPipe(n.StandardOutput.BaseStream, Console.OpenStandardOutput());
            pout.Connect();
            n.WaitForExit();
            if (n.ExitCode != 0)
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"\nERROR backing up {backupEntries[rsyncProcesses.IndexOf(n)].Name}. See previous errors\n");
                ResetColor();
            }
        });
    }
}

public class RsyncBackupEntry
{
    public readonly string Name;
    public readonly string Source;
    public readonly string Destination;
    public readonly bool Delete;

    public RsyncBackupEntry(string name, string source, string destination, bool delete)
    {
        this.Name = name;
        this.Source = source;
        this.Destination = destination;
        this.Delete = delete;
    }
}