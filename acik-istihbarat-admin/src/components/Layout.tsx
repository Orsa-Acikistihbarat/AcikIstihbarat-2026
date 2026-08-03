import React from 'react';
import { Outlet, Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const Layout: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const isActive = (path: string) => location.pathname === path || location.pathname.startsWith(`${path}/`);

  return (
    <div className="bg-slate-50 dark:bg-slate-950 min-h-screen flex relative overflow-hidden transition-colors duration-300">
      {/* Background Mesh Gradients */}
      <div className="fixed inset-0 pointer-events-none -z-10">
        <div className="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] rounded-full bg-turquoise-300/10 dark:bg-turquoise-900/10 blur-[100px]" />
        <div className="absolute bottom-[-10%] right-[-5%] w-[30%] h-[50%] rounded-full bg-bordeaux-300/10 dark:bg-bordeaux-900/10 blur-[120px]" />
      </div>

      {/* Sidebar */}
      <div className="w-[260px] glass-panel border-r border-slate-200/50 dark:border-slate-800/50 flex flex-col h-screen fixed z-20">
        <div className="p-6 border-b border-slate-200/50 dark:border-slate-800/50">
          <Link className="flex-none text-xl font-heading font-black tracking-tight text-slate-900 dark:text-white group" to="/" aria-label="Brand">
            AÇIK<span className="text-turquoise-600 dark:text-turquoise-400 group-hover:text-bordeaux-500 transition-colors duration-300">İSTİHBARAT</span>
            <br/><span className="text-xs font-sans font-medium text-slate-500 dark:text-slate-400 uppercase tracking-widest mt-1 block">Yönetim Paneli</span>
          </Link>
        </div>

        <nav className="p-4 flex flex-col flex-wrap w-full flex-grow overflow-y-auto">
          <ul className="space-y-2">
            <li>
              <Link to="/" className={`flex items-center gap-x-3 py-2.5 px-3.5 text-sm font-medium rounded-xl transition-all duration-300 ${isActive('/') && location.pathname === '/' ? 'bg-gradient-to-r from-turquoise-500 to-turquoise-600 text-white shadow-md shadow-turquoise-500/20' : 'text-slate-600 hover:bg-turquoise-50 hover:text-turquoise-700 dark:text-slate-400 dark:hover:bg-turquoise-900/20 dark:hover:text-turquoise-300'}`}>
                <svg className="size-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
                Dashboard
              </Link>
            </li>
            <li>
              <Link to="/haberler" className={`flex items-center gap-x-3 py-2.5 px-3.5 text-sm font-medium rounded-xl transition-all duration-300 ${isActive('/haberler') ? 'bg-gradient-to-r from-turquoise-500 to-turquoise-600 text-white shadow-md shadow-turquoise-500/20' : 'text-slate-600 hover:bg-turquoise-50 hover:text-turquoise-700 dark:text-slate-400 dark:hover:bg-turquoise-900/20 dark:hover:text-turquoise-300'}`}>
                <svg className="size-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" x2="8" y1="13" y2="13"/><line x1="16" x2="8" y1="17" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>
                Haberler
              </Link>
            </li>
            <li>
              <Link to="/kategoriler" className={`flex items-center gap-x-3 py-2.5 px-3.5 text-sm font-medium rounded-xl transition-all duration-300 ${isActive('/kategoriler') ? 'bg-gradient-to-r from-turquoise-500 to-turquoise-600 text-white shadow-md shadow-turquoise-500/20' : 'text-slate-600 hover:bg-turquoise-50 hover:text-turquoise-700 dark:text-slate-400 dark:hover:bg-turquoise-900/20 dark:hover:text-turquoise-300'}`}>
                <svg className="size-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 2H2v10l9.29 9.29c.94.94 2.48.94 3.42 0l6.58-6.58c.94-.94.94-2.48 0-3.42L12 2Z"/><path d="M7 7h.01"/></svg>
                Kategoriler
              </Link>
            </li>
            <li>
              <Link to="/medya" className={`flex items-center gap-x-3 py-2.5 px-3.5 text-sm font-medium rounded-xl transition-all duration-300 ${isActive('/medya') ? 'bg-gradient-to-r from-turquoise-500 to-turquoise-600 text-white shadow-md shadow-turquoise-500/20' : 'text-slate-600 hover:bg-turquoise-50 hover:text-turquoise-700 dark:text-slate-400 dark:hover:bg-turquoise-900/20 dark:hover:text-turquoise-300'}`}>
                <svg className="size-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="18" height="18" x="3" y="3" rx="2" ry="2"/><circle cx="9" cy="9" r="2"/><path d="m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21"/></svg>
                Medya Kütüphanesi
              </Link>
            </li>
          </ul>
        </nav>

        <div className="p-4 border-t border-slate-200/50 dark:border-slate-800/50">
          <div className="flex items-center gap-x-3 py-2 px-3 mb-2">
            <div className="w-8 h-8 rounded-full bg-turquoise-100 text-turquoise-700 dark:bg-turquoise-900/50 dark:text-turquoise-300 flex items-center justify-center font-bold text-sm">
              {user?.username?.charAt(0).toUpperCase() || 'U'}
            </div>
            <span className="text-sm font-medium text-slate-800 dark:text-slate-200">{user?.username}</span>
          </div>
          <button onClick={handleLogout} className="flex items-center gap-x-3 py-2.5 px-3.5 text-sm font-medium rounded-xl text-bordeaux-600 hover:bg-bordeaux-50 hover:text-bordeaux-700 dark:text-bordeaux-400 dark:hover:bg-bordeaux-900/20 dark:hover:text-bordeaux-300 w-full text-left transition-colors duration-300">
            <svg className="size-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" x2="9" y1="12" y2="12"/></svg>
            Çıkış Yap
          </button>
        </div>
      </div>

      {/* Main Content */}
      <div className="w-full pt-10 px-4 sm:px-6 md:px-8 lg:ps-[280px] z-10 relative">
        <Outlet />
      </div>
    </div>
  );
};

export default Layout;
