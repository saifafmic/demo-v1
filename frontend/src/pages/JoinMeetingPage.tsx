import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../context/AuthContext';
import { Video, Lock, Shield, User as UserIcon } from 'lucide-react';

export const JoinMeetingPage: React.FC = () => {
  const { token } = useParams<{ token: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [meeting, setMeeting] = useState<any>(null);
  const [displayName, setDisplayName] = useState(user?.displayName || '');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [isJoining, setIsJoining] = useState(false);

  useEffect(() => {
    if (!token) return;

    api.get(`/meetings/token/${token}`).then((res) => {
      setIsLoading(false);
      if (res.success && res.data) {
        setMeeting(res.data);
      } else {
        // Also check if token is a direct meeting GUID
        api.get(`/meetings/${token}`).then((directRes) => {
          if (directRes.success && directRes.data) {
            setMeeting(directRes.data);
          } else {
            setError(res.message || 'Meeting not found or link has expired.');
          }
        });
      }
    });
  }, [token]);

  useEffect(() => {
    if (user?.displayName && !displayName) {
      setDisplayName(user.displayName);
    }
  }, [user]);

  const handleJoin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!displayName.trim()) {
      setError('Please provide a display name');
      return;
    }

    if (meeting?.isPasswordProtected && !password.trim()) {
      setError('This meeting requires a password.');
      return;
    }

    setIsJoining(true);
    setError('');

    // Pre-validate join credentials
    const joinRes = await api.post(`/meetings/${meeting.id}/join`, {
      displayName: displayName.trim(),
      password: password.trim() || undefined,
    });

    setIsJoining(false);

    if (joinRes.success) {
      navigate(`/room/${meeting.id}`);
    } else {
      setError(joinRes.message || 'Failed to join meeting.');
    }
  };

  if (isLoading) {
    return (
      <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <p style={{ color: 'var(--text-muted)' }}>Loading meeting details...</p>
      </div>
    );
  }

  return (
    <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '1.5rem' }}>
      <div
        className="card"
        style={{
          width: '100%',
          maxWidth: '460px',
          background: 'rgba(15, 23, 42, 0.85)',
          border: '1px solid var(--border-glow)',
          boxShadow: 'var(--shadow-lg)',
        }}
      >
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div
            style={{
              width: '56px',
              height: '56px',
              background: 'var(--accent-gradient)',
              borderRadius: 'var(--radius-lg)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              margin: '0 auto 1rem',
              boxShadow: 'var(--accent-glow)',
            }}
          >
            <Video size={28} color="white" />
          </div>
          <h1 style={{ fontSize: '1.65rem', marginBottom: '0.4rem' }}>
            {meeting?.title || 'Join Video Conference'}
          </h1>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>
            Hosted by {meeting?.hostDisplayName || 'Organizer'}
          </p>
        </div>

        {error && (
          <div
            style={{
              padding: '0.75rem 1rem',
              background: 'rgba(239, 68, 68, 0.15)',
              border: '1px solid rgba(239, 68, 68, 0.3)',
              borderRadius: 'var(--radius-sm)',
              color: '#fca5a5',
              fontSize: '0.875rem',
              marginBottom: '1.25rem',
            }}
          >
            {error}
          </div>
        )}

        {meeting && (
          <form onSubmit={handleJoin}>
            <div className="form-group">
              <label className="form-label">Your Name</label>
              <input
                type="text"
                className="form-input"
                placeholder="Enter your display name"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                required
              />
            </div>

            {meeting.isPasswordProtected && (
              <div className="form-group">
                <label className="form-label" style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                  <Lock size={14} color="#f59e0b" />
                  Meeting Password
                </label>
                <input
                  type="password"
                  className="form-input"
                  placeholder="Enter meeting password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
              </div>
            )}

            <button
              type="submit"
              disabled={isJoining}
              className="btn btn-primary"
              style={{ width: '100%', marginTop: '0.5rem', padding: '0.8rem' }}
            >
              <Video size={18} />
              {isJoining ? 'Joining...' : 'Join Meeting Now'}
            </button>
          </form>
        )}
      </div>
    </div>
  );
};
