using Gherkin.Ast;
using SpecFlow.Variants.SpecFlowPlugin.Providers;
using System.CodeDom;
using TechTalk.SpecFlow.Parser;
using Xunit;

namespace SpecFlow.Variants.UnitTests
{
    public class XUnitProviderExtendedTests : TestBase
    {
        private readonly SpecFlowDocument _document;
        private readonly CodeNamespace _generatedCode;

        public XUnitProviderExtendedTests()
        {
            _document = CreateSpecFlowDocument();
            _generatedCode = SetupFeatureGenerator<XUnitProviderExtended>(_document);
        }

        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsAndExamples)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData)]
        public void XUnitProviderExtended_CorrectNumberOfMethodsGenerated(string scenarioName)
        {
            var scenario = _document.GetScenario<ScenarioDefinition>(scenarioName);
            var expectedNumOfMethods = ExpectedNumOfMethods(scenario);
            var actualNumOfMethods = _generatedCode.GetTestMethods(scenario).Count;

            Assert.Equal(expectedNumOfMethods, actualNumOfMethods);
        }

        [Fact]
        public void XUnitProviderExtended_SpecflowGeneratedCodeCompiles()
        {
            var assemblies = new[] { "System.Core.dll", "TechTalk.SpecFlow.dll", "System.dll", "System.Runtime.dll", "xunit.core.dll", "xunit.abstractions.dll" };
            var compilerResults = GetCompilerResults(_generatedCode, assemblies);

            Assert.Empty(compilerResults.Errors);
        }
    }
}
