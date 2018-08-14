﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaDoCodigo.API.Model;
using CasaDoCodigo.Models;
using CasaDoCodigo.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CasaDoCodigo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidoController : BaseApiController
    {
        private readonly IPedidoRepository pedidoRepository;

        public PedidoController(ILogger<CarrinhoController> logger,
            IPedidoRepository pedidoRepository) : base(logger)
        {
            this.pedidoRepository = pedidoRepository;
        }

        // GET: api/Pedido
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Pedido/5
        [HttpGet("{id}", Name = "Get")]
        public async Task<PedidoViewModel> Get(int id)
        {
            try
            {
                Pedido pedido = await pedidoRepository.GetPedido();
                PedidoViewModel viewModel = new PedidoViewModel(pedido);
                return viewModel;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message, "GetPedido");
                throw;
            }
        }

        // POST: api/Pedido
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Pedido/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}