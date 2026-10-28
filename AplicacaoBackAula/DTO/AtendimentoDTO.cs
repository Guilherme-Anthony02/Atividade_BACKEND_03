namespace AplicacaoBackAula.DTO
{
    public class AtendimentoDTO
    {
        public int IdAtendimento { get; set; }

        public int IdPessoa { get; set; }

        public DateOnly DataAtendimento { get; set; }

        public string StatusAtendimento { get; set; } = null!;

        public List<AtendimentoItemDTO> AtendimentoItens { get; set; } = new();
    }
}
