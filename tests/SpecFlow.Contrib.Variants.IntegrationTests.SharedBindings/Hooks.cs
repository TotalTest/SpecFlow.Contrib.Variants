using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TechTalk.SpecFlow;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace SpecFlow.Contrib.Variants.IntegrationTests.SharedBindings
{
    [Binding]
    public sealed class Hooks
    {
        private readonly ScenarioContext _scenarioContext;
        private string _baseDir;
        private string _driverDir;
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
            _baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLowerInvariant().Replace("net5\\", "");
            _driverDir = _baseDir.Replace(ns, GetType().Namespace.ToLowerInvariant());

            switch (browser)
            {
                case "Chrome":
                    _driver = SetupChromeDriver();
                    break;
                case "Edge":
                    _driver = SetupEdgeDriver();
                    break;
                default:
                    _driver = SetupChromeDriver();
                    break;
            }

            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            _scenarioContext.ScenarioContainer.RegisterInstanceAs(_driver);
        }

        private IWebDriver SetupChromeDriver()
        {
            var co = new ChromeOptions();
            co.AddArgument("headless");
#if DEBUG
            //new DriverManager().SetUpDriver(new ChromeConfig());
            return new ChromeDriver(_driverDir, co);
#else
            var envChromeWebDriver = Environment.GetEnvironmentVariable("ChromeWebDriver");
            return new ChromeDriver(envChromeWebDriver, co);
#endif
        }

        private IWebDriver SetupEdgeDriver()
        {
            var ed = new EdgeOptions();
            //ed.AddArgument("headless");
#if DEBUG
            new DriverManager().SetUpDriver(new EdgeConfig(), VersionResolveStrategy.MatchingBrowser);
            return new EdgeDriver(ed);
#else
            var envEdgeWebDriver = Environment.GetEnvironmentVariable("EdgeWebDriver");
            return new EdgeDriver(envEdgeWebDriver, ed);
#endif
        }

        [AfterScenario]
        public void After()
        {
            if (_scenarioContext.TestError != null)
            {
                var path = Path.Combine(_baseDir, "Screenshots");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                try
                {
                    ((ITakesScreenshot)_driver)?.GetScreenshot().SaveAsFile($@"{path}\{_scenarioContext.ScenarioInfo.Title}.jpg", ScreenshotImageFormat.Jpeg);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected Error trying to take a screen shot");
                    Console.WriteLine($"Error message: {e.Message}");
                }
            }

            try
            {
                _driver?.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected error disposing driver");
                Console.WriteLine($"Error message: {e.Message}");
            }

            _scenarioContext.ScenarioContainer.Dispose();
        }

        [AfterTestRun]
        public static void AfterRun()
        {
            var processes = Process.GetProcessesByName("msedgedriver").ToList();
            processes.AddRange(Process.GetProcessesByName("chromedriver"));

            foreach (var process in processes)
                process.Kill();
        }
    }
}
