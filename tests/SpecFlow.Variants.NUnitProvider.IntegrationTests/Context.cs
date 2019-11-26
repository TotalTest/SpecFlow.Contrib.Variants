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
            _scenarioContext.Add("Namespace", GetType().Namespace);

            /// <summary>
            /// Example of accessing variant via NUnit TestContext
            /// </summary>
            //var cats = TestContext.CurrentContext.Test.Properties["Category"];
            //var browser = cats?.FirstOrDefault(a => a.ToString().StartsWith("Browser"))?.ToString().Split(':')[1];
        }
    }
}
