-- Active: 1789223976048@@127.0.0.1@1433@master
CREATE DATABASE RinhaBackend;

CREATE TABLE dbo.Clientes (
    Id INT IDENTITY(1,1) NOT NULL,
    Nome VARCHAR(50) NOT NULL,
    Saldo INT NOT NULL,
    Limite INT NOT NULL,

    CONSTRAINT PK_Clientes
        PRIMARY KEY (Id)
);

CREATE TABLE dbo.Transacoes (
    Id INT IDENTITY(1,1) NOT NULL,
    ClienteId INT NOT NULL,
    Valor INT NOT NULL,
    Tipo CHAR(1) NOT NULL,
    Descricao VARCHAR(10) NOT NULL,
    RealizadaEm DATETIME2 NOT NULL
        CONSTRAINT DF_Transacoes_RealizadaEm DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Transacoes
        PRIMARY KEY (Id),

    CONSTRAINT FK_Transacoes_Clientes
        FOREIGN KEY (ClienteId)
        REFERENCES dbo.Clientes(Id)
);

INSERT INTO dbo.Clientes (Nome, Saldo, Limite)
VALUES
    ('user 1', 0, 100000),
    ('user 2', 0, 80000),
    ('user 3', 0, 1000000),
    ('user 4', 0, 10000000),
    ('user 5', 0, 500000);