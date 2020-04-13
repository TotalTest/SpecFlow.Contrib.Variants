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

            var result = features.All(a => File.ReadLines(a.FullName).Any(line => line.Contains("// Generation customised by SpecFlow.Contrib.Variants")));

            Assert.IsTrue(result);
        }
    }
}
