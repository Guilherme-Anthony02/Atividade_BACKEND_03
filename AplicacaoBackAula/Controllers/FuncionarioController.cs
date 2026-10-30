using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AplicacaoBackAula.DTO;
using AplicacaoBackAula.Models;

namespace AplicacaoBackAula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FuncionarioController : ControllerBase
    {
        //DECLARAÇÃO DO ATRIBUTO DE CONTEXTO
        private readonly BdAulaBackendContext _context;

        //DECLARAÇÃO DO MÉTODO CONSTRUTOR
        public FuncionarioController(BdAulaBackendContext context)
        {
            //INSTANCIA DO ATRIBUTO CONSTRUTOR
            _context = context;
        }

        //LISTAR TODOS OS FUNCIONÁRIOS COM SEUS DADOS PESSOAIS E ENDEREÇOS
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FuncionarioDTO>>> GetAll()
        {
            var funcionarios = await _context.Funcionarios
                .Include(f => f.IdpessoaNavigation)
                    .ThenInclude(p => p.Enderecos)
                .Select(f => new FuncionarioDTO
                {
                    IdPessoa = f.Idpessoa,
                    Cargo = f.Cargo,
                    DataAdmissao = f.DataAdmissao,
                    Pessoa = new PessoaDTO
                    {
                        IdPessoa = f.IdpessoaNavigation.Idpessoa,
                        Nome = f.IdpessoaNavigation.Nome,
                        CPF = (long)(long?)f.IdpessoaNavigation.Cpf,
                        DataNascimento = f.IdpessoaNavigation.DataNascimento,
                        sexo = f.IdpessoaNavigation.Sexo,
                        email = f.IdpessoaNavigation.Email,
                        telefone = (long)f.IdpessoaNavigation.Telefone,
                        AtivoPessoa = f.IdpessoaNavigation.AtivoPessoa,
                        Enderecos = f.IdpessoaNavigation.Enderecos.Select(e => new EnderecoDTO
                        {
                            IdEndereco = e.Idendereco,
                            CEP = e.Cep,
                            Numero = e.Numero,
                            Complemento = e.Complemento
                        }).ToList()
                    }
                })
                .ToListAsync();

            return Ok(funcionarios);
        }

        //LISTAR UM FUNCIONÁRIO ESPECÍFICO PELO ID
        [HttpGet("{id}")]
        public async Task<ActionResult<FuncionarioDTO>> GetFuncionario(int id)
        {
            var funcionario = await _context.Funcionarios
                .Include(f => f.IdpessoaNavigation)
                    .ThenInclude(p => p.Enderecos)
                .Where(f => f.Idpessoa == id)
                .Select(f => new FuncionarioDTO
                {
                    IdPessoa = f.Idpessoa,
                    Cargo = f.Cargo,
                    DataAdmissao = f.DataAdmissao,
                    Pessoa = new PessoaDTO
                    {
                        IdPessoa = f.IdpessoaNavigation.Idpessoa,
                        Nome = f.IdpessoaNavigation.Nome,
                        CPF = (long)(long?)f.IdpessoaNavigation.Cpf,
                        DataNascimento = f.IdpessoaNavigation.DataNascimento,
                        sexo = f.IdpessoaNavigation.Sexo,
                        email = f.IdpessoaNavigation.Email,
                        telefone = (long)f.IdpessoaNavigation.Telefone,
                        AtivoPessoa = f.IdpessoaNavigation.AtivoPessoa,
                        Enderecos = f.IdpessoaNavigation.Enderecos.Select(e => new EnderecoDTO
                        {
                            IdEndereco = e.Idendereco,
                            CEP = e.Cep,
                            Numero = e.Numero,
                            Complemento = e.Complemento
                        }).ToList()
                    }
                })
                .FirstOrDefaultAsync();

            if (funcionario == null)
            {
                return NotFound(new
                {
                    mensagem = "FUNCIONÁRIO NÃO LOCALIZADO!"
                });
            }

            return Ok(funcionario);
        }

        //CADASTRAR UM NOVO FUNCIONÁRIO COM SEUS DADOS PESSOAIS E ENDEREÇO
        [HttpPost]
        public async Task<ActionResult<FuncionarioDTO>> Post(FuncionarioDTO dto)
        {
            //ABRINDO UMA TRANSAÇÃO NO BANCO DE DADOS
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //VALIDAR DADOS DA PESSOA
                if (dto.Pessoa == null)
                {
                    return BadRequest(new
                    {
                        mensagem = "DADOS DA PESSOA SÃO OBRIGATÓRIOS!"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Pessoa.Nome))
                {
                    return BadRequest(new
                    {
                        mensagem = "NOME DA PESSOA É OBRIGATÓRIO!"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Cargo))
                {
                    return BadRequest(new
                    {
                        mensagem = "CARGO DO FUNCIONÁRIO É OBRIGATÓRIO!"
                    });
                }

                //CRIAR NOVA PESSOA
                var pessoa = new Pessoa
                {
                    Nome = dto.Pessoa.Nome,
                    Cpf = dto.Pessoa.CPF,
                    DataNascimento = dto.Pessoa.DataNascimento,
                    Sexo = dto.Pessoa.sexo,
                    Email = dto.Pessoa.email,
                    Telefone = dto.Pessoa.telefone,
                    AtivoPessoa = dto.Pessoa.AtivoPessoa,
                    Enderecos = dto.Pessoa.Enderecos.Select(e => new Endereco
                    {
                        Cep = e.CEP,
                        Numero = e.Numero,
                        Complemento = e.Complemento
                    }).ToList()
                };

                //ADICIONAR PESSOA
                _context.Pessoas.Add(pessoa);

                //SALVAR E OBTER O ID GERADO
                await _context.SaveChangesAsync();

                int idPessoaGerado = pessoa.Idpessoa;

                //CRIAR NOVO FUNCIONÁRIO
                var funcionario = new Funcionario
                {
                    Idpessoa = idPessoaGerado,
                    Cargo = dto.Cargo,
                    DataAdmissao = dto.DataAdmissao
                };

                //ADICIONAR FUNCIONÁRIO
                _context.Funcionarios.Add(funcionario);

                //SALVAR AS MUDANÇAS
                await _context.SaveChangesAsync();

                //CONFIRMAR A TRANSAÇÃO
                await transaction.CommitAsync();

                //RETORNAR O FUNCIONÁRIO CRIADO
                var funcioarioRetorno = await _context.Funcionarios
                    .Include(f => f.IdpessoaNavigation)
                        .ThenInclude(p => p.Enderecos)
                    .Where(f => f.Idpessoa == idPessoaGerado)
                    .Select(f => new FuncionarioDTO
                    {
                        IdPessoa = f.Idpessoa,
                        Cargo = f.Cargo,
                        DataAdmissao = f.DataAdmissao,
                        Pessoa = new PessoaDTO
                        {
                            IdPessoa = f.IdpessoaNavigation.Idpessoa,
                            Nome = f.IdpessoaNavigation.Nome,
                            CPF = (long)(long?)f.IdpessoaNavigation.Cpf,
                            DataNascimento = f.IdpessoaNavigation.DataNascimento,
                            sexo = f.IdpessoaNavigation.Sexo,
                            email = f.IdpessoaNavigation.Email,
                            telefone = (long)f.IdpessoaNavigation.Telefone,
                            AtivoPessoa = f.IdpessoaNavigation.AtivoPessoa,
                            Enderecos = f.IdpessoaNavigation.Enderecos.Select(e => new EnderecoDTO
                            {
                                IdEndereco = e.Idendereco,
                                CEP = e.Cep,
                                Numero = e.Numero,
                                Complemento = e.Complemento
                            }).ToList()
                        }
                    })
                    .FirstOrDefaultAsync();

                return CreatedAtAction(
                    nameof(GetFuncionario),
                    new { id = idPessoaGerado },
                    funcioarioRetorno);
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

        //ALTERAR UM FUNCIONÁRIO, SEUS DADOS PESSOAIS E ENDEREÇOS
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, FuncionarioDTO dto)
        {
            //ABRINDO UMA TRANSAÇÃO NO BANCO DE DADOS
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //VALIDAR DADOS DA PESSOA
                if (dto.Pessoa == null)
                {
                    return BadRequest(new
                    {
                        mensagem = "DADOS DA PESSOA SÃO OBRIGATÓRIOS!"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Pessoa.Nome))
                {
                    return BadRequest(new
                    {
                        mensagem = "NOME DA PESSOA É OBRIGATÓRIO!"
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Cargo))
                {
                    return BadRequest(new
                    {
                        mensagem = "CARGO DO FUNCIONÁRIO É OBRIGATÓRIO!"
                    });
                }

                //BUSCAR O FUNCIONÁRIO E SEUS DADOS RELACIONADOS
                var funcionario = await _context.Funcionarios
                    .Include(f => f.IdpessoaNavigation)
                        .ThenInclude(p => p.Enderecos)
                    .FirstOrDefaultAsync(f => f.Idpessoa == id);

                //VERIFICAR SE O FUNCIONÁRIO EXISTE
                if (funcionario == null)
                {
                    return NotFound(new
                    {
                        mensagem = "FUNCIONÁRIO NÃO ENCONTRADO!"
                    });
                }

                //ATUALIZAR DADOS DO FUNCIONÁRIO
                funcionario.Cargo = dto.Cargo;
                funcionario.DataAdmissao = dto.DataAdmissao;

                //ATUALIZAR DADOS DA PESSOA
                funcionario.IdpessoaNavigation.Nome = dto.Pessoa.Nome;
                funcionario.IdpessoaNavigation.Cpf = dto.Pessoa.CPF;
                funcionario.IdpessoaNavigation.DataNascimento = dto.Pessoa.DataNascimento;
                funcionario.IdpessoaNavigation.Sexo = dto.Pessoa.sexo;
                funcionario.IdpessoaNavigation.Email = dto.Pessoa.email;
                funcionario.IdpessoaNavigation.Telefone = dto.Pessoa.telefone;
                funcionario.IdpessoaNavigation.AtivoPessoa = dto.Pessoa.AtivoPessoa;

                //PERCORRER OS ENDEREÇOS DA PESSOA
                foreach (var endPessoa in dto.Pessoa.Enderecos)
                {
                    //VERIFICAR SE O ENDEREÇO JÁ EXISTE (COM ID > 0)
                    if (endPessoa.IdEndereco > 0)
                    {
                        //BUSCAR O ENDEREÇO NO MODELO
                        var endereco = funcionario.IdpessoaNavigation.Enderecos
                            .FirstOrDefault(e => e.Idpessoa == endPessoa.IdEndereco);

                        //ATUALIZAR ENDEREÇO EXISTENTE
                        if (endereco != null)
                        {
                            endereco.Numero = endPessoa.Numero;
                            endereco.Cep = endPessoa.CEP;
                            endereco.Complemento = endPessoa.Complemento;
                        }
                    }
                    else
                    {
                        //ADICIONAR NOVO ENDEREÇO
                        funcionario.IdpessoaNavigation.Enderecos.Add(new Endereco
                        {
                            Numero = endPessoa.Numero,
                            Cep = endPessoa.CEP,
                            Complemento = endPessoa.Complemento
                        });
                    }
                }

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

        //EXCLUIR UM FUNCIONÁRIO
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //BUSCAR O FUNCIONÁRIO
                var funcionario = await _context.Funcionarios
                    .Include(f => f.IdpessoaNavigation)
                        .ThenInclude(p => p.Enderecos)
                    .FirstOrDefaultAsync(f => f.Idpessoa == id);

                if (funcionario == null)
                {
                    return NotFound(new
                    {
                        mensagem = "FUNCIONÁRIO NÃO ENCONTRADO!"
                    });
                }

                //VERIFICAR SE FUNCIONÁRIO TEM ATENDIMENTOS ASSOCIADOS
                var temAtendimentos = await _context.Atendimentoitems
                    .AnyAsync(ai => ai.Idfuncionario == id);

                if (temAtendimentos)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        mensagem = "NÃO É POSSÍVEL EXCLUIR UM FUNCIONÁRIO QUE POSSUI ATENDIMENTOS ASSOCIADOS!"
                    });
                }

                //REMOVER ENDEREÇOS
                foreach (var endereco in funcionario.IdpessoaNavigation.Enderecos.ToList())
                {
                    _context.Enderecos.Remove(endereco);
                }

                //REMOVER FUNCIONÁRIO
                _context.Funcionarios.Remove(funcionario);

                //REMOVER PESSOA
                _context.Pessoas.Remove(funcionario.IdpessoaNavigation);

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
