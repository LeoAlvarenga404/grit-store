"use client";

import { SubmitButton } from "@/components/submit-button";
import { Button } from "@/components/ui/button";
import { Dialog, DialogClose, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { cancelOrder } from "../actions";

export function CancelOrderDialog({ orderId, orderNumber }: { orderId: string; orderNumber: string }) {
  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button variant="outline">Cancelar pedido</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Cancelar o pedido {orderNumber}?</DialogTitle>
          <DialogDescription>O pagamento é estornado e o pedido não pode ser reaberto.</DialogDescription>
        </DialogHeader>
        <form action={cancelOrder}>
          <input type="hidden" name="orderId" value={orderId} />
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Voltar
              </Button>
            </DialogClose>
            <SubmitButton variant="destructive" pendingLabel="Cancelando…">
              Sim, cancelar
            </SubmitButton>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
