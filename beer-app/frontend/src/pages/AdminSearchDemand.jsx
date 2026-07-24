import { useEffect, useState } from 'react';
import { fetchExternalSearchDemand, getRolesFromToken } from '../lib/api';

// #83: surfaces what #72 logs — customer "look up any beer" searches with no match in
// the tavern's own list — aggregated by frequency. Independent fetch with its own error
// state, same pattern as AdminDashboard.jsx's anomalies panel.
function AdminSearchDemand() {
  const isAdmin = getRolesFromToken().includes('Admin');
  const [demand, setDemand] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!isAdmin) return;
    fetchExternalSearchDemand().then(setDemand).catch((err) => setError(err.message));
  }, [isAdmin]);

  if (!isAdmin) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">Search demand</h2>
        <p className="mt-2 text-gray-600">Sign in with an admin account to view search demand.</p>
      </section>
    );
  }

  return (
    <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
      <h2 className="m-0 text-xl font-bold">Search demand</h2>
      <p className="mt-2 text-gray-600">
        What customers looked up that the tavern doesn&apos;t carry, most-searched first.
      </p>

      {error && <p className="mt-3 text-red-700">{error}</p>}

      {demand && demand.length === 0 && <p className="mt-3 text-gray-500">No unmatched searches yet.</p>}

      {demand && demand.length > 0 && (
        <div className="mt-4 overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-gray-200 text-gray-500">
                <th className="py-2 pr-4 font-medium">Query</th>
                <th className="py-2 pr-4 font-medium">Count</th>
                <th className="py-2 font-medium">Last searched</th>
              </tr>
            </thead>
            <tbody>
              {demand.map((item) => (
                <tr key={item.query} className="border-b border-gray-100">
                  <td className="py-2 pr-4">{item.query}</td>
                  <td className="py-2 pr-4">{item.count}</td>
                  <td className="py-2">{new Date(item.lastSearchedAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

export default AdminSearchDemand;
