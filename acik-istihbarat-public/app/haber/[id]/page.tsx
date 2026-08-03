import { fetchApi } from "@/lib/api";
import { HaberDetay } from "@/types";
import { notFound } from "next/navigation";
import Link from "next/link";
import DOMPurify from "isomorphic-dompurify";
import { Calendar, Tag, FileText, ChevronLeft, ChevronRight, Download } from "lucide-react";

export default async function HaberDetayPage({ params }: { params: Promise<{ id: string }> }) {
  const resolvedParams = await params;
  let haber: HaberDetay | null = null;
  
  try {
    haber = await fetchApi<HaberDetay>(`/Haberler/${resolvedParams.id}`);
  } catch (e) {
    console.error("Haber fetch error", e);
  }

  if (!haber) {
    notFound();
  }

  const cleanHtml = DOMPurify.sanitize(haber.htmlIcerigi);

  return (
    <article className="max-w-[85rem] px-4 py-10 sm:px-6 lg:px-8 mx-auto">
      <div className="max-w-4xl mx-auto">
        <header className="mb-12 text-center relative">
          <div className="flex items-center justify-center gap-4 mb-6 animate-fade-in-up">
            <Link href={`/kategori/${haber.kategoriId}`} className="inline-flex items-center gap-1.5 py-1 px-3 rounded-full text-xs font-bold uppercase tracking-wider bg-bordeaux-600 text-white hover:bg-bordeaux-700 transition-colors shadow-sm dark:bg-bordeaux-700 dark:hover:bg-bordeaux-600">
              <Tag className="size-3.5" />
              {haber.kategori?.ad || 'Kategori'}
            </Link>
            <span className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 dark:text-slate-400">
              <Calendar className="size-4 text-bordeaux-500/70" />
              {new Date(haber.tarih).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })}
            </span>
          </div>

          <h1 className="text-4xl font-heading font-black md:text-5xl lg:text-6xl text-slate-900 dark:text-white leading-tight mb-6 tracking-tight drop-shadow-sm animate-fade-in-up [animation-delay:100ms] opacity-0 [animation-fill-mode:forwards]">
            {haber.baslik}
          </h1>
          <p className="text-xl md:text-2xl text-slate-600 dark:text-slate-300 font-medium leading-relaxed max-w-3xl mx-auto animate-fade-in-up [animation-delay:200ms] opacity-0 [animation-fill-mode:forwards]">
            {haber.spot}
          </p>
          <div className="w-24 h-1 bg-gradient-to-r from-turquoise-500 to-bordeaux-500 mx-auto mt-10 rounded-full opacity-0 animate-fade-in-up [animation-delay:300ms] [animation-fill-mode:forwards]"></div>
        </header>

        {/* Gorsel / Slider */}
        {haber.gorseller && haber.gorseller.length > 0 && (
          <div className="mb-14 relative rounded-3xl overflow-hidden shadow-2xl ring-1 ring-slate-900/10 dark:ring-white/10 animate-fade-in-up [animation-delay:400ms] opacity-0 [animation-fill-mode:forwards]">
            {haber.gorseller.length === 1 ? (
              <img src={haber.gorseller[0].dosyaUrl} alt={haber.gorseller[0].baslik || haber.baslik} className="w-full h-auto max-h-[650px] object-cover transition-transform duration-[10000ms] hover:scale-105" />
            ) : (
              <div data-hs-carousel='{"loadingClasses": "opacity-0"}' className="relative">
                <div className="hs-carousel relative overflow-hidden w-full h-[300px] sm:h-[450px] md:h-[650px] bg-slate-100 dark:bg-slate-800">
                  <div className="hs-carousel-body absolute top-0 bottom-0 start-0 flex flex-nowrap transition-transform duration-700 opacity-0">
                    {haber.gorseller.map(gorsel => (
                      <div key={gorsel.id} className="hs-carousel-slide relative">
                        <img src={gorsel.dosyaUrl} alt={gorsel.baslik} className="w-full h-full object-cover transition-transform duration-[10000ms] hover:scale-105" />
                      </div>
                    ))}
                  </div>
                </div>

                <button type="button" className="hs-carousel-prev hs-carousel:disabled:opacity-50 disabled:pointer-events-none absolute inset-y-0 start-0 inline-flex justify-center items-center w-12 md:w-16 h-full text-white hover:bg-black/10 transition-colors focus:outline-none focus:bg-black/10 group">
                  <span className="size-12 flex justify-center items-center bg-white/10 backdrop-blur-md rounded-full group-hover:bg-turquoise-600 transition-all duration-300 group-hover:scale-110 shadow-lg" aria-hidden="true">
                    <ChevronLeft className="size-6" />
                  </span>
                  <span className="sr-only">Önceki</span>
                </button>
                <button type="button" className="hs-carousel-next hs-carousel:disabled:opacity-50 disabled:pointer-events-none absolute inset-y-0 end-0 inline-flex justify-center items-center w-12 md:w-16 h-full text-white hover:bg-black/10 transition-colors focus:outline-none focus:bg-black/10 group">
                  <span className="sr-only">Sonraki</span>
                  <span className="size-12 flex justify-center items-center bg-white/10 backdrop-blur-md rounded-full group-hover:bg-turquoise-600 transition-all duration-300 group-hover:scale-110 shadow-lg" aria-hidden="true">
                    <ChevronRight className="size-6" />
                  </span>
                </button>
              </div>
            )}
          </div>
        )}

        <div className="prose prose-lg md:prose-xl max-w-none prose-slate prose-a:text-turquoise-600 hover:prose-a:text-turquoise-500 dark:prose-invert dark:prose-a:text-turquoise-400 font-sans leading-relaxed prose-headings:font-heading prose-headings:font-bold prose-p:first-of-type:first-letter:text-7xl prose-p:first-of-type:first-letter:font-heading prose-p:first-of-type:first-letter:font-black prose-p:first-of-type:first-letter:text-turquoise-600 prose-p:first-of-type:first-letter:mr-3 prose-p:first-of-type:first-letter:float-left prose-p:first-of-type:first-letter:leading-none" dangerouslySetInnerHTML={{ __html: cleanHtml }} />

        {/* Belgeler */}
        {haber.belgeler && haber.belgeler.length > 0 && (
          <div className="mt-16 glass-panel rounded-3xl p-6 md:p-8 relative overflow-hidden group">
            <div className="absolute top-0 right-0 w-32 h-32 bg-bordeaux-500/5 rounded-full blur-3xl -z-10 transition-transform group-hover:scale-150 duration-500" />
            
            <div className="flex items-center gap-3 mb-6">
              <div className="p-2.5 bg-turquoise-100 text-turquoise-600 rounded-xl dark:bg-turquoise-900/30 dark:text-turquoise-400 shadow-sm border border-turquoise-200 dark:border-turquoise-800">
                <FileText className="size-6" />
              </div>
              <h3 className="text-2xl font-heading font-bold text-slate-900 dark:text-white tracking-tight">İlgili Belgeler</h3>
            </div>
            <ul className="grid sm:grid-cols-2 gap-5">
              {haber.belgeler.map(belge => (
                <li key={belge.id}>
                  <a href={belge.dosyaUrl} target="_blank" rel="noopener noreferrer" className="group/link flex items-center justify-between p-4 bg-white/80 rounded-2xl border border-slate-200 shadow-sm hover:shadow-lg hover:-translate-y-1 hover:border-turquoise-300 transition-all duration-300 dark:bg-slate-800/80 dark:border-slate-700/50 dark:hover:border-turquoise-500/50 backdrop-blur-sm">
                    <span className="flex items-center gap-3 font-medium text-slate-700 dark:text-slate-300 group-hover/link:text-turquoise-600 dark:group-hover/link:text-turquoise-400 transition-colors line-clamp-1">
                      {belge.baslik || belge.dosyaAdi}
                    </span>
                    <Download className="size-5 text-slate-400 group-hover/link:text-turquoise-600 dark:group-hover/link:text-turquoise-400 transition-colors" />
                  </a>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </article>
  );
}
