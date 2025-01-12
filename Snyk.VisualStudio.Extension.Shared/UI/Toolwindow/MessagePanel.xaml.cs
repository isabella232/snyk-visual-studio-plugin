﻿namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.Shell;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.UI.Notifications;

    /// <summary>
    /// Interaction logic for MessagePanel.xaml.
    /// </summary>
    public partial class MessagePanel : UserControl
    {
        private IList<StackPanel> panels;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePanel"/> class.
        /// </summary>
        public MessagePanel()
        {
            this.InitializeComponent();

            this.panels = new List<StackPanel>
            {
                this.selectIssueMessagePanel,
                this.noIssuesMessagePanel,
                this.runScanMessagePanel,
                this.messagePanel,
                this.overviewPanel,
                this.scanningProjectMessagePanel,
                this.localCodeEngineIsEnabledPanel,
            };
        }

        /// <summary>
        /// Gets or sets <see cref="ISnykServiceProvider"/> instance.
        /// </summary>
        public ISnykServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets <see cref="ToolWindowContext"/> instance.
        /// </summary>
        public ToolWindowContext Context { get; set; }

        /// <summary>
        /// Sets text on panel.
        /// </summary>
        public string Text
        {
            set
            {
                this.message.Text = value;

                this.ShowPanel(this.messagePanel);
            }
        }

        /// <summary>
        /// Show run scan message.
        /// </summary>
        public void ShowRunScanMessage() => this.ShowPanel(this.runScanMessagePanel);

        /// <summary>
        /// Show select issue message.
        /// </summary>
        public void ShowSelectIssueMessage() => this.ShowPanel(this.selectIssueMessagePanel);

        /// <summary>
        /// 
        /// </summary>
        public void ShowDisabledDueToLocalCodeEngineMessage() => this.ShowPanel(this.localCodeEngineIsEnabledPanel);

        /// <summary>
        /// Show scanning message.
        /// </summary>
        public void ShowScanningMessage() => this.ShowPanel(this.scanningProjectMessagePanel);

        /// <summary>
        /// Show overview screen message.
        /// </summary>
        public void ShowOverviewScreenMessage() => this.ShowPanel(this.overviewPanel);

        private void RunButton_Click(object sender, RoutedEventArgs e) => ThreadHelper.JoinableTaskFactory.RunAsync(SnykTasksService.Instance.ScanAsync);

        private void ShowPanel(StackPanel panel)
        {
            foreach (var stackPanel in this.panels)
            {
                stackPanel.Visibility = Visibility.Collapsed;
            }

            panel.Visibility = Visibility.Visible;
        }

        private void TestCodeNow_Click(object sender, RoutedEventArgs e)
        {
            Action<string> successCallbackAction = (apiToken) =>
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    this.testCodeNowButton.IsEnabled = true;

                    this.connectVSToSnykProgressBar.Visibility = Visibility.Collapsed;
                });

                this.ServiceProvider.Options.ApiToken = apiToken;

                this.Context.TransitionTo(RunScanState.Instance);
            };

            Action<string> errorCallbackAction = (error) =>
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    this.testCodeNowButton.IsEnabled = true;

                    this.connectVSToSnykProgressBar.Visibility = Visibility.Collapsed;

                    NotificationService.Instance.ShowErrorInfoBar(error);
                });

                this.Context.TransitionTo(OverviewState.Instance);
            };

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.testCodeNowButton.IsEnabled = false;

                this.connectVSToSnykProgressBar.Visibility = Visibility.Visible;
            });

            this.ServiceProvider.Options.Authenticate(successCallbackAction, errorCallbackAction);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs args)
        {
            Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri));

            args.Handled = true;
        }
    }
}
