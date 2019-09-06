using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Generator.UnitTestProvider;
using TechTalk.SpecFlow.Utils;

namespace SpecFlow.Variants.SpecFlowPlugin.Providers
{
    public class NUnitProviderExtended : IUnitTestGeneratorProvider
    {
        private readonly CodeDomHelper _codeDomHelper;
        private readonly string _variantKey;
        private IEnumerable<string> _filteredCategories;

        public NUnitProviderExtended(CodeDomHelper codeDomHelper, string variantKey)
        {
            _codeDomHelper = codeDomHelper;
            _variantKey = variantKey;
        }

        public UnitTestGeneratorTraits GetTraits()
        {
            return UnitTestGeneratorTraits.RowTests | UnitTestGeneratorTraits.ParallelExecution;
        }

        public void SetTestClass(TestClassGenerationContext generationContext, string featureTitle, string featureDescription)
        {
            _codeDomHelper.AddAttribute(generationContext.TestClass, "NUnit.Framework.TestFixtureAttribute");
            _codeDomHelper.AddAttribute(generationContext.TestClass, "NUnit.Framework.DescriptionAttribute", new object[1] { featureTitle });
        }

        public void SetTestClassInitializeMethod(TestClassGenerationContext generationContext)
        {
            _codeDomHelper.AddAttribute(generationContext.TestClassInitializeMethod, "NUnit.Framework.OneTimeSetUpAttribute");
        }

        public void SetTestClassCategories(TestClassGenerationContext generationContext, IEnumerable<string> featureCategories)
        {
            _codeDomHelper.AddAttributeForEachValue(generationContext.TestClass, "NUnit.Framework.CategoryAttribute", featureCategories);
        }

        public void SetTestClassIgnore(TestClassGenerationContext generationContext)
        {
            _codeDomHelper.AddAttribute(generationContext.TestClass, "NUnit.Framework.IgnoreAttribute", new object[1] { "Ignored feature" });
        }

        public void SetTestClassParallelize(TestClassGenerationContext generationContext)
        {
            _codeDomHelper.AddAttribute(generationContext.TestClass, "NUnit.Framework.ParallelizableAttribute");
        }

        public void SetTestClassCleanupMethod(TestClassGenerationContext generationContext)
        {
            _codeDomHelper.AddAttribute(generationContext.TestClassCleanupMethod, "NUnit.Framework.OneTimeTearDownAttribute");
        }

        public void FinalizeTestClass(TestClassGenerationContext generationContext)
        {
            generationContext.ScenarioInitializeMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(null, generationContext.TestRunnerField.Name), "ScenarioContext"), "ScenarioContainer"), "RegisterInstanceAs", new CodeTypeReference[1]
            {
                new CodeTypeReference("NUnit.Framework.TestContext")
            }), new CodeExpression[1]
            {
                new CodeVariableReferenceExpression("NUnit.Framework.TestContext.CurrentContext")
            }));
        }

        public void SetTestInitializeMethod(TestClassGenerationContext generationContext)
        {
            _codeDomHelper.AddAttribute(generationContext.TestInitializeMethod, "NUnit.Framework.SetUpAttribute");
        }

        public void SetTestMethod(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string friendlyTestName)
        {
            _codeDomHelper.AddAttribute(testMethod, "NUnit.Framework.TestAttribute");
            _codeDomHelper.AddAttribute(testMethod, "NUnit.Framework.DescriptionAttribute", new object[1] { friendlyTestName });
        }

        public void SetTestMethodCategories(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> scenarioCategories)
        {
            // Remove categories that are not the current variant
            _filteredCategories = scenarioCategories.Where(a => !a.StartsWith(_variantKey) || a.EndsWith(testMethod.Name.Split('_').Last()));
            _codeDomHelper.AddAttributeForEachValue(testMethod, "NUnit.Framework.CategoryAttribute", _filteredCategories);
        }

        public void SetRow(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> arguments, IEnumerable<string> tags, bool isIgnored)
        {
            // Get current variant
            var variant = arguments.FirstOrDefault(a => a.StartsWith(_variantKey));

            // Base nunit provider stuff
            var list = arguments.Select(a => a.Replace($"{_variantKey}:", "")).Select(arg => new CodeAttributeArgument(new CodePrimitiveExpression(arg))).ToList();
            int num = tags.Any() ? 1 : 0;
            var source = tags.Select(t => new CodePrimitiveExpression(t));
            var codeExpression = num != 0 ? new CodeArrayCreateExpression(typeof(string[]), source.ToArray()) : (CodeExpression)new CodePrimitiveExpression(null);
            list.Add(new CodeAttributeArgument(codeExpression));
            if (num != 0)
            {
                CodeExpression codeExpression2 = new CodePrimitiveExpression(string.Join(",", tags.ToArray().Append(variant)));
                list.Add(new CodeAttributeArgument("Category", codeExpression2));
            }
            else if (variant != null)
            {
                // Add the current variant as the nunit Category attribute
                list.Add(new CodeAttributeArgument("Category", new CodePrimitiveExpression(string.Join(",", _filteredCategories.ToArray().Prepend(variant)))));
            }

            // Filter arguments to build the nunit TestName attribute
            var list2 = arguments.Where(a => !a.StartsWith(_variantKey) && a != null).Select(arg => new CodeAttributeArgument(new CodePrimitiveExpression(arg))).ToList();
            var str = string.Concat(list2.Select(arg => string.Format("\"{0}\", ", ((CodePrimitiveExpression)arg.Value).Value))).TrimEnd(' ', ',').Replace('.', '_');
            var testName = variant != null ? testMethod.Name + " with " + variant?.Split(':')[1] + " and " + str : testMethod.Name + " with " + str;
            list.Add(new CodeAttributeArgument("TestName", new CodePrimitiveExpression(testName)));

            _codeDomHelper.AddAttribute(testMethod, "NUnit.Framework.TestCaseAttribute", list.ToArray());

            // Remove unneeded TestCategory attributes
            testMethod.CustomAttributes.Cast<CodeAttributeDeclaration>().Where(a => a.Name == "NUnit.Framework.CategoryAttribute")
                .ToList().ForEach(atr => testMethod.CustomAttributes.Remove(atr));
        }

        public void SetTestMethodIgnore(TestClassGenerationContext generationContext, CodeMemberMethod testMethod)
        {
            _codeDomHelper.AddAttribute(testMethod, "NUnit.Framework.IgnoreAttribute", new object[1] { "Ignored scenario" });
        }

        public void SetTestCleanupMethod(TestClassGenerationContext generationContext)
        {
            _codeDomHelper.AddAttribute(generationContext.TestCleanupMethod, "NUnit.Framework.TearDownAttribute");
        }

        public void SetTestMethodAsRow(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle, string exampleSetName, string variantName, IEnumerable<KeyValuePair<string, string>> arguments)
        {
        }

        public void SetRowTest(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle)
        {
        }
    }
}