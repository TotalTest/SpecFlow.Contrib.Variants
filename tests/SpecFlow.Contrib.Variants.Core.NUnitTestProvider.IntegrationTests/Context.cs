using TechTalk.SpecFlow;

namespace SpecFlow.Contrib.Variants.Core.NUnitTestProvider.IntegrationTests
{
    [Binding]
    public class Context
    {
        [BeforeScenario]
        public void Before()
        {
            /// <summary>
            /// Example of accessing variant via NUnit TestContext
            /// </summary>
            //var cats = TestContext.CurrentContext.Test.Properties["Category"];
            //var browser = cats?.FirstOrDefault(a => a.ToString().StartsWith("Browser"))?.ToString().Split(':')[1];
        }
    }
}
