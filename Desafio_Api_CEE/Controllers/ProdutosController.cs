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
                return BadRequest("� obrigat�rio ter 18 ou mais de idade para solicitar um cart�o.");

            if (produto.Bandeira != "Mastercard" && produto.Bandeira != "Visa")
            {
                return BadRequest("A bandeira do cart�o deve ser: 'Mastercard' ou 'Visa'.");
            }

            if (produto.DataVenc != "5" && produto.DataVenc != "10" && produto.DataVenc != "15" && produto.DataVenc != "20")
            {
                return BadRequest("A Data de Vencimento deve ser: '5', '10', '15', ou '20'.");
            }

            if (produto.Tipo != "PLATINUM" && produto.Tipo != "GOLD" && produto.Tipo != "BLACK" && produto.Tipo != "DIAMOND")
            {
                return BadRequest("O Tipo do cart�o deve ser: 'PLATINUM', 'GOLD', 'BLACK' ou 'DIAMOND'.");
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
                return BadRequest("Por favor, insira uma senha de 6 d�gitos que cumpra com os seguintes requisitos:" +
                                  "\n1. N�o corresponda a sua data de nascimento" +
                                  "\n2. N�o possua n�meros repetidos" +
                                  "\n3. N�o possua n�meros em sequencia.");
            }
            else
            {
                //produto.Status = "ENTREGUE"
                produto.Cvv = rnd.Next(100, 1000).ToString();
                produto.NumeroCartao = GerarNumeroCartao();

                try
                {
                    await _produtoServices.CreateAsync(produto);

                    return Ok("ID do seu Cart�o: " + produto.Id + "\n" + 
                              "N�mero do seu Cart�o: " + produto.NumeroCartao + "\n" + 
                              "Nome a ser impresso: " + produto.NomeCartao + "\n" + 
                              "Data de Vencimento: " + produto.DataVenc + " anos" + "\n" + 
                              "\nPara ativar o seu cart�o, realize as seguintes tarefas:\n" +
                              "Utilize o servi�o 'Entregar' e insira os seguintes dados: " +
                              "Id, N�mero do cart�o, Ag�ncia, Conta e senha --> " + produto.Senha +
                              "\nUtilize o servi�o 'Ativar' e insira os seguintes dados: " +
                              "Id, N�mero do cart�o, Ag�ncia, Conta e Senha --> " + produto.Senha);
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao solicitar o cart�o.");

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
                return Ok("Cart�o foi entregue com sucesso!");
            }
            else
            {
                return BadRequest("Cart�o n�o encontrado. Verifique se as informa��es inseridas pr�viamente est�o corretas.");
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
                    return Ok("Parab�ns! Seu cart�o foi ativado com sucesso.");
                }
                else
                {
                    return BadRequest("O Status do seu cart�o n�o foi registrado como entregue. Utilize o servi�o 'entregar' e em seguida ative seu cart�o.");
                }

            }
            else
            {
                return BadRequest("N�o foi poss�vel encontrar o seu cart�o. Verifique se as informa��es inseridas pr�viamente est�o corretas.");
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
                        return Ok("Seu cart�o foi bloqueado com sucesso!");
                    }
                    else
                    {
                        return BadRequest("Justifique o motivo do bloqueio com: 'Perda', 'Roubo' ou 'Danificado'.");
                    }
                }
                else
                {
                    return BadRequest("O bloqueio s� pode ser efetuado em cart�es com o status categorizados como 'ATIVO'.");
                }
            }
            else
            {
                return BadRequest("N�o foi poss�vel encontrar o seu cart�o. Verifique se as informa��es inseridas pr�viamente est�o corretas.");
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
                return Ok("O cancelamento do seu cart�o foi efetuado com sucesso!");
            }
            else
            {
                return BadRequest("N�o foi poss�vel encontrar o seu cart�o. Verifique se as informa��es inseridas pr�viamente est�o corretas.");
            }
        }

        [HttpGet("Consultar")]
        public async Task<IActionResult> Consultar(string numeroCartao)
        {
            var cartao = await _produtoServices.GetAsyncByNumeroCartao(numeroCartao);

            if(cartao is null)
            {
                return BadRequest("N�o foi poss�vel encontrar o seu cart�o. Verifique se as informa��es inseridas pr�viamente est�o corretas.");
            }

            if(cartao.Status == "BLOQUEADO")
            {
                return BadRequest("Seu cart�o est� bloqueado. Se desejar desbloque�-lo, entre em contato com a sua ag�ncia.");
            }

            if(cartao.Status == "CANCELADO")
            {
                return BadRequest("Seu cart�o foi cancelado. Se voc� acredita que essa informa��o est� errada, entre em contato com a sua ag�ncia.");
            }
            else
            {
                return Ok("N�mero do cart�o: " + cartao.NumeroCartao + ".\nNome: " + cartao.NomeCartao + "\nLimite: " + cartao.Limite +
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
