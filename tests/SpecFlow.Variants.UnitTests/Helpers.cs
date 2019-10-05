using Gherkin.Ast;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow.Parser;

namespace SpecFlow.Variants.UnitTests
{
    internal static class Helpers
    {
        public static T GetScenario<T>(this SpecFlowDocument document, string scenarioName) where T : ScenarioDefinition
        {
            return (T)document.SpecFlowFeature.Children.FirstOrDefault(a => a.Name == scenarioName);
        }

        public static IList<Tag> GetTagsByNameStart(this ScenarioDefinition scenario, string tagName)
        {
            return scenario.GetTags().Where(a => a.GetNameWithoutAt().StartsWith(tagName)).ToList();
        }

        public static IList<Tag> GetTagsByNameStart(this Feature feature, string tagName)
        {
            return feature.Tags.Where(a => a.GetNameWithoutAt().StartsWith(tagName)).ToList();
        }

        public static Tag GetTagsByNameExact(this ScenarioDefinition scenario, string tagName)
        {
            return scenario.GetTags().Where(a => a.GetNameWithoutAt() == tagName).FirstOrDefault();
        }

        public static IList<Tag> GetTagsExceptNameStart(this ScenarioDefinition scenario, string tagName)
        {
            return scenario.GetTags().Where(a => !a.GetNameWithoutAt().StartsWith(tagName)).ToList();
        }

        public static IList<TableRow> GetExamplesTableBody(this ScenarioOutline scenario)
        {
            return scenario.Examples.First().TableBody.ToList();
        }

        public static IList<TableCell> GetExamplesTableHeaders(this ScenarioOutline scenario)
        {
            return scenario.Examples.First().TableHeader.Cells.ToList();
        }

        public static IList<CodeTypeMember> GetTestMethods(this CodeNamespace generatedCode, ScenarioDefinition scenario)
        {
            return generatedCode.Types[0].Members.Cast<CodeTypeMember>().Where(a => a.Name.StartsWith(scenario.Name.Replace(" ", "")
                .Replace(",", ""), StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        public static IList<CodeTypeMember> GetRowTestMethods(this CodeNamespace generatedCode, ScenarioDefinition scenario)
        {
            return generatedCode.GetTestMethods(scenario).Where(a => a.CustomAttributes.Count > 0).ToList();
        }

        public static CodeTypeMember GetRowTestBaseMethod(this CodeNamespace generatedCode, ScenarioDefinition scenario)
        {
            return generatedCode.GetTestMethods(scenario).FirstOrDefault(a => a.CustomAttributes.Count == 0);
        }

        public static IList<CodeAttributeDeclaration> GetMethodAttributes(this CodeTypeMember member, string attributeName)
        {
            return member.CustomAttributes.Cast<CodeAttributeDeclaration>().Where(a => a.Name == attributeName).ToList();
        }

        public static IList<CodeParameterDeclarationExpression> GetMethodParameters(this CodeTypeMember member)
        {
            return ((CodeMemberMethod)member).Parameters.Cast<CodeParameterDeclarationExpression>().ToList();
        }

        public static IList<CodeAttributeArgument> GetAttributeArguments(this CodeAttributeArgumentCollection args)
        {
            return args.Cast<CodeAttributeArgument>().ToList();
        }

        public static string GetArgumentValue(this CodeAttributeArgument codeExpression)
        {
            return ((CodePrimitiveExpression)codeExpression.Value).Value.ToString();
        }
    }
}
