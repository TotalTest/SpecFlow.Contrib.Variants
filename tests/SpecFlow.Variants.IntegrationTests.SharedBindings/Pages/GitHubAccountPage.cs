using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace SpecFlow.Variants.IntegrationTests.SharedBindings.Pages
{
    public class GitHubAccountPage
    {
        public string CurrentUrl => WaitForPage();

        private readonly IWebDriver _driver;
        private readonly By _repos = By.ClassName("repo");
        private readonly By _logo = By.ClassName("octicon-mark-github");

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

        private string WaitForPage()
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
            wait.Until((a) => a.FindElement(_logo).Displayed);
            return _driver.Url;
        }
    }
}
