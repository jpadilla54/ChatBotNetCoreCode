using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ChatBot.Models;
using ApiAiSDK;
using System.Text;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;

namespace ChatBot.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string fbToken = "EAAItAL3k9J0BAHcPmPzKUC0eaupNAB7hnBTZBiG3b0HM5fxbYmquSkxvjNmm6YZBhN3QRvBLupzJcIDNRza0hPPZC9xiuSCg94zBjZCWZAukZA9oJRmLnBxoMQUNuUHeWdZBhJpLX5hBHgwwfKvfR9LU6GcrrGQfkQS29EiZCVBrbdeKS23S4RSH";
        private string postUrl = "https://graph.facebook.com/v2.6/me/messages";

        [HttpGet]
        public string Webhook(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.verify_token")] string verify_token)
        {
            var json = Request.Query;

            if (verify_token.Equals("my_token_is_great"))
            {
                return challenge;
            }
            else
            {
                return "";
            }
        }

        [HttpPost]
        public void Webhook()
        {
            var json = (dynamic)null;
            try
            {
                using (StreamReader sr = new StreamReader(this.Request.Body))
                {
                    json = sr.ReadToEnd();
                }
                dynamic data = JsonConvert.DeserializeObject(json);
                postToFB((string)data.entry[0].messaging[0].sender.id, (string)data.entry[0].messaging[0].message.text);
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public void postToFB(string recipientId, string messageText)
        {
            //Post to ApiAi
            string messageTextAnswer = postApiAi(messageText);
            string postParameters = string.Format("access_token={0}&recipient={1}&message={2}", fbToken, "{ id:" + recipientId + "}", "{ text:\"" + messageTextAnswer + "\"}");
            //Response from ApiAI or answer to FB question from user post it to   FB back.
            var client = new HttpClient();
            client.PostAsync(postUrl, new StringContent(postParameters, Encoding.UTF8, "application/json"));
        }

        private string apiAiToken = "836a19f128b74dda831caed476073a8d";
        public String postApiAi(string messageText)
        {
            var config = new AIConfiguration(apiAiToken,
                                             SupportedLanguage.English);
            ApiAi apiAi = new ApiAi(config);
            var response = apiAi.TextRequest(messageText);
            return response.Result.Fulfillment.Speech;
        }
    }
}
