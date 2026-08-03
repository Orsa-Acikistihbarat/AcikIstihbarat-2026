import { fetchApi } from "@/lib/api";
import { HaberListesiItem, PagedResult, SliderItem } from "@/types";
import Link from "next/link";
import Image from "next/image";

export default async function Home() {
  // Fetch mansetler (Slider)
  let mansetler: SliderItem[] = [];
  try {
    mansetler = await fetchApi<SliderItem[]>('/Haberler/manset');
  } catch (e) {
    console.error("Mansetler fetch error", e);
  }

  // Fetch son haberler
  let sonHaberler: PagedResult<HaberListesiItem> | null = null;
  try {
    sonHaberler = await fetchApi<PagedResult<HaberListesiItem>>('/Haberler?page=1&pageSize=12');
  } catch (e) {
    console.error("Son haberler fetch error", e);
  }

  return (
    <div className="w-full">
      
      {/* Hero / Mansetler Section */}
      {mansetler.length > 0 && (
        <div className="w-full mb-10 lg:mb-16">
          <div data-hs-carousel='{"loadingClasses": "opacity-0","isAutoPlay": true, "speed": 5000}' className="relative">
            <div className="hs-carousel relative overflow-hidden w-full h-[32rem] md:h-[calc(100vh-80px)] bg-slate-900">
              <div className="hs-carousel-body absolute top-0 bottom-0 start-0 flex flex-nowrap transition-transform duration-700 opacity-0">
                {mansetler.map((manset) => (
                  <div key={`${manset.tip}-${manset.id}`} className="hs-carousel-slide relative overflow-hidden">
                    {manset.thumbnailUrl ? (
                      <img
                        src={manset.thumbnailUrl}
                        alt={manset.baslik}
                        className="w-full h-full object-cover scale-105 transition-transform duration-[10000ms] ease-out hover:scale-110"
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center bg-slate-800">
                        <svg className="size-24 text-slate-600" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 16 16">
                          <path d="M4 0h5.293A1 1 0 0 1 10 .293L13.707 4a1 1 0 0 1 .293.707V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2zm5.5 1.5v2a1 1 0 0 0 1 1h2l-3-3zM3 4.5a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5zm0 3a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5zm0 3a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5z"/>
                        </svg>
                      </div>
                    )}
                    <div className="absolute inset-0 bg-gradient-to-t from-slate-950 via-slate-900/60 to-transparent"></div>
                    <div className="absolute bottom-0 start-0 end-0 p-6 md:p-16 max-w-[85rem] mx-auto z-10">
                      <Link href={manset.tip === 'Haber' ? `/haber/${manset.id}` : `/yazi/${manset.id}`} className="group block max-w-4xl">
                        <span className="inline-flex items-center gap-1.5 py-1 px-3 rounded-full text-xs font-semibold uppercase tracking-wider bg-bordeaux-600 text-white mb-4 shadow-[0_0_15px_rgba(185,28,28,0.5)]">
                          {manset.badgeLabel}
                        </span>
                        <h2 className="text-3xl md:text-5xl lg:text-7xl font-heading font-black text-white leading-tight mb-4 group-hover:text-turquoise-400 transition-colors duration-300 drop-shadow-xl animate-fade-in-up">
                          {manset.baslik}
                        </h2>
                        <p className="text-slate-200 md:text-xl line-clamp-2 md:line-clamp-3 font-medium drop-shadow-md animate-fade-in-up [animation-delay:100ms] opacity-0 [animation-fill-mode:forwards]">
                          {manset.spot}
                        </p>
                      </Link>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <button type="button" className="hs-carousel-prev hs-carousel:disabled:opacity-50 disabled:pointer-events-none absolute inset-y-0 start-0 inline-flex justify-center items-center w-12 md:w-20 h-full text-white hover:bg-white/10 transition-colors focus:outline-none focus:bg-white/10 group">
              <span className="sr-only">Previous</span>
              <span className="size-12 flex justify-center items-center bg-white/10 backdrop-blur-md rounded-full group-hover:bg-turquoise-600 group-hover:scale-110 transition-all duration-300 shadow-lg" aria-hidden="true">
                <svg className="flex-shrink-0 size-6" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m15 18-6-6 6-6"/></svg>
              </span>
            </button>
            <button type="button" className="hs-carousel-next hs-carousel:disabled:opacity-50 disabled:pointer-events-none absolute inset-y-0 end-0 inline-flex justify-center items-center w-12 md:w-20 h-full text-white hover:bg-white/10 transition-colors focus:outline-none focus:bg-white/10 group">
              <span className="sr-only">Next</span>
              <span className="size-12 flex justify-center items-center bg-white/10 backdrop-blur-md rounded-full group-hover:bg-turquoise-600 group-hover:scale-110 transition-all duration-300 shadow-lg" aria-hidden="true">
                <svg className="flex-shrink-0 size-6" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m9 18 6-6-6-6"/></svg>
              </span>
            </button>
          </div>
        </div>
      )}

      {/* Latest News Grid */}
      <div className="max-w-[85rem] px-4 pb-10 sm:px-6 lg:px-8 lg:pb-14 mx-auto">
        <div className="max-w-2xl mb-8 md:mb-12">
          <h2 className="text-3xl font-heading font-black md:text-5xl text-slate-900 dark:text-white tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-turquoise-600 to-bordeaux-500">Son Haberler</h2>
          <p className="mt-3 text-lg md:text-xl text-slate-600 dark:text-slate-400">Gündemden en son ve en önemli gelişmeler.</p>
        </div>

        <div className="grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 md:gap-8">
          {sonHaberler?.items.map((haber) => (
            <Link key={haber.id} className="group flex flex-col h-full bg-white/60 dark:bg-slate-900/60 backdrop-blur-md rounded-2xl shadow-sm border border-slate-200/50 hover:shadow-2xl hover:shadow-turquoise-500/10 hover:-translate-y-2 transition-all duration-300 overflow-hidden dark:border-slate-800/50 dark:hover:border-turquoise-500/30" href={`/haber/${haber.id}`}>
              <div className="h-48 md:h-56 relative overflow-hidden bg-slate-100 dark:bg-slate-800">
                {haber.thumbnailUrl ? (
                  <img src={haber.thumbnailUrl} alt={haber.baslik} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700 ease-out"/>
                ) : (
                  <div className="w-full h-full flex items-center justify-center">
                    <svg className="size-12 text-slate-300 dark:text-slate-600" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 16 16">
                      <path d="M4 0h5.293A1 1 0 0 1 10 .293L13.707 4a1 1 0 0 1 .293.707V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2zm5.5 1.5v2a1 1 0 0 0 1 1h2l-3-3zM3 4.5a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5zm0 3a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5zm0 3a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5z"/>
                    </svg>
                  </div>
                )}
                <div className="absolute inset-0 bg-gradient-to-t from-slate-900/80 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
                {haber.kategoriAd && (
                   <span className="absolute top-2 left-2 inline-flex items-center py-0.5 px-2 rounded-md text-[10px] font-bold uppercase tracking-wider bg-bordeaux-600/90 text-white shadow-sm backdrop-blur-sm dark:bg-bordeaux-600/90">
                    {haber.kategoriAd}
                  </span>
                )}
              </div>
              <div className="p-6 flex flex-col flex-grow relative">
                <span className="block mb-3 text-xs font-semibold uppercase tracking-wider text-turquoise-600 dark:text-turquoise-400">
                  {new Date(haber.tarih).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })}
                </span>
                <h3 className="text-xl font-heading font-bold text-slate-900 dark:text-white group-hover:text-turquoise-600 dark:group-hover:text-turquoise-400 transition-colors line-clamp-2 mb-3 leading-snug">
                  {haber.baslik}
                </h3>
                <p className="text-sm text-slate-500 dark:text-slate-400 line-clamp-3 leading-relaxed">
                  {haber.spot}
                </p>
                <div className="mt-auto pt-4 flex items-center text-sm font-medium text-bordeaux-600 dark:text-bordeaux-400 opacity-0 group-hover:opacity-100 transition-opacity duration-300 translate-y-2 group-hover:translate-y-0">
                  Devamını Oku
                  <svg className="flex-shrink-0 size-4 ml-1" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m9 18 6-6-6-6"/></svg>
                </div>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
}
