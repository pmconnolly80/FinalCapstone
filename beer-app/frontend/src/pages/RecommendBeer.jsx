import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { submitRecommendation } from '../lib/api';

// #73: standalone "recommend a beer" entry point, also reachable pre-filled from a
// BeerList external-search hit (state passed via navigate()).
function RecommendBeer() {
  const location = useLocation();
  const navigate = useNavigate();
  const prefill = location.state || {};

  const [beerName, setBeerName] = useState(prefill.beerName || '');
  const [breweryName, setBreweryName] = useState(prefill.breweryName || '');
  const [note, setNote] = useState('');
  const [message, setMessage] = useState('');
  const [submitted, setSubmitted] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!beerName.trim()) {
      setMessage('A beer name is required.');
      return;
    }
    try {
      await submitRecommendation({
        beerName: beerName.trim(),
        breweryName: breweryName.trim(),
        externalCatalogBeerId: prefill.externalCatalogBeerId,
        note: note.trim(),
      });
      setSubmitted(true);
      setMessage('');
    } catch (error) {
      setMessage(error.message);
    }
  };

  if (submitted) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">Thanks!</h2>
        <p className="mt-2 text-gray-600">Your recommendation was sent to the tavern.</p>
        <button
          type="button"
          onClick={() => navigate('/beers')}
          className="mt-4 rounded-full border-0 bg-gray-900 px-4 py-2 text-sm font-medium text-white hover:bg-gray-700"
        >
          Back to beers
        </button>
      </section>
    );
  }

  return (
    <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
      <h2 className="m-0 text-xl font-bold">Recommend a beer</h2>
      <p className="mt-2 text-gray-600">Tell the tavern what you&apos;d like to see on the list.</p>

      <form onSubmit={handleSubmit} className="mt-4 grid gap-3">
        <label className="grid gap-1 text-sm font-medium text-gray-700">
          Beer name
          <input value={beerName} onChange={(e) => setBeerName(e.target.value)} />
        </label>
        <label className="grid gap-1 text-sm font-medium text-gray-700">
          Brewery (optional)
          <input value={breweryName} onChange={(e) => setBreweryName(e.target.value)} />
        </label>
        <label className="grid gap-1 text-sm font-medium text-gray-700">
          Note (optional)
          <textarea value={note} onChange={(e) => setNote(e.target.value)} rows={3} />
        </label>

        {message && <p className="text-red-700">{message}</p>}

        <button
          type="submit"
          className="rounded-full border-0 bg-gray-900 px-6 py-3 font-medium text-white hover:bg-gray-700"
        >
          Submit recommendation
        </button>
      </form>
    </section>
  );
}

export default RecommendBeer;
