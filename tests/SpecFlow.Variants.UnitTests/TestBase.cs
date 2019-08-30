using BoDi;
using Gherkin.Ast;
using Microsoft.CSharp;
using SpecFlow.Variants.SpecFlowPlugin.Generator;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Linq;
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
            return ParseDocumentFromString(SampleFeatureFile.FeatureFileDocument);
        }

        protected CodeNamespace SetupFeatureGenerator<T>(SpecFlowDocument document, string testClassName = "TestClassName", string tagetNamespace = "Target.Namespace") where T : IUnitTestGeneratorProvider
        {
            var codeDomHelper = new CodeDomHelper(CodeDomProviderLanguage.CSharp);
            _unitTestGeneratorProvider = (IUnitTestGeneratorProvider)Activator.CreateInstance(typeof(T), codeDomHelper, SampleFeatureFile.Variant);
            var featureGenerator = FeatureGenerator(codeDomHelper);
            return featureGenerator.GenerateUnitTestFixture(document, testClassName, tagetNamespace);
        }

        protected SpecFlowDocument ParseDocumentFromString(string documentSource, CultureInfo parserCultureInfo = null)
        {
            var parser = new SpecFlowGherkinParser(parserCultureInfo ?? new CultureInfo("en-GB"));
            using (var reader = new StringReader(documentSource))
            {
                return parser.Parse(reader, null);
            }
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
            var options = new CompilerParameters(assemblies);
            return new CSharpCodeProvider().CompileAssemblyFromDom(options, ccu);
        }

        protected int NumOfMethods(CodeNamespace generatedCode, ScenarioDefinition scenario)
        {
            var members = generatedCode.Types[0].Members.Cast<CodeTypeMember>().Where(a => a.Name.StartsWith(scenario.Name.Replace(" ", "").Replace(",", ""), StringComparison.InvariantCultureIgnoreCase));
            return members.Count();
        }

        protected int ExpectedNumOfMethods(ScenarioDefinition scenario)
        {
            int numOfMethods = 1;
            if (!_unitTestGeneratorProvider.GetTraits().HasFlag(UnitTestGeneratorTraits.RowTests))
            {
                if (scenario.HasTags())
                {
                    var variantTags = scenario.GetTags().Count(a => a.GetNameWithoutAt().StartsWith(SampleFeatureFile.Variant));
                    if (variantTags > 0)
                        numOfMethods = variantTags;
                }

                if (scenario is ScenarioOutline so)
                {
                    numOfMethods *= so.Examples.First().TableBody.Count();
                    numOfMethods++;
                }

                return numOfMethods;
            }
            else
            {
                if (scenario is ScenarioOutline so)
                    return numOfMethods;

                if (scenario.HasTags())
                {
                    var variantTags = scenario.GetTags().Count(a => a.GetNameWithoutAt().StartsWith(SampleFeatureFile.Variant));
                    if (variantTags > 0)
                        numOfMethods = variantTags;
                }

                return numOfMethods;
            }
        }
    }
}
