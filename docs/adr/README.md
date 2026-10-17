# Decisões de arquitetura (ADRs)

Cada ADR registra uma decisão no sub-plano em que ela apareceu no código. Todas estão aceitas. Uma emenda posterior aparece no próprio ADR, com a data.

| # | Decisão | Sub-plano |
|---|---|---|
| [0001](0001-money-decimal-sem-arredondamento.md) | `Money` em `decimal`, sem arredondamento e com teto | 0a |
| [0002](0002-xunit-v3-microsoft-testing-platform.md) | xUnit v3 sobre o Microsoft.Testing.Platform | 0a |
| [0003](0003-seaweedfs-como-armazenamento-s3.md) | SeaweedFS como armazenamento S3 local | 0b |
| [0004](0004-composicao-explicita-dos-modulos.md) | Composição explícita dos módulos com `IModule` | 0b |
| [0005](0005-transacao-por-comando.md) | Uma transação por comando, outbox por módulo | 0b (emenda no 4b) |
| [0006](0006-erros-como-problem-details.md) | Erros como ProblemDetails | 0b |
| [0007](0007-concorrencia-otimista-por-agregado.md) | Concorrência otimista por agregado com `xmin` | 1a |
| [0008](0008-migrations-por-modulo-so-em-development.md) | Migrations por módulo, aplicadas só em Development | 1a (emenda no 1b) |
| [0009](0009-snake-case-e-atributos-em-jsonb.md) | Nomes em snake_case e atributos em `jsonb` | 1a |
| [0010](0010-slug-unico-e-imutavel.md) | Slug único e imutável | 1a |
| [0011](0011-contrato-json-estrito.md) | Contrato JSON estrito | 1a |
| [0012](0012-moeda-unica-brl.md) | Uma moeda na loja: BRL | 1a |
| [0013](0013-busca-do-catalogo.md) | Busca do catálogo (full text + trigramas, facetas, keyset) | 1b |
| [0014](0014-identidade-e-tokens.md) | Identidade e tokens (JWT, refresh com detecção de reuso) | 2a |
| [0015](0015-checkout-com-saga.md) | Checkout com saga e compensação | 3–4d |
| [0016](0016-imagens-de-produto-em-s3.md) | Imagens de produto em S3 com URL pré-assinada | 5a |
| [0017](0017-cache-do-catalogo.md) | Cache do catálogo com HybridCache | 5b |
