import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { fetchBeer, saveBeer } from '../lib/api';

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
  });

  useEffect(() => {
    if (!isEdit) return;

    fetchBeer(id)
      .then((data) => setForm(data))
      .catch((err) => console.error(err));
  }, [id, isEdit]);

  const handleSubmit = async (event) => {
    event.preventDefault();

    const payload = {
      ...form,
      abv: form.abv === '' || form.abv == null ? null : Number(form.abv),
      ibu: form.ibu === '' || form.ibu == null ? null : Number(form.ibu),
      styleFamily: form.styleFamily || null,
      class: form.class || null,
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
      <input placeholder="Brewery" value={form.brewery} onChange={(e) => setForm({ ...form, brewery: e.target.value })} />
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
