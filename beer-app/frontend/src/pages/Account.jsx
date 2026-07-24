import { Link, useNavigate } from 'react-router-dom';
import { getRolesFromToken, logout } from '../lib/api';

const linkClass =
  'block rounded-2xl bg-white p-4 font-medium text-gray-900 shadow-[0_8px_24px_rgba(0,0,0,0.06)] hover:bg-gray-50';

// The bottom tab bar (#67) only has room for a single "Account" tab — this hub is
// where all the links that used to live flat in the top nav now land, plus every
// admin-only screen, role-aware per #82.
function Account() {
  const navigate = useNavigate();
  const roles = getRolesFromToken();
  const isStaff = roles.includes('Bartender') || roles.includes('Admin');
  const isAdmin = roles.includes('Admin');

  const handleSignOut = () => {
    logout();
    navigate('/');
  };

  return (
    <div className="grid gap-4">
      <h2 className="m-0 text-xl font-bold">Account</h2>

      <div className="grid gap-2">
        {isStaff && (
          <Link to="/my-pin" className={linkClass}>
            My PIN
          </Link>
        )}
        <Link to="/account/linked-providers" className={linkClass}>
          Linked accounts
        </Link>
        <Link to="/recommend" className={linkClass}>
          Recommend a beer
        </Link>
        <Link to="/privacy" className={linkClass}>
          Privacy policy
        </Link>
      </div>

      {isAdmin && (
        <>
          <h3 className="m-0 mt-2 text-sm font-semibold uppercase tracking-wide text-gray-500">
            Admin
          </h3>
          <div className="grid gap-2">
            <Link to="/admin/dashboard" className={linkClass}>
              Dashboard
            </Link>
            <Link to="/admin/confirmations" className={linkClass}>
              Confirmations
            </Link>
            <Link to="/admin/users" className={linkClass}>
              Users
            </Link>
            <Link to="/admin/beers" className={linkClass}>
              Manage Beers
            </Link>
            <Link to="/admin/recommendations" className={linkClass}>
              Recommendations
            </Link>
            <Link to="/admin/search-demand" className={linkClass}>
              Search Demand
            </Link>
          </div>
        </>
      )}

      <button
        type="button"
        onClick={handleSignOut}
        className="mt-2 rounded-full border-0 bg-gray-900 px-6 py-3 font-medium text-white hover:bg-gray-700"
      >
        Sign out
      </button>
    </div>
  );
}

export default Account;
