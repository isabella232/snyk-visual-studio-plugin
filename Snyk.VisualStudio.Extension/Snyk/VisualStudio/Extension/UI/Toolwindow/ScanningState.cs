﻿namespace Snyk.VisualStudio.Extension.UI.Toolwindow
{
    using System.Windows;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Implements Scanning state for tool window.
    /// </summary>
    public class ScanningState : ToolWindowState
    {
        /// <summary>
        /// Gets a value indicating whether new <see cref="ScanningState"/> object.
        /// </summary>
        public static ScanningState Instance => new ScanningState();

        /// <summary>
        /// Hide main message and progress bar.
        /// </summary>
        public override void HideComponents()
        {
            this.ToolWindowControl.HideMainMessage();

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.ToolWindowControl.progressBar.Value = 0;
                this.ToolWindowControl.progressBar.IsIndeterminate = false;

                this.ToolWindowControl.progressBarPanel.Visibility = Visibility.Collapsed;

                this.ToolWindowControl.resultsGrid.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// Display main message and progress bar.
        /// </summary>
        public override void DisplayComponents()
        {
            this.ToolWindowControl.DisplayMainMessage("Scanning project for vulnerabilities...");

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.ToolWindowControl.progressBar.Value = 0;
                this.ToolWindowControl.progressBar.IsIndeterminate = true;

                this.ToolWindowControl.progressBarPanel.Visibility = Visibility.Visible;

                this.ToolWindowControl.resultsGrid.Visibility = Visibility.Visible;
            });
        }
    }
}
