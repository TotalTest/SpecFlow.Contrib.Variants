using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Generator.UnitTestProvider;
using TechTalk.SpecFlow.Utils;

namespace SpecFlow.Variants.SpecFlowPlugin.Providers
{
    internal class MsTestProviderExtended : MsTestV2GeneratorProvider
    {
        private readonly string _variantKey;

        public MsTestProviderExtended(CodeDomHelper codeDomHelper, string variantKey) : base(codeDomHelper)
        {
            _variantKey = variantKey;
        }

        public override void SetTestMethodCategories(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> scenarioCategories)
        {
            var filteredCategories = scenarioCategories.Where(a => a.StartsWith(_variantKey) && !a.EndsWith(testMethod.Name.Split('_').Last()));
            base.SetTestMethodCategories(generationContext, testMethod, scenarioCategories.Except(filteredCategories));
        }
    }
}