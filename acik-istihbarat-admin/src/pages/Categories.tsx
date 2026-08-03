import React, { useState, useEffect } from 'react';
import api from '../lib/axios';

interface Kategori {
  id: number;
  ad: string;
  sira: number;
  parentId: number | null;
}

const Categories: React.FC = () => {
  const [categories, setCategories] = useState<Kategori[]>([]);
  const [loading, setLoading] = useState(true);
  const [formData, setFormData] = useState({ id: 0, ad: '', sira: 0, parentId: '' });
  const [isEditing, setIsEditing] = useState(false);

  useEffect(() => {
    fetchCategories();
  }, []);

  const fetchCategories = async () => {
    try {
      const response = await api.get('/kategoriler');
      setCategories(response.data);
    } catch (error) {
      console.error('Failed to fetch categories', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      ad: formData.ad,
      sira: formData.sira,
      parentId: formData.parentId ? parseInt(formData.parentId) : null
    };

    try {
      if (isEditing) {
        await api.put(`/kategoriler/${formData.id}`, payload);
      } else {
        await api.post('/kategoriler', payload);
      }
      resetForm();
      fetchCategories();
    } catch (error) {
      console.error('Failed to save category', error);
      alert('Kategori kaydedilemedi.');
    }
  };

  const handleDelete = async (id: number) => {
    if (window.confirm('Bu kategoriyi silmek istediğinize emin misiniz?')) {
      try {
        await api.delete(`/kategoriler/${id}`);
        fetchCategories();
      } catch (error: any) {
        alert(error.response?.data?.message || 'Silinemedi');
      }
    }
  };

  const handleEdit = (c: Kategori) => {
    setFormData({ id: c.id, ad: c.ad, sira: c.sira, parentId: c.parentId ? c.parentId.toString() : '' });
    setIsEditing(true);
  };

  const resetForm = () => {
    setFormData({ id: 0, ad: '', sira: 0, parentId: '' });
    setIsEditing(false);
  };

  return (
    <div>
      <h2 className="text-2xl font-semibold text-gray-800 mb-6">Kategoriler</h2>
      
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1 bg-white p-6 rounded-xl shadow-sm border border-gray-200">
          <h3 className="text-lg font-medium mb-4">{isEditing ? 'Kategori Düzenle' : 'Yeni Kategori Ekle'}</h3>
          <form onSubmit={handleSubmit}>
            <div className="mb-4">
              <label className="block text-sm font-medium mb-2">Kategori Adı</label>
              <input type="text" className="py-2 px-3 block w-full border-gray-200 rounded-lg text-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 border outline-none" value={formData.ad} onChange={e => setFormData({...formData, ad: e.target.value})} required />
            </div>
            <div className="mb-4">
              <label className="block text-sm font-medium mb-2">Üst Kategori</label>
              <select className="py-2 px-3 block w-full border-gray-200 rounded-lg text-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 border outline-none" value={formData.parentId} onChange={e => setFormData({...formData, parentId: e.target.value})}>
                <option value="">-- Yok --</option>
                {categories.filter(c => c.id !== formData.id).map(c => (
                  <option key={c.id} value={c.id}>{c.ad}</option>
                ))}
              </select>
            </div>
            <div className="mb-6">
              <label className="block text-sm font-medium mb-2">Sıra</label>
              <input type="number" className="py-2 px-3 block w-full border-gray-200 rounded-lg text-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 border outline-none" value={formData.sira} onChange={e => setFormData({...formData, sira: parseInt(e.target.value) || 0})} required />
            </div>
            <div className="flex gap-2">
              <button type="submit" className="py-2 px-3 inline-flex justify-center items-center gap-x-2 text-sm font-medium rounded-lg border border-transparent bg-blue-600 text-white hover:bg-blue-700 w-full">
                {isEditing ? 'Güncelle' : 'Ekle'}
              </button>
              {isEditing && (
                <button type="button" onClick={resetForm} className="py-2 px-3 inline-flex justify-center items-center gap-x-2 text-sm font-medium rounded-lg border border-gray-200 bg-white text-gray-800 shadow-sm hover:bg-gray-50 w-full">
                  İptal
                </button>
              )}
            </div>
          </form>
        </div>

        <div className="lg:col-span-2 bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Id</th>
                <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Ad</th>
                <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Sıra</th>
                <th className="px-6 py-3 text-end text-xs font-medium text-gray-500 uppercase">İşlem</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {loading ? (
                <tr><td colSpan={4} className="px-6 py-4 text-center">Yükleniyor...</td></tr>
              ) : categories.map((c) => (
                <tr key={c.id}>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{c.id}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-800">
                    {c.parentId ? <span className="text-gray-400 mr-2">↳</span> : ''}
                    {c.ad}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{c.sira}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-end text-sm font-medium">
                    <button onClick={() => handleEdit(c)} className="text-blue-600 hover:text-blue-800 mr-3">Düzenle</button>
                    <button onClick={() => handleDelete(c.id)} className="text-red-600 hover:text-red-800">Sil</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default Categories;
