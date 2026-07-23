import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { fetchBeer, getRolesFromToken, saveBeer, searchBreweries, searchCatalogBeer } from '../lib/api';

// Beer CRUD is admin-only (#32) — the API already enforced this server-side, but the
// customer-facing surface shouldn't render a form that can only ever fail to save.
function BeerForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEdit = Boolean(id);
  const isAdmin = getRolesFromToken().includes('Admin');

  const [form, setForm] = useState({
    name: '',
    brewery: '',
    style: '',
    description: '',
    abv: '',
    ibu: '',
    styleFamily: '',
    class: '',
    obdbBreweryId: null,
  });
  const [breweryDirty, setBreweryDirty] = useState(false);
  const [brewerySuggestions, setBrewerySuggestions] = useState([]);
  const [nameDirty, setNameDirty] = useState(false);
  const [catalogSuggestions, setCatalogSuggestions] = useState([]);
  const [catalogBeerApplied, setCatalogBeerApplied] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!isEdit || !isAdmin) return;

    fetchBeer(id)
      .then((data) => setForm(data))
      .catch(() => setError('Could not load this beer. Try again.'));
  }, [id, isEdit, isAdmin]);

  // Autocomplete against Open Brewery DB (#30) — a quick-fill, not a requirement; the
  // brewery field stays a plain text input the admin can always type into by hand.
  useEffect(() => {
    if (!breweryDirty) {
      setBrewerySuggestions([]);
      return;
    }
    const query = form.brewery.trim();
    if (query.length < 2) {
      setBrewerySuggestions([]);
      return;
    }

    const timer = setTimeout(async () => {
      try {
        const results = await searchBreweries(query);
        setBrewerySuggestions(results || []);
      } catch {
        setBrewerySuggestions([]);
      }
    }, 300);

    return () => clearTimeout(timer);
  }, [form.brewery, breweryDirty]);

  // Catalog.beer beer-level pre-fill (#31, go decision — TECHNICAL_ARCHITECTURE_PLAN.md
  // §6): pre-fills style/ABV/IBU/style-family/class/description for the admin to verify,
  // never a source of truth. CC BY 4.0 requires attribution wherever its data appears.
  useEffect(() => {
    if (!nameDirty) {
      setCatalogSuggestions([]);
      return;
    }
    const query = form.name.trim();
    if (query.length < 2) {
      setCatalogSuggestions([]);
      return;
    }

    const timer = setTimeout(async () => {
      try {
        const results = await searchCatalogBeer(query);
        setCatalogSuggestions(results || []);
      } catch {
        setCatalogSuggestions([]);
      }
    }, 300);

    return () => clearTimeout(timer);
  }, [form.name, nameDirty]);

  const handleBreweryChange = (value) => {
    setBreweryDirty(true);
    setForm((f) => ({ ...f, brewery: value, obdbBreweryId: null }));
  };

  const selectBrewerySuggestion = (suggestion) => {
    setForm((f) => ({ ...f, brewery: suggestion.name, obdbBreweryId: suggestion.id }));
    setBrewerySuggestions([]);
    setBreweryDirty(false);
  };

  const handleNameChange = (value) => {
    setNameDirty(true);
    setForm((f) => ({ ...f, name: value }));
  };

  const selectCatalogSuggestion = (suggestion) => {
    setForm((f) => ({
      ...f,
      name: suggestion.name,
      style: suggestion.style || f.style,
      abv: suggestion.abv ?? f.abv,
      ibu: suggestion.ibu ?? f.ibu,
      styleFamily: suggestion.styleFamily || f.styleFamily,
      class: suggestion.class || f.class,
      description: suggestion.description || f.description,
    }));
    setCatalogSuggestions([]);
    setNameDirty(false);
    setCatalogBeerApplied(true);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError('');

    const payload = {
      ...form,
      abv: form.abv === '' || form.abv == null ? null : Number(form.abv),
      ibu: form.ibu === '' || form.ibu == null ? null : Number(form.ibu),
      styleFamily: form.styleFamily || null,
      class: form.class || null,
      obdbBreweryId: form.obdbBreweryId || null,
    };

    try {
      await saveBeer(payload, id);
      navigate('/admin/beers');
    } catch (err) {
      setError(err.message);
    }
  };

  if (!isAdmin) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">{isEdit ? 'Edit Beer' : 'Add Beer'}</h2>
        <p className="mt-2 text-gray-600">Sign in with an admin account to add or edit beers.</p>
      </section>
    );
  }

  return (
    <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 12, background: '#fff', borderRadius: 16, padding: 20, boxShadow: '0 10px 30px rgba(0,0,0,0.06)' }}>
      <h2>{isEdit ? 'Edit Beer' : 'Add Beer'}</h2>

      <div style={{ position: 'relative' }}>
        <input
          placeholder="Name"
          value={form.name}
          autoComplete="off"
          required
          onChange={(e) => handleNameChange(e.target.value)}
        />
        {catalogSuggestions.length > 0 && (
          <ul style={{ position: 'absolute', zIndex: 1, listStyle: 'none', margin: 0, padding: 4, background: '#fff', border: '1px solid #e5e7eb', borderRadius: 8, width: '100%' }}>
            {catalogSuggestions.map((suggestion) => (
              <li key={suggestion.id}>
                <button
                  type="button"
                  onClick={() => selectCatalogSuggestion(suggestion)}
                  style={{ display: 'block', width: '100%', textAlign: 'left', border: 'none', background: 'none', padding: '6px 8px' }}
                >
                  {suggestion.name}
                  {suggestion.brewerName ? ` — ${suggestion.brewerName}` : ''}
                  {suggestion.style ? ` (${suggestion.style})` : ''}
                  {suggestion.cbVerified ? ' ✓ Verified' : ''}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div style={{ position: 'relative' }}>
        <input
          placeholder="Brewery"
          value={form.brewery}
          autoComplete="off"
          required
          onChange={(e) => handleBreweryChange(e.target.value)}
        />
        {brewerySuggestions.length > 0 && (
          <ul style={{ position: 'absolute', zIndex: 1, listStyle: 'none', margin: 0, padding: 4, background: '#fff', border: '1px solid #e5e7eb', borderRadius: 8, width: '100%' }}>
            {brewerySuggestions.map((suggestion) => (
              <li key={suggestion.id}>
                <button
                  type="button"
                  onClick={() => selectBrewerySuggestion(suggestion)}
                  style={{ display: 'block', width: '100%', textAlign: 'left', border: 'none', background: 'none', padding: '6px 8px' }}
                >
                  {suggestion.name}
                  {suggestion.city ? ` — ${[suggestion.city, suggestion.state].filter(Boolean).join(', ')}` : ''}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      <input placeholder="Style" required value={form.style} onChange={(e) => setForm({ ...form, style: e.target.value })} />
      <textarea placeholder="Description" value={form.description || ''} onChange={(e) => setForm({ ...form, description: e.target.value })} />

      <fieldset style={{ display: 'grid', gap: 12, gridTemplateColumns: 'repeat(2, 1fr)', border: 0, padding: 0, margin: 0 }}>
        <input
          type="number"
          step="0.1"
          placeholder="ABV %"
          value={form.abv ?? ''}
          onChange={(e) => setForm({ ...form, abv: e.target.value })}
        />
        <input
          type="number"
          step="1"
          placeholder="IBU"
          value={form.ibu ?? ''}
          onChange={(e) => setForm({ ...form, ibu: e.target.value })}
        />
        <input
          placeholder="Style family"
          value={form.styleFamily || ''}
          onChange={(e) => setForm({ ...form, styleFamily: e.target.value })}
        />
        <label style={{ display: 'grid', gap: 4 }}>
          Class
          <select value={form.class || ''} onChange={(e) => setForm({ ...form, class: e.target.value })}>
            <option value="">Unspecified</option>
            <option value="Ale">Ale</option>
            <option value="Lager">Lager</option>
          </select>
        </label>
      </fieldset>

      {catalogBeerApplied && (
        <p style={{ margin: 0, fontSize: 12, color: '#6b7280' }}>
          Style, ABV, IBU, style family, class, and description pre-filled from Catalog.beer
          (CC BY 4.0) — verify before saving.
        </p>
      )}

      {error && <p style={{ margin: 0, color: '#b91c1c' }}>{error}</p>}

      <button type="submit" style={{ padding: '10px 16px', border: 'none', borderRadius: 999, background: '#111827', color: '#fff' }}>Save</button>
    </form>
  );
}

export default BeerForm;
