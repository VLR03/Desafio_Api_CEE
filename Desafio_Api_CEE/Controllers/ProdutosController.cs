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
            return BadRequest("Dados do produto n�o fornecidos.");
        }

        if (string.IsNullOrEmpty(produto.Agencia))
        {
            return BadRequest("A ag�ncia � obrigat�ria para solicitar o cart�o.");
        }

        if (string.IsNullOrEmpty(produto.Conta))
        {
            return BadRequest("A conta � obrigat�ria para solicitar o cart�o.");
        }

        if (string.IsNullOrEmpty(produto.Cpf))
        {
            return BadRequest("O CPF � obrigat�rio para solicitar o cart�o.");
        }

        if (produto.DataNasc == null || CalcularIdade(produto.DataNasc.Value) < 18)
        {
            return BadRequest("A solicita��o de cart�o s� pode ser feita por maiores de 18 anos.");
        }

        if (string.IsNullOrEmpty(produto.NomeCompleto))
        {
            return BadRequest("O nome completo � obrigat�rio para solicitar o cart�o.");
        }

        if (string.IsNullOrEmpty(produto.NomeCartao))
        {
            return BadRequest("O nome para o cart�o � obrigat�rio para solicitar o cart�o.");
        }

        if (produto.Bandeira != "Mastercard" && produto.Bandeira != "Visa")
        {
            return BadRequest("A op��o de bandeira do cart�o � inv�lida. Escolha entre Mastercard ou Visa.");
        }

        if (produto.Tipo != "PLATINUM" && produto.Tipo != "GOLD" && produto.Tipo != "BLACK DIAMOND")
        {
            return BadRequest("A op��o de tipo de cart�o � inv�lida. Escolha entre PLATINUM, GOLD ou BLACK DIAMOND.");
        }

        if (produto.DataVenc != "5" && produto.DataVenc != "10" && produto.DataVenc != "15" && produto.DataVenc != "25")
        {
            return BadRequest("A op��o de data de vencimento � inv�lida. Escolha entre 5, 10, 15 ou 25.");
        }

        if (string.IsNullOrEmpty(produto.Senha) || string.IsNullOrEmpty(produto.SenhaConfirm))
        {
            return BadRequest("A senha e a confirma��o de senha s�o obrigat�rias para solicitar o cart�o.");
        }

        if (produto.Senha != produto.SenhaConfirm)
        {
            return BadRequest("A senha e a confirma��o de senha n�o correspondem.");
        }

        if (!ValidarSenha(produto.Senha, produto.DataNasc.Value))
        {
            return BadRequest("A senha deve ter 6 d�gitos que n�o correspondam � data de nascimento do cliente, sem n�meros repetidos ou sequenciais.");
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
                InstrucoesAtivacao = "Digite a senha previamente cadastrada para ativar o cart�o."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao solicitar o cart�o.");
        }
    }

    [HttpPut("ativar")]
    public async Task<IActionResult> AtivarCartao(string numeroCartao, string agencia, string conta, string senha)
    {

        var cartao = await _produtoServices.GetAsyncByNumeroCartao(numeroCartao);
        if (cartao == null)
        {
            return NotFound("O n�mero do cart�o n�o foi encontrado");
        }

        // Verifique se os dados do cart�o s�o v�lidos
        if (cartao.Agencia != agencia || cartao.Conta != conta)
        {
            return BadRequest("Dados do cart�o inv�lidos");
        }

        // Verifique se a senha est� correta
        if (cartao.Senha != senha)
        {
            return Unauthorized("Senha incorreta");
        }

        // Verifique o status do cart�o
        if (cartao.Status != "ENTREGUE")
        {
            return BadRequest("O cart�o n�o pode ser ativado");
        }

        // Realize as a��es necess�rias para ativar o cart�o
        // Por exemplo, atualize o status do cart�o para "ATIVO"
        cartao.Status = "ATIVO";
        await _produtoServices.UpdateAsync(numeroCartao, cartao);

        return Ok("Cart�o ativado com sucesso");
    }

    [HttpPost("bloquear")]
    public async Task<IActionResult> BloquearCartao(string numeroCartao, string agencia, string conta, string senha, string motivo)
    {
        // Implemente as valida��es e l�gica para bloquear o cart�o aqui

        // Exemplo de c�digo:
        var cartao = await _produtoServices.GetAsync(numeroCartao);
        if (cartao == null)
        {
            return NotFound();
        }

        // Realize as a��es necess�rias para bloquear o cart�o

        return Ok("Cart�o bloqueado com sucesso");
    }

    [HttpPost("cancelar")]
    public async Task<IActionResult> CancelarCartao(string numeroCartao, string agencia, string conta, string senha, string motivo)
    {
        // Implemente as valida��es e l�gica para cancelar o cart�o aqui

        // Exemplo de c�digo:
        var cartao = await _produtoServices.GetAsync(numeroCartao);
        if (cartao == null)
        {
            return NotFound();
        }

        // Realize as a��es necess�rias para cancelar o cart�o

        return Ok("Cart�o cancelado com sucesso");
    }

    [HttpGet("{numeroCartao}")]
    public async Task<IActionResult> ConsultarCartao(string numeroCartao)
    {
        // Implemente a l�gica para consultar o cart�o aqui

        // Exemplo de c�digo:
        var cartao = await _produtoServices.GetAsync(numeroCartao);
        if (cartao == null)
        {
            return NotFound();
        }

        return Ok(cartao); // Ou retorne as informa��es relevantes do cart�o
    }
}
