import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { CreateMeetingModal } from '../components/CreateMeetingModal';
import { Plus, Search, Calendar, Copy, Check, Video, Trash2, Lock, Shield } from 'lucide-react';

export const MeetingsPage: React.FC = () => {
  const navigate = useNavigate();
  const [meetings, setMeetings] = useState<any[]>([]);
  const [filter, setFilter] = useState<'all' | 'live' | 'scheduled' | 'ended'>('all');
  const [search, setSearch] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const loadMeetings = async () => {
    const res = await api.get('/meetings');
    if (res.success && res.data) {
      setMeetings(res.data);
    }
  };

  useEffect(() => {
    loadMeetings();
  }, []);

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this meeting?')) {
      const res = await api.delete(`/meetings/${id}`);
      if (res.success) {
        setMeetings((prev) => prev.filter((m) => m.id !== id));
      }
    }
  };

  const handleCopyLink = (m: any) => {
    const link = m.meetingLink || `${window.location.origin}/join/${m.publicToken}`;
    navigator.clipboard.writeText(link);
    setCopiedId(m.id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const filtered = meetings.filter((m) => {
    const matchesSearch =
      m.title.toLowerCase().includes(search.toLowerCase()) ||
      (m.description && m.description.toLowerCase().includes(search.toLowerCase()));

    if (!matchesSearch) return false;

    if (filter === 'live') return m.status === 2;
    if (filter === 'scheduled') return m.status === 0;
    if (filter === 'ended') return m.status === 3;
    return true;
  });

  return (
    <div className="container">
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '2rem', flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h1 style={{ fontSize: '1.85rem', marginBottom: '0.35rem' }}>Meeting Management</h1>
          <p style={{ color: 'var(--text-muted)' }}>Manage, join, and schedule your video conferences</p>
        </div>

        <button onClick={() => setIsModalOpen(true)} className="btn btn-primary">
          <Plus size={18} /> New Meeting
        </button>
      </div>

      {/* Filters and Search Bar */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: '1.5rem',
          flexWrap: 'wrap',
          gap: '1rem',
        }}
      >
        <div style={{ display: 'flex', gap: '0.5rem', background: 'rgba(15, 23, 42, 0.6)', padding: '0.35rem', borderRadius: 'var(--radius-md)' }}>
          {(['all', 'live', 'scheduled', 'ended'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setFilter(tab)}
              className="btn"
              style={{
                padding: '0.45rem 1rem',
                fontSize: '0.85rem',
                background: filter === tab ? 'var(--accent-primary)' : 'transparent',
                color: filter === tab ? 'white' : 'var(--text-muted)',
              }}
            >
              {tab.charAt(0).toUpperCase() + tab.slice(1)}
            </button>
          ))}
        </div>

        <div style={{ position: 'relative', width: '300px' }}>
          <Search size={16} color="var(--text-muted)" style={{ position: 'absolute', left: '12px', top: '12px' }} />
          <input
            type="text"
            placeholder="Search meetings..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="form-input"
            style={{ paddingLeft: '2.2rem', fontSize: '0.85rem' }}
          />
        </div>
      </div>

      {/* Meetings List */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {filtered.length === 0 ? (
          <div className="card" style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
            <Calendar size={38} style={{ margin: '0 auto 1rem', opacity: 0.5 }} />
            <p>No meetings found matching your criteria.</p>
          </div>
        ) : (
          filtered.map((m) => (
            <div
              key={m.id}
              className="card"
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '1.25rem 1.5rem',
                flexWrap: 'wrap',
                gap: '1rem',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem' }}>
                <div
                  style={{
                    width: '46px',
                    height: '46px',
                    borderRadius: 'var(--radius-md)',
                    background:
                      m.status === 2
                        ? 'rgba(16, 185, 129, 0.2)'
                        : m.status === 0
                        ? 'rgba(56, 189, 248, 0.2)'
                        : 'rgba(148, 163, 184, 0.1)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: m.status === 2 ? '#10b981' : m.status === 0 ? '#38bdf8' : '#94a3b8',
                  }}
                >
                  <Video size={22} />
                </div>

                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.2rem' }}>
                    <h3 style={{ fontSize: '1.1rem' }}>{m.title}</h3>
                    {m.isPasswordProtected && (
                      <span title="Password protected"><Lock size={14} color="#f59e0b" /></span>
                    )}
                    {m.isWaitingRoomEnabled && (
                      <span title="Waiting room enabled"><Shield size={14} color="#a855f7" /></span>
                    )}
                    <span
                      className={`badge ${
                        m.status === 2 ? 'badge-live' : m.status === 0 ? 'badge-scheduled' : 'badge-ended'
                      }`}
                      style={{ fontSize: '0.65rem' }}
                    >
                      {m.status === 2 ? 'Live' : m.status === 0 ? 'Scheduled' : 'Ended'}
                    </span>
                  </div>

                  <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                    Host: {m.hostDisplayName} • {m.scheduledStartAt ? new Date(m.scheduledStartAt).toLocaleString() : 'Created ' + new Date(m.createdAt).toLocaleDateString()}
                  </p>
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                <button
                  onClick={() => handleCopyLink(m)}
                  className="btn btn-secondary"
                  style={{ padding: '0.55rem 0.85rem', fontSize: '0.85rem' }}
                  title="Copy link"
                >
                  {copiedId === m.id ? <Check size={16} color="#10b981" /> : <Copy size={16} />}
                  <span>{copiedId === m.id ? 'Copied' : 'Link'}</span>
                </button>

                <button
                  onClick={() => navigate(`/room/${m.id}`)}
                  className="btn btn-primary"
                  style={{ padding: '0.55rem 1.1rem', fontSize: '0.85rem' }}
                >
                  <Video size={16} />
                  <span>Join</span>
                </button>

                <button
                  onClick={() => handleDelete(m.id)}
                  className="btn btn-secondary"
                  style={{ padding: '0.55rem', color: '#ef4444' }}
                  title="Delete meeting"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          ))
        )}
      </div>

      <CreateMeetingModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onCreated={() => loadMeetings()}
      />
    </div>
  );
};
