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
            //esta variable la use para ver que manda el webhook de messenger para verificar el token
            //var json = Request.Query;

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

        //metodo de prueba para emular mensajes de facebook pero en local
        //se consulta asi en el url /Home/Intent
        public void Intent()
        {
            string text = "crear tutoria para mate discretas el lunes a las 4 pm";
            postApiAiPrueba(text);
        }

        public void postApiAiPrueba(string messageText)
        {
            //Post to ApiAi
            AIResponse response = postApiAi(messageText);
            var action = response.Result.Action;
            var parameter = response.Result.Parameters;
            string speech = response.Result.Fulfillment.Speech;

            if (action != "creartutoria" && action!="consultartutoria")
            {
                //sino es ningun action significa que debemos enviar la repuesta a messenger
                string respuestaamessenger = speech;
            }

            if (action == "creartutoria")
            {
                string clases = parameter.Where(p => p.Key == "Clases").FirstOrDefault().Value.ToString();
                DateTime date = Convert.ToDateTime(parameter.Where(p => p.Key == "time").FirstOrDefault().Value);
                if (clases == null)
                {
                    //mandamos a messenger el text
                }
                if (date == null)
                {
                    //mandamos a messenger el text
                }

                //sino mandamos el post a la api de tutorias
                var datep = date;
                string f = clases;
            }

            if (action == "consultartutoria")
            {
                string clases = parameter.Where(p => p.Key == "Clases").FirstOrDefault().Value.ToString();
                DateTime date = Convert.ToDateTime(parameter.Where(p => p.Key == "time").FirstOrDefault().Value);
                if (clases == null)
                {
                    //mandamos a messenger el text
                }
                if (date == null)
                {
                    //mandamos a messenger el text
                }

                //sino mandamos el post a la api de tutorias


                var datep = date;
                string f = clases;
            }
        }

        public void postToFB(string recipientId, string messageText)
        {
            //Post to ApiAi
            AIResponse response = postApiAi(messageText);
            var action = response.Result.Action;
            var parameter = response.Result.Parameters;
            string speech = response.Result.Fulfillment.Speech;
            string messageTextAnswer = response.Result.Fulfillment.Speech;


            //aqui empezamos a evaluar si viene un action para crear o consultar
            //se evalua sino es ningun action y se va directo a messenger
            if (action != "creartutoria" && action != "consultartutoria")
            {
                //Si no es ningun action posible entonces son respuestas a preguntas desde APiAi y se van directo a messenger
                //Post to messenger
                EnviarMessenger(recipientId, messageTextAnswer);
            }

            //se evalua si es para crear tutoria
            if (action == "creartutoria")
            {
                //se lee los parametros del diccionario de paramtros y se asignan a variables
                string clases = parameter.Where(p => p.Key == "Clases").FirstOrDefault().Value.ToString();
                DateTime date = Convert.ToDateTime(parameter.Where(p => p.Key == "time").FirstOrDefault().Value);
                if (clases == null)
                {
                    //mandamos a messenger el text de que hace falta
                    EnviarMessenger(recipientId, messageTextAnswer);
                }
                if (date == null)
                {
                    //mandamos a messenger el text
                    EnviarMessenger(recipientId, messageTextAnswer);
                }

                //sino mandamos el post a la api de tutorias para crearla con esos parametros



                //variables para ver que reciben los parametros en breakpoints, no se ocupan
                var datep = date;
                string f = clases;
            }

            if (action == "consultartutoria")
            {
                string clases = parameter.Where(p => p.Key == "Clases").FirstOrDefault().Value.ToString();
                DateTime date = Convert.ToDateTime(parameter.Where(p => p.Key == "time").FirstOrDefault().Value);
                if (clases == null)
                {
                    //mandamos a messenger el text
                    EnviarMessenger(recipientId, messageTextAnswer);
                }
                if (date == null)
                {
                    //mandamos a messenger el text
                    EnviarMessenger(recipientId, messageTextAnswer);
                }

                //sino mandamos a consultar a la api de tutorias

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

    
