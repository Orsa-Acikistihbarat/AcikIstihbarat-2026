import { fetchApi } from "@/lib/api";
import { Yazi } from "@/types";
import { notFound } from "next/navigation";
import Link from "next/link";
import DOMPurify from "isomorphic-dompurify";
import { Calendar, User } from "lucide-react";

export default async function YaziDetayPage({ params }: { params: Promise<{ id: string }> }) {
  const resolvedParams = await params;
  let yazi: Yazi | null = null;
  
  try {
    yazi = await fetchApi<Yazi>(`/Yazilar/${resolvedParams.id}`);
  } catch (e) {
    console.error("Yazi fetch error", e);
  }

  if (!yazi) {
    notFound();
  }

  const cleanHtml = DOMPurify.sanitize(yazi.tamMetin);

  return (
    <article className="max-w-[85rem] px-4 py-10 sm:px-6 lg:px-8 mx-auto">
      <div className="max-w-4xl mx-auto">
        <header className="mb-12 text-center relative">
          <div className="flex items-center justify-center gap-4 mb-6 animate-fade-in-up">
            <Link href={`/yazar/${yazi.yazarId}`} className="inline-flex items-center gap-1.5 py-1 px-3 rounded-full text-xs font-bold uppercase tracking-wider bg-bordeaux-600 text-white hover:bg-bordeaux-700 transition-colors shadow-sm dark:bg-bordeaux-700 dark:hover:bg-bordeaux-600">
              <User className="size-3.5" />
              {yazi.yazarAd || 'Yazar'}
            </Link>
            <span className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 dark:text-slate-400">
              <Calendar className="size-4 text-turquoise-600/70 dark:text-turquoise-400/70" />
              {yazi.tarih ? new Date(yazi.tarih).toLocaleString('tr-TR', { dateStyle: 'long', timeStyle: 'short' }) : ''}
            </span>
          </div>

          <h1 className="text-4xl font-heading font-black md:text-5xl lg:text-6xl text-slate-900 dark:text-white leading-tight mb-6 tracking-tight drop-shadow-sm animate-fade-in-up [animation-delay:100ms] opacity-0 [animation-fill-mode:forwards]">
            {yazi.baslik}
          </h1>
          {yazi.onIzlemeMetni && (
            <p className="text-xl md:text-2xl text-slate-600 dark:text-slate-300 font-medium leading-relaxed max-w-3xl mx-auto animate-fade-in-up [animation-delay:200ms] opacity-0 [animation-fill-mode:forwards]">
              {yazi.onIzlemeMetni}
            </p>
          )}
          <div className="w-24 h-1 bg-gradient-to-r from-turquoise-500 to-bordeaux-500 mx-auto mt-10 rounded-full opacity-0 animate-fade-in-up [animation-delay:300ms] [animation-fill-mode:forwards]"></div>
        </header>

        <div className="prose prose-lg md:prose-xl max-w-none prose-slate prose-a:text-turquoise-600 hover:prose-a:text-turquoise-500 dark:prose-invert dark:prose-a:text-turquoise-400 font-sans leading-relaxed prose-headings:font-heading prose-headings:font-bold prose-p:first-of-type:first-letter:text-7xl prose-p:first-of-type:first-letter:font-heading prose-p:first-of-type:first-letter:font-black prose-p:first-of-type:first-letter:text-turquoise-600 prose-p:first-of-type:first-letter:mr-3 prose-p:first-of-type:first-letter:float-left prose-p:first-of-type:first-letter:leading-none" dangerouslySetInnerHTML={{ __html: cleanHtml }} />
      </div>
    </article>
  );
}
