import type { Metadata } from "next";
import Link from "next/link";
import { AuthShell } from "@/components/auth-shell";
import { ErrorMessage } from "@/components/error-message";
import { SubmitButton } from "@/components/submit-button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { register } from "@/lib/session/actions";

export const metadata: Metadata = { title: "Criar conta" };

const errorMessages: Record<string, string> = {
  "identity.email_taken": "Este e-mail já tem uma conta.",
  "validation.failed": "Confira o e-mail e use uma senha de 8 a 128 caracteres.",
  http_429: "Muitas tentativas. Aguarde um minuto e entre pela tela de login.",
};

export default async function RegisterPage({ searchParams }: { searchParams: Promise<{ error?: string }> }) {
  const { error } = await searchParams;

  return (
    <AuthShell title="Criar conta" subtitle="Leva um minuto: e-mail e senha.">
      {error && <ErrorMessage className="mb-6">{errorMessages[error] ?? "Não foi possível criar a conta. Tente de novo."}</ErrorMessage>}
      <form action={register} className="space-y-5">
        <div className="space-y-2"><Label htmlFor="email">E-mail</Label><Input id="email" name="email" type="email" autoComplete="email" required className="h-11" /></div>
        <div className="space-y-2">
          <Label htmlFor="password">Senha</Label>
          <Input
            id="password"
            name="password"
            type="password"
            autoComplete="new-password"
            minLength={8}
            maxLength={128}
            required
            aria-describedby="password-hint"
            className="h-11"
          />
          <p id="password-hint" className="text-xs text-muted-foreground">De 8 a 128 caracteres.</p>
        </div>
        <SubmitButton size="lg" className="w-full" pendingLabel="Criando conta…">Criar conta</SubmitButton>
      </form>
      <p className="mt-6 text-center text-sm text-muted-foreground">Já tem conta? <Link href="/login" className="font-medium text-foreground underline underline-offset-4">Entrar</Link></p>
    </AuthShell>
  );
}
