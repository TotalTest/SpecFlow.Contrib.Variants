using SpecFlow.Contrib.Variants;
using SpecFlow.Contrib.Variants.Generator;
using SpecFlow.Contrib.Variants.Providers;
using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Generator.CodeDom;
using TechTalk.SpecFlow.Generator.Interfaces;
using TechTalk.SpecFlow.Generator.Plugins;
using TechTalk.SpecFlow.Generator.UnitTestConverter;
using TechTalk.SpecFlow.Generator.UnitTestProvider;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.UnitTestProvider;

[assembly: GeneratorPlugin(typeof(VariantsPlugin))]

namespace SpecFlow.Contrib.Variants
{
    public class VariantsPlugin : IGeneratorPlugin
    {
        private string _variantKey = "Variant";
        private string utp;
        //private Configuration _config;

        public void Initialize(GeneratorPluginEvents generatorPluginEvents, GeneratorPluginParameters generatorPluginParameters, UnitTestProviderConfiguration unitTestProviderConfiguration)
        {
            utp = unitTestProviderConfiguration.UnitTestProvider;
            generatorPluginEvents.CustomizeDependencies += CustomizeDependencies;
        }

        public void CustomizeDependencies(object sender, CustomizeDependenciesEventArgs eventArgs)
        {
            // Resolve relevant instances to be used for custom IFeatureGenerator implementation
            var objectContainer = eventArgs.ObjectContainer;
            var language = objectContainer.Resolve<ProjectSettings>().ProjectPlatformSettings.Language;
            var codeDomHelper = objectContainer.Resolve<CodeDomHelper>(language);
            var decoratorRegistry = objectContainer.Resolve<DecoratorRegistry>();

            // Get variant key from config
            var projSettings = eventArgs.ObjectContainer.Resolve<ProjectSettings>();
            if (projSettings.ConfigurationHolder.HasConfiguration && projSettings.ConfigurationHolder.ConfigSource == ConfigSource.Json)
            {
                // TODO: use this once dependency is resolved ->  _config = JsonConvert.DeserializeObject<Configuration>(projSettings.ConfigurationHolder.Content);
                var vk = GetJsonValueByRegex(projSettings.ConfigurationHolder.Content, "variantkey");
                _variantKey = !string.IsNullOrEmpty(vk) ? vk : _variantKey;
            }
            else if (projSettings.ConfigurationHolder.HasConfiguration && projSettings.ConfigurationHolder.ConfigSource == ConfigSource.AppConfig)
            {
                var appconfig = projSettings.ConfigurationHolder.Content;
                var vk = GetGeneratorPath(appconfig);
                _variantKey = !string.IsNullOrEmpty(vk) ? vk : _variantKey;
            }

            // Create custom unit test provider based on user defined config value
            //TODO: use this once dependency is resolved -> var generatorProvider = GetGeneratorProviderFromConfig(codeDomHelper, utp ?? _config?.PluginParameters?.UnitTestProvider ?? "");
            // https://github.com/dotnet/sdk/issues/9594

            if (string.IsNullOrEmpty(utp))
            {
                var c = objectContainer.Resolve<UnitTestProviderConfiguration>();
                utp = c.UnitTestProvider;

                if (string.IsNullOrEmpty(utp))
                    throw new Exception("Unit test provider not detected, please install as a nuget package described here: https://github.com/SpecFlowOSS/SpecFlow/wiki/SpecFlow-and-.NET-Core");
            }

            //TODO: remove after proving => var generatorProvider = GetGeneratorProviderFromConfig(codeDomHelper, utp ?? GetJsonValueByRegex(projSettings.ConfigurationHolder.Content, "unittestprovider"));
            var generatorProvider = GetGeneratorProviderFromConfig(codeDomHelper, utp);
            var specflowConfiguration = eventArgs.SpecFlowProjectConfiguration.SpecFlowConfiguration;

            // Create generator instance to be registered and replace original
            var customFeatureGenerator = new FeatureGeneratorExtended(generatorProvider, codeDomHelper, specflowConfiguration, decoratorRegistry, _variantKey);
            var customFeatureGeneratorProvider = new FeatureGeneratorProviderExtended(customFeatureGenerator);

            // Register dependencies
            objectContainer.RegisterInstanceAs(generatorProvider);
            objectContainer.RegisterInstanceAs(customFeatureGenerator);
            objectContainer.RegisterInstanceAs<IFeatureGeneratorProvider>(customFeatureGeneratorProvider, "default");
        }

        //private string GetUnitTestProviderFromConfig(string config)
        //{
        //    var reg = new Regex(@"(?<=unittestprovider\""\:\"").+?(?=\"")", RegexOptions.IgnoreCase);
        //    var match = reg.Match(config.Replace(" ", ""));
        //    return match.Success ? match.Value : "";
        //}

        //private string GetUnitVariantKeyFromConfig(string config)
        //{
        //    var reg = new Regex(@"(?<=variantkey\""\:\"").+?(?=\"")", RegexOptions.IgnoreCase);
        //    var match = reg.Match(config.Replace(" ", ""));
        //    return match.Success ? match.Value : "";
        //}

        private string GetJsonValueByRegex(string config, string key)
        {
            var reg = new Regex($@"(?<={key}\""\:\"").+?(?=\"")", RegexOptions.IgnoreCase);
            var match = reg.Match(config?.Replace(" ", "") ?? "");
            return match.Success ? match.Value : "";
        }

        private IUnitTestGeneratorProvider GetGeneratorProviderFromConfig(CodeDomHelper codeDomHelper, string config) =>
            config switch
            {
                "mstest" => new MsTestProviderExtended(codeDomHelper, _variantKey),
                "nunit" => new NUnitProviderExtended(codeDomHelper, _variantKey),
                "xunit" => new XUnitProviderExtended(codeDomHelper, _variantKey),
                _ => new MsTestProviderExtended(codeDomHelper, _variantKey),
            };

        private string GetGeneratorPath(string config)
        {
            var browser = XElement.Parse(config);
            var el = browser.Element("generator");
            var attribute = el?.Attribute("path").Value ?? string.Empty;

            return attribute.StartsWith("variantkey", StringComparison.InvariantCultureIgnoreCase)
                ? Regex.Replace(attribute, "variantkey:", "", RegexOptions.IgnoreCase) : string.Empty;
        }
    }
}

    //public class Configuration
    //{
    //    public PluginParameters PluginParameters { get; set; }
    //}

    //public class PluginParameters
    //{
    //    public string VariantKey { get; set; }
    //    public string UnitTestProvider { get; set; }
    //}