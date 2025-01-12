﻿namespace Snyk.VisualStudio.Extension.Tests
{
    using System.Threading;
    using Moq;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Service;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="OssService"/>.
    /// </summary>
    public class OssServiceTest
    {
        [Fact]
        public void OssServiceTest_NoCachedValuesExists_ReturnNewScanResult()
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var cliMock = new Mock<ICli>();
            var optionsMock = new Mock<ISnykOptions>();

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.Options)
                .Returns(optionsMock.Object);

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.NewCli())
                .Returns(cliMock.Object);

            var fakeCliResult = new CliResult();

            cliMock
                .Setup(cli => cli.ScanAsync(It.IsAny<string>()))
                .ReturnsAsync(fakeCliResult);

            var ossService = new OssService(serviceProviderMock.Object);

            var tokenSource = new CancellationTokenSource();

            var cliResult = ossService.ScanAsync(string.Empty, tokenSource.Token);

            Assert.NotNull(cliResult);
        }

        [Fact]
        public async System.Threading.Tasks.Task OssServiceTest_CachedValuesExists_ReturnCachedResultAsync()
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var cliMock = new Mock<ICli>();
            var optionsMock = new Mock<ISnykOptions>();

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.Options)
                .Returns(optionsMock.Object);

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.NewCli())
                .Returns(cliMock.Object);

            var fakeCliResult = new CliResult();

            cliMock
                .Setup(cli => cli.ScanAsync(It.IsAny<string>()))
                .ReturnsAsync(fakeCliResult);

            var ossService = new OssService(serviceProviderMock.Object);

            var tokenSource = new CancellationTokenSource();

            // Run scan first time will setup cache value.
            await ossService.ScanAsync(string.Empty, tokenSource.Token);

            // Get cached value.
            var cliResult = ossService.ScanAsync(string.Empty, tokenSource.Token);

            Assert.NotNull(cliResult);

            cliMock
                .Verify(cli => cli.ScanAsync(It.IsAny<string>()), Times.Exactly(1));
        }

        [Fact]
        public void OssServiceTest_ClearCache_ReturnNewValue()
        {
            var serviceProviderMock = new Mock<ISnykServiceProvider>();
            var cliMock = new Mock<ICli>();
            var optionsMock = new Mock<ISnykOptions>();

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.Options)
                .Returns(optionsMock.Object);

            serviceProviderMock
                .Setup(serviceProvider => serviceProvider.NewCli())
                .Returns(cliMock.Object);

            var fakeCliResult = new CliResult();

            cliMock
                .Setup(cli => cli.ScanAsync(It.IsAny<string>()))
                .ReturnsAsync(fakeCliResult);

            var ossService = new OssService(serviceProviderMock.Object);

            var tokenSource = new CancellationTokenSource();

            ossService.ScanAsync(string.Empty, tokenSource.Token);

            ossService.ClearCache();

            var cliResult = ossService.ScanAsync(string.Empty, tokenSource.Token);

            Assert.NotNull(cliResult);

            cliMock
                .Verify(cli => cli.ScanAsync(It.IsAny<string>()), Times.Exactly(2));
        }
    }
}
