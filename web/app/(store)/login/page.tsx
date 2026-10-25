import type { Metadata } from "next";
import Link from "next/link";
import { AuthShell } from "@/components/auth-shell";
import { ErrorMessage } from "@/components/error-message";
import { SubmitButton } from "@/components/submit-button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { logIn } from "@/lib/session/actions";
import { safeReturnPath } from "@/lib/session/return-path";

export const metadata: Metadata = { title: "Entrar" };

const errorMessages: Record<string, string> = {
  "identity.invalid_credentials": "E-mail ou senha incorretos.",
  "identity.locked_out": "Muitas senhas erradas. Tente de novo em 15 minutos.",
  "validation.failed": "Confira o e-mail e a senha.",
  http_429: "Muitas tentativas. Aguarde um minuto e tente de novo.",
};

export default async function LoginPage({ searchParams }: { searchParams: Promise<{ error?: string; next?: string }> }) {
  const { error, next } = await searchParams;
  const returnPath = safeReturnPath(next);
  const registerHref = returnPath === "/" ? "/register" : `/register?next=${encodeURIComponent(returnPath)}`;

  return (
    <AuthShell title="Entrar" subtitle="Acesse sua conta para ver a sacola e os pedidos.">
      {error && <ErrorMessage className="mb-6">{errorMessages[error] ?? "Não foi possível entrar. Tente de novo."}</ErrorMessage>}
      <form action={logIn} className="space-y-5">
        <input type="hidden" name="next" value={returnPath} />
        <div className="space-y-2"><Label htmlFor="email">E-mail</Label><Input id="email" name="email" type="email" autoComplete="email" required className="h-11" /></div>
        <div className="space-y-2"><Label htmlFor="password">Senha</Label><Input id="password" name="password" type="password" autoComplete="current-password" required className="h-11" /></div>
        <SubmitButton size="lg" className="w-full" pendingLabel="Entrando…">Entrar</SubmitButton>
      </form>
      <p className="mt-6 text-center text-sm text-muted-foreground">Não tem conta? <Link href={registerHref} className="font-medium text-foreground underline underline-offset-4">Criar conta</Link></p>
    </AuthShell>
  );
}
