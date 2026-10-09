import React, { useState, useEffect } from 'react';
import { api } from '../api/client';
import { Bell, CheckCheck, Clock } from 'lucide-react';

export const NotificationsPage: React.FC = () => {
  const [notifications, setNotifications] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const loadNotifications = async () => {
    setIsLoading(true);
    const res = await api.get('/notifications');
    setIsLoading(false);
    if (res.success && res.data) {
      setNotifications(res.data);
    }
  };

  useEffect(() => {
    loadNotifications();
  }, []);

  const handleMarkAsRead = async (id: string) => {
    const res = await api.post(`/notifications/${id}/read`);
    if (res.success) {
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      );
    }
  };

  const handleMarkAllAsRead = async () => {
    const res = await api.post('/notifications/read-all');
    if (res.success) {
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    }
  };

  return (
    <div className="container" style={{ maxWidth: '780px' }}>
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: '2rem',
          flexWrap: 'wrap',
          gap: '1rem',
        }}
      >
        <div>
          <h1 style={{ fontSize: '1.85rem', marginBottom: '0.35rem' }}>Notifications</h1>
          <p style={{ color: 'var(--text-muted)' }}>Stay updated with meeting invites and room updates</p>
        </div>

        {notifications.some((n) => !n.isRead) && (
          <button onClick={handleMarkAllAsRead} className="btn btn-secondary">
            <CheckCheck size={18} /> Mark All as Read
          </button>
        )}
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
        {notifications.length === 0 ? (
          <div className="card" style={{ textAlign: 'center', padding: '3.5rem', color: 'var(--text-muted)' }}>
            <Bell size={40} style={{ margin: '0 auto 1rem', opacity: 0.5 }} />
            <p>You have no notifications at this time.</p>
          </div>
        ) : (
          notifications.map((n) => (
            <div
              key={n.id}
              className="card"
              style={{
                display: 'flex',
                alignItems: 'flex-start',
                justifyContent: 'space-between',
                padding: '1.2rem',
                borderLeft: n.isRead ? '1px solid var(--border-subtle)' : '4px solid var(--accent-primary)',
                background: n.isRead ? 'rgba(30, 41, 59, 0.4)' : 'var(--bg-card)',
              }}
            >
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.35rem' }}>
                  <h3 style={{ fontSize: '1.05rem', color: n.isRead ? 'var(--text-muted)' : 'var(--text-main)' }}>
                    {n.title}
                  </h3>
                  {!n.isRead && (
                    <span
                      style={{
                        width: '8px',
                        height: '8px',
                        borderRadius: '50%',
                        background: 'var(--accent-primary)',
                      }}
                    />
                  )}
                </div>

                <p style={{ color: 'var(--text-muted)', fontSize: '0.875rem', marginBottom: '0.5rem' }}>
                  {n.message}
                </p>

                <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.75rem', color: 'var(--text-dark)' }}>
                  <Clock size={12} />
                  <span>{new Date(n.createdAt).toLocaleString()}</span>
                </div>
              </div>

              {!n.isRead && (
                <button
                  onClick={() => handleMarkAsRead(n.id)}
                  className="btn btn-secondary"
                  style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                >
                  Mark read
                </button>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
};
