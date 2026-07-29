import { useState } from 'react';
import { Link, NavLink } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { AccountMenu } from './AccountMenu';

function navLinkClassName({ isActive }: { isActive: boolean }) {
  return isActive
    ? 'text-on-primary bg-primary px-4 py-1.5 font-label-md rounded-full transition-colors'
    : 'text-on-surface-variant px-4 py-1.5 font-label-md hover:text-primary transition-colors rounded-full';
}

// One nav list serves both breakpoints: a static row from md up, an absolute drop-down panel
// below it. Rendering it once keeps a single element per link in the accessibility tree.
const NAV_LINKS_CLASSES = [
  'md:flex absolute md:static top-full left-0 right-0',
  'flex-col md:flex-row items-stretch md:items-center',
  'gap-stack-sm md:gap-stack-md',
  'bg-surface md:bg-transparent shadow-sm md:shadow-none',
  'border-t md:border-t-0 border-outline-variant/30',
  'px-margin-mobile md:px-0 py-4 md:py-0',
].join(' ');

/** Sticky app header: brand, primary nav (collapsed behind a toggle under md), and the account menu. */
export function AppHeader() {
  const { isAuthenticated } = useAuth();
  const [isNavOpen, setIsNavOpen] = useState(false);

  const closeNav = () => setIsNavOpen(false);

  return (
    // bg-surface/95, not /80: at 80% the chips and titles scrolling underneath showed through
    // and read as a smudge behind the brand and nav.
    <header className="sticky top-0 z-50 w-full bg-surface/95 backdrop-blur-md shadow-sm">
      <nav
        aria-label="Main"
        className="relative flex justify-between items-center w-full px-margin-mobile md:px-margin-desktop py-4 max-w-container-max mx-auto min-h-[var(--app-header-height)]"
      >
        <Link to="/" className="flex items-center gap-stack-sm" onClick={closeNav}>
          <span className="material-symbols-outlined text-primary text-[28px]" aria-hidden="true">
            flight_takeoff
          </span>
          <span className="text-headline-md font-headline-md font-extrabold text-primary">
            TripPlanner
          </span>
        </Link>

        {/* Nav links, account and the mobile toggle share one flex child so `justify-between`
            keeps them together on the right. As three separate children the links were pushed
            to the centre of the header instead. */}
        <div className="flex items-center gap-stack-md">
          <div
            id="app-nav-links"
            className={`${isNavOpen ? 'flex' : 'hidden'} ${NAV_LINKS_CLASSES}`}
          >
            <NavLink to="/" end className={navLinkClassName} onClick={closeNav}>
              Discover
            </NavLink>
            {isAuthenticated ? (
              <NavLink to="/trips" className={navLinkClassName} onClick={closeNav}>
                My trips
              </NavLink>
            ) : (
              <NavLink to="/login" className={navLinkClassName} onClick={closeNav}>
                Log in
              </NavLink>
            )}
          </div>

          {isAuthenticated && <AccountMenu />}

          <button
            type="button"
            className="md:hidden w-10 h-10 rounded-full flex items-center justify-center text-on-surface-variant hover:text-primary focus:outline-none focus:ring-2 focus:ring-primary/40 transition-colors"
            aria-label={isNavOpen ? 'Close navigation menu' : 'Open navigation menu'}
            aria-expanded={isNavOpen}
            aria-controls="app-nav-links"
            onClick={() => setIsNavOpen((open) => !open)}
          >
            <span className="material-symbols-outlined" aria-hidden="true">
              {isNavOpen ? 'close' : 'menu'}
            </span>
          </button>
        </div>
      </nav>
    </header>
  );
}
