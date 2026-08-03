import Link from 'next/link';
import { Search, Menu, X, ChevronDown } from 'lucide-react';
import { fetchApi } from '@/lib/api';
import { Kategori, Yazar } from '@/types';

export default async function Header() {
  let kategoriler: Kategori[] = [];
  let yazarlar: Yazar[] = [];
  try {
    kategoriler = await fetchApi<Kategori[]>('/Kategoriler');
    yazarlar = await fetchApi<Yazar[]>('/Yazarlar');
  } catch (e) {
    console.error("Kategoriler/Yazarlar fetch error in Header", e);
  }

  return (
    <header className="fixed top-0 inset-x-0 z-50 flex flex-wrap sm:justify-start sm:flex-nowrap w-full text-sm py-3 sm:py-0 glass-header transition-colors duration-300">
      <nav className="relative max-w-[85rem] w-full mx-auto px-4 sm:flex sm:items-center sm:justify-between" aria-label="Global">
        <div className="flex items-center justify-between h-[60px]">
          <Link className="flex-none text-2xl font-heading font-black tracking-tighter text-slate-900 dark:text-white group" href="/">
            AÇIK<span className="text-turquoise-600 dark:text-turquoise-400 group-hover:text-bordeaux-500 transition-colors duration-300">İSTİHBARAT</span>
          </Link>
          <div className="sm:hidden">
            <button type="button" className="hs-collapse-toggle p-2 inline-flex justify-center items-center gap-2 rounded-lg border border-slate-200 font-medium bg-white/50 text-slate-700 shadow-sm align-middle hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-white focus:ring-turquoise-500 transition-all dark:bg-slate-900/50 dark:border-slate-700 dark:text-slate-400 dark:hover:bg-slate-800" data-hs-collapse="#navbar-with-collapse" aria-controls="navbar-with-collapse" aria-label="Toggle navigation">
              <Menu className="hs-collapse-open:hidden size-5" />
              <X className="hs-collapse-open:block hidden size-5" />
            </button>
          </div>
        </div>
        <div id="navbar-with-collapse" className="hidden basis-full grow sm:block transition-all duration-300">
          <div className="flex flex-col gap-5 mt-5 sm:flex-row sm:items-center sm:justify-end sm:mt-0 sm:pl-5">
            <Link className="font-heading font-semibold text-turquoise-600 dark:text-turquoise-400 py-3 relative after:absolute after:bottom-0 after:left-0 after:w-full after:h-0.5 after:bg-turquoise-600 animate-fade-in-up" href="/" aria-current="page">Ana Sayfa</Link>
            
            <div className="relative group py-3 sm:px-3 animate-fade-in-up [animation-delay:100ms] [animation-fill-mode:forwards] opacity-0">
              <button type="button" className="flex items-center w-full font-heading font-medium text-slate-600 group-hover:text-turquoise-600 dark:text-slate-400 dark:group-hover:text-turquoise-400 transition-colors duration-300">
                Haberler
                <ChevronDown className="group-hover:-rotate-180 transition-transform duration-300 ms-1 size-4" />
              </button>
              <div className="absolute left-0 sm:left-auto top-full mt-2 transition-all duration-300 opacity-0 invisible group-hover:opacity-100 group-hover:visible translate-y-2 group-hover:translate-y-0 w-full sm:w-56 z-50 bg-white/90 backdrop-blur-md shadow-xl rounded-xl p-2 dark:bg-slate-900/90 border border-slate-100 dark:border-slate-800 max-h-[70vh] overflow-y-auto">
                {kategoriler.map(kat => (
                  <Link key={kat.id} className="flex items-center gap-x-3.5 py-2.5 px-3 rounded-lg text-sm font-medium text-slate-700 hover:bg-slate-100 hover:text-turquoise-600 focus:ring-2 focus:ring-turquoise-500 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-turquoise-400 transition-colors" href={`/kategori/${kat.id}`}>
                    {kat.ad}
                  </Link>
                ))}
              </div>
            </div>

            <div className="relative group py-3 sm:px-3 animate-fade-in-up [animation-delay:150ms] [animation-fill-mode:forwards] opacity-0">
              <button type="button" className="flex items-center w-full font-heading font-medium text-slate-600 group-hover:text-turquoise-600 dark:text-slate-400 dark:group-hover:text-turquoise-400 transition-colors duration-300">
                Yazarlar
                <ChevronDown className="group-hover:-rotate-180 transition-transform duration-300 ms-1 size-4" />
              </button>
              <div className="absolute left-0 sm:left-auto top-full mt-2 transition-all duration-300 opacity-0 invisible group-hover:opacity-100 group-hover:visible translate-y-2 group-hover:translate-y-0 w-full sm:w-56 z-50 bg-white/90 backdrop-blur-md shadow-xl rounded-xl p-2 dark:bg-slate-900/90 border border-slate-100 dark:border-slate-800 max-h-[70vh] overflow-y-auto">
                {yazarlar.map(yazar => (
                  <Link key={yazar.id} className="flex items-center gap-x-3.5 py-2.5 px-3 rounded-lg text-sm font-medium text-slate-700 hover:bg-slate-100 hover:text-turquoise-600 focus:ring-2 focus:ring-turquoise-500 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-turquoise-400 transition-colors" href={`/yazar/${yazar.id}`}>
                    {yazar.ad}
                  </Link>
                ))}
              </div>
            </div>
            
            <form action="/arama" method="GET" className="relative sm:ml-4 flex items-center group animate-fade-in-up [animation-delay:200ms] [animation-fill-mode:forwards] opacity-0">
              <input 
                type="text" 
                name="q" 
                placeholder="Ara..." 
                className="py-2 px-4 pl-10 block w-full bg-slate-100/70 border-transparent rounded-full text-sm focus:border-turquoise-500 focus:ring-turquoise-500 focus:bg-white dark:bg-slate-800/70 dark:text-slate-200 dark:focus:bg-slate-900 dark:focus:ring-turquoise-800 transition-all shadow-inner placeholder-slate-400 backdrop-blur-sm"
              />
              <div className="absolute inset-y-0 left-0 flex items-center pointer-events-none pl-4">
                <Search className="size-4 text-slate-400 group-focus-within:text-turquoise-500 transition-colors" />
              </div>
            </form>
          </div>
        </div>
      </nav>
    </header>
  );
}
