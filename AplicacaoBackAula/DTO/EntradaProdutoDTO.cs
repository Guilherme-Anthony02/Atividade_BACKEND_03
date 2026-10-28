using System;
namespace AplicacaoBackAula.DTO
{
    public class EntradaProdutoDTO
    {
        public int IdEntradaProduto { get; set; }
        public int IdItem { get; set; }
        public DateOnly DataEntradaProduto { get; set; }
        public int QuantidadeEntrada { get; set; }
        public string? Unidade { get; set; }
        public decimal ValorUnitarioEntrada { get; set; }

        // Dados auxiliares retornados
        public int QuantidadeEstoque { get; set; }
        public ProdutoDTO? Produto { get; set; }
    }
}
