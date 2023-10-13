using static System.Console;


namespace bbprog;

public class RsyncInterface
{
    public static void RunBackup(List<RsyncBackupEntry> backupEntries, string rsyncArgs)
    {
        System.Diagnostics.ProcessStartInfo procStartInfo = new();
        procStartInfo.UseShellExecute = false;
        procStartInfo.RedirectStandardOutput = true;
        procStartInfo.FileName = "rsync";
        for (int i = 0; i < backupEntries.Count; i++)
        {
            WriteLine($"Backing up: {backupEntries[i].Name}");
            if(backupEntries[i].Delete)
                procStartInfo.Arguments = $"{rsyncArgs} {backupEntries[i].Source} {backupEntries[i].Destination} --delete";
            else
                procStartInfo.Arguments = $"{rsyncArgs} {backupEntries[i].Source} {backupEntries[i].Destination}";
                
            System.Diagnostics.Process rsync = new();
            rsync.StartInfo = procStartInfo;
            rsync.Start();
                
            StreamPipe pout = new StreamPipe(rsync.StandardOutput.BaseStream, Console.OpenStandardOutput());
            pout.Connect();
            rsync.WaitForExit();
            if (rsync.ExitCode != 0)
            {
                ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR backing up {backupEntries[i].Name}. See previous errors");
                ResetColor();
            }
        }
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