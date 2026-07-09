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
  });

  useEffect(() => {
    if (!isEdit) return;

    fetchBeer(id)
      .then((data) => setForm(data))
      .catch((err) => console.error(err));
  }, [id, isEdit]);

  const handleSubmit = async (event) => {
    event.preventDefault();

    try {
      await saveBeer(form, id);
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
      <button type="submit" style={{ padding: '10px 16px', border: 'none', borderRadius: 999, background: '#111827', color: '#fff' }}>Save</button>
    </form>
  );
}

export default BeerForm;
