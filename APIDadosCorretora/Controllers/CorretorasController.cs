using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using APIDadosCorretora.Models;

namespace APIDadosCorretora.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CorretorasController : ControllerBase
    {
        [HttpGet]
        public Corretora Get(
            [FromServices] IConfiguration configuration,
            [FromServices]ILogger<CorretorasController> logger)
        {
            var corretora = new Corretora()
            {
                Codigo = configuration["Corretora:Codigo"],
                Nome = configuration["Corretora:Nome"]
            };

            logger.LogInformation($"Dados: {JsonSerializer.Serialize(corretora)}");

            return corretora;
        }     
    }
}