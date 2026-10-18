import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { productDetails } from "@/lib/product-variants";

// The tabs primitive sets the list height and the underline offset under the group-data orientation variant, so the overrides use the same variant.
const trigger =
  "h-11 flex-none rounded-none px-0 py-0 text-sm font-semibold tracking-[0.08em] uppercase group-data-[orientation=horizontal]/tabs:after:bottom-[-2px]";

export function ProductTabs({ description, attributes }: { description: string; attributes: Record<string, string> }) {
  const details = productDetails(attributes);

  return (
    <Tabs defaultValue="description" className="mt-16 max-w-3xl">
      <TabsList variant="line" className="w-full justify-start gap-8 border-b p-0 group-data-[orientation=horizontal]/tabs:h-auto">
        <TabsTrigger value="description" className={trigger}>
          Descrição
        </TabsTrigger>
        {details.length > 0 && (
          <TabsTrigger value="details" className={trigger}>
            Detalhes
          </TabsTrigger>
        )}
      </TabsList>
      <TabsContent value="description" className="pt-6 text-sm leading-relaxed whitespace-pre-line text-muted-foreground">
        {description}
      </TabsContent>
      {details.length > 0 && (
        <TabsContent value="details" className="pt-6">
          <dl className="grid gap-x-8 gap-y-3 text-sm sm:grid-cols-[14rem_1fr]">
            {details.map((detail) => (
              <div key={detail.label} className="contents">
                <dt className="text-muted-foreground">{detail.label}</dt>
                <dd>{detail.value}</dd>
              </div>
            ))}
          </dl>
        </TabsContent>
      )}
    </Tabs>
  );
}
