using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using APIAcoes.Models;
using APIAcoes.Clients;
using APIAcoes.Data;

namespace APIAcoes.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AcoesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AcoesController> _logger;
        private readonly AcoesRepository _repository;

        public AcoesController(IConfiguration configuration,
            ILogger<AcoesController> logger,
            AcoesRepository repository)
        {
            _configuration = configuration;
            _logger = logger;
            _repository = repository;
        }

        [HttpPost]
        [ProducesResponseType(typeof(Resultado), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public Resultado Post(
            [FromServices]NotificacaoSlackClient notificacaoSlackClient,
            Acao acao)
        {
            var cotacaoAcao = new CadastroAcao()
            {
                Codigo = acao.Codigo,
                Valor = acao.Valor,
                CodCorretora = _configuration["Corretora:Codigo"],
                NomeCorretora = _configuration["Corretora:Nome"]
            };
            var conteudoAcao = JsonSerializer.Serialize(cotacaoAcao);
            _logger.LogInformation($"Dados: {conteudoAcao}");

            string nomeQueue = _configuration["AzureQueueStorage:Queue"];
 
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(_configuration["AzureQueueStorage:Connection"]);
            CloudQueueClient queueClient =
                storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(nomeQueue);

            if (queue.CreateIfNotExists())
                _logger.LogInformation($"Criada a fila {nomeQueue} no Azure Storage");

            queue.AddMessage(new CloudQueueMessage(conteudoAcao));
            _logger.LogInformation(
               $"Azure Queue Storge - Envio para a fila {nomeQueue} concluído | " +
                conteudoAcao);
            notificacaoSlackClient.PostAlerta(acao.Codigo, acao.Valor.Value);

            return new Resultado()
            {
                Mensagem = "Informações de ação enviadas com sucesso!"
            };
        }

        [HttpGet("{codigo}")]
        [ProducesResponseType(typeof(CotacaoAcao), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public ActionResult<CotacaoAcao> GetCotacao(string codigo)
        {
            if (String.IsNullOrWhiteSpace(codigo))
            {
                _logger.LogError(
                    $"GetCotacao - Codigo de Acao nao informado");
                return new BadRequestObjectResult(new
                {
                    Sucesso = false,
                    Mensagem = "Código de Ação não informado"
                });
            }

            _logger.LogInformation($"GetCotacao - codigo da Acao: {codigo}");
            CotacaoAcao acao = null;
            if (!String.IsNullOrWhiteSpace(codigo))
                acao = _repository.Get(codigo.ToUpper());

            if (acao != null)
            {
                _logger.LogInformation(
                    $"GetCotacao - Acao: {codigo} | Valor atual: {acao.Valor} | Ultima atualizacao: {acao.Data}");
                return new OkObjectResult(acao);
            }
            else
            {
                _logger.LogError(
                    $"GetCotacao - Codigo de Acao nao encontrado: {codigo}");
                return new NotFoundObjectResult(new
                {
                    Sucesso = false,
                    Mensagem = $"Código de Ação não encontrado: {codigo}"
                });
            }
        }

        [HttpGet]
        public ActionResult<List<CotacaoAcao>> GetAll()
        {
            var dados = _repository.GetAll();
            _logger.LogInformation($"GetAll - encontrado(s) {dados.Count} registro(s)");
            return dados;
        }
    }
}