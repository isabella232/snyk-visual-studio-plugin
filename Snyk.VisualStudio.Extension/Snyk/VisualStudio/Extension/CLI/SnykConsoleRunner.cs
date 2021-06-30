﻿namespace Snyk.VisualStudio.Extension.CLI
{
    using System;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// Incapsulate work with console/terminal.
    /// </summary>
    public class SnykConsoleRunner
    {
        private bool isStopped = false;

        /// <summary>
        /// Gets or sets a value indicating whether process.
        /// </summary>
        public Process Process { get; set; }

        /// <summary>
        /// Gets a value indicating whether is current process is stoped or still running.
        /// </summary>
        public bool IsStopped => this.isStopped;

        /// <summary>
        /// Run file name with arguments.
        /// </summary>
        /// <param name="fileName">Path to file for run.</param>
        /// <param name="arguments">Arguments for programm to run.</param>
        /// <returns>Result string from programm.</returns>
        public virtual string Run(string fileName, string arguments)
        {
            this.CreateProcess(fileName, arguments);

            return this.Execute();
        }

        /// <summary>
        /// Create process to run external programm in console.
        /// </summary>
        /// <param name="fileName">Programm file name (full path).</param>
        /// <param name="arguments">Arguments for programm to run.</param>
        /// <returns>Result process.</returns>
        public virtual Process CreateProcess(string fileName, string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            processStartInfo.EnvironmentVariables["SNYK_INTEGRATION_NAME"] = SnykExtension.IntegrationName;
            processStartInfo.EnvironmentVariables["SNYK_INTEGRATION_VERSION"] = SnykExtension.GetIntegrationVersion();

            this.Process = new Process
            {
                StartInfo = processStartInfo,
            };

            return this.Process;
        }

        /// <summary>
        /// Execute current process.
        /// </summary>
        /// <returns>Return result from external process.</returns>
        public virtual string Execute()
        {
            var stringBuilder = new StringBuilder();

            try
            {
                this.Process.Start();

                while (!this.Process.StandardOutput.EndOfStream)
                {
                    stringBuilder.AppendLine(this.Process.StandardOutput.ReadLine());
                }
            }
            catch (Exception exception)
            {
                stringBuilder.Append(exception.Message);
            }

            this.Process = null;

            return stringBuilder.ToString().Replace("\n", string.Empty).Replace("\r", string.Empty);
        }

        /// <summary>
        /// Stop (kill) current running process.
        /// </summary>
        public void Stop()
        {
            this.Process?.Kill();

            this.isStopped = true;
        }
    }
}