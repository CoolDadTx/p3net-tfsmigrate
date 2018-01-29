/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fsmb.Apollo;
using TfsMigrate.Diagnostics;
using TfsMigrate.Processors;
using TfsMigrate.Processors.VersionControl;
using TfsMigrate.Settings;

namespace TfsMigrate.Hosting
{
    public class ConsoleProcessorRunner
    {       
        public async Task RunProcessorAsync ( string processorName, ISettingsManager settingsManager, HostSettings hostSettings )
        {
            try
            {                
                var host = new ProcessorHost() {
                    Logger = Logger.Default,
                    SettingsManager = settingsManager,
                    Settings = hostSettings,
                };

                _cancel = new CancellationTokenSource();

                Console.CancelKeyPress += OnCancel;

                var processor = LoadProcessor(processorName);
                await processor.InitializeAsync(host, _cancel.Token).ConfigureAwait(false);

                var totalTime = new Stopwatch();
                totalTime.Start();

                Logger.StartActivity($"Running {processorName}");
                using (var logger = Logger.BeginScope(processorName))
                {
                    await processor.RunAsync(_cancel.Token).ConfigureAwait(false);
                };
                totalTime.Stop();

                Logger.StopActivity($"{processorName} completed - Elapsed Time = {totalTime.Elapsed}");
            } catch (OperationCanceledException)
            {
                Logger.Warning("Processor cancelled");
            } catch (Exception e)
            {
                Logger.Error(e);
                throw;
            } finally
            {
                Console.CancelKeyPress -= OnCancel;
            };
        }        

        #region Private Members        

        private void OnCancel ( object sender, EventArgs e )
        {
            _cancel.Cancel();
        }        

        private IEnumerable<Assembly> FindAssemblies ( string assemblyPattern )
        {
            var files = from f in Directory.EnumerateFiles(Environment.CurrentDirectory, assemblyPattern)
                        where String.Compare(Path.GetExtension(f), ".dll", true) == 0
                        select f;

            foreach (var file in files)
            {
                Assembly asm = null;
                try
                {
                    asm = Assembly.Load(Path.GetFileNameWithoutExtension(file));                    
                } catch
                { /* Ignore */ };

                if (asm != null)
                    yield return asm;
            };
        }

        private IProcessor LoadProcessor ( string processorName )
        {
            var endsWithProcessor = processorName.EndsWith("Processor", StringComparison.OrdinalIgnoreCase);
            var simpleName = endsWithProcessor ? processorName.LeftOf("Processor") : processorName;
            var fullName = endsWithProcessor ? processorName : (processorName + "Processor");

            //Get all the assemblies
            var assemblies = FindAssemblies("TfsMigrate.Processors.*");
            foreach (var assembly in assemblies)
            {
                var processorType = assembly.GetExportedTypes().FirstOrDefault(t =>
                                        !t.IsAbstract && t.GetInterface(typeof(IProcessor).Name) != null
                                            && (String.Compare(t.Name, simpleName, true) == 0 || (String.Compare(t.Name, fullName, true) == 0))                                               
                                    );
                if (processorType != null)
                    return Activator.CreateInstance(processorType) as IProcessor;
            };

            throw new Exception($"No processor found called '{processorName}'");
        }

        private CancellationTokenSource _cancel;
        #endregion
    }
}
