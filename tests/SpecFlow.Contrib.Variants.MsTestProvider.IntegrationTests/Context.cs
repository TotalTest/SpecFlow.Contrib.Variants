using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace SpecFlow.Contrib.Variants.MsTestProvider.IntegrationTests
{
    [Binding]
    public class Context
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly TestContext _testContext;

        public Context(ScenarioContext scenarioContext, TestContext testContext)
        {
            _scenarioContext = scenarioContext;
            _testContext = testContext;
        }

        [BeforeScenario]
        public void Before()
        {
            _scenarioContext.Add("Namespace", GetType().Namespace);

            /// <summary>
            /// Example of accessing variant via MsTest TestContext
            /// </summary>
            //var browser = _testContext.Properties["Browser"];
        }
    }
}
