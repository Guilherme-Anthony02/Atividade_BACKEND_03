using System;

namespace AplicacaoBackAula.DTO
{
    public class FuncionarioDTO
    {
        public int IdPessoa { get; set; }
        public string? Cargo { get; set; }
        public DateOnly DataAdmissao { get; set; }

        // Dados da pessoa vinculada
        public PessoaDTO? Pessoa { get; set; }
    }
}
