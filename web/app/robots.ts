import type { MetadataRoute } from "next";
import { siteUrl } from "@/lib/site-url";

// Pages behind a session or a payment have nothing to index.
export default function robots(): MetadataRoute.Robots {
  return {
    rules: { userAgent: "*", allow: "/", disallow: ["/admin", "/api", "/basket", "/checkout", "/orders", "/login", "/register"] },
    sitemap: `${siteUrl}/sitemap.xml`,
  };
}
