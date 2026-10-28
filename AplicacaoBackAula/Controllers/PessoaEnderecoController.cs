using AplicacaoBackAula.DTO;
using AplicacaoBackAula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacaoBackAula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PessoaEnderecoController : ControllerBase
    {
        private readonly BdAulaBackendContext _context;

        public PessoaEnderecoController(BdAulaBackendContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PessoaDTO>>> GetAll()
        {
            var pessoas = await _context.Pessoas
                .AsNoTracking()
                .Select(p => new PessoaDTO
                {
                    IdPessoa = p.Idpessoa,
                    Nome = p.Nome,
                    CPF = p.Cpf,
                    DataNascimento = p.DataNascimento,
                    sexo = p.Sexo,
                    email = p.Email,
                    telefone = p.Telefone,
                    AtivoPessoa = p.AtivoPessoa,
                    Enderecos = p.Enderecos.Select(e => new EnderecoDTO
                    {
                        IdEndereco = e.Idendereco,
                        CEP = e.Cep,
                        Numero = e.Numero,
                        Complemento = e.Complemento
                    }).ToList()
                })
                .ToListAsync();

            return Ok(pessoas);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PessoaDTO>> GetPessoa(int id)
        {
            var pessoa = await _context.Pessoas
                .AsNoTracking()
                .Where(p => p.Idpessoa == id)
                .Select(p => new PessoaDTO
                {
                    IdPessoa = p.Idpessoa,
                    Nome = p.Nome,
                    CPF = p.Cpf,
                    DataNascimento = p.DataNascimento,
                    sexo = p.Sexo,
                    email = p.Email,
                    telefone = p.Telefone,
                    AtivoPessoa = p.AtivoPessoa,
                    Enderecos = p.Enderecos.Select(e => new EnderecoDTO
                    {
                        IdEndereco = e.Idendereco,
                        CEP = e.Cep,
                        Numero = e.Numero,
                        Complemento = e.Complemento
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (pessoa is null)
                return NotFound(new { mensagem = "Cliente não encontrado." });

            return Ok(pessoa);
        }

        [HttpPost]
        public async Task<ActionResult<PessoaDTO>> Post([FromBody] PessoaDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome) ||
                string.IsNullOrWhiteSpace(dto.email) ||
                string.IsNullOrWhiteSpace(dto.sexo) ||
                dto.CPF <= 0)
            {
                return BadRequest(new { mensagem = "Informe nome, CPF, sexo e e-mail." });
            }

            var duplicado = await _context.Pessoas.AnyAsync(p =>
                p.Cpf == dto.CPF || p.Email == dto.email);
            if (duplicado)
                return Conflict(new { mensagem = "Já existe cliente com esse CPF ou e-mail." });

            var pessoa = new Pessoa
            {
                Nome = dto.Nome.Trim(),
                Cpf = dto.CPF,
                DataNascimento = dto.DataNascimento,
                Sexo = dto.sexo.Trim(),
                Email = dto.email.Trim(),
                Telefone = dto.telefone,
                AtivoPessoa = string.IsNullOrWhiteSpace(dto.AtivoPessoa) ? "S" : dto.AtivoPessoa,
                Enderecos = (dto.Enderecos ?? new List<EnderecoDTO>())
                    .Where(e => !string.IsNullOrWhiteSpace(e.CEP) &&
                                !string.IsNullOrWhiteSpace(e.Numero))
                    .Select(e => new Endereco
                    {
                        Cep = e.CEP!,
                        Numero = e.Numero!,
                        Complemento = e.Complemento ?? string.Empty
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
                    IdEndereco = e.Idendereco,
                    CEP = e.Cep,
                    Numero = e.Numero,
                    Complemento = e.Complemento
                }).ToList()
            };

            return CreatedAtAction(nameof(GetPessoa), new { id = pessoa.Idpessoa }, resposta);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] PessoaDTO dto)
        {
            var pessoa = await _context.Pessoas
                .Include(p => p.Enderecos)
                .FirstOrDefaultAsync(p => p.Idpessoa == id);

            if (pessoa is null)
                return NotFound(new { mensagem = "Cliente não encontrado." });

            if (string.IsNullOrWhiteSpace(dto.Nome) ||
                string.IsNullOrWhiteSpace(dto.email) ||
                string.IsNullOrWhiteSpace(dto.sexo) ||
                dto.CPF <= 0)
                return BadRequest(new { mensagem = "Informe nome, CPF, sexo e e-mail." });

            var duplicado = await _context.Pessoas.AnyAsync(p =>
                p.Idpessoa != id && (p.Cpf == dto.CPF || p.Email == dto.email));
            if (duplicado)
                return Conflict(new { mensagem = "Outro cliente já utiliza esse CPF ou e-mail." });

            pessoa.Nome = dto.Nome.Trim();
            pessoa.Cpf = dto.CPF;
            pessoa.DataNascimento = dto.DataNascimento;
            pessoa.Sexo = dto.sexo.Trim();
            pessoa.Email = dto.email.Trim();
            pessoa.Telefone = dto.telefone;
            pessoa.AtivoPessoa = string.IsNullOrWhiteSpace(dto.AtivoPessoa) ? "S" : dto.AtivoPessoa;

            foreach (var enderecoDto in dto.Enderecos ?? new List<EnderecoDTO>())
            {
                if (enderecoDto.IdEndereco > 0)
                {
                    var endereco = pessoa.Enderecos.FirstOrDefault(e => e.Idendereco == enderecoDto.IdEndereco);
                    if (endereco is null)
                        return BadRequest(new { mensagem = "O endereço informado não pertence a esse cliente." });

                    endereco.Cep = enderecoDto.CEP ?? endereco.Cep;
                    endereco.Numero = enderecoDto.Numero ?? endereco.Numero;
                    endereco.Complemento = enderecoDto.Complemento ?? string.Empty;
                }
                else if (!string.IsNullOrWhiteSpace(enderecoDto.CEP) &&
                         !string.IsNullOrWhiteSpace(enderecoDto.Numero))
                {
                    pessoa.Enderecos.Add(new Endereco
                    {
                        Cep = enderecoDto.CEP,
                        Numero = enderecoDto.Numero,
                        Complemento = enderecoDto.Complemento ?? string.Empty
                    });
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var pessoa = await _context.Pessoas
                .Include(p => p.Enderecos)
                .Include(p => p.Atendimentos)
                .FirstOrDefaultAsync(p => p.Idpessoa == id);

            if (pessoa is null)
                return NotFound(new { mensagem = "Cliente não encontrado." });

            if (pessoa.Atendimentos.Count > 0)
                return Conflict(new { mensagem = "Não é possível excluir um cliente que possui atendimentos registrados." });

            _context.Pessoas.Remove(pessoa);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
