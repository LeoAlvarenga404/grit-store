# 0008 — Migrations por módulo, aplicadas só em Development

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 1a
- **Altera:** ADR 0004 (o contrato `IModule` ganha um quarto membro)

## Contexto

O Catalog é o primeiro módulo com tabelas reais. Alguém precisa aplicar as migrations no AppHost e nos testes de integração, sem que o build (que gera o OpenAPI rodando a API) nem a produção toquem no banco por acidente.

## Decisão

- **Ferramenta:** `dotnet-ef` 10.0.4 num manifesto local (`dotnet-tools.json` na raiz). Cada módulo tem um `IDesignTimeDbContextFactory` para o `dotnet ef` montar o contexto sem a API nem connection string.
- **Onde ficam:** `Infrastructure/Migrations` de cada módulo, marcadas como `generated_code = true` no `.editorconfig`. Nunca são editadas à mão.
- **Quem aplica:** a própria API, depois de `builder.Build()`, **só em Development**, chamando o novo `IModule.MigrateDatabaseAsync` de cada módulo. Development cobre o AppHost e o `WebApplicationFactory` dos testes. A geração do OpenAPI no build roda como Production, então nunca abre conexão.
- **Produção não aplica nada:** quando existir deploy, um `dotnet ef migrations bundle` revisado faz isso.
- **Rejeitado:** um projeto migrador rodado pelo AppHost (mais um projeto, imagem e disco para o mesmo efeito).
- O módulo de teste (probe) implementa o método como no-op e continua com `EnsureCreated` antes de o host subir, porque `EnsureCreated` não faz nada quando já existe qualquer tabela no banco.

## Exceção: SQL que o EF não modela (sub-plano 1b)

A busca do catálogo precisa de objetos que o modelo do EF não representa: a configuração de texto `portuguese_unaccent`, a função `immutable_unaccent`, a coluna gerada `search_vector` (`tsvector`) e os índices GIN/trigram sobre expressões. Esses objetos ficam fora do modelo de propósito.

- A migration é criada pelo `dotnet ef migrations add` como sempre; **só os corpos de `Up` e `Down` dela são escritos à mão**, em SQL. O `.Designer.cs` e o snapshot continuam gerados e nunca são editados.
- **Limite 1:** o PostgreSQL recusa `ALTER COLUMN … TYPE` em `name` ou `description` enquanto a coluna gerada depende delas. Uma migration futura que mude esses tipos precisa remover e recriar `search_vector` em SQL.
- **Limite 2:** `immutable_unaccent` é declarada `IMMUTABLE` sobre o arquivo de regras do `unaccent`. Se as regras mudarem (por exemplo, numa atualização do PostgreSQL), o índice e os vetores gravados precisam ser reconstruídos.

## Consequências

- O EF 9+ lança exceção em `Migrate` quando o modelo mudou sem migration: uma migration esquecida derruba a subida em Development e os testes.
- Um módulo novo com persistência copia três peças: a configuração, a factory de design e o `MigrateDatabaseAsync`, com `HasDefaultSchema("<módulo>")` e a tabela de histórico no próprio schema.
- O módulo Payments ganhou o schema `payments` com a migration `InitialPayments`, só para a trilha de eventos do webhook do Stripe (emenda 2026-10-17, ADR 0018).
