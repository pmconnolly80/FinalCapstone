// Defaults to whatever host the page itself was loaded from, so a phone on the tavern's
// network that opens the app at the host machine's LAN IP still reaches the API there —
// a literal 'localhost' fallback would instead point the phone at itself.
const API_BASE_URL = import.meta.env.VITE_API_URL || `${window.location.protocol}//${window.location.hostname}:5153`;

// Same-tab auth state changes (login, register, logout) don't fire the browser's
// 'storage' event — that only fires in *other* tabs. Dispatching this ourselves is what
// lets App's nav react immediately instead of staying stale until a manual reload.
export const AUTH_CHANGED_EVENT = 'beer-auth-changed';

export function logout() {
  localStorage.removeItem('beer-token');
  window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
}

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

export async function updateBeerAvailability(id, availability) {
  const response = await fetch(`${API_BASE_URL}/api/beers/${id}/availability`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ availability }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to update availability');
  }
}

export async function deleteBeer(id, reason) {
  const response = await fetch(`${API_BASE_URL}/api/beers/${id}?reason=${encodeURIComponent(reason)}`, {
    method: 'DELETE',
    headers: authHeaders(),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to delete beer');
  }
}

export async function confirmBeer(beerId, pin) {
  const token = localStorage.getItem('beer-token');
  let response;
  try {
    response = await fetch(`${API_BASE_URL}/api/confirmations`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify({ beerId, pin }),
    });
  } catch {
    // fetch() itself throws (rather than resolving with a non-ok response) when there's
    // no network path at all — no signal, airplane mode, dead connection. That's a
    // distinct failure mode from a wrong/locked-out PIN and gets its own UI treatment.
    const networkError = new Error('No network connection');
    networkError.isNetworkError = true;
    throw networkError;
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to confirm beer');
  }
  return response.json();
}

export async function setBeerAvailabilityViaPin(beerId, pin, availability) {
  const token = localStorage.getItem('beer-token');
  let response;
  try {
    response = await fetch(`${API_BASE_URL}/api/confirmations/availability`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify({ beerId, pin, availability }),
    });
  } catch {
    // Same distinction as confirmBeer — a dead connection at the bar gets its own
    // message rather than looking like a wrong PIN.
    const networkError = new Error('No network connection');
    networkError.isNetworkError = true;
    throw networkError;
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to update availability');
  }
}

export async function fetchMyProgress() {
  const token = localStorage.getItem('beer-token');
  const response = await fetch(`${API_BASE_URL}/api/me/progress`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) throw new Error('Failed to load progress');
  return response.json();
}

export async function setMyRating(beerId, rating) {
  const response = await fetch(`${API_BASE_URL}/api/me/ratings/${beerId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ rating }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to save rating');
  }
}

export async function reportBeerUnavailable(beerId) {
  const response = await fetch(`${API_BASE_URL}/api/beers/${beerId}/unavailability-reports`, {
    method: 'POST',
    headers: authHeaders(),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to submit report');
  }
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

export async function searchCatalogBeer(query) {
  const response = await fetch(`${API_BASE_URL}/api/catalog-beer/search?query=${encodeURIComponent(query)}`, {
    headers: authHeaders(),
  });
  if (!response.ok) throw new Error('Failed to search Catalog.beer');
  return response.json();
}

export async function searchBeerLookup(query) {
  const response = await fetch(`${API_BASE_URL}/api/beer-lookup/search?query=${encodeURIComponent(query)}`, {
    headers: authHeaders(),
  });
  if (!response.ok) {
    if (response.status === 429) throw new Error("You're searching a bit fast — try again in a minute.");
    throw new Error('Failed to look up beer');
  }
  return response.json();
}

export async function fetchDashboardSummary() {
  const response = await fetch(`${API_BASE_URL}/api/admin/dashboard/summary`, {
    headers: authHeaders(),
  });
  if (!response.ok) throw new Error('Failed to load dashboard summary');
  return response.json();
}

export async function fetchBeerConfirmationCounts() {
  const response = await fetch(`${API_BASE_URL}/api/admin/dashboard/beer-confirmations`, {
    headers: authHeaders(),
  });
  if (!response.ok) throw new Error('Failed to load most/least-confirmed beers');
  return response.json();
}

export async function fetchAdminAnomalies() {
  const response = await fetch(`${API_BASE_URL}/api/admin/anomalies`, {
    headers: authHeaders(),
  });
  if (!response.ok) throw new Error('Failed to load anomalies');
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

export async function getAdminUsers() {
  const response = await fetch(`${API_BASE_URL}/api/admin/users`, {
    headers: authHeaders(),
  });
  if (!response.ok) throw new Error('Failed to load users');
  return response.json();
}

export async function inviteBartender(email) {
  const response = await fetch(`${API_BASE_URL}/api/admin/users/invite-bartender`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ email }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to invite bartender');
  }
  return response.json();
}

export async function assignRole(id, role, reason) {
  const response = await fetch(`${API_BASE_URL}/api/admin/users/${id}/role`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ role, reason }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to change role');
  }
}

export async function deactivateAccount(id, reason) {
  const response = await fetch(`${API_BASE_URL}/api/admin/users/${id}/deactivate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ reason }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to deactivate account');
  }
}

export async function reactivateAccount(id, reason) {
  const response = await fetch(`${API_BASE_URL}/api/admin/users/${id}/reactivate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ reason }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to reactivate account');
  }
}

export async function issueOrResetStaffPin(userId, pin) {
  const response = await fetch(`${API_BASE_URL}/api/staff-pins/${userId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ pin }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to set PIN');
  }
}

export async function deactivateStaffPin(userId) {
  const response = await fetch(`${API_BASE_URL}/api/staff-pins/${userId}`, {
    method: 'DELETE',
    headers: authHeaders(),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to deactivate PIN');
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

export async function register(email, password, marketingConsent = false) {
  const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, marketingConsent }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Registration failed');
  }
  return response.json();
}

// #46: a fresh sign-in needs no ticket — the challenge endpoint's find-or-create-by-email
// flow runs unconditionally. This is a plain URL (not a fetch) since the whole point is a
// full-page browser redirect through the provider's OAuth screen.
export function externalLoginUrl(provider) {
  return `${API_BASE_URL}/api/auth/external-login/${provider}`;
}

export async function createExternalLoginTicket() {
  const response = await fetch(`${API_BASE_URL}/api/auth/external-login-tickets`, {
    method: 'POST',
    headers: authHeaders(),
  });

  if (!response.ok) throw new Error('Could not start account linking');
  return response.json();
}

export async function getLinkedProviders() {
  const response = await fetch(`${API_BASE_URL}/api/auth/external-logins`, {
    headers: authHeaders(),
  });

  if (!response.ok) throw new Error('Failed to load linked accounts');
  return response.json();
}

// Attaching an additional provider to the signed-in account needs a ticket first (the
// redirect that follows can't carry an Authorization header), so — unlike
// externalLoginUrl — this has to be an async function that fetches, then navigates.
export async function startLinkingProvider(provider) {
  const { ticket } = await createExternalLoginTicket();
  window.location.href = `${externalLoginUrl(provider)}?ticket=${encodeURIComponent(ticket)}`;
}

export async function submitRecommendation({ beerName, breweryName, externalCatalogBeerId, note }) {
  const response = await fetch(`${API_BASE_URL}/api/recommendations`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({
      beerName,
      breweryName: breweryName || null,
      externalCatalogBeerId: externalCatalogBeerId || null,
      note: note || null,
    }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to submit recommendation');
  }
  return response.json();
}

export async function fetchAdminRecommendations(status) {
  const qs = status ? `?status=${encodeURIComponent(status)}` : '';
  const response = await fetch(`${API_BASE_URL}/api/admin/recommendations${qs}`, {
    headers: authHeaders(),
  });
  if (!response.ok) throw new Error('Failed to load recommendations');
  return response.json();
}

export async function updateRecommendationStatus(id, status) {
  const response = await fetch(`${API_BASE_URL}/api/admin/recommendations/${id}/status`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ status }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Failed to update recommendation');
  }
}

export async function fetchExternalSearchDemand() {
  const response = await fetch(`${API_BASE_URL}/api/admin/external-search-demand`, {
    headers: authHeaders(),
  });
  if (!response.ok) throw new Error('Failed to load search demand');
  return response.json();
}

export async function forgotPassword(email) {
  const response = await fetch(`${API_BASE_URL}/api/auth/forgot-password`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Request failed');
  }
  return response.json();
}

export async function resetPassword(email, token, newPassword) {
  const response = await fetch(`${API_BASE_URL}/api/auth/reset-password`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, token, newPassword }),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || 'Password reset failed');
  }
  return response.json();
}
