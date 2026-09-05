import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      { source: "/acikmedya", destination: "/acikmedya/index.html" },
    ];
  },
  async headers() {
    return [
      {
        source: "/acikmedya",
        headers: [
          { key: "Cache-Control", value: "no-cache, must-revalidate" },
        ],
      },
    ];
  },
};

export default nextConfig;
