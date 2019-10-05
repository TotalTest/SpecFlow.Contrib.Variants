using Gherkin.Ast;
using SpecFlow.Variants.SpecFlowPlugin.Providers;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow.Generator;
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
            _document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            _generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(_document);
        }

        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsAndExamples)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData)]
        public void MsTestProviderExtended_ScenarioVariants_CorrectNumberOfMethodsGenerated(string scenarioName)
        {
            var scenario = _document.GetScenario<ScenarioDefinition>(scenarioName);
            var expectedNumOfMethods = ExpectedNumOfMethodsForFeatureVariants(scenario);
            var actualNumOfMethods = _generatedCode.GetTestMethods(scenario).Count;

            Assert.Equal(expectedNumOfMethods, actualNumOfMethods);
        }

        [Fact]
        public void MsTestProviderExtended_ScenarioVariants_SpecflowGeneratedCodeCompiles()
        {
            var assemblies = new[] { "System.Core.dll", "TechTalk.SpecFlow.dll", "System.dll", "System.Runtime.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions.dll" };
            var compilerResults = GetCompilerResults(_generatedCode, assemblies);

            Assert.Empty(compilerResults.Errors);
        }

        [Fact]
        public void MsTestProviderExtended_ScenarioVariants_BaseTestMethodHasCorrectArguments()
        {
            TestSetupForAttributes(out var scenario, out _, out var tableHeaders, out _);
            var baseTestMethod = _generatedCode.GetRowTestBaseMethod(scenario);
            var methodParams = baseTestMethod.GetMethodParameters();

            for (var i = 0; i < tableHeaders.Count; i++)
            {
                Assert.Equal(methodParams[i].Name, tableHeaders[i].Value);
            }
        }

        [Fact]
        public void MsTestProviderExtended_ScenarioVariants_TestMethodsHaveCorrectProperties()
        {
            TestSetupForAttributes(out _, out var testMethods, out var tableHeaders, out var tableBody);

            var rowCounter = 0;
            var variantCounter = 0;

            for (var i = 0; i < testMethods.Count; i++)
            {
                var attArg = testMethods[i].GetMethodAttributes("Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute").ToList();

                // Check first argument is the feature title
                var featureTitleArg = attArg[0];
                var argName = featureTitleArg.Arguments[0].GetArgumentValue() == "FeatureTitle";
                var argValue = featureTitleArg.Arguments[1].GetArgumentValue() == SampleFeatureFile.FeatureTitle;

                Assert.True(argName);
                Assert.True(argValue);

                var cells = tableBody[rowCounter].Cells.ToList();

                // Check second argument is the variant
                var variantArg = attArg[1];
                var variantName = variantArg.Arguments[0].GetArgumentValue() == "VariantName";
                var variantValue = variantArg.Arguments[1].GetArgumentValue() == $"{cells[0].Value}_{SampleFeatureFile.Variants[variantCounter]}";

                Assert.True(variantName);
                Assert.True(variantValue);

                // Check the end arguments are examples table row cells
                for (var k = 0; k < cells.Count; k++)
                {
                    var exampleArg = attArg[k + 2];
                    var exampleName = exampleArg.Arguments[0].GetArgumentValue() == $"Parameter:{tableHeaders[k].Value}";
                    var exampleValue = exampleArg.Arguments[1].GetArgumentValue() == $"{cells[k].Value}";

                    Assert.True(exampleName);
                    Assert.True(exampleValue);
                }

                rowCounter++;
                if (i % 2 != 0) variantCounter++;
                if (rowCounter == tableBody.Count) rowCounter = 0;
            }
        }

        [Fact]
        public void MsTestProviderExtended_ScenarioVariants_TestMethodsHaveCorrectCategories()
        {
            TestSetupForAttributes(out var scenario, out var testMethods, out _, out _);

            var variantCounter = 0;
            for (var i = 0; i < testMethods.Count; i++)
            {
                var attArg = testMethods[i].GetMethodAttributes("Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute").ToList();

                // Check first argument is the variant
                var variantMatches = attArg[0].Arguments[0].GetArgumentValue() == $"{SampleFeatureFile.Variant}:{SampleFeatureFile.Variants[variantCounter]}";
                Assert.True(variantMatches);

                // Check rest of the categories are non variant tags
                var nonVariantTags = scenario.GetTagsExceptNameStart(SampleFeatureFile.Variant).Select(a => a.GetNameWithoutAt()).ToList();
                var categoryAttr = attArg.SelectMany(a => a.Arguments.GetAttributeArguments().Select(b => b.GetArgumentValue())).ToList();

                Assert.True(!nonVariantTags.Except(categoryAttr).Any());

                if (i % 2 != 0) variantCounter++;
            }
        }

        [Fact]
        public void MsTestProviderExtended_ScenarioVariants_TestMethodsHaveCorrectDescriptionAndName()
        {
            TestSetupForAttributes(out var scenario, out var testMethods, out _, out var tableBody);

            var rowCounter = 0;
            var variantCounter = 0;

            for (var i = 0; i < testMethods.Count; i++)
            {
                var cells = tableBody[rowCounter].Cells.ToList();
                var attArg = testMethods[i].GetMethodAttributes("Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute").First();

                // Check description attribute value is correct
                var expectedDescription = $"{scenario.Name}: {cells[0].Value}_{SampleFeatureFile.Variants[variantCounter]}";
                var actualDescription = attArg.Arguments[0].GetArgumentValue();

                // Check test method name is correct
                var expectedMethodName = $"{scenario.Name.Replace(" ", "").Replace(",", "")}_{cells[0].Value}_{SampleFeatureFile.Variants[variantCounter]}";
                var actualMethodName = testMethods[i].Name;

                Assert.Equal(expectedDescription, actualDescription);
                Assert.Equal(expectedMethodName, actualMethodName, StringComparer.InvariantCultureIgnoreCase);

                rowCounter++;
                if (i % 2 != 0) variantCounter++;
                if (rowCounter == tableBody.Count) rowCounter = 0;
            }
        }

        [Fact]
        public void MsTestProviderExtended_FeatureVariants_CorrectNumberOfMethodsGenerated()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            var generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);

            foreach (var scenario in document.Feature.Children)
            {
                var expectedNumOfMethods = ExpectedNumOfMethodsForFeatureVariants(document.Feature, scenario);
                var actualNumOfMethods = generatedCode.GetTestMethods(scenario).Count;
                Assert.Equal(expectedNumOfMethods, actualNumOfMethods);
            }
        }

        [Fact]
        public void MsTestProviderExtended_FeatureVariants_SpecflowGeneratedCodeCompiles()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            var generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);

            var assemblies = new[] { "System.Core.dll", "TechTalk.SpecFlow.dll", "System.dll", "System.Runtime.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions.dll" };
            var compilerResults = GetCompilerResults(generatedCode, assemblies);

            Assert.Empty(compilerResults.Errors);
        }

        [Fact]
        public void MsTestProviderExtended_FeatureAndScenarioVariants_SpecflowGeneratedCodeCompileFails()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureAndScenarioVariantTags);

            Action act = () => SetupFeatureGenerator<MsTestProviderExtended>(document);
            var ex = Assert.Throws<TestGeneratorException>(act);

            Assert.Equal("Variant tags were detected at feature and scenario level, please specify at one level or the other.", ex.Message);
        }

        private void TestSetupForAttributes(out ScenarioOutline scenario, out IList<CodeTypeMember> testMethods, out IList<TableCell> tableHeaders, out IList<TableRow> tableBody)
        {
            scenario = _document.GetScenario<ScenarioOutline>(SampleFeatureFile.ScenarioTitle_TagsAndExamples);
            testMethods = _generatedCode.GetRowTestMethods(scenario);
            tableHeaders = scenario.GetExamplesTableHeaders();
            tableBody = scenario.GetExamplesTableBody();
        }
    }
}
