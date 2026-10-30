namespace AplicacaoBackAula.DTO
{
    public class PedidoCheckoutDTO
    {
        public int IdPessoa { get; set; }
        public List<PedidoCheckoutItemDTO> Itens { get; set; } = new();
    }

    public class PedidoCheckoutItemDTO
    {
        public int IdItem { get; set; }
        public int Quantidade { get; set; }
        // A tabela de atendimento exige um funcionário responsável por item.
        public int IdFuncionario { get; set; }
    }

    public class AtualizarSituacaoPedidoDTO
    {
        public string Situacao { get; set; } = string.Empty;
    }
}
