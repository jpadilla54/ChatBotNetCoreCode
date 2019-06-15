using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ChatBot.Models;
using ApiAiSDK;
using ApiAiSDK.Model;
using System.Text;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using Nancy.Json;

namespace ChatBot.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string fbToken = "EAAItAL3k9J0BAGFmfo5jcDVcaTZAZBUPP4vWXz1Nd5jwy1yH5dI9iLRm2xR9RlrOs4sIs5TjFYUr2ie2KZBphZB3wRIqfXXJhwLrqP1bJ2rxI9f924HGAaFJ8vSTBf18cFJalesQxcMk93SS8mKxs2f0ZCoR0tnaW0wRd73RArRXoRvjq5KOk";
        private string postUrl = "https://graph.facebook.com/v2.8/me/messages";
        private string apiAiToken = "836a19f128b74dda831caed476073a8d";

        [HttpGet]
        public string Webhook(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.verify_token")] string verify_token)
        {
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
            AIResponse response = postApiAi(messageText);
            var action = response.Result.Action;
            var parameter = response.Result.Parameters;
            string messageTextAnswer = response.Result.Fulfillment.Speech;


            if (action != "creartutoria" && action != "consultartutoria")
            {
                EnviarMessenger(recipientId, messageTextAnswer);
            }

            if (action == "creartutoria")
            {
                if ((parameter.Where(p => p.Key == "Clases").FirstOrDefault().Value.ToString()) == null)
                {
                    EnviarMessenger(recipientId, messageTextAnswer);
                    return;
                }
                if ((parameter.Where(p => p.Key == "date").FirstOrDefault().Value) == "")
                {
                    EnviarMessenger(recipientId, messageTextAnswer);
                    return;
                }
                if ((parameter.Where(p => p.Key == "time").FirstOrDefault().Value) == "")
                {
                    EnviarMessenger(recipientId, messageTextAnswer);
                    return;
                }

                string clases = parameter.Where(p => p.Key == "Clases").FirstOrDefault().Value.ToString();
                DateTime date = Convert.ToDateTime(parameter.Where(p => p.Key == "date").FirstOrDefault().Value);
                DateTime time = Convert.ToDateTime(parameter.Where(p => p.Key == "time").FirstOrDefault().Value);
                int dia = Convert.ToInt32(date.ToString("dd"));
                int mes = Convert.ToInt32(date.ToString("MM"));
                int anio = Convert.ToInt32(date.ToString("yy"));
                int hora = Convert.ToInt32(time.ToString("hh"));
                int minutos = Convert.ToInt32(time.ToString("mm"));
                DateTime fechatutoria = new DateTime(anio, mes, dia, hora, minutos, 0);
                CrearTutorias creartutoria = new CrearTutorias();

                creartutoria.nombre = recipientId;
                creartutoria.fecha = fechatutoria;
                creartutoria.Idclase = 1;

                var client = new HttpClient();
                string uri = "https://tutoriaswebapp.azurewebsites.net/api/tutoria/AgregarTutoria";
                var jsonInString = JsonConvert.SerializeObject(creartutoria);
                try
                {
                    client.PostAsync(uri, new StringContent(jsonInString, Encoding.UTF8, "application/json"));
                }
                catch (Exception ex)
                {
                    EnviarMessenger(recipientId, "Hubo un error al crear la tutoria");
                    return;
                }
                EnviarMessenger(recipientId, messageTextAnswer);
            }

            if (action == "consultartutoria")
            {
                string clases = parameter.Where(p => p.Key == "Clases").FirstOrDefault().Value.ToString();
                DateTime date = Convert.ToDateTime(parameter.Where(p => p.Key == "date").FirstOrDefault().Value);
                DateTime time = Convert.ToDateTime(parameter.Where(p => p.Key == "time").FirstOrDefault().Value);


                if (clases == null)
                {
                    EnviarMessenger(recipientId, messageTextAnswer);
                }
                if (date == null)
                {
                    EnviarMessenger(recipientId, messageTextAnswer);
                }
                if (time == null)
                {
                    EnviarMessenger(recipientId, messageTextAnswer);
                }

                int dia = Convert.ToInt32(date.ToString("dd"));
                int mes = Convert.ToInt32(date.ToString("MM"));
                int anio = Convert.ToInt32(date.ToString("yy"));
                int hora = Convert.ToInt32(time.ToString("hh"));
                int minutos = Convert.ToInt32(time.ToString("mm"));

                DateTime fechatutoria = new DateTime(anio, mes, dia, hora, minutos, 0);


                Tutoria tutoria = new Tutoria();

                tutoria.nombre = recipientId;
                tutoria.fecha = fechatutoria;
                tutoria.Idclase = 1;

                var client = new HttpClient();
                string uri = "https://tutoriaswebapp.azurewebsites.net/api/tutoria";
                var jsonInString = JsonConvert.SerializeObject(tutoria);
                client.PostAsync(uri, new StringContent(jsonInString, Encoding.UTF8, "application/json"));

            }
        }

        public void EnviarMessenger(string recipientId, string messageTextAnswer)
        {
            string postParameters = string.Format("access_token={0}&recipient={1}&message={2}", fbToken, "{ id:" + recipientId + "}", "{ text:\"" + messageTextAnswer + "\"}");
            var client = new HttpClient();
            client.PostAsync(postUrl, new StringContent(postParameters, Encoding.UTF8, "application/json"));
        }

        public AIResponse postApiAi(string messageText)
        {
            var config = new AIConfiguration(apiAiToken,
                                             SupportedLanguage.Spanish);
            ApiAi apiAi = new ApiAi(config);
            var response = apiAi.TextRequest(messageText);

            return response;
        }
    }
}


