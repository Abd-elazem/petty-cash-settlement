import { useMemo, useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { GlobalErrorBanner } from "../components/GlobalErrorBanner";

export function AppLayout(): JSX.Element {
  const { user, signOut, authError, clearError } = useAuth();
  const [isSidebarOpen, setSidebarOpen] = useState(false);
  const [isProfileMenuOpen, setProfileMenuOpen] = useState(false);

  const handleSignOut = async (): Promise<void> => {
    setProfileMenuOpen(false);
    await signOut();
  };
  const navigation = useMemo(
    () => [
      { to: "/", label: "Dashboard", end: true },
      { to: "/my-settlements", label: "My Settlements", end: false },
      ...(user?.isManager ? [{ to: "/manager-inbox", label: "Manager Inbox", end: false }] : [])
    ],
    [user?.isManager]
  );

  return (
    <div className="app-shell">
      <aside id="app-sidebar" className={`app-sidebar ${isSidebarOpen ? "app-sidebar--open" : ""}`}>
        <h1>Petty Cash Settlement</h1>
        <nav>
          <ul>
            {navigation.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) =>
                    isActive ? "app-nav-link app-nav-link--active" : "app-nav-link"
                  }
                  onClick={() => setSidebarOpen(false)}
                >
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </aside>

      <div className="app-main">
        <header className="app-header">
          <button
            className="button-ghost"
            type="button"
            onClick={() => setSidebarOpen((prev) => !prev)}
            aria-expanded={isSidebarOpen}
            aria-controls="app-sidebar"
          >
            ☰
          </button>

          <div className="app-header-title">Petty Cash Settlement</div>

          <div className="profile-menu">
            <button
              className="button-primary"
              type="button"
              onClick={() => setProfileMenuOpen((prev) => !prev)}
            >
              {user?.name ?? "User"}
            </button>
            {isProfileMenuOpen ? (
              <div className="profile-menu-popover">
                <div className="profile-menu-user">{user?.username}</div>
                <button className="button-link" type="button" onClick={() => void handleSignOut()}>
                  Sign out
                </button>
              </div>
            ) : null}
          </div>
        </header>
        {authError ? <GlobalErrorBanner message={authError} onDismiss={clearError} /> : null}
        <main>
          <Outlet />
        </main>
      </div>
    </div>
  );
}

