using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using TechTalk.SpecFlow;

namespace SpecFlow.Variants.IntegrationTests.SharedBindings
{
    [Binding]
    public sealed class Hooks
    {
        private readonly ScenarioContext _scenarioContext;
        private IWebDriver _driver;

        public Hooks(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            var browser = _scenarioContext["Browser"];
            var ns = _scenarioContext["Namespace"].ToString().ToLowerInvariant();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLowerInvariant();
            var driverDir = baseDir.Replace(ns, GetType().Namespace.ToLowerInvariant());

            switch (browser)
            {
                case "Chrome":
                    _driver = SetupChromeDriver(driverDir);
                    break;
                case "Firefox":
                    _driver = SetupFirefoxDriver(driverDir);
                    break;
                default:
                    _driver = SetupChromeDriver(driverDir);
                    break;
            }

            _driver.Manage().Window.Maximize();
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            _scenarioContext.ScenarioContainer.RegisterInstanceAs(_driver);
        }

        private IWebDriver SetupChromeDriver(string driverDir)
        {
            var co = new ChromeOptions();
            co.AddArgument("headless");
            return new ChromeDriver(driverDir, co);
        }

        private IWebDriver SetupFirefoxDriver(string driverDir)
        {
            var fo = new FirefoxOptions();
            fo.SetPreference("marionette", true);
            fo.AddArgument("--headless");
            return new FirefoxDriver(driverDir, fo);
        }

        [AfterScenario]
        public void After()
        {
            if (_scenarioContext.TestError != null)
            {
                ((ITakesScreenshot)_driver).GetScreenshot().SaveAsFile($@"C:\Users\prab\Desktop\{_scenarioContext.ScenarioInfo.Title}.jpg", ScreenshotImageFormat.Jpeg);
            }
            
            _driver.Dispose();
        }
    }
}
