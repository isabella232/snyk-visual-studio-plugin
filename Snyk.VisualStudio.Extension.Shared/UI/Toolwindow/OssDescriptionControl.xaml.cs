﻿namespace Snyk.VisualStudio.Extension.Shared.UI.Toolwindow
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using Snyk.VisualStudio.Extension.Shared.CLI;

    /// <summary>
    /// Interaction logic for OssDescriptionControl.xaml.
    /// </summary>
    public partial class OssDescriptionControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OssDescriptionControl"/> class.
        /// </summary>
        public OssDescriptionControl() => this.InitializeComponent();

        /// <summary>
        /// Sets <see cref="Vulnerability"/> information and update corresponding UI elements.
        /// </summary>
        public Vulnerability Vulnerability
        {
            set
            {
                var vulnerability = value;

                this.vulnerabilityDescriptionGrid.Visibility = Visibility.Visible;

                this.vulnerableModule.Text = vulnerability.Name;

                string introducedThroughText = vulnerability.From != null && vulnerability.From.Length != 0
                            ? string.Join(", ", vulnerability.From) : string.Empty;

                this.introducedThrough.Text = introducedThroughText;
                this.exploitMaturity.Text = vulnerability.Exploit;
                this.fixedIn.Text = string.IsNullOrWhiteSpace(vulnerability.FixedInDisplayText)
                    ? $"There is no fixed version for {vulnerability.Name}" : vulnerability.FixedInDisplayText;

                string detaiedIntroducedThroughText = vulnerability.From != null && vulnerability.From.Length != 0
                            ? string.Join(" > ", vulnerability.From) : string.Empty;

                this.detaiedIntroducedThrough.Text = detaiedIntroducedThroughText;

                this.fix.Text = vulnerability.FixedIn != null && vulnerability.FixedIn.Length != 0
                                         ? "Upgrade to " + string.Join(" > ", vulnerability.FixedIn) : string.Empty;

                this.overview.Html = new MarkdownSharp.Markdown().Transform(vulnerability.Description);

                this.moreAboutThisIssue.NavigateUri = new Uri(vulnerability.Url);
            }
        }

        /// <summary>
        /// Adapt components for VS theme change.
        /// </summary>
        public void AdaptComponentsForThemeChange()
        {
            this.overview.AdaptForeground();
        }

        private void MoreAboutThisIssue_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs args)
        {
            Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri));

            args.Handled = true;
        }
    }
}
