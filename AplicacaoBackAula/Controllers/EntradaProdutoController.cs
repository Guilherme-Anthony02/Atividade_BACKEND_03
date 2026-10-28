using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AplicacaoBackAula.DTO;
using AplicacaoBackAula.Models;

namespace AplicacaoBackAula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EntradaProdutoController : ControllerBase
    {
         private readonly BdAulaBackendContext _context;

        //DECLARAÇÃO DO MÉTODO CONSTRUTOR
        public EntradaProdutoController(BdAulaBackendContext context)
        {
            //INSTANCIA DO ATRIBUTO CONSTRUTOR
            _context = context;
        }

        //LISTAR TODAS AS ENTRADAS DE PRODUTOS
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EntradaProdutoDTO>>> GetEntradasProduto()
        {
            var entradas = await _context.EntradaProdutos
                .Include(ep => ep.IditemNavigation)
                .Select(ep => new EntradaProdutoDTO
                {
                    IdEntradaProduto = ep.IdentradaProduto,
                    IdItem = ep.Iditem,
                    DataEntradaProduto = ep.DataEntradaProduto,
                    QuantidadeEntrada = ep.QuantidadeEntrada,
                    Unidade = ep.Unidade,
                    ValorUnitarioEntrada = ep.ValorUnitarioEntrada,
                    QuantidadeEstoque = ep.IditemNavigation.QuantidadeEstoque,
                    Produto = null
                })
                .ToListAsync();

            return Ok(entradas);
        }

        //LISTAR APENAS UMA ENTRADA DE PRODUTO
        [HttpGet("{id}")]
        public async Task<ActionResult<EntradaProdutoDTO>> GetEntradaProduto(int id)
        {
            var entrada = await _context.EntradaProdutos
                .Include(ep => ep.IditemNavigation)
                .Where(ep => ep.IdentradaProduto == id)
                .Select(ep => new EntradaProdutoDTO
                {
                    IdEntradaProduto = ep.IdentradaProduto,
                    IdItem = ep.Iditem,
                    DataEntradaProduto = ep.DataEntradaProduto,
                    QuantidadeEntrada = ep.QuantidadeEntrada,
                    Unidade = ep.Unidade,
                    ValorUnitarioEntrada = ep.ValorUnitarioEntrada,
                    QuantidadeEstoque = ep.IditemNavigation.QuantidadeEstoque,
                    Produto = null
                })
                .FirstOrDefaultAsync();

            if (entrada == null)
            {
                return NotFound(new
                {
                    mensagem = "ENTRADA DE PRODUTO NÃO LOCALIZADA!"
                });
            }

            return Ok(entrada);
        }

        //INSERIR ENTRADA DE PRODUTO
        [HttpPost]
        public async Task<ActionResult> PostEntradaProduto(EntradaProdutoDTO entradaProdutoDTO)
        {
            //ABRINDO UMA TRANSAÇÃO NO BANCO DE DADOS
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //VALIDAR CAMPOS OBRIGATÓRIOS
                if (entradaProdutoDTO.IdItem <= 0)
                {
                    return BadRequest(new
                    {
                        mensagem = "ID DO ITEM É OBRIGATÓRIO!"
                    });
                }

                if (entradaProdutoDTO.QuantidadeEntrada <= 0)
                {
                    return BadRequest(new
                    {
                        mensagem = "QUANTIDADE DE ENTRADA DEVE SER MAIOR QUE ZERO!"
                    });
                }

                if (entradaProdutoDTO.ValorUnitarioEntrada <= 0)
                {
                    return BadRequest(new
                    {
                        mensagem = "VALOR UNITÁRIO DEVE SER MAIOR QUE ZERO!"
                    });
                }

                if (string.IsNullOrWhiteSpace(entradaProdutoDTO.Unidade))
                {
                    return BadRequest(new
                    {
                        mensagem = "UNIDADE É OBRIGATÓRIA!"
                    });
                }

                //VALIDAR SE O PRODUTO/ITEM EXISTE E TEM ESTOQUE
                var estoque = await _context.Estoques
                    .FirstOrDefaultAsync(e => e.Iditem == entradaProdutoDTO.IdItem);

                if (estoque == null)
                {
                    return BadRequest(new
                    {
                        mensagem = "PRODUTO NÃO ENCONTRADO OU SEM ESTOQUE CONFIGURADO!"
                    });
                }

                //CRIAR NOVA ENTRADA DE PRODUTO
                var entradaProduto = new EntradaProduto
                {
                    Iditem = entradaProdutoDTO.IdItem,
                    DataEntradaProduto = entradaProdutoDTO.DataEntradaProduto,
                    QuantidadeEntrada = entradaProdutoDTO.QuantidadeEntrada,
                    Unidade = entradaProdutoDTO.Unidade,
                    ValorUnitarioEntrada = entradaProdutoDTO.ValorUnitarioEntrada
                };

                //ADICIONAR ENTRADA DE PRODUTO
                _context.EntradaProdutos.Add(entradaProduto);

                //SALVAR AS MUDANÇAS
                await _context.SaveChangesAsync();

                //CONFIRMAR A TRANSAÇÃO
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetEntradaProduto),
                    new { id = entradaProduto.IdentradaProduto },
                    entradaProduto
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    mensagem = "ERRO NO CADASTRO",
                    erro = ex.Message
                });
            }
        }

        //ALTERAR ENTRADA DE PRODUTO
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEntradaProduto(int id, EntradaProdutoDTO dados)
        {
            //ABRINDO UMA TRANSAÇÃO NO BANCO DE DADOS
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //VALIDAR CAMPOS OBRIGATÓRIOS
                if (dados.IdItem <= 0)
                {
                    return BadRequest(new
                    {
                        mensagem = "ID DO ITEM É OBRIGATÓRIO!"
                    });
                }

                if (dados.QuantidadeEntrada <= 0)
                {
                    return BadRequest(new
                    {
                        mensagem = "QUANTIDADE DE ENTRADA DEVE SER MAIOR QUE ZERO!"
                    });
                }

                if (dados.ValorUnitarioEntrada <= 0)
                {
                    return BadRequest(new
                    {
                        mensagem = "VALOR UNITÁRIO DEVE SER MAIOR QUE ZERO!"
                    });
                }

                if (string.IsNullOrWhiteSpace(dados.Unidade))
                {
                    return BadRequest(new
                    {
                        mensagem = "UNIDADE É OBRIGATÓRIA!"
                    });
                }

                //BUSCAR ENTRADA EXISTENTE
                var entradaProdutoExistente = await _context.EntradaProdutos
                    .FirstOrDefaultAsync(ep => ep.IdentradaProduto == id);

                if (entradaProdutoExistente == null)
                {
                    return NotFound(new
                    {
                        mensagem = "ENTRADA DE PRODUTO NÃO ENCONTRADA!"
                    });
                }

                //VALIDAR SE O NOVO ITEM EXISTE E TEM ESTOQUE (CASO TENHA MUDADO)
                if (entradaProdutoExistente.Iditem != dados.IdItem)
                {
                    var estoque = await _context.Estoques
                        .FirstOrDefaultAsync(e => e.Iditem == dados.IdItem);

                    if (estoque == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new
                        {
                            mensagem = "PRODUTO NÃO ENCONTRADO OU SEM ESTOQUE CONFIGURADO!"
                        });
                    }
                }

                //ATUALIZAR DADOS DA ENTRADA
                entradaProdutoExistente.Iditem = dados.IdItem;
                entradaProdutoExistente.DataEntradaProduto = dados.DataEntradaProduto;
                entradaProdutoExistente.QuantidadeEntrada = dados.QuantidadeEntrada;
                entradaProdutoExistente.Unidade = dados.Unidade;
                entradaProdutoExistente.ValorUnitarioEntrada = dados.ValorUnitarioEntrada;

                //SALVAR AS MUDANÇAS
                await _context.SaveChangesAsync();

                //CONFIRMAR A TRANSAÇÃO
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    mensagem = "ERRO NA ATUALIZAÇÃO",
                    erro = ex.Message
                });
            }
        }

        //EXCLUIR ENTRADA DE PRODUTO
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntradaProduto(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //BUSCAR ENTRADA DE PRODUTO
                var entradaProduto = await _context.EntradaProdutos
                    .FirstOrDefaultAsync(ep => ep.IdentradaProduto == id);

                if (entradaProduto == null)
                {
                    return NotFound(new
                    {
                        mensagem = "ENTRADA DE PRODUTO NÃO ENCONTRADA!"
                    });
                }

                //REMOVER A ENTRADA DE PRODUTO
                _context.EntradaProdutos.Remove(entradaProduto);

                //SALVAR AS MUDANÇAS
                await _context.SaveChangesAsync();

                //CONFIRMAR A TRANSAÇÃO
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    mensagem = "ERRO NA EXCLUSÃO",
                    erro = ex.Message
                });
            }
        }
    }
}
