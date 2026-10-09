import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { api } from '../api/client';
import { Shield, Users, Video, Clock, CheckCircle2, XCircle, BarChart3, Activity } from 'lucide-react';

export const AdminPage: React.FC = () => {
  const { isAdmin } = useAuth();
  const [stats, setStats] = useState<any>(null);
  const [users, setUsers] = useState<any[]>([]);
  const [meetings, setMeetings] = useState<any[]>([]);
  const [history, setHistory] = useState<any[]>([]);
  const [activeTab, setActiveTab] = useState<'stats' | 'users' | 'meetings' | 'history'>('stats');
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!isAdmin) return;

    const loadAdminData = async () => {
      setIsLoading(true);
      const [statsRes, usersRes, meetingsRes, historyRes] = await Promise.all([
        api.get('/admin/statistics'),
        api.get('/admin/users'),
        api.get('/admin/meetings'),
        api.get('/admin/history'),
      ]);

      if (statsRes.success) setStats(statsRes.data);
      if (usersRes.success) setUsers(usersRes.data);
      if (meetingsRes.success) setMeetings(meetingsRes.data);
      if (historyRes.success) setHistory(historyRes.data);
      setIsLoading(false);
    };

    loadAdminData();
  }, [isAdmin]);

  const handleToggleUser = async (userId: string, currentStatus: boolean) => {
    const action = currentStatus ? 'deactivate' : 'activate';
    const res = await api.put(`/admin/users/${userId}/${action}`);
    if (res.success) {
      setUsers((prev) =>
        prev.map((u) => (u.id === userId ? { ...u, isActive: !currentStatus } : u))
      );
    }
  };

  if (!isAdmin) {
    return (
      <div className="container" style={{ textAlign: 'center', padding: '4rem 1rem' }}>
        <Shield size={48} color="#ef4444" style={{ margin: '0 auto 1rem' }} />
        <h2>Access Restricted</h2>
        <p style={{ color: 'var(--text-muted)' }}>You must have Administrator privileges to view this portal.</p>
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ marginBottom: '2rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem', marginBottom: '0.35rem' }}>
          <div style={{ background: 'linear-gradient(135deg, #a855f7, #ec4899)', padding: '0.45rem', borderRadius: '10px', display: 'flex' }}>
            <Shield size={20} color="white" />
          </div>
          <h1 style={{ fontSize: '1.85rem' }}>Administration Portal</h1>
        </div>
        <p style={{ color: 'var(--text-muted)' }}>Platform monitoring, user management, and system-wide audits</p>
      </div>

      {/* Admin Tabs */}
      <div
        style={{
          display: 'flex',
          gap: '0.5rem',
          background: 'rgba(15, 23, 42, 0.6)',
          padding: '0.35rem',
          borderRadius: 'var(--radius-md)',
          marginBottom: '2rem',
          width: 'fit-content',
        }}
      >
        <button
          onClick={() => setActiveTab('stats')}
          className="btn"
          style={{
            padding: '0.5rem 1.25rem',
            background: activeTab === 'stats' ? 'var(--accent-primary)' : 'transparent',
            color: activeTab === 'stats' ? 'white' : 'var(--text-muted)',
          }}
        >
          <BarChart3 size={16} /> Overview
        </button>
        <button
          onClick={() => setActiveTab('users')}
          className="btn"
          style={{
            padding: '0.5rem 1.25rem',
            background: activeTab === 'users' ? 'var(--accent-primary)' : 'transparent',
            color: activeTab === 'users' ? 'white' : 'var(--text-muted)',
          }}
        >
          <Users size={16} /> Users ({users.length})
        </button>
        <button
          onClick={() => setActiveTab('meetings')}
          className="btn"
          style={{
            padding: '0.5rem 1.25rem',
            background: activeTab === 'meetings' ? 'var(--accent-primary)' : 'transparent',
            color: activeTab === 'meetings' ? 'white' : 'var(--text-muted)',
          }}
        >
          <Video size={16} /> All Meetings ({meetings.length})
        </button>
        <button
          onClick={() => setActiveTab('history')}
          className="btn"
          style={{
            padding: '0.5rem 1.25rem',
            background: activeTab === 'history' ? 'var(--accent-primary)' : 'transparent',
            color: activeTab === 'history' ? 'white' : 'var(--text-muted)',
          }}
        >
          <Activity size={16} /> Audit History
        </button>
      </div>

      {/* Overview Statistics Tab */}
      {activeTab === 'stats' && stats && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1.25rem' }}>
          <div className="card">
            <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 600 }}>Total Registered Users</span>
            <h2 style={{ fontSize: '2.4rem', marginTop: '0.5rem', color: '#6366f1' }}>{stats.totalUsers}</h2>
            <p style={{ fontSize: '0.8rem', color: '#10b981', marginTop: '0.2rem' }}>{stats.activeUsers} Active Accounts</p>
          </div>

          <div className="card">
            <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 600 }}>Total Conferences</span>
            <h2 style={{ fontSize: '2.4rem', marginTop: '0.5rem', color: '#38bdf8' }}>{stats.totalMeetings}</h2>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.2rem' }}>Lifetime Created Rooms</p>
          </div>

          <div className="card">
            <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 600 }}>Active Live Rooms</span>
            <h2 style={{ fontSize: '2.4rem', marginTop: '0.5rem', color: '#10b981' }}>{stats.liveMeetings}</h2>
            <p style={{ fontSize: '0.8rem', color: '#10b981', marginTop: '0.2rem' }}>Currently Transmitting Media</p>
          </div>

          <div className="card">
            <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 600 }}>Recorded Conferences</span>
            <h2 style={{ fontSize: '2.4rem', marginTop: '0.5rem', color: '#a855f7' }}>{stats.totalRecordings}</h2>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.2rem' }}>Archived Recordings</p>
          </div>
        </div>
      )}

      {/* Users Management Tab */}
      {activeTab === 'users' && (
        <div className="card" style={{ overflowX: 'auto', padding: '1rem' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)' }}>
                <th style={{ padding: '0.75rem 1rem' }}>Name</th>
                <th style={{ padding: '0.75rem 1rem' }}>Email</th>
                <th style={{ padding: '0.75rem 1rem' }}>Role</th>
                <th style={{ padding: '0.75rem 1rem' }}>Status</th>
                <th style={{ padding: '0.75rem 1rem' }}>Registered</th>
                <th style={{ padding: '0.75rem 1rem', textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id} style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                  <td style={{ padding: '0.85rem 1rem', fontWeight: 600 }}>{u.displayName}</td>
                  <td style={{ padding: '0.85rem 1rem', color: 'var(--text-muted)' }}>{u.email}</td>
                  <td style={{ padding: '0.85rem 1rem' }}>
                    <span className="badge" style={{ background: u.role === 1 || u.role === 'Admin' ? 'rgba(168, 85, 247, 0.2)' : 'rgba(56, 189, 248, 0.2)' }}>
                      {u.role === 1 || u.role === 'Admin' ? 'Admin' : 'User'}
                    </span>
                  </td>
                  <td style={{ padding: '0.85rem 1rem' }}>
                    <span style={{ color: u.isActive ? '#10b981' : '#ef4444', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '4px' }}>
                      {u.isActive ? <CheckCircle2 size={15} /> : <XCircle size={15} />}
                      {u.isActive ? 'Active' : 'Deactivated'}
                    </span>
                  </td>
                  <td style={{ padding: '0.85rem 1rem', color: 'var(--text-dark)' }}>
                    {new Date(u.createdAt).toLocaleDateString()}
                  </td>
                  <td style={{ padding: '0.85rem 1rem', textAlign: 'right' }}>
                    <button
                      onClick={() => handleToggleUser(u.id, u.isActive)}
                      className={`btn ${u.isActive ? 'btn-danger' : 'btn-secondary'}`}
                      style={{ padding: '0.35rem 0.75rem', fontSize: '0.75rem' }}
                    >
                      {u.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Meetings Tab */}
      {activeTab === 'meetings' && (
        <div className="card" style={{ overflowX: 'auto', padding: '1rem' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)' }}>
                <th style={{ padding: '0.75rem 1rem' }}>Title</th>
                <th style={{ padding: '0.75rem 1rem' }}>Room Name</th>
                <th style={{ padding: '0.75rem 1rem' }}>Host</th>
                <th style={{ padding: '0.75rem 1rem' }}>Status</th>
                <th style={{ padding: '0.75rem 1rem' }}>Created</th>
              </tr>
            </thead>
            <tbody>
              {meetings.map((m) => (
                <tr key={m.id} style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                  <td style={{ padding: '0.85rem 1rem', fontWeight: 600 }}>{m.title}</td>
                  <td style={{ padding: '0.85rem 1rem', fontFamily: 'var(--font-mono)', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                    {m.roomName}
                  </td>
                  <td style={{ padding: '0.85rem 1rem' }}>{m.hostDisplayName}</td>
                  <td style={{ padding: '0.85rem 1rem' }}>
                    <span className={`badge ${m.status === 2 ? 'badge-live' : m.status === 0 ? 'badge-scheduled' : 'badge-ended'}`}>
                      {m.status === 2 ? 'Live' : m.status === 0 ? 'Scheduled' : 'Ended'}
                    </span>
                  </td>
                  <td style={{ padding: '0.85rem 1rem', color: 'var(--text-dark)' }}>
                    {new Date(m.createdAt).toLocaleDateString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Audit History Tab */}
      {activeTab === 'history' && (
        <div className="card" style={{ overflowX: 'auto', padding: '1rem' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.875rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)' }}>
                <th style={{ padding: '0.75rem 1rem' }}>Timestamp</th>
                <th style={{ padding: '0.75rem 1rem' }}>Meeting</th>
                <th style={{ padding: '0.75rem 1rem' }}>Action</th>
                <th style={{ padding: '0.75rem 1rem' }}>Details</th>
              </tr>
            </thead>
            <tbody>
              {history.map((h) => (
                <tr key={h.id} style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                  <td style={{ padding: '0.75rem 1rem', color: 'var(--text-dark)', whiteSpace: 'nowrap' }}>
                    {new Date(h.timestamp).toLocaleString()}
                  </td>
                  <td style={{ padding: '0.75rem 1rem', fontWeight: 600 }}>{h.meetingTitle}</td>
                  <td style={{ padding: '0.75rem 1rem' }}>
                    <span style={{ color: 'var(--accent-primary)', fontWeight: 600 }}>{h.action}</span>
                  </td>
                  <td style={{ padding: '0.75rem 1rem', color: 'var(--text-muted)', fontSize: '0.8rem' }}>
                    {h.metadata || '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
