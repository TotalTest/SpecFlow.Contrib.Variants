using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace SpecFlow.Contrib.Variants.MsTestProvider.IntegrationTests
{
    [TestClass]
    public class GenerationTests
    {
        [TestMethod]
        public void MsTest_FrameworkGeneratedFeatures_CustomGenerationIsApplied()
        {
            var curDir = Directory.GetCurrentDirectory();
            var features = Directory.GetParent(curDir).Parent.GetFiles().Where(a => a.FullName.EndsWith(".feature.cs")).ToList();

            var result = features.All(a => File.ReadLines(a.FullName).Any(line => line == "// Generation customised by SpecFlow.Contrib.Variants v3.9.90-pre.1"));

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MsTest_GeneratedFeatures_NonParallelAttributeIsApplied()
        {
            var curDir = Directory.GetCurrentDirectory();
            var feature = Directory.GetParent(curDir).Parent.GetFiles().First(a => a.FullName.EndsWith("MsTestNonParallelTests.feature.cs"));

            var result = File.ReadLines(feature.FullName).Any(line => line.Contains("[Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelizeAttribute()]"));

            Assert.IsTrue(result);
        }
    }
}
