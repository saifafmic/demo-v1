import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { api } from '../api/client';
import { CreateMeetingModal } from '../components/CreateMeetingModal';
import { Video, Plus, Calendar, Link as LinkIcon, Search, Users, Copy, Check, Clock } from 'lucide-react';

export const DashboardPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [upcomingMeetings, setUpcomingMeetings] = useState<any[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [joinCode, setJoinCode] = useState('');
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');

  const loadMeetings = async () => {
    const res = await api.get('/meetings/upcoming');
    if (res.success && res.data) {
      setUpcomingMeetings(res.data);
    }
  };

  useEffect(() => {
    loadMeetings();
  }, []);

  const handleJoinWithCode = (e: React.FormEvent) => {
    e.preventDefault();
    if (!joinCode.trim()) return;
    const cleanCode = joinCode.trim().replace(/^https?:\/\/.*\/join\//, '');
    navigate(`/join/${cleanCode}`);
  };

  const handleCopyLink = (meeting: any) => {
    const link = meeting.meetingLink || `${window.location.origin}/join/${meeting.publicToken}`;
    navigator.clipboard.writeText(link);
    setCopiedId(meeting.id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const filteredMeetings = upcomingMeetings.filter((m) =>
    m.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
    (m.description && m.description.toLowerCase().includes(searchQuery.toLowerCase()))
  );

  return (
    <div className="container">
      {/* Welcome Hero Banner */}
      <div
        className="card"
        style={{
          background: 'linear-gradient(135deg, rgba(30, 41, 59, 0.9) 0%, rgba(15, 23, 42, 0.95) 100%)',
          border: '1px solid var(--border-glow)',
          padding: '2.5rem',
          marginBottom: '2.5rem',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '1.5rem',
        }}
      >
        <div>
          <span className="badge badge-live" style={{ marginBottom: '0.75rem' }}>
            <span className="pulse-dot" style={{ background: '#10b981' }} /> Ready to connect
          </span>
          <h1 style={{ fontSize: '2.2rem', marginBottom: '0.5rem' }}>
            Hello, <span className="brand-gradient">{user?.displayName || 'User'}</span>!
          </h1>
          <p style={{ color: 'var(--text-muted)', fontSize: '1.05rem', maxWidth: '520px' }}>
            Start high-definition, low-latency video conferences powered by LiveKit WebRTC and ASP.NET Core.
          </p>
        </div>

        {/* Quick Join Input Box */}
        <form
          onSubmit={handleJoinWithCode}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            background: 'rgba(15, 23, 42, 0.8)',
            padding: '0.4rem 0.5rem',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-subtle)',
            width: '100%',
            maxWidth: '380px',
          }}
        >
          <input
            type="text"
            placeholder="Enter meeting link or code..."
            value={joinCode}
            onChange={(e) => setJoinCode(e.target.value)}
            style={{
              background: 'transparent',
              border: 'none',
              color: 'white',
              padding: '0.5rem 0.75rem',
              outline: 'none',
              flex: 1,
              fontSize: '0.9rem',
            }}
          />
          <button type="submit" className="btn btn-primary" style={{ padding: '0.6rem 1rem', fontSize: '0.85rem' }}>
            Join
          </button>
        </form>
      </div>

      {/* Action Cards */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
          gap: '1.5rem',
          marginBottom: '2.5rem',
        }}
      >
        <div
          className="card"
          onClick={() => setIsModalOpen(true)}
          style={{ cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '1.25rem' }}
        >
          <div
            style={{
              width: '54px',
              height: '54px',
              borderRadius: 'var(--radius-md)',
              background: 'linear-gradient(135deg, #6366f1, #a855f7)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'white',
              boxShadow: 'var(--accent-glow)',
            }}
          >
            <Video size={26} />
          </div>
          <div>
            <h3 style={{ fontSize: '1.15rem', marginBottom: '0.2rem' }}>New Meeting</h3>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Instant room with 1-click link</p>
          </div>
        </div>

        <div
          className="card"
          onClick={() => setIsModalOpen(true)}
          style={{ cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '1.25rem' }}
        >
          <div
            style={{
              width: '54px',
              height: '54px',
              borderRadius: 'var(--radius-md)',
              background: 'linear-gradient(135deg, #0ea5e9, #38bdf8)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'white',
            }}
          >
            <Calendar size={26} />
          </div>
          <div>
            <h3 style={{ fontSize: '1.15rem', marginBottom: '0.2rem' }}>Schedule</h3>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Plan future conferences</p>
          </div>
        </div>
      </div>

      {/* Upcoming Meetings Section */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1.25rem' }}>
        <div>
          <h2 style={{ fontSize: '1.4rem' }}>Upcoming & Active Meetings</h2>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.875rem' }}>Your scheduled and live rooms</p>
        </div>

        <div style={{ position: 'relative', width: '260px' }}>
          <Search size={16} color="var(--text-muted)" style={{ position: 'absolute', left: '12px', top: '12px' }} />
          <input
            type="text"
            placeholder="Search meetings..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="form-input"
            style={{ paddingLeft: '2.2rem', fontSize: '0.85rem' }}
          />
        </div>
      </div>

      {filteredMeetings.length === 0 ? (
        <div
          className="card"
          style={{
            textAlign: 'center',
            padding: '3rem 1.5rem',
            color: 'var(--text-muted)',
          }}
        >
          <Calendar size={42} style={{ margin: '0 auto 1rem', opacity: 0.5 }} />
          <h3 style={{ fontSize: '1.15rem', color: 'var(--text-main)', marginBottom: '0.4rem' }}>
            No Upcoming Meetings
          </h3>
          <p style={{ fontSize: '0.9rem', marginBottom: '1.5rem' }}>
            You don't have any active or scheduled conferences right now.
          </p>
          <button onClick={() => setIsModalOpen(true)} className="btn btn-primary">
            <Plus size={16} /> Create Your First Meeting
          </button>
        </div>
      ) : (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(360px, 1fr))',
            gap: '1.25rem',
          }}
        >
          {filteredMeetings.map((meeting) => (
            <div key={meeting.id} className="card" style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between' }}>
                <div>
                  <span
                    className={`badge ${meeting.status === 2 ? 'badge-live' : 'badge-scheduled'}`}
                    style={{ marginBottom: '0.5rem' }}
                  >
                    {meeting.status === 2 ? 'Live Now' : 'Scheduled'}
                  </span>
                  <h3 style={{ fontSize: '1.2rem' }}>{meeting.title}</h3>
                </div>

                <button
                  onClick={() => handleCopyLink(meeting)}
                  className="btn btn-secondary"
                  style={{ padding: '0.45rem', borderRadius: '50%' }}
                  title="Copy Meeting Link"
                >
                  {copiedId === meeting.id ? <Check size={16} color="#10b981" /> : <Copy size={16} />}
                </button>
              </div>

              {meeting.description && (
                <p style={{ color: 'var(--text-muted)', fontSize: '0.875rem' }}>{meeting.description}</p>
              )}

              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '1.5rem',
                  fontSize: '0.8rem',
                  color: 'var(--text-muted)',
                  borderTop: '1px solid var(--border-subtle)',
                  paddingTop: '0.75rem',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                  <Clock size={14} />
                  <span>
                    {meeting.scheduledStartAt
                      ? new Date(meeting.scheduledStartAt).toLocaleDateString(undefined, {
                          month: 'short',
                          day: 'numeric',
                          hour: '2-digit',
                          minute: '2-digit',
                        })
                      : 'Instant Room'}
                  </span>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                  <Users size={14} />
                  <span>{meeting.activeParticipantCount || 1} participant</span>
                </div>
              </div>

              <button
                onClick={() => navigate(`/room/${meeting.id}`)}
                className="btn btn-primary"
                style={{ width: '100%', marginTop: 'auto' }}
              >
                <Video size={16} /> Enter Meeting
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Modal Dialog */}
      <CreateMeetingModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onCreated={(newMeeting) => {
          loadMeetings();
          navigate(`/room/${newMeeting.id}`);
        }}
      />
    </div>
  );
};
