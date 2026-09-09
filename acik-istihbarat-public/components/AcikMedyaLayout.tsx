import Link from 'next/link';
import { getNewsletterFolders } from '@/lib/newsletters';
import { resolveNewsletterLabel } from '@/lib/newsletterLabels';
import NewsletterSubscribeForm from '@/components/NewsletterSubscribeForm';

export default async function AcikMedyaLayout({
  children,
  activeFolder,
}: {
  children: React.ReactNode;
  activeFolder: string;
}) {
  const folders = getNewsletterFolders();

  return (
    <div className="min-h-screen flex flex-col">
      <header className="pt-8 pb-4 px-4 sm:px-6 lg:px-8 border-b border-slate-200 dark:border-slate-800">
        <div className="max-w-[85rem] w-full mx-auto flex flex-col flex-wrap gap-5 sm:flex-row sm:items-center sm:justify-between">
          <Link className="flex-none shrink-0 text-2xl font-heading font-black tracking-tighter text-slate-900 dark:text-white group" href="/">
            AÇIK<span className="text-turquoise-600 dark:text-turquoise-400 group-hover:text-bordeaux-500 transition-colors duration-300">İSTİHBARAT</span>
          </Link>

          <nav className="flex flex-nowrap shrink-0 items-center gap-4" aria-label="Bültenler">
            {folders.map((folder) => {
              const isActive = folder === activeFolder;
              return (
                <Link
                  key={folder}
                  href={`/acikmedya/${folder}`}
                  aria-current={isActive ? 'page' : undefined}
                  className={
                    isActive
                      ? 'font-heading font-semibold text-turquoise-600 dark:text-turquoise-400 py-3 relative after:absolute after:bottom-0 after:left-0 after:w-full after:h-0.5 after:bg-turquoise-600 dark:after:bg-turquoise-400 transition-colors duration-300'
                      : 'font-heading font-medium text-slate-600 hover:text-turquoise-600 dark:text-slate-400 dark:hover:text-turquoise-400 transition-colors duration-300'
                  }
                >
                  {resolveNewsletterLabel(folder)}
                </Link>
              );
            })}
          </nav>

          <NewsletterSubscribeForm folders={folders} />
        </div>
      </header>

      <main className="flex-grow">{children}</main>
    </div>
  );
}
