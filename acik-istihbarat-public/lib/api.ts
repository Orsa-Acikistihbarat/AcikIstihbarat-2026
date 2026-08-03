// Use internal Docker network URL for server-side fetches, and external URL for client-side fetches
const API_BASE_URL = typeof window === 'undefined' 
  ? (process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5128/api/public')
  : (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5128/api/public');
export async function fetchApi<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;
  
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  const response = await fetch(url, {
    ...options,
    headers,
    next: { revalidate: 60 } // Default revalidate every 60 seconds
  });

  if (!response.ok) {
    console.error(`API Error: ${response.status} ${response.statusText}`);
    throw new Error(`Failed to fetch API data: ${response.statusText}`);
  }

  // Handle empty responses
  if (response.status === 204) {
    return {} as T;
  }

  return response.json() as Promise<T>;
}
