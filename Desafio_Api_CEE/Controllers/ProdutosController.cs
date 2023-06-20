using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Desafio_Api_CEE.Models;
using Desafio_Api_CEE.Services;
using System.Diagnostics;

namespace Desafio_Api_CEE.Controllers
{
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

        private bool ValidarSenha(Produto newProduto)
        {
            var dataNascimento = newProduto.DataNasc.Value;
            var ano = dataNascimento.Year.ToString().Substring(2);
            var mes = dataNascimento.Month.ToString().PadLeft(2, '0');
            var dia = dataNascimento.Day.ToString().PadLeft(2, '0');

            var idade = DateTime.Today.Year - dataNascimento.Year;
            var senha = newProduto.Senha.ToString();
            var senhaConfirm = newProduto.SenhaConfirm.ToString();

            if (idade >= 18 && senha.Length == 6 && senha != (ano + mes + dia) && int.TryParse(senha, out int senhaNumerica) && senha.Distinct().Count() == 6)
            {
                bool possuiSequencia = false;
                for (int i = 0; i < senha.Length - 1; i++)
                {
                    if (senha[i] + 1 == senha[i + 1])
                    {
                        possuiSequencia = true;
                        break;
                    }
                }
                return !possuiSequencia;
            }

            return false;
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
        public async Task<ActionResult<Produto>> Solicitar([FromBody] Produto produto)
        {
            Random rnd = new Random();
            var checarSenha = ValidarSenha(produto);

            if (DateTime.Today.Year - produto.DataNasc.Value.Year < 18)
                return BadRequest("É obrigatório ter 18 ou mais de idade para solicitar um cartão.");

            if (produto.Bandeira != "Mastercard" && produto.Bandeira != "Visa")
            {
                return BadRequest("A bandeira do cartão deve ser: 'Mastercard' ou 'Visa'.");
            }

            if (produto.DataVenc != "5" && produto.DataVenc != "10" && produto.DataVenc != "15" && produto.DataVenc != "20")
            {
                return BadRequest("A Data de Vencimento deve ser: '5', '10', '15', ou '20'.");
            }

            if (produto.Tipo != "PLATINUM" && produto.Tipo != "GOLD" && produto.Tipo != "BLACK" && produto.Tipo != "DIAMOND")
            {
                return BadRequest("O Tipo do cartão deve ser: 'PLATINUM', 'GOLD', 'BLACK' ou 'DIAMOND'.");
            }
            else
            {
                switch (produto.Tipo)
                {
                    case "GOLD":
                        produto.Limite = "R$1.500,00";
                        break;
                    case "PLATINUM":
                        produto.Limite = "R$15.000,00";
                        break;
                    case "BLACK":
                        produto.Limite = "R$30.000,00";
                        break;
                    case "DIAMOND":
                        produto.Limite = "ILIMITADO";
                        break;
                }
            }

            if (!checarSenha)
            {
                return BadRequest("Por favor, insira uma senha de 6 dígitos que cumpra com os seguintes requisitos:" +
                                  "\n1. Não corresponda a sua data de nascimento" +
                                  "\n2. Não possua números repetidos" +
                                  "\n3. Não possua números em sequencia.");
            }
            else
            {
                //produto.Status = "ENTREGUE"
                produto.Cvv = rnd.Next(100, 1000).ToString();
                produto.NumeroCartao = GerarNumeroCartao();

                try
                {
                    await _produtoServices.CreateAsync(produto);

                    return Ok("ID do seu Cartão: " + produto.Id + "\n" + 
                              "Número do seu Cartão: " + produto.NumeroCartao + "\n" + 
                              "Nome a ser impresso: " + produto.NomeCartao + "\n" + 
                              "Data de Vencimento: " + produto.DataVenc + " anos" + "\n" + 
                              "\nPara ativar o seu cartão, realize as seguintes tarefas:\n" +
                              "Utilize o serviço 'Entregar' e insira os seguintes dados: " +
                              "Id, Número do cartão, Agência, Conta e senha --> " + produto.Senha +
                              "\nUtilize o serviço 'Ativar' e insira os seguintes dados: " +
                              "Id, Número do cartão, Agência, Conta e Senha --> " + produto.Senha);
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao solicitar o cartão.");

                }
            }
        }

        [HttpPut("Entregar")]
        public async Task<IActionResult> Entregar(string id, string numeroCartao, string agencia, string conta, string senha)
        {
            var cartao = await _produtoServices.GetAsync(id);

            if (cartao.Conta == conta && cartao.Senha == senha &&
                cartao.NumeroCartao == numeroCartao && cartao.Agencia == agencia)
            {
                cartao.Status = "ENTREGUE";
                await _produtoServices.UpdateAsync(id, cartao);
                return Ok("Cartão foi entregue com sucesso!");
            }
            else
            {
                return BadRequest("Cartão não encontrado. Verifique se as informações inseridas préviamente estão corretas.");
            }
        }

        [HttpPut("ativar")]
        public async Task<IActionResult> Ativar(string id, string numeroCartao, string agencia, string conta, string senha)
        {

            var cartao = await _produtoServices.GetAsync(id);

            if (cartao.NumeroCartao.ToString() == numeroCartao && cartao.Agencia.ToString() == agencia &&
                cartao.Conta.ToString() == conta && cartao.Senha.ToString() == senha)
            {
                if (cartao.Status == "ENTREGUE")
                {
                    cartao.Status = "ATIVO";
                    await _produtoServices.UpdateAsync(id, cartao);
                    return Ok("Parabéns! Seu cartão foi ativado com sucesso.");
                }
                else
                {
                    return BadRequest("O Status do seu cartão não foi registrado como entregue. Utilize o serviço 'entregar' e em seguida ative seu cartão.");
                }

            }
            else
            {
                return BadRequest("Não foi possível encontrar o seu cartão. Verifique se as informações inseridas préviamente estão corretas.");
            }
        }

        [HttpPost("bloquear")]
        public async Task<IActionResult> Bloquear(string id,string numeroCartao, string agencia, string conta, string senha, string motivo)
        {
            var cartao = await _produtoServices.GetAsync(id);

            if(cartao.NumeroCartao == numeroCartao && cartao.Agencia.ToString() == agencia &&
               cartao.Conta.ToString() == conta && cartao.Senha.ToString() == senha)
            {
                if (cartao.Status == "ATIVO")
                {
                    if (motivo == "Perda" || motivo == "Roubo" || motivo == "Danificado")
                    {
                        cartao.Status = motivo;
                        await _produtoServices.UpdateAsync(id, cartao);
                        return Ok("Seu cartão foi bloqueado com sucesso!");
                    }
                    else
                    {
                        return BadRequest("Justifique o motivo do bloqueio com: 'Perda', 'Roubo' ou 'Danificado'.");
                    }
                }
                else
                {
                    return BadRequest("O bloqueio só pode ser efetuado em cartões com o status categorizados como 'ATIVO'.");
                }
            }
            else
            {
                return BadRequest("Não foi possível encontrar o seu cartão. Verifique se as informações inseridas préviamente estão corretas.");
            }
        }

        [HttpPost("cancelar")]
        public async Task<IActionResult> Cancelar(string id, string numeroCartao, string agencia, string conta, string senha)
        {
            var cartao = await _produtoServices.GetAsync(id);

            if (cartao.NumeroCartao == numeroCartao && cartao.Agencia == agencia &&
                cartao.Conta == conta && cartao.Senha == senha)
            {
                cartao.Status = "CANCELADO";
                await _produtoServices.UpdateAsync(id, cartao);
                return Ok("O cancelamento do seu cartão foi efetuado com sucesso!");
            }
            else
            {
                return BadRequest("Não foi possível encontrar o seu cartão. Verifique se as informações inseridas préviamente estão corretas.");
            }
        }

        [HttpGet("Consultar")]
        public async Task<IActionResult> Consultar(string numeroCartao)
        {
            var cartao = await _produtoServices.GetAsyncByNumeroCartao(numeroCartao);

            if(cartao is null)
            {
                return BadRequest("Não foi possível encontrar o seu cartão. Verifique se as informações inseridas préviamente estão corretas.");
            }

            if(cartao.Status == "BLOQUEADO")
            {
                return BadRequest("Seu cartão está bloqueado. Se desejar desbloqueá-lo, entre em contato com a sua agência.");
            }

            if(cartao.Status == "CANCELADO")
            {
                return BadRequest("Seu cartão foi cancelado. Se você acredita que essa informação está errada, entre em contato com a sua agência.");
            }
            else
            {
                return Ok("Número do cartão: " + cartao.NumeroCartao + ".\nNome: " + cartao.NomeCartao + "\nLimite: " + cartao.Limite +
                          "\nCVV: " + cartao.Cvv + "\nStatus: " + cartao.Status + "\nData de Vencimento: " + cartao.DataVenc);
            }
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var cartao = await _produtoServices.GetAsync(id);

            if (cartao is null)
            {
                return NotFound();
            }

            await _produtoServices.RemoveAsync(cartao.Id!);

            return NoContent();
        }
    }
}
