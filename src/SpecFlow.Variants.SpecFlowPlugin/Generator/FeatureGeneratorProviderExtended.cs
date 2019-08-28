using TechTalk.SpecFlow.Generator.UnitTestConverter;
using TechTalk.SpecFlow.Parser;

namespace SpecFlow.Variants.SpecFlowPlugin.Generator
{
    internal class FeatureGeneratorProviderExtended : IFeatureGeneratorProvider
    {
        private readonly IFeatureGenerator _unitTestFeatureGenerator;

        public int Priority => int.MaxValue;

        public FeatureGeneratorProviderExtended(IFeatureGenerator unitTestFeatureGenerator)
        {
            _unitTestFeatureGenerator = unitTestFeatureGenerator;
        }

        public bool CanGenerate(SpecFlowDocument document)
        {
            return true;
        }

        public IFeatureGenerator CreateGenerator(SpecFlowDocument document)
        {
            return _unitTestFeatureGenerator;
        }
    }
}