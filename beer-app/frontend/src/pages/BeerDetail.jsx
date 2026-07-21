import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { fetchBeer } from '../lib/api';
import ConfirmPinPad from '../components/ConfirmPinPad';

function BeerDetail() {
  const { id } = useParams();
  const [beer, setBeer] = useState(null);
  const [error, setError] = useState('');
  const [confirming, setConfirming] = useState(false);
  const hasToken = Boolean(localStorage.getItem('beer-token'));

  useEffect(() => {
    fetchBeer(id)
      .then((data) => setBeer(data))
      .catch(() => setError('Could not load this beer. Try again.'));
  }, [id]);

  if (error) {
    return (
      <div className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <p className="m-0 text-red-700">{error}</p>
        <Link to="/beers" className="mt-4 inline-block text-gray-600">
          Back to list
        </Link>
      </div>
    );
  }

  if (!beer) return <p>Loading...</p>;

  const hasNerdStats = beer.abv != null || beer.ibu != null || beer.styleFamily || beer.class;
  const brewery = beer.breweryInfo;
  const breweryLocation = brewery && [brewery.city, brewery.state].filter(Boolean).join(', ');
  const breweryLine = brewery && [brewery.breweryType, breweryLocation].filter(Boolean).join(' • ');

  return (
    <div className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
      <h2 className="m-0 text-2xl font-bold">{beer.name}</h2>
      <p className="mt-2">
        <strong>Brewery:</strong> {beer.brewery}
      </p>
      <p>
        <strong>Style:</strong> {beer.style}
      </p>
      <p className="text-gray-600">{beer.description}</p>

      {hasNerdStats && (
        <dl className="mt-4 grid grid-cols-2 gap-x-4 gap-y-2 rounded-xl bg-gray-50 p-4 text-sm sm:grid-cols-4">
          {beer.abv != null && (
            <div>
              <dt className="text-gray-500">ABV</dt>
              <dd className="m-0 font-semibold">{beer.abv}%</dd>
            </div>
          )}
          {beer.ibu != null && (
            <div>
              <dt className="text-gray-500">IBU</dt>
              <dd className="m-0 font-semibold">{beer.ibu}</dd>
            </div>
          )}
          {beer.styleFamily && (
            <div>
              <dt className="text-gray-500">Style family</dt>
              <dd className="m-0 font-semibold">{beer.styleFamily}</dd>
            </div>
          )}
          {beer.class && (
            <div>
              <dt className="text-gray-500">Class</dt>
              <dd className="m-0 font-semibold">{beer.class}</dd>
            </div>
          )}
        </dl>
      )}

      {brewery && (
        <div className="mt-4 rounded-xl border border-gray-200 p-4">
          <p className="m-0 text-sm font-semibold text-gray-500">Brewery</p>
          <p className="m-0 mt-1 font-semibold">{brewery.name}</p>
          {breweryLine && <p className="m-0 text-sm text-gray-600">{breweryLine}</p>}
          {brewery.websiteUrl && (
            <a
              href={brewery.websiteUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-1 inline-block text-amber-700 underline"
            >
              Visit website
            </a>
          )}
        </div>
      )}

      {hasToken && (
        <button
          onClick={() => setConfirming(true)}
          className="mt-4 block w-full rounded-full bg-gray-900 px-5 py-3 text-base text-white"
        >
          Confirm with bartender
        </button>
      )}
      <Link to="/beers" className="mt-4 inline-block text-gray-600">
        Back to list
      </Link>
      {confirming && <ConfirmPinPad beer={beer} onClose={() => setConfirming(false)} />}
    </div>
  );
}

export default BeerDetail;
