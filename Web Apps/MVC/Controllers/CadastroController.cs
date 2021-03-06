﻿using MVC.Models;
using MVC.Models.ViewModels;
using MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVC.Model.Redis;
using MVC.SignalR;
using Polly.CircuitBreaker;
using System;
using System.Threading.Tasks;

namespace MVC.Controllers
{
    public class CadastroController : BaseController
    {
        private readonly IIdentityParser<ApplicationUser> appUserParser;

        public CadastroController(
            IIdentityParser<ApplicationUser> appUserParser,
            ILogger<CadastroController> logger,
            IUserRedisRepository repository)
            : base(logger, repository)
        {
            this.appUserParser = appUserParser;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            await CheckUserCounterData();

            try
            {
                var usuario = appUserParser.Parse(HttpContext.User);
                CadastroViewModel cadastro
                    = new CadastroViewModel()
                    {
                        Bairro = usuario.Bairro,
                        CEP = usuario.CEP,
                        Complemento = usuario.Complemento,
                        Email = usuario.Email,
                        Endereco = usuario.Endereco,
                        Municipio = usuario.Municipio,
                        Nome = usuario.Nome,
                        Telefone = usuario.Telefone,
                        UF = usuario.UF
                    };

                return View(cadastro);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                HandleException();
            }
            return View();
        }
    }
}
