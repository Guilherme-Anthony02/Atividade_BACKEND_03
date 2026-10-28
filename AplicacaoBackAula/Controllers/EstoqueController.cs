using AplicacaoBackAula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstoqueController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public EstoqueController(BdAulaBackendContext context)
        {
            _context = context;
        }

        // Lista o estoque com os dados básicos do produto.
        [HttpGet]
        public async Task<IActionResult> GetEstoque()
        {
            var estoque = await _context.Estoques
                .Include(e => e.IditemNavigation)
                    .ThenInclude(p => p.IditemNavigation)
                        .ThenInclude(i => i.IdcategoriaNavigation)
                .Select(e => new
                {
                    idItem = e.Iditem,
                    quantidadeEstoque = e.QuantidadeEstoque,
                    item = e.IditemNavigation.IditemNavigation.Item1,
                    descricaoItem = e.IditemNavigation.IditemNavigation.DescricaoItem,
                    preco = e.IditemNavigation.IditemNavigation.Preco,
                    precoVenda = e.IditemNavigation.PrecoVenda,
                    unidade = e.IditemNavigation.Unidade,
                    categoria = e.IditemNavigation.IditemNavigation.IdcategoriaNavigation.Categoria,
                    ativo = e.IditemNavigation.IditemNavigation.AtivoProdutoServico
                })
                .ToListAsync();

            return Ok(estoque);
        }

        // Consulta a quantidade disponível de um produto específico.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEstoquePorId(int id)
        {
            var estoque = await _context.Estoques
                .Include(e => e.IditemNavigation)
                    .ThenInclude(p => p.IditemNavigation)
                        .ThenInclude(i => i.IdcategoriaNavigation)
                .Where(e => e.Iditem == id)
                .Select(e => new
                {
                    idItem = e.Iditem,
                    quantidadeEstoque = e.QuantidadeEstoque,
                    item = e.IditemNavigation.IditemNavigation.Item1,
                    descricaoItem = e.IditemNavigation.IditemNavigation.DescricaoItem,
                    preco = e.IditemNavigation.IditemNavigation.Preco,
                    precoVenda = e.IditemNavigation.PrecoVenda,
                    unidade = e.IditemNavigation.Unidade,
                    categoria = e.IditemNavigation.IditemNavigation.IdcategoriaNavigation.Categoria,
                    ativo = e.IditemNavigation.IditemNavigation.AtivoProdutoServico
                })
                .FirstOrDefaultAsync();

            if (estoque == null)
            {
                return NotFound(new { mensagem = "Produto não encontrado no estoque." });
            }

            return Ok(estoque);
        }
    }
}
