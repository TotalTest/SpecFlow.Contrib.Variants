using NUnit.Framework;
using System.IO;
using System.Linq;

namespace SpecFlow.Contrib.Variants.Core.NUnitTestProvider.IntegrationTests
{
    [TestFixture]
    public class GenerationTests
    {
        [Test]
        public void NUnit_GeneratedFeatures_CustomGenerationIsApplied()
        {
            var curDir = Directory.GetCurrentDirectory();
            var features = Directory.GetParent(curDir).Parent.Parent.GetFiles().Where(a => a.FullName.EndsWith(".feature.cs")).ToList();

            var result = features.All(a => File.ReadLines(a.FullName).Any(line => line.Contains("// Generation customised by SpecFlow.Contrib.Variants")));

            Assert.IsTrue(result);
        }
    }
}
