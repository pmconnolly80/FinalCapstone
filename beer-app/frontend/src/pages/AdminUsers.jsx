import { useEffect, useState } from 'react';
import {
  assignRole,
  deactivateAccount,
  deactivateStaffPin,
  getAdminUsers,
  getRolesFromToken,
  inviteBartender,
  issueOrResetStaffPin,
  reactivateAccount,
} from '../lib/api';

const ROLES = ['Admin', 'Bartender', 'Customer'];
const STAFF_ROLES = ['Admin', 'Bartender'];

const STATUS_BADGES = {
  active: { label: 'Active', className: 'bg-green-100 text-green-800' },
  deactivated: { label: 'Deactivated', className: 'bg-red-100 text-red-800' },
};

const PIN_BADGES = {
  active: { label: 'PIN active', className: 'bg-green-100 text-green-800' },
  none: { label: 'No PIN', className: 'bg-gray-100 text-gray-600' },
};

// User Management screen (#55): the missing UI in front of #53's role assignment,
// #54's account deactivate/reactivate, and Sprint 2's staff-PIN lifecycle API
// (StaffPinsController) — no admin UI has ever existed for that last one. Same
// admin-gate-then-load shape and two-step reason-guarded action pattern as
// AdminConfirmations.jsx; the role check here is a convenience, the API enforces
// Admin server-side regardless.
//
// #77: the invite form is a direct, un-guarded submit (no reason step) — inviting a new
// bartender isn't a correction to an existing account the way role/deactivate/reactivate
// are, so it doesn't fit that pattern.
//
// #75: default view filters to staff (Bartender/Admin) only, since this screen is meant
// for managing a handful of bartenders, not browsing the full customer base — a "show
// all" toggle covers the rare customer lookup. The filter box searches email only (the
// user model has no separate display name), same client-side-filter pattern as
// AdminConfirmations.jsx.
function AdminUsers() {
  const isAdmin = getRolesFromToken().includes('Admin');
  const [users, setUsers] = useState([]);
  const [pendingAction, setPendingAction] = useState(null);
  const [reason, setReason] = useState('');
  const [pin, setPin] = useState('');
  const [message, setMessage] = useState('');
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteMessage, setInviteMessage] = useState('');
  const [showAll, setShowAll] = useState(false);
  const [filter, setFilter] = useState('');

  const load = () => {
    getAdminUsers()
      .then(setUsers)
      .catch((error) => setMessage(error.message));
  };

  useEffect(() => {
    if (isAdmin) load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAdmin]);

  if (!isAdmin) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">User management</h2>
        <p className="mt-2 text-gray-600">Sign in with an admin account to manage users.</p>
      </section>
    );
  }

  const startAction = (userId, type, extra = {}) => {
    setPendingAction({ userId, type, ...extra });
    setReason('');
    setPin('');
    setMessage('');
  };

  const cancelAction = () => {
    setPendingAction(null);
    setReason('');
    setPin('');
    setMessage('');
  };

  const confirmPendingAction = async () => {
    if (!pendingAction) return;
    const { userId, type, role } = pendingAction;

    try {
      if (type === 'pin') {
        if (!/^\d{6}$/.test(pin)) {
          setMessage('PINs are exactly 6 digits.');
          return;
        }
        await issueOrResetStaffPin(userId, pin);
      } else {
        if (!reason.trim()) {
          setMessage('A reason is required.');
          return;
        }
        if (type === 'role') {
          await assignRole(userId, role, reason.trim());
        } else if (type === 'deactivate') {
          await deactivateAccount(userId, reason.trim());
        } else if (type === 'reactivate') {
          await reactivateAccount(userId, reason.trim());
        }
      }
      cancelAction();
      load();
    } catch (error) {
      setMessage(error.message);
    }
  };

  const deactivatePin = async (userId) => {
    try {
      await deactivateStaffPin(userId);
      load();
    } catch (error) {
      setMessage(error.message);
    }
  };

  const handleInvite = async (event) => {
    event.preventDefault();
    setInviteMessage('');
    const email = inviteEmail.trim();
    try {
      await inviteBartender(email);
      setInviteMessage(`Invited ${email} — they'll get an email to set their password.`);
      setInviteEmail('');
      load();
    } catch (error) {
      setInviteMessage(error.message);
    }
  };

  const needle = filter.trim().toLowerCase();
  const visible = users
    .filter((u) => showAll || STAFF_ROLES.includes(u.role))
    .filter((u) => !needle || u.email.toLowerCase().includes(needle));

  return (
    <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
      <h2 className="m-0 text-xl font-bold">User management</h2>
      <p className="mt-1 text-sm text-gray-600">
        Change a user&apos;s role, deactivate or reactivate their account, and manage their
        staff PIN. Role changes and account status changes require a reason.
      </p>

      <form onSubmit={handleInvite} className="mt-4 flex flex-wrap items-end gap-2 rounded-xl bg-gray-50 p-4">
        <label className="flex flex-col gap-1 text-sm text-gray-600">
          Invite a new bartender
          <input
            type="email"
            required
            value={inviteEmail}
            onChange={(e) => setInviteEmail(e.target.value)}
            placeholder="newhire@example.com"
            className="w-64"
          />
        </label>
        <button type="submit" className="border-0 bg-blue-700 text-white">
          Invite bartender
        </button>
      </form>
      {inviteMessage && <p className="mt-2 text-sm">{inviteMessage}</p>}

      {message && <p className="mt-3 text-red-700">{message}</p>}

      <div className="mt-4 flex flex-wrap items-center gap-3">
        <input
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          placeholder="Filter by email"
          className="w-64"
        />
        <label className="flex items-center gap-2 text-sm text-gray-600">
          <input type="checkbox" checked={showAll} onChange={(e) => setShowAll(e.target.checked)} />
          Show all users (including customers)
        </label>
      </div>

      <div className="mt-4 overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-gray-200 text-gray-500">
              <th className="py-2 pr-4 font-medium">Email</th>
              <th className="py-2 pr-4 font-medium">Role</th>
              <th className="py-2 pr-4 font-medium">Status</th>
              <th className="py-2 pr-4 font-medium">PIN</th>
              <th className="py-2 font-medium"></th>
            </tr>
          </thead>
          <tbody>
            {visible.map((user) => {
              const statusBadge = STATUS_BADGES[user.isActive ? 'active' : 'deactivated'];
              const pinBadge = PIN_BADGES[user.hasActivePin ? 'active' : 'none'];
              const isStaff = STAFF_ROLES.includes(user.role);
              const isPending = pendingAction?.userId === user.id;

              return (
                <tr key={user.id} className="border-b border-gray-100 align-top">
                  <td className="py-2 pr-4">{user.email}</td>
                  <td className="py-2 pr-4">
                    <select
                      value={isPending && pendingAction.type === 'role' ? pendingAction.role : user.role}
                      onChange={(e) => startAction(user.id, 'role', { role: e.target.value })}
                    >
                      {ROLES.map((role) => (
                        <option key={role} value={role}>
                          {role}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="py-2 pr-4">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusBadge.className}`}>
                      {statusBadge.label}
                    </span>
                  </td>
                  <td className="py-2 pr-4">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${pinBadge.className}`}>
                      {pinBadge.label}
                    </span>
                  </td>
                  <td className="py-2">
                    {isPending && (pendingAction.type === 'role' || pendingAction.type === 'deactivate' || pendingAction.type === 'reactivate') && (
                      <span className="flex flex-wrap items-center gap-2">
                        <input
                          value={reason}
                          onChange={(e) => setReason(e.target.value)}
                          placeholder="Reason"
                          className="w-40"
                        />
                        <button type="button" onClick={confirmPendingAction} className="border-0 bg-red-700 text-white">
                          Confirm
                        </button>
                        <button type="button" onClick={cancelAction}>
                          Cancel
                        </button>
                      </span>
                    )}
                    {isPending && pendingAction.type === 'pin' && (
                      <span className="flex flex-wrap items-center gap-2">
                        <input
                          type="password"
                          inputMode="numeric"
                          maxLength={6}
                          value={pin}
                          onChange={(e) => setPin(e.target.value)}
                          placeholder="6-digit PIN"
                          className="w-28 text-center tracking-[4px]"
                        />
                        <button type="button" onClick={confirmPendingAction} className="border-0 bg-red-700 text-white">
                          Confirm
                        </button>
                        <button type="button" onClick={cancelAction}>
                          Cancel
                        </button>
                      </span>
                    )}
                    {!isPending && (
                      <span className="flex flex-wrap items-center gap-2">
                        {user.isActive ? (
                          <button type="button" onClick={() => startAction(user.id, 'deactivate')}>
                            Deactivate
                          </button>
                        ) : (
                          <button type="button" onClick={() => startAction(user.id, 'reactivate')}>
                            Reactivate
                          </button>
                        )}
                        {isStaff && (
                          <button type="button" onClick={() => startAction(user.id, 'pin')}>
                            Set PIN
                          </button>
                        )}
                        {isStaff && user.hasActivePin && (
                          <button type="button" onClick={() => deactivatePin(user.id)}>
                            Deactivate PIN
                          </button>
                        )}
                      </span>
                    )}
                  </td>
                </tr>
              );
            })}
            {visible.length === 0 && (
              <tr>
                <td colSpan="5" className="py-4 text-gray-500">
                  {users.length === 0 ? 'No users found.' : 'No users match this filter.'}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export default AdminUsers;
