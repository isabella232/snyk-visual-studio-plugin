﻿using Microsoft.VisualStudio.Shell;

namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell.Settings;
    using Serilog;
    using Snyk.Analytics;
    using Snyk.Code.Library.Service;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Snyk.VisualStudio.Extension.Shared.Theme;
    using Snyk.VisualStudio.Extension.Shared.UI;
    using Snyk.VisualStudio.Extension.Shared.UI.Notifications;
    using Snyk.VisualStudio.Extension.Shared.UI.Toolwindow;
    using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Main logic for Snyk extension.
    /// </summary>
    public class SnykService : ISnykServiceProvider, ISnykService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykService>();

        private readonly IAsyncServiceProvider serviceProvider;

        private SettingsManager settingsManager;

        private SnykVsThemeService vsThemeService;

        private SnykTasksService tasksService;

        private DTE2 dte;

        private ISnykAnalyticsService analyticsService;

        private SnykUserStorageSettingsService userStorageSettingsService;

        private ISnykCodeService snykCodeService;

        private IOssService ossService;

        private ISnykApiService apiService;

        private ISentryService sentryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider implementation.</param>
        public SnykService(IAsyncServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        /// <summary>
        /// Gets Snyk options implementation.
        /// </summary>
        public ISnykOptions Options => this.Package.GeneralOptionsDialogPage;

        /// <summary>
        /// Gets solution service.
        /// </summary>
        public ISolutionService SolutionService => SnykSolutionService.Instance;

        /// <summary>
        /// Gets Tasks service.
        /// </summary>
        public SnykTasksService TasksService => this.tasksService;

        /// <summary>
        /// Gets VS Settings manager.
        /// </summary>
        public SettingsManager SettingsManager => this.settingsManager;

        /// <summary>
        /// Gets VS dte instance.
        /// </summary>
        public DTE2 DTE => this.dte;

        /// <summary>
        /// Gets Snyk Extension package intance.
        /// </summary>
        public SnykVSPackage Package => this.serviceProvider as SnykVSPackage;

        /// <summary>
        /// Gets implementation of IAsyncServiceProvider.
        /// </summary>
        public IAsyncServiceProvider AsyncServiceProvider => this.serviceProvider;

        /// <summary>
        /// Gets VS theme service.
        /// </summary>
        public SnykVsThemeService VsThemeService => this.vsThemeService;

        /// <summary>
        /// Gets Analytics service instance. If analytics service not created yet it will create it and return.
        /// </summary>
        public ISnykAnalyticsService AnalyticsService
        {
            get
            {
                if (this.analyticsService == null)
                {
                    this.InitializeAnalyticsService();

                    // When settings change (API endpoint/analytics enabling), re-initialize the service
                    this.Options.SettingsChanged += (sender, args) =>
                    {
                        Logger.Information("Notifying analytics service after settings change");
                        this.InitializeAnalyticsService();
                        ThreadHelper.JoinableTaskFactory.Run(async () => await this.analyticsService.ObtainUserAsync(this.Options.ApiToken));
                        Logger.Information("Analytics service re-initialized");
                    };
                }

                return this.analyticsService;
            }
        }

        /// <summary>
        /// Gets user storage settings service instance.
        /// </summary>
        public SnykUserStorageSettingsService UserStorageSettingsService
        {
            get
            {
                if (this.userStorageSettingsService == null)
                {
                    string settingsFilePath = Path.Combine(SnykExtension.GetExtensionDirectoryPath(), "settings.json");

                    this.userStorageSettingsService = new SnykUserStorageSettingsService(settingsFilePath, this);
                }

                return this.userStorageSettingsService;
            }
        }

        /// <inheritdoc/>
        public ISnykCodeService SnykCodeService
        {
            get
            {
                if (this.snykCodeService == null)
                {
                    this.SetupSnykCodeService();
                }

                return this.snykCodeService;
            }
        }

        /// <inheritdoc/>
        public ISnykApiService ApiService
        {
            get
            {
                if (this.apiService == null)
                {
                    this.apiService = new SnykApiService(this.Options);

                    this.Options.SettingsChanged += this.OnSettingsChanged;
                }

                return this.apiService;
            }
        }

        /// <inheritdoc/>
        public IOssService OssService
        {
            get
            {
                if (this.ossService == null)
                {
                    this.ossService = new OssService(this);
                }

                return this.ossService;
            }
        }

        /// <inheritdoc/>
        public ISentryService SentryService
        {
            get
            {
                if (this.sentryService == null)
                {
                    this.sentryService = new SentryService(this);
                }

                return this.sentryService;
            }
        }

        /// <inheritdoc/>
        public SnykToolWindowControl ToolWindow => this.Package.ToolWindowControl;

        public ApiEndpointResolver ApiEndpointResolver => new ApiEndpointResolver(this.Options);

        /// <summary>
        /// Get Visual Studio service by type.
        /// </summary>
        /// <param name="serviceType">Needed service type.</param>
        /// <returns>Result VS service instance</returns>
        public async Task<object> GetServiceAsync(Type serviceType) => await this.serviceProvider.GetServiceAsync(serviceType);

        /// <summary>
        /// Get Visual Studio service by type (not async method).
        /// </summary>
        /// <param name="serviceType">Needed service type.</param>
        /// <returns>Result VS service instance</returns>
        public object GetService(Type serviceType) => null;

        /// <summary>
        /// Initialize service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token instance.</param>
        /// <returns>Task.</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.Information("Initialize Snyk services");

                await this.Package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                this.settingsManager = new ShellSettingsManager(this.Package);
                this.vsThemeService = new SnykVsThemeService(this);

                await this.vsThemeService.InitializeAsync();
                await SnykToolWindowCommand.InitializeAsync(this);
                await SnykTasksService.InitializeAsync(this);

                this.dte = await this.serviceProvider.GetServiceAsync(typeof(DTE)) as DTE2;
                await SnykSolutionService.Instance.InitializeAsync(this);
                this.tasksService = SnykTasksService.Instance;

                NotificationService.Initialize(this);
                VsStatusBar.Initialize(this);
                VsCodeService.Initialize();

                Logger.Information("Leave SnykService.InitializeAsync");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on initialize Snyk service");
            }
        }

        /// <summary>
        /// Create new instance of SnykCli class with Options and Logger parameters.
        /// </summary>
        /// <returns>New SnykCli instance.</returns>
        public ICli NewCli() => new SnykCli { Options = this.Options, };

        /// <summary>
        /// Check is Options.ApiToken initialized. But if it's empty it will call CLI.GetApiToken() method.
        /// </summary>
        /// <returns>User API token string</returns>
        public string GetApiToken()
        {
            if (!string.IsNullOrEmpty(this.Options.ApiToken))
            {
                return this.Options.ApiToken;
            }

            return this.NewCli().GetApiToken();
        }

        private void OnSettingsChanged(object sender, SnykSettingsChangedEventArgs e) => this.SetupSnykCodeService();

        private void SetupSnykCodeService()
        {
            try
            {
                var options = this.Options;

                string endpoint = new ApiEndpointResolver(this.Options).GetSnykCodeApiUrl();

                this.snykCodeService = CodeServiceFactory.CreateSnykCodeService(
                    options.ApiToken,
                    endpoint,
                    this.SolutionService.FileProvider,
                    SnykExtension.IntegrationName,
                    options.Organization ?? string.Empty);

                VsStatusBarNotificationService.Instance.InitializeEventListeners(this.snykCodeService, options);
            }
            catch (Exception e)
            {
                Logger.Error(e, string.Empty);
            }
        }

        private void InitializeAnalyticsService()
        {
            Logger.Information("Initialize Analytics Service...");
            var writeKey = SnykExtension.AppSettings?.SegmentAnalyticsWriteKey;

            string anonymousId = this.Options.AnonymousId;
            if (string.IsNullOrEmpty(anonymousId))
            {
                anonymousId = System.Guid.NewGuid().ToString();
                this.Options.AnonymousId = anonymousId;
            }

            var enabled = this.Options.UsageAnalyticsEnabled;
            var endpoint = this.ApiEndpointResolver.UserMeEndpoint;

            Logger.Information("analytics enabled = {Enabled}, endpoint = {Endpoint}", enabled, endpoint);
            SnykAnalyticsService.Initialize(this.Options.AnonymousId, writeKey, enabled, endpoint);
            this.analyticsService = SnykAnalyticsService.Instance;
            Logger.Information("Analytics service initialized");
        }
    }
}
