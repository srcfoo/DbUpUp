﻿using System;
using System.IO;
using System.Data.SqlClient;
using NDesk.Options;
using DbUp.Engine;

namespace DbUp.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = "";
            var database = "";
            var directory = "";
            var username = "";
            var password = "";
            var connectionString = "";
            var workingDir = "";
            var dryrun = false;
            var printAll = false;
            var headVersion = "";

            bool show_help = false;

            var optionSet = new OptionSet() {
                { "s|server=", "the SQL Server host", s => server = s },
                { "db|database=", "database to upgrade", d => database = d},
                { "d|directory=", "directory containing SQL Update files", dir => directory = dir },
                { "u|user=", "Database username", u => username = u},
                { "p|password=", "Database password", p => password = p},
                { "cs|connectionString=", "Full connection string", cs => connectionString = cs},
                { "w|workingDir=", "Working directory for existing Git repo", w => workingDir = @w },
                { "h|help",  "show this message and exit", v => show_help = v != null },
                { "dr|dryrun",  "Return a list of files that would have been executed", dr => dryrun = dr != null },
                { "pa|printAll", "Print the contents of the files returned in the dry run", pa => printAll = pa != null },
            };

            optionSet.Parse(args);

            if (args.Length == 0)
                show_help = true;

            if (show_help)
            {
                optionSet.WriteOptionDescriptions(System.Console.Out);
                return;

            }

            Directory.SetCurrentDirectory(workingDir);

            if (String.IsNullOrEmpty(connectionString))
            {
                connectionString = BuildConnectionString(server, database, username, password);
            }

            // Get the version hash of the database and repo@HEAD
            DatabaseVersion databaseVersion = new DatabaseVersion(connectionString);
            Git aGit = new Git(workingDir);
            aGit.UpdateLocalRepo();
            headVersion = aGit.HeadVersion();

            LogRunDetails(databaseVersion.Version, headVersion, connectionString, workingDir);

            var dbup = DeployChanges.To
                .SqlDatabase(connectionString)
                .LogToConsole()
                .WithScriptsFromFileSystem(directory)
                .Build();

            if (dryrun)
            {
                dbup.DryRun(databaseVersion.Version, headVersion, workingDir, printAll, connectionString);
                return;
            }

            if (dbup.IsUpgradeRequired(databaseVersion.Version, workingDir))
            {
                if (dbup.PerformUpgrade(databaseVersion.Version, headVersion, workingDir, connectionString).Successful)
                {
                    // Set the new database version
                    databaseVersion.Version = headVersion;
                }
            }
        }

        private static string BuildConnectionString(string server, string database, string username, string password)
        {
            var conn = new SqlConnectionStringBuilder();
            conn.DataSource = server;
            conn.InitialCatalog = database;
            if (!String.IsNullOrEmpty(username))
            {
                conn.UserID = username;
                conn.Password = password;
                conn.IntegratedSecurity = false;
            }
            else
            {
                conn.IntegratedSecurity = true;
            }

            return conn.ToString();
        }

        private static void LogRunDetails(string databaseVersion, string headVersion, string connectionString, string workingDir)
        {
            System.Console.WriteLine("--------------------------------------------------------------------------------");
            System.Console.WriteLine("Connection: " + connectionString);
            System.Console.WriteLine("Working Directory: " + workingDir);
            System.Console.WriteLine("DB Version: " + databaseVersion);
            System.Console.WriteLine("HEAD Version: " + headVersion);
            System.Console.WriteLine("--------------------------------------------------------------------------------");
        }
    }
}