using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace SpecFlow.Contrib.Variants.MsTestProvider.IntegrationTests
{
    [Binding]
    public class Context
    {
        private readonly TestContext _testContext;

        public Context(TestContext testContext)
        {
            _testContext = testContext;
        }

        [BeforeScenario]
        public void Before()
        {
            /// <summary>
            /// Example of accessing variant via MsTest TestContext
            /// </summary>
            //var browser = _testContext.Properties["Browser"];
        }
    }
}
