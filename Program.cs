using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;
using RinhaBackend.Api.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddScoped<IDbConnection>(_ => 
    new SqlConnection(
        builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/clientes/{id}/transacoes", async (int id, TransacaoRequest request, IDbConnection db) =>
{
    if (id < 1 || id > 5)
        return Results.NotFound();
    
    var payloadInvalido =
        request.Valor <= 0 ||
        request.Tipo is not ("c" or "d") ||
        string.IsNullOrEmpty(request.Descricao) ||
        request.Descricao.Length > 10;

    if (payloadInvalido)
        return Results.UnprocessableEntity();
    
    var valorComSinal = request.Tipo == "c" ? request.Valor : -request.Valor;
    
    db.Open();

    using var transaction = db.BeginTransaction();

    try
    {
        var cliente = await db.QuerySingleOrDefaultAsync(
            """
                UPDATE dbo.Clientes
                SET Saldo = Saldo + @ValorComSinal
                OUTPUT 
                    INSERTED.Saldo,
                    INSERTED.Limite
                WHERE Id = @Id
                    AND Saldo + @ValorComSinal >= -Limite;
                """,
            new
            {
                Id = id,
                ValorComSinal = valorComSinal,
            },
            transaction
        );

        if (cliente is null)
        {
            transaction.Rollback();

            return Results.UnprocessableEntity();
        }

        await db.ExecuteAsync(
            """
            INSERT INTO dbo.Transacoes
                (ClienteId, Valor, Tipo, Descricao)
            VALUES 
                (@ClienteId, @Valor, @Tipo, @Descricao)
            """,
            new
            {
                ClienteId = id,
                Valor = request.Valor,
                Tipo = request.Tipo,
                Descricao = request.Descricao
            },
            transaction
        );
        
        transaction.Commit();

        return Results.Ok(new
        {
            limite = cliente.Limite,
            saldo = cliente.Saldo,
        });
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
});

app.MapGet("/clientes/{id}/extrato", async (int id, IDbConnection db) =>
{
    if (id < 1 || id > 5)
        return Results.NotFound();
    
    var client = await db.QuerySingleOrDefaultAsync<SaldoExtrato>(
        """
        SELECT saldo as total, SYSUTCDATETIME() AS data_extrato, limite 
        FROM dbo.Clientes 
        WHERE Id = @id
        """,
        new { id }
    );
    
    if (client is null)
        return Results.NotFound();

    var lastTransactions = await db.QueryAsync(
        """
        SELECT TOP 10 Valor as valor, Tipo as tipo, Descricao as descricao, RealizadaEm as realizada_em
        FROM dbo.Transacoes
        WHERE ClienteId = @id
        ORDER BY RealizadaEm DESC
        """,
        new { id }
    );

    return Results.Ok(new
    {
        saldo = new
        {
            total = client.Total,
            data_extrato = DateTime.SpecifyKind(
                (DateTime)client.Data_Extrato,
                DateTimeKind.Utc
            ),
            limite = client.Limite
        },
        ultimas_transacoes = lastTransactions
    });
});

app.Run();
