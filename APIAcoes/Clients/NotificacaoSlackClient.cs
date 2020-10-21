using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace APIAcoes.Clients
{
    public class NotificacaoSlackClient
    {
        private HttpClient _client;
 
        public NotificacaoSlackClient(HttpClient client,
            IConfiguration configuration)
        {
            _client = client;
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            _client.BaseAddress = new Uri(configuration["UrlLogicAppSlack"]);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

        }

        public void PostAlerta(string codAcao, double vlAcao)
        {
            var requestMessage =
                  new HttpRequestMessage(HttpMethod.Post, String.Empty);

            requestMessage.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    codigoAcao = codAcao,
                    valor = vlAcao
                }), Encoding.UTF8, "application/json");

            var respLogicApp = _client
                .SendAsync(requestMessage).Result;
            respLogicApp.EnsureSuccessStatusCode();
        }
    }
}