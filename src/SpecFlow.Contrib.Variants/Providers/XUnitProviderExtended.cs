using Gherkin.Ast;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Generator.CodeDom;
using TechTalk.SpecFlow.Generator.UnitTestProvider;

namespace SpecFlow.Contrib.Variants.Providers
{
    internal class XUnitProviderExtended : IUnitTestGeneratorProvider
    {
        private readonly CodeDomHelper _codeDomHelper;
        private readonly string _variantKey;
        private CodeTypeDeclaration _currentFixtureDataTypeDeclaration;

        public XUnitProviderExtended(CodeDomHelper codeDomHelper, string variantKey)
        {
            _codeDomHelper = codeDomHelper;
            _variantKey = variantKey;
        }

        public UnitTestGeneratorTraits GetTraits()
        {
            return UnitTestGeneratorTraits.ParallelExecution;
        }

        public void SetTestClassInitializeMethod(TestClassGenerationContext generationContext)
        {
            generationContext.TestClassInitializeMethod.Attributes |= MemberAttributes.Static;
            generationContext.TestRunnerField.Attributes |= MemberAttributes.Static;
            _currentFixtureDataTypeDeclaration = _codeDomHelper.CreateGeneratedTypeDeclaration("FixtureData");
            generationContext.TestClass.Members.Add(_currentFixtureDataTypeDeclaration);
            var nestedTypeReference = _codeDomHelper.CreateNestedTypeReference(generationContext.TestClass, _currentFixtureDataTypeDeclaration.Name);
            var fixtureInterface = CreateFixtureInterface(generationContext, nestedTypeReference);
            _codeDomHelper.SetTypeReferenceAsInterface(fixtureInterface);
            generationContext.TestClass.BaseTypes.Add(fixtureInterface);
            var codeConstructor = new CodeConstructor { Attributes = MemberAttributes.Public };
            _currentFixtureDataTypeDeclaration.Members.Add(codeConstructor);
            codeConstructor.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(generationContext.TestClass.Name)), generationContext.TestClassInitializeMethod.Name, new CodeExpression[0]));
        }

        public void SetTestClassCategories(TestClassGenerationContext generationContext, IEnumerable<string> featureCategories)
        {
            foreach (string featureCategory in featureCategories)
                SetProperty(generationContext.TestClass, "Category", featureCategory);
        }

        public void SetTestClassParallelize(TestClassGenerationContext generationContext)
        {
            _codeDomHelper.AddAttribute(generationContext.TestClass, "Xunit.CollectionAttribute", new CodeAttributeArgument[1]
            {
                new CodeAttributeArgument(new CodePrimitiveExpression(Guid.NewGuid()))
            });
        }

        public void SetTestClassCleanupMethod(TestClassGenerationContext generationContext)
        {
            generationContext.TestClassCleanupMethod.Attributes |= MemberAttributes.Static;
            _currentFixtureDataTypeDeclaration.BaseTypes.Add(typeof(IDisposable));
            var codeMemberMethod = new CodeMemberMethod { PrivateImplementationType = new CodeTypeReference(typeof(IDisposable)), Name = "Dispose" };
            _currentFixtureDataTypeDeclaration.Members.Add(codeMemberMethod);
            codeMemberMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(generationContext.TestClass.Name)), generationContext.TestClassCleanupMethod.Name, new CodeExpression[0]));
        }

        public void FinalizeTestClass(TestClassGenerationContext generationContext)
        {
            IgnoreFeature(generationContext);
            generationContext.ScenarioInitializeMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(null, generationContext.TestRunnerField.Name), "ScenarioContext"), "ScenarioContainer"), "RegisterInstanceAs", new CodeTypeReference[1]
            {
                new CodeTypeReference("Xunit.Abstractions.ITestOutputHelper")
            }), new CodeExpression[1] { new CodeVariableReferenceExpression("_testOutputHelper") }));
        }

        public void SetTestInitializeMethod(TestClassGenerationContext generationContext)
        {
            var ctorMethod = new CodeConstructor { Attributes = MemberAttributes.Public };
            generationContext.TestClass.Members.Add(ctorMethod);
            SetTestConstructor(generationContext, ctorMethod);
        }

        public void SetTestMethod(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string friendlyTestName)
        {
            _codeDomHelper.AddAttribute(testMethod, "Xunit.FactAttribute", new CodeAttributeArgument[1]
            {
                new CodeAttributeArgument("DisplayName", new CodePrimitiveExpression(friendlyTestName))
            });

            SetProperty(testMethod, "FeatureTitle", generationContext.Feature.Name);
            SetDescription(testMethod, friendlyTestName);
        }

        public void SetTestMethodCategories(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> scenarioCategories)
        {
            var variantValue = testMethod.Name.Split('_').Last();
            var filteredCategories = scenarioCategories.Where(a => a.StartsWith(_variantKey) && !a.EndsWith(variantValue));

            foreach (string scenarioCategory in scenarioCategories.Except(filteredCategories))
                SetProperty(testMethod, "Category", scenarioCategory);

            //testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeFieldReferenceExpression(null, generationContext.TestRunnerField.Name), "ScenarioContext"), "Add",
            //    new CodeExpression[2] { new CodePrimitiveExpression(_variantHelper.VariantKey), new CodePrimitiveExpression(_variantValue) }));
        }

        public void SetTestMethodIgnore(TestClassGenerationContext generationContext, CodeMemberMethod testMethod)
        {
            var attributeDeclaration1 = testMethod.CustomAttributes.OfType<CodeAttributeDeclaration>().FirstOrDefault(codeAttributeDeclaration => codeAttributeDeclaration.Name == "Xunit.FactAttribute");
            if (attributeDeclaration1 != null)
                attributeDeclaration1.Arguments.Add(new CodeAttributeArgument("Skip", new CodePrimitiveExpression("Ignored")));
            var attributeDeclaration2 = testMethod.CustomAttributes.OfType<CodeAttributeDeclaration>().FirstOrDefault(codeAttributeDeclaration => codeAttributeDeclaration.Name == "Xunit.TheoryAttribute");
            if (attributeDeclaration2 == null)
                return;
            attributeDeclaration2.Arguments.Add(new CodeAttributeArgument("Skip", new CodePrimitiveExpression("Ignored")));
        }

        public void SetTestCleanupMethod(TestClassGenerationContext generationContext)
        {
            generationContext.TestClass.BaseTypes.Add(typeof(IDisposable));
            var codeMemberMethod = new CodeMemberMethod { PrivateImplementationType = new CodeTypeReference(typeof(IDisposable)), Name = "Dispose" };
            generationContext.TestClass.Members.Add(codeMemberMethod);
            codeMemberMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), generationContext.TestCleanupMethod.Name, new CodeExpression[0]));
        }

        private void SetTestConstructor(TestClassGenerationContext generationContext, CodeConstructor ctorMethod)
        {
            ctorMethod.Parameters.Add(new CodeParameterDeclarationExpression((CodeTypeReference)generationContext.CustomData["fixtureData"], "fixtureData"));
            ctorMethod.Parameters.Add(new CodeParameterDeclarationExpression("Xunit.Abstractions.ITestOutputHelper", "testOutputHelper"));
            ctorMethod.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_testOutputHelper"), new CodeVariableReferenceExpression("testOutputHelper")));
            ctorMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), generationContext.TestInitializeMethod.Name, new CodeExpression[0]));
        }

        private void SetProperty(CodeTypeMember codeTypeMember, string name, string value)
        {
            _codeDomHelper.AddAttribute(codeTypeMember, "Xunit.TraitAttribute", name, value);
        }

        private void SetDescription(CodeTypeMember codeTypeMember, string description)
        {
            SetProperty(codeTypeMember, "Description", description);
        }

        private CodeTypeReference CreateFixtureInterface(TestClassGenerationContext generationContext, CodeTypeReference fixtureDataType)
        {
            generationContext.TestClass.Members.Add(new CodeMemberField("Xunit.Abstractions.ITestOutputHelper", "_testOutputHelper"));
            generationContext.CustomData.Add("fixtureData", fixtureDataType);
            return new CodeTypeReference("Xunit.IClassFixture", new CodeTypeReference[1] { fixtureDataType });
        }

        private void IgnoreFeature(TestClassGenerationContext generationContext)
        {
            var tags = generationContext.Feature.Tags;
            bool func(Tag x) => string.Equals(x.Name, "@Ignore", StringComparison.InvariantCultureIgnoreCase);
            if (!tags.Any(func))
                return;
            foreach (var member in generationContext.TestClass.Members)
            {
                var testMethod = member as CodeMemberMethod;
                if (testMethod != null && !IsTestMethodAlreadyIgnored(testMethod))
                    SetTestMethodIgnore(generationContext, testMethod);
            }
        }

        private bool IsTestMethodAlreadyIgnored(CodeMemberMethod testMethod)
        {
            return IsTestMethodAlreadyIgnored(testMethod, "Xunit.FactAttribute", "Xunit.TheoryAttribute");
        }

        private bool IsTestMethodAlreadyIgnored(CodeMemberMethod testMethod, string factAttributeName, string theoryAttributeName)
        {
            var attributeDeclaration1 = testMethod.CustomAttributes.OfType<CodeAttributeDeclaration>().FirstOrDefault(codeAttributeDeclaration => codeAttributeDeclaration.Name == factAttributeName);
            bool? nullable1 = attributeDeclaration1 != null ? new bool?(attributeDeclaration1.Arguments.OfType<CodeAttributeArgument>().Any(x => string.Equals(x.Name, "Skip", StringComparison.InvariantCultureIgnoreCase))) : new bool?();
            var attributeDeclaration2 = testMethod.CustomAttributes.OfType<CodeAttributeDeclaration>().FirstOrDefault(codeAttributeDeclaration => codeAttributeDeclaration.Name == theoryAttributeName);
            bool? nullable2 = attributeDeclaration2 != null ? new bool?(attributeDeclaration2.Arguments.OfType<CodeAttributeArgument>().Any(x => string.Equals(x.Name, "Skip", StringComparison.InvariantCultureIgnoreCase))) : new bool?();
            if (!nullable1.GetValueOrDefault())
                return nullable2.GetValueOrDefault();
            return true;
        }

        public void SetTestClass(TestClassGenerationContext generationContext, string featureTitle, string featureDescription)
        {
        }

        public void SetRowTest(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioDescription = null)
        {
        }

        public void SetRow(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> arguments, IEnumerable<string> tags, bool isIgnored)
        {
        }

        public void SetTestMethodAsRow(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle, string exampleSetName, string variantName, IEnumerable<KeyValuePair<string, string>> arguments)
        {
        }

        public void SetTestClassIgnore(TestClassGenerationContext generationContext)
        {
        }
    }
}