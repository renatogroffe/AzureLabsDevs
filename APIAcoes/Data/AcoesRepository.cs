using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using StackExchange.Redis;
using APIAcoes.Models;

namespace APIAcoes.Data
{
    public class AcoesRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ConnectionMultiplexer _conexaoRedis;
        private readonly TelemetryConfiguration _telemetryConfig;

        public AcoesRepository(
            IConfiguration configuration,
            ConnectionMultiplexer conexaoRedis,
            TelemetryConfiguration telemetryConfig)
        {
            _configuration = configuration;
            _conexaoRedis = conexaoRedis;
            _telemetryConfig = telemetryConfig;
        }

        public CotacaoAcao Get(string codigo)
        {
            DateTimeOffset inicio = DateTime.Now;
            Stopwatch watch = new Stopwatch();
            watch.Start();

            string strDadosAcao =
                _conexaoRedis.GetDatabase().StringGet(
                    $"{_configuration["Redis:PrefixoChave"]}-{codigo}");

            watch.Stop();
            TelemetryClient client = new TelemetryClient(_telemetryConfig);
            client.TrackDependency(
                "Redis", "Get", strDadosAcao, inicio, watch.Elapsed, true);

            if (!String.IsNullOrWhiteSpace(strDadosAcao))
                return JsonSerializer.Deserialize<CotacaoAcao>(
                    strDadosAcao,
                    new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
            else
                return null;
        }
        public List<CotacaoAcao> GetAll()
        {
            using (var conexao = new SqlConnection(_configuration["BaseAcoes"]))
            {
                return conexao.Query<CotacaoAcao>(
                    "SELECT Codigo, DataReferencia AS Data, " +
                    "CodCorretora, NomeCorretora, Valor " +
                    "FROM dbo.HistoricoAcoes " +
                    "ORDER BY DataReferencia DESC"
                ).AsList();
            }
        }
    }
}