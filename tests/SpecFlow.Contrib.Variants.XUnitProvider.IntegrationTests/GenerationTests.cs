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
    }
}
