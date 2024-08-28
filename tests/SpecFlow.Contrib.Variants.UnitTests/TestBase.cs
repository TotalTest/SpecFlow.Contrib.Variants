using BoDi;
using Gherkin.Ast;
using Microsoft.CSharp;
using SpecFlow.Contrib.Variants.Generator;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Linq;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Generator.CodeDom;
using TechTalk.SpecFlow.Generator.UnitTestConverter;
using TechTalk.SpecFlow.Generator.UnitTestProvider;
using TechTalk.SpecFlow.Parser;

namespace SpecFlow.Contrib.Variants.UnitTests
{
    public class TestBase
    {
        private IUnitTestGeneratorProvider _unitTestGeneratorProvider;

        protected SpecFlowDocument CreateSpecFlowDocument(string document)
        {
            var parser = new SpecFlowGherkinParser(new CultureInfo("en-GB"));
            using (var reader = new StringReader(document))
            {
                return parser.Parse(reader, new SpecFlowDocumentLocation("Test"));
            }
        }

        protected CodeNamespace SetupFeatureGenerator<T>(SpecFlowDocument document, string testClassName = "TestClassName", string tagetNamespace = "Target.Namespace") where T : IUnitTestGeneratorProvider
        {
            var codeDomHelper = new CodeDomHelper(CodeDomProviderLanguage.CSharp);
            _unitTestGeneratorProvider = (T)Activator.CreateInstance(typeof(T), codeDomHelper, SampleFeatureFile.Variant);
            var featureGenerator = FeatureGenerator(codeDomHelper);
            return featureGenerator.GenerateUnitTestFixture(document, testClassName, tagetNamespace);
        }

        private IFeatureGenerator FeatureGenerator(CodeDomHelper codeDomHelper)
        {
            var dr = new DecoratorRegistry(new ObjectContainer());
            var runtimeConfiguration = ConfigurationLoader.GetDefault();
            runtimeConfiguration.AllowDebugGeneratedFiles = true;

            return new FeatureGeneratorExtended(_unitTestGeneratorProvider, codeDomHelper, runtimeConfiguration, dr, SampleFeatureFile.Variant);
        }

        protected CompilerResults GetCompilerResults(CodeNamespace generatedCode, string[] assemblies)
        {
            var ccu = new CodeCompileUnit();
            ccu.Namespaces.Add(generatedCode);
            return new CSharpCodeProvider().CompileAssemblyFromDom(new CompilerParameters(assemblies), ccu);
        }

        protected int ExpectedNumOfMethodsForFeatureVariants(Scenario scenario, Feature feature = null)
        {
            int numOfMethods = 1;
            if (!_unitTestGeneratorProvider.GetTraits().HasFlag(UnitTestGeneratorTraits.RowTests))
            {
                if (scenario.Tags.Any())
                {
                    var variantTags = scenario.GetTagsByNameStart(SampleFeatureFile.Variant).Count;
                    if (variantTags > 0) numOfMethods = variantTags;
                }

                if (scenario is ScenarioOutline so)
                {
                    numOfMethods *= so.GetExamplesTableBody().Count;
                    numOfMethods++;
                }

                return numOfMethods;
            }
            else
            {
                if (scenario is ScenarioOutline) return numOfMethods;

                int variantTags = 0;
                if (feature?.HasTags() == true)
                {
                    variantTags += feature.GetTagsByNameStart(SampleFeatureFile.Variant).Count;
                }
                else if (scenario.Tags.Any())
                {
                    variantTags += scenario.GetTagsByNameStart(SampleFeatureFile.Variant).Count;
                }

                if (variantTags > 0) numOfMethods = variantTags;
                return numOfMethods;
            }
        }

        protected int ExpectedNumOfMethodsForFeatureVariants(Feature feature, Scenario scenario)
        {
            int numOfMethods = 1;
            if (!_unitTestGeneratorProvider.GetTraits().HasFlag(UnitTestGeneratorTraits.RowTests))
            {
                if (feature.HasTags())
                {
                    var variantTags = feature.GetTagsByNameStart(SampleFeatureFile.Variant).Count;
                    if (variantTags > 0) numOfMethods = variantTags;
                }

                if (scenario is ScenarioOutline so)
                {
                    numOfMethods *= so.GetExamplesTableBody().Count;
                    numOfMethods++;
                }

                return numOfMethods;
            }
            else
            {
                if (scenario is ScenarioOutline) return numOfMethods;

                if (feature.HasTags())
                {
                    var variantTags = feature.GetTagsByNameStart(SampleFeatureFile.Variant).Count;
                    if (variantTags > 0) numOfMethods = variantTags;
                }

                return numOfMethods;
            }
        }

        protected string GetScenarioContextVariantStatement(CodeTypeMember method, bool isBase = false, int statementLine = 5)
        {
            var statement = ((CodeMemberMethod)method).Statements.Cast<CodeStatement>().ToList()[statementLine] as CodeExpressionStatement;
            var expression = statement.Expression as CodeMethodInvokeExpression;
            if (!(expression.Method.TargetObject is CodePropertyReferenceExpression property))
                return null;
            var field = property.TargetObject as CodeFieldReferenceExpression;

            string keyValue;
            string keyName;
            if (isBase)
            {
                keyName = ((CodePrimitiveExpression)expression.Parameters[0]).Value.ToString();
                keyValue = ((CodeVariableReferenceExpression)expression.Parameters[1]).VariableName;
            }
            else
            {
                keyName = ((CodePrimitiveExpression)expression.Parameters[0]).Value.ToString();
                keyValue = ((CodePrimitiveExpression)expression.Parameters[1]).Value.ToString();
            }

            return $"{field.FieldName}.{property.PropertyName}.{expression.Method.MethodName}(\"{keyName}\", \"{keyValue}\");";
        }

        protected string GetVariantParameterOfRowMethod(CodeTypeMember method)
        {
            var statement = ((CodeMemberMethod)method).Statements.Cast<CodeStatement>().ToList()[0] as CodeExpressionStatement;
            var expression = statement.Expression as CodeMethodInvokeExpression;
            var lastParam = expression.Parameters.Cast<CodeExpression>().Last(a => a is CodePrimitiveExpression) as CodePrimitiveExpression;
            return lastParam.Value.ToString();
        }

        protected string GetScenarioInfoStatement(CodeTypeMember method, bool isBase = false, int statementLine = 5)
        {
            var statement = ((CodeMemberMethod)method).Statements.Cast<CodeStatement>().ToList()[statementLine] as CodeVariableDeclarationStatement;
            var expression = statement.InitExpression as CodeMethodInvokeExpression;
            if (!(expression.Method.TargetObject is CodePropertyReferenceExpression property))
                return null;
            var field = property.TargetObject as CodeFieldReferenceExpression;

            string keyValue;
            string keyName;
            if (isBase)
            {
                keyName = ((CodePrimitiveExpression)expression.Parameters[0]).Value.ToString();
                keyValue = ((CodeVariableReferenceExpression)expression.Parameters[1]).VariableName;
            }
            else
            {
                keyName = ((CodePrimitiveExpression)expression.Parameters[0]).Value.ToString();
                keyValue = ((CodePrimitiveExpression)expression.Parameters[1]).Value.ToString();
            }

            return $"{field.FieldName}.{property.PropertyName}.{expression.Method.MethodName}(\"{keyName}\", \"{keyValue}\");";
        }
    }
}
