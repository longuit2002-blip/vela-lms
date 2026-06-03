import type { NextConfig } from "next";

// Same-origin BFF proxy: the browser only ever talks to the Next.js origin, so the API's refresh
// cookie is first-party (SameSite=Lax). The API base is server-side only (not NEXT_PUBLIC_*).
const apiDestination = process.env.API_PROXY_TARGET ?? "http://localhost:5080";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${apiDestination}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
