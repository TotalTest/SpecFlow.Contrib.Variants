using OpenQA.Selenium;
using System.Linq;

namespace SpecFlow.Variants.IntegrationTests.SharedBindings.Pages
{
    public class GitHubAccountPage
    {
        public string CurrentUrl => _driver.Url;

        private readonly IWebDriver _driver;
        private readonly By _repos = By.ClassName("repo");

        public GitHubAccountPage(IWebDriver driver)
        {
            _driver = driver;
        }

        public void Navigate(string account)
        {
            _driver.Navigate().GoToUrl($"https://github.com/{account}");
        }

        public void SelectRepo(string repo)
        {
            _driver.FindElements(_repos).First(a => a.Text == repo).Click();
        }
    }
}
