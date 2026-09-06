import Link from 'next/link';
import { getNewsletterFolders } from '@/lib/newsletters';

export default async function AcikMedyaLayout({ children }: { children: React.ReactNode }) {
  const folders = getNewsletterFolders();

  return (
    <div className="min-h-screen flex flex-col">
      <header className="pt-8 pb-4 px-4 sm:px-6 lg:px-8 border-b border-slate-200 dark:border-slate-800">
        <div className="max-w-[85rem] w-full mx-auto flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <Link className="flex-none text-2xl font-heading font-black tracking-tighter text-slate-900 dark:text-white group" href="/">
            AÇIK<span className="text-turquoise-600 dark:text-turquoise-400 group-hover:text-bordeaux-500 transition-colors duration-300">İSTİHBARAT</span>
          </Link>

          <nav className="flex flex-wrap items-center gap-4" aria-label="Bültenler">
            {folders.map((folder) => (
              <Link
                key={folder}
                href={`/acikmedya/${folder}`}
                className="font-heading font-medium text-slate-600 hover:text-turquoise-600 dark:text-slate-400 dark:hover:text-turquoise-400 transition-colors duration-300"
              >
                {folder}
              </Link>
            ))}
          </nav>

          <form className="flex items-center gap-2">
            <input
              type="email"
              placeholder="E-posta adresiniz"
              className="py-2 px-3 rounded-lg text-sm border border-slate-200 bg-white/50 text-slate-700 focus:outline-none focus:ring-2 focus:ring-turquoise-500 dark:bg-slate-900/50 dark:border-slate-700 dark:text-slate-300"
            />
            <button
              type="submit"
              className="py-2 px-4 rounded-lg text-sm font-heading font-semibold text-white bg-turquoise-600 hover:bg-turquoise-700 transition-colors dark:bg-turquoise-700 dark:hover:bg-turquoise-600"
            >
              Abone Ol
            </button>
          </form>
        </div>
      </header>

      <main className="flex-grow">{children}</main>
    </div>
  );
}
