using System.IO;
using System.Linq;
using Xunit;

namespace SpecFlow.Contrib.Variants.XUnitProvider.IntegrationTests
{
    public class GenerationTests
    {
        [Fact]
        public void XUnitTest_FrameworkGeneratedFeatures_CustomGenerationIsApplied()
        {
            var curDir = Directory.GetCurrentDirectory();
            var features = Directory.GetParent(curDir).Parent.GetFiles().Where(a => a.FullName.EndsWith(".feature.cs")).ToList();

            var result = features.All(a => File.ReadLines(a.FullName).Any(line => line.Contains("// Generation customised by SpecFlow.Contrib.Variants")));

            Assert.True(result);
        }

        [Fact]
        public void XUnit_GeneratedFeatures_NonParallelAttributeIsApplied()
        {
            var curDir = Directory.GetCurrentDirectory();
            var feature = Directory.GetParent(curDir).Parent.GetFiles().First(a => a.FullName.EndsWith("XUnitNonParallelTests.feature.cs"));

            var result = File.ReadLines(feature.FullName).Any(line => line.Contains("[Xunit.CollectionAttribute(\"SpecFlowNonParallelizableFeatures\")]"));

            Assert.True(result);
        }
    }
}
