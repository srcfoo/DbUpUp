using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace DbUp.Engine
{
    /// <summary>
    /// Git commands to find migrations that need to be executed since the last release
    /// </summary>
    public class Git
    {
        // Location of the git repository on the local machine
        private string workingDir = "";
        // git.exe should be on the user's path
        private string gitExe = "git.exe";

        public Git(string workingDir)
        {
            this.workingDir = workingDir;
        }

        ///<summary>
        /// Runs arbitrary git commands
        /// </summary>
        /// <returns>Git command result as a string</returns>
        public string ExecuteCommand(string command)
        {
            Process git = new Process();
            ProcessStartInfo gitStartInfo = new ProcessStartInfo();
            gitStartInfo.FileName = this.gitExe;
            gitStartInfo.Arguments = command;
            gitStartInfo.UseShellExecute = false;
            gitStartInfo.RedirectStandardOutput = true;
            gitStartInfo.WorkingDirectory = this.workingDir;
            git.StartInfo = gitStartInfo;
            git.Start();
            string gitOutput = git.StandardOutput.ReadToEnd();
            git.WaitForExit();

            return gitOutput;
        }

        /// <summary>
        /// Update the local repository with the lastest upstream changes
        /// </summary>
        public void UpdateLocalRepo()
        {
            CheckoutMaster();
            RemoteUpdate();
            PullChanges();          
        }

        /// <summary>
        /// Make sure the local repository is on the correct branch. Assumes master will be used.
        /// </summary>
        private void CheckoutMaster()
        {
            Console.WriteLine(this.ExecuteCommand("checkout master"));
        }

        /// <summary>
        /// Get the latest updates from the remote
        /// </summary>
        private void RemoteUpdate()
        {
            Console.WriteLine(this.ExecuteCommand("remote update origin"));
        }

        /// <summary>
        /// Pull the remote updates into the local repository (only from origin not upstream)
        /// </summary>
        private void PullChanges()
        {
            Console.WriteLine(this.ExecuteCommand("pull origin"));
        }

        /// <summary>
        /// Fetch the hash for the HEAD of the current branch
        /// </summary>
        /// <returns>Git hash as a string</returns>
        public string HeadVersion()
        {
            return this.ExecuteCommand("rev-parse HEAD");
        }

        /// <summary>
        /// Find files in the local directory that need to be migrated to the target database
        /// </summary>
        /// <param name="databaseVersionHash">The most recent git hash stored in the target database to be updated</param>
        /// <param name="repoVersionHash">The hash in the local repo that we want to compare the database hash to (typically HEAD)</param>
        /// <returns>List of files prefixed with relative paths to the local repo</returns>
        public string[] GetScripts(string databaseVersionHash, string repoVersionHash)
        {
            // Get all files that have been updated since the database was last updated
            // (M)odified, (A)dded, (C)opied, (R)named
            // (Renamed doesn't seem to do anything in testing but is here for consistency and claritys since -CM seem to catch renamed files)
            var gitDiff = string.Format("diff --name-only --diff-filter=MACR {0}..{1}", databaseVersionHash, repoVersionHash);
            return this.ExecuteCommand(gitDiff).Split('\n');
        }
    }
}
