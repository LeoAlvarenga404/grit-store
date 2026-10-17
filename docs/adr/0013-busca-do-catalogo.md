# 0013 — Busca do catálogo

- **Status:** aceita
- **Data:** 2026-10-16
- **Sub-plano:** 1b

## Contexto

O roadmap pede busca em português que aguente acentos, plurais e erros de digitação, com facetas, autocomplete, paginação por cursor e detalhe por slug, sem um motor de busca à parte. O design (§3) reserva o Dapper para a listagem pesada e usa o EF nas leituras simples.

## Decisão

- **Rotas públicas em `/api/v1/catalog`:** `products` (busca e listagem), `products/{slug}`, `facets`, `suggestions`. Um prefixo próprio evita reservar slugs como `facets`. O admin continua em `/api/v1/admin`.
- **Documento de busca:** coluna gerada `search_vector` (`tsvector`, `STORED`) com nome (peso A) e descrição (peso C), na configuração `catalog.portuguese_unaccent` (unaccent seguido do stemmer português). Criada em SQL bruto (exceção do ADR 0008); o EF não mapeia a coluna.
- **Consulta:** `plainto_tsquery` (toda palavra precisa casar, sem operadores) OU semelhança de trigramas `<<%` no nome sem acentos, no limite padrão 0,5 do `pg_trgm`. A tolerância depende da semelhança, não do tipo de erro: uma letra trocada no meio só é tolerada em palavras de 8 letras ou mais, e uma letra faltando em palavras de 7 ou mais (`camizeta`, `mochla` acham; `tenys`, `bita` não). Algumas transposições passam (`impermeavle`, 0,6) e outras não (`lantrena`). Descrição não tem tolerância a erro. Nenhum `SET` de sessão, porque vazaria no pool de conexões.
- **Ranking:** `round(ts_rank + strict_word_similarity, 6)` como `numeric`, desempate por `id DESC`. Sem `q`, a ordem é `id DESC` (UUIDv7: o mais novo primeiro, com precisão de milissegundo).
- **Paginação keyset:** cursor `"{rank}_{id}"` em cultura invariante, opaco para o cliente e validado (cursor inválido é 400). O `numeric` arredondado faz o cursor voltar exato. `limit` de 1 a 50 (padrão 20).
- **Visibilidade:** um produto é público quando tem ao menos um SKU (sem flag de publicação na v1).
- **Filtros:** categoria por slug; preço mínimo e máximo inclusivos; `attribute=chave:valor` repetível. Preço e atributos precisam valer para o **mesmo SKU**, com os atributos do produto mesclados por baixo dos do SKU (`p.attributes || s.attributes`: o valor do SKU vence).
- **Facetas:** conjuntivas, contam produtos e cada contagem é igual ao que a busca devolve para aquele refinamento. Um produto com SKUs em duas faixas de preço conta nas duas. Faixas fixas e semiabertas `[min, max)`. Para listar uma faixa, o cliente intersecta com o filtro de preço que já enviou: `minPrice` = o maior entre `min` e o mínimo atual, `maxPrice` = o menor entre `max − 0,01` e o máximo atual.
- **Autocomplete:** prefixo de qualquer palavra (`<%`) ou palavra com erro (`<<%`), no máximo 8, nomes que começam com o texto primeiro. Usa Dapper, porque o EF não expressa esses operadores sem um pacote extra: desvio do design §3. O cache no Redis fica para o 5b (chave normalizada; invalidação em toda escrita de produto, inclusive o primeiro SKU).
- **Detalhe por slug com EF.** Só o slug canônico responde, para cada página ter um endereço. O texto da rota precisa antes ter o formato de slug, porque `string.Normalize` lança exceção com U+FFFE.
- **Dapper só em handlers de query**, na conexão do `NpgsqlDataSource` da API, uma por consulta (ADR 0005). Um teste de arquitetura impede handler de `ICommand` que use Dapper diretamente.
- **Texto de entrada:** `TextRule` rejeita U+0000 e U+FFFE em todos os campos de texto (400), porque o PostgreSQL recusa o primeiro e o .NET lança exceção ao normalizar o segundo.

## Consequências

- Os testes de busca usam um banco próprio (`search_corpus`) no mesmo contêiner, com um corpus fixo e resultados exatos para acentos, plurais, erros e ranking.
- Limites conhecidos: erros em palavras curtas não são tolerados; uma consulta só com stopwords (como "de") casa pelo ramo de trigramas com todo nome que tem a palavra; sem `q`, cada página ordena todos os produtos públicos, e as facetas sem filtro percorrem todos os SKUs a cada pedido (o cache do 5b resolveu isso: o k6 do 5b mediu cerca de 1,1 s por faceta sem filtro sobre 100 mil SKUs sem cache e 43,6 ms de p95 com cache; ver ADR 0017 e o README).
- Mudar o tipo de `name` ou `description` exige recriar `search_vector` em SQL (ADR 0008).
