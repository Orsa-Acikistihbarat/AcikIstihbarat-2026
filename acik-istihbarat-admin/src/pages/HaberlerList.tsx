import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../lib/axios';

const HaberlerList: React.FC = () => {
  const [haberler, setHaberler] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchHaberler();
  }, []);

  const fetchHaberler = async () => {
    try {
      const response = await api.get('/haberler');
      setHaberler(response.data.items || []);
    } catch (error) {
      console.error('Failed to fetch haberler', error);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (window.confirm('Bu haberi silmek istediğinize emin misiniz?')) {
      try {
        await api.delete(`/haberler/${id}`);
        fetchHaberler();
      } catch (error) {
        alert('Silme başarısız.');
      }
    }
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Haberler</h2>
        <Link to="/haberler/yeni" className="py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-lg border border-transparent bg-blue-600 text-white hover:bg-blue-700">
          Yeni Haber Ekle
        </Link>
      </div>
      
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Id</th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Başlık</th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Kategori</th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Tarih</th>
              <th className="px-6 py-3 text-end text-xs font-medium text-gray-500 uppercase">İşlem</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {loading ? (
              <tr><td colSpan={5} className="px-6 py-4 text-center">Yükleniyor...</td></tr>
            ) : haberler.length === 0 ? (
              <tr><td colSpan={5} className="px-6 py-4 text-center text-gray-500">Henüz haber bulunmuyor.</td></tr>
            ) : haberler.map((h) => (
              <tr key={h.id}>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{h.id}</td>
                <td className="px-6 py-4 text-sm font-medium text-gray-800 max-w-md truncate" title={h.baslik}>{h.baslik}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{h.kategoriAd}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{new Date(h.tarih).toLocaleDateString('tr-TR')}</td>
                <td className="px-6 py-4 whitespace-nowrap text-end text-sm font-medium">
                  <Link to={`/haberler/duzenle/${h.id}`} className="text-blue-600 hover:text-blue-800 mr-3">Düzenle</Link>
                  <button onClick={() => handleDelete(h.id)} className="text-red-600 hover:text-red-800">Sil</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default HaberlerList;
