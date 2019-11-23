using TechTalk.SpecFlow;

namespace SpecFlow.Variants.XUnitProvider.IntegrationTests
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
            var browser = _scenarioContext["Browser"];
            _scenarioContext.Add("Browser", browser);
            _scenarioContext.Add("Namespace", GetType().Namespace);
        }
    }
}
