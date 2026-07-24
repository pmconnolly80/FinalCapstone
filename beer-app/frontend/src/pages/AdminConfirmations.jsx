import { useEffect, useState } from 'react';
import {
  fetchAdminConfirmations,
  fetchConfirmationAudits,
  getRolesFromToken,
  voidConfirmation,
} from '../lib/api';

// Admin correction screen (#16): review confirmation history, void with a required
// reason behind an explicit confirm step, and read the audit trail in place. The role
// check here is a convenience — the API enforces Admin server-side regardless.
//
// #76: inline microcopy at the void step itself (not just the page-level paragraph
// above) states what voiding actually does, including that an already-awarded mug is
// not revoked — mirrored from TECHNICAL_ARCHITECTURE_PLAN.md §4.1.
function AdminConfirmations() {
  const isAdmin = getRolesFromToken().includes('Admin');
  const [rows, setRows] = useState([]);
  const [audits, setAudits] = useState([]);
  const [filter, setFilter] = useState('');
  const [voidingId, setVoidingId] = useState(null);
  const [reason, setReason] = useState('');
  const [message, setMessage] = useState('');

  const load = () => {
    Promise.all([fetchAdminConfirmations(), fetchConfirmationAudits()])
      .then(([confirmations, corrections]) => {
        setRows(confirmations);
        setAudits(corrections);
      })
      .catch((error) => setMessage(error.message));
  };

  useEffect(() => {
    if (isAdmin) load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAdmin]);

  if (!isAdmin) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">Confirmation review</h2>
        <p className="mt-2 text-gray-600">Sign in with an admin account to review and correct confirmations.</p>
      </section>
    );
  }

  const needle = filter.trim().toLowerCase();
  const visible = needle
    ? rows.filter((r) =>
        [r.customerEmail, r.bartenderEmail, r.beerName].some((v) => v.toLowerCase().includes(needle))
      )
    : rows;

  const startVoid = (id) => {
    setVoidingId(id);
    setReason('');
    setMessage('');
  };

  const confirmVoid = async () => {
    if (!reason.trim()) {
      setMessage('A reason is required to void a confirmation.');
      return;
    }
    try {
      await voidConfirmation(voidingId, reason.trim());
      setVoidingId(null);
      setReason('');
      setMessage('');
      load();
    } catch (error) {
      setMessage(error.message);
    }
  };

  return (
    <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
      <h2 className="m-0 text-xl font-bold">Confirmation review</h2>
      <p className="mt-1 text-sm text-gray-600">
        Voiding removes the confirmation from the customer&apos;s progress and records who
        corrected it, when, and why. The beer can be confirmed again afterwards.
      </p>

      <input
        value={filter}
        onChange={(e) => setFilter(e.target.value)}
        placeholder="Filter by customer, bartender, or beer"
        className="mt-4 w-full"
      />
      {message && <p className="mt-3 text-red-700">{message}</p>}

      <div className="mt-4 overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-gray-200 text-gray-500">
              <th className="py-2 pr-4 font-medium">Customer</th>
              <th className="py-2 pr-4 font-medium">Beer</th>
              <th className="py-2 pr-4 font-medium">Confirmed by</th>
              <th className="py-2 pr-4 font-medium">When</th>
              <th className="py-2 font-medium"></th>
            </tr>
          </thead>
          <tbody>
            {visible.map((row) => (
              <tr key={row.id} className="border-b border-gray-100 align-top">
                <td className="py-2 pr-4">{row.customerEmail}</td>
                <td className="py-2 pr-4">{row.beerName}</td>
                <td className="py-2 pr-4">{row.bartenderEmail}</td>
                <td className="py-2 pr-4 whitespace-nowrap">{new Date(row.confirmedAt).toLocaleString()}</td>
                <td className="py-2">
                  {voidingId === row.id ? (
                    <div className="flex max-w-xs flex-col gap-2">
                      <p className="m-0 text-xs text-gray-500">
                        Voiding removes this from the customer&apos;s progress and frees the beer
                        for re-confirmation. If a mug was already awarded, it is not revoked.
                      </p>
                      <span className="flex flex-wrap items-center gap-2">
                        <input
                          value={reason}
                          onChange={(e) => setReason(e.target.value)}
                          placeholder="Reason for voiding"
                          className="w-48"
                        />
                        <button type="button" onClick={confirmVoid} className="border-0 bg-red-700 text-white">
                          Confirm void
                        </button>
                        <button type="button" onClick={() => setVoidingId(null)}>
                          Cancel
                        </button>
                      </span>
                    </div>
                  ) : (
                    <button type="button" onClick={() => startVoid(row.id)}>
                      Void
                    </button>
                  )}
                </td>
              </tr>
            ))}
            {visible.length === 0 && (
              <tr>
                <td colSpan="5" className="py-4 text-gray-500">
                  No confirmations match.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <h3 className="mt-8 text-lg font-semibold">Voided confirmations</h3>
      {audits.length === 0 ? (
        <p className="mt-2 text-sm text-gray-500">No corrections yet.</p>
      ) : (
        <div className="mt-2 overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-gray-200 text-gray-500">
                <th className="py-2 pr-4 font-medium">Customer</th>
                <th className="py-2 pr-4 font-medium">Beer</th>
                <th className="py-2 pr-4 font-medium">Voided by</th>
                <th className="py-2 pr-4 font-medium">When</th>
                <th className="py-2 font-medium">Reason</th>
              </tr>
            </thead>
            <tbody>
              {audits.map((audit) => (
                <tr key={audit.id} className="border-b border-gray-100">
                  <td className="py-2 pr-4">{audit.customerEmail}</td>
                  <td className="py-2 pr-4">{audit.beerName}</td>
                  <td className="py-2 pr-4">{audit.adminEmail}</td>
                  <td className="py-2 pr-4 whitespace-nowrap">{new Date(audit.correctedAt).toLocaleString()}</td>
                  <td className="py-2">{audit.reason}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

export default AdminConfirmations;
