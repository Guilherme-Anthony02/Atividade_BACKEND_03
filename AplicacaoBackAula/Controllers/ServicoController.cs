
using AplicacaoBackAula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
    public class ServicoRequest
    {
        public int IdCategoria { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public string Ativo { get; set; } = "S";
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ServicoController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public ServicoController(BdAulaBackendContext context)
        {
            _context = context;
        }

        // GET: api/Servico
        // Lista todos os serviços
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var servicos = await _context.Items
                .Where(i => i.TipoItem == "S")
                .Select(i => new
                {
                    id = i.Iditem,
                    idCategoria = i.Idcategoria,
                    nome = i.Item1,
                    descricao = i.DescricaoItem,
                    preco = i.Preco,
                    ativo = i.AtivoProdutoServico
                })
                .ToListAsync();

            return Ok(servicos);
        }

        // GET: api/Servico/{id}
        // Consulta um serviço pelo ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Consultar(int id)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(i =>
                    i.Iditem == id && i.TipoItem == "S");

            if (item == null)
            {
                return NotFound(new
                {
                    mensagem = "Serviço não encontrado."
                });
            }

            return Ok(new
            {
                id = item.Iditem,
                idCategoria = item.Idcategoria,
                nome = item.Item1,
                descricao = item.DescricaoItem,
                preco = item.Preco,
                ativo = item.AtivoProdutoServico
            });
        }

        // POST: api/Servico
        // Cadastra um novo serviço
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] ServicoRequest dto)
        {
            if (dto.IdCategoria <= 0 ||
                string.IsNullOrWhiteSpace(dto.Nome) ||
                dto.Preco <= 0)
            {
                return BadRequest(new
                {
                    mensagem = "Categoria, nome e preço positivo são obrigatórios."
                });
            }

            bool categoriaExiste = await _context.Categoria
                .AnyAsync(c => c.Idcategoria == dto.IdCategoria);

            if (!categoriaExiste)
            {
                return BadRequest(new
                {
                    mensagem = "Categoria não encontrada."
                });
            }

            var item = new Item
            {
                Idcategoria = dto.IdCategoria,
                Item1 = dto.Nome.Trim(),
                DescricaoItem = dto.Descricao ?? "",
                Preco = dto.Preco,
                TipoItem = "S",
                AtivoProdutoServico = dto.Ativo == "N" ? "N" : "S"
            };

            _context.Items.Add(item);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(Consultar),
                new { id = item.Iditem },
                new
                {
                    id = item.Iditem,
                    nome = item.Item1
                }
            );
        }

        // PUT: api/Servico/{id}
        // Atualiza um serviço
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(
            int id,
            [FromBody] ServicoRequest dto)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(i =>
                    i.Iditem == id && i.TipoItem == "S");

            if (item == null)
            {
                return NotFound(new
                {
                    mensagem = "Serviço não encontrado."
                });
            }

            if (dto.IdCategoria <= 0 ||
                string.IsNullOrWhiteSpace(dto.Nome) ||
                dto.Preco <= 0)
            {
                return BadRequest(new
                {
                    mensagem = "Categoria, nome e preço positivo são obrigatórios."
                });
            }

            bool categoriaExiste = await _context.Categoria
                .AnyAsync(c => c.Idcategoria == dto.IdCategoria);

            if (!categoriaExiste)
            {
                return BadRequest(new
                {
                    mensagem = "Categoria não encontrada."
                });
            }

            item.Idcategoria = dto.IdCategoria;
            item.Item1 = dto.Nome.Trim();
            item.DescricaoItem = dto.Descricao ?? "";
            item.Preco = dto.Preco;
            item.AtivoProdutoServico = dto.Ativo == "N" ? "N" : "S";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Servico/{id}/desativar
        // Desativa um serviço
        [HttpPatch("{id:int}/desativar")]
        public async Task<IActionResult> Desativar(int id)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(i =>
                    i.Iditem == id && i.TipoItem == "S");

            if (item == null)
            {
                return NotFound(new
                {
                    mensagem = "Serviço não encontrado."
                });
            }

            item.AtivoProdutoServico = "N";

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
