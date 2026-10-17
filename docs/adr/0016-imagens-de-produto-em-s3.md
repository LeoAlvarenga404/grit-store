# 0016 — Imagens de produto em S3 com URL pré-assinada

- **Status:** aceita
- **Data:** 2026-10-17
- **Sub-plano:** 5a (a parte de imagens que saiu da 1c)

## Contexto

O admin precisa anexar fotos aos produtos, e a vitrine precisa mostrá-las. O armazenamento é o SeaweedFS com API S3 (ADR 0003), declarado no AppHost. A API não deve trafegar os bytes da imagem, e nenhuma chave de acesso pode ir para o repositório.

## Decisão

- **Upload por PUT pré-assinado numa chave de staging.** A API devolve uma URL válida por 5 minutos para `uploads/{productId}/{guid-v7}.{ext}`, com o `Content-Type` assinado. Nenhuma URL de PUT é emitida para `products/`. Quem faz o PUT é a Server Action do Next.js: não precisa de CORS no SeaweedFS nem de JavaScript no navegador.
- **Confirmação publica numa chave final (emenda 2026-10-17, revisão da M5).** A chave enviada precisa estar em `uploads/{productId}/`. A API faz HEAD nela e só aceita jpeg, png ou webp de até 2 MB. Depois copia o objeto no servidor para `products/{productId}/{mesmo arquivo}`, com a condição de ETag igual ao do HEAD, anexa a chave final e apaga o staging depois do commit. Um arquivo fora da regra, ou trocado entre o HEAD e a cópia, é apagado e a resposta é `catalog.invalid_image`. Sem essa cópia, a URL de PUT, ainda válida, sobrescrevia a imagem já confirmada com um arquivo de mais de 2 MB.
- **No domínio, o produto guarda só as chaves:** até 8 imagens, na ordem do upload, e a primeira é a capa. A remoção é feita pelo nome do arquivo.
- **Leitura por GET pré-assinado de 1 hora, assinado a cada resposta,** em vez de bucket público. O SeaweedFS do toolkit não tem identidade anônima, e um bucket público exigiria versionar um `s3.json` com chaves literais.
- **`ProductImageStorage` é uma classe concreta**, sem interface, porque não existe segunda implementação. Os testes usam um SeaweedFS real (Testcontainer) só nas classes de imagem. O cliente S3 é criado sob demanda, então hosts sem `ConnectionStrings:storage` nunca o tocam. O bucket é criado na subida em Development, junto com as migrations (ADR 0008).
- **AWSSDK.S3** com endereçamento por caminho e o protocolo das URLs assinadas tirado do esquema do endpoint, porque o SDK assina `https://` por padrão e o armazenamento local fala `http`.

## Consequências

- As URLs de leitura mudam a cada resposta, então o navegador não reaproveita a imagem do cache. O cache de catálogo (5b) guarda só as chaves e gera as URLs na hora, o que mantém o prazo de 1 hora fora do TTL.
- O seed de Development grava as imagens direto em `products/{productId}/`, sem staging, só na subida e só para produtos novos (ADR 0019).
- Objetos órfãos (staging sem confirmação, produto apagado) ficam no armazenamento. Uma limpeza periódica fica para depois.
- O SeaweedFS 4.46 aceita PUT, HEAD e GET pré-assinados mesmo sem o bucket existir, então a criação do bucket na subida não é coberta por teste.
