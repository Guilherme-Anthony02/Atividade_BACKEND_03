using AplicacaoBackAula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
    public class EntradaEstoqueRequest
    {
        public int IdItem { get; set; }
        public int Quantidade { get; set; }
        public DateOnly DataEntrada { get; set; }
        public decimal ValorUnitario { get; set; }
        public string Unidade { get; set; } = "UN";
    }

    [ApiController]
    [Route("api/[controller]")]
    public class MovimentacaoEstoqueController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;
        public MovimentacaoEstoqueController(BdAulaBackendContext context) => _context = context;

        [HttpPost("entrada")]
        public async Task<IActionResult> RegistrarEntrada([FromBody] EntradaEstoqueRequest dto)
        {
            if (dto.IdItem <= 0 || dto.Quantidade <= 0 || dto.ValorUnitario <= 0 ||
                string.IsNullOrWhiteSpace(dto.Unidade))
                return BadRequest(new { mensagem = "Informe produto, quantidade positiva, data, unidade e valor unitário positivo." });

            var produto = await _context.Produtos.FirstOrDefaultAsync(p => p.Iditem == dto.IdItem);
            if (produto == null) return BadRequest(new { mensagem = "O ID informado não corresponde a um produto." });

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var estoque = await _context.Estoques.FirstOrDefaultAsync(e => e.Iditem == dto.IdItem);
                if (estoque == null)
                {
                    estoque = new Estoque { Iditem = dto.IdItem, QuantidadeEstoque = 0 };
                    _context.Estoques.Add(estoque);
                }
                estoque.QuantidadeEstoque += dto.Quantidade;

                var entrada = new EntradaProduto {
                    Iditem = dto.IdItem,
                    DataEntradaProduto = dto.DataEntrada,
                    QuantidadeEntrada = dto.Quantidade,
                    ValorUnitarioEntrada = dto.ValorUnitario,
                    Unidade = dto.Unidade
                };
                _context.EntradaProdutos.Add(entrada);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { mensagem = "Entrada registrada e estoque atualizado.", idEntrada = entrada.IdentradaProduto, quantidadeEstoque = estoque.QuantidadeEstoque });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { mensagem = "Falha ao registrar entrada de estoque.", erro = ex.Message });
            }
        }
    }
}
