using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Generator.CodeDom;
using TechTalk.SpecFlow.Generator.UnitTestProvider;

namespace SpecFlow.Contrib.Variants.Providers
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
            var variantValue = testMethod.Name.Split('_').Last();

            var filteredCategories = scenarioCategories.Where(a => a.StartsWith(_variantKey) && !a.EndsWith(variantValue));
            base.SetTestMethodCategories(generationContext, testMethod, scenarioCategories.Except(filteredCategories));

            var variant = scenarioCategories.FirstOrDefault(a => a.StartsWith(_variantKey) && a.EndsWith(variantValue));
            if (variant != null)
            {
                CodeDomHelper.AddAttribute(testMethod, "Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute", _variantKey, variantValue);
            }

            if (generationContext.CustomData.ContainsKey("featureCategories")
                && ((string[])generationContext.CustomData["featureCategories"]).Any(a => a.StartsWith(_variantKey)))
            {
                var dupeCounter = false;
                testMethod.CustomAttributes.Cast<CodeAttributeDeclaration>().ToList().ForEach(a =>
                {
                    if (a.Name.Contains("TestCategory"))
                    {
                        var args = a.Arguments.Cast<CodeAttributeArgument>().Where(b => ((CodePrimitiveExpression)b.Value).Value.ToString().StartsWith(_variantKey)
                            && !((CodePrimitiveExpression)b.Value).Value.ToString().EndsWith(variantValue));

                        if (args.Any())
                        {
                            testMethod.CustomAttributes.Remove(a);
                            return;
                        }

                        var args2 = a.Arguments.Cast<CodeAttributeArgument>().Where(b => ((CodePrimitiveExpression)b.Value).Value.ToString().StartsWith(_variantKey)
                            && ((CodePrimitiveExpression)b.Value).Value.ToString().EndsWith(variantValue));

                        if (args2.Any() && !dupeCounter)
                        {
                            testMethod.CustomAttributes.Remove(a);
                            dupeCounter = true;
                        }
                    }
                });
            }
        }
    }
}