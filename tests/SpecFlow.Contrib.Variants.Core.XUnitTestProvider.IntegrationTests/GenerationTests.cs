using System.IO;
using System.Linq;
using Xunit;

namespace SpecFlow.Contrib.Variants.Core.XUnitTestProvider.IntegrationTests
{
    public class GenerationTests
    {
        [Fact]
        public void XUnitTest_GeneratedFeatures_CustomGenerationIsApplied()
        {
            var curDir = Directory.GetCurrentDirectory();
            var features = Directory.GetParent(curDir).Parent.Parent.GetFiles().Where(a => a.FullName.EndsWith(".feature.cs")).ToList();

            var result = features.All(a => File.ReadLines(a.FullName).Any(line => line.Contains("// Generation customised by SpecFlow.Contrib.Variants")));

            Assert.True(result);
        }
    }
}
