using TechTalk.SpecFlow;

namespace SpecFlow.Contrib.Variants.XUnitProvider.IntegrationTests
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
            // TODO: remove when safe
            //_scenarioContext.Add("Namespace", GetType().Namespace);
        }
    }
}
