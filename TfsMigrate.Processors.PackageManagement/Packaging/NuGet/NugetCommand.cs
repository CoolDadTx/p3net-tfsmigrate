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

namespace TfsMigrate.Processors.PackageManagement.Packaging.NuGet
{
    public class NuGetCommand
    {
        #region construction

        public NuGetCommand ( ) : this(null)
        {
        }

        public NuGetCommand ( string commandLine )
        {
            _command = commandLine ?? "nuget.exe";

            if (!File.Exists(_command))
                throw new FileNotFoundException("NuGet could not be found.");
        }
        #endregion
               
        public async Task PushPackageAsync ( string packageSource, string packagePath, CancellationToken cancellationToken )
        {
            var result = await RunCommandAsync($"push -NonInteractive -Source \"{packageSource}\" -ApiKey VSTS \"{packagePath}\"", Path.GetDirectoryName(packagePath), cancellationToken).ConfigureAwait(false);
            ThrowIfCommandFailed(result);
        }
        
        #region Private Members        
        
        private void LogMessage ( string message, TraceLevel level )
        {
            if (String.IsNullOrEmpty(message))
                return;

            //Some messages are reported as errors when they aren't or vice versa
            message = message.Trim();
            
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
                FileName = _command,
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

        private string _command;

        #endregion
    }
}
