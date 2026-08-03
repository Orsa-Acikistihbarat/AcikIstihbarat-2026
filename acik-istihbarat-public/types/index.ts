export interface Kategori {
  id: number;
  ad: string;
  slug: string;
  parentId: number | null;
  sira: number;
  altKategoriler: Kategori[];
}

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

export interface HaberListesiItem {
  id: number;
  baslik: string;
  spot: string;
  tarih: string;
  thumbnailUrl: string | null;
  kategoriId: number;
  kategoriAd: string;
}

export interface HaberDetay {
  id: number;
  baslik: string;
  spot: string;
  htmlIcerigi: string;
  kategoriId: number;
  kategori: Kategori;
  mansetmi: boolean;
  tarih: string;
  gorseller: Medya[];
  belgeler: Medya[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface Yazar {
  id: number;
  ad: string;
}

export interface Yazi {
  id: number;
  yazarId: number;
  yazarAd: string;
  baslik: string;
  onIzlemeMetni: string;
  tamMetin: string;
  tarih: string;
}

export interface SliderItem {
  id: number;
  tip: "Haber" | "Yazi";
  baslik: string;
  spot: string;
  tarih: string;
  thumbnailUrl: string;
  badgeLabel: string;
}
