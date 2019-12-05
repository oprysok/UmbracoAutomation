using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UmbracoAutomation.CLI
{
    public class WebDriverHelper
    {
        public void PublishNodeSelenium(string siteUrl, int nodeId, string user, string password, bool includeChildren = false, int timeOutSeconds = 600, bool visible = false)
        {
            PublishNodeSelenium(siteUrl, new int[] { nodeId }, user, password, includeChildren, timeOutSeconds, visible);
        }

        public void PublishNodeSelenium(string siteUrl, int[] nodeIds, string user, string password, bool includeChildren = false, int timeOutSeconds = 600, bool visible = false)
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");
            IWebDriver driver = new ChromeDriver(chromeOptions);
            var consoleMessage = string.Empty;

            try
            {
                driver.Navigate().GoToUrl($"{siteUrl}/umbraco");
                var name = WaitForElement(driver, By.Name("username"), timeoutSeconds: 10);
                name.SendKeys(user);
                var pass = WaitForElement(driver, By.Name("password"));
                pass.SendKeys(password);
                var login = WaitForElement(driver, By.XPath("//localize[@key=\"general_login\"]"));
                pass.SendKeys(Keys.Return);
                // login.Click();
                var usr = WaitForElement(driver, By.Id("section-avatar"), false, 180);
                nodeIds.ToList().ForEach(n =>
                {
                    consoleMessage = n.ToString();
                    driver.Navigate().GoToUrl($"{siteUrl}/umbraco/dialogs/publish.aspx?id={n.ToString()}");
                    var checkbox = WaitForElement(driver, By.Id("publishAllCheckBox"));
                    if (includeChildren)
                        checkbox.Click();
                    var publish = driver.FindElement(By.Id("ok"));
                    publish.Click();
                    var message = WaitForElement(driver, By.Id("feedbackMsg"), true, timeOutSeconds);
                    var div = message.FindElement(By.TagName("div"));
                    consoleMessage += ": " + div.Text;
                    if (!div.GetAttribute("class").Contains("success"))
                    {
                        throw new Exception("Publish failed for " + n.ToString());
                    }
                });
                driver.Quit();
            }
            catch (Exception ex)
            {
                driver.Quit();
                throw ex;
            }
            Console.Out.WriteLine(consoleMessage);
        }

        public IWebElement WaitForElement(IWebDriver driver, By locator, bool visible = false, int timeoutSeconds = 60)
        {
            try
            {
                new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds)).Until(ExpectedConditions.ElementExists(locator));
                if (visible)
                    new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds)).Until(ExpectedConditions.ElementIsVisible(locator));
            }
            catch (Exception ex)
            {
                Console.WriteLine(locator.ToString());
                throw ex;
            }
            return driver.FindElement(locator);
        }
    }
}
