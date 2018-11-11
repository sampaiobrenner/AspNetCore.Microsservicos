﻿using CasaDoCodigo.Controllers;
using CasaDoCodigo.Models;
using CasaDoCodigo.Models.ViewModels;
using CasaDoCodigo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using MVC.Models;
using Polly.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace MVC.Test
{
    public class CarrinhoControllerTest : BaseControllerTest
    {
        private readonly Mock<IHttpContextAccessor> contextAccessorMock;
        private readonly Mock<IIdentityParser<ApplicationUser>> appUserParserMock;
        private readonly Mock<ILogger<CarrinhoController>> loggerMock;
        private readonly Mock<ICatalogoService> catalogoServiceMock;
        private readonly Mock<ICarrinhoService> carrinhoServiceMock;

        public CarrinhoControllerTest()
        {
            contextAccessorMock = new Mock<IHttpContextAccessor>();
            appUserParserMock = new Mock<IIdentityParser<ApplicationUser>>();
            loggerMock = new Mock<ILogger<CarrinhoController>>();
            catalogoServiceMock = new Mock<ICatalogoService>();
            carrinhoServiceMock = new Mock<ICarrinhoService>();
        }

        #region Index
        [Fact]
        public async Task Index_Success()
        {
            //arrange
            var clienteId = "cliente_id";
            var produtos = GetFakeProdutos();
            var testProduct = produtos[0];
            catalogoServiceMock
                .Setup(c => c.GetProduto(testProduct.Codigo))
                .ReturnsAsync(testProduct)
                .Verifiable();

            var itemCarrinho = new ItemCarrinho(testProduct.Codigo, testProduct.Codigo, testProduct.Nome, testProduct.Preco, 1, testProduct.UrlImagem);
            carrinhoServiceMock
                .Setup(c => c.AddItem(clienteId, It.IsAny<ItemCarrinho>()))
                .ReturnsAsync(
                new CarrinhoCliente(clienteId,
                    new List<ItemCarrinho>
                    {
                        itemCarrinho
                    }))
                .Verifiable();
            var controller = GetCarrinhoController();
            SetControllerUser(clienteId, controller);

            //act
            var result = await controller.Index(testProduct.Codigo);

            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<CarrinhoCliente>(viewResult.Model);
            Assert.Equal(model.Itens[0].ProdutoNome, produtos[0].Nome);
        }

        [Fact]
        public async Task Index_Without_Product_Success()
        {
            //arrange
            var clienteId = "cliente_id";
            var produtos = GetFakeProdutos();
            var testProduct = produtos[0];

            var itemCarrinho = new ItemCarrinho(testProduct.Codigo, testProduct.Codigo, testProduct.Nome, testProduct.Preco, 1, testProduct.UrlImagem);
            carrinhoServiceMock
                .Setup(c => c.GetCarrinho(clienteId))
                .ReturnsAsync(
                new CarrinhoCliente(clienteId,
                    new List<ItemCarrinho>
                    {
                        itemCarrinho
                    }))
                .Verifiable();

            var controller = GetCarrinhoController();
            SetControllerUser(clienteId, controller);

            //act
            var result = await controller.Index();

            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<CarrinhoCliente>(viewResult.Model);
            Assert.Equal(model.Itens[0].ProdutoNome, produtos[0].Nome);
        }

        [Fact]
        public async Task Index_BrokenCircuitException()
        {
            //arrange
            var clienteId = "cliente_id";
            var produtos = GetFakeProdutos();
            var testProduct = produtos[0];
            catalogoServiceMock
                .Setup(c => c.GetProduto(It.IsAny<string>()))
                .ThrowsAsync(new BrokenCircuitException())
                .Verifiable();

            var itemCarrinho = new ItemCarrinho(testProduct.Codigo, testProduct.Codigo, testProduct.Nome, testProduct.Preco, 1, testProduct.UrlImagem);
            carrinhoServiceMock
                .Setup(c => c.AddItem(clienteId, It.IsAny<ItemCarrinho>()))
                .ReturnsAsync(
                new CarrinhoCliente(clienteId,
                    new List<ItemCarrinho>
                    {
                        itemCarrinho
                    }))
                .Verifiable();

            var controller = GetCarrinhoController();
            SetControllerUser(clienteId, controller);

            //act
            var result = await controller.Index(testProduct.Codigo);

            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);
            Assert.True(!string.IsNullOrWhiteSpace(controller.ViewBag.MsgServicoIndisponivel as string));
        }

        [Fact]
        public async Task Index_ProductNotFound()
        {
            //arrange
            var clienteId = "cliente_id";
            var produtos = GetFakeProdutos();
            var testProduct = produtos[0];
            catalogoServiceMock
                .Setup(c => c.GetProduto(testProduct.Codigo))
                .ReturnsAsync((Produto)null)
                .Verifiable();

            var controller = GetCarrinhoController();
            SetControllerUser(clienteId, controller);

            //act
            var result = await controller.Index(testProduct.Codigo);

            //assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ProdutoNaoEncontrado", redirectToActionResult.ActionName);
            Assert.Equal("Carrinho", redirectToActionResult.ControllerName);
            Assert.Equal(redirectToActionResult.Fragment, testProduct.Codigo);
        }
        #endregion

        #region UpdateQuantidade
        [Fact]
        public async Task UpdateQuantidade_Success()
        {
            //arrange
            var clienteId = "cliente_id";
            var controller = GetCarrinhoController();
            SetControllerUser(clienteId, controller);
            var itemCarrinho = GetFakeItemCarrinho();
            UpdateQuantidadeInput updateQuantidadeInput = new UpdateQuantidadeInput("001", 7);
            carrinhoServiceMock
                .Setup(c => c.UpdateItem(clienteId, It.IsAny<UpdateQuantidadeInput>()))
                .ReturnsAsync(new UpdateQuantidadeOutput(itemCarrinho, new CarrinhoCliente()));

            //act
            var result = await controller.UpdateQuantidade(updateQuantidadeInput);

            //assert
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<UpdateQuantidadeOutput>(okObjectResult.Value);

        }

        [Fact]
        public async Task UpdateQuantidade_Invalid_ProdutoId()
        {
            //arrange
            var clienteId = "cliente_id";
            UpdateQuantidadeInput updateQuantidadeInput = new UpdateQuantidadeInput(null, 7);
            carrinhoServiceMock
                .Setup(c => c.UpdateItem(clienteId, It.IsAny<UpdateQuantidadeInput>()))
                .ReturnsAsync(new UpdateQuantidadeOutput(new ItemCarrinho(), new CarrinhoCliente()));

            var controller = GetCarrinhoController();
            SetControllerUser(clienteId, controller);
            controller.ModelState.AddModelError("ProdutoId", "Required");

            //act
            var result = await controller.UpdateQuantidade(updateQuantidadeInput);

            //assert
            var badRequestObjectResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badRequestObjectResult.Value);
        }

        [Fact]
        public async Task UpdateQuantidade_ProdutoId_NotFound()
        {
            //arrange
            var clienteId = "cliente_id";
            UpdateQuantidadeInput updateQuantidadeInput = new UpdateQuantidadeInput("001", 7);
            carrinhoServiceMock
                .Setup(c => c.UpdateItem(clienteId, It.IsAny<UpdateQuantidadeInput>()))
                .ReturnsAsync((UpdateQuantidadeOutput)null);

            var controller = GetCarrinhoController();
            SetControllerUser(clienteId, controller);

            //act
            var result = await controller.UpdateQuantidade(updateQuantidadeInput);

            //assert
            var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(updateQuantidadeInput, notFoundObjectResult.Value);
        }
        #endregion

        #region Checkout
        [Fact]
        public async Task Checkout_success()
        {
            //arrange
            var carrinho = GetCarrinhoController();

            //act
            IActionResult actionResult = await carrinho.Checkout(new Cadastro());

            //assert
            ViewResult viewResult = Assert.IsType<ViewResult>(actionResult);
        }

        [Fact]
        public async Task Checkout_Invalid_Cadastro()
        {
            //arrange
            var carrinho = GetCarrinhoController();
            carrinho.ModelState.AddModelError("Email", "Required");

            //act
            IActionResult actionResult = await carrinho.Checkout(new Cadastro());

            //assert
            RedirectToActionResult redirectToActionResult = Assert.IsType<RedirectToActionResult>(actionResult);
            redirectToActionResult.ControllerName = "CarrinhoController";
            redirectToActionResult.ActionName = "Checkout";
        }

        [Fact]
        public async Task Checkout_Service_Error()
        {
            //arrange
            carrinhoServiceMock
                .Setup(c => c.Checkout(It.IsAny<string>(), It.IsAny<CadastroViewModel>()))
                .ThrowsAsync(new Exception());
            var controller = GetCarrinhoController();

            //act
            IActionResult actionResult = await controller.Checkout(new Cadastro());

            //assert
            ViewResult viewResult = Assert.IsType<ViewResult>(actionResult);
            loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);
            Assert.True(!string.IsNullOrWhiteSpace(controller.ViewBag.MsgServicoIndisponivel as string));
        }

        [Fact]
        public async Task Checkout_Service_BrokenCircuitException()
        {
            //arrange
            carrinhoServiceMock
                .Setup(c => c.Checkout(It.IsAny<string>(), It.IsAny<CadastroViewModel>()))
                .ThrowsAsync(new BrokenCircuitException());
            var controller = GetCarrinhoController();

            //act
            IActionResult actionResult = await controller.Checkout(new Cadastro());

            //assert
            ViewResult viewResult = Assert.IsType<ViewResult>(actionResult);
            loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);
            Assert.True(!string.IsNullOrWhiteSpace(controller.ViewBag.MsgServicoIndisponivel as string));
        }
        #endregion

        private ItemCarrinho GetFakeItemCarrinho()
        {
            var produtos = GetFakeProdutos();
            var testProduct = produtos[0];
            var itemCarrinho = new ItemCarrinho(testProduct.Codigo, testProduct.Codigo, testProduct.Nome, testProduct.Preco, 7, testProduct.UrlImagem);
            return itemCarrinho;
        }

        private static void SetControllerUser(string clienteId, CarrinhoController controller)
        {
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { new Claim("sub", clienteId) }
                ));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private CarrinhoController GetCarrinhoController()
        {
            return new CarrinhoController(contextAccessorMock.Object, appUserParserMock.Object, loggerMock.Object, catalogoServiceMock.Object, carrinhoServiceMock.Object);
        }
    }
}
