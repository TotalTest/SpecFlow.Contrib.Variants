using Gherkin.Ast;
using SpecFlow.Variants.SpecFlowPlugin.Generator.ClassGenerator;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Generator.UnitTestConverter;
using TechTalk.SpecFlow.Generator.UnitTestProvider;
using TechTalk.SpecFlow.Parser;
using TechTalk.SpecFlow.Tracing;
using TechTalk.SpecFlow.Utils;

[assembly: InternalsVisibleTo("SpecFlow.Variants.UnitTests")]
namespace SpecFlow.Variants.SpecFlowPlugin.Generator
{
    internal class FeatureGeneratorExtended : TestClassGenerator, IFeatureGenerator
    {
        private readonly IUnitTestGeneratorProvider _testGeneratorProvider;
        private readonly CodeDomHelper _codeDomHelper;
        private readonly SpecFlowConfiguration _specFlowConfiguration;
        private readonly IDecoratorRegistry _decoratorRegistry;
        private int _tableCounter;

        /////NEWCODE\\\\\
        private readonly VariantHelper _variantHelper;
        /////NEWCODE\\\\\

        public FeatureGeneratorExtended(IUnitTestGeneratorProvider testGeneratorProvider, CodeDomHelper codeDomHelper, SpecFlowConfiguration specFlowConfiguration, IDecoratorRegistry decoratorRegistry, string variantKey)
            : base(decoratorRegistry, testGeneratorProvider, codeDomHelper, specFlowConfiguration)
        {
            _testGeneratorProvider = testGeneratorProvider;
            _codeDomHelper = codeDomHelper;
            _specFlowConfiguration = specFlowConfiguration;
            _decoratorRegistry = decoratorRegistry;

            /////NEWCODE\\\\\
            _variantHelper = new VariantHelper(variantKey);
            /////NEWCODE\\\\\
        }

        public CodeNamespace GenerateUnitTestFixture(SpecFlowDocument document, string testClassName, string targetNamespace)
        {
            var specFlowFeature = document.SpecFlowFeature;
            testClassName = testClassName ?? string.Format("{0}Feature", specFlowFeature.Name.ToIdentifier());
            base.CreateNamespace(targetNamespace);
            base.CreateTestClassStructure(testClassName, document);

            base.SetupTestClass();
            base.SetupTestClassInitializeMethod();
            base.SetupTestClassCleanupMethod();
            base.SetupTestInitializeMethod();
            base.SetupTestCleanupMethod();

            SetupScenarioInitializeMethod(base.GenerationContext);
            SetupScenarioStartMethod(base.GenerationContext);
            SetupFeatureBackground(base.GenerationContext);
            SetupScenarioCleanupMethod(base.GenerationContext);


            ////NEWCODE\\\\
            var variantTags = _variantHelper.GetFeatureVariantTagValues(specFlowFeature);

            if (_variantHelper.AnyScenarioHasVariantTag(specFlowFeature) && _variantHelper.FeatureHasVariantTags)
                throw new TestGeneratorException("Variant tags were detected at feature and scenario level, please specify at one level or the other.");
            
            ////NEWCODE\\\\

            foreach (var scenarioDefinition in specFlowFeature.ScenarioDefinitions)
            {
                if (string.IsNullOrEmpty(scenarioDefinition.Name))
                    throw new TestGeneratorException("The scenario must have a title specified.");

                if (scenarioDefinition is ScenarioOutline scenarioOutline)
                {
                    /////NEWCODE\\\\\
                    variantTags = _variantHelper.FeatureHasVariantTags ? variantTags : _variantHelper.GetScenarioVariantTagValues(scenarioDefinition);
                    GenerateScenarioOutlineTest(base.GenerationContext, scenarioOutline, variantTags);
                    /////NEWCODE\\\\\
                }
                else
                {
                    /////NEWCODE\\\\\
                    variantTags = _variantHelper.FeatureHasVariantTags ? variantTags : _variantHelper.GetScenarioVariantTagValues(scenarioDefinition);
                    if (variantTags.Count > 0) { variantTags.ForEach(a => GenerateTest(base.GenerationContext, (Scenario)scenarioDefinition, a)); }
                    else { GenerateTest(base.GenerationContext, (Scenario)scenarioDefinition, null); }
                    /////NEWCODE\\\\\
                }
            }
            _testGeneratorProvider.FinalizeTestClass(base.GenerationContext);
            return base.CodeNamespace;
        }

        private void SetupScenarioCleanupMethod(TestClassGenerationContext generationContext)
        {
            var scenarioCleanupMethod = generationContext.ScenarioCleanupMethod;
            scenarioCleanupMethod.Attributes = MemberAttributes.Public;
            scenarioCleanupMethod.Name = "ScenarioCleanup";
            var runnerExpression = base.GetTestRunnerExpression("testRunner");
            scenarioCleanupMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "CollectScenarioErrors", new CodeExpression[0]));
        }

        private void SetupScenarioStartMethod(TestClassGenerationContext generationContext)
        {
            var scenarioStartMethod = generationContext.ScenarioStartMethod;
            scenarioStartMethod.Attributes = MemberAttributes.Public;
            scenarioStartMethod.Name = "ScenarioStart";
            var runnerExpression = base.GetTestRunnerExpression("testRunner");
            scenarioStartMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "OnScenarioStart", new CodeExpression[0]));
        }

        private void SetupFeatureBackground(TestClassGenerationContext generationContext)
        {
            if (!generationContext.Feature.HasFeatureBackground())
                return;
            var backgroundMethod = generationContext.FeatureBackgroundMethod;
            backgroundMethod.Attributes = MemberAttributes.Public;
            backgroundMethod.Name = "FeatureBackground";
            var background = generationContext.Feature.Background;
            _codeDomHelper.AddLineDirective(background, backgroundMethod.Statements, _specFlowConfiguration);
            foreach (var step in background.Steps)
                GenerateStep(backgroundMethod, step, null);
            _codeDomHelper.AddLineDirectiveHidden(backgroundMethod.Statements, _specFlowConfiguration);
        }

        private void SetupScenarioInitializeMethod(TestClassGenerationContext generationContext)
        {
            var initializeMethod = generationContext.ScenarioInitializeMethod;
            initializeMethod.Attributes = MemberAttributes.Public;
            initializeMethod.Name = "ScenarioInitialize";
            initializeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ScenarioInfo), "scenarioInfo"));
            var runnerExpression = base.GetTestRunnerExpression("testRunner");
            initializeMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "OnScenarioInitialize", new CodeExpression[1]
            {
                new CodeVariableReferenceExpression("scenarioInfo")
            }));
        }

        private void GenerateScenarioOutlineTest(TestClassGenerationContext generationContext, ScenarioOutline scenarioOutline, List<string> variantTags = null)
        {
            scenarioOutline.ValidateExampleSetConsistency();
            var identifierMapping = scenarioOutline.CreateParamToIdentifierMapping();
            var outlineTestMethod = CreateScenarioOutlineTestMethod(generationContext, scenarioOutline, identifierMapping);

            /////NEWCODE\\\\
            if (generationContext.GenerateRowTests)
            {
                if (variantTags?.Count > 0)
                    GenerateScenarioOutlineExamplesAsRowTests(generationContext, scenarioOutline, outlineTestMethod, variantTags);
                else
                    GenerateScenarioOutlineExamplesAsRowTests(generationContext, scenarioOutline, outlineTestMethod, null);
            }
            else
            {
                if (variantTags?.Count > 0)
                    variantTags.ForEach(a => GenerateScenarioOutlineExamplesAsIndividualMethods(scenarioOutline, generationContext, outlineTestMethod, identifierMapping, a));
                else
                    GenerateScenarioOutlineExamplesAsIndividualMethods(scenarioOutline, generationContext, outlineTestMethod, identifierMapping, null);
            }
            /////NEWCODE\\\\    

            var referenceExpression = new CodeVariableReferenceExpression("exampleTags");
            GenerateTestBody(generationContext, scenarioOutline, outlineTestMethod, referenceExpression, identifierMapping);
        }

        private void GenerateScenarioOutlineExamplesAsIndividualMethods(ScenarioOutline scenarioOutline, TestClassGenerationContext generationContext, CodeMemberMethod scenatioOutlineTestMethod, ParameterSubstitution paramToIdentifier, string tag = null)
        {
            int num = 0;
            foreach (var example in scenarioOutline.Examples)
            {
                var flag = example.TableBody.CanUseFirstColumnAsName();
                string str;
                if (!string.IsNullOrEmpty(example.Name))
                {
                    str = example.Name.ToIdentifier();
                }
                else
                {
                    var examples = scenarioOutline.Examples;
                    bool func(Examples es) => string.IsNullOrEmpty(es.Name);
                    str = examples.Count(func) > 1 ? string.Format("ExampleSet {0}", num).ToIdentifier() : null;
                }

                foreach (var data in example.TableBody.Select((r, i) => new
                {
                    Row = r,
                    Index = i
                }))
                {
                    var variantName = flag ? data.Row.Cells.First().Value : string.Format("Variant {0}", data.Index);
                    GenerateScenarioOutlineTestVariant(generationContext, scenarioOutline, scenatioOutlineTestMethod, paramToIdentifier, example.Name ?? "", str, data.Row, example.Tags, variantName, tag);
                }
                num++;
            }
        }

        private void GenerateScenarioOutlineExamplesAsRowTests(TestClassGenerationContext generationContext, ScenarioOutline scenarioOutline, CodeMemberMethod scenatioOutlineTestMethod, List<string> variantTags = null)
        {
            SetupTestMethod(generationContext, scenatioOutlineTestMethod, scenarioOutline, null, null, null, true);
            foreach (var example in scenarioOutline.Examples)
            {
                /////NEWCODE\\\\\
                var hasVariantTags = variantTags?.Count > 0;

                if (hasVariantTags)
                {
                    scenatioOutlineTestMethod.Parameters.RemoveAt(scenatioOutlineTestMethod.Parameters.Count - 1);
                    scenatioOutlineTestMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), _variantHelper.VariantKey.ToLowerInvariant()));
                    scenatioOutlineTestMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string[]), "exampleTags"));
                }

                foreach (var tableRow in example.TableBody)
                {
                    if (hasVariantTags)
                    {
                        foreach (var variant in variantTags)
                        {
                            var arguments = tableRow.Cells.Select(c => c.Value).ToList();
                            arguments.Add($"{_variantHelper.VariantKey}:{variant}");
                            _testGeneratorProvider.SetRow(generationContext, scenatioOutlineTestMethod, arguments, example.Tags.GetTagsExcept("@Ignore"), example.Tags.HasTag("@Ignore"));
                        }
                    }
                    else
                    {
                        var arguments = tableRow.Cells.Select(c => c.Value).ToList();
                        _testGeneratorProvider.SetRow(generationContext, scenatioOutlineTestMethod, arguments, example.Tags.GetTagsExcept("@Ignore"), example.Tags.HasTag("@Ignore"));
                    }
                    /////NEWCODE\\\\\
                }
            }
        }

        private CodeMemberMethod CreateScenarioOutlineTestMethod(TestClassGenerationContext generationContext, ScenarioOutline scenarioOutline, ParameterSubstitution paramToIdentifier)
        {
            var method = generationContext.TestClass.CreateMethod();
            method.Attributes = MemberAttributes.Public;
            method.Name = string.Format("{0}", scenarioOutline.Name.ToIdentifier());
            foreach (var keyValuePair in paramToIdentifier)
                method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), keyValuePair.Value));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string[]), "exampleTags"));
            return method;
        }

        private void GenerateScenarioOutlineTestVariant(TestClassGenerationContext generationContext, ScenarioOutline scenarioOutline, CodeMemberMethod scenatioOutlineTestMethod, IEnumerable<KeyValuePair<string, string>> paramToIdentifier, string exampleSetTitle, string exampleSetIdentifier, Gherkin.Ast.TableRow row, IEnumerable<Tag> exampleSetTags, string variantName, string tag = null)
        {
            variantName = string.IsNullOrEmpty(tag) ? variantName : $"{variantName}_{tag}";
            var testMethod = CreateTestMethod(generationContext, scenarioOutline, exampleSetTags, variantName, exampleSetIdentifier);
            _codeDomHelper.AddLineDirective(scenarioOutline, testMethod.Statements, _specFlowConfiguration);
            var list1 = new List<CodeExpression>();
            list1.AddRange(row.Cells.Select(paramCell => new CodePrimitiveExpression(paramCell.Value)).Cast<CodeExpression>().ToList());
            list1.Add(exampleSetTags.GetStringArrayExpression());
            testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), scenatioOutlineTestMethod.Name, list1.ToArray()));
            _codeDomHelper.AddLineDirectiveHidden(testMethod.Statements, _specFlowConfiguration);
            var list2 = paramToIdentifier.Select((p2i, paramIndex) => new KeyValuePair<string, string>(p2i.Key, row.Cells.ElementAt(paramIndex).Value)).ToList();
            _testGeneratorProvider.SetTestMethodAsRow(generationContext, testMethod, scenarioOutline.Name, exampleSetTitle, variantName, list2);
        }

        private CodeMemberMethod CreateTestMethod(TestClassGenerationContext generationContext, ScenarioDefinition scenario, IEnumerable<Tag> additionalTags, string variantName = null, string exampleSetIdentifier = null)
        {
            var method = generationContext.TestClass.CreateMethod();
            SetupTestMethod(generationContext, method, scenario, additionalTags, variantName, exampleSetIdentifier, false);
            return method;
        }

        private void GenerateTest(TestClassGenerationContext generationContext, Scenario scenario, string tag = null)
        {
            /////NEWCODE\\\\
            var variantName = string.IsNullOrEmpty(tag) ? null : $"_{tag}";
            /////NEWCODE\\\\\

            var testMethod = CreateTestMethod(generationContext, scenario, null, variantName, null);
            GenerateTestBody(generationContext, scenario, testMethod, null, null);
        }

        private void GenerateTestBody(TestClassGenerationContext generationContext, ScenarioDefinition scenario, CodeMemberMethod testMethod, CodeExpression additionalTagsExpression = null, ParameterSubstitution paramToIdentifier = null)
        {
            CodeExpression left;
            if (additionalTagsExpression == null)
                left = scenario.GetTags().GetStringArrayExpression();
            else if (!scenario.HasTags())
            {
                left = additionalTagsExpression;
            }
            else
            {
                testMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "__tags", scenario.GetTags().GetStringArrayExpression()));
                left = new CodeVariableReferenceExpression("__tags");
                testMethod.Statements.Add(new CodeConditionStatement(new CodeBinaryOperatorExpression(additionalTagsExpression, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null)), new CodeStatement[1]
                {
                    new CodeAssignStatement(left, new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof (Enumerable)), "ToArray", new CodeExpression[1]
                    {
                      new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof (Enumerable)), "Concat", new CodeExpression[2]
                      {
                        left,
                        additionalTagsExpression
                      })
                    }))
                }));
            }
            testMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(ScenarioInfo), "scenarioInfo", new CodeObjectCreateExpression(typeof(ScenarioInfo), new CodeExpression[3]
            {
                new CodePrimitiveExpression(scenario.Name),
                new CodePrimitiveExpression(scenario.Description),
                left
            })));
            _codeDomHelper.AddLineDirective(scenario, testMethod.Statements, _specFlowConfiguration);
            testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), generationContext.ScenarioInitializeMethod.Name, new CodeExpression[1]
            {
                new CodeVariableReferenceExpression("scenarioInfo")
            }));
            testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), generationContext.ScenarioStartMethod.Name, new CodeExpression[0]));
            if (generationContext.Feature.HasFeatureBackground())
            {
                _codeDomHelper.AddLineDirective(generationContext.Feature.Background, testMethod.Statements, _specFlowConfiguration);
                testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), generationContext.FeatureBackgroundMethod.Name, new CodeExpression[0]));
            }
            foreach (var step in scenario.Steps)
                GenerateStep(testMethod, step, paramToIdentifier);
            _codeDomHelper.AddLineDirectiveHidden(testMethod.Statements, _specFlowConfiguration);
            testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), generationContext.ScenarioCleanupMethod.Name, new CodeExpression[0]));
        }

        private void SetupTestMethod(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, ScenarioDefinition scenarioDefinition, IEnumerable<Tag> additionalTags, string variantName, string exampleSetIdentifier, bool rowTest = false)
        {
            testMethod.Attributes = MemberAttributes.Public;
            testMethod.Name = GetTestMethodName(scenarioDefinition, variantName, exampleSetIdentifier);
            var str = scenarioDefinition.Name;
            if (variantName != null)
            {
                /////NEWCODE\\\\\
                if (variantName.IndexOf("_") == 0) { variantName = variantName.Remove(0, 1); }
                /////NEWCODE\\\\\
                str = string.Format("{0}: {1}", scenarioDefinition.Name, variantName);
            }
            if (rowTest)
                _testGeneratorProvider.SetRowTest(generationContext, testMethod, str);
            else
                _testGeneratorProvider.SetTestMethod(generationContext, testMethod, str);
            _decoratorRegistry.DecorateTestMethod(generationContext, testMethod, scenarioDefinition.GetTags().ConcatTags(additionalTags), out List<string> unprocessedTags);
            if (!unprocessedTags.Any())
                return;
            _testGeneratorProvider.SetTestMethodCategories(generationContext, testMethod, unprocessedTags);
        }

        private void GenerateStep(CodeMemberMethod testMethod, Step gherkinStep, ParameterSubstitution paramToIdentifier)
        {
            var specFlowStep = gherkinStep.AsSpecFlowStep();
            var codeExpressionList = new List<CodeExpression> { paramToIdentifier.GetSubstitutedString(specFlowStep.Text) };
            if (specFlowStep.Argument != null)
                _codeDomHelper.AddLineDirectiveHidden(testMethod.Statements, _specFlowConfiguration);
            codeExpressionList.Add(paramToIdentifier.GetSubstitutedString((specFlowStep.Argument as DocString)?.Content));
            codeExpressionList.Add(GetTableArgExpression(specFlowStep.Argument as DataTable, testMethod.Statements, paramToIdentifier));
            codeExpressionList.Add(new CodePrimitiveExpression(specFlowStep.Keyword));
            _codeDomHelper.AddLineDirective(specFlowStep, testMethod.Statements, _specFlowConfiguration);
            var runnerExpression = base.GetTestRunnerExpression("testRunner");
            testMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, specFlowStep.StepKeyword.ToString(), codeExpressionList.ToArray()));
        }

        private string GetTestMethodName(ScenarioDefinition scenario, string variantName, string exampleSetIdentifier)
        {
            var str1 = string.Format("{0}", scenario.Name.ToIdentifier());
            if (variantName != null)
            {
                var str2 = variantName.ToIdentifier().TrimStart('_');
                str1 = string.IsNullOrEmpty(exampleSetIdentifier) ? string.Format("{0}_{1}", str1, str2) : string.Format("{0}_{1}_{2}", str1, exampleSetIdentifier, str2);
            }
            return str1;
        }

        private CodeExpression GetTableArgExpression(DataTable tableArg, CodeStatementCollection statements, ParameterSubstitution paramToIdentifier)
        {
            if (tableArg == null)
                return new CodeCastExpression(typeof(Table), new CodePrimitiveExpression(null));
            _tableCounter++;
            var tableRow1 = tableArg.Rows.First();
            var array = tableArg.Rows.Skip(1).ToArray();
            var referenceExpression = new CodeVariableReferenceExpression("table" + _tableCounter);
            statements.Add(new CodeVariableDeclarationStatement(typeof(Table), referenceExpression.VariableName, new CodeObjectCreateExpression(typeof(Table), new CodeExpression[1]
            {
                tableRow1.Cells.Select(c => c.Value).GetStringArrayExpression(paramToIdentifier)
            })));
            foreach (Gherkin.Ast.TableRow tableRow2 in array)
            {
                statements.Add(new CodeMethodInvokeExpression(referenceExpression, "AddRow", new CodeExpression[1]
                {
                    tableRow1.Cells.Select(c => c.Value).GetStringArrayExpression(paramToIdentifier)
                }));
            }
            return referenceExpression;
        }
    }
}