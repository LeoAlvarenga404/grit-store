"use client";

import type { ComponentProps } from "react";
import { useFormStatus } from "react-dom";
import { Button } from "@/components/ui/button";

// Disabled while its form's Server Function runs: a second click would send the form again.
export function SubmitButton({ pendingLabel, children, disabled, ...props }: ComponentProps<typeof Button> & { pendingLabel: string }) {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" disabled={pending || disabled} {...props}>
      {pending ? pendingLabel : children}
    </Button>
  );
}
