using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.VisualStudio.Interfaces;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Codinex.VisualStudio.Diagnostics.Errors
{
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
    public sealed class VsDiagnosticsCollector(
        IVsOutputWindowService vsOutputWindowService,
        IVisualStudioServices visualStudio) : IVsDiagnosticsCollector
    {
        // Maps the running instance's major installation version to its marketing product
        // year (e.g. 18.x ships as "Visual Studio 2026"). Extend this as new majors ship;
        // there's no public API that returns the marketing year directly.
        private static readonly Dictionary<int, string> ProductYearByMajorVersion = new()
        {
            [15] = "2017",
            [16] = "2019",
            [17] = "2022",
            [18] = "2026"
        };

        public async Task<string> CollectOutputLogAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await vsOutputWindowService.ReadOutputAsync("Codinex", cancellationToken);
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> CollectVsInfoAsync()
        {
            var info = new Dictionary<string, string>();
            string installPath = null;

            try
            {
                var dte = await visualStudio.GetDteAsync();

                if (dte != null)
                {
                    info["VsVersion"] = dte.Version;
                    info["VsEdition"] = dte.Edition;
                    info["VsName"] = dte.Name;
                    installPath = TryGetInstallPath(dte);
                }
            }
            catch (Exception ex)
            {
                info["VsInfoError"] = ex.Message;
            }

            foreach (var kvp in CollectSetupInfo(installPath))
            {
                info[kvp.Key] = kvp.Value;
            }

            var majorVersion = ParseMajorVersion(
                info.TryGetValue("VsFullVersion", out var fullVersion) ? fullVersion : null,
                info.TryGetValue("VsVersion", out var shortVersion) ? shortVersion : null);

            if (majorVersion.HasValue && ProductYearByMajorVersion.TryGetValue(majorVersion.Value, out var year))
            {
                info["VsProductYear"] = year;
                info["VsProductName"] = $"Visual Studio {year}";
            }

            return info;
        }

        private static string TryGetInstallPath(EnvDTE80.DTE2 dte)
        {
            try
            {
                // dte.FullName is the path to devenv.exe; its parent's parent is the install root
                // (…\Common7\IDE\devenv.exe -> install root two levels up).
                var ideDir = System.IO.Path.GetDirectoryName(dte.FullName);
                var common7Dir = System.IO.Path.GetDirectoryName(ideDir);
                return System.IO.Path.GetDirectoryName(common7Dir);
            }
            catch
            {
                return null;
            }
        }

        // Queries the Visual Studio Setup Configuration COM API for the running instance's
        // full version, channel, and instance id — the same data source behind Help > About.
        private static Dictionary<string, string> CollectSetupInfo(string installPath)
        {
            var result = new Dictionary<string, string>();

            try
            {
                var query = (ISetupConfiguration2)new SetupConfiguration();
                var enumInstances = query.EnumAllInstances();

                var instances = new ISetupInstance[1];

                while (true)
                {
                    enumInstances.Next(1, instances, out var fetched);

                    if (fetched == 0)
                    {
                        break;
                    }

                    if (instances[0] is not ISetupInstance2 instance)
                    {
                        continue;
                    }

                    var thisInstallPath = instance.GetInstallationPath();

                    if (installPath != null &&
                        !string.Equals(
                            thisInstallPath?.TrimEnd('\\'),
                            installPath.TrimEnd('\\'),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    result["VsInstanceId"] = instance.GetInstanceId();
                    result["VsFullVersion"] = instance.GetInstallationVersion();
                    result["VsInstallPath"] = thisInstallPath;

                    if (instance is ISetupInstanceCatalog catalog)
                    {
                        var props = catalog.GetCatalogInfo();

                        if (props != null)
                        {
                            foreach (var name in props.GetNames())
                            {
                                try
                                {
                                    var value = props.GetValue(name);

                                    if (value != null)
                                    {
                                        result[$"VsCatalog_{name}"] = value.ToString();
                                    }
                                }
                                catch
                                {
                                    // Some catalog properties can be missing/inapplicable per instance; skip those.
                                }
                            }
                        }
                    }

                    break;
                }
            }
            catch
            {
                // Setup Configuration COM API isn't available in every host (e.g. tests
                // running outside a real VS process) — degrade gracefully.
            }

            return result;
        }

        private static int? ParseMajorVersion(string fullVersion, string shortVersion)
        {
            var source = !string.IsNullOrWhiteSpace(fullVersion) ? fullVersion : shortVersion;

            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            var majorPart = source.Split('.')[0];

            return int.TryParse(majorPart, out var major) ? major : null;
        }
    }
}
