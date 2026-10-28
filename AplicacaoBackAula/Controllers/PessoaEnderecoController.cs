using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AplicacaoBackAula.DTO;
using AplicacaoBackAula.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AplicacaoBackAula.Controllers
{
    //ANOTAÇÕES DE IDENTIFICAÇÃO DE CONTROLLER
    [ApiController]
    [Route("api/[controller]")]
    public class PessoaEnderecoController : ControllerBase
    {
        //DECLARAÇÃO DO ATRIBUTO DE CONTEXTO
        private readonly BdAulaBackendContext _context;

        //DECLARAÇÃO DO MÉTODO CONSTRUTOR
        public PessoaEnderecoController(BdAulaBackendContext context)
        {
            //INSTANCIA DO ATRIBUTO CONSTRUTOR
            _context = context;
        }
        //DECLARAÇÃO DO MÉTODO PARA LISTAR TODAS AS PESSOAS E SEU ENDEREÇOS
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PessoaDTO>>> GetAll()
        {
            var pessoas = await _context.Pessoas
                .Select(p => new PessoaDTO
                {
                    IdPessoa = p.Idpessoa,
                    Nome = p.Nome,
                    CPF = (long)(long?)p.Cpf,
                    DataNascimento = p.DataNascimento,
                    sexo = p.Sexo,
                    email = p.Email,
                    telefone = (long)p.Telefone,
                    AtivoPessoa = p.AtivoPessoa,

                    Enderecos = p.Enderecos.Select(e => new EnderecoDTO
                    {
                        IdPessoa = e.Idpessoa,
                        CEP = e.Cep,
                        Numero = e.Numero,
                        Complemento = e.Complemento,


                    }).ToList()

                }).ToListAsync();


            return Ok(pessoas);
        }//FIM MÉTODO GetAll

        [HttpGet("{id}")]
        public async Task<ActionResult<PessoaDTO>> GetPessoa(int id)
        {
            var pessoa = await _context.Pessoas
                .Where(p => p.Idpessoa == id)
                       .Select(p => new PessoaDTO
                       {
                           IdPessoa = p.Idpessoa,
                           Nome = p.Nome,
                           CPF = (long)(long?)p.Cpf,
                           DataNascimento = p.DataNascimento,
                           sexo = p.Sexo,
                           email = p.Email,
                           telefone = (long)p.Telefone,
                           AtivoPessoa = p.AtivoPessoa,

                           Enderecos = p.Enderecos.Select(e => new EnderecoDTO
                           {
                               IdPessoa = e.Idpessoa,
                               CEP = e.Cep,
                               Numero = e.Numero,
                               Complemento = e.Complemento,


                           }).ToList()

                       }).ToListAsync();


            return Ok(pessoa);
        }//FIM MÉTODO Getpessoa

        //MÉTODO PARA CADASTRAR UMA PESSOA
        [HttpPost]
        public async Task<ActionResult<PessoaDTO>> Post(PessoaDTO dto)
        {
            var pessoa = new Pessoa
            {
                Nome = dto.Nome,
                Cpf = dto.CPF,
                DataNascimento = dto.DataNascimento,
                Sexo = dto.sexo,
                Email = dto.email,
                Telefone = dto.telefone,
                AtivoPessoa = dto.AtivoPessoa,

                Enderecos = dto.Enderecos.Select(e => new Endereco
                {
                    Cep = e.CEP,
                    Numero = e.Numero,
                    Complemento = e.Complemento
                    }).ToList()
                };

            _context.Pessoas.Add(pessoa);

            await _context.SaveChangesAsync();
            var resposta = new PessoaDTO
            {
                IdPessoa = pessoa.Idpessoa,
                Nome = pessoa.Nome,
                CPF = pessoa.Cpf,
                DataNascimento = pessoa.DataNascimento,
                sexo = pessoa.Sexo,
                email = pessoa.Email,
                telefone = pessoa.Telefone,
                AtivoPessoa = pessoa.AtivoPessoa,

                Enderecos = pessoa.Enderecos.Select(e => new EnderecoDTO
                {
                    CEP = e.Cep,
                    Numero = e.Numero,
                    Complemento = e.Complemento
                
                }).ToList()
            };

            return CreatedAtAction(
                nameof(GetPessoa),
                new { id = pessoa.Idpessoa },
                resposta);
        }

        //MÉTODO PARA ALTERAR UMA PESSOA
        [HttpPut("{id}")]
        public async Task<ActionResult<Pessoa>> Put(int id, PessoaDTO dto)
        {
            //BUSCA A PESSOA E SEUS ENDEREÇOS E ATRIBUI A VARIÁVEL PESSOA
            var pessoa = await _context.Pessoas
                .Include(p => p.Enderecos)
                .FirstOrDefaultAsync(p => p.Idpessoa == id);

            //VERIFICA SE A PESSOA EXISTE
            if (pessoa == null)
            {
                return NotFound();
            }

            //PROJETA OS DADOS DO PessoaDTO PARA O MODELO Pessoa ORIUNDA DO BD
            pessoa.Nome = dto.Nome;
            pessoa.Cpf = dto.CPF;
            pessoa.DataNascimento = dto.DataNascimento;
            pessoa.Sexo = dto.sexo;
            pessoa.Email = dto.email;
            pessoa.Telefone = dto.telefone;
            pessoa.AtivoPessoa = dto.AtivoPessoa;

            //PERCORRE OS ENDEREÇOS DA PESSOA
            foreach (var endPessoa in dto.Enderecos)
            {
                //VERIFICA SE O ID VIM JA EXISTE COM A PESSOA
                if (endPessoa.IdPessoa > 0)
                {
                    //BUSCA O ENDEREÇO DA PESSOA NO ATRIBUTO DO MODELO Pessoa.
                    var endereco = pessoa.Enderecos
                        .FirstOrDefault(e => e.Idpessoa == endPessoa.IdPessoa);

                    //CASO NÃO ENCONTRE EMITE UMA MENSAGEM DE QUE O ENDEREÇO NÃO PERTENCE A ESSA PESSOA
                    if (endereco != null)
                    {
                        return BadRequest($"O endereço não pertence a essa pessoa");
                    }

                    //AO LOCALIZAR PROJETA NO MODELO Endereco OS DADOS VINDO DO ATRIBUTO Endereços
                    //DO MODELO O PESSOA QUE VENHO DO PessoaDto
                    endereco.Numero = endPessoa.Numero;
                    endereco.Cep = endPessoa.CEP;
                    endereco.Complemento = endPessoa.Complemento;
                }
                else
                {
                    //CASO O ID NÃO SEJA MAIOR QUE 0(ZERO) É UM NOVO ENDEREÇO A SER ADICIONADO A PESSOA
                    pessoa.Enderecos.Add(new Endereco
                    {
                        Numero = endPessoa.Numero,
                        Cep = endPessoa.CEP,
                        Complemento = endPessoa.Complemento
                    });
                }
            }

            //SALVA NO BANCO DE DADOS
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //MÉTODO PARA EXCLUIR UMA PESSOA
        [HttpDelete("{id}")]
        public async Task<ActionResult<Pessoa>> Delete(int id)
        {
            //BUSCA A PESSOA E SEUS ENDREÇOS E ATRIBUI A VARIÁVEL PESSOA
            var pessoa = await _context.Pessoas
                .Include(p => p.Enderecos)
                .FirstOrDefaultAsync(p => p.Idpessoa == id);

            //VERIFICA SE A PESSOA EXISTE
            if (pessoa == null)
            {
                return NotFound();
            }

            //PREPARA A INSTRUÇÃO PARA EXECUTA  NO BANCO DE DADOS
            _context.Pessoas.Remove(pessoa);

            //SALVA NO BANCO DE DADOS
            await _context.SaveChangesAsync();

            return NoContent();
        }



    }//FIM DA CLASSE CONTROLLER PessoaEnderecoController
}//FIM DO NAMESPACE

