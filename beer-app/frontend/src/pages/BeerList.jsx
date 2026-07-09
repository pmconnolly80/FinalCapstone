import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { fetchBeers } from '../lib/api';

function BeerList() {
  const [beers, setBeers] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchBeers()
      .then((data) => setBeers(data))
      .catch((err) => console.error(err))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div style={{ display: 'grid', gap: 12 }}>
      <h2>Beer List</h2>
      {loading ? (
        <p>Loading beers…</p>
      ) : beers.length === 0 ? (
        <p>No beers yet.</p>
      ) : (
        beers.map((beer) => (
          <Link key={beer.id} to={`/beers/${beer.id}`} style={{ display: 'block', background: '#fff', borderRadius: 14, padding: 16, boxShadow: '0 8px 24px rgba(0,0,0,0.06)' }}>
            <strong>{beer.name}</strong>
            <div style={{ color: '#4b5563' }}>{beer.brewery} • {beer.style}</div>
          </Link>
        ))
      )}
    </div>
  );
}

export default BeerList;
