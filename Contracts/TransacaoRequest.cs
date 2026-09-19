namespace RinhaBackend.Api.Contracts;

public record TransacaoRequest(
    int Valor,
    string Tipo,
    string Descricao
);