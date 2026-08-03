import { fetchApi } from "@/lib/api";
import { Yazi } from "@/types";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Calendar, User } from "lucide-react";

export default async function YazarPage({ params }: { params: Promise<{ id: string }> }) {
  const resolvedParams = await params;
  let yazilar: Yazi[] = [];
  
  try {
    yazilar = await fetchApi<Yazi[]>(`/Yazarlar/${resolvedParams.id}/Yazilar`);
  } catch (e) {
    console.error("Yazilar fetch error", e);
  }

  if (!yazilar) {
    notFound();
  }

  const yazarAd = yazilar.length > 0 ? yazilar[0].yazarAd : "Yazar";

  return (
    <div className="max-w-[85rem] px-4 py-10 sm:px-6 lg:px-8 mx-auto">
      <div className="mb-10 text-center animate-fade-in-up">
        <h1 className="text-3xl md:text-5xl font-heading font-black text-slate-900 dark:text-white uppercase tracking-tighter mb-4">
          <span className="text-transparent bg-clip-text bg-gradient-to-r from-turquoise-600 to-bordeaux-500">
            {yazarAd}
          </span> Yazıları
        </h1>
        <div className="w-24 h-1 bg-gradient-to-r from-turquoise-500 to-bordeaux-500 mx-auto rounded-full"></div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {yazilar.length === 0 ? (
          <div className="col-span-full text-center py-12 text-slate-500 dark:text-slate-400">
            Bu yazara ait yazı bulunamadı.
          </div>
        ) : (
          yazilar.map((yazi, index) => (
            <Link key={yazi.id} href={`/yazi/${yazi.id}`} className={`group flex flex-col h-full bg-white dark:bg-slate-900 rounded-2xl shadow-sm hover:shadow-xl border border-slate-200 dark:border-slate-800 transition-all duration-300 hover:-translate-y-1 animate-fade-in-up`} style={{ animationDelay: `${index * 100}ms`, animationFillMode: 'forwards' }}>
              <div className="p-5 flex flex-col flex-grow">
                <h3 className="text-xl font-heading font-bold text-slate-900 dark:text-white group-hover:text-turquoise-600 dark:group-hover:text-turquoise-400 transition-colors mb-3 line-clamp-2">
                  {yazi.baslik}
                </h3>
                <p className="text-slate-600 dark:text-slate-400 text-sm mb-4 line-clamp-3 flex-grow">
                  {yazi.onIzlemeMetni}
                </p>
                <div className="mt-auto pt-4 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between text-xs text-slate-500 dark:text-slate-400 font-medium">
                  <span className="flex items-center gap-1.5">
                    <User className="size-3.5 text-turquoise-500" />
                    {yazi.yazarAd}
                  </span>
                  <span className="flex items-center gap-1.5">
                    <Calendar className="size-3.5 text-bordeaux-400" />
                    {yazi.tarih ? new Date(yazi.tarih).toLocaleString('tr-TR', { dateStyle: 'long', timeStyle: 'short' }) : ''}
                  </span>
                </div>
              </div>
            </Link>
          ))
        )}
      </div>
    </div>
  );
}
