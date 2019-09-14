using Gherkin.Ast;
using SpecFlow.Variants.SpecFlowPlugin.Providers;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow.Parser;
using Xunit;

namespace SpecFlow.Variants.UnitTests
{
    public class NUnitProviderExtendedTests : TestBase
    {
        private readonly SpecFlowDocument _document;
        private readonly CodeNamespace _generatedCode;

        public NUnitProviderExtendedTests()
        {
            _document = CreateSpecFlowDocument();
            _generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(_document);
        }

        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsAndExamples)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData)]
        public void NUnitProviderExtended_CorrectNumberOfMethodsGenerated(string scenarioName)
        {
            var scenario = _document.GetScenario<ScenarioDefinition>(scenarioName);
            var expectedNumOfMethods = ExpectedNumOfMethods(scenario);
            var actualNumOfMethods = _generatedCode.GetTestMethods(scenario).Count;

            Assert.Equal(expectedNumOfMethods, actualNumOfMethods);
        }

        [Fact]
        public void NUnitProviderExtended_SpecflowGeneratedCodeCompiles()
        {
            var assemblies = new[] { "System.Core.dll", "TechTalk.SpecFlow.dll", "System.dll", "System.Runtime.dll", "nunit.framework.dll" };
            var compilerResults = GetCompilerResults(_generatedCode, assemblies);

            Assert.Empty(compilerResults.Errors);
        }

        [Fact]
        public void NUnitProviderExtended_CorrectNumberOfTestCaseAttributes()
        {
            TestSetupForAttributes(out var scenario, out _, out var testCaseAttributes, out _);

            var expectedNumOfTestCaseAttributes = scenario.GetTagsByNameStart(SampleFeatureFile.Variant).Count
                * scenario.GetExamplesTableBody().Count;

            Assert.Equal(expectedNumOfTestCaseAttributes, testCaseAttributes.Count);
        }

        [Fact]
        public void NUnitProviderExtended_TestCaseAttributesHaveCorrectArguments()
        {
            TestSetupForAttributes(out _, out _, out var testCaseAttributes, out var tableBody);

            var attributeCounter = 0;
            for (var i = 0; i < tableBody.Count; i++)
            {
                var cells = tableBody[i].Cells.ToList();
                for (var j = 0; j < SampleFeatureFile.Variants.Length; j++)
                {
                    var attArg = testCaseAttributes[attributeCounter].Arguments.GetAttributeArguments();
                    attributeCounter++;

                    // Check initial arguments are examples table row cells
                    for (var k = 0; k < cells.Count; k++)
                    {
                        var exampleValueMatches = attArg[k].GetArgumentValue() == cells[k].Value;
                        Assert.True(exampleValueMatches);
                    }

                    // Check third argument is the variant
                    var variantArgumentMatches = attArg[cells.Count].GetArgumentValue() == SampleFeatureFile.Variants[j];
                    Assert.True(variantArgumentMatches);
                }
            }
        }

        [Fact]
        public void NUnitProviderExtended_TestCaseAttributesHaveCorrectCategory()
        {
            TestSetupForAttributes(out var scenario, out _, out var testCaseAttributes, out var tableBody);

            var attributeCounter = 0;
            for (var i = 0; i < tableBody.Count; i++)
            {
                var cells = tableBody[i].Cells.ToList();
                for (var j = 0; j < SampleFeatureFile.Variants.Length; j++)
                {
                    var attArg = testCaseAttributes[attributeCounter].Arguments.GetAttributeArguments();
                    attributeCounter++;

                    // Check forth argument is the category with the correct value
                    var varantTag = scenario.GetTagsByNameExact($"{SampleFeatureFile.Variant}:{SampleFeatureFile.Variants[j]}").GetNameWithoutAt();
                    var nonVariantTags = scenario.GetTagsExceptNameStart(SampleFeatureFile.Variant).Select(a => a.GetNameWithoutAt());
                    var expCategoryValue = $"{varantTag},{string.Join(",", nonVariantTags)}";
                    var categoryAttr = attArg[cells.Count + 2];

                    Assert.Equal("Category", categoryAttr.Name);
                    Assert.Equal(expCategoryValue, categoryAttr.GetArgumentValue());
                }
            }
        }

        [Fact]
        public void NUnitProviderExtended_TestCaseAttributesHaveCorrectTestName()
        {
            TestSetupForAttributes(out _, out var testMethod, out var testCaseAttributes, out var tableBody);

            var attributeCounter = 0;
            for (var i = 0; i < tableBody.Count; i++)
            {
                var cells = tableBody[i].Cells.Select(a => a.Value).ToList();
                for (var j = 0; j < SampleFeatureFile.Variants.Length; j++)
                {
                    var attArg = testCaseAttributes[attributeCounter].Arguments.GetAttributeArguments();
                    attributeCounter++;

                    // Check forth argument is the category with the correct value
                    var currentVariant = SampleFeatureFile.Variants[j];
                    var expTestName = $"{testMethod.Name} with {currentVariant} and {string.Join(", ", cells)}";
                    var testNameAttr = attArg[cells.Count + 3];

                    Assert.Equal("TestName", testNameAttr.Name);
                    Assert.Equal(expTestName, testNameAttr.GetArgumentValue().Replace("\"", ""));
                }
            }
        }

        private void TestSetupForAttributes(out ScenarioOutline scenario, out CodeTypeMember testMethod, out IList<CodeAttributeDeclaration> testCaseAttributes, out IList<TableRow> tableBody)
        {
            scenario = _document.GetScenario<ScenarioOutline>(SampleFeatureFile.ScenarioTitle_TagsAndExamples);
            testMethod = _generatedCode.GetTestMethods(scenario).First();
            testCaseAttributes = testMethod.GetMethodAttributes("NUnit.Framework.TestCaseAttribute");
            tableBody = scenario.GetExamplesTableBody();
        }
    }
}
