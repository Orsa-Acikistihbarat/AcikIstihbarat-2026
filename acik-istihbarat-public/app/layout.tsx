import type { Metadata } from "next";
import { Inter, Outfit } from "next/font/google";
import { headers } from "next/headers";
import "./globals.css";
import PrelineScript from "@/components/PrelineScript";

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin"],
});

const outfit = Outfit({
  variable: "--font-outfit",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Açık İstihbarat - Haber Portalı",
  description: "Açık İstihbarat Haber Portalı",
};

import Header from "@/components/Header";
import Footer from "@/components/Footer";

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const hdrs = await headers();
  const isNewsletterRoute = (hdrs.get("x-pathname") ?? "").startsWith("/acikmedya");

  return (
    <html
      lang="tr"
      className={`${inter.variable} ${outfit.variable} h-full antialiased`}
    >
      <body className={`${inter.variable} ${outfit.variable} font-sans antialiased flex flex-col min-h-screen text-slate-800 dark:text-slate-200 transition-colors duration-300 relative`}>
        {/* Background Mesh Gradients */}
        <div className="fixed inset-0 pointer-events-none -z-10 bg-slate-50 dark:bg-slate-950 overflow-hidden">
          <div className="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] rounded-full bg-turquoise-300/20 dark:bg-turquoise-900/20 blur-[100px]" />
          <div className="absolute top-[20%] right-[-5%] w-[30%] h-[50%] rounded-full bg-bordeaux-300/10 dark:bg-bordeaux-900/10 blur-[120px]" />
          <div className="absolute bottom-[-10%] left-[20%] w-[50%] h-[40%] rounded-full bg-turquoise-400/10 dark:bg-turquoise-800/10 blur-[120px]" />
        </div>

        {isNewsletterRoute ? (
          <main className="flex-grow">{children}</main>
        ) : (
          <>
            <Header />
            <main className="flex-grow pt-[80px]">
              {children}
            </main>
            <Footer />
          </>
        )}
        <PrelineScript />
      </body>
    </html>
  );
}
