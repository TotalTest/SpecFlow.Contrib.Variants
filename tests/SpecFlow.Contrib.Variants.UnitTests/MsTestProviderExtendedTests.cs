using Gherkin.Ast;
using SpecFlow.Contrib.Variants.Generator;
using SpecFlow.Contrib.Variants.Providers;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Parser;
using Xunit;

namespace SpecFlow.Contrib.Variants.UnitTests
{
    public class MsTestProviderExtendedTests : TestBase
    {
        #region Scenario tags tests
        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsAndExamples)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData)]
        public void MsTestProviderExtended_ScenarioVariants_CorrectNumberOfMethodsGenerated(string scenarioName)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);
            var scenario = document.GetScenario<Scenario>(scenarioName);

            var expectedNumOfMethods = ExpectedNumOfMethodsForFeatureVariants(scenario);
            var actualNumOfMethods = generatedCode.GetTestMethods(scenario).Count;

            Assert.Equal(expectedNumOfMethods, actualNumOfMethods);
        }

        [Fact]
        public void MsTestProviderExtended_ScenarioVariants_SpecflowGeneratedCodeCompiles()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);
            var assemblies = new[] { "BoDi.dll", "System.Core.dll", "TechTalk.SpecFlow.dll", "System.dll", "System.Runtime.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions.dll" };

            var compilerResults = GetCompilerResults(generatedCode, assemblies);

            Assert.Empty(compilerResults.Errors);
        }

        [Fact]
        public void MsTestProviderExtended_ScenarioVariants_BaseTestMethodHasCorrectArguments()
        {
            TestSetupForAttributes(out var generatedCode, out var scenario, out _, out var tableHeaders, out _);
            var baseTestMethod = generatedCode.GetRowTestBaseMethod(scenario);
            var methodParams = baseTestMethod.GetMethodParameters();

            for (var i = 0; i < tableHeaders.Count; i++)
            {
                Assert.Equal(methodParams[i].Name, tableHeaders[i].Value);
            }
        }

        [Fact]
        public void MsTestProviderExtended_ScenarioVariants_TestMethodsHaveCorrectProperties()
        {
            TestSetupForAttributes(out _, out _, out var testMethods, out var tableHeaders, out var tableBody);

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

                // Check second argument is the variant full name
                var variantArg = attArg[1];
                var variantKey = variantArg.Arguments[0].GetArgumentValue() == "Variant";
                var variantValue = variantArg.Arguments[1].GetArgumentValue() == $"{SampleFeatureFile.Variants[variantCounter]}";

                Assert.True(variantKey);
                Assert.True(variantValue);

                // Check third argument is the variant key and value
                var variantNameArg = attArg[2];
                var variantName = variantNameArg.Arguments[0].GetArgumentValue() == "VariantName";
                var variantFullValue = variantNameArg.Arguments[1].GetArgumentValue() == $"{cells[0].Value}_{SampleFeatureFile.Variants[variantCounter]}";

                Assert.True(variantName);
                Assert.True(variantFullValue);

                // Check the end arguments are examples table row cells
                for (var k = 0; k < cells.Count; k++)
                {
                    var exampleArg = attArg[k + 3];
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
            TestSetupForAttributes(out _, out var scenario, out var testMethods, out _, out _);

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
            TestSetupForAttributes(out _, out var scenario, out var testMethods, out _, out var tableBody);

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

        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain, false, false)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags, false)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData, true)]
        public void MsTestProviderExtended_ScenarioVariants_TestMethodHasInjectedVariant(string scenarioName, bool isoutline, bool hasVariants = true)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);
            var scenario = document.GetScenario<Scenario>(scenarioName);

            if (isoutline)
            {
                var baseMethod = generatedCode.GetRowTestBaseMethod(scenario);
                var expectedStatement = $"testRunner.ScenarioContext.Add(\"{SampleFeatureFile.Variant}\", \"{SampleFeatureFile.Variant.ToLowerInvariant()}\");";
                var statement = GetScenarioContextVariantStatement(baseMethod, true, 4);
                Assert.Equal(expectedStatement, statement);

                var rowMethods = generatedCode.GetRowTestMethods(scenario);
                var rowCounter = 0;
                var variantCounter = 0;
                for (var i = 0; i < rowMethods.Count; i++)
                {
                    var name = GetVariantParameterOfRowMethod(rowMethods[i]);
                    Assert.Equal(SampleFeatureFile.Variants[variantCounter], name);

                    rowCounter++;
                    if (i % 2 != 0) variantCounter++;
                }
            }
            else
            {
                var testMethods = generatedCode.GetTestMethods(scenario);
                if (hasVariants)
                {
                    for (var i = 0; i < testMethods.Count; i++)
                    {
                        var expectedStatement = $"testRunner.ScenarioContext.Add(\"{SampleFeatureFile.Variant}\", \"{SampleFeatureFile.Variants[i]}\");";
                        var statement = GetScenarioContextVariantStatement(testMethods[i]);
                        Assert.Equal(expectedStatement, statement);
                    }
                }
                else
                {
                    for (var i = 0; i < testMethods.Count; i++)
                    {
                        Assert.Null(GetScenarioContextVariantStatement(testMethods[i]));
                    }
                }
            }
        }
        #endregion

        #region Feature tags tests
        [Fact]
        public void MsTestProviderExtended_FeatureVariants_CorrectNumberOfMethodsGenerated()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            var generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);

            foreach (var scenario in document.Feature.Children.Cast<Scenario>())
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
            var assemblies = new[] { "BoDi.dll", "System.Core.dll", "TechTalk.SpecFlow.dll", "System.dll", "System.Runtime.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.dll", "Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions.dll" };

            var compilerResults = GetCompilerResults(generatedCode, assemblies);

            Assert.Empty(compilerResults.Errors);
        }

        [Fact]
        public void MsTestProviderExtended_FeatureVariants_BaseTestMethodHasCorrectArguments()
        {
            TestSetupForAttributesFeature(out var generatedCode, out _, out var scenario, out _, out var tableHeaders, out _);

            var baseTestMethod = generatedCode.GetRowTestBaseMethod(scenario);
            var methodParams = baseTestMethod.GetMethodParameters();

            for (var i = 0; i < tableHeaders.Count; i++)
            {
                Assert.Equal(methodParams[i].Name, tableHeaders[i].Value);
            }
        }

        [Fact]
        public void MsTestProviderExtended_FeatureVariants_TestMethodsHaveCorrectProperties()
        {
            TestSetupForAttributesFeature(out _, out _, out _, out var testMethods, out var tableHeaders, out var tableBody);

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

                // Check second argument is the variant full name
                var variantArg = attArg[1];
                var variantKey = variantArg.Arguments[0].GetArgumentValue() == "Variant";
                var variantValue = variantArg.Arguments[1].GetArgumentValue() == $"{SampleFeatureFile.Variants[variantCounter]}";

                Assert.True(variantKey);
                Assert.True(variantValue);

                // Check third argument is the variant key and value
                var variantNameArg = attArg[2];
                var variantName = variantNameArg.Arguments[0].GetArgumentValue() == "VariantName";
                var variantFullValue = variantNameArg.Arguments[1].GetArgumentValue() == $"{cells[0].Value}_{SampleFeatureFile.Variants[variantCounter]}";

                Assert.True(variantName);
                Assert.True(variantFullValue);

                // Check the end arguments are examples table row cells
                for (var k = 0; k < cells.Count; k++)
                {
                    var exampleArg = attArg[k + 3];
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
        public void MsTestProviderExtended_FeatureVariants_TestMethodsHaveCorrectCategories()
        {
            TestSetupForAttributesFeature(out _, out _, out var scenario, out var testMethods, out _, out _);

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
        public void MsTestProviderExtended_FeatureVariants_TestMethodsHaveCorrectDescriptionAndName()
        {
            TestSetupForAttributesFeature(out _, out _, out var scenario, out var testMethods, out _, out var tableBody);

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

        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain, false)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags, false)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData, true)]
        public void MsTestProviderExtended_FeatureVariants_TestMethodHasInjectedVariant(string scenarioName, bool isoutline)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            var generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);
            var scenario = document.GetScenario<Scenario>(scenarioName);

            if (isoutline)
            {
                var baseMethod = generatedCode.GetRowTestBaseMethod(scenario);
                var expectedStatement = $"testRunner.ScenarioContext.Add(\"{SampleFeatureFile.Variant}\", \"{SampleFeatureFile.Variant.ToLowerInvariant()}\");";
                var statement = GetScenarioContextVariantStatement(baseMethod, true);
                Assert.Equal(expectedStatement, statement);

                var rowMethods = generatedCode.GetRowTestMethods(scenario);
                var rowCounter = 0;
                var variantCounter = 0;
                for (var i = 0; i < rowMethods.Count; i++)
                {
                    var name = GetVariantParameterOfRowMethod(rowMethods[i]);
                    Assert.Equal(SampleFeatureFile.Variants[variantCounter], name);

                    rowCounter++;
                    if (i % 2 != 0) variantCounter++;
                }
            }
            else
            {
                var testMethods = generatedCode.GetTestMethods(scenario);
                for (var i = 0; i < testMethods.Count; i++)
                {
                    var expectedStatement = $"testRunner.ScenarioContext.Add(\"{SampleFeatureFile.Variant}\", \"{SampleFeatureFile.Variants[i]}\");";
                    var statement = GetScenarioContextVariantStatement(testMethods[i]);
                    Assert.Equal(expectedStatement, statement);
                }
            }
        }
        #endregion

        #region Negative tests
        [Fact]
        public void MsTestProviderExtended_FeatureAndScenarioVariants_SpecflowGeneratedCodeCompileFails()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureAndScenarioVariantTags);

            Action act = () => SetupFeatureGenerator<MsTestProviderExtended>(document);
            var ex = Assert.Throws<TestGeneratorException>(act);

            Assert.Equal("Variant tags were detected at feature and scenario level, please specify at one level or the other.", ex.Message);
        }
        #endregion

        #region Regression tests
        [Fact]
        public void MsTestProviderExtended_Regression_InlineTablesGeneratedCorrectly()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);
            var scenario = document.GetScenario<ScenarioOutline>(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData);
            var baseMethod = generatedCode.GetRowTestBaseMethod(scenario);
            var tableStep = scenario.Steps.First(a => a.Argument is DataTable).Argument as DataTable;
            var tableRows = tableStep.Rows.ToList();
            var methodStatements = baseMethod.GetMethodStatements().GetTableStatements(tableRows.Count);

            var expectedHeaders = tableRows[0].Cells.Select(a => a.Value);
            var headerStatementArgs = methodStatements[0].GetStepTableHeaderArgs();
            Assert.True(expectedHeaders.SequenceEqual(headerStatementArgs));

            for (var i = 1; i < tableRows.Count; i++)
            {
                var cellValues = tableRows[i].Cells.Select(a => a.Value);
                var cellStatementArgs = methodStatements[i].GetStepTableCellArgs();

                Assert.True(cellValues.SequenceEqual(cellStatementArgs));
            }
        }
        #endregion

        [Fact]
        public void MsTestProviderExtended_Generation_CustomGenerationApplied()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            var generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);

            var document2 = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode2 = SetupFeatureGenerator<MsTestProviderExtended>(document2);

            var customComment = generatedCode.Comments.Cast<CodeCommentStatement>()
                .Count(a => a.Comment.Text == FeatureGeneratorExtended.CustomGeneratedComment);

            var customComment2 = generatedCode2.Comments.Cast<CodeCommentStatement>()
                .Count(a => a.Comment.Text == FeatureGeneratorExtended.CustomGeneratedComment);

            Assert.Equal(1, customComment);
            Assert.Equal(1, customComment2);
        }

        private void TestSetupForAttributes(out CodeNamespace generatedCode, out ScenarioOutline scenario, out IList<CodeTypeMember> testMethods, out IList<TableCell> tableHeaders, out IList<TableRow> tableBody)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);
            scenario = document.GetScenario<ScenarioOutline>(SampleFeatureFile.ScenarioTitle_TagsAndExamples);
            testMethods = generatedCode.GetRowTestMethods(scenario);
            tableHeaders = scenario.GetExamplesTableHeaders();
            tableBody = scenario.GetExamplesTableBody();
        }

        private void TestSetupForAttributesFeature(out CodeNamespace generatedCode, out Feature feature, out ScenarioOutline scenario, out IList<CodeTypeMember> testMethods, out IList<TableCell> tableHeaders, out IList<TableRow> tableBody)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            generatedCode = SetupFeatureGenerator<MsTestProviderExtended>(document);
            scenario = document.GetScenario<ScenarioOutline>(SampleFeatureFile.ScenarioTitle_TagsAndExamples);
            feature = document.Feature;
            testMethods = generatedCode.GetRowTestMethods(scenario);
            tableHeaders = scenario.GetExamplesTableHeaders();
            tableBody = scenario.GetExamplesTableBody();
        }
    }
}
