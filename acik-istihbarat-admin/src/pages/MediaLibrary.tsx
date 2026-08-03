import React, { useState, useEffect } from 'react';
import api from '../lib/axios';

export interface Medya {
  id: number;
  dosyaAdi: string;
  dosyaUrl: string;
  dosyaTipi: string;
  dosyaBoyutu: number;
  baslik: string;
  anahtarKelimeler: string;
  yuklenmeTarihi: string;
}

const MediaLibrary: React.FC = () => {
  const [mediaList, setMediaList] = useState<Medya[]>([]);
  const [loading, setLoading] = useState(true);
  const [file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [baslik, setBaslik] = useState('');
  const [anahtarKelimeler, setAnahtarKelimeler] = useState('');

  useEffect(() => {
    fetchMedia();
  }, []);

  const fetchMedia = async () => {
    try {
      const response = await api.get('/medya');
      setMediaList(response.data);
    } catch (error) {
      console.error('Failed to fetch media', error);
    } finally {
      setLoading(false);
    }
  };

  const handleUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) return;

    const formData = new FormData();
    formData.append('File', file);
    if (baslik) formData.append('Baslik', baslik);
    if (anahtarKelimeler) formData.append('AnahtarKelimeler', anahtarKelimeler);

    setUploading(true);
    try {
      await api.post('/medya', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      setFile(null);
      setBaslik('');
      setAnahtarKelimeler('');
      fetchMedia();
    } catch (error) {
      console.error('Upload failed', error);
      alert('Yükleme başarısız.');
    } finally {
      setUploading(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (window.confirm('Bu dosyayı kalıcı olarak silmek istediğinize emin misiniz?')) {
      try {
        await api.delete(`/medya/${id}`);
        fetchMedia();
      } catch (error) {
        alert('Silme başarısız.');
      }
    }
  };

  return (
    <div>
      <h2 className="text-2xl font-semibold text-gray-800 mb-6">Medya Kütüphanesi</h2>
      
      {/* Upload Section */}
      <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 mb-8">
        <h3 className="text-lg font-medium mb-4">Yeni Dosya Yükle</h3>
        <form onSubmit={handleUpload} className="grid grid-cols-1 md:grid-cols-4 gap-4 items-end">
          <div className="md:col-span-1">
            <label className="block text-sm font-medium mb-2">Dosya</label>
            <input type="file" className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100" onChange={e => setFile(e.target.files?.[0] || null)} required />
          </div>
          <div className="md:col-span-1">
            <label className="block text-sm font-medium mb-2">Başlık (Opsiyonel)</label>
            <input type="text" className="py-2 px-3 block w-full border-gray-200 rounded-lg text-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 border outline-none" value={baslik} onChange={e => setBaslik(e.target.value)} />
          </div>
          <div className="md:col-span-1">
            <label className="block text-sm font-medium mb-2">Anahtar Kelimeler</label>
            <input type="text" className="py-2 px-3 block w-full border-gray-200 rounded-lg text-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 border outline-none" value={anahtarKelimeler} onChange={e => setAnahtarKelimeler(e.target.value)} />
          </div>
          <div className="md:col-span-1">
            <button type="submit" disabled={uploading || !file} className="py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-medium rounded-lg border border-transparent bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 w-full">
              {uploading ? 'Yükleniyor...' : 'Yükle'}
            </button>
          </div>
        </form>
      </div>

      {/* Gallery Section */}
      <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-4">
        {loading ? (
          <p>Yükleniyor...</p>
        ) : mediaList.length === 0 ? (
          <p className="col-span-full text-gray-500">Henüz medya bulunmuyor.</p>
        ) : (
          mediaList.map((m) => (
            <div key={m.id} className="group relative bg-white border border-gray-200 rounded-xl overflow-hidden hover:shadow-md transition">
              <div className="aspect-w-1 aspect-h-1 w-full overflow-hidden bg-gray-100 flex justify-center items-center h-32">
                {m.dosyaTipi.startsWith('image/') ? (
                  <img src={`http://localhost:5128${m.dosyaUrl}`} alt={m.baslik} className="object-cover h-full w-full" />
                ) : (
                  <svg className="size-10 text-gray-400" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" x2="8" y1="13" y2="13"/><line x1="16" x2="8" y1="17" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>
                )}
              </div>
              <div className="p-3">
                <p className="text-xs font-medium text-gray-800 truncate" title={m.baslik || m.dosyaAdi}>{m.baslik || m.dosyaAdi}</p>
                <p className="text-xs text-gray-500 mt-1">{(m.dosyaBoyutu / 1024).toFixed(1)} KB</p>
              </div>
              <button onClick={() => handleDelete(m.id)} className="absolute top-2 right-2 bg-red-600 text-white p-1 rounded opacity-0 group-hover:opacity-100 transition-opacity focus:outline-none">
                <svg className="size-4" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/></svg>
              </button>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default MediaLibrary;
