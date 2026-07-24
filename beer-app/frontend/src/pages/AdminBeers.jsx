import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  deleteBeer,
  getRolesFromToken,
  searchBeers,
  updateBeerAvailability,
} from '../lib/api';

const AVAILABILITY_OPTIONS = ['OnTap', 'Available', 'OutOfStock', 'Retired'];

// Beer Management Table (#57): the only place catalog CRUD appears now (per
// MVP_SCREEN_PLAN.md) — Add/Edit reuse BeerForm.jsx unchanged, Delete is net-new here.
// Same admin-gate-then-load shape as AdminUsers.jsx/AdminConfirmations.jsx. Availability
// changes fire immediately (#56's PATCH endpoint needs no reason); only Delete goes
// through the two-step reason guard, since #56's DELETE requires one.
//
// #76: the delete-step microcopy calls out that a beer with existing customer
// confirmations can't actually be deleted — BeerConfirmation.BeerId is a restrict-on-
// delete FK (ApplicationDbContext), so the API call fails rather than cascading. Void
// the confirmations via AdminConfirmations.jsx first, or use Retired instead.
function AdminBeers() {
  const isAdmin = getRolesFromToken().includes('Admin');
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [beers, setBeers] = useState([]);
  const [pendingDeleteId, setPendingDeleteId] = useState(null);
  const [reason, setReason] = useState('');
  const [message, setMessage] = useState('');

  const load = () => {
    searchBeers({ search: debouncedSearch, availability: 'all' })
      .then((data) => setBeers(data.items))
      .catch((error) => setMessage(error.message));
  };

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchInput.trim()), 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    if (isAdmin) load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAdmin, debouncedSearch]);

  if (!isAdmin) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">Beer management</h2>
        <p className="mt-2 text-gray-600">Sign in with an admin account to manage beers.</p>
      </section>
    );
  }

  const handleAvailabilityChange = async (beerId, availability) => {
    try {
      await updateBeerAvailability(beerId, availability);
      setMessage('');
      load();
    } catch (error) {
      setMessage(error.message);
    }
  };

  const startDelete = (beerId) => {
    setPendingDeleteId(beerId);
    setReason('');
    setMessage('');
  };

  const cancelDelete = () => {
    setPendingDeleteId(null);
    setReason('');
    setMessage('');
  };

  const confirmDelete = async () => {
    if (!reason.trim()) {
      setMessage('A reason is required to delete a beer.');
      return;
    }
    try {
      await deleteBeer(pendingDeleteId, reason.trim());
      cancelDelete();
      load();
    } catch (error) {
      setMessage(error.message);
    }
  };

  return (
    <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-xl font-bold">Beer management</h2>
        <Link
          to="/beers/new"
          className="rounded-full border-0 bg-gray-900 px-4 py-2 text-sm font-medium text-white hover:bg-gray-700"
        >
          Add Beer
        </Link>
      </div>

      <input
        value={searchInput}
        onChange={(e) => setSearchInput(e.target.value)}
        placeholder="Search by name, brewery, or style"
        className="mt-4 w-full"
      />
      <p className="mt-1 text-sm text-gray-600">
        Availability changes apply immediately, with no confirm step. Deleting requires a
        reason and can&apos;t be undone.
      </p>

      {message && <p className="mt-3 text-red-700">{message}</p>}

      <div className="mt-4 overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-gray-200 text-gray-500">
              <th className="py-2 pr-4 font-medium">Name</th>
              <th className="py-2 pr-4 font-medium">Brewery</th>
              <th className="py-2 pr-4 font-medium">Style</th>
              <th className="py-2 pr-4 font-medium">Availability</th>
              <th className="py-2 font-medium"></th>
            </tr>
          </thead>
          <tbody>
            {beers.map((beer) => (
              <tr key={beer.id} className="border-b border-gray-100 align-top">
                <td className="py-2 pr-4">{beer.name}</td>
                <td className="py-2 pr-4">{beer.brewery}</td>
                <td className="py-2 pr-4">{beer.style}</td>
                <td className="py-2 pr-4">
                  <select
                    value={beer.availability}
                    onChange={(e) => handleAvailabilityChange(beer.id, e.target.value)}
                  >
                    {AVAILABILITY_OPTIONS.map((option) => (
                      <option key={option} value={option}>
                        {option}
                      </option>
                    ))}
                  </select>
                </td>
                <td className="py-2">
                  {pendingDeleteId === beer.id ? (
                    <div className="flex max-w-xs flex-col gap-2">
                      <p className="m-0 text-xs text-gray-500">
                        This can&apos;t be undone. If any customer has already confirmed this
                        beer, the delete will fail — void those confirmations first, or mark it
                        Retired instead.
                      </p>
                      <span className="flex flex-wrap items-center gap-2">
                        <input
                          value={reason}
                          onChange={(e) => setReason(e.target.value)}
                          placeholder="Reason for deleting"
                          className="w-40"
                        />
                        <button type="button" onClick={confirmDelete} className="border-0 bg-red-700 text-white">
                          Confirm delete
                        </button>
                        <button type="button" onClick={cancelDelete}>
                          Cancel
                        </button>
                      </span>
                    </div>
                  ) : (
                    <span className="flex flex-wrap items-center gap-2">
                      <Link to={`/beers/${beer.id}/edit`}>Edit</Link>
                      <button type="button" onClick={() => startDelete(beer.id)}>
                        Delete
                      </button>
                    </span>
                  )}
                </td>
              </tr>
            ))}
            {beers.length === 0 && (
              <tr>
                <td colSpan="5" className="py-4 text-gray-500">
                  No beers match.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export default AdminBeers;
