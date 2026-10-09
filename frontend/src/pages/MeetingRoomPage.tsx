import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Room,
  RoomEvent,
  VideoPresets,
  Track,
  Participant,
  RemoteParticipant,
  LocalParticipant,
  TrackPublication,
  ConnectionState,
} from 'livekit-client';
import { api } from '../api/client';
import { useAuth } from '../context/AuthContext';
import { VideoTile } from '../components/VideoTile';
import { Whiteboard } from '../components/Whiteboard';
import {
  Mic,
  MicOff,
  Video as VideoIcon,
  VideoOff,
  Monitor,
  Edit,
  MessageSquare,
  Users,
  PhoneOff,
  Shield,
  Copy,
  Check,
  Subtitles,
  Send,
  MoreVertical,
  UserX,
  Ban,
  ArrowUpCircle,
  ArrowDownCircle,
  Radio,
} from 'lucide-react';

interface ChatMessage {
  id?: string;
  senderName: string;
  message: string;
  sentAt: string;
  isLocal?: boolean;
}

export const MeetingRoomPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  // Room & State
  const [room, setRoom] = useState<Room | null>(null);
  const [meetingData, setMeetingData] = useState<any>(null);
  const [connectionState, setConnectionState] = useState<string>('Connecting');
  const [isHost, setIsHost] = useState(false);
  const [error, setError] = useState('');
  const [deviceWarning, setDeviceWarning] = useState<string>('');

  // Media Controls
  const [isMicOn, setIsMicOn] = useState(false);
  const [isCameraOn, setIsCameraOn] = useState(false);
  const [isScreenSharing, setIsScreenSharing] = useState(false);

  // Participants & Tracks
  const [participants, setParticipants] = useState<Participant[]>([]);
  const [activeSpeakers, setActiveSpeakers] = useState<string[]>([]);
  const [tracks, setTracks] = useState<TrackPublication[]>([]);

  // Features Toggles
  const [isWhiteboardOpen, setIsWhiteboardOpen] = useState(false);
  const [isCaptionsOn, setIsCaptionsOn] = useState(false);
  const [captionText, setCaptionText] = useState('');
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);
  const [activeTab, setActiveTab] = useState<'participants' | 'chat' | 'waiting'>('chat');

  // Real-time Chat
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [unreadChatCount, setUnreadChatCount] = useState(0);

  // Tools & UI
  const [copiedLink, setCopiedLink] = useState(false);
  const recognitionRef = useRef<any>(null);
  const chatScrollRef = useRef<HTMLDivElement | null>(null);

  // 1. Join Meeting & Connect to LiveKit Room
  useEffect(() => {
    let currentRoom: Room | null = null;

    const joinAndConnect = async () => {
      try {
        setError('');
        setConnectionState('Connecting');

        // Step 1: Call ASP.NET Core API to validate permissions and get LiveKit JWT
        const joinRes = await api.post(`/meetings/${id}/join`, {
          displayName: user?.displayName || 'Guest Participant',
        });

        if (!joinRes.success || !joinRes.data) {
          setError(joinRes.message || 'Failed to join meeting.');
          setConnectionState('Disconnected');
          return;
        }

        const { serverUrl, token, roomName, isHost: hostStatus, title } = joinRes.data;
        setIsHost(hostStatus);
        setMeetingData({ title, roomName });

        // Step 2: Initialize LiveKit Room instance
        const lkRoom = new Room({
          adaptiveStream: true,
          dynacast: true,
        });

        currentRoom = lkRoom;
        setRoom(lkRoom);

        // Step 3: Register LiveKit event listeners
        lkRoom.on(RoomEvent.ConnectionStateChanged, (state: ConnectionState) => {
          setConnectionState(state.toString());
        });

        const updateParticipantsList = () => {
          const all: Participant[] = [];
          if (lkRoom.localParticipant) {
            all.push(lkRoom.localParticipant);
            setIsScreenSharing(lkRoom.localParticipant.isScreenShareEnabled);
            setIsCameraOn(lkRoom.localParticipant.isCameraEnabled);
            setIsMicOn(lkRoom.localParticipant.isMicrophoneEnabled);
          }
          lkRoom.remoteParticipants.forEach((p) => {
            all.push(p);
          });
          setParticipants([...all]);
        };

        lkRoom.on(RoomEvent.ParticipantConnected, updateParticipantsList);
        lkRoom.on(RoomEvent.ParticipantDisconnected, updateParticipantsList);

        lkRoom.on(RoomEvent.ActiveSpeakersChanged, (speakers: Participant[]) => {
          setActiveSpeakers(speakers.map((s) => s.identity));
        });

        // Comprehensive track events for real-time video, audio & screen sharing
        lkRoom.on(RoomEvent.TrackSubscribed, updateParticipantsList);
        lkRoom.on(RoomEvent.TrackUnsubscribed, updateParticipantsList);
        lkRoom.on(RoomEvent.TrackPublished, updateParticipantsList);
        lkRoom.on(RoomEvent.TrackUnpublished, updateParticipantsList);
        lkRoom.on(RoomEvent.TrackMuted, updateParticipantsList);
        lkRoom.on(RoomEvent.TrackUnmuted, updateParticipantsList);
        lkRoom.on(RoomEvent.LocalTrackPublished, updateParticipantsList);
        lkRoom.on(RoomEvent.LocalTrackUnpublished, updateParticipantsList);

        // Step 4: Handle LiveKit Real-Time Data Packets (Meeting Chat)
        lkRoom.on(RoomEvent.DataReceived, (payload: Uint8Array, participant?: RemoteParticipant) => {
          try {
            const raw = new TextDecoder().decode(payload);
            const data = JSON.parse(raw);

            if (data.type === 'chat') {
              const incomingMsg: ChatMessage = {
                senderName: participant?.name || 'Participant',
                message: data.message,
                sentAt: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
                isLocal: false,
              };
              setMessages((prev) => [...prev, incomingMsg]);
              if (!isSidebarOpen || activeTab !== 'chat') {
                setUnreadChatCount((c) => c + 1);
              }
            }
          } catch {
            // Ignore non-chat packets (e.g. whiteboard actions)
          }
        });

        // Step 5: Connect to LiveKit Real-Time WebRTC Server
        await lkRoom.connect(serverUrl, token);
        setConnectionState('Connected');

        // Step 6: Gracefully attempt to publish local audio and video tracks
        try {
          await lkRoom.localParticipant.setMicrophoneEnabled(true);
          setIsMicOn(true);
        } catch (micErr: any) {
          console.warn('Microphone access failed:', micErr);
          setIsMicOn(false);
          setDeviceWarning('Microphone could not be accessed automatically. You can unmute when ready.');
        }

        // Attempt camera start with multi-device fallback
        await startCamera(lkRoom);

        updateParticipantsList();

        // Load historical chat messages from DB
        api.get(`/meetings/${id}/messages`).then((res) => {
          if (res.success && res.data) {
            const loaded = res.data.map((m: any) => ({
              id: m.id,
              senderName: m.senderName,
              message: m.message,
              sentAt: new Date(m.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
              isLocal: m.senderName === (user?.displayName || 'You'),
            }));
            setMessages(loaded);
          }
        });
      } catch (err: any) {
        setError(err?.message || 'Error establishing WebRTC real-time connection.');
        setConnectionState('Disconnected');
      }
    };

    joinAndConnect();

    return () => {
      if (currentRoom) {
        currentRoom.disconnect();
      }
    };
  }, [id, user?.displayName]);

  // Scroll chat down when new messages arrive
  useEffect(() => {
    if (chatScrollRef.current) {
      chatScrollRef.current.scrollTop = chatScrollRef.current.scrollHeight;
    }
  }, [messages]);

  // Helper to start camera with device enumeration fallback
  const startCamera = async (targetRoom: Room) => {
    try {
      await targetRoom.localParticipant.setCameraEnabled(true);
      setIsCameraOn(true);
      setDeviceWarning('');
      return true;
    } catch (err: any) {
      console.warn('Direct camera start failed, scanning devices...', err);
    }

    try {
      const devices = await navigator.mediaDevices.enumerateDevices();
      const videoDevices = devices.filter((d) => d.kind === 'videoinput');
      // Filter out Windows Hello / IR facial recognition cameras
      const nonIr = videoDevices.filter(
        (d) =>
          !d.label.toLowerCase().includes('ir') &&
          !d.label.toLowerCase().includes('hello')
      );
      const candidates = nonIr.length > 0 ? nonIr : videoDevices;

      for (const dev of candidates) {
        try {
          await targetRoom.localParticipant.setCameraEnabled(true, {
            deviceId: dev.deviceId,
          });
          setIsCameraOn(true);
          setDeviceWarning('');
          return true;
        } catch {
          // Continue trying next camera
        }
      }
    } catch (enumErr) {
      console.warn('Camera device enumeration error:', enumErr);
    }

    setIsCameraOn(false);
    setDeviceWarning(
      'Could not start video source. Check if another application (e.g. Lenovo Vantage Camera Privacy Mode, Windows Camera, or Zoom) is currently using your webcam, or open the physical camera shutter.'
    );
    return false;
  };

  // 2. Microphone Toggle
  const toggleMic = async () => {
    if (!room) return;
    const nextState = !isMicOn;
    try {
      await room.localParticipant.setMicrophoneEnabled(nextState);
      setIsMicOn(nextState);
      setDeviceWarning('');
    } catch (err: any) {
      console.warn('Toggle mic error:', err);
      setIsMicOn(false);
      setDeviceWarning('Could not access microphone: ' + (err?.message || 'Device unavailable'));
    }
  };

  // 3. Camera Toggle
  const toggleCamera = async () => {
    if (!room) return;
    if (isCameraOn) {
      await room.localParticipant.setCameraEnabled(false);
      setIsCameraOn(false);
      setDeviceWarning('');
    } else {
      await startCamera(room);
    }
  };

  // 4. Screen Sharing Toggle
  const toggleScreenShare = async () => {
    if (!room) return;
    try {
      const nextState = !isScreenSharing;
      await room.localParticipant.setScreenShareEnabled(nextState);
      setIsScreenSharing(nextState);
    } catch (e: any) {
      console.warn('Screen share toggle cancelled or error:', e);
      setIsScreenSharing(false);
    }
  };

  // 5. Live Captions (Speech Recognition)
  const toggleCaptions = () => {
    const SpeechRecognition =
      (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;

    if (!SpeechRecognition) {
      alert('Live speech recognition is not supported in this browser. Please use Chrome or Edge.');
      return;
    }

    if (!isCaptionsOn) {
      try {
        const recognition = new SpeechRecognition();
        recognition.continuous = true;
        recognition.interimResults = true;
        recognition.lang = 'en-US';

        recognition.onresult = (event: any) => {
          let interim = '';
          for (let i = event.resultIndex; i < event.results.length; ++i) {
            interim += event.results[i][0].transcript;
          }
          setCaptionText(interim);
        };

        recognition.onerror = () => {
          setCaptionText('');
        };

        recognition.start();
        recognitionRef.current = recognition;
        setIsCaptionsOn(true);
      } catch (e) {
        console.error(e);
      }
    } else {
      if (recognitionRef.current) {
        recognitionRef.current.stop();
      }
      setIsCaptionsOn(false);
      setCaptionText('');
    }
  };

  // 6. Send Real-Time Meeting Chat
  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newMessage.trim() || !room) return;

    const messageText = newMessage.trim();
    const sender = user?.displayName || room.localParticipant.name || 'You';

    // Broadcast over LiveKit Data Channel
    try {
      const payload = JSON.stringify({
        type: 'chat',
        message: messageText,
      });
      room.localParticipant.publishData(new TextEncoder().encode(payload), { reliable: true });

      // Add to local state
      setMessages((prev) => [
        ...prev,
        {
          senderName: sender,
          message: messageText,
          sentAt: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
          isLocal: true,
        },
      ]);
      setNewMessage('');

      // Persist to SQL Server via API
      api.post(`/meetings/${id}/messages`, {
        message: messageText,
        senderName: sender,
      });
    } catch (err) {
      console.error('Failed to send chat message', err);
    }
  };

  // 7. Copy Meeting Link
  const handleCopyLink = () => {
    const url = `${window.location.origin}/join/${meetingData?.publicToken || id}`;
    navigator.clipboard.writeText(url);
    setCopiedLink(true);
    setTimeout(() => setCopiedLink(false), 2000);
  };

  // 8. Leave Meeting
  const handleLeaveMeeting = async () => {
    if (room) {
      room.disconnect();
    }
    await api.post(`/meetings/${id}/leave`);
    navigate('/dashboard');
  };

  // 9. End Meeting for All (Host/CoHost/Admin)
  const handleEndMeeting = async () => {
    if (confirm('Are you sure you want to end this meeting for all participants?')) {
      if (room) {
        room.disconnect();
      }
      await api.post(`/meetings/${id}/end`);
      navigate('/dashboard');
    }
  };

  // Moderator actions
  const handleRemoveParticipant = async (participantId: string) => {
    if (confirm('Remove this participant from the meeting?')) {
      await api.post(`/meetings/${id}/participants/${participantId}/remove`);
    }
  };

  const handleBanParticipant = async (participantId: string) => {
    if (confirm('Ban this participant from re-entering?')) {
      await api.post(`/meetings/${id}/participants/${participantId}/ban`);
    }
  };

  // Screen share tiles collection
  const screenShareTiles: Array<{
    id: string;
    displayName: string;
    isLocal: boolean;
    publication: TrackPublication | undefined;
  }> = [];

  participants.forEach((p) => {
    const isLocal = p instanceof LocalParticipant;
    const screenPub = p.getTrackPublication(Track.Source.ScreenShare);
    if (screenPub && (screenPub.track || (isLocal && isScreenSharing))) {
      screenShareTiles.push({
        id: `${p.identity}-screen`,
        displayName: `${p.name || 'Participant'}'s Screen`,
        isLocal,
        publication: screenPub,
      });
    }
  });

  const hasActiveScreenShare = screenShareTiles.length > 0;

  // Calculate dynamic video grid class
  const totalTiles = participants.length || 1;
  const gridClass =
    totalTiles === 1
      ? 'grid-1'
      : totalTiles === 2
      ? 'grid-2'
      : totalTiles <= 4
      ? 'grid-4'
      : 'grid-6';

  return (
    <div className="meeting-layout">
      {/* Main Stage */}
      <div className="meeting-main">
        {/* Meeting Header */}
        <div className="meeting-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <h2 style={{ fontSize: '1.15rem', fontWeight: 700 }}>
              {meetingData?.title || 'Video Conference'}
            </h2>

            {/* Connection status pill */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.4rem',
                fontSize: '0.75rem',
                fontWeight: 600,
                padding: '0.2rem 0.65rem',
                borderRadius: 'var(--radius-full)',
                background:
                  connectionState === 'Connected'
                    ? 'rgba(16, 185, 129, 0.15)'
                    : 'rgba(239, 68, 68, 0.15)',
                color: connectionState === 'Connected' ? '#10b981' : '#f87171',
                border: `1px solid ${
                  connectionState === 'Connected'
                    ? 'rgba(16, 185, 129, 0.3)'
                    : 'rgba(239, 68, 68, 0.3)'
                }`,
              }}
            >
              <span
                className="pulse-dot"
                style={{
                  background: connectionState === 'Connected' ? '#10b981' : '#ef4444',
                }}
              />
              <span>{connectionState}</span>
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <button
              onClick={handleCopyLink}
              className="btn btn-secondary"
              style={{ padding: '0.45rem 0.85rem', fontSize: '0.85rem' }}
            >
              {copiedLink ? <Check size={16} color="#10b981" /> : <Copy size={16} />}
              <span>{copiedLink ? 'Copied' : 'Share Link'}</span>
            </button>
          </div>
        </div>

        {/* Warning notification banner (e.g. camera in use) */}
        {deviceWarning && (
          <div
            style={{
              background: 'rgba(245, 158, 11, 0.15)',
              border: '1px solid rgba(245, 158, 11, 0.35)',
              color: '#fcd34d',
              padding: '0.65rem 1.25rem',
              borderRadius: 'var(--radius-md)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              margin: '0.75rem 1.5rem 0',
              fontSize: '0.85rem',
              gap: '0.75rem',
              boxShadow: '0 2px 8px rgba(0,0,0,0.2)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <span>⚠️</span>
              <span>{deviceWarning}</span>
            </div>
            <button
              onClick={() => setDeviceWarning('')}
              style={{
                background: 'none',
                border: 'none',
                color: '#fcd34d',
                cursor: 'pointer',
                fontSize: '1rem',
                padding: '0 0.25rem',
              }}
            >
              ✕
            </button>
          </div>
        )}

        {/* Video Grid Stage */}
        <div className="meeting-stage">
          {error && (
            <div
              className="card"
              style={{
                background: 'rgba(239, 68, 68, 0.2)',
                border: '1px solid rgba(239, 68, 68, 0.4)',
                textAlign: 'center',
                padding: '2rem',
                maxWidth: '480px',
              }}
            >
              <h3 style={{ color: '#fca5a5', marginBottom: '0.5rem' }}>Connection Failed</h3>
              <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginBottom: '1.5rem' }}>
                {error}
              </p>
              <button onClick={() => navigate('/dashboard')} className="btn btn-primary">
                Return to Dashboard
              </button>
            </div>
          )}

          {!error && (
            <>
              {hasActiveScreenShare ? (
                <div className="stage-screen-layout">
                  <div className="screen-share-main">
                    {screenShareTiles.map((st) => (
                      <VideoTile
                        key={st.id}
                        displayName={st.displayName}
                        isLocal={st.isLocal}
                        isScreenShare={true}
                        isVideoMuted={false}
                        isAudioMuted={true}
                        videoTrackPublication={st.publication}
                      />
                    ))}
                  </div>

                  <div className="screen-share-filmstrip">
                    {participants.map((p) => {
                      const isLocal = p instanceof LocalParticipant;
                      const videoTrackPub = p.getTrackPublication(Track.Source.Camera);
                      const audioTrackPub = p.getTrackPublication(Track.Source.Microphone);
                      const isSpeaking = activeSpeakers.includes(p.identity);

                      return (
                        <div key={p.identity} className="filmstrip-tile">
                          <VideoTile
                            displayName={p.name || 'Participant'}
                            isLocal={isLocal}
                            isHost={isHost && isLocal}
                            isSpeaking={isSpeaking}
                            isAudioMuted={!p.isMicrophoneEnabled}
                            isVideoMuted={!p.isCameraEnabled}
                            videoTrackPublication={videoTrackPub}
                            audioTrackPublication={audioTrackPub}
                          />
                        </div>
                      );
                    })}
                  </div>
                </div>
              ) : (
                <div className={`video-grid ${gridClass}`}>
                  {participants.map((p) => {
                    const isLocal = p instanceof LocalParticipant;
                    const videoTrackPub = p.getTrackPublication(Track.Source.Camera);
                    const audioTrackPub = p.getTrackPublication(Track.Source.Microphone);
                    const isSpeaking = activeSpeakers.includes(p.identity);

                    return (
                      <VideoTile
                        key={p.identity}
                        displayName={p.name || 'Participant'}
                        isLocal={isLocal}
                        isHost={isHost && isLocal}
                        isSpeaking={isSpeaking}
                        isAudioMuted={!p.isMicrophoneEnabled}
                        isVideoMuted={!p.isCameraEnabled}
                        videoTrackPublication={videoTrackPub}
                        audioTrackPublication={audioTrackPub}
                      />
                    );
                  })}
                </div>
              )}
            </>
          )}

          {/* Interactive Whiteboard Canvas Overlay */}
          {isWhiteboardOpen && (
            <Whiteboard room={room} onClose={() => setIsWhiteboardOpen(false)} />
          )}

          {/* Live Captions Transcript Banner */}
          {isCaptionsOn && captionText && (
            <div className="captions-banner">
              <strong>Live Transcript:</strong> {captionText}
            </div>
          )}
        </div>

        {/* Bottom Meeting Controls Bar */}
        <div className="meeting-controls">
          <button
            onClick={toggleMic}
            className={`btn btn-icon ${isMicOn ? 'btn-secondary' : 'btn-icon muted'}`}
            title={isMicOn ? 'Mute Microphone' : 'Unmute Microphone'}
          >
            {isMicOn ? <Mic size={20} /> : <MicOff size={20} />}
          </button>

          <button
            onClick={toggleCamera}
            className={`btn btn-icon ${isCameraOn ? 'btn-secondary' : 'btn-icon muted'}`}
            title={isCameraOn ? 'Turn Off Camera' : 'Turn On Camera'}
          >
            {isCameraOn ? <VideoIcon size={20} /> : <VideoOff size={20} />}
          </button>

          <button
            onClick={toggleScreenShare}
            className={`btn btn-icon ${isScreenSharing ? 'btn-icon active' : 'btn-secondary'}`}
            title={isScreenSharing ? 'Stop Screen Share' : 'Share Screen'}
          >
            <Monitor size={20} />
          </button>

          <button
            onClick={() => setIsWhiteboardOpen(!isWhiteboardOpen)}
            className={`btn btn-icon ${isWhiteboardOpen ? 'btn-icon active' : 'btn-secondary'}`}
            title="Collaborative Whiteboard"
          >
            <Edit size={20} />
          </button>

          <button
            onClick={toggleCaptions}
            className={`btn btn-icon ${isCaptionsOn ? 'btn-icon active' : 'btn-secondary'}`}
            title="Live Captions"
          >
            <Subtitles size={20} />
          </button>

          <div style={{ width: '1px', height: '32px', background: 'var(--border-subtle)', margin: '0 0.5rem' }} />

          <button
            onClick={() => {
              setIsSidebarOpen(!isSidebarOpen);
              setActiveTab('chat');
              setUnreadChatCount(0);
            }}
            className={`btn btn-icon ${isSidebarOpen && activeTab === 'chat' ? 'btn-icon active' : 'btn-secondary'}`}
            style={{ position: 'relative' }}
            title="Meeting Chat"
          >
            <MessageSquare size={20} />
            {unreadChatCount > 0 && (
              <span
                style={{
                  position: 'absolute',
                  top: '-4px',
                  right: '-4px',
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
                {unreadChatCount}
              </span>
            )}
          </button>

          <button
            onClick={() => {
              setIsSidebarOpen(!isSidebarOpen);
              setActiveTab('participants');
            }}
            className={`btn btn-icon ${isSidebarOpen && activeTab === 'participants' ? 'btn-icon active' : 'btn-secondary'}`}
            title="Participants"
          >
            <Users size={20} />
          </button>

          <div style={{ width: '1px', height: '32px', background: 'var(--border-subtle)', margin: '0 0.5rem' }} />

          {/* End/Leave Buttons */}
          <button onClick={handleLeaveMeeting} className="btn btn-secondary" title="Leave Meeting">
            <PhoneOff size={18} color="#ef4444" />
            <span>Leave</span>
          </button>

          {isHost && (
            <button onClick={handleEndMeeting} className="btn btn-danger" title="End Meeting for Everyone">
              End Meeting
            </button>
          )}
        </div>
      </div>

      {/* Collapsible Right Sidebar */}
      {isSidebarOpen && (
        <div className="meeting-sidebar">
          <div className="sidebar-tabs">
            <div
              className={`sidebar-tab ${activeTab === 'chat' ? 'active' : ''}`}
              onClick={() => {
                setActiveTab('chat');
                setUnreadChatCount(0);
              }}
            >
              Meeting Chat
            </div>
            <div
              className={`sidebar-tab ${activeTab === 'participants' ? 'active' : ''}`}
              onClick={() => setActiveTab('participants')}
            >
              Participants ({participants.length})
            </div>
          </div>

          {/* Chat Tab Content */}
          {activeTab === 'chat' && (
            <div className="sidebar-content">
              <div ref={chatScrollRef} className="chat-messages">
                {messages.length === 0 ? (
                  <p style={{ textAlign: 'center', color: 'var(--text-muted)', fontSize: '0.85rem', marginTop: '2rem' }}>
                    No messages yet. Say hello to participants!
                  </p>
                ) : (
                  messages.map((m, idx) => (
                    <div key={idx} className={`chat-bubble ${m.isLocal ? 'mine' : 'other'}`}>
                      {!m.isLocal && <div className="chat-sender">{m.senderName}</div>}
                      <div>{m.message}</div>
                      <div className="chat-time">{m.sentAt}</div>
                    </div>
                  ))
                )}
              </div>

              <form onSubmit={handleSendMessage} style={{ display: 'flex', gap: '0.5rem', marginTop: 'auto' }}>
                <input
                  type="text"
                  placeholder="Send a message to everyone..."
                  value={newMessage}
                  onChange={(e) => setNewMessage(e.target.value)}
                  className="form-input"
                  style={{ fontSize: '0.875rem', padding: '0.65rem 0.85rem' }}
                />
                <button type="submit" className="btn btn-primary" style={{ padding: '0.65rem' }}>
                  <Send size={16} />
                </button>
              </form>
            </div>
          )}

          {/* Participants Tab Content */}
          {activeTab === 'participants' && (
            <div className="sidebar-content">
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                {participants.map((p) => {
                  const isLocal = p instanceof LocalParticipant;
                  return (
                    <div
                      key={p.identity}
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        padding: '0.65rem 0.85rem',
                        background: 'rgba(255, 255, 255, 0.04)',
                        borderRadius: 'var(--radius-sm)',
                        border: '1px solid var(--border-subtle)',
                      }}
                    >
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
                        <div
                          style={{
                            width: '32px',
                            height: '32px',
                            borderRadius: '50%',
                            background: 'linear-gradient(135deg, #6366f1, #a855f7)',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            fontSize: '0.8rem',
                            fontWeight: 700,
                          }}
                        >
                          {p.name?.charAt(0)?.toUpperCase() || 'P'}
                        </div>
                        <div>
                          <div style={{ fontSize: '0.875rem', fontWeight: 600 }}>
                            {p.name} {isLocal && '(You)'}
                          </div>
                          {isHost && isLocal && (
                            <span style={{ fontSize: '0.7rem', color: '#f59e0b', fontWeight: 700 }}>Host</span>
                          )}
                        </div>
                      </div>

                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                        {!p.isMicrophoneEnabled ? <MicOff size={15} color="#ef4444" /> : <Mic size={15} color="#10b981" />}
                        {!p.isCameraEnabled ? <VideoOff size={15} color="#ef4444" /> : <VideoIcon size={15} color="#10b981" />}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
