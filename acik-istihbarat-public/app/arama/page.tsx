import { fetchApi } from "@/lib/api";
import { HaberListesiItem, PagedResult, SliderItem } from "@/types";
import Link from "next/link";

export default async function AramaPage({ searchParams }: { searchParams: Promise<{ q?: string, mode?: string, page?: string }> }) {
  const params = await searchParams;
  const q = params.q || "";
  const mode = params.mode || "or";
  const page = parseInt(params.page || "1", 10);
  
  let result: { haberler: PagedResult<HaberListesiItem>, yazilar: PagedResult<SliderItem> } | null = null;
  
  if (q) {
    try {
      result = await fetchApi<{ haberler: PagedResult<HaberListesiItem>, yazilar: PagedResult<SliderItem> }>(`/Arama?q=${encodeURIComponent(q)}&mode=${encodeURIComponent(mode)}&page=${page}&pageSize=12`);
    } catch (e) {
      console.error("Arama fetch error", e);
    }
  }

  const haberler = result?.haberler;
  const yazilar = result?.yazilar;
  const hasResults = (haberler && haberler.items.length > 0) || (yazilar && yazilar.items.length > 0);

  return (
    <div className="w-full">
      {/* Search Header */}
      <div className="bg-slate-900 text-white py-16 md:py-24 relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-r from-blue-900/40 to-slate-900"></div>
        <div className="max-w-[85rem] px-4 sm:px-6 lg:px-8 mx-auto relative z-10">
          <div className="max-w-3xl">
            <h1 className="text-4xl md:text-5xl lg:text-6xl font-black mb-4 tracking-tight">Arama Sonuçları</h1>
            {q ? (
              <p className="text-lg md:text-xl text-slate-300 font-medium">"<span className="text-white font-bold">{q}</span>" için bulunan sonuçlar.</p>
            ) : (
              <p className="text-lg md:text-xl text-slate-300 font-medium">Lütfen aramak istediğiniz kelimeyi girin.</p>
            )}
          </div>
          
          <div className="mt-8 max-w-2xl">
            <form action="/arama" method="GET" className="relative flex flex-col sm:flex-row gap-3">
              <div className="relative flex-grow">
                <input 
                  type="text" 
                  name="q" 
                  defaultValue={q}
                  placeholder="Haber, yazar veya konu ara..." 
                  className="w-full pl-5 pr-12 py-4 bg-white/10 border border-white/20 text-white placeholder-slate-300 rounded-2xl focus:ring-2 focus:ring-blue-500 focus:border-transparent backdrop-blur-md transition-all text-lg"
                />
              </div>
              <div className="flex gap-3">
                <select 
                  name="mode" 
                  defaultValue={mode}
                  className="px-4 py-4 bg-white/10 border border-white/20 text-white rounded-2xl focus:ring-2 focus:ring-blue-500 focus:border-transparent backdrop-blur-md transition-all text-sm appearance-none outline-none cursor-pointer"
                  style={{ backgroundImage: 'url("data:image/svg+xml;charset=US-ASCII,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20width%3D%22292.4%22%20height%3D%22292.4%22%3E%3Cpath%20fill%3D%22%23FFFFFF%22%20d%3D%22M287%2069.4a17.6%2017.6%200%200%200-13-5.4H18.4c-5%200-9.3%201.8-12.9%205.4A17.6%2017.6%200%200%200%200%2082.2c0%205%201.8%209.3%205.4%2012.9l128%20127.9c3.6%203.6%207.8%205.4%2012.8%205.4s9.2-1.8%2012.8-5.4L287%2095c3.5-3.5%205.4-7.8%205.4-12.8%200-5-1.9-9.2-5.5-12.8z%22%2F%3E%3C%2Fsvg%3E")', backgroundRepeat: 'no-repeat', backgroundPosition: 'right 1rem top 50%', backgroundSize: '0.65rem auto' }}
                >
                  <option value="or" className="text-slate-900">Herhangi Bir Kelime</option>
                  <option value="and" className="text-slate-900">Tüm Kelimeler</option>
                  <option value="exact" className="text-slate-900">Tam İfade</option>
                </select>
                <button type="submit" className="px-6 py-4 bg-blue-600 hover:bg-blue-700 text-white rounded-2xl transition-colors font-semibold flex items-center justify-center">
                  <svg className="size-6" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      <div className="max-w-[85rem] px-4 py-10 sm:px-6 lg:px-8 lg:py-14 mx-auto">
        {!q ? (
          null /* Zaten header'da gösteriyoruz */
        ) : !hasResults ? (
          <div className="text-center py-32 bg-slate-50 dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800">
            <h3 className="text-2xl font-bold text-slate-700 dark:text-slate-300 mb-2">Sonuç Bulunamadı</h3>
            <p className="text-slate-500 dark:text-slate-400">"{q}" aramasıyla eşleşen bir haber veya makale bulunamadı. Lütfen farklı anahtar kelimeler deneyin.</p>
          </div>
        ) : (
          <>
            {haberler && haberler.items.length > 0 && (
              <div>
                <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-6 border-b border-slate-200 dark:border-slate-800 pb-2">Haberler</h2>
                <div className="grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 md:gap-8">
              {haberler.items.map((haber) => (
                <Link key={haber.id} className="group flex flex-col h-full bg-white rounded-2xl shadow-sm border border-slate-200/60 hover:shadow-xl hover:-translate-y-1 transition-all duration-300 overflow-hidden dark:bg-slate-900 dark:border-slate-800 dark:hover:border-slate-700" href={`/haber/${haber.id}`}>
                  <div className="h-48 md:h-56 relative overflow-hidden bg-slate-100 dark:bg-slate-800">
                    {haber.thumbnailUrl ? (
                      <img src={haber.thumbnailUrl} alt={haber.baslik} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500 ease-out"/>
                    ) : (
                      <div className="w-full h-full flex items-center justify-center">
                        <svg className="size-12 text-slate-300 dark:text-slate-600" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 16 16">
                          <path d="M4 0h5.293A1 1 0 0 1 10 .293L13.707 4a1 1 0 0 1 .293.707V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2zm5.5 1.5v2a1 1 0 0 0 1 1h2l-3-3zM3 4.5a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5zm0 3a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5zm0 3a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5z"/>
                        </svg>
                      </div>
                    )}
                    <div className="absolute inset-0 bg-gradient-to-t from-slate-900/60 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
                    {haber.kategoriAd && (
                      <span className="absolute top-4 left-4 inline-flex items-center gap-1.5 py-1 px-2.5 rounded-full text-xs font-bold uppercase tracking-wider bg-white/90 text-slate-900 shadow-sm backdrop-blur-sm dark:bg-slate-900/90 dark:text-white">
                        {haber.kategoriAd}
                      </span>
                    )}
                  </div>
                  <div className="p-5 flex flex-col flex-grow">
                    <span className="block mb-2 text-xs font-semibold uppercase tracking-wider text-blue-600 dark:text-blue-400">
                      {new Date(haber.tarih).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })}
                    </span>
                    <h3 className="text-lg font-bold text-slate-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors line-clamp-2 mb-2 leading-snug">
                      {haber.baslik}
                    </h3>
                    <p className="text-sm text-slate-500 dark:text-slate-400 line-clamp-3 leading-relaxed">
                      {haber.spot}
                    </p>
                  </div>
                </Link>
              ))}
                </div>
              </div>
            )}

            {yazilar && yazilar.items.length > 0 && (
              <div className="mt-16">
                <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-6 border-b border-slate-200 dark:border-slate-800 pb-2">Makaleler</h2>
                <div className="grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 md:gap-8">
                  {yazilar.items.map((yazi) => (
                    <Link key={yazi.id} className="group flex flex-col h-full bg-white rounded-2xl shadow-sm border border-slate-200/60 hover:shadow-xl hover:-translate-y-1 transition-all duration-300 overflow-hidden dark:bg-slate-900 dark:border-slate-800 dark:hover:border-slate-700" href={`/yazi/${yazi.id}`}>
                      <div className="h-48 md:h-56 relative overflow-hidden bg-slate-100 dark:bg-slate-800">
                        <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-slate-100 to-slate-200 dark:from-slate-800 dark:to-slate-900">
                          <svg className="size-16 text-slate-300 dark:text-slate-700" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 16 16">
                            <path d="M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h12zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H2z" />
                            <path d="M4.5 3a.5.5 0 0 0-.5.5v9a.5.5 0 0 0 .5.5h7a.5.5 0 0 0 .5-.5v-9a.5.5 0 0 0-.5-.5h-7zM5 4h6v8H5V4z" />
                          </svg>
                        </div>
                        {yazi.badgeLabel && (
                          <span className="absolute top-4 left-4 inline-flex items-center gap-1.5 py-1 px-2.5 rounded-full text-xs font-bold uppercase tracking-wider bg-slate-900/90 text-white shadow-sm backdrop-blur-sm dark:bg-white/90 dark:text-slate-900">
                            {yazi.badgeLabel}
                          </span>
                        )}
                      </div>
                      <div className="p-5 flex flex-col flex-grow">
                        <span className="block mb-2 text-xs font-semibold uppercase tracking-wider text-bordeaux-600 dark:text-bordeaux-400">
                          {new Date(yazi.tarih).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })}
                        </span>
                        <h3 className="text-lg font-bold text-slate-900 dark:text-white group-hover:text-bordeaux-600 dark:group-hover:text-bordeaux-400 transition-colors line-clamp-2 mb-2 leading-snug">
                          {yazi.baslik}
                        </h3>
                        <p className="text-sm text-slate-500 dark:text-slate-400 line-clamp-3 leading-relaxed">
                          {yazi.spot}
                        </p>
                      </div>
                    </Link>
                  ))}
                </div>
              </div>
            )}

            {/* Pagination */}
            {((haberler && haberler.totalPages > 1) || (yazilar && yazilar.totalPages > 1)) && (
              <div className="mt-16 flex justify-center">
                <nav className="flex items-center gap-x-2">
                  {page > 1 && (
                    <Link href={`/arama?q=${encodeURIComponent(q)}&mode=${encodeURIComponent(mode)}&page=${page - 1}`} className="size-10 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full bg-white text-slate-700 shadow-sm border border-slate-200 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all dark:bg-slate-900 dark:border-slate-800 dark:text-white dark:hover:bg-slate-800">
                      <svg className="flex-shrink-0 size-4" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m15 18-6-6 6-6"/></svg>
                      <span aria-hidden="true" className="sr-only">Önceki</span>
                    </Link>
                  )}
                  
                  <div className="flex items-center">
                    <span className="px-4 py-2 text-sm font-medium text-slate-600 bg-slate-100 rounded-full dark:bg-slate-800 dark:text-slate-300">
                      Sayfa {page} / {Math.max(haberler?.totalPages || 0, yazilar?.totalPages || 0)}
                    </span>
                  </div>

                  {page < Math.max(haberler?.totalPages || 0, yazilar?.totalPages || 0) && (
                    <Link href={`/arama?q=${encodeURIComponent(q)}&mode=${encodeURIComponent(mode)}&page=${page + 1}`} className="size-10 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full bg-white text-slate-700 shadow-sm border border-slate-200 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all dark:bg-slate-900 dark:border-slate-800 dark:text-white dark:hover:bg-slate-800">
                      <span aria-hidden="true" className="sr-only">Sonraki</span>
                      <svg className="flex-shrink-0 size-4" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m9 18 6-6-6-6"/></svg>
                    </Link>
                  )}
                </nav>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
