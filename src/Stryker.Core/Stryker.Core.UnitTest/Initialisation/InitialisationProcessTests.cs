using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using Buildalyzer;
using Mono.Collections.Generic;
using Moq;
using Stryker.Core.Exceptions;
using Stryker.Core.Initialisation;
using Stryker.Core.Mutants;
using Stryker.Core.Options;
using Stryker.Core.ProjectComponents;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.Core.TestRunners;
using Xunit;

namespace Stryker.Core.UnitTest.Initialisation
{
    public class InitialisationProcessTests : TestBase
    {
        [Fact]
        public void InitialisationProcess_ShouldCallNeededResolvers()
        {
            var testRunnerMock = new Mock<ITestRunner>(MockBehavior.Strict);
            var inputFileResolverMock = new Mock<IInputFileResolver>(MockBehavior.Strict);
            var initialBuildProcessMock = new Mock<IInitialBuildProcess>(MockBehavior.Strict);
            var initialTestProcessMock = new Mock<IInitialTestProcess>(MockBehavior.Strict);

            testRunnerMock.Setup(x => x.RunAll(It.IsAny<ITimeoutValueCalculator>(), null, null))
                .Returns(new TestRunResult(true)); // testrun is successful
            testRunnerMock.Setup(x => x.DiscoverTests()).Returns(new TestSet());
            testRunnerMock.Setup(x => x.Dispose());
            var projectContents = new CsharpFolderComposite();
            projectContents.Add(new CsharpFileLeaf());
            var folder = new CsharpFolderComposite();
            folder.AddRange(new Collection<IProjectComponent>
                {
                    new CsharpFileLeaf()
                });
            inputFileResolverMock.Setup(x => x.ResolveSourceProjectInfo(It.IsAny<StrykerOptions>(), TODO))
                .Returns(new SourceProjectInfo(new MockFileSystem())
                {
                    ProjectUnderTestAnalyzerResult = TestHelper.SetupProjectAnalyzerResult(
                        references: new string[0]).Object,
                    TestProjectAnalyzerResults = new List<IAnalyzerResult> { TestHelper.SetupProjectAnalyzerResult(
                        projectFilePath: "C://Example/Dir/ProjectFolder",
                        targetFramework: "netcoreapp2.1").Object
                    },
                    ProjectContents = folder
                });
            initialTestProcessMock.Setup(x => x.InitialTest(It.IsAny<StrykerOptions>(), It.IsAny<ITestRunner>())).Returns(new InitialTestRun(new TestRunResult(true), new TimeoutValueCalculator(1)));
            initialBuildProcessMock.Setup(x => x.InitialBuild(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), null));

            var target = new InitialisationProcess(
                inputFileResolverMock.Object,
                initialBuildProcessMock.Object,
                initialTestProcessMock.Object,
                testRunnerMock.Object);

            var options = new StrykerOptions
            {
                ProjectName = "TheProjectName",
                ProjectVersion = "TheProjectVersion"
            };

            var result = target.Initialize(options);

            inputFileResolverMock.Verify(x => x.ResolveSourceProjectInfo(It.IsAny<StrykerOptions>(), TODO), Times.Once);
        }

        [Fact]
        public void InitialisationProcess_ShouldThrowOnFailedInitialTestRun()
        {
            var testRunnerMock = new Mock<ITestRunner>(MockBehavior.Strict);
            var inputFileResolverMock = new Mock<IInputFileResolver>(MockBehavior.Strict);
            var initialBuildProcessMock = new Mock<IInitialBuildProcess>(MockBehavior.Strict);
            var initialTestProcessMock = new Mock<IInitialTestProcess>(MockBehavior.Strict);

            testRunnerMock.Setup(x => x.RunAll(It.IsAny<ITimeoutValueCalculator>(), null, null));
            var folder = new CsharpFolderComposite();
            folder.Add(new CsharpFileLeaf());

            inputFileResolverMock.Setup(x => x.ResolveSourceProjectInfo(It.IsAny<StrykerOptions>(), TODO)).Returns(
                new SourceProjectInfo(new MockFileSystem())
                {
                    ProjectUnderTestAnalyzerResult = TestHelper.SetupProjectAnalyzerResult(
                        references: new string[0]).Object,
                    TestProjectAnalyzerResults = new List<IAnalyzerResult> { TestHelper.SetupProjectAnalyzerResult(
                        projectFilePath: "C://Example/Dir/ProjectFolder",
                        targetFramework: "netcoreapp2.1").Object
                    },
                    ProjectContents = folder
                });

            initialBuildProcessMock.Setup(x => x.InitialBuild(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), null));
            testRunnerMock.Setup(x => x.DiscoverTests()).Returns(new TestSet());
            testRunnerMock.Setup(x => x.Dispose());
            initialTestProcessMock.Setup(x => x.InitialTest(It.IsAny<StrykerOptions>(), It.IsAny<ITestRunner>())).Throws(new InputException("")); // failing test

            var target = new InitialisationProcess(
                inputFileResolverMock.Object,
                initialBuildProcessMock.Object,
                initialTestProcessMock.Object,
                testRunnerMock.Object);
            var options = new StrykerOptions
            {
                ProjectName = "TheProjectName",
                ProjectVersion = "TheProjectVersion"
            };

            target.Initialize(options);
            Assert.Throws<InputException>(() => target.InitialTest(options));

            inputFileResolverMock.Verify(x => x.ResolveSourceProjectInfo(It.IsAny<StrykerOptions>(), TODO), Times.Once);
            initialTestProcessMock.Verify(x => x.InitialTest(It.IsAny<StrykerOptions>(), testRunnerMock.Object), Times.Once);
        }
    }
}
