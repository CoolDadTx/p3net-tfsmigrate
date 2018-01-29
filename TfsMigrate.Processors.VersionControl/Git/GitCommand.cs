/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TfsMigrate.Diagnostics;
using TfsMigrate.IO;

namespace TfsMigrate.Processors.VersionControl.Git
{
    public class GitCommand
    {
        #region construction

        public GitCommand ( string commandLine )
        {
            _gitCommand = commandLine;

            if (!File.Exists(_gitCommand))
                throw new FileNotFoundException("Git could not be found.");
        }
        #endregion
               
        public async Task CheckOutBranchAsync ( string localPath, string branch, bool isNew, CancellationToken cancellationToken )
        {
            Logger.Debug($"Checking out branch '{branch}' to '{localPath}'");

            var flag = isNew ? "-b" : "";
            var result = await RunCommandAsync($"checkout {flag} {branch}", localPath, cancellationToken).ConfigureAwait(false);
            ThrowIfCommandFailed(result);           
        }

        public async Task ConfigureAsync ( string localPath, string configurationSetting, bool isGlobal, CancellationToken cancellationToken )
        {
            var scope = isGlobal ? "global" : "local";

            Logger.Debug($"Configuring {scope} setting for '{localPath}' - {configurationSetting}");

            var result = await RunCommandAsync($"config --{scope} {configurationSetting}", localPath, cancellationToken).ConfigureAwait(false);
            ThrowIfCommandFailed(result);
        }

        public async Task<bool> CommitChangesAsync ( string localPath, string message, CancellationToken cancellationToken )
        {
            Logger.Debug($"Committing changes to '{localPath}' with message '{message}'");
            
            var returnCode = await RunCommandAsync($"commit -m \"{message}\"", localPath, cancellationToken).ConfigureAwait(false);
            if (returnCode != 0 && returnCode != 1)
                throw new Exception($"Error committing changes - {returnCode}");

            return returnCode == 0;            
        }

        public async Task CommitAndPushChangesAsync ( string localPath, string branch, string message, CancellationToken cancellationToken )
        {
            Logger.Debug($"Committing and pushing changes for '{localPath}' to branch '{branch}'");
            
            //This is supposed to work for a new or existing repo                
            await StageChangesAsync(localPath, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            //Now commit the changes
            var hasChanges = await CommitChangesAsync(localPath, message, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
                
            //Push the changes, this will create the branch if it doesn't exist yet
            await PushChangesAsync(localPath, branch, cancellationToken).ConfigureAwait(false);            
        }

        public async Task InitializeRepositoryAsync ( string basePath, string repoName, CancellationToken cancellationToken )
        {
            Logger.Debug($"Initializing local repo '{repoName}' at '{basePath}'");
            
            //Make sure the base path exists
            await FileSystem.CreateDirectoryAsync(basePath, false, cancellationToken).ConfigureAwait(false);

            //Initialize the repo
            var result = await RunCommandAsync($"init {repoName}", basePath, cancellationToken).ConfigureAwait(false);
            ThrowIfCommandFailed(result);            
        }

        public async Task PushChangesAsync ( string localPath, string branch, CancellationToken cancellationToken )
        {
            Logger.Debug($"Pushing changes to '{branch}'");
            
            var result = await RunCommandAsync($"push origin {branch}", localPath, cancellationToken).ConfigureAwait(false);
            ThrowIfCommandFailed(result);            
        }

        public async Task SetRepositoryOriginAsync ( string localPath, string remoteUrl, CancellationToken cancellationToken )
        {
            Logger.Debug($"Setting repo at '{localPath}' to remote URL '{remoteUrl}'");
            
            var result = await RunCommandAsync($"remote add origin {remoteUrl}", localPath, cancellationToken).ConfigureAwait(false);
            ThrowIfCommandFailed(result);
        }

        public async Task StageChangesAsync ( string localPath, CancellationToken cancellationToken )
        {
            Logger.Debug($"Staging changes from '{localPath}'");

            var result = await RunCommandAsync("add --all", localPath, cancellationToken).ConfigureAwait(false);
            ThrowIfCommandFailed(result);
        }                

        #region Private Members        

        private void LogMessage ( string message, TraceLevel level )
        {
            if (String.IsNullOrEmpty(message))
                return;

            //Some messages are reported as errors when they aren't or vice versa
            message = message.Trim();

            if ((message.IndexOf("master -> master", StringComparison.OrdinalIgnoreCase) >= 0) ||
                 message.StartsWith("* [New branch]", StringComparison.OrdinalIgnoreCase) ||
                 message.StartsWith("To ", StringComparison.OrdinalIgnoreCase) ||
                 message.StartsWith("Switched to", StringComparison.OrdinalIgnoreCase) ||
                 message.StartsWith("Switched back", StringComparison.OrdinalIgnoreCase))
                return;

            if (message.StartsWith("Warning:", StringComparison.OrdinalIgnoreCase))
                level = TraceLevel.Warning;

            switch (level)
            {
                case TraceLevel.Error: Logger.Error(message); break;
                case TraceLevel.Warning: Logger.Warning(message); break;
                case TraceLevel.Verbose: Logger.Debug(message); break;

                default: Logger.Info(message); break;
            };            
        }

        private Task<int> RunCommandAsync ( string commandLine, string workingDirectory, CancellationToken cancellationToken )
        {
            var startInfo = new ProcessStartInfo() {
                Arguments = commandLine,
                CreateNoWindow = true,
                FileName = _gitCommand,
                WorkingDirectory = workingDirectory,
            };

            return Task.Run(() => RunCommand(startInfo, cancellationToken));
        }

        private int RunCommand ( ProcessStartInfo startInfo, CancellationToken cancellationToken )
        {
            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;

                process.ErrorDataReceived += ( o, e ) => LogMessage(e.Data, TraceLevel.Error);
                process.OutputDataReceived += ( o, e ) => LogMessage(e.Data, TraceLevel.Verbose);

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                cancellationToken.ThrowIfCancellationRequested();
                while (!process.WaitForExit(5000))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        process.Kill();
                        cancellationToken.ThrowIfCancellationRequested();
                    };
                };

                return process.ExitCode;
            };
        }

        private void ThrowIfCommandFailed ( int returnCode, string message = "Error executing command" )
        {
            if (returnCode != 0)
                throw new Exception($"{message} - {returnCode}");
        }

        private string _gitCommand;
        
        #endregion
    }
}
