using BoDi;
using Gherkin.Ast;
using Microsoft.CSharp;
using SpecFlow.Variants.SpecFlowPlugin.Generator;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Generator.UnitTestConverter;
using TechTalk.SpecFlow.Generator.UnitTestProvider;
using TechTalk.SpecFlow.Parser;
using TechTalk.SpecFlow.Utils;

namespace SpecFlow.Variants.UnitTests
{
    public class TestBase
    {
        private IUnitTestGeneratorProvider _unitTestGeneratorProvider;

        protected SpecFlowDocument CreateSpecFlowDocument()
        {
            var parser = new SpecFlowGherkinParser(new CultureInfo("en-GB"));
            using (var reader = new StringReader(SampleFeatureFile.FeatureFileDocument))
            {
                return parser.Parse(reader, null);
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

        protected int ExpectedNumOfMethods(ScenarioDefinition scenario)
        {
            int numOfMethods = 1;
            if (!_unitTestGeneratorProvider.GetTraits().HasFlag(UnitTestGeneratorTraits.RowTests))
            {
                if (scenario.HasTags())
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

                if (scenario.HasTags())
                {
                    var variantTags = scenario.GetTagsByNameStart(SampleFeatureFile.Variant).Count;
                    if (variantTags > 0) numOfMethods = variantTags;
                }

                return numOfMethods;
            }
        }
    }
}
