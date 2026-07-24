import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { fetchMyProgress, searchBeerLookup, searchBeers } from '../lib/api';

const AVAILABILITY_FILTERS = [
  { value: '', label: 'In Stock' },
  { value: 'OnTap', label: 'On Tap' },
  { value: 'Available', label: 'Available' },
  { value: 'OutOfStock', label: 'Out of Stock' },
  { value: 'Retired', label: 'Retired' },
  { value: 'all', label: 'All' },
];

const HAD_STATUS_FILTERS = [
  { value: '', label: 'All' },
  { value: 'had', label: 'Had it' },
  { value: 'nothad', label: "Haven't had" },
];

const AVAILABILITY_BADGES = {
  OnTap: { label: 'On Tap', className: 'bg-green-100 text-green-800' },
  Available: { label: 'Available', className: 'bg-blue-100 text-blue-800' },
  OutOfStock: { label: 'Out of Stock', className: 'bg-gray-100 text-gray-600' },
  Retired: { label: 'Retired', className: 'bg-gray-100 text-gray-500' },
};

const CHIP_BASE = 'rounded-full border px-3 py-1 text-sm';
const CHIP_ACTIVE = 'border-gray-900 bg-gray-900 text-white';
const CHIP_INACTIVE = 'border-gray-300 bg-white text-gray-700';

// Style/brewery "filter chips" are quick-search shortcuts, not a separate structured
// filter — the search API has one free-text field spanning name/brewery/style, so a
// chip just fills the search box with that beer's style or brewery. Computed from the
// current result page so they stay relevant to what's actually visible.
function quickFilterChips(beers, field) {
  return [...new Set(beers.map((beer) => beer[field]))].sort().slice(0, 6);
}

function BeerList() {
  const navigate = useNavigate();
  const isSignedIn = Boolean(localStorage.getItem('beer-token'));

  // #72: "look up any beer" is a deliberately separate mode from the tavern's own list,
  // so customers aren't confused about what's actually served — reuses the same search
  // input/debounce pattern rather than a second page.
  const [mode, setMode] = useState('catalog');

  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [availability, setAvailability] = useState('');
  const [hadStatus, setHadStatus] = useState('');
  const [beers, setBeers] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [confirmedCount, setConfirmedCount] = useState(null);

  const [lookupInput, setLookupInput] = useState('');
  const [debouncedLookup, setDebouncedLookup] = useState('');
  const [lookupResults, setLookupResults] = useState(null);
  const [lookupLoading, setLookupLoading] = useState(false);
  const [lookupError, setLookupError] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchInput.trim()), 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedLookup(lookupInput.trim()), 300);
    return () => clearTimeout(timer);
  }, [lookupInput]);

  useEffect(() => {
    if (mode !== 'lookup' || !isSignedIn) return;
    if (!debouncedLookup) {
      setLookupResults(null);
      return;
    }
    setLookupLoading(true);
    setLookupError('');
    searchBeerLookup(debouncedLookup)
      .then(setLookupResults)
      .catch((err) => setLookupError(err.message))
      .finally(() => setLookupLoading(false));
  }, [mode, isSignedIn, debouncedLookup]);

  // Drives the #70 first-visit hint: a brand-new customer's had/not-had filters have
  // nothing to differentiate yet, so point them at style/bartender instead.
  useEffect(() => {
    if (!isSignedIn) return;
    fetchMyProgress()
      .then((data) => setConfirmedCount(data.confirmedCount))
      .catch(() => {});
  }, [isSignedIn]);

  useEffect(() => {
    setLoading(true);
    setError('');
    searchBeers({ search: debouncedSearch, availability, hadStatus })
      .then((data) => {
        setBeers(data.items);
        setTotalCount(data.totalCount);
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [debouncedSearch, availability, hadStatus]);

  const toggleQuickSearch = (value) => {
    setSearchInput((current) => (current === value ? '' : value));
  };

  const styleChips = quickFilterChips(beers, 'style');
  const breweryChips = quickFilterChips(beers, 'brewery');

  return (
    <div className="grid gap-4">
      <h2 className="m-0 text-xl font-bold">Beer List</h2>

      {isSignedIn && (
        <div className="flex flex-wrap gap-2">
          {[
            { value: 'catalog', label: "What's on our list" },
            { value: 'lookup', label: 'Look up any beer' },
          ].map((tab) => (
            <button
              key={tab.value}
              type="button"
              onClick={() => setMode(tab.value)}
              className={`${CHIP_BASE} ${mode === tab.value ? CHIP_ACTIVE : CHIP_INACTIVE}`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      )}

      {mode === 'lookup' && isSignedIn ? (
        <div className="grid gap-4">
          <p className="m-0 text-sm text-gray-500">
            Searches outside beer databases — not what the tavern actually carries.
          </p>
          <input
            value={lookupInput}
            onChange={(e) => setLookupInput(e.target.value)}
            placeholder="Search any beer or brewery"
            className="w-full"
          />

          {lookupError && <p className="text-red-700">{lookupError}</p>}

          {lookupLoading && <p>Searching…</p>}

          {!lookupLoading && lookupResults && lookupResults.beers.length === 0 && lookupResults.breweries.length === 0 && (
            <p>No results.</p>
          )}

          {lookupResults && lookupResults.beers.length > 0 && (
            <div className="grid gap-3">
              {lookupResults.beers.map((beer) => (
                <div
                  key={beer.id}
                  className="rounded-2xl border border-amber-200 bg-amber-50 p-4 shadow-[0_8px_24px_rgba(0,0,0,0.06)]"
                >
                  <strong>{beer.name}</strong>
                  <div className="text-gray-600">
                    {beer.brewerName} • {beer.style}
                  </div>
                  <button
                    type="button"
                    onClick={() =>
                      navigate('/recommend', {
                        state: { beerName: beer.name, breweryName: beer.brewerName, externalCatalogBeerId: beer.id },
                      })
                    }
                    className="mt-2 rounded-full border-0 bg-gray-900 px-4 py-2 text-sm font-medium text-white hover:bg-gray-700"
                  >
                    Recommend this beer
                  </button>
                </div>
              ))}
            </div>
          )}

          {lookupResults && lookupResults.breweries.length > 0 && (
            <div className="grid gap-3">
              {lookupResults.breweries.map((brewery) => (
                <div key={brewery.id} className="rounded-2xl border border-amber-200 bg-amber-50 p-4">
                  <strong>{brewery.name}</strong>
                  <div className="text-gray-600">
                    {brewery.city}
                    {brewery.city && brewery.state ? ', ' : ''}
                    {brewery.state}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      ) : (
        <>
      <input
        value={searchInput}
        onChange={(e) => setSearchInput(e.target.value)}
        placeholder="Search by name, brewery, or style"
        className="w-full"
      />

      <div className="flex flex-wrap gap-2">
        {AVAILABILITY_FILTERS.map((filter) => (
          <button
            key={filter.value}
            type="button"
            onClick={() => setAvailability(filter.value)}
            className={`${CHIP_BASE} ${availability === filter.value ? CHIP_ACTIVE : CHIP_INACTIVE}`}
          >
            {filter.label}
          </button>
        ))}
      </div>

      {isSignedIn && confirmedCount === 0 && (
        <p className="m-0 rounded-xl bg-amber-50 p-3 text-sm text-amber-900">
          New here? Try filtering by style, or ask the bartender what&apos;s popular tonight.
        </p>
      )}

      {isSignedIn && (
        <div className="flex flex-wrap gap-2">
          {HAD_STATUS_FILTERS.map((filter) => (
            <button
              key={filter.value}
              type="button"
              onClick={() => setHadStatus(filter.value)}
              className={`${CHIP_BASE} ${hadStatus === filter.value ? 'border-amber-600 bg-amber-600 text-white' : CHIP_INACTIVE}`}
            >
              {filter.label}
            </button>
          ))}
        </div>
      )}

      {(styleChips.length > 0 || breweryChips.length > 0) && (
        <div className="flex flex-wrap gap-2">
          {[...styleChips, ...breweryChips].map((value) => (
            <button
              key={value}
              type="button"
              onClick={() => toggleQuickSearch(value)}
              className={`rounded-full border px-3 py-1 text-xs ${
                searchInput === value ? 'border-gray-900 bg-gray-100 text-gray-900' : 'border-gray-200 bg-white text-gray-500'
              }`}
            >
              {value}
            </button>
          ))}
        </div>
      )}

      {error && <p className="text-red-700">{error}</p>}

      {loading ? (
        <p>Loading beers…</p>
      ) : beers.length === 0 ? (
        <p>No beers match.</p>
      ) : (
        <div className="grid gap-3">
          <p className="m-0 text-sm text-gray-500">
            {totalCount} beer{totalCount === 1 ? '' : 's'}
          </p>
          {beers.map((beer) => {
            const badge = AVAILABILITY_BADGES[beer.availability];
            return (
              <Link
                key={beer.id}
                to={`/beers/${beer.id}`}
                className="block rounded-2xl bg-white p-4 shadow-[0_8px_24px_rgba(0,0,0,0.06)]"
              >
                <div className="flex items-center justify-between gap-2">
                  <strong>{beer.name}</strong>
                  <span className="flex items-center gap-2">
                    {beer.confirmed && <span className="text-sm text-green-700">✓ Had it</span>}
                    {badge && (
                      <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${badge.className}`}>
                        {badge.label}
                      </span>
                    )}
                  </span>
                </div>
                <div className="text-gray-600">
                  {beer.brewery} • {beer.style}
                </div>
              </Link>
            );
          })}
        </div>
      )}
        </>
      )}
    </div>
  );
}

export default BeerList;
