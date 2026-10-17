import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  experimental: {
    serverActions: {
      // Product images reach the upload action as form data: up to 2 MB plus the multipart envelope.
      bodySizeLimit: "3mb",
    },
  },
};

export default nextConfig;
