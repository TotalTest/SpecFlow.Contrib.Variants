using NUnit.Framework;
using NUnit.Framework.Internal;
using System.IO;
using System.Linq;

namespace SpecFlow.Contrib.Variants.NUnitProvider.IntegrationTests
{
    [TestFixture]
    public class GenerationTests
    {
        [Test]
        public void NUnit_FrameworkGeneratedFeatures_CustomGenerationIsApplied()
        {
            var curDir = AssemblyHelper.GetAssemblyPath(typeof(GenerationTests).Assembly);
            var features = Directory.GetParent(curDir).Parent.Parent.GetFiles().Where(a => a.FullName.EndsWith(".feature.cs")).ToList();

            var result = features.All(a => File.ReadLines(a.FullName).Any(line => line == "// Generation customised by SpecFlow.Contrib.Variants v3.9.90-pre.1"));

            Assert.IsTrue(result);
        }

        [Test]
        public void NUnit_GeneratedFeatures_NonParallelAttributeIsApplied()
        {
            var curDir = AssemblyHelper.GetAssemblyPath(typeof(GenerationTests).Assembly);
            var feature = Directory.GetParent(curDir).Parent.Parent.GetFiles().First(a => a.FullName.EndsWith("NUnitNonParallelTests.feature.cs"));

            var result = File.ReadLines(feature.FullName).Any(line => line.Contains("[NUnit.Framework.NonParallelizableAttribute()]"));

            Assert.IsTrue(result);
        }
    }
}
