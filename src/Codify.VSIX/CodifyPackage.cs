
using Codify.Storage;
using Codify.UI.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Codify.Core.Interfaces;
using Codify.VisualStudio.Events.Build;
using Codify.VSIX.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Task = System.Threading.Tasks.Task;

namespace Codify.VSIX
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(CodifyPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(CodifyToolWindow))]
    public sealed class CodifyPackage : AsyncPackage
    {
        /// <summary>
        /// CodifyPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "eb873b7a-8287-48ac-8a6b-646d4166809b";

        public static string ProjectName { get; set; }


        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                //System.Diagnostics.Debugger.Launch();
                await InitializePackageCoreAsync(cancellationToken, progress);
            }
            catch (Exception ex)
            {
                await HandlePackageInitializationErrorAsync(ex, cancellationToken);
            }
        }
        /// <summary>
        /// Performs the actual package initialization.
        /// Exceptions are intentionally not caught here because InitializeAsync is the bootstrap boundary.
        /// </summary>
        private async Task InitializePackageCoreAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            // Register global exception handlers as early as possible.
            RegisterGlobalExceptionHandlers();

            // 1. Basic IO setup (stays here as it's environment-related)
            await Task.Yield();

            // 4. UI/Command initialization on Main Thread
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Initialize UI Commands
            await CodifyToolWindowCommand.InitializeAsync(this);

            StoragePaths.EnsureCreated();

            var pane = await CreateVsOutputWindowPaneAsync();

            // 2. Initialize the Dependency Injection Container
            // This replaces all manual "new Service()" calls.
            CodifyServiceContainer.Initialize(this, pane);

            // 3. Perform Async Initializations
            // Since some services need to load files from disk, we do it here.
            await InitializeCoreServicesAsync();


            Debug.WriteLine("[Codify] DI Container & Package Initialized.");
        }
        /// <summary>
        /// Handles package initialization errors safely.
        /// This method must never throw because Visual Studio calls it during package loading.
        /// </summary>
        private async Task HandlePackageInitializationErrorAsync(
            Exception exception,
            CancellationToken cancellationToken)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                if (CodifyServiceContainer.IsInitialized)
                {
                    var handler = CodifyServiceContainer.Get<IErrorHandler>();

                    handler.Handle(
                        exception,
                        "CodifyPackage.InitializeAsync");
                }
                else
                {
                    Debug.WriteLine("[Codify] Package initialization failed before DI initialization.");
                    Debug.WriteLine(exception.ToString());

                    try
                    {
                        var pane = await CreateVsOutputWindowPaneAsync();

                        pane.OutputStringThreadSafe(
                            "[Codify] Package initialization failed before DI initialization.\n");

                        pane.OutputStringThreadSafe(
                            exception.ToString() + "\n");
                    }
                    catch
                    {
                        // Ignore output pane creation failure during bootstrap error handling.
                    }
                }

                VsShellUtilities.ShowMessageBox(
                    this,
                    "Codify could not start correctly. Please check Visual Studio Output window for details.",
                    "Codify",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch
            {
                Debug.WriteLine("[Codify] Failed to handle package initialization error.");
                Debug.WriteLine(exception.ToString());
            }
        }

        private static void RegisterGlobalExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    var exception = e.ExceptionObject as Exception
                                    ?? new Exception("Unknown AppDomain unhandled exception.");

                    if (CodifyServiceContainer.IsInitialized)
                    {
                        var handler = CodifyServiceContainer.Get<IErrorHandler>();

                        handler.Handle(
                            exception,
                            "AppDomain.UnhandledException");
                    }
                    else
                    {
                        Debug.WriteLine("[Codify] AppDomain unhandled exception before DI initialization.");
                        Debug.WriteLine(exception.ToString());
                    }
                }
                catch
                {
                    // ignored
                }
            };

            Dispatcher.CurrentDispatcher.UnhandledException += (s, e) =>
            {
                try
                {
                    if (CodifyServiceContainer.IsInitialized)
                    {
                        var handler = CodifyServiceContainer.Get<IErrorHandler>();

                        handler.Handle(
                            e.Exception,
                            "DispatcherUnhandledException");
                    }
                    else
                    {
                        Debug.WriteLine("[Codify] Dispatcher unhandled exception before DI initialization.");
                        Debug.WriteLine(e.Exception.ToString());
                    }

                    e.Handled = true;
                }
                catch
                {
                    e.Handled = true;
                }
            };
        }


        private async Task<IVsOutputWindowPane> CreateVsOutputWindowPaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            // Create Output Window pane
            var outputWindow = (IVsOutputWindow)await GetServiceAsync(typeof(SVsOutputWindow));

            var paneGuid = new Guid("7F6D8F51-2A4E-4A33-B7A7-8B1E9A7D1234");

            outputWindow?.CreatePane(
                ref paneGuid,
                "Codify",
                fInitVisible: 1,
                fClearWithSolution: 1);

            IVsOutputWindowPane pane = null;

            outputWindow?.GetPane(ref paneGuid, out pane);

            return pane;
        }
        /// <summary>
        /// Handles the async initialization of services that require file I/O or settings loading.
        /// </summary>
        private async Task InitializeCoreServicesAsync()
        {
            // We get the services from the container and call their init methods.

            var settings = CodifyServiceContainer.Get<SettingsManager>();
            await settings.InitializeAsync();

            var providers = CodifyServiceContainer.Get<ProviderManager>();
            await providers.InitializeAsync();

            foreach (var startupTask in CodifyServiceContainer.Instance.GetServices<IStartupTask>())
            {
                startupTask.Start();
            }

            var buildListener = CodifyServiceContainer.Get<BuildEventsListener>();
            await buildListener.InitializeAsync();
        }

        #endregion
    }
}
