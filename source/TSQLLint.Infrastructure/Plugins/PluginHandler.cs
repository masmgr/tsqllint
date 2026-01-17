using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;
using TSQLLint.Common;
using TSQLLint.Core.Interfaces;

namespace TSQLLint.Infrastructure.Plugins
{
    public class PluginHandler : IPluginHandler
    {
        private readonly IAssemblyWrapper assemblyWrapper;
        private readonly IFileSystem fileSystem;
        private readonly IReporter reporter;
        private readonly IFileversionWrapper versionWrapper;
        private Dictionary<Type, IPlugin> plugins;
        private Dictionary<string, ISqlLintRule> rules;

        public PluginHandler(IReporter reporter, Dictionary<string, ISqlLintRule> rules)
            : this(reporter, new FileSystem(), new AssemblyWrapper(), new VersionInfoWrapper(), rules) { }

        public PluginHandler(
            IReporter reporter,
            IFileSystem fileSystem,
            IAssemblyWrapper assemblyWrapper,
            IFileversionWrapper versionWrapper,
            Dictionary<string, ISqlLintRule> rules)
        {
            this.reporter = reporter;
            this.fileSystem = fileSystem;
            this.assemblyWrapper = assemblyWrapper;
            this.versionWrapper = versionWrapper;
            this.rules = rules;
        }

        public IList<IPlugin> Plugins => plugins.Values.ToList();

        private Dictionary<Type, IPlugin> List => plugins ??= new Dictionary<Type, IPlugin>();

        public void ProcessPaths(Dictionary<string, string> pluginPaths)
        {
            // process user specified plugins
            foreach (var pluginPath in pluginPaths)
            {
                ProcessPath(pluginPath.Value);
            }
        }

        public void ProcessPath(string path)
        {
            // remove quotes from path
            path = path.Replace("\"", string.Empty).Trim();

            char[] arrToReplace = { '\\', '/' };
            foreach (var toReplace in arrToReplace)
            {
                path = path.Replace(toReplace, fileSystem.Path.DirectorySeparatorChar);
            }

            if (!fileSystem.File.Exists(path))
            {
                if (fileSystem.Directory.Exists(path))
                {
                    LoadPluginDirectory(path);
                }
                else
                {
                    reporter.Report($"\nFailed to load plugin(s) defined by '{path}'. No file or directory found by that name.\n");
                }
            }
            else
            {
                LoadPlugin(path);
            }
        }

        private static void LogExceptionDetails(Exception ex, string context)
        {
            var message = new StringBuilder();
            message.AppendLine($"Exception during: {context}");
            message.AppendLine($"Exception Type: {ex.GetType().FullName}");
            message.AppendLine($"Message: {ex.Message}");
            message.AppendLine($"Stack Trace: {ex.StackTrace}");

            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                message.AppendLine($"Inner Exception: {innerEx.GetType().FullName}");
                message.AppendLine($"Inner Message: {innerEx.Message}");
                innerEx = innerEx.InnerException;
            }

            Trace.WriteLine(message.ToString());
        }

        public void LoadPluginDirectory(string path)
        {
            var subdirectoryEntries = fileSystem.Directory.GetDirectories(path);
            foreach (var entry in subdirectoryEntries)
            {
                ProcessPath(entry);
            }

            var fileEntries = fileSystem.Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
            foreach (var entry in fileEntries)
            {
                LoadPlugin(entry);
            }
        }

        public void LoadPlugin(string assemblyPath)
        {
            try
            {
                var path = fileSystem.Path.GetFullPath(assemblyPath);
                Assembly dll;

                try
                {
                    dll = assemblyWrapper.LoadFrom(path);
                }
                catch (FileNotFoundException ex)
                {
                    reporter.Report($"Plugin assembly not found: {assemblyPath}");
                    LogExceptionDetails(ex, $"Loading assembly from {assemblyPath}");
                    return;
                }
                catch (BadImageFormatException ex)
                {
                    reporter.Report($"Plugin assembly has invalid format (wrong architecture or corrupted): {assemblyPath}");
                    LogExceptionDetails(ex, $"Loading assembly from {assemblyPath}");
                    return;
                }
                catch (FileLoadException ex)
                {
                    reporter.Report($"Plugin assembly could not be loaded: {assemblyPath} - {ex.Message}");
                    LogExceptionDetails(ex, $"Loading assembly from {assemblyPath}");
                    return;
                }

                foreach (var type in assemblyWrapper.GetExportedTypes(dll))
                {
                    var inerfaces = type.GetInterfaces();

                    if (!inerfaces.Contains(typeof(IPlugin)))
                    {
                        continue;
                    }

                    if (!List.ContainsKey(type))
                    {
                        var plugin = (IPlugin)Activator.CreateInstance(type);
                        List.Add(type, plugin);
                        var version = versionWrapper.GetVersion(dll);
                        reporter.Report($"Loaded plugin: '{type.FullName}', Version: '{version}'");

                        foreach (var rule in plugin.GetRules())
                        {
                            try
                            {
                                rules.Add(rule.Key, rule.Value);
                            }
                            catch (ArgumentException ex) when (ex.ParamName == "key")
                            {
                                reporter.Report($"Plugin '{type.FullName}' attempted to register duplicate rule: {rule.Key}");
                                LogExceptionDetails(ex, $"Loading rule {rule.Key} from plugin {type.FullName}");
                            }
                            catch (Exception ex)
                            {
                                reporter.Report($"Failed to load rule '{rule.Key}' from plugin '{type.FullName}': {ex.Message}");
                                LogExceptionDetails(ex, $"Loading rule {rule.Key} from plugin {type.FullName}");
                            }
                        }
                    }
                    else
                    {
                        reporter.Report($"Already loaded plugin with type '{type.FullName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                reporter.Report($"Unexpected error loading plugin from {assemblyPath}: {ex.Message}");
                LogExceptionDetails(ex, $"Loading plugin from {assemblyPath}");
            }
        }

        public void ActivatePlugins(IPluginContext pluginContext)
        {
            foreach (var plugin in List)
            {
                try
                {
                    plugin.Value.PerformAction(pluginContext, reporter);
                }
                catch (NotImplementedException)
                {
                    // Plugin doesn't implement PerformAction (uses GetRules instead)
                    // This is normal, don't report as error
                    Trace.WriteLine($"Plugin {plugin.Key} does not implement PerformAction");
                }
                catch (Exception ex)
                {
                    reporter.Report($"Plugin '{plugin.Key}' threw exception during activation: {ex.GetType().Name} - {ex.Message}");
                    LogExceptionDetails(ex, $"Activating plugin {plugin.Key} for file {pluginContext.FilePath}");
                }
            }
        }
    }
}
