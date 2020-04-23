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
    public class NUnitProviderExtendedTests : TestBase
    {
        #region Scenario tags tests
        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsAndExamples)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData)]
        public void NUnitProviderExtended_ScenarioVariants_CorrectNumberOfMethodsGenerated(string scenarioName)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(document);
            var scenario = document.GetScenario<Scenario>(scenarioName);

            var expectedNumOfMethods = ExpectedNumOfMethodsForFeatureVariants(scenario);
            var actualNumOfMethods = generatedCode.GetTestMethods(scenario).Count;

            Assert.Equal(expectedNumOfMethods, actualNumOfMethods);
        }

        [Fact]
        public void NUnitProviderExtended_ScenarioVariants_SpecflowGeneratedCodeCompiles()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(document);
            var assemblies = new[] { "BoDi.dll", "System.Core.dll", "TechTalk.SpecFlow.dll", "System.dll", "System.Runtime.dll", "nunit.framework.dll" };

            var compilerResults = GetCompilerResults(generatedCode, assemblies);

            Assert.Empty(compilerResults.Errors);
        }

        [Fact]
        public void NUnitProviderExtended_ScenarioVariants_CorrectNumberOfTestCaseAttributesForRowTests()
        {
            TestSetupForAttributesForRowTests(out var scenario, out _, out var testCaseAttributes, out _);

            var expectedNumOfTestCaseAttributes = scenario.GetTagsByNameStart(SampleFeatureFile.Variant).Count
                * scenario.GetExamplesTableBody().Count;

            Assert.Equal(expectedNumOfTestCaseAttributes, testCaseAttributes.Count);
        }

        [Fact]
        public void NUnitProviderExtended_ScenarioVariants_CorrectNumberOfTestCaseAttributesForNonRowTests()
        {
            TestSetupForAttributesForNonRowTests(out _, out var testMethods);

            var result = testMethods.All(a =>
            {
                var attrs = a.GetMethodAttributes("NUnit.Framework.TestCaseAttribute");
                return attrs.Count == 1;
            });

            Assert.True(result);
        }

        [Fact]
        public void NUnitProviderExtended_ScenarioVariants_TestCaseAttributesHaveCorrectArguments()
        {
            TestSetupForAttributesForRowTests(out _, out _, out var testCaseAttributes, out var tableBody);

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
        public void NUnitProviderExtended_ScenarioVariants_TestCaseAttributesHaveCorrectCategory()
        {
            TestSetupForAttributesForRowTests(out var scenario, out _, out var testCaseAttributes, out var tableBody);

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
        public void NUnitProviderExtended_ScenarioVariants_TestCaseAttributesHaveCorrectTestName()
        {
            TestSetupForAttributesForRowTests(out var scenario, out var testMethod, out var testCaseAttributes, out var tableBody);

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
                    var expTestName = $"{scenario.Name} with {currentVariant} and {string.Join(", ", cells)}";
                    var testNameAttr = attArg[cells.Count + 3];

                    Assert.Equal("TestName", testNameAttr.Name);
                    Assert.Equal(expTestName, testNameAttr.GetArgumentValue().Replace("\"", ""));
                }
            }
        }

        [Fact]
        public void NUnitProviderExtended_ScenarioVariants_TestCaseAttributesHaveCorrectTestNameForNonRowTests()
        {
            TestSetupForAttributesForNonRowTests(out var scenario, out var testMethods);

            for (var j = 0; j < SampleFeatureFile.Variants.Length; j++)
            {
                var testMethod = testMethods[j];
                var testCaseAttributes = testMethod.GetMethodAttributes("NUnit.Framework.TestCaseAttribute");

                var attArg = testCaseAttributes[0].Arguments.GetAttributeArguments();

                // Check forth argument is the category with the correct value
                var currentVariant = SampleFeatureFile.Variants[j];
                var expTestName = $"{scenario.Name}: {currentVariant}";
                var testNameAttr = attArg[0];

                Assert.Equal("TestName", testNameAttr.Name);
                Assert.Equal(expTestName, testNameAttr.GetArgumentValue().Replace("\"", ""));
            }
        }

        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain, false, false)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags, false)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData, true)]
        public void NUnitProviderExtended_ScenarioVariants_TestMethodHasInjectedVariant(string scenarioName, bool isoutline, bool hasVariants = true)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(document);
            var scenario = document.GetScenario<Scenario>(scenarioName);

            if (isoutline)
            {
                var rowMethod = generatedCode.GetRowTestMethods(scenario).First();
                var expectedStatement = $"testRunner.ScenarioContext.Add(\"{SampleFeatureFile.Variant}\", \"{SampleFeatureFile.Variant.ToLowerInvariant()}\");";
                var statement = GetScenarioContextVariantStatement(rowMethod, true, 4);
                Assert.Equal(expectedStatement, statement);
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
        public void NUnitProviderExtended_FeatureVariants_SpecflowGeneratedCodeCompiles()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            var generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(document);
            var assemblies = new[] { "BoDi.dll", "System.Core.dll", "TechTalk.SpecFlow.dll", "System.dll", "System.Runtime.dll", "nunit.framework.dll" };

            var compilerResults = GetCompilerResults(generatedCode, assemblies);

            Assert.Empty(compilerResults.Errors);
        }

        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsAndExamples)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData)]
        public void NUnitProviderExtended_FeatureVariants_CorrectNumberOfMethodsGenerated(string scenarioName)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            var generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(document);
            var scenario = document.GetScenario<Scenario>(scenarioName);

            var expectedNumOfMethods = ExpectedNumOfMethodsForFeatureVariants(scenario, document.Feature);
            var actualNumOfMethods = generatedCode.GetTestMethods(scenario).Count;

            Assert.Equal(expectedNumOfMethods, actualNumOfMethods);
        }

        [Fact]
        public void NUnitProviderExtended_FeautureVariants_CorrectNumberOfTestCaseAttributes()
        {
            TestSetupForAttributesFeature(out var feature, out var scenario, out _, out var testCaseAttributes, out _);

            var expectedNumOfTestCaseAttributes = feature.GetTagsByNameStart(SampleFeatureFile.Variant).Count
                * scenario.GetExamplesTableBody().Count;

            Assert.Equal(expectedNumOfTestCaseAttributes, testCaseAttributes.Count);
        }

        [Fact]
        public void NUnitProviderExtended_FeatureVariants_TestCaseAttributesHaveCorrectArguments()
        {
            TestSetupForAttributesFeature(out _, out _, out _, out var testCaseAttributes, out var tableBody);

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
        public void NUnitProviderExtended_FeatureVariants_TestCaseAttributesHaveCorrectCategory()
        {
            TestSetupForAttributesFeature(out var feature, out var scenario, out _, out var testCaseAttributes, out var tableBody);

            var attributeCounter = 0;
            for (var i = 0; i < tableBody.Count; i++)
            {
                var cells = tableBody[i].Cells.ToList();
                for (var j = 0; j < SampleFeatureFile.Variants.Length; j++)
                {
                    var attArg = testCaseAttributes[attributeCounter].Arguments.GetAttributeArguments();
                    attributeCounter++;

                    // Check forth argument is the category with the correct value
                    var varantTag = feature.GetTagsByNameExact($"{SampleFeatureFile.Variant}:{SampleFeatureFile.Variants[j]}").GetNameWithoutAt();
                    var nonVariantTags = scenario.GetTagsExceptNameStart(SampleFeatureFile.Variant).Select(a => a.GetNameWithoutAt());
                    var expCategoryValue = $"{varantTag},{string.Join(",", nonVariantTags)}";
                    var categoryAttr = attArg[cells.Count + 2];

                    Assert.Equal("Category", categoryAttr.Name);
                    Assert.Equal(expCategoryValue, categoryAttr.GetArgumentValue());
                }
            }
        }

        [Fact]
        public void NUnitProviderExtended_FeatureVariants_TestCaseAttributesHaveCorrectTestName()
        {
            TestSetupForAttributesFeature(out _, out var scenario, out var testMethod, out var testCaseAttributes, out var tableBody);

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
                    var expTestName = $"{scenario.Name} with {currentVariant} and {string.Join(", ", cells)}";
                    var testNameAttr = attArg[cells.Count + 3];

                    Assert.Equal("TestName", testNameAttr.Name);
                    Assert.Equal(expTestName, testNameAttr.GetArgumentValue().Replace("\"", ""));
                }
            }
        }

        [Theory]
        [InlineData(SampleFeatureFile.ScenarioTitle_Plain, false)]
        [InlineData(SampleFeatureFile.ScenarioTitle_Tags, false)]
        [InlineData(SampleFeatureFile.ScenarioTitle_TagsExamplesAndInlineData, true)]
        public void NUnitProviderExtended_FeatureVariants_TestMethodHasInjectedVariant(string scenarioName, bool isoutline)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            var generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(document);
            var scenario = document.GetScenario<Scenario>(scenarioName);

            if (isoutline)
            {
                var rowMethod = generatedCode.GetRowTestMethods(scenario).First();
                var expectedStatement = $"testRunner.ScenarioContext.Add(\"{SampleFeatureFile.Variant}\", \"{SampleFeatureFile.Variant.ToLowerInvariant()}\");";
                var statement = GetScenarioContextVariantStatement(rowMethod, true, 2);
                Assert.Equal(expectedStatement, statement);
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
        public void NUnitProviderExtended_FeatureAndScenarioVariants_SpecflowGeneratedCodeCompileFails()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureAndScenarioVariantTags);

            Action act = () => SetupFeatureGenerator<NUnitProviderExtended>(document);
            var ex = Assert.Throws<TestGeneratorException>(act);

            Assert.Equal("Variant tags were detected at feature and scenario level, please specify at one level or the other.", ex.Message);
        }
        #endregion

        [Fact]
        public void NUnitProviderExtended_Generation_CustomGenerationApplied()
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            var generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(document);

            var document2 = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode2 = SetupFeatureGenerator<NUnitProviderExtended>(document2);

            var customComment = generatedCode.Comments.Cast<CodeCommentStatement>()
                .Count(a => a.Comment.Text == FeatureGeneratorExtended.CustomGeneratedComment);

            var customComment2 = generatedCode2.Comments.Cast<CodeCommentStatement>()
                .Count(a => a.Comment.Text == FeatureGeneratorExtended.CustomGeneratedComment);

            Assert.Equal(1, customComment);
            Assert.Equal(1, customComment2);
        }

        private void TestSetupForAttributesForRowTests(out ScenarioOutline scenario, out CodeTypeMember testMethod, out IList<CodeAttributeDeclaration> testCaseAttributes, out IList<TableRow> tableBody)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(document);
            scenario = document.GetScenario<ScenarioOutline>(SampleFeatureFile.ScenarioTitle_TagsAndExamples);
            testMethod = generatedCode.GetTestMethods(scenario).First();
            testCaseAttributes = testMethod.GetMethodAttributes("NUnit.Framework.TestCaseAttribute");
            tableBody = scenario.GetExamplesTableBody();
        }

        private void TestSetupForAttributesForNonRowTests(out Scenario scenario, out IList<CodeTypeMember> testMethods)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithScenarioVariantTags);
            var generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(document);
            scenario = document.GetScenario<Scenario>(SampleFeatureFile.ScenarioTitle_Tags);
            testMethods = generatedCode.GetTestMethods(scenario);
        }

        private void TestSetupForAttributesFeature(out Feature feature, out ScenarioOutline scenario, out CodeTypeMember testMethod, out IList<CodeAttributeDeclaration> testCaseAttributes, out IList<TableRow> tableBody)
        {
            var document = CreateSpecFlowDocument(SampleFeatureFile.FeatureFileWithFeatureVariantTags);
            var generatedCode = SetupFeatureGenerator<NUnitProviderExtended>(document);
            scenario = document.GetScenario<ScenarioOutline>(SampleFeatureFile.ScenarioTitle_TagsAndExamples);
            feature = document.Feature;
            testMethod = generatedCode.GetTestMethods(scenario).First();
            testCaseAttributes = testMethod.GetMethodAttributes("NUnit.Framework.TestCaseAttribute");
            tableBody = scenario.GetExamplesTableBody();
        }
    }
}
