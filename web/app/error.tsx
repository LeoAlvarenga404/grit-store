"use client";

import Link from "next/link";
import { useEffect } from "react";
import { StatusPage } from "@/components/status-page";
import { Button } from "@/components/ui/button";

export default function Error({ error, retry }: { error: Error & { digest?: string }; retry: () => void }) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <StatusPage code="Ops" title="Algo deu errado" text="Não conseguimos carregar esta página. Tente de novo em instantes.">
      <Button onClick={() => retry()}>Tentar de novo</Button>
      <Button asChild variant="outline">
        <Link href="/">Voltar para a loja</Link>
      </Button>
    </StatusPage>
  );
}
