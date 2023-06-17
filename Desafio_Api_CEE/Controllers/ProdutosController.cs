using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Desafio_Api_CEE.Models;
using Desafio_Api_CEE.Services;

namespace Desafio_Api_CEE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProdutosController : ControllerBase
{
    private readonly ProdutoServices _produtoServices;

    public ProdutosController(ProdutoServices produtoServices)
    {
        _produtoServices = produtoServices;
    }

    [HttpGet]
    public async Task<List<Produto>> GetProdutos() =>
        await _produtoServices.GetAsync();

    [HttpPost]
    public async Task<Produto> PostProduto(Produto produto)
    {
        await _produtoServices.CreateAsync(produto);

        return produto;
    }

}
