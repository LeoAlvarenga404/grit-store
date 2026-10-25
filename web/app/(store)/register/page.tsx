import type { Metadata } from "next";
import Link from "next/link";
import { AuthShell } from "@/components/auth-shell";
import { ErrorMessage } from "@/components/error-message";
import { RegisterForm } from "./register-form";

export const metadata: Metadata = { title: "Criar conta" };

const errorMessages: Record<string, string> = {
  "identity.email_taken": "Este e-mail já tem uma conta.",
  "validation.failed": "Confira o e-mail e use uma senha que atenda a todas as regras.",
  http_429: "Muitas tentativas. Aguarde um minuto e entre pela tela de login.",
};

export default async function RegisterPage({ searchParams }: { searchParams: Promise<{ error?: string }> }) {
  const { error } = await searchParams;

  return (
    <AuthShell title="Criar conta" subtitle="Leva um minuto: e-mail e uma senha forte.">
      {error && <ErrorMessage className="mb-6">{errorMessages[error] ?? "Não foi possível criar a conta. Tente de novo."}</ErrorMessage>}
      <RegisterForm />
      <p className="mt-6 text-center text-sm text-muted-foreground">Já tem conta? <Link href="/login" className="font-medium text-foreground underline underline-offset-4">Entrar</Link></p>
    </AuthShell>
  );
}
