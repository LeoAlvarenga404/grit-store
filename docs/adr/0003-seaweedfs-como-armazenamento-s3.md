# 0003 — SeaweedFS como armazenamento S3 local

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 0b (questão Q-M1 do roadmap)

## Contexto

O design (§10) pede um armazenamento compatível com S3 no AppHost para as imagens dos produtos. A escolha óbvia era o MinIO, mas em outubro de 2026:

- a imagem `minio/minio` não existe mais no Docker Hub;
- o repositório do MinIO no GitHub está arquivado;
- a integração de MinIO do Aspire Community Toolkit se declara obsoleta.

## Decisão

Usar o **SeaweedFS** com a API S3 ligada (`AddSeaweedFS("storage").WithS3()`, do CommunityToolkit 13.6.0). A imagem é `chrislusf/seaweedfs:4.46`, com licença Apache-2.0. O RustFS ficou como segunda opção: a versão 1.0 só saiu em setembro de 2026.

## Consequências

- O código das imagens (entregue no 5a, ADR 0016) fala S3 puro, sem nada específico do SeaweedFS. Trocar de servidor é uma linha no AppHost.
- A imagem tem uns 94 MB e é baixada na primeira vez que o AppHost sobe.
- No 0b o recurso só foi declarado; a API passou a referenciá-lo no 5a, com as imagens de produto (ADR 0016).
