import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { fetchBeer, saveBeer, searchBreweries } from '../lib/api';

function BeerForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEdit = Boolean(id);

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

  useEffect(() => {
    if (!isEdit) return;

    fetchBeer(id)
      .then((data) => setForm(data))
      .catch((err) => console.error(err));
  }, [id, isEdit]);

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

  const handleBreweryChange = (value) => {
    setBreweryDirty(true);
    setForm((f) => ({ ...f, brewery: value, obdbBreweryId: null }));
  };

  const selectBrewerySuggestion = (suggestion) => {
    setForm((f) => ({ ...f, brewery: suggestion.name, obdbBreweryId: suggestion.id }));
    setBrewerySuggestions([]);
    setBreweryDirty(false);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

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
      navigate('/beers');
    } catch (error) {
      console.error(error);
    }
  };

  return (
    <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 12, background: '#fff', borderRadius: 16, padding: 20, boxShadow: '0 10px 30px rgba(0,0,0,0.06)' }}>
      <h2>{isEdit ? 'Edit Beer' : 'Add Beer'}</h2>
      <input placeholder="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />

      <div style={{ position: 'relative' }}>
        <input
          placeholder="Brewery"
          value={form.brewery}
          autoComplete="off"
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

      <input placeholder="Style" value={form.style} onChange={(e) => setForm({ ...form, style: e.target.value })} />
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

      <button type="submit" style={{ padding: '10px 16px', border: 'none', borderRadius: 999, background: '#111827', color: '#fff' }}>Save</button>
    </form>
  );
}

export default BeerForm;
