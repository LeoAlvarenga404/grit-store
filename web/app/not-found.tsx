import type { Metadata } from "next";
import Link from "next/link";
import { StatusPage } from "@/components/status-page";
import { Button } from "@/components/ui/button";

export const metadata: Metadata = { title: "Página não encontrada" };

export default function NotFound() {
  return (
    <StatusPage code="404" title="Página não encontrada" text="O endereço pode ter mudado ou o produto saiu de linha.">
      <Button asChild>
        <Link href="/">Voltar para a loja</Link>
      </Button>
      <Button asChild variant="outline">
        <Link href="/catalog">Ver produtos</Link>
      </Button>
    </StatusPage>
  );
}
