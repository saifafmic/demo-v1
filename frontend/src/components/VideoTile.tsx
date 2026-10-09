import React, { useRef, useEffect, useState } from 'react';
import { Track, TrackPublication } from 'livekit-client';
import { MicOff, Shield, Monitor } from 'lucide-react';

interface VideoTileProps {
  displayName: string;
  isLocal?: boolean;
  isHost?: boolean;
  isCoHost?: boolean;
  isSpeaking?: boolean;
  isAudioMuted?: boolean;
  isVideoMuted?: boolean;
  isScreenShare?: boolean;
  videoTrackPublication?: TrackPublication | null;
  audioTrackPublication?: TrackPublication | null;
}

export const VideoTile: React.FC<VideoTileProps> = ({
  displayName,
  isLocal = false,
  isHost = false,
  isCoHost = false,
  isSpeaking = false,
  isAudioMuted = false,
  isVideoMuted = false,
  isScreenShare = false,
  videoTrackPublication,
  audioTrackPublication,
}) => {
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);

  const [videoTrack, setVideoTrack] = useState<Track | undefined>(
    videoTrackPublication?.track
  );
  const [audioTrack, setAudioTrack] = useState<Track | undefined>(
    audioTrackPublication?.track
  );

  // Keep track of track subscriptions & updates
  useEffect(() => {
    setVideoTrack(videoTrackPublication?.track);
    if (!videoTrackPublication) return;

    const onSubscribed = (track: Track) => setVideoTrack(track);
    const onUnsubscribed = () => setVideoTrack(undefined);

    videoTrackPublication.on('subscribed', onSubscribed);
    videoTrackPublication.on('unsubscribed', onUnsubscribed);

    return () => {
      videoTrackPublication.off('subscribed', onSubscribed);
      videoTrackPublication.off('unsubscribed', onUnsubscribed);
    };
  }, [videoTrackPublication, videoTrackPublication?.track]);

  useEffect(() => {
    setAudioTrack(audioTrackPublication?.track);
    if (!audioTrackPublication) return;

    const onSubscribed = (track: Track) => setAudioTrack(track);
    const onUnsubscribed = () => setAudioTrack(undefined);

    audioTrackPublication.on('subscribed', onSubscribed);
    audioTrackPublication.on('unsubscribed', onUnsubscribed);

    return () => {
      audioTrackPublication.off('subscribed', onSubscribed);
      audioTrackPublication.off('unsubscribed', onUnsubscribed);
    };
  }, [audioTrackPublication, audioTrackPublication?.track]);

  // Attach Video Track to DOM element
  useEffect(() => {
    const videoEl = videoRef.current;
    if (videoEl && videoTrack) {
      videoTrack.attach(videoEl);
      return () => {
        videoTrack.detach(videoEl);
      };
    }
  }, [videoTrack]);

  // Attach Audio Track to DOM element (remote participants only)
  useEffect(() => {
    const audioEl = audioRef.current;
    if (audioEl && audioTrack && !isLocal) {
      audioTrack.attach(audioEl);
      return () => {
        audioTrack.detach(audioEl);
      };
    }
  }, [audioTrack, isLocal]);

  const hasVideo = !!videoTrack && !isVideoMuted;
  const initials =
    displayName
      .split(' ')
      .map((n) => n[0])
      .join('')
      .substring(0, 2)
      .toUpperCase() || 'U';

  return (
    <div
      className={`video-tile ${isScreenShare ? 'screen-share' : ''} ${
        isSpeaking && !isScreenShare ? 'speaking' : ''
      }`}
    >
      {/* Remote Audio output element */}
      {!isLocal && <audio ref={audioRef} autoPlay />}

      {/* Video Stream Element */}
      <video
        ref={videoRef}
        autoPlay
        playsInline
        muted={isLocal}
        style={{
          display: hasVideo ? 'block' : 'none',
          objectFit: isScreenShare ? 'contain' : 'cover',
          transform: isLocal && !isScreenShare ? 'scaleX(-1)' : 'none',
        }}
      />

      {/* Avatar Fallback when video track is disabled */}
      {!hasVideo && (
        <div className="tile-avatar">
          {isScreenShare ? <Monitor size={36} /> : initials}
        </div>
      )}

      {/* Bottom Information Pill */}
      <div className="tile-info">
        {isHost && (
          <span
            style={{
              color: '#f59e0b',
              display: 'flex',
              alignItems: 'center',
              gap: '3px',
              fontSize: '0.75rem',
            }}
          >
            <Shield size={13} /> Host
          </span>
        )}
        {isCoHost && !isHost && (
          <span
            style={{
              color: '#a855f7',
              display: 'flex',
              alignItems: 'center',
              gap: '3px',
              fontSize: '0.75rem',
            }}
          >
            <Shield size={13} /> Co-Host
          </span>
        )}
        <span>
          {displayName} {isLocal && '(You)'}
        </span>
        {isAudioMuted && !isScreenShare && <MicOff size={14} color="#ef4444" />}
      </div>
    </div>
  );
};
