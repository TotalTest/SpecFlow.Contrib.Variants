using Gherkin.Ast;
using SpecFlow.Variants.SpecFlowPlugin.Providers;
using System.CodeDom;
using System.Linq;
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

        [Fact]
        public void XUnitProviderExtended_BaseTestMethodHasCorrectArguments()
        {
            var scenario = _document.GetScenario<ScenarioOutline>(SampleFeatureFile.ScenarioTitle_TagsAndExamples);
            var baseTestMethod = _generatedCode.GetRowTestBaseMethod(scenario);
            var methodParams = baseTestMethod.GetMethodParameters();
            var tableHeaders = scenario.GetExamplesTableHeaders();

            for (var i = 0; i < tableHeaders.Count; i++)
            {
                Assert.Equal(methodParams[i].Name, tableHeaders[i].Value);
            }
        }

        [Fact]
        public void XUnitProviderExtended_TestMethodsHasCorrectFactAttribute()
        {
            var scenario = _document.GetScenario<ScenarioOutline>(SampleFeatureFile.ScenarioTitle_TagsAndExamples);
            var testMethods = _generatedCode.GetRowTestMethods(scenario);
            var tableBody = scenario.GetExamplesTableBody();

            var rowCounter = 0;
            var variantCounter = 0;

            for (var i = 0; i < testMethods.Count; i++)
            {
                var attArg = testMethods[i].GetMethodAttributes("Xunit.FactAttribute").FirstOrDefault();
                var cells = tableBody[rowCounter].Cells.ToList();

                var factName = attArg.Arguments[0].Name == "DisplayName";
                var factValue = attArg.Arguments[0].GetArgumentValue() == $"{SampleFeatureFile.ScenarioTitle_TagsAndExamples}: {cells[0].Value}_{SampleFeatureFile.Variants[variantCounter]}";

                Assert.True(factName);
                Assert.True(factValue);

                rowCounter++;
                if (i % 2 != 0) variantCounter++;
                if (rowCounter == tableBody.Count) rowCounter = 0;
            }
        }

        [Fact]
        public void XUnitProviderExtended_TestMethodsHaveCorrectTraits()
        {
            var scenario = _document.GetScenario<ScenarioOutline>(SampleFeatureFile.ScenarioTitle_TagsAndExamples);
            var testMethods = _generatedCode.GetRowTestMethods(scenario);
            var tableBody = scenario.GetExamplesTableBody();
            var nonVariantTags = scenario.GetTagsExceptNameStart(SampleFeatureFile.Variant).Select(a => a.GetNameWithoutAt()).ToList();

            var rowCounter = 0;
            var variantCounter = 0;

            for (var i = 0; i < testMethods.Count; i++)
            {
                var attArg = testMethods[i].GetMethodAttributes("Xunit.TraitAttribute").ToList();

                // Check first argument is the feature title
                var featureTitleArg = attArg[0];
                var argName = featureTitleArg.Arguments[0].GetArgumentValue() == "FeatureTitle";
                var argValue = featureTitleArg.Arguments[1].GetArgumentValue() == SampleFeatureFile.FeatureTitle;

                Assert.True(argName);
                Assert.True(argValue);

                var cells = tableBody[rowCounter].Cells.ToList();

                // Check second argument is the variant
                var descArg = attArg[1];
                var descName = descArg.Arguments[0].GetArgumentValue() == "Description";
                var descValue = descArg.Arguments[1].GetArgumentValue() == $"{SampleFeatureFile.ScenarioTitle_TagsAndExamples}: {cells[0].Value}_{SampleFeatureFile.Variants[variantCounter]}";

                Assert.True(descName);
                Assert.True(descValue);

                // Check third argument is the variant
                var variantArg = attArg[2];
                var variantName = variantArg.Arguments[0].GetArgumentValue() == "Category";
                var variantValue = variantArg.Arguments[1].GetArgumentValue() == $"{SampleFeatureFile.Variant}:{SampleFeatureFile.Variants[variantCounter]}";

                Assert.True(variantName);
                Assert.True(variantValue);

                // Check the end arguments are non variant tags
                for (var k = 0; k < nonVariantTags.Count; k++)
                {
                    var catArg = attArg[k + 3];
                    var catName = catArg.Arguments[0].GetArgumentValue() == "Category";
                    var catValue = catArg.Arguments[1].GetArgumentValue() == $"{nonVariantTags[k]}";

                    Assert.True(catName);
                    Assert.True(catName);
                }

                rowCounter++;
                if (i % 2 != 0) variantCounter++;
                if (rowCounter == tableBody.Count) rowCounter = 0;
            }
        }
    }
}
