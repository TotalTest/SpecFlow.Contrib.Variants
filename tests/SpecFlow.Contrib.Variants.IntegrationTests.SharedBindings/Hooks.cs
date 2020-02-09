using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TechTalk.SpecFlow;

namespace SpecFlow.Contrib.Variants.IntegrationTests.SharedBindings
{
    [Binding]
    public sealed class Hooks
    {
        private readonly ScenarioContext _scenarioContext;
        private string _baseDir;
        private IWebDriver _driver;

        public Hooks(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            _scenarioContext.TryGetValue("Browser", out var browser);
            var ns = _scenarioContext["Namespace"].ToString().ToLowerInvariant();
            _baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLowerInvariant();
            var driverDir = _baseDir.Replace(ns, GetType().Namespace.ToLowerInvariant());

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

            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            _scenarioContext.ScenarioContainer.RegisterInstanceAs(_driver);
        }

        private IWebDriver SetupChromeDriver(string driverDir)
        {
            var co = new ChromeOptions();
            co.AddArguments("--incognito");
            co.AddArgument("headless");
            return new ChromeDriver(driverDir, co);
        }

        private IWebDriver SetupFirefoxDriver(string driverDir)
        {
            var fo = new FirefoxOptions();
            fo.SetPreference("marionette", true);
            fo.AddArgument("--headless");
            fo.AddArgument("--private");
            return new FirefoxDriver(driverDir, fo);
        }

        [AfterScenario]
        public void After()
        {
            if (_scenarioContext.TestError != null)
            {
                var path = Path.Combine(_baseDir, "Screenshots");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                ((ITakesScreenshot)_driver).GetScreenshot().SaveAsFile($@"{path}\{_scenarioContext.ScenarioInfo.Title}.jpg", ScreenshotImageFormat.Jpeg);
            }

            _driver.Dispose();
            _scenarioContext.ScenarioContainer.Dispose();
        }

        [AfterTestRun]
        public static void AfterRun()
        {
            var processes = Process.GetProcessesByName("geckodriver").ToList();
            processes.AddRange(Process.GetProcessesByName("chromedriver"));

            foreach (var process in processes)
                process.Kill();
        }
    }
}
