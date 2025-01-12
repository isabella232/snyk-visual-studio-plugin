﻿namespace Snyk.Code.Library.Tests.Api
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Snyk.Code.Library.Domain;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.Code.Library.Service;
    using Snyk.Common;
    using Xunit;
    using static Snyk.Code.Library.Service.SnykCodeService;

    /// <summary>
    /// Test cases for <see cref="SnykCodeService"/>.
    /// </summary>
    public class SnykCodeServiceTest
    {
        private Mock<IBundleService> bundleServiceMock;
        private Mock<IFiltersService> filtersServiceMock;
        private Mock<IAnalysisService> analysisServiceMock;
        private Mock<ICodeCacheService> codeCacheServiceMock;
        private Mock<IDcIgnoreService> dcIgnoreServiceMock;
        private Mock<IFileProvider> fileProviderMock;

        private SnykCodeService snykCodeService;

        public SnykCodeServiceTest()
        {
            this.bundleServiceMock = new Mock<IBundleService>();
            this.filtersServiceMock = new Mock<IFiltersService>();
            this.analysisServiceMock = new Mock<IAnalysisService>();
            this.codeCacheServiceMock = new Mock<ICodeCacheService>();
            this.dcIgnoreServiceMock = new Mock<IDcIgnoreService>();
            this.fileProviderMock = new Mock<IFileProvider>();

            this.snykCodeService = new SnykCodeService(
                this.bundleServiceMock.Object,
                this.analysisServiceMock.Object,
                this.filtersServiceMock.Object,
                this.codeCacheServiceMock.Object,
                this.dcIgnoreServiceMock.Object);
        }

        [Fact]
        public async Task SnykCodeService_CodeCacheAndFileChangesExists_UpdatePreviousScanAsync()
        {
            var addedFiles = new List<string>
            {
                "C:\\Project\\Test\\Main.cs",
                "C:\\Project\\Test\\DbService.cs",
            };

            var changedFiles = new List<string> { "C:\\Project\\Test\\Window.cs", };

            this.fileProviderMock
                .Setup(fileProvider => fileProvider.GetChangedFiles())
                .Returns(changedFiles);

            this.fileProviderMock
                .Setup(fileProvider => fileProvider.GetAllChangedFiles())
                .Returns(changedFiles);

            var removedFiles = new List<string> { "C:\\Project\\Test\\DummyWindow.cs", };

            this.fileProviderMock
                .Setup(fileProvider => fileProvider.GetRemovedFiles())
                .Returns(removedFiles);

            this.fileProviderMock
                .Setup(fileProvider => fileProvider.ClearHistory());

            this.codeCacheServiceMock
                .Setup(analysisService => analysisService.IsCacheValid())
                .Returns(false);

            this.codeCacheServiceMock
                .Setup(analysisService => analysisService.IsCacheExists())
                .Returns(true);

            string bundleId = "testBundleId";

            this.codeCacheServiceMock
                .Setup(codeCacheService => codeCacheService.GetCachedBundleId())
                .Returns(bundleId);

            var extendFilePathToHashDict = new Dictionary<string, string>()
            {
                { "/Main.cs", "Hash1" },
                { "/DbService.cs", "Hash2" },
                { "/Window.cs", "Hash3" },
            };

            this.codeCacheServiceMock
                .Setup(codeCacheService => codeCacheService.GetFilePathToHashDictionaryAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(extendFilePathToHashDict);

            var analysisResults = new AnalysisResult
            {
                Status = AnalysisStatus.Complete,
                Progress = 1,
                FileAnalyses = new List<FileAnalysis>
                {
                    new FileAnalysis
                    {
                        FileName = "Main.cs",
                    },
                    new FileAnalysis
                    {
                        FileName = "DbService.cs",
                    },
                    new FileAnalysis
                    {
                        FileName = "Window.cs",
                    },
                },
            };

            this.codeCacheServiceMock
                .Setup(codeCacheService => codeCacheService.SetAnalysisResult(analysisResults));

            this.codeCacheServiceMock
                .Setup(codeCacheService => codeCacheService.SetCachedBundleId(bundleId));

            this.filtersServiceMock
                .Setup(filtersService => filtersService.FilterFilesAsync(changedFiles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(changedFiles);

            this.dcIgnoreServiceMock
                .Setup(dcIgnoreService => dcIgnoreService.FilterFiles(It.IsAny<string>(), changedFiles, It.IsAny<CancellationToken>()))
                .Returns(changedFiles);

            var extendedBundle = new Bundle { Id = bundleId, };

            this.bundleServiceMock
                .Setup(bundleService => bundleService.ExtendBundleAsync(
                    bundleId,
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(extendedBundle);

            this.bundleServiceMock
                .Setup(bundleService => bundleService.UploadMissingFilesAsync(
                    extendedBundle,
                    this.codeCacheServiceMock.Object,
                    It.IsAny<FireScanCodeProgressUpdate>(),
                    It.IsAny<CancellationToken>()));

            this.analysisServiceMock
                .Setup(analysisService => analysisService.GetAnalysisAsync(
                    extendedBundle.Id,
                    It.IsAny<FireScanCodeProgressUpdate>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResults);

            var analysisResult = await this.snykCodeService.ScanAsync(this.fileProviderMock.Object);

            Assert.NotNull(analysisResult);
            Assert.Equal(3, analysisResult.FileAnalyses.Count);
        }

        [Fact]
        public async Task SnykCodeService_CodeCacheExists_ReturnWithoutRemoteQueryAsync()
        {
            var analysisResults = new AnalysisResult
            {
                Status = AnalysisStatus.Complete,
                Progress = 1,
                FileAnalyses = new List<FileAnalysis>
                {
                    new FileAnalysis
                    {
                        FileName = "app1.js",
                    },
                    new FileAnalysis
                    {
                        FileName = "app2.js",
                    },
                },
            };

            this.codeCacheServiceMock
                .Setup(analysisService => analysisService.GetCachedAnalysisResult())
                .Returns(analysisResults);

            this.codeCacheServiceMock
                .Setup(analysisService => analysisService.IsCacheExists())
                .Returns(true);

            this.codeCacheServiceMock
                .Setup(analysisService => analysisService.IsCacheValid())
                .Returns(true);

            var solutionServiceMock = new Mock<ISolutionService>();

            solutionServiceMock
                .Setup(solutionService => solutionService.GetSolutionFolderAsync())
                .ReturnsAsync(TestResource.GetResourcesPath());

            var fileProvider = new SnykCodeFileProvider(solutionServiceMock.Object);

            var analysisResult = await this.snykCodeService.ScanAsync(fileProvider);

            Assert.NotNull(analysisResult);

            this.analysisServiceMock
                .Verify(analysisService => analysisService.GetAnalysisAsync(It.IsAny<string>(), It.IsAny<FireScanCodeProgressUpdate>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(0));

            this.codeCacheServiceMock
                .Verify(codeCacheService => codeCacheService.GetCachedAnalysisResult(), Times.Exactly(1));
        }

        [Fact]
        public void SnykCodeService_JsonErrorProvided_GetSnykCodeErrorMessageReturnClearMessageWithoutJson()
        {
            string error = this.snykCodeService.GetSnykCodeErrorMessage(new Exception("{\"code\": 401, \"message\": \"Not authorised\"}"));

            Assert.NotNull(error);
        }

        [Fact]
        public async Task SnykCodeService_TwoFilesWithIssuesProvided_ScanSuccessAsync()
        {
            var analysisResults = new AnalysisResult
            {
                Status = AnalysisStatus.Complete,
                Progress = 1,
                FileAnalyses = new List<FileAnalysis>
                {
                    new FileAnalysis
                    {
                        FileName = "app1.js",
                    },
                    new FileAnalysis
                    {
                        FileName = "app2.js",
                    },
                },
            };

            string filePath1 = TestResource.GetFileFullPath("app1.js");
            string filePath2 = TestResource.GetFileFullPath("app2.js");

            var filePaths = new List<string> { filePath1, filePath2 };

            string bundleId = "dummyId";

            var bundle = new Bundle
            {
                Id = bundleId,
                MissingFiles = new string[] { filePath1, filePath2 },
            };

            this.fileProviderMock
                .Setup(fileProvider => fileProvider.GetFilesAsync())
                .ReturnsAsync(filePaths);

            this.filtersServiceMock
                .Setup(filtersService => filtersService.FilterFilesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(filePaths);

            this.bundleServiceMock
                .Setup(bundleService => bundleService.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(bundle);

            this.bundleServiceMock
                .Setup(bundleService => bundleService.UploadFilesAsync(bundleId, It.IsAny<Dictionary<string, (string, string)>>(), It.IsAny<FireScanCodeProgressUpdate>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            this.bundleServiceMock
                .Setup(bundleService => bundleService.CheckBundleAsync(bundleId, It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((id, cancellationToken) => bundle.MissingFiles = new string[] { })
                .ReturnsAsync(bundle);

            this.analysisServiceMock
                .Setup(analysisService => analysisService.GetAnalysisAsync(bundleId, It.IsAny<FireScanCodeProgressUpdate>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResults);

            this.codeCacheServiceMock
                .Setup(analysisService => analysisService.GetCachedAnalysisResult())
                .Returns((AnalysisResult)null);

            this.codeCacheServiceMock
                .Setup(analysisService => analysisService.IsCacheExists())
                .Returns(false);

            this.codeCacheServiceMock
                .Setup(analysisService => analysisService.GetCachedAnalysisResult())
                .Returns((AnalysisResult)null);

            var analysisResult = await this.snykCodeService.ScanAsync(this.fileProviderMock.Object);

            Assert.NotNull(analysisResult);
            Assert.Equal(2, analysisResult.FileAnalyses.Count);

            Assert.Equal("app1.js", analysisResult.FileAnalyses[0].FileName);
            Assert.Equal("app2.js", analysisResult.FileAnalyses[1].FileName);

            this.bundleServiceMock
                .Verify(bundleService => bundleService.CreateBundleAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            this.bundleServiceMock
                .Verify(bundleService => bundleService.UploadMissingFilesAsync(It.IsAny<Bundle>(), It.IsAny<ICodeCacheService>(), It.IsAny<FireScanCodeProgressUpdate>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            this.analysisServiceMock
                .Verify(analysisService => analysisService.GetAnalysisAsync(bundleId, It.IsAny<FireScanCodeProgressUpdate>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task SnykCodeService_NoFilesForScanProvided_ReturnNullSuccessAsync()
        {
            var result = await this.snykCodeService.ScanAsync(this.fileProviderMock.Object);

            Assert.Null(result);
        }
    }
}
