const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5153';

export async function searchBeers(params = {}) {
  const token = localStorage.getItem('beer-token');
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      query.set(key, value);
    }
  });
  const qs = query.toString();

  const response = await fetch(`${API_BASE_URL}/api/beers${qs ? `?${qs}` : ''}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
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

export async function confirmBeer(beerId, pin) {
  const token = localStorage.getItem('beer-token');
  const response = await fetch(`${API_BASE_URL}/api/confirmations`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify({ beerId, pin }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to confirm beer');
  }
  return response.json();
}

export async function fetchMyProgress() {
  const token = localStorage.getItem('beer-token');
  const response = await fetch(`${API_BASE_URL}/api/me/progress`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) throw new Error('Failed to load progress');
  return response.json();
}

// Reads the role claim out of the stored JWT (client-side convenience only — every admin
// endpoint is enforced server-side regardless). Returns [] for missing or malformed tokens.
export function getRolesFromToken() {
  const token = localStorage.getItem('beer-token');
  if (!token) return [];
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? [];
    return Array.isArray(roles) ? roles : [roles];
  } catch {
    return [];
  }
}

function authHeaders() {
  const token = localStorage.getItem('beer-token');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function searchBreweries(query) {
  const response = await fetch(`${API_BASE_URL}/api/breweries/search?query=${encodeURIComponent(query)}`, {
    headers: authHeaders(),
  });
  if (!response.ok) throw new Error('Failed to search breweries');
  return response.json();
}

export async function fetchAdminConfirmations() {
  const response = await fetch(`${API_BASE_URL}/api/admin/confirmations`, {
    headers: authHeaders(),
  });
  if (!response.ok) throw new Error('Failed to load confirmations');
  return response.json();
}

export async function fetchConfirmationAudits() {
  const response = await fetch(`${API_BASE_URL}/api/admin/confirmations/audits`, {
    headers: authHeaders(),
  });
  if (!response.ok) throw new Error('Failed to load corrections');
  return response.json();
}

export async function voidConfirmation(id, reason) {
  const response = await fetch(`${API_BASE_URL}/api/admin/confirmations/${id}/void`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ reason }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to void confirmation');
  }
}

export async function setMyPin(pin) {
  const token = localStorage.getItem('beer-token');
  const response = await fetch(`${API_BASE_URL}/api/staff-pins/me`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify({ pin }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to set PIN');
  }
}

export async function login(email, password) {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Login failed');
  }
  return response.json();
}

export async function register(email, password) {
  const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Registration failed');
  }
  return response.json();
}
