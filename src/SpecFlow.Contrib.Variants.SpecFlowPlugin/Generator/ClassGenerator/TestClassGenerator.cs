using SpecFlow.Contrib.Variants.SpecFlowPlugin.Generator;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Generator.UnitTestConverter;
using TechTalk.SpecFlow.Generator.UnitTestProvider;
using TechTalk.SpecFlow.Parser;
using TechTalk.SpecFlow.Utils;

namespace SpecFlow.Contrib.Variants.SpecFlowPlugin.Generator.ClassGenerator
{
    internal class TestClassGenerator : ITestClassGenerator
    {
        protected CodeNamespace CodeNamespace { get; private set; }
        protected TestClassGenerationContext GenerationContext { get; private set; }

        private readonly IDecoratorRegistry _decoratorRegistry;
        private readonly IUnitTestGeneratorProvider _testGeneratorProvider;
        private readonly CodeDomHelper _codeDomHelper;
        private readonly SpecFlowConfiguration _specFlowConfiguration;

        public TestClassGenerator(IDecoratorRegistry decoratorRegistry, IUnitTestGeneratorProvider testGeneratorProvider, CodeDomHelper codeDomHelper, SpecFlowConfiguration specFlowConfiguration)
        {
            _decoratorRegistry = decoratorRegistry;
            _testGeneratorProvider = testGeneratorProvider;
            _codeDomHelper = codeDomHelper;
            _specFlowConfiguration = specFlowConfiguration;
        }

        public void CreateNamespace(string targetNamespace)
        {
            targetNamespace = targetNamespace ?? "SpecFlowTests";
            if (!targetNamespace.StartsWith("global", StringComparison.CurrentCultureIgnoreCase) && _codeDomHelper.TargetLanguage == CodeDomProviderLanguage.VB)
                targetNamespace = $"GlobalVBNetNamespace.{targetNamespace}";
            CodeNamespace = new CodeNamespace(targetNamespace)
            {
                Imports = { new CodeNamespaceImport("TechTalk.SpecFlow") }
            };
        }

        public void CreateTestClassStructure(string testClassName, SpecFlowDocument document)
        {
            var generatedTypeDeclaration = _codeDomHelper.CreateGeneratedTypeDeclaration(testClassName);
            CodeNamespace.Types.Add(generatedTypeDeclaration);
            GenerationContext = new TestClassGenerationContext(_testGeneratorProvider, document, CodeNamespace, generatedTypeDeclaration, generatedTypeDeclaration.DeclareTestRunnerMember<ITestRunner>("testRunner"), generatedTypeDeclaration.CreateMethod(), generatedTypeDeclaration.CreateMethod(), generatedTypeDeclaration.CreateMethod(), generatedTypeDeclaration.CreateMethod(), generatedTypeDeclaration.CreateMethod(), generatedTypeDeclaration.CreateMethod(), generatedTypeDeclaration.CreateMethod(), document.SpecFlowFeature.HasFeatureBackground() ? generatedTypeDeclaration.CreateMethod() : null, _testGeneratorProvider.GetTraits().HasFlag(UnitTestGeneratorTraits.RowTests) && _specFlowConfiguration.AllowRowTests);
        }

        public void SetupTestClass()
        {
            GenerationContext.TestClass.IsPartial = true;
            GenerationContext.TestClass.TypeAttributes |= TypeAttributes.Public;
            _codeDomHelper.AddLinePragmaInitial(GenerationContext.TestClass, GenerationContext.Document.SourceFilePath, _specFlowConfiguration);
            _testGeneratorProvider.SetTestClass(GenerationContext, GenerationContext.Feature.Name, GenerationContext.Feature.Description);
            _decoratorRegistry.DecorateTestClass(GenerationContext, out List<string> unprocessedTags);
            if (!unprocessedTags.Any())
                return;
            _testGeneratorProvider.SetTestClassCategories(GenerationContext, unprocessedTags);
        }

        public void SetupTestClassInitializeMethod()
        {
            var initializeMethod = GenerationContext.TestClassInitializeMethod;
            initializeMethod.Attributes = MemberAttributes.Public;
            initializeMethod.Name = "FeatureSetup";
            _testGeneratorProvider.SetTestClassInitializeMethod(GenerationContext);
            CodeExpression[] codeExpressionArray1;
            if (!_testGeneratorProvider.GetTraits().HasFlag(UnitTestGeneratorTraits.ParallelExecution))
            {
                codeExpressionArray1 = (new CodePrimitiveExpression[2]
                {
                    new CodePrimitiveExpression(null),
                    new CodePrimitiveExpression(0)
                });
            }
            else
            {
                codeExpressionArray1 = new CodeExpression[0];
            }

            var codeExpressionArray2 = codeExpressionArray1;
            CodeExpression runnerExpression = GetTestRunnerExpression();
            initializeMethod.Statements.Add(new CodeAssignStatement(runnerExpression, new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(TestRunnerManager)), "GetTestRunner", codeExpressionArray2)));
            initializeMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(FeatureInfo), "featureInfo", new CodeObjectCreateExpression(typeof(FeatureInfo), new CodeExpression[5]
            {
                new CodeObjectCreateExpression(typeof(CultureInfo), new CodeExpression[1]
                {
                     new CodePrimitiveExpression(GenerationContext.Feature.Language)
                }),
                new CodePrimitiveExpression(GenerationContext.Feature.Name),
                new CodePrimitiveExpression(GenerationContext.Feature.Description),
                new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("ProgrammingLanguage"), _codeDomHelper.TargetLanguage.ToString()),
                GenerationContext.Feature.Tags.GetStringArrayExpression()
            })));
            initializeMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "OnFeatureStart", new CodeExpression[1]
            {
                new CodeVariableReferenceExpression("featureInfo")
            }));
        }

        public void SetupTestInitializeMethod()
        {
            var initializeMethod = GenerationContext.TestInitializeMethod;
            initializeMethod.Attributes = MemberAttributes.Public;
            initializeMethod.Name = "TestInitialize";
            _testGeneratorProvider.SetTestInitializeMethod(GenerationContext);
        }

        public void SetupTestCleanupMethod()
        {
            var testCleanupMethod = GenerationContext.TestCleanupMethod;
            testCleanupMethod.Attributes = MemberAttributes.Public;
            testCleanupMethod.Name = "ScenarioTearDown";
            _testGeneratorProvider.SetTestCleanupMethod(GenerationContext);
            var runnerExpression = GetTestRunnerExpression();
            testCleanupMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "OnScenarioEnd", new CodeExpression[0]));
        }

        public void SetupTestClassCleanupMethod()
        {
            var classCleanupMethod = GenerationContext.TestClassCleanupMethod;
            classCleanupMethod.Attributes = MemberAttributes.Public;
            classCleanupMethod.Name = "FeatureTearDown";
            _testGeneratorProvider.SetTestClassCleanupMethod(GenerationContext);
            var runnerExpression = GetTestRunnerExpression();
            classCleanupMethod.Statements.Add(new CodeMethodInvokeExpression(runnerExpression, "OnFeatureEnd", new CodeExpression[0]));
            classCleanupMethod.Statements.Add(new CodeAssignStatement(runnerExpression, new CodePrimitiveExpression(null)));
        }

        protected CodeExpression GetTestRunnerExpression()
        {
            return new CodeVariableReferenceExpression("testRunner");
        }
    }
}
