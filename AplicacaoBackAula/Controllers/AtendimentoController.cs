using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AplicacaoBackAula.DTO;
using AplicacaoBackAula.Models;

namespace AplicacaoBackAula.Controllers
{
    //ANOTAÇÕES DE IDENTIFICAÇÃO DE CONTROLLER
    [ApiController]
    [Route("api/[controller]")]
    public class AtendimentoController : ControllerBase
    {
        //DECLARAÇÃO DO ATRIBUTO DE CONTEXTO
        private readonly BdAulaBackendContext _context;

        //DECLARAÇÃO DO MÉTODO CONSTRUTOR
        public AtendimentoController(BdAulaBackendContext context)
        {
            //INSTANCIA DO ATRIBUTO CONSTRUTOR
            _context = context;
        }

        //LISTAR TODOS OS ATENDIMENTOS
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AtendimentoDTO>>> GetAll()
        {
            var atendimentos = await _context.Atendimentos
                .Include(a => a.IdpessoaNavigation)
                .Include(a => a.Atendimentoitems)
                    .ThenInclude(ai => ai.IditemNavigation)
                .Include(a => a.Atendimentoitems)
                    .ThenInclude(ai => ai.IdfuncionarioNavigation)
                        .ThenInclude(f => f.IdpessoaNavigation)
                .Select(a => new AtendimentoDTO
                {
                    IdAtendimento = a.Idatendimento,
                    IdPessoa = a.Idpessoa,
                    DataAtendimento = a.DataAtendimento,
                    StatusAtendimento = a.StatusAtendimento,
                    AtendimentoItens = a.Atendimentoitems.Select(ai => new AtendimentoItemDTO
                    {
                        IdAtendimento = ai.Idatendimento,
                        IdItem = ai.Iditem,
                        IdFuncionario = ai.Idfuncionario,
                        QuantidadeItens = ai.QuantidadeItens,
                        ValorunitarioEntrada = ai.ValorunitarioEntrada,
                        ItemNome = ai.IditemNavigation.Item1,
                        DescricaoItem = ai.IditemNavigation.DescricaoItem,
                        PrecoItem = ai.IditemNavigation.Preco,
                        NomeFuncionario = ai.IdfuncionarioNavigation.IdpessoaNavigation.Nome,
                        CargoFuncionario = ai.IdfuncionarioNavigation.Cargo
                    }).ToList()
                })
                .ToListAsync();

            return Ok(atendimentos);
        }

        //LISTAR APENAS UM ATENDIMENTO
        [HttpGet("{id}")]
        public async Task<ActionResult<AtendimentoDTO>> GetAtendimento(int id)
        {
            var atendimento = await _context.Atendimentos
                .Include(a => a.IdpessoaNavigation)
                .Include(a => a.Atendimentoitems)
                    .ThenInclude(ai => ai.IditemNavigation)
                .Include(a => a.Atendimentoitems)
                    .ThenInclude(ai => ai.IdfuncionarioNavigation)
                        .ThenInclude(f => f.IdpessoaNavigation)
                .Where(a => a.Idatendimento == id)
                .Select(a => new AtendimentoDTO
                {
                    IdAtendimento = a.Idatendimento,
                    IdPessoa = a.Idpessoa,
                    DataAtendimento = a.DataAtendimento,
                    StatusAtendimento = a.StatusAtendimento,
                    AtendimentoItens = a.Atendimentoitems.Select(ai => new AtendimentoItemDTO
                    {
                        IdAtendimento = ai.Idatendimento,
                        IdItem = ai.Iditem,
                        IdFuncionario = ai.Idfuncionario,
                        QuantidadeItens = ai.QuantidadeItens,
                        ValorunitarioEntrada = ai.ValorunitarioEntrada,
                        ItemNome = ai.IditemNavigation.Item1,
                        DescricaoItem = ai.IditemNavigation.DescricaoItem,
                        PrecoItem = ai.IditemNavigation.Preco,
                        NomeFuncionario = ai.IdfuncionarioNavigation.IdpessoaNavigation.Nome,
                        CargoFuncionario = ai.IdfuncionarioNavigation.Cargo
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (atendimento == null)
            {
                return NotFound(new
                {
                    mensagem = "ATENDIMENTO NÃO LOCALIZADO!"
                });
            }

            return Ok(atendimento);
        }

        //CADASTRAR ATENDIMENTO COM SEUS ITENS
        [HttpPost]
        public async Task<ActionResult<AtendimentoDTO>> PostAtendimento(AtendimentoDTO dto)
        {
            //ABRINDO UMA TRANSAÇÃO NO BANCO DE DADOS
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //VALIDAR SE PESSOA EXISTE
                var pessoaExiste = await _context.Pessoas
                    .FirstOrDefaultAsync(p => p.Idpessoa == dto.IdPessoa);

                if (pessoaExiste == null)
                {
                    return BadRequest(new
                    {
                        mensagem = "PESSOA NÃO ENCONTRADA!"
                    });
                }

                //CRIAR NOVO ATENDIMENTO
                var atendimento = new Atendimento
                {
                    Idpessoa = dto.IdPessoa,
                    DataAtendimento = dto.DataAtendimento,
                    StatusAtendimento = dto.StatusAtendimento
                };

                //ADICIONAR ATENDIMENTO
                _context.Atendimentos.Add(atendimento);
                await _context.SaveChangesAsync();

                int idAtendimentoGerado = atendimento.Idatendimento;

                //ADICIONAR ITENS DO ATENDIMENTO
                if (dto.AtendimentoItens != null && dto.AtendimentoItens.Any())
                {
                    foreach (var item in dto.AtendimentoItens)
                    {
                        //VALIDAR SE ITEM EXISTE
                        var itemExiste = await _context.Items
                            .FirstOrDefaultAsync(i => i.Iditem == item.IdItem);

                        if (itemExiste == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                mensagem = $"ITEM COM ID {item.IdItem} NÃO ENCONTRADO!"
                            });
                        }

                        //VALIDAR SE FUNCIONÁRIO EXISTE
                        var funcionarioExiste = await _context.Funcionarios
                            .FirstOrDefaultAsync(f => f.Idpessoa == item.IdFuncionario);

                        if (funcionarioExiste == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                mensagem = $"FUNCIONÁRIO COM ID {item.IdFuncionario} NÃO ENCONTRADO!"
                            });
                        }

                        //VALIDAR QUANTIDADE E VALOR
                        if (item.QuantidadeItens <= 0)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                mensagem = "QUANTIDADE DE ITENS DEVE SER MAIOR QUE ZERO!"
                            });
                        }

                        if (item.ValorunitarioEntrada <= 0)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                mensagem = "VALOR UNITÁRIO DEVE SER MAIOR QUE ZERO!"
                            });
                        }

                        //CRIAR ATENDIMENTO ITEM
                        var atendimentoItem = new Atendimentoitem
                        {
                            Idatendimento = idAtendimentoGerado,
                            Iditem = item.IdItem,
                            Idfuncionario = item.IdFuncionario,
                            QuantidadeItens = item.QuantidadeItens,
                            ValorunitarioEntrada = item.ValorunitarioEntrada
                        };

                        _context.Atendimentoitems.Add(atendimentoItem);
                    }

                    await _context.SaveChangesAsync();
                }

                //CONFIRMAR A TRANSAÇÃO
                await transaction.CommitAsync();

                //BUSCAR O ATENDIMENTO CRIADO COM SEUS ITENS
                var atendimentoRetorno = await _context.Atendimentos
                    .Include(a => a.IdpessoaNavigation)
                    .Include(a => a.Atendimentoitems)
                        .ThenInclude(ai => ai.IditemNavigation)
                    .Include(a => a.Atendimentoitems)
                        .ThenInclude(ai => ai.IdfuncionarioNavigation)
                            .ThenInclude(f => f.IdpessoaNavigation)
                    .Where(a => a.Idatendimento == idAtendimentoGerado)
                    .Select(a => new AtendimentoDTO
                    {
                        IdAtendimento = a.Idatendimento,
                        IdPessoa = a.Idpessoa,
                        DataAtendimento = a.DataAtendimento,
                        StatusAtendimento = a.StatusAtendimento,
                        AtendimentoItens = a.Atendimentoitems.Select(ai => new AtendimentoItemDTO
                        {
                            IdAtendimento = ai.Idatendimento,
                            IdItem = ai.Iditem,
                            IdFuncionario = ai.Idfuncionario,
                            QuantidadeItens = ai.QuantidadeItens,
                            ValorunitarioEntrada = ai.ValorunitarioEntrada,
                            ItemNome = ai.IditemNavigation.Item1,
                            DescricaoItem = ai.IditemNavigation.DescricaoItem,
                            PrecoItem = ai.IditemNavigation.Preco,
                            NomeFuncionario = ai.IdfuncionarioNavigation.IdpessoaNavigation.Nome,
                            CargoFuncionario = ai.IdfuncionarioNavigation.Cargo
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                return CreatedAtAction(
                    nameof(GetAtendimento),
                    new { id = idAtendimentoGerado },
                    atendimentoRetorno
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

        //ALTERAR ATENDIMENTO E SEUS ITENS
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAtendimento(int id, AtendimentoDTO dto)
        {
            //ABRINDO UMA TRANSAÇÃO NO BANCO DE DADOS
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //BUSCAR ATENDIMENTO EXISTENTE
                var atendimentoExistente = await _context.Atendimentos
                    .Include(a => a.Atendimentoitems)
                    .FirstOrDefaultAsync(a => a.Idatendimento == id);

                if (atendimentoExistente == null)
                {
                    return NotFound(new
                    {
                        mensagem = "ATENDIMENTO NÃO ENCONTRADO!"
                    });
                }

                //VALIDAR SE PESSOA EXISTE
                var pessoaExiste = await _context.Pessoas
                    .FirstOrDefaultAsync(p => p.Idpessoa == dto.IdPessoa);

                if (pessoaExiste == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        mensagem = "PESSOA NÃO ENCONTRADA!"
                    });
                }

                //ATUALIZAR DADOS DO ATENDIMENTO
                atendimentoExistente.Idpessoa = dto.IdPessoa;
                atendimentoExistente.DataAtendimento = dto.DataAtendimento;
                atendimentoExistente.StatusAtendimento = dto.StatusAtendimento;

                //REMOVER ITENS ANTIGOS
                _context.Atendimentoitems.RemoveRange(atendimentoExistente.Atendimentoitems);

                await _context.SaveChangesAsync();

                //ADICIONAR NOVOS ITENS
                if (dto.AtendimentoItens != null && dto.AtendimentoItens.Any())
                {
                    foreach (var item in dto.AtendimentoItens)
                    {
                        //VALIDAR SE ITEM EXISTE
                        var itemExiste = await _context.Items
                            .FirstOrDefaultAsync(i => i.Iditem == item.IdItem);

                        if (itemExiste == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                mensagem = $"ITEM COM ID {item.IdItem} NÃO ENCONTRADO!"
                            });
                        }

                        //VALIDAR SE FUNCIONÁRIO EXISTE
                        var funcionarioExiste = await _context.Funcionarios
                            .FirstOrDefaultAsync(f => f.Idpessoa == item.IdFuncionario);

                        if (funcionarioExiste == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                mensagem = $"FUNCIONÁRIO COM ID {item.IdFuncionario} NÃO ENCONTRADO!"
                            });
                        }

                        //VALIDAR QUANTIDADE E VALOR
                        if (item.QuantidadeItens <= 0)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                mensagem = "QUANTIDADE DE ITENS DEVE SER MAIOR QUE ZERO!"
                            });
                        }

                        if (item.ValorunitarioEntrada <= 0)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                mensagem = "VALOR UNITÁRIO DEVE SER MAIOR QUE ZERO!"
                            });
                        }

                        //CRIAR NOVO ATENDIMENTO ITEM
                        var atendimentoItem = new Atendimentoitem
                        {
                            Idatendimento = id,
                            Iditem = item.IdItem,
                            Idfuncionario = item.IdFuncionario,
                            QuantidadeItens = item.QuantidadeItens,
                            ValorunitarioEntrada = item.ValorunitarioEntrada
                        };

                        _context.Atendimentoitems.Add(atendimentoItem);
                    }
                }

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

        //EXCLUIR ATENDIMENTO E SEUS ITENS
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAtendimento(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var atendimento = await _context.Atendimentos
                    .Include(a => a.Atendimentoitems)
                    .FirstOrDefaultAsync(a => a.Idatendimento == id);

                if (atendimento == null)
                {
                    return NotFound(new
                    {
                        mensagem = "ATENDIMENTO NÃO ENCONTRADO!"
                    });
                }

                //REMOVER TODOS OS ITENS DO ATENDIMENTO
                _context.Atendimentoitems.RemoveRange(atendimento.Atendimentoitems);

                //REMOVER O ATENDIMENTO
                _context.Atendimentos.Remove(atendimento);

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
