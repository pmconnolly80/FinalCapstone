import { useEffect, useState } from 'react';
import { fetchAdminRecommendations, getRolesFromToken, updateRecommendationStatus } from '../lib/api';

const STATUS_FILTERS = [
  { value: '', label: 'All' },
  { value: 'New', label: 'New' },
  { value: 'Reviewed', label: 'Reviewed' },
  { value: 'Added', label: 'Added' },
  { value: 'Declined', label: 'Declined' },
];

const CHIP_BASE = 'rounded-full border px-3 py-1 text-sm';
const CHIP_ACTIVE = 'border-gray-900 bg-gray-900 text-white';
const CHIP_INACTIVE = 'border-gray-300 bg-white text-gray-700';

// #73 admin triage: same admin-gate-then-load shape as AdminUsers.jsx/AdminBeers.jsx.
// Status transitions fire immediately (no reason guard) since the PATCH endpoint
// doesn't require one — closer to AdminBeers.jsx's availability <select> than
// AdminUsers.jsx's pendingAction reason guard.
function AdminRecommendations() {
  const isAdmin = getRolesFromToken().includes('Admin');
  const [status, setStatus] = useState('');
  const [recommendations, setRecommendations] = useState([]);
  const [message, setMessage] = useState('');

  const load = () => {
    fetchAdminRecommendations(status)
      .then(setRecommendations)
      .catch((error) => setMessage(error.message));
  };

  useEffect(() => {
    if (isAdmin) load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAdmin, status]);

  if (!isAdmin) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">Beer recommendations</h2>
        <p className="mt-2 text-gray-600">Sign in with an admin account to view recommendations.</p>
      </section>
    );
  }

  const handleStatusChange = async (id, newStatus) => {
    try {
      await updateRecommendationStatus(id, newStatus);
      setMessage('');
      load();
    } catch (error) {
      setMessage(error.message);
    }
  };

  return (
    <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
      <h2 className="m-0 text-xl font-bold">Beer recommendations</h2>

      <div className="mt-4 flex flex-wrap gap-2">
        {STATUS_FILTERS.map((filter) => (
          <button
            key={filter.value}
            type="button"
            onClick={() => setStatus(filter.value)}
            className={`${CHIP_BASE} ${status === filter.value ? CHIP_ACTIVE : CHIP_INACTIVE}`}
          >
            {filter.label}
          </button>
        ))}
      </div>

      {message && <p className="mt-3 text-red-700">{message}</p>}

      <div className="mt-4 grid gap-3">
        {recommendations.length === 0 && <p className="text-gray-500">No recommendations match.</p>}
        {recommendations.map((rec) => (
          <div key={rec.id} className="rounded-2xl border border-gray-200 p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <strong>{rec.beerName}</strong>
                {rec.breweryName && <span className="text-gray-600"> • {rec.breweryName}</span>}
              </div>
              <span className="text-sm text-gray-500">{rec.status}</span>
            </div>
            <p className="mt-1 text-sm text-gray-500">{rec.customerEmail}</p>
            {rec.note && <p className="mt-1 text-sm text-gray-700">{rec.note}</p>}
            <div className="mt-2 flex flex-wrap gap-2">
              {rec.status !== 'Reviewed' && (
                <button type="button" onClick={() => handleStatusChange(rec.id, 'Reviewed')}>
                  Mark Reviewed
                </button>
              )}
              {rec.status !== 'Added' && (
                <button type="button" onClick={() => handleStatusChange(rec.id, 'Added')}>
                  Mark Added
                </button>
              )}
              {rec.status !== 'Declined' && (
                <button type="button" onClick={() => handleStatusChange(rec.id, 'Declined')}>
                  Mark Declined
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

export default AdminRecommendations;
