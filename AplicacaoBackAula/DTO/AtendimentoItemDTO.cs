namespace AplicacaoBackAula.DTO
{
    public class AtendimentoItemDTO
    {
        public int IdAtendimento { get; set; }

        public int IdItem { get; set; }

        public int IdFuncionario { get; set; }

        public int QuantidadeItens { get; set; }

        public decimal ValorunitarioEntrada { get; set; }

        public string? ItemNome { get; set; }

        public string? DescricaoItem { get; set; }

        public decimal PrecoItem { get; set; }

        public string? NomeFuncionario { get; set; }

        public string? CargoFuncionario { get; set; }
    }
}
