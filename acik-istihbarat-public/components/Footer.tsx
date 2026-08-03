import Link from 'next/link';


const Twitter = ({ className }: { className?: string }) => (
  <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M22 4s-.7 2.1-2 3.4c1.6 10-9.4 17.3-18 11.6 2.2.1 4.4-.6 6-2C3 15.5.5 9.6 3 5c2.2 2.6 5.6 4.1 9 4-.9-4.2 4-6.6 7-3.8 1.1 0 3-1.2 3-1.2z"/></svg>
);
const Instagram = ({ className }: { className?: string }) => (
  <svg className={className} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="20" height="20" x="2" y="2" rx="5" ry="5"/><path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"/><line x1="17.5" x2="17.51" y1="6.5" y2="6.5"/></svg>
);

export default function Footer() {
  return (
    <footer className="mt-auto w-full bg-slate-50/80 dark:bg-slate-950/80 backdrop-blur-md border-t border-slate-200/50 dark:border-slate-800/50 transition-colors duration-300">
      <div className="max-w-[85rem] py-10 px-4 sm:px-6 lg:px-8 mx-auto">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8 items-center">
          
          <div className="text-center md:text-left">
            <Link className="flex-none text-2xl font-heading font-black tracking-tighter text-slate-900 dark:text-white group" href="/" aria-label="Brand">
              AÇIK<span className="text-turquoise-600 dark:text-turquoise-400 group-hover:text-bordeaux-500 transition-colors duration-300">İSTİHBARAT</span>
            </Link>
            <p className="mt-3 text-sm text-slate-500 dark:text-slate-400 max-w-xs mx-auto md:mx-0 font-medium">
              Yakın tarihin en saklı arşivi
            </p>
          </div>



          <div className="flex justify-center md:justify-end gap-x-4">
            <a target="_blank" rel="noopener noreferrer" className="inline-flex justify-center items-center size-10 text-center text-slate-500 hover:bg-turquoise-100 hover:text-turquoise-600 dark:hover:bg-turquoise-900/30 dark:hover:text-turquoise-400 rounded-full focus:outline-none focus:ring-2 focus:ring-turquoise-500 transition-all" href="https://www.x.com/acikistihbarat">
              <Twitter className="size-4" />
            </a>
            <a target="_blank" rel="noopener noreferrer" className="inline-flex justify-center items-center size-10 text-center text-slate-500 hover:bg-turquoise-100 hover:text-turquoise-600 dark:hover:bg-turquoise-900/30 dark:hover:text-turquoise-400 rounded-full focus:outline-none focus:ring-2 focus:ring-turquoise-500 transition-all" href="https://www.instagram.com/acikistihbarat">
              <Instagram className="size-4" />
            </a>
          </div>
        </div>

        <div className="mt-8 pt-8 border-t border-slate-200/50 dark:border-slate-800/50 flex flex-col md:flex-row justify-between items-center gap-4">
          <p className="text-sm text-slate-500 dark:text-slate-400 font-medium">
            © 2004 Açık İstihbarat. Tüm hakları saklıdır.
          </p>
          <div className="flex gap-4">
            <a href="#" className="text-xs text-slate-500 hover:text-bordeaux-600 dark:text-slate-400 dark:hover:text-bordeaux-400 transition-colors">Gizlilik Politikası</a>
            <a href="#" className="text-xs text-slate-500 hover:text-bordeaux-600 dark:text-slate-400 dark:hover:text-bordeaux-400 transition-colors">Kullanım Şartları</a>
            <a href="#" className="text-xs text-slate-500 hover:text-bordeaux-600 dark:text-slate-400 dark:hover:text-bordeaux-400 transition-colors">Künye</a>
          </div>
        </div>
      </div>
    </footer>
  );
}
