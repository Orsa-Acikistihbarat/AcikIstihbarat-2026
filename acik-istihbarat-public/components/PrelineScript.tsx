"use client";

import { useEffect } from "react";
import { usePathname } from "next/navigation";

declare global {
  interface Window {
    HSStaticMethods: any;
  }
}

export default function PrelineScript() {
  const path = usePathname();

  useEffect(() => {
    const loadPreline = async () => {
      try {
        await import("preline");
        window.HSStaticMethods?.autoInit();
      } catch (e) {
        console.error("Failed to load preline", e);
      }
    };
    loadPreline();
  }, [path]);

  return null;
}
