using SpecFlow.Variants.SpecFlowPlugin.Providers;
using System.CodeDom;
using System.Linq;
using TechTalk.SpecFlow.Parser;
using Xunit;

namespace SpecFlow.Variants.UnitTests
{
    public class MsTestProviderExtendedTests : TestBase
    {
        private readonly SpecFlowDocument _document;
        private readonly CodeNamespace _generatedCode;

        public MsTestProviderExtendedTests()
        {
            _document = CreateSpecFlowDocument();
            _generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(_document);
        }

        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsAndExamples)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData)]
        public void MsTestProviderExtended_CorrectNumberOfMethodsGenerated(string scenarioName)
        {
            var scenario = _document.SpecFlowFeature.Children.FirstOrDefault(a => a.Name == scenarioName);
            var expectedNumOfMethods = ExpectedNumOfMethods(scenario);
            var actualNumOfMethods = NumOfMethods(_generatedCode, scenario);

            Assert.Equal(expectedNumOfMethods, actualNumOfMethods);
        }

        [Fact]
        public void MsTestProviderExtended_SpecflowGeneratedCodeCompiles()
        {
            var assemblies = new[] { "System.Core.dll", "TechTalk.SpecFlow.dll", "System.dll", "System.Runtime.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions.dll" };
            var compilerResults = GetCompilerResults(_generatedCode, assemblies);

            Assert.Empty(compilerResults.Errors);
        }
    }
}
