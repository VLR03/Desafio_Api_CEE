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

    private int CalcularIdade(DateTime dataNascimento)
    {
        var hoje = DateTime.Today;
        var idade = hoje.Year - dataNascimento.Year;
        if (dataNascimento.Date > hoje.AddYears(-idade))
        {
            idade--;
        }
        return idade;
    }

    private bool ValidarSenha(string senha, DateTime dataNascimento)
    {
        if (senha.Length != 6)
        {
            return false;
        }

        var senhaNumerica = 0;
        if (int.TryParse(senha, out senhaNumerica) && senhaNumerica == int.Parse(dataNascimento.ToString("yyMMdd")))
        {
            return false;
        }

        var sequenciaNumerica = "01234567890";
        if (sequenciaNumerica.Contains(senha) || senha.Distinct().Count() <= 1)
        {
            return false;
        }

        return true;
    }

    private string GerarNumeroCartao()
    {
        var rnd = new Random();

        var prefixo = rnd.Next(1000, 9999).ToString();

        var primeirosnumerosMeio = "";
        for (var i = 0; i < 4; i++)
        {
            var grupo = rnd.Next(1000, 9999);
            primeirosnumerosMeio = grupo.ToString() + " ";
        }

        var ultimossnumerosMeio = "";
        for (var i = 0; i < 4; i++)
        {
            var grupo2 = rnd.Next(1000, 9999);
            ultimossnumerosMeio = grupo2.ToString() + " ";
        }

        var ultimosDigitos = rnd.Next(1000, 9999).ToString();

        var numeroCartao = prefixo + " " + primeirosnumerosMeio + ultimossnumerosMeio + ultimosDigitos;
        return numeroCartao;
    }

    [HttpPost("solicitar")]
    public async Task<ActionResult<Produto>> SolicitarCartao([FromBody] Produto produto)
    {
        produto.Status = "SOLICITADO";

        if (produto == null)
        {
            return BadRequest("Dados do produto não fornecidos.");
        }

        if (string.IsNullOrEmpty(produto.Agencia))
        {
            return BadRequest("A agência é obrigatória para solicitar o cartão.");
        }

        if (string.IsNullOrEmpty(produto.Conta))
        {
            return BadRequest("A conta é obrigatória para solicitar o cartão.");
        }

        if (string.IsNullOrEmpty(produto.Cpf))
        {
            return BadRequest("O CPF é obrigatório para solicitar o cartão.");
        }

        if (produto.DataNasc == null || CalcularIdade(produto.DataNasc.Value) < 18)
        {
            return BadRequest("A solicitação de cartão só pode ser feita por maiores de 18 anos.");
        }

        if (string.IsNullOrEmpty(produto.NomeCompleto))
        {
            return BadRequest("O nome completo é obrigatório para solicitar o cartão.");
        }

        if (string.IsNullOrEmpty(produto.NomeCartao))
        {
            return BadRequest("O nome para o cartão é obrigatório para solicitar o cartão.");
        }

        if (produto.Bandeira != "Mastercard" && produto.Bandeira != "Visa")
        {
            return BadRequest("A opção de bandeira do cartão é inválida. Escolha entre Mastercard ou Visa.");
        }

        if (produto.Tipo != "PLATINUM" && produto.Tipo != "GOLD" && produto.Tipo != "BLACK DIAMOND")
        {
            return BadRequest("A opção de tipo de cartão é inválida. Escolha entre PLATINUM, GOLD ou BLACK DIAMOND.");
        }

        if (produto.DataVenc != "5" && produto.DataVenc != "10" && produto.DataVenc != "15" && produto.DataVenc != "25")
        {
            return BadRequest("A opção de data de vencimento é inválida. Escolha entre 5, 10, 15 ou 25.");
        }

        if (string.IsNullOrEmpty(produto.Senha) || string.IsNullOrEmpty(produto.SenhaConfirm))
        {
            return BadRequest("A senha e a confirmação de senha são obrigatórias para solicitar o cartão.");
        }

        if (produto.Senha != produto.SenhaConfirm)
        {
            return BadRequest("A senha e a confirmação de senha não correspondem.");
        }

        if (!ValidarSenha(produto.Senha, produto.DataNasc.Value))
        {
            return BadRequest("A senha deve ter 6 dígitos que não correspondam à data de nascimento do cliente, sem números repetidos ou sequenciais.");
        }

        produto.Status = "ENTREGUE";
        produto.NumeroCartao = GerarNumeroCartao();

        try
        {
            await _produtoServices.CreateAsync(produto);

            return Ok(new
            {
                NumeroCartao = produto.NumeroCartao,
                NomeImpresso = produto.NomeCartao.ToUpper(),
                DataVencimento = produto.DataVenc,
                InstrucoesAtivacao = "Digite a senha previamente cadastrada para ativar o cartão."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao solicitar o cartão.");
        }
    }

    [HttpPut("ativar")]
    public async Task<IActionResult> AtivarCartao(string numeroCartao, string agencia, string conta, string senha)
    {

        var cartao = await _produtoServices.GetAsyncByNumeroCartao(numeroCartao);
        if (cartao == null)
        {
            return NotFound("O número do cartão não foi encontrado");
        }

        // Verifique se os dados do cartão são válidos
        if (cartao.Agencia != agencia || cartao.Conta != conta)
        {
            return BadRequest("Dados do cartão inválidos");
        }

        // Verifique se a senha está correta
        if (cartao.Senha != senha)
        {
            return Unauthorized("Senha incorreta");
        }

        // Verifique o status do cartão
        if (cartao.Status != "ENTREGUE")
        {
            return BadRequest("O cartão não pode ser ativado");
        }

        // Realize as ações necessárias para ativar o cartão
        // Por exemplo, atualize o status do cartão para "ATIVO"
        cartao.Status = "ATIVO";
        await _produtoServices.UpdateAsync(numeroCartao, cartao);

        return Ok("Cartão ativado com sucesso");
    }

    [HttpPost("bloquear")]
    public async Task<IActionResult> BloquearCartao(string numeroCartao, string agencia, string conta, string senha, string motivo)
    {
        // Implemente as validações e lógica para bloquear o cartão aqui

        // Exemplo de código:
        var cartao = await _produtoServices.GetAsync(numeroCartao);
        if (cartao == null)
        {
            return NotFound();
        }

        // Realize as ações necessárias para bloquear o cartão

        return Ok("Cartão bloqueado com sucesso");
    }

    [HttpPost("cancelar")]
    public async Task<IActionResult> CancelarCartao(string numeroCartao, string agencia, string conta, string senha, string motivo)
    {
        // Implemente as validações e lógica para cancelar o cartão aqui

        // Exemplo de código:
        var cartao = await _produtoServices.GetAsync(numeroCartao);
        if (cartao == null)
        {
            return NotFound();
        }

        // Realize as ações necessárias para cancelar o cartão

        return Ok("Cartão cancelado com sucesso");
    }

    [HttpGet("{numeroCartao}")]
    public async Task<IActionResult> ConsultarCartao(string numeroCartao)
    {
        // Implemente a lógica para consultar o cartão aqui

        // Exemplo de código:
        var cartao = await _produtoServices.GetAsync(numeroCartao);
        if (cartao == null)
        {
            return NotFound();
        }

        return Ok(cartao); // Ou retorne as informações relevantes do cartão
    }
}
