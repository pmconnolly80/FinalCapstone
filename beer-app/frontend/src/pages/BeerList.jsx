import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { searchBeers } from '../lib/api';

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
  const isSignedIn = Boolean(localStorage.getItem('beer-token'));

  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [availability, setAvailability] = useState('');
  const [hadStatus, setHadStatus] = useState('');
  const [beers, setBeers] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchInput.trim()), 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

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
    </div>
  );
}

export default BeerList;
