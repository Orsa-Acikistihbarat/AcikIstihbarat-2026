import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import api from '../lib/axios';
import MediaSelectorModal from '../components/MediaSelectorModal';
import { useEditor, EditorContent } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import Image from '@tiptap/extension-image';
import LinkExtension from '@tiptap/extension-link';

const HaberEditor: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const isEditing = !!id;
  const navigate = useNavigate();

  const [baslik, setBaslik] = useState('');
  const [spot, setSpot] = useState('');
  const [kategoriId, setKategoriId] = useState('');
  const [mansetmi, setMansetmi] = useState(false);
  
  const [categories, setCategories] = useState<any[]>([]);
  
  const [medyalar, setMedyalar] = useState<any[]>([]);
  const [isMediaModalOpen, setIsMediaModalOpen] = useState(false);
  const [mediaModalType, setMediaModalType] = useState<'featured' | 'gallery' | 'document' | 'editor'>('gallery');

  const editor = useEditor({
    extensions: [StarterKit, Image, LinkExtension],
    content: '',
    editorProps: {
      attributes: {
        class: 'prose prose-sm sm:prose lg:prose-lg xl:prose-2xl mx-auto focus:outline-none min-h-[300px] border border-gray-200 p-4 rounded-lg bg-white',
      },
    },
  });

  useEffect(() => {
    fetchCategories();
    if (isEditing) {
      fetchHaber();
    }
  }, [id]);

  const fetchCategories = async () => {
    try {
      const response = await api.get('/kategoriler');
      setCategories(response.data);
    } catch (error) {
      console.error('Failed to fetch categories', error);
    }
  };

  const fetchHaber = async () => {
    try {
      const response = await api.get(`/haberler/${id}`);
      const data = response.data;
      setBaslik(data.baslik);
      setSpot(data.spot);
      setKategoriId(data.kategoriId.toString());
      setMansetmi(data.mansetmi);
      if (editor) {
        editor.commands.setContent(data.htmlIcerigi);
      }
      
      const mappedMedyalar = [];
      if (data.gorseller) {
        data.gorseller.forEach((g: any, index: number) => {
          mappedMedyalar.push({
            medyaId: g.id,
            oncuResimMi: index === 0, // Simplified: first image is featured if it's the only one
            sira: index,
            medya: g
          });
        });
      }
      if (data.belgeler) {
        data.belgeler.forEach((b: any, index: number) => {
          mappedMedyalar.push({
            medyaId: b.id,
            oncuResimMi: false,
            sira: (data.gorseller?.length || 0) + index,
            medya: b
          });
        });
      }
      // Note: Real application should fetch actual HaberMedya records or API should return them
      // For now, we will construct it from DTO or replace it entirely on save.
      
    } catch (error) {
      console.error('Failed to fetch haber', error);
      alert('Haber bulunamadı');
      navigate('/haberler');
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!kategoriId) {
      alert('Kategori seçiniz'); return;
    }

    const payload = {
      baslik,
      spot,
      htmlIcerigi: editor?.getHTML() || '',
      kategoriId: parseInt(kategoriId),
      mansetmi
    };

    try {
      let savedHaberId = id;
      if (isEditing) {
        await api.put(`/haberler/${id}`, payload);
      } else {
        const response = await api.post('/haberler', payload);
        savedHaberId = response.data.id;
      }

      // Save media relations
      if (savedHaberId) {
        const mediaPayload = medyalar.map((m, index) => ({
          medyaId: m.medyaId || m.id,
          oncuResimMi: m.oncuResimMi || false,
          sira: index
        }));
        await api.post(`/haberler/${savedHaberId}/medyalar`, mediaPayload);
      }

      navigate('/haberler');
    } catch (error) {
      console.error('Save failed', error);
      alert('Kaydetme başarısız');
    }
  };

  const openMediaModal = (type: 'featured' | 'gallery' | 'document' | 'editor') => {
    setMediaModalType(type);
    setIsMediaModalOpen(true);
  };

  const handleMediaSelected = (selectedMedia: any[]) => {
    if (selectedMedia.length === 0) return;

    if (mediaModalType === 'editor') {
      // Insert into tiptap
      selectedMedia.forEach(m => {
        if (m.dosyaTipi.startsWith('image/')) {
          editor?.chain().focus().setImage({ src: `http://localhost:5128${m.dosyaUrl}` }).run();
        } else {
          editor?.chain().focus().setLink({ href: `http://localhost:5128${m.dosyaUrl}` }).insertContent(m.baslik || m.dosyaAdi).run();
        }
      });
    } else if (mediaModalType === 'featured') {
      const featured = { ...selectedMedia[0], oncuResimMi: true, medyaId: selectedMedia[0].id, medya: selectedMedia[0] };
      // Remove existing featured
      const newMedyalar = medyalar.filter(m => !m.oncuResimMi);
      setMedyalar([featured, ...newMedyalar]);
    } else {
      // Gallery or Document
      const mapped = selectedMedia.map(m => ({ ...m, oncuResimMi: false, medyaId: m.id, medya: m }));
      setMedyalar([...medyalar, ...mapped]);
    }
  };

  const removeMedya = (index: number) => {
    const newMedyalar = [...medyalar];
    newMedyalar.splice(index, 1);
    setMedyalar(newMedyalar);
  };

  return (
    <div className="pb-20">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">{isEditing ? 'Haber Düzenle' : 'Yeni Haber Ekle'}</h2>
        <div className="flex gap-2">
          <button type="button" onClick={() => navigate('/haberler')} className="py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-medium rounded-lg border border-gray-200 bg-white text-gray-800 shadow-sm hover:bg-gray-50">
            İptal
          </button>
          <button type="button" onClick={handleSave} className="py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-lg border border-transparent bg-blue-600 text-white hover:bg-blue-700">
            Kaydet
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 flex flex-col gap-6">
          {/* Main Info */}
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200">
            <div className="mb-4">
              <label className="block text-sm font-medium mb-2">Başlık</label>
              <input type="text" value={baslik} onChange={e => setBaslik(e.target.value)} className="py-2 px-3 block w-full border-gray-200 rounded-lg text-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 border outline-none" required />
            </div>
            <div className="mb-4">
              <label className="block text-sm font-medium mb-2">Spot Metin</label>
              <textarea value={spot} onChange={e => setSpot(e.target.value)} rows={3} className="py-2 px-3 block w-full border-gray-200 rounded-lg text-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 border outline-none" required />
            </div>
          </div>

          {/* Editor */}
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200">
            <div className="flex justify-between items-center mb-4">
              <label className="block text-sm font-medium">İçerik</label>
              <button type="button" onClick={() => openMediaModal('editor')} className="text-sm text-blue-600 hover:text-blue-800 flex items-center gap-1">
                <svg className="size-4" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 5v14"/><path d="M5 12h14"/></svg>
                Medyadan Ekle
              </button>
            </div>
            <div className="mb-2 flex gap-1 border-b pb-2">
              <button type="button" onClick={() => editor?.chain().focus().toggleBold().run()} className={`p-1 rounded ${editor?.isActive('bold') ? 'bg-gray-200' : ''}`}><b>B</b></button>
              <button type="button" onClick={() => editor?.chain().focus().toggleItalic().run()} className={`p-1 rounded ${editor?.isActive('italic') ? 'bg-gray-200' : ''}`}><i>I</i></button>
              <button type="button" onClick={() => editor?.chain().focus().toggleHeading({ level: 2 }).run()} className={`p-1 rounded ${editor?.isActive('heading', { level: 2 }) ? 'bg-gray-200' : ''}`}>H2</button>
              <button type="button" onClick={() => editor?.chain().focus().toggleHeading({ level: 3 }).run()} className={`p-1 rounded ${editor?.isActive('heading', { level: 3 }) ? 'bg-gray-200' : ''}`}>H3</button>
            </div>
            <EditorContent editor={editor} />
          </div>
        </div>

        <div className="lg:col-span-1 flex flex-col gap-6">
          {/* Settings */}
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200">
            <h3 className="font-semibold text-gray-800 mb-4">Ayarlar</h3>
            <div className="mb-4">
              <label className="block text-sm font-medium mb-2">Kategori</label>
              <select value={kategoriId} onChange={e => setKategoriId(e.target.value)} className="py-2 px-3 block w-full border-gray-200 rounded-lg text-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 border outline-none" required>
                <option value="">Seçiniz</option>
                {categories.map(c => (
                  <option key={c.id} value={c.id}>{c.parentId ? '↳ ' : ''}{c.ad}</option>
                ))}
              </select>
            </div>
            <div className="flex items-center">
              <input type="checkbox" id="manset" checked={mansetmi} onChange={e => setMansetmi(e.target.checked)} className="shrink-0 mt-0.5 border-gray-200 rounded text-blue-600 focus:ring-blue-500" />
              <label htmlFor="manset" className="text-sm text-gray-700 ms-3">Manşet Haberi</label>
            </div>
          </div>

          {/* Media Attachments */}
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200">
            <div className="flex justify-between items-center mb-4">
              <h3 className="font-semibold text-gray-800">İlişkili Medya</h3>
              <div className="dropdown relative">
                <button type="button" onClick={() => openMediaModal('gallery')} className="text-sm text-blue-600 hover:text-blue-800">+ Ekle</button>
              </div>
            </div>

            <div className="space-y-3">
              {medyalar.length === 0 && <p className="text-sm text-gray-500">Medyadan görsel veya belge seçin.</p>}
              
              {medyalar.map((m, idx) => (
                <div key={idx} className={`flex items-center gap-3 p-2 border rounded-lg ${m.oncuResimMi ? 'border-yellow-400 bg-yellow-50' : 'border-gray-200'}`}>
                  {m.medya?.dosyaTipi?.startsWith('image/') ? (
                    <img src={`http://localhost:5128${m.medya.dosyaUrl}`} alt="" className="w-12 h-12 object-cover rounded" />
                  ) : (
                    <div className="w-12 h-12 bg-gray-100 flex items-center justify-center rounded">📄</div>
                  )}
                  <div className="flex-grow min-w-0">
                    <p className="text-sm truncate font-medium">{m.medya?.baslik || m.medya?.dosyaAdi}</p>
                    {m.oncuResimMi && <span className="text-[10px] bg-yellow-400 text-yellow-900 px-1 rounded">Öncü Resim</span>}
                  </div>
                  <button type="button" onClick={() => {
                    const newM = [...medyalar];
                    newM[idx].oncuResimMi = true;
                    // disable others
                    newM.forEach((item, i) => { if(i !== idx) item.oncuResimMi = false; });
                    setMedyalar(newM);
                  }} className="text-xs text-blue-600" title="Öncü Yap">★</button>
                  <button type="button" onClick={() => removeMedya(idx)} className="text-red-500 hover:text-red-700">
                    <svg className="size-4" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M18 6 6 18"/><path d="m6 6 12 12"/></svg>
                  </button>
                </div>
              ))}
            </div>
          </div>

        </div>
      </div>

      <MediaSelectorModal 
        isOpen={isMediaModalOpen} 
        onClose={() => setIsMediaModalOpen(false)} 
        onSelect={handleMediaSelected} 
        multiple={mediaModalType === 'gallery' || mediaModalType === 'document' || mediaModalType === 'editor'} 
      />
    </div>
  );
};

export default HaberEditor;
