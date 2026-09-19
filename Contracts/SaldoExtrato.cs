namespace RinhaBackend.Api.Contracts;

public sealed class SaldoExtrato
{
    public int Total { get; set; }
    public DateTime Data_Extrato { get; set; }
    public int Limite { get; set; }
}