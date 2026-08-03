import { fetchApi } from "@/lib/api";
import { HaberListesiItem, PagedResult, Kategori } from "@/types";
import Link from "next/link";
import { notFound } from "next/navigation";

export default async function KategoriPage({ params, searchParams }: { params: Promise<{ id: string }>, searchParams: Promise<{ page?: string }> }) {
  const resolvedParams = await params;
  const resolvedSearchParams = await searchParams;
  const page = resolvedSearchParams.page ? parseInt(resolvedSearchParams.page) : 1;
  let result: PagedResult<HaberListesiItem> | null = null;
  let currentKategoriName = "Kategori";
  
  try {
    result = await fetchApi<PagedResult<HaberListesiItem>>(`/Haberler/kategori/${resolvedParams.id}?page=${page}&pageSize=12`);
    const kategoriler = await fetchApi<Kategori[]>('/Kategoriler');
    const matched = kategoriler.find(k => k.id.toString() === resolvedParams.id);
    if (matched) {
      currentKategoriName = matched.ad;
    }
  } catch (e) {
    console.error("Kategori fetch error", e);
  }

  if (!result || result.items.length === 0) {
    if (page === 1) {
       // Kategori boş olabilir ama şimdilik 404 dönmeyelim.
       // notFound(); 
    }
  }

  return (
    <div className="w-full">
      {/* Category Header */}
      <div className="bg-slate-900 text-white py-16 md:py-24 relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-r from-blue-900/40 to-slate-900"></div>
        <div className="max-w-[85rem] px-4 sm:px-6 lg:px-8 mx-auto relative z-10">
          <div className="max-w-3xl">
            <h1 className="text-4xl md:text-5xl lg:text-6xl font-black mb-4 tracking-tight">{currentKategoriName} Haberleri</h1>
            <p className="text-lg md:text-xl text-slate-300 font-medium">Bu kategorideki en son gelişmeler ve haberler.</p>
          </div>
        </div>
      </div>

      <div className="max-w-[85rem] px-4 py-10 sm:px-6 lg:px-8 lg:py-14 mx-auto">
        {!result || result.items.length === 0 ? (
          <div className="text-center py-32 bg-slate-50 dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800">
            <h3 className="text-2xl font-bold text-slate-700 dark:text-slate-300 mb-2">Haber Bulunamadı</h3>
            <p className="text-slate-500 dark:text-slate-400">Bu kategoride henüz haber bulunmamaktadır.</p>
          </div>
        ) : (
          <>
            <div className="grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 md:gap-8">
              {result.items.map((haber) => (
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
                      <span className="absolute top-2 left-2 inline-flex items-center py-0.5 px-2 rounded-md text-[10px] font-bold uppercase tracking-wider bg-bordeaux-600/90 text-white shadow-sm backdrop-blur-sm dark:bg-bordeaux-600/90">
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

            {/* Pagination */}
            {result.totalPages > 1 && (
              <div className="mt-16 flex justify-center">
                <nav className="flex items-center gap-x-2">
                  {page > 1 && (
                    <Link href={`/kategori/${resolvedParams.id}?page=${page - 1}`} className="size-10 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full bg-white text-slate-700 shadow-sm border border-slate-200 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all dark:bg-slate-900 dark:border-slate-800 dark:text-white dark:hover:bg-slate-800">
                      <svg className="flex-shrink-0 size-4" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m15 18-6-6 6-6"/></svg>
                      <span aria-hidden="true" className="sr-only">Önceki</span>
                    </Link>
                  )}
                  
                  <div className="flex items-center">
                    <span className="px-4 py-2 text-sm font-medium text-slate-600 bg-slate-100 rounded-full dark:bg-slate-800 dark:text-slate-300">
                      Sayfa {page} / {result.totalPages}
                    </span>
                  </div>

                  {page < result.totalPages && (
                    <Link href={`/kategori/${resolvedParams.id}?page=${page + 1}`} className="size-10 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full bg-white text-slate-700 shadow-sm border border-slate-200 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all dark:bg-slate-900 dark:border-slate-800 dark:text-white dark:hover:bg-slate-800">
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
