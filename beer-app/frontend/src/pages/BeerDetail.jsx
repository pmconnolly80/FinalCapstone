import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { fetchBeer } from '../lib/api';

function BeerDetail() {
  const { id } = useParams();
  const [beer, setBeer] = useState(null);

  useEffect(() => {
    fetchBeer(id)
      .then((data) => setBeer(data))
      .catch((err) => console.error(err));
  }, [id]);

  if (!beer) return <p>Loading...</p>;

  return (
    <div style={{ background: '#fff', borderRadius: 16, padding: 20, boxShadow: '0 10px 30px rgba(0,0,0,0.06)' }}>
      <h2>{beer.name}</h2>
      <p><strong>Brewery:</strong> {beer.brewery}</p>
      <p><strong>Style:</strong> {beer.style}</p>
      <p>{beer.description}</p>
      <Link to="/beers">Back to list</Link>
    </div>
  );
}

export default BeerDetail;
