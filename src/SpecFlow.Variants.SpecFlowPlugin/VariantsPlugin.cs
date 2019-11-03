using SpecFlow.Variants.SpecFlowPlugin;
using SpecFlow.Variants.SpecFlowPlugin.Generator;
using SpecFlow.Variants.SpecFlowPlugin.Providers;
using System.Linq;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Generator.Interfaces;
using TechTalk.SpecFlow.Generator.Plugins;
using TechTalk.SpecFlow.Generator.UnitTestConverter;
using TechTalk.SpecFlow.Generator.UnitTestProvider;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.Utils;

[assembly: GeneratorPlugin(typeof(VariantsPlugin))]
[assembly: RuntimePlugin(typeof(VariantsPlugin))]

namespace SpecFlow.Variants.SpecFlowPlugin
{
    public class VariantsPlugin : IGeneratorPlugin, IRuntimePlugin
    {
        private string _variantKey = "Variant";

        public void Initialize(GeneratorPluginEvents generatorPluginEvents, GeneratorPluginParameters generatorPluginParameters)
        {
            generatorPluginEvents.CustomizeDependencies += CustomizeDependencies;
        }

        public void CustomizeDependencies(object sender, CustomizeDependenciesEventArgs eventArgs)
        {
            // Resolve relevant instances to be used for custom IFeatureGenerator implementation
            var objectContainer = eventArgs.ObjectContainer;
            var language = objectContainer.Resolve<ProjectSettings>().ProjectPlatformSettings.Language;
            var codeDomHelper = objectContainer.Resolve<CodeDomHelper>(language);
            var decoratorRegistry = objectContainer.Resolve<DecoratorRegistry>();

            // Resolve specflow configuration to confirm custom variant key, use default if none provided
            var specflowConfiguration = objectContainer.Resolve<SpecFlowConfiguration>();
            var configParam = specflowConfiguration.Plugins.FirstOrDefault(a => a.Name == GetType().Namespace.Replace(".SpecFlowPlugin", string.Empty))?.Parameters;
            _variantKey = !string.IsNullOrEmpty(configParam) ? configParam : _variantKey;

            // Create custom unit test provider based on user defined config value
            var generatorProvider = GetGeneratorProviderFromConfig(codeDomHelper, specflowConfiguration.UnitTestProvider);

            // Create generator instance to be registered and replace original
            var customFeatureGenerator = new FeatureGeneratorExtended(generatorProvider, codeDomHelper, specflowConfiguration, decoratorRegistry, _variantKey);
            var customFeatureGeneratorProvider = new FeatureGeneratorProviderExtended(customFeatureGenerator);

            // Register dependencies
            objectContainer.RegisterInstanceAs(generatorProvider);
            objectContainer.RegisterInstanceAs(customFeatureGenerator);
            objectContainer.RegisterInstanceAs<IFeatureGeneratorProvider>(customFeatureGeneratorProvider, "default");
        }

        private IUnitTestGeneratorProvider GetGeneratorProviderFromConfig(CodeDomHelper codeDomHelper, string config)
        {
            switch (config)
            {
                case "mstest":
                    return new MsTestProviderExtended(codeDomHelper, _variantKey);
                case "nunit":
                    return new NUnitProviderExtended(codeDomHelper, _variantKey);
                case "xunit":
                    return new XUnitProviderExtended(codeDomHelper, _variantKey);
                default:
                    return new MsTestProviderExtended(codeDomHelper, _variantKey);
            }
        }

        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters)
        {
        }
    }
}