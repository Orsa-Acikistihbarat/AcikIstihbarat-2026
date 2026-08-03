import React from 'react';
import { Newspaper, FolderTree, Image as ImageIcon, Users } from 'lucide-react';

const Dashboard: React.FC = () => {
  const stats = [
    { title: "Toplam Haber", value: "145", icon: <Newspaper className="size-6 text-turquoise-600 dark:text-turquoise-400" />, trend: "+12%" },
    { title: "Kategoriler", value: "8", icon: <FolderTree className="size-6 text-bordeaux-600 dark:text-bordeaux-400" />, trend: "Sabit" },
    { title: "Medya Dosyaları", value: "320", icon: <ImageIcon className="size-6 text-blue-600 dark:text-blue-400" />, trend: "+5%" },
    { title: "Aktif Kullanıcılar", value: "4", icon: <Users className="size-6 text-purple-600 dark:text-purple-400" />, trend: "+1" },
  ];

  return (
    <div className="pb-10">
      <div className="mb-8 animate-fade-in-up">
        <h2 className="text-3xl font-heading font-black text-slate-900 dark:text-white bg-clip-text text-transparent bg-gradient-to-r from-turquoise-600 to-bordeaux-500">Dashboard</h2>
        <p className="mt-2 text-slate-600 dark:text-slate-400 font-medium">Açık İstihbarat yönetim paneline hoş geldiniz. Sistemin genel durumunu buradan takip edebilirsiniz.</p>
      </div>

      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {stats.map((stat, idx) => (
          <div key={idx} className={`glass-panel p-6 rounded-2xl flex flex-col justify-between hover:-translate-y-1 hover:shadow-xl transition-all duration-300 animate-fade-in-up opacity-0 [animation-fill-mode:forwards]`} style={{ animationDelay: `${(idx + 1) * 100}ms` }}>
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-sm font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider">{stat.title}</h3>
              <div className="p-2.5 bg-white dark:bg-slate-800 rounded-xl shadow-sm border border-slate-100 dark:border-slate-700">
                {stat.icon}
              </div>
            </div>
            <div className="flex items-end justify-between">
              <span className="text-4xl font-heading font-black text-slate-900 dark:text-white">{stat.value}</span>
              <span className={`text-sm font-bold ${stat.trend.startsWith('+') ? 'text-emerald-500' : 'text-slate-400'}`}>{stat.trend}</span>
            </div>
          </div>
        ))}
      </div>

      <div className="mt-10 glass-panel rounded-3xl p-8 animate-fade-in-up [animation-delay:500ms] opacity-0 [animation-fill-mode:forwards]">
        <h3 className="text-xl font-heading font-bold text-slate-900 dark:text-white mb-6">Son Aktiviteler</h3>
        <div className="space-y-4">
          {[1, 2, 3].map((item) => (
            <div key={item} className="flex items-center gap-4 p-4 bg-white/50 dark:bg-slate-800/50 rounded-2xl border border-slate-200/50 dark:border-slate-700/50 hover:bg-white dark:hover:bg-slate-800 transition-colors">
              <div className="w-10 h-10 rounded-full bg-turquoise-100 dark:bg-turquoise-900/30 flex items-center justify-center text-turquoise-600 dark:text-turquoise-400">
                <Newspaper className="size-5" />
              </div>
              <div className="flex-1">
                <p className="text-sm font-medium text-slate-900 dark:text-white">Yeni haber eklendi: <span className="font-bold">Örnek Haber Başlığı {item}</span></p>
                <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">2 saat önce eklendi</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
