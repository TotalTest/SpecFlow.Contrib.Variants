using Gherkin.Ast;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Generator.UnitTestConverter;
using TechTalk.SpecFlow.Generator.UnitTestProvider;
using TechTalk.SpecFlow.Parser;
using TechTalk.SpecFlow.Tracing;
using TechTalk.SpecFlow.Utils;

namespace SpecFlow.Variants.SpecFlowPlugin.Generator
{
    public class FeatureGeneratorExtended : IFeatureGenerator
    {
        private readonly IUnitTestGeneratorProvider _testGeneratorProvider;
        private readonly CodeDomHelper _codeDomHelper;
        private readonly SpecFlowConfiguration _specFlowConfiguration;
        private readonly IDecoratorRegistry _decoratorRegistry;
        private int _tableCounter;

        /////NEWCODE\\\\\
        private readonly string _variantKey;
        /////NEWCODE\\\\\

        public FeatureGeneratorExtended(IUnitTestGeneratorProvider testGeneratorProvider, CodeDomHelper codeDomHelper, SpecFlowConfiguration specFlowConfiguration, IDecoratorRegistry decoratorRegistry, string variantKey)
        {
            _testGeneratorProvider = testGeneratorProvider;
            _codeDomHelper = codeDomHelper;
            _specFlowConfiguration = specFlowConfiguration;
            _decoratorRegistry = decoratorRegistry;

            /////NEWCODE\\\\\
            _variantKey = variantKey;
            /////NEWCODE\\\\\
        }

        private CodeMemberMethod CreateMethod(CodeTypeDeclaration type)
        {
            var codeMemberMethod = new CodeMemberMethod();
            type.Members.Add(codeMemberMethod);
            return codeMemberMethod;
        }

        private static bool HasFeatureBackground(SpecFlowFeature feature)
        {
            return feature.Background != null;
        }

        private TestClassGenerationContext CreateTestClassStructure(CodeNamespace codeNamespace, string testClassName, SpecFlowDocument document)
        {
            var generatedTypeDeclaration = _codeDomHelper.CreateGeneratedTypeDeclaration(testClassName);
            codeNamespace.Types.Add(generatedTypeDeclaration);
            return new TestClassGenerationContext(_testGeneratorProvider, document, codeNamespace, generatedTypeDeclaration, DeclareTestRunnerMember(generatedTypeDeclaration), CreateMethod(generatedTypeDeclaration), CreateMethod(generatedTypeDeclaration), CreateMethod(generatedTypeDeclaration), CreateMethod(generatedTypeDeclaration), CreateMethod(generatedTypeDeclaration), CreateMethod(generatedTypeDeclaration), CreateMethod(generatedTypeDeclaration), HasFeatureBackground(document.SpecFlowFeature) ? CreateMethod(generatedTypeDeclaration) : (CodeMemberMethod)null, _testGeneratorProvider.GetTraits().HasFlag((Enum)UnitTestGeneratorTraits.RowTests) && _specFlowConfiguration.AllowRowTests);
        }

        private CodeNamespace CreateNamespace(string targetNamespace)
        {
            targetNamespace = targetNamespace ?? "SpecFlowTests";
            if (!targetNamespace.StartsWith("global", StringComparison.CurrentCultureIgnoreCase) && _codeDomHelper.TargetLanguage == CodeDomProviderLanguage.VB)
                targetNamespace = string.Format("GlobalVBNetNamespace.{0}", targetNamespace);
            return new CodeNamespace(targetNamespace)
            {
                Imports = { new CodeNamespaceImport("TechTalk.SpecFlow") }
            };
        }

        public CodeNamespace GenerateUnitTestFixture(SpecFlowDocument document, string testClassName, string targetNamespace)
        {
            var specFlowFeature = document.SpecFlowFeature;
            testClassName = testClassName ?? string.Format("{0}Feature", specFlowFeature.Name.ToIdentifier());
            var codeNamespace = CreateNamespace(targetNamespace);
            var testClassStructure = CreateTestClassStructure(codeNamespace, testClassName, document);
            SetupTestClass(testClassStructure);
            SetupTestClassInitializeMethod(testClassStructure);
            SetupTestClassCleanupMethod(testClassStructure);
            SetupScenarioInitializeMethod(testClassStructure);
            SetupScenarioStartMethod(testClassStructure);
            SetupFeatureBackground(testClassStructure);
            SetupScenarioCleanupMethod(testClassStructure);
            SetupTestInitializeMethod(testClassStructure);
            SetupTestCleanupMethod(testClassStructure);
            foreach (var scenarioDefinition in specFlowFeature.ScenarioDefinitions)
            {
                if (string.IsNullOrEmpty(scenarioDefinition.Name))
                    throw new TestGeneratorException("The scenario must have a title specified.");

                /////NEWCODE\\\\\
                var variantTags = new List<string>();
                /////NEWCODE\\\\\

                if (scenarioDefinition is ScenarioOutline scenarioOutline)
                {
                    /////NEWCODE\\\\\
                    scenarioOutline.Tags?.Where(a => a.Name.StartsWith($"@{_variantKey}")).ToList().ForEach(a => variantTags.Add(a.Name.Split(':')[1]));
                    GenerateScenarioOutlineTest(testClassStructure, scenarioOutline, variantTags);
                    /////NEWCODE\\\\\
                }
                else
                {
                    /////NEWCODE\\\\\
                    ((Scenario)scenarioDefinition).Tags?.Where(a => a.Name.StartsWith($"@{_variantKey}")).ToList().ForEach(a => variantTags.Add(a.Name.Split(':')[1]));
                    if (variantTags.Count > 0) { variantTags.ForEach(a => GenerateTest(testClassStructure, (Scenario)scenarioDefinition, a)); }
                    else { GenerateTest(testClassStructure, (Scenario)scenarioDefinition, null); }
                    /////NEWCODE\\\\\
                }
            }
            _testGeneratorProvider.FinalizeTestClass(testClassStructure);
            return codeNamespace;
        }

        private void SetupScenarioCleanupMethod(TestClassGenerationContext generationContext)
        {
            var scenarioCleanupMethod = generationContext.ScenarioCleanupMethod;
            scenarioCleanupMethod.Attributes = (MemberAttributes)24576;
            scenarioCleanupMethod.Name = "ScenarioCleanup";
            var runnerExpression = GetTestRunnerExpression();
            scenarioCleanupMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "CollectScenarioErrors", new CodeExpression[0]));
        }

        private void SetupTestClass(TestClassGenerationContext generationContext)
        {
            generationContext.TestClass.IsPartial = true;
            generationContext.TestClass.TypeAttributes |= TypeAttributes.Public;
            AddLinePragmaInitial(generationContext.TestClass, generationContext.Document.SourceFilePath);
            _testGeneratorProvider.SetTestClass(generationContext, generationContext.Feature.Name, generationContext.Feature.Description);
            _decoratorRegistry.DecorateTestClass(generationContext, out List<string> unprocessedTags);
            if (!unprocessedTags.Any())
                return;
            _testGeneratorProvider.SetTestClassCategories(generationContext, unprocessedTags);
        }

        private CodeMemberField DeclareTestRunnerMember(CodeTypeDeclaration type)
        {
            var codeMemberField = new CodeMemberField(typeof(ITestRunner), "testRunner");
            type.Members.Add(codeMemberField);
            return codeMemberField;
        }

        private CodeExpression GetTestRunnerExpression()
        {
            return new CodeVariableReferenceExpression("testRunner");
        }

        private IEnumerable<string> GetNonIgnoreTags(IEnumerable<Tag> tags)
        {
            return tags.Where(t => !t.Name.Equals("@Ignore", StringComparison.InvariantCultureIgnoreCase)).Select(t => t.GetNameWithoutAt());
        }

        private bool HasIgnoreTag(IEnumerable<Tag> tags)
        {
            return tags.Any(t => t.Name.Equals("@Ignore", StringComparison.InvariantCultureIgnoreCase));
        }

        private void SetupTestClassInitializeMethod(TestClassGenerationContext generationContext)
        {
            var initializeMethod = generationContext.TestClassInitializeMethod;
            initializeMethod.Attributes = MemberAttributes.Public;
            initializeMethod.Name = "FeatureSetup";
            _testGeneratorProvider.SetTestClassInitializeMethod(generationContext);
            CodeExpression[] codeExpressionArray1;
            if (!_testGeneratorProvider.GetTraits().HasFlag(UnitTestGeneratorTraits.ParallelExecution))
                codeExpressionArray1 = (new CodePrimitiveExpression[2]
                {
                    new CodePrimitiveExpression( null),
                    new CodePrimitiveExpression( 0)
                });
            else
                codeExpressionArray1 = new CodeExpression[0];
            var codeExpressionArray2 = codeExpressionArray1;
            CodeExpression runnerExpression = GetTestRunnerExpression();
            initializeMethod.Statements.Add(new CodeAssignStatement(runnerExpression, new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(TestRunnerManager)), "GetTestRunner", codeExpressionArray2)));
            initializeMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(FeatureInfo), "featureInfo", new CodeObjectCreateExpression(typeof(FeatureInfo), new CodeExpression[5]
            {
                new CodeObjectCreateExpression(typeof (CultureInfo), new CodeExpression[1]
                {
                     new CodePrimitiveExpression( generationContext.Feature.Language)
                }),
                new CodePrimitiveExpression(generationContext.Feature.Name),
                new CodePrimitiveExpression(generationContext.Feature.Description),
                new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("ProgrammingLanguage"), _codeDomHelper.TargetLanguage.ToString()),
                GetStringArrayExpression(generationContext.Feature.Tags)
            })));
            initializeMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "OnFeatureStart", new CodeExpression[1]
            {
                new CodeVariableReferenceExpression("featureInfo")
            }));
        }

        private CodeExpression GetStringArrayExpression(IEnumerable<Tag> tags)
        {
            if (!tags.Any())
                return new CodeCastExpression(typeof(string[]), new CodePrimitiveExpression((object)null));
            return new CodeArrayCreateExpression(typeof(string[]), tags.Select(tag => new CodePrimitiveExpression(tag.GetNameWithoutAt())).Cast<CodeExpression>().ToArray());
        }

        private CodeExpression GetStringArrayExpression(IEnumerable<string> items, ParameterSubstitution paramToIdentifier)
        {
            return new CodeArrayCreateExpression(typeof(string[]), items.Select(item => GetSubstitutedString(item, paramToIdentifier)).ToArray());
        }

        private void SetupTestClassCleanupMethod(TestClassGenerationContext generationContext)
        {
            var classCleanupMethod = generationContext.TestClassCleanupMethod;
            classCleanupMethod.Attributes = MemberAttributes.Public;
            classCleanupMethod.Name = "FeatureTearDown";
            _testGeneratorProvider.SetTestClassCleanupMethod(generationContext);
            var runnerExpression = GetTestRunnerExpression();
            classCleanupMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "OnFeatureEnd", new CodeExpression[0]));
            classCleanupMethod.Statements.Add(new CodeAssignStatement(runnerExpression, new CodePrimitiveExpression(null)));
        }

        private void SetupTestInitializeMethod(TestClassGenerationContext generationContext)
        {
            var initializeMethod = generationContext.TestInitializeMethod;
            initializeMethod.Attributes = (MemberAttributes)24576;
            initializeMethod.Name = "TestInitialize";
            _testGeneratorProvider.SetTestInitializeMethod(generationContext);
        }

        private void SetupTestCleanupMethod(TestClassGenerationContext generationContext)
        {
            var testCleanupMethod = generationContext.TestCleanupMethod;
            testCleanupMethod.Attributes = (MemberAttributes)24576;
            testCleanupMethod.Name = "ScenarioTearDown";
            _testGeneratorProvider.SetTestCleanupMethod(generationContext);
            var runnerExpression = GetTestRunnerExpression();
            testCleanupMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "OnScenarioEnd", new CodeExpression[0]));
        }

        private void SetupScenarioInitializeMethod(TestClassGenerationContext generationContext)
        {
            var initializeMethod = generationContext.ScenarioInitializeMethod;
            initializeMethod.Attributes = (MemberAttributes)24576;
            initializeMethod.Name = "ScenarioInitialize";
            initializeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ScenarioInfo), "scenarioInfo"));
            var runnerExpression = GetTestRunnerExpression();
            initializeMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "OnScenarioInitialize", new CodeExpression[1]
            {
                new CodeVariableReferenceExpression("scenarioInfo")
            }));
        }

        private void SetupScenarioStartMethod(TestClassGenerationContext generationContext)
        {
            var scenarioStartMethod = generationContext.ScenarioStartMethod;
            scenarioStartMethod.Attributes = (MemberAttributes)24576;
            scenarioStartMethod.Name = "ScenarioStart";
            var runnerExpression = GetTestRunnerExpression();
            scenarioStartMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "OnScenarioStart", new CodeExpression[0]));
        }

        private void SetupFeatureBackground(TestClassGenerationContext generationContext)
        {
            if (!HasFeatureBackground(generationContext.Feature))
                return;
            var backgroundMethod = generationContext.FeatureBackgroundMethod;
            backgroundMethod.Attributes = MemberAttributes.Public;
            backgroundMethod.Name = "FeatureBackground";
            var background = generationContext.Feature.Background;
            AddLineDirective(backgroundMethod.Statements, background);
            foreach (var step in background.Steps)
                GenerateStep(backgroundMethod, step, null);
            AddLineDirectiveHidden(backgroundMethod.Statements);
        }

        private void GenerateScenarioOutlineTest(TestClassGenerationContext generationContext, ScenarioOutline scenarioOutline, List<string> variantTags = null)
        {
            ValidateExampleSetConsistency(scenarioOutline);
            var identifierMapping = CreateParamToIdentifierMapping(scenarioOutline);
            var outlineTestMethod = CreateScenatioOutlineTestMethod(generationContext, scenarioOutline, identifierMapping);

            /////NEWCODE\\\\
            if (generationContext.GenerateRowTests)
            {
                if (variantTags != null && variantTags.Count > 0)
                    GenerateScenarioOutlineExamplesAsRowTests(generationContext, scenarioOutline, outlineTestMethod, variantTags);
                else
                    GenerateScenarioOutlineExamplesAsRowTests(generationContext, scenarioOutline, outlineTestMethod, null);
            }
            else
            {
                if (variantTags != null && variantTags.Count > 0)
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
                bool flag = CanUseFirstColumnAsName(example.TableBody);
                string str;
                if (!string.IsNullOrEmpty(example.Name))
                {
                    str = example.Name.ToIdentifier();
                }
                else
                {
                    var examples = scenarioOutline.Examples;
                    Func<Examples, bool> func = es => string.IsNullOrEmpty(es.Name);
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
                ++num;
            }
        }

        private void GenerateScenarioOutlineExamplesAsRowTests(TestClassGenerationContext generationContext, ScenarioOutline scenarioOutline, CodeMemberMethod scenatioOutlineTestMethod, List<string> variantTags = null)
        {
            SetupTestMethod(generationContext, scenatioOutlineTestMethod, scenarioOutline, null, null, null, true);
            foreach (var example in scenarioOutline.Examples)
            {
                /////NEWCODE\\\\\
                var hasVariantTags = variantTags != null && variantTags.Count > 0;

                if (hasVariantTags)
                {
                    scenatioOutlineTestMethod.Parameters.RemoveAt(scenatioOutlineTestMethod.Parameters.Count - 1);
                    scenatioOutlineTestMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), _variantKey.ToLowerInvariant()));
                    scenatioOutlineTestMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string[]), "exampleTags"));
                }

                foreach (var tableRow in example.TableBody)
                {
                    if (hasVariantTags)
                    {
                        foreach (var variant in variantTags)
                        {
                            var arguments = tableRow.Cells.Select(c => c.Value).ToList();
                            arguments.Add($"{_variantKey}:{variant}");
                            _testGeneratorProvider.SetRow(generationContext, scenatioOutlineTestMethod, arguments, GetNonIgnoreTags(example.Tags), HasIgnoreTag(example.Tags));
                        }
                    }
                    else
                    {
                        IEnumerable<string> arguments = tableRow.Cells.Select(c => c.Value).ToList();
                        _testGeneratorProvider.SetRow(generationContext, scenatioOutlineTestMethod, arguments, GetNonIgnoreTags(example.Tags), HasIgnoreTag(example.Tags));
                    }
                    /////NEWCODE\\\\\
                }
            }
        }

        private ParameterSubstitution CreateParamToIdentifierMapping(ScenarioOutline scenarioOutline)
        {
            var parameterSubstitution = new ParameterSubstitution();

            foreach (var cell in scenarioOutline.Examples.First().TableHeader.Cells)
                parameterSubstitution.Add(cell.Value, cell.Value.ToIdentifierCamelCase());

            return parameterSubstitution;
        }

        private void ValidateExampleSetConsistency(ScenarioOutline scenarioOutline)
        {
            if (scenarioOutline.Examples.Count() <= 1)
                return;
            var firstExamplesHeader = scenarioOutline.Examples.First().TableHeader.Cells.Select(c => c.Value).ToArray();
            var source = scenarioOutline.Examples.Skip(1);
            Func<Examples, IEnumerable<string>> func = examples => examples.TableHeader.Cells.Select(c => c.Value);
            if (source.Select(func).Any(paramNames => !paramNames.SequenceEqual(firstExamplesHeader)))
                throw new TestGeneratorException("The example sets must provide the same parameters.");
        }

        private bool CanUseFirstColumnAsName(IEnumerable<Gherkin.Ast.TableRow> tableBody)
        {
            Func<Gherkin.Ast.TableRow, bool> func = r => !r.Cells.Any();
            if (tableBody.Any(func))
                return false;
            return tableBody.Select(r => r.Cells.First().Value.ToIdentifier()).Distinct().Count() == tableBody.Count();
        }

        private CodeMemberMethod CreateScenatioOutlineTestMethod(TestClassGenerationContext generationContext, ScenarioOutline scenarioOutline, ParameterSubstitution paramToIdentifier)
        {
            var method = CreateMethod(generationContext.TestClass);
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
            AddLineDirective(testMethod.Statements, scenarioOutline);
            var list1 = new List<CodeExpression>();
            list1.AddRange(row.Cells.Select(paramCell => new CodePrimitiveExpression(paramCell.Value)).Cast<CodeExpression>().ToList());
            list1.Add(GetStringArrayExpression(exampleSetTags));
            testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), scenatioOutlineTestMethod.Name, list1.ToArray()));
            AddLineDirectiveHidden(testMethod.Statements);
            var list2 = paramToIdentifier.Select((p2i, paramIndex) => new KeyValuePair<string, string>(p2i.Key, row.Cells.ElementAt(paramIndex).Value)).ToList();
            _testGeneratorProvider.SetTestMethodAsRow(generationContext, testMethod, scenarioOutline.Name, exampleSetTitle, variantName, list2);
        }

        private CodeMemberMethod CreateTestMethod(TestClassGenerationContext generationContext, ScenarioDefinition scenario, IEnumerable<Tag> additionalTags, string variantName = null, string exampleSetIdentifier = null)
        {
            var method = CreateMethod(generationContext.TestClass);
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
                left = GetStringArrayExpression(scenario.GetTags());
            else if (!scenario.HasTags())
            {
                left = additionalTagsExpression;
            }
            else
            {
                testMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "__tags", GetStringArrayExpression(scenario.GetTags())));
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
            AddLineDirective(testMethod.Statements, scenario);
            testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), generationContext.ScenarioInitializeMethod.Name, new CodeExpression[1]
            {
                new CodeVariableReferenceExpression("scenarioInfo")
            }));
            testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), generationContext.ScenarioStartMethod.Name, new CodeExpression[0]));
            if (HasFeatureBackground(generationContext.Feature))
            {
                AddLineDirective(testMethod.Statements, generationContext.Feature.Background);
                testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), generationContext.FeatureBackgroundMethod.Name, new CodeExpression[0]));
            }
            foreach (var step in scenario.Steps)
                GenerateStep(testMethod, step, paramToIdentifier);
            AddLineDirectiveHidden(testMethod.Statements);
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
            _decoratorRegistry.DecorateTestMethod(generationContext, testMethod, ConcatTags(scenarioDefinition.GetTags(), additionalTags), out List<string> unprocessedTags);
            if (!unprocessedTags.Any())
                return;
            _testGeneratorProvider.SetTestMethodCategories(generationContext, testMethod, unprocessedTags);
        }

        private static string GetTestMethodName(ScenarioDefinition scenario, string variantName, string exampleSetIdentifier)
        {
            var str1 = string.Format("{0}", scenario.Name.ToIdentifier());
            if (variantName != null)
            {
                var str2 = variantName.ToIdentifier().TrimStart('_');
                str1 = string.IsNullOrEmpty(exampleSetIdentifier) ? string.Format("{0}_{1}", str1, str2) : string.Format("{0}_{1}_{2}", str1, exampleSetIdentifier, str2);
            }
            return str1;
        }

        private IEnumerable<Tag> ConcatTags(params IEnumerable<Tag>[] tagLists)
        {
            return ((IEnumerable<IEnumerable<Tag>>)tagLists).Where(tagList => tagList != null).SelectMany(tagList => tagList);
        }

        private CodeExpression GetSubstitutedString(string text, ParameterSubstitution paramToIdentifier)
        {
            if (text == null)
                return new CodeCastExpression(typeof(string), new CodePrimitiveExpression(null));
            if (paramToIdentifier == null)
                return new CodePrimitiveExpression(text);
            var regex = new Regex("\\<(?<param>[^\\>]+)\\>");
            var input = text.Replace("{", "{{").Replace("}", "}}");
            var arguments = new List<string>();
            MatchEvaluator evaluator = match =>
            {
                string id;
                if (!paramToIdentifier.TryGetIdentifier(match.Groups["param"].Value, out id))
                    return match.Value;
                int num = arguments.IndexOf(id);
                if (num < 0)
                {
                    num = arguments.Count;
                    arguments.Add(id);
                }
                return "{" + num + "}";
            };
            string str2 = regex.Replace(input, evaluator);
            if (arguments.Count == 0)
                return new CodePrimitiveExpression(text);
            var codeExpressionList = new List<CodeExpression> { new CodePrimitiveExpression(str2) };
            codeExpressionList.AddRange(arguments.Select(id => new CodeVariableReferenceExpression(id)).Cast<CodeExpression>());
            return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)), "Format", codeExpressionList.ToArray());
        }

        private void GenerateStep(CodeMemberMethod testMethod, Step gherkinStep, ParameterSubstitution paramToIdentifier)
        {
            var specFlowStep = AsSpecFlowStep(gherkinStep);
            var codeExpressionList = new List<CodeExpression> { GetSubstitutedString(specFlowStep.Text, paramToIdentifier) };
            if (specFlowStep.Argument != null)
                AddLineDirectiveHidden(testMethod.Statements);
            codeExpressionList.Add(GetDocStringArgExpression(specFlowStep.Argument as DocString, paramToIdentifier));
            codeExpressionList.Add(GetTableArgExpression(specFlowStep.Argument as DataTable, testMethod.Statements, paramToIdentifier));
            codeExpressionList.Add(new CodePrimitiveExpression(specFlowStep.Keyword));
            AddLineDirective(testMethod.Statements, specFlowStep);
            var runnerExpression = GetTestRunnerExpression();
            testMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, specFlowStep.StepKeyword.ToString(), codeExpressionList.ToArray()));
        }

        private SpecFlowStep AsSpecFlowStep(Step step)
        {
            if (step is SpecFlowStep specFlowStep)
                return specFlowStep;
            throw new TestGeneratorException("The step must be a SpecFlowStep.");
        }

        private CodeExpression GetTableArgExpression(DataTable tableArg, CodeStatementCollection statements, ParameterSubstitution paramToIdentifier)
        {
            if (tableArg == null)
                return new CodeCastExpression(typeof(Table), new CodePrimitiveExpression(null));
            _tableCounter = _tableCounter + 1;
            var tableRow1 = tableArg.Rows.First();
            var array = tableArg.Rows.Skip(1).ToArray();
            var referenceExpression = new CodeVariableReferenceExpression("table" + _tableCounter);
            statements.Add(new CodeVariableDeclarationStatement(typeof(Table), referenceExpression.VariableName, new CodeObjectCreateExpression(typeof(Table), new CodeExpression[1]
            {
                GetStringArrayExpression(tableRow1.Cells.Select( c => c.Value), paramToIdentifier)
            })));
            foreach (Gherkin.Ast.TableRow tableRow2 in array)
                statements.Add(new CodeMethodInvokeExpression(referenceExpression, "AddRow", new CodeExpression[1]
                {
                    GetStringArrayExpression(tableRow2.Cells.Select( c => c.Value), paramToIdentifier)
                }));
            return referenceExpression;
        }

        private CodeExpression GetDocStringArgExpression(DocString docString, ParameterSubstitution paramToIdentifier)
        {
            return GetSubstitutedString(docString == null ? null : docString.Content, paramToIdentifier);
        }

        private void AddLinePragmaInitial(CodeTypeDeclaration testType, string sourceFile)
        {
            if (_specFlowConfiguration.AllowDebugGeneratedFiles)
                return;
            _codeDomHelper.BindTypeToSourceFile(testType, Path.GetFileName(sourceFile));
        }

        private void AddLineDirectiveHidden(CodeStatementCollection statements)
        {
            if (_specFlowConfiguration.AllowDebugGeneratedFiles)
                return;
            _codeDomHelper.AddDisableSourceLinePragmaStatement(statements);
        }

        private void AddLineDirective(CodeStatementCollection statements, Background background)
        {
            AddLineDirective(statements, background.Location);
        }

        private void AddLineDirective(CodeStatementCollection statements, ScenarioDefinition scenarioDefinition)
        {
            AddLineDirective(statements, scenarioDefinition.Location);
        }

        private void AddLineDirective(CodeStatementCollection statements, Step step)
        {
            AddLineDirective(statements, step.Location);
        }

        private void AddLineDirective(CodeStatementCollection statements, Location location)
        {
            if (location == null || _specFlowConfiguration.AllowDebugGeneratedFiles)
                return;
            _codeDomHelper.AddSourceLinePragmaStatement(statements, location.Line, location.Column);
        }
    }
}