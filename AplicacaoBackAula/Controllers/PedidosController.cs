using AplicacaoBackAula.DTO;
using AplicacaoBackAula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public PedidosController(BdAulaBackendContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? situacao)
        {
            var query = _context.Atendimentos.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(situacao))
                query = query.Where(p => p.StatusAtendimento == situacao);

            var pedidos = await query
                .OrderByDescending(p => p.Idatendimento)
                .Select(p => new {
                    idPedido = p.Idatendimento,
                    idCliente = p.Idpessoa,
                    cliente = p.IdpessoaNavigation.Nome,
                    data = p.DataAtendimento,
                    situacao = p.StatusAtendimento,
                    itens = p.Atendimentoitems.Select(i => new {
                        idItem = i.Iditem,
                        produto = i.IditemNavigation.Item1,
                        tipo = i.IditemNavigation.TipoItem,
                        quantidade = i.QuantidadeItens,
                        valorUnitario = i.ValorunitarioEntrada,
                        subtotal = i.QuantidadeItens * i.ValorunitarioEntrada
                    }).ToList(),
                    total = p.Atendimentoitems.Sum(i => i.QuantidadeItens * i.ValorunitarioEntrada)
                }).ToListAsync();

            return Ok(pedidos);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Consultar(int id)
        {
            var pedido = await _context.Atendimentos
                .Where(p => p.Idatendimento == id)
                .Select(p => new {
                    idPedido = p.Idatendimento,
                    idCliente = p.Idpessoa,
                    cliente = p.IdpessoaNavigation.Nome,
                    data = p.DataAtendimento,
                    situacao = p.StatusAtendimento,
                    itens = p.Atendimentoitems.Select(i => new {
                        idItem = i.Iditem,
                        produto = i.IditemNavigation.Item1,
                        tipo = i.IditemNavigation.TipoItem,
                        quantidade = i.QuantidadeItens,
                        valorUnitario = i.ValorunitarioEntrada,
                        subtotal = i.QuantidadeItens * i.ValorunitarioEntrada,
                        funcionario = i.IdfuncionarioNavigation.IdpessoaNavigation.Nome
                    }).ToList(),
                    total = p.Atendimentoitems.Sum(i => i.QuantidadeItens * i.ValorunitarioEntrada)
                }).FirstOrDefaultAsync();

            return pedido == null ? NotFound(new { mensagem = "Pedido não encontrado." }) : Ok(pedido);
        }

        [HttpGet("cliente/{idPessoa:int}")]
        public async Task<IActionResult> ListarDoCliente(int idPessoa)
        {
            var existe = await _context.Pessoas.AnyAsync(p => p.Idpessoa == idPessoa);
            if (!existe) return NotFound(new { mensagem = "Cliente não encontrado." });

            var pedidos = await _context.Atendimentos
                .Where(p => p.Idpessoa == idPessoa)
                .OrderByDescending(p => p.Idatendimento)
                .Select(p => new {
                    idPedido = p.Idatendimento,
                    idCliente = p.Idpessoa,
                    data = p.DataAtendimento,
                    situacao = p.StatusAtendimento,
                    itens = p.Atendimentoitems.Select(i => new {
                        idItem = i.Iditem,
                        produto = i.IditemNavigation.Item1,
                        tipo = i.IditemNavigation.TipoItem,
                        quantidade = i.QuantidadeItens,
                        valorUnitario = i.ValorunitarioEntrada,
                        subtotal = i.QuantidadeItens * i.ValorunitarioEntrada
                    }).ToList(),
                    total = p.Atendimentoitems.Sum(i => i.QuantidadeItens * i.ValorunitarioEntrada)
                }).ToListAsync();

            return Ok(pedidos);
        }

        // Checkout transacional: valida itens, funcionário e saldo do estoque antes de gravar.
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] PedidoCheckoutDTO dto)
        {
            if (dto.IdPessoa <= 0 || dto.Itens == null || dto.Itens.Count == 0)
                return BadRequest(new { mensagem = "Informe o cliente e ao menos um item." });

            var cliente = await _context.Pessoas.FirstOrDefaultAsync(p => p.Idpessoa == dto.IdPessoa && p.AtivoPessoa != "N");
            if (cliente == null) return BadRequest(new { mensagem = "Cliente não encontrado ou inativo." });

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var pedido = new Atendimento {
                    Idpessoa = dto.IdPessoa,
                    DataAtendimento = DateOnly.FromDateTime(DateTime.Today),
                    StatusAtendimento = "A"
                };
                _context.Atendimentos.Add(pedido);
                await _context.SaveChangesAsync();

                foreach (var linha in dto.Itens)
                {
                    if (linha.Quantidade <= 0)
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new { mensagem = "A quantidade deve ser maior que zero." });
                    }

                    var item = await _context.Items.FirstOrDefaultAsync(i => i.Iditem == linha.IdItem);
                    if (item == null || item.AtivoProdutoServico == "N")
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new { mensagem = $"Item {linha.IdItem} não encontrado ou inativo." });
                    }

                    var funcionario = await _context.Funcionarios.AnyAsync(f => f.Idpessoa == linha.IdFuncionario);
                    if (!funcionario)
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new { mensagem = $"Informe um funcionário válido para o item {linha.IdItem}." });
                    }

                    var produto = await _context.Produtos.Include(p => p.Estoque)
                        .FirstOrDefaultAsync(p => p.Iditem == linha.IdItem);
                    if (produto != null)
                    {
                        var estoque = await _context.Estoques.FirstOrDefaultAsync(e => e.Iditem == linha.IdItem);
                        if (estoque == null || estoque.QuantidadeEstoque < linha.Quantidade)
                        {
                            await tx.RollbackAsync();
                            return Conflict(new { mensagem = $"Estoque insuficiente para {item.Item1}." });
                        }
                        estoque.QuantidadeEstoque -= linha.Quantidade;
                    }

                    var preco = produto?.PrecoVenda ?? item.Preco;
                    _context.Atendimentoitems.Add(new Atendimentoitem {
                        Idatendimento = pedido.Idatendimento,
                        Iditem = linha.IdItem,
                        Idfuncionario = linha.IdFuncionario,
                        QuantidadeItens = linha.Quantidade,
                        ValorunitarioEntrada = preco
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return CreatedAtAction(nameof(Consultar), new { id = pedido.Idatendimento }, new { idPedido = pedido.Idatendimento, situacao = pedido.StatusAtendimento });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { mensagem = "Não foi possível registrar o pedido.", erro = ex.Message });
            }
        }

        [HttpPatch("{id:int}/situacao")]
        public async Task<IActionResult> AtualizarSituacao(int id, [FromBody] AtualizarSituacaoPedidoDTO dto)
        {
            var permitidos = new[] { "A", "P", "C", "F" }; // Aberto, processando, cancelado, finalizado
            if (string.IsNullOrWhiteSpace(dto.Situacao) || !permitidos.Contains(dto.Situacao.ToUpperInvariant()))
                return BadRequest(new { mensagem = "Situação inválida. Use A, P, C ou F." });

            var pedido = await _context.Atendimentos.Include(p => p.Atendimentoitems)
                .FirstOrDefaultAsync(p => p.Idatendimento == id);
            if (pedido == null) return NotFound(new { mensagem = "Pedido não encontrado." });

            var nova = dto.Situacao.ToUpperInvariant();
            if (pedido.StatusAtendimento == "C" || pedido.StatusAtendimento == "F")
                return Conflict(new { mensagem = "Pedidos cancelados ou finalizados não podem mudar de situação." });

            // Ao cancelar, devolve os produtos ao estoque.
            if (nova == "C" && pedido.StatusAtendimento != "C")
            {
                foreach (var linha in pedido.Atendimentoitems)
                {
                    var produto = await _context.Produtos.AnyAsync(p => p.Iditem == linha.Iditem);
                    if (!produto) continue;
                    var estoque = await _context.Estoques.FirstOrDefaultAsync(e => e.Iditem == linha.Iditem);
                    if (estoque != null) estoque.QuantidadeEstoque += linha.QuantidadeItens;
                }
            }

            pedido.StatusAtendimento = nova;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
