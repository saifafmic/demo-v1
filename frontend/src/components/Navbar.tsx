import React, { useState, useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Video, Bell, Shield, User, LogOut, Calendar, LayoutDashboard } from 'lucide-react';
import { api } from '../api/client';

export const Navbar: React.FC = () => {
  const { user, logout, isAdmin } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [unreadCount, setUnreadCount] = useState(0);

  useEffect(() => {
    if (user) {
      api.get('/notifications').then((res) => {
        if (res.success && res.data) {
          const unread = res.data.filter((n: any) => !n.isRead).length;
          setUnreadCount(unread);
        }
      });
    }
  }, [user, location.pathname]);

  if (!user) return null;

  return (
    <nav className="navbar">
      <Link to="/dashboard" className="navbar-brand">
        <div style={{ background: 'var(--accent-gradient)', padding: '0.45rem', borderRadius: '10px', display: 'flex' }}>
          <Video size={20} color="white" />
        </div>
        <span>Sync<span className="brand-gradient">Meet</span></span>
      </Link>

      <ul className="nav-links">
        <li>
          <Link
            to="/dashboard"
            className={`nav-link ${location.pathname === '/dashboard' ? 'active' : ''}`}
            style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}
          >
            <LayoutDashboard size={17} />
            <span>Dashboard</span>
          </Link>
        </li>
        <li>
          <Link
            to="/meetings"
            className={`nav-link ${location.pathname.startsWith('/meetings') ? 'active' : ''}`}
            style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}
          >
            <Calendar size={17} />
            <span>Meetings</span>
          </Link>
        </li>
        {isAdmin && (
          <li>
            <Link
              to="/admin"
              className={`nav-link ${location.pathname.startsWith('/admin') ? 'active' : ''}`}
              style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', color: '#a855f7' }}
            >
              <Shield size={17} />
              <span>Admin Portal</span>
            </Link>
          </li>
        )}
      </ul>

      <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem' }}>
        <Link
          to="/notifications"
          style={{
            position: 'relative',
            color: 'var(--text-muted)',
            padding: '0.5rem',
            borderRadius: '50%',
            display: 'flex',
          }}
          title="Notifications"
        >
          <Bell size={20} />
          {unreadCount > 0 && (
            <span
              style={{
                position: 'absolute',
                top: '2px',
                right: '2px',
                background: '#ef4444',
                color: 'white',
                fontSize: '0.7rem',
                fontWeight: 700,
                width: '18px',
                height: '18px',
                borderRadius: '50%',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              {unreadCount}
            </span>
          )}
        </Link>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.85rem' }}>
          <Link
            to="/profile"
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.65rem',
              padding: '0.35rem 0.75rem',
              borderRadius: 'var(--radius-full)',
              background: 'rgba(255, 255, 255, 0.05)',
              border: '1px solid var(--border-subtle)',
            }}
          >
            <div
              style={{
                width: '32px',
                height: '32px',
                borderRadius: '50%',
                background: 'linear-gradient(135deg, #6366f1, #a855f7)',
                color: 'white',
                fontWeight: 700,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: '0.85rem',
              }}
            >
              {user.displayName?.charAt(0)?.toUpperCase() || 'U'}
            </div>
            <span style={{ fontSize: '0.875rem', fontWeight: 600 }}>{user.displayName}</span>
          </Link>

          <button
            onClick={() => {
              logout();
              navigate('/login');
            }}
            className="btn btn-secondary"
            style={{ padding: '0.45rem 0.85rem', fontSize: '0.85rem' }}
            title="Log out"
          >
            <LogOut size={16} />
          </button>
        </div>
      </div>
    </nav>
  );
};
