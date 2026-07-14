const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5153';

export async function fetchBeers() {
  const response = await fetch(`${API_BASE_URL}/api/beers`);
  if (!response.ok) throw new Error('Failed to load beers');
  return response.json();
}

export async function fetchBeer(id) {
  const response = await fetch(`${API_BASE_URL}/api/beers/${id}`);
  if (!response.ok) throw new Error('Failed to load beer');
  return response.json();
}

export async function saveBeer(beer, id) {
  const token = localStorage.getItem('beer-token');
  const response = await fetch(`${API_BASE_URL}/api/beers${id ? `/${id}` : ''}`, {
    method: id ? 'PUT' : 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(beer),
  });

  if (!response.ok) throw new Error('Failed to save beer');
  return response.json();
}

export async function login(email, password) {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) throw new Error('Login failed');
  return response.json();
}

export async function register(email, password) {
  const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) throw new Error('Registration failed');
  return response.json();
}
