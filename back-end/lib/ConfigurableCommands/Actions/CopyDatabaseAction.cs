using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.Logging;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    /// <summary>
    /// Copies a database (backup/restore) to another server.
    /// Typically used to copy production database to development
    /// </summary>
    public class CopyDatabaseAction : AbstractAction
    {
        [ConfigurableProperty(Seq = 1, Name = "SOURCE_CONNECTION_STRING", Mandatory = true, Help = @"Data Source=BVBISQLPRD;Integrated Security=True;", Default = "Data Source=BVBISQLPRD;Integrated Security=True;", Description = "The source connection string.")]
        public string SourceConnectionString { get; set; }

        [ConfigurableProperty(Seq = 2, Name = "DESTINATION_CONNECTION_STRING", Mandatory = true, Help = @"Data Source=BVBISQLDEV;Integrated Security=True;", Default = "Data Source=BVBISQLDEV;Integrated Security=True;", Description = "The destination connection string.")]
        public string DestinationConnectionString { get; set; }

        [ConfigurableProperty(Seq = 3, Name = "DATABASE_NAME", Mandatory = true, Help = @"TEST", Description = "The database to copy.")]
        public string DatabaseName { get; set; }

        [ConfigurableProperty(Seq = 4, Name = "SOURCE_BACKUP_DIRECTORY", Mandatory = true, Help = @"\\BVBISQLPRD\R$\BACKUP", Default = @"\\BVBISQLPRD\R$\BACKUP", Description = "The source backup directory (UNC format).")]
        public string SourceBackupDirectory { get; set; }

        [ConfigurableProperty(Seq = 5, Name = "DESTINATION_RESTORE_DIRECTORY", Mandatory = true, Help = @"\\BVBISQLDEV\R$\BACKUP", Default = @"\\BVBISQLDEV\R$\BACKUP", Description = "The destination restore directory (UNC format).")]
        public string DestinationRestoreDirectory { get; set; }

        [ConfigurableProperty(Seq = 6, Name = "RETENTION_DAYS", Mandatory = true, Help = @"1", Default = @"1", Description = "The number of days to retain backup files on disk.")]
        public int RetentionDays { get; set; }

        [ConfigurableProperty(Seq = 7, Name = "DESTINATION_POST_RESTORE_SQL", Mandatory = true, Help = @"", Default = "", Description = "Optional SQL to run on destination database after restore.")]
        public string DestinationPostRestoreSQL { get; set; }

        public override void Execute()
        {
            Logger.Log(LogType.INFORMATION, "Start backup action.");
            var date = DateTime.Now.ToString("yyyyMMddHHmmss");
            var backupPath = $"{SourceBackupDirectory}\\{DatabaseName}_{date}.bak";
            var backupSearch = $"{DatabaseName}_*.bak";
            var restorePath = $"{DestinationRestoreDirectory}\\{DatabaseName}_{date}.bak";
            var restoreSearch = $"{DatabaseName}_*.bak";

            Logger.Log(LogType.INFORMATION, $"Source connection string: {SourceConnectionString}.");
            Logger.Log(LogType.INFORMATION, $"Database Name: : {DatabaseName}.");
            Logger.Log(LogType.INFORMATION, $"Backing up database...");
            BackupDatabase(backupPath);

            if (!backupPath.Equals(restorePath, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log(LogType.INFORMATION, $"Copying backup file...");
                CopyBackupFile(backupPath, restorePath);
            }

            Logger.Log(LogType.INFORMATION, $"Destination connection string: {DestinationConnectionString}.");
            Logger.Log(LogType.INFORMATION, $"Restoring database...");
            RestoreDatabase(restorePath, true);

            Logger.Log(LogType.INFORMATION, $"Deleting backup/restore files. Retention days: {RetentionDays}.");
            DeleteBackupFiles(backupSearch);
            DeleteRestoreFiles(restoreSearch);
            Logger.Log(LogType.INFORMATION, "End backup action.");
        }

        private void BackupDatabase(string backupUNCPath)
        {
            using (var db = new SqlConnection(SourceConnectionString))
            {
                var sql = $"BACKUP DATABASE [{DatabaseName}] TO DISK='{backupUNCPath}'";
                db.Execute(sql);
            }
        }

        private void DeleteBackupFiles(string backupSearch)
        {
            var files = Directory.GetFiles(SourceBackupDirectory, backupSearch);
            foreach (var file in files)
            {
                if (DateTime.Now.AddDays(RetentionDays * -1) > File.GetCreationTime(file))
                    File.Delete(file);
            }
        }

        private void DeleteRestoreFiles(string restoreSearch)
        {
            var files = Directory.GetFiles(DestinationRestoreDirectory, restoreSearch);
            foreach (var file in files)
            {
                if (DateTime.Now.AddDays(RetentionDays * -1) > File.GetCreationTime(file))
                    File.Delete(file);
            }
        }

        private void CopyBackupFile(string backupPath, string restorePath)
        {
            File.Copy(backupPath, restorePath);
        }

        private void RestoreDatabase(string restorePath, bool replace)
        {
            using (var db = new SqlConnection(DestinationConnectionString))
            {
                var replaceText = replace ? "REPLACE," : "";
                var sql = $"RESTORE DATABASE [{DatabaseName}] FROM DISK='{restorePath}' WITH {replaceText} RECOVERY";
                db.Execute(sql);
            }
        }

        private void PostRestoreSql()
        {
            using (var db = new SqlConnection(DestinationConnectionString))
            {
                db.Execute(this.DestinationPostRestoreSQL);
            }
        }
    }
}
