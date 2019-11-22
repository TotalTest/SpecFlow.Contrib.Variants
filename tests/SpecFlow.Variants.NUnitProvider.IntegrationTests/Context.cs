using NUnit.Framework;
using System.Linq;
using TechTalk.SpecFlow;

namespace SpecFlow.Variants.NUnitProvider.IntegrationTests
{
    [Binding]
    public class Context
    {
        private readonly ScenarioContext _scenarioContext;

        public Context(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [BeforeScenario]
        public void Before()
        {
            var cats = TestContext.CurrentContext.Test.Properties["Category"];
            var browser = cats?.FirstOrDefault(a => a.ToString().StartsWith("Browser"))?
                .ToString().Split(':')[1];

            _scenarioContext.Add("Browser", browser);
            _scenarioContext.Add("Namespace", GetType().Namespace);
        }
    }
}
