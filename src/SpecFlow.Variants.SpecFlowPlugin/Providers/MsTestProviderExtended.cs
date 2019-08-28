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

        // TODO: Check if safe to remove 
        private IEnumerable<string> _scenarioCategories;

        public MsTestProviderExtended(CodeDomHelper codeDomHelper, string variantKey) : base(codeDomHelper)
        {
            _variantKey = variantKey;
        }

        public override void SetTestMethodCategories(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> scenarioCategories)
        {
            // TODO: Check if safe to remove 
            _scenarioCategories = scenarioCategories;

            var filteredCategories = scenarioCategories.Where(a => a.StartsWith(_variantKey) && !a.EndsWith(testMethod.Name.Split('_').Last()));
            base.SetTestMethodCategories(generationContext, testMethod, scenarioCategories.Except(filteredCategories));
        }

        // TODO: Check if safe to remove
        public override void SetTestMethodAsRow(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle, string exampleSetName, string variantName, IEnumerable<KeyValuePair<string, string>> arguments)
        {
            base.SetTestMethodAsRow(generationContext, testMethod, scenarioTitle, exampleSetName, variantName, arguments);

            // Below is an example of removing attributes
            //var attrArgumentsToRemove = _scenarioCategories.Where(a => a.StartsWith(_variantKey) && !a.EndsWith(variantName.Split('_')[1]));
            //
            //testMethod.CustomAttributes.Cast<CodeAttributeDeclaration>().Where(a => a.Name == "Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute" && a.Arguments.Cast<CodeAttributeArgument>().Any(b =>
            //    {
            //        var cd = b.Value as CodePrimitiveExpression;
            //        return attrArgumentsToRemove.Any(qr => qr == cd.Value.ToString());
            //    })).ToList().ForEach(qqq => testMethod.CustomAttributes.Remove(qqq));
        }
    }
}