import React, { useRef, useState, useEffect } from 'react';
import { Room, RoomEvent } from 'livekit-client';
import { Edit3, Slash, Square, Circle, Type, Trash2, X, Download } from 'lucide-react';

interface WhiteboardProps {
  room: Room | null;
  onClose: () => void;
}

type ToolType = 'brush' | 'line' | 'rect' | 'circle' | 'text';

interface DrawAction {
  tool: ToolType;
  color: string;
  size: number;
  startX: number;
  startY: number;
  endX?: number;
  endY?: number;
  points?: { x: number; y: number }[];
  text?: string;
}

export const Whiteboard: React.FC<WhiteboardProps> = ({ room, onClose }) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [currentTool, setCurrentTool] = useState<ToolType>('brush');
  const [currentColor, setCurrentColor] = useState('#6366f1');
  const [strokeSize, setStrokeSize] = useState(3);
  const [isDrawing, setIsDrawing] = useState(false);
  const [startPos, setStartPos] = useState({ x: 0, y: 0 });
  const [points, setPoints] = useState<{ x: number; y: number }[]>([]);

  // Setup canvas size
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const resize = () => {
      canvas.width = canvas.parentElement?.clientWidth || 800;
      canvas.height = canvas.parentElement?.clientHeight || 600;
    };

    resize();
    window.addEventListener('resize', resize);
    return () => window.removeEventListener('resize', resize);
  }, []);

  // Listen for remote whiteboard data packets
  useEffect(() => {
    if (!room) return;

    const handleData = (payload: Uint8Array) => {
      try {
        const text = new TextDecoder().decode(payload);
        const data = JSON.parse(text);
        if (data.type === 'wb_action') {
          drawRemoteAction(data.action);
        } else if (data.type === 'wb_clear') {
          clearCanvasLocal();
        }
      } catch {
        // Ignore unparsed packets
      }
    };

    room.on(RoomEvent.DataReceived, handleData);
    return () => {
      room.off(RoomEvent.DataReceived, handleData);
    };
  }, [room]);

  const sendDataPacket = (data: any) => {
    if (!room) return;
    try {
      const bytes = new TextEncoder().encode(JSON.stringify(data));
      room.localParticipant.publishData(bytes, { reliable: true });
    } catch (e) {
      console.warn('Failed to broadcast whiteboard packet', e);
    }
  };

  const getCanvasCoords = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return { x: 0, y: 0 };
    const rect = canvas.getBoundingClientRect();
    return {
      x: e.clientX - rect.left,
      y: e.clientY - rect.top,
    };
  };

  const drawRemoteAction = (action: DrawAction) => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.strokeStyle = action.color;
    ctx.fillStyle = action.color;
    ctx.lineWidth = action.size;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';

    if (action.tool === 'brush' && action.points && action.points.length > 1) {
      ctx.beginPath();
      ctx.moveTo(action.points[0].x, action.points[0].y);
      for (let i = 1; i < action.points.length; i++) {
        ctx.lineTo(action.points[i].x, action.points[i].y);
      }
      ctx.stroke();
    } else if (action.tool === 'line' && action.endX !== undefined && action.endY !== undefined) {
      ctx.beginPath();
      ctx.moveTo(action.startX, action.startY);
      ctx.lineTo(action.endX, action.endY);
      ctx.stroke();
    } else if (action.tool === 'rect' && action.endX !== undefined && action.endY !== undefined) {
      ctx.strokeRect(action.startX, action.startY, action.endX - action.startX, action.endY - action.startY);
    } else if (action.tool === 'circle' && action.endX !== undefined && action.endY !== undefined) {
      const radius = Math.sqrt(Math.pow(action.endX - action.startX, 2) + Math.pow(action.endY - action.startY, 2));
      ctx.beginPath();
      ctx.arc(action.startX, action.startY, radius, 0, 2 * Math.PI);
      ctx.stroke();
    } else if (action.tool === 'text' && action.text) {
      ctx.font = `${action.size * 6}px sans-serif`;
      ctx.fillText(action.text, action.startX, action.startY);
    }
  };

  const startDrawing = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const coords = getCanvasCoords(e);
    setIsDrawing(true);
    setStartPos(coords);

    if (currentTool === 'brush') {
      setPoints([coords]);
    } else if (currentTool === 'text') {
      const text = prompt('Enter text to place on whiteboard:');
      if (text) {
        const action: DrawAction = {
          tool: 'text',
          color: currentColor,
          size: strokeSize,
          startX: coords.x,
          startY: coords.y,
          text,
        };
        drawRemoteAction(action);
        sendDataPacket({ type: 'wb_action', action });
      }
      setIsDrawing(false);
    }
  };

  const drawMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!isDrawing) return;
    const coords = getCanvasCoords(e);

    if (currentTool === 'brush') {
      const canvas = canvasRef.current;
      const ctx = canvas?.getContext('2d');
      if (ctx) {
        ctx.strokeStyle = currentColor;
        ctx.lineWidth = strokeSize;
        ctx.lineCap = 'round';
        ctx.beginPath();
        const prev = points[points.length - 1] || coords;
        ctx.moveTo(prev.x, prev.y);
        ctx.lineTo(coords.x, coords.y);
        ctx.stroke();
      }
      setPoints((prev) => [...prev, coords]);
    }
  };

  const stopDrawing = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!isDrawing) return;
    setIsDrawing(false);
    const coords = getCanvasCoords(e);

    const action: DrawAction = {
      tool: currentTool,
      color: currentColor,
      size: strokeSize,
      startX: startPos.x,
      startY: startPos.y,
      endX: coords.x,
      endY: coords.y,
      points: currentTool === 'brush' ? points : undefined,
    };

    if (currentTool !== 'brush') {
      drawRemoteAction(action);
    }

    sendDataPacket({ type: 'wb_action', action });
    setPoints([]);
  };

  const clearCanvasLocal = () => {
    const canvas = canvasRef.current;
    const ctx = canvas?.getContext('2d');
    if (ctx && canvas) {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
    }
  };

  const clearAll = () => {
    clearCanvasLocal();
    sendDataPacket({ type: 'wb_clear' });
  };

  const downloadCanvas = () => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const link = document.createElement('a');
    link.download = `whiteboard_${Date.now()}.png`;
    link.href = canvas.toDataURL();
    link.click();
  };

  return (
    <div className="whiteboard-overlay">
      <div className="whiteboard-toolbar">
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <button
            onClick={() => setCurrentTool('brush')}
            className={`btn ${currentTool === 'brush' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.45rem 0.75rem', fontSize: '0.85rem' }}
            title="Free Draw"
          >
            <Edit3 size={16} />
          </button>
          <button
            onClick={() => setCurrentTool('line')}
            className={`btn ${currentTool === 'line' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.45rem 0.75rem', fontSize: '0.85rem' }}
            title="Line"
          >
            <Slash size={16} />
          </button>
          <button
            onClick={() => setCurrentTool('rect')}
            className={`btn ${currentTool === 'rect' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.45rem 0.75rem', fontSize: '0.85rem' }}
            title="Rectangle"
          >
            <Square size={16} />
          </button>
          <button
            onClick={() => setCurrentTool('circle')}
            className={`btn ${currentTool === 'circle' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.45rem 0.75rem', fontSize: '0.85rem' }}
            title="Circle"
          >
            <Circle size={16} />
          </button>
          <button
            onClick={() => setCurrentTool('text')}
            className={`btn ${currentTool === 'text' ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.45rem 0.75rem', fontSize: '0.85rem' }}
            title="Text"
          >
            <Type size={16} />
          </button>

          <div style={{ width: '1px', height: '24px', background: '#cbd5e1', margin: '0 0.5rem' }} />

          {/* Color Picker */}
          {['#6366f1', '#ef4444', '#10b981', '#f59e0b', '#000000'].map((color) => (
            <button
              key={color}
              onClick={() => setCurrentColor(color)}
              style={{
                width: '24px',
                height: '24px',
                borderRadius: '50%',
                background: color,
                border: currentColor === color ? '2px solid #0f172a' : '2px solid transparent',
                cursor: 'pointer',
              }}
            />
          ))}

          <input
            type="range"
            min={1}
            max={10}
            value={strokeSize}
            onChange={(e) => setStrokeSize(Number(e.target.value))}
            style={{ width: '80px', marginLeft: '0.5rem', cursor: 'pointer' }}
          />
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <button onClick={clearAll} className="btn btn-secondary" style={{ padding: '0.45rem 0.75rem' }} title="Clear Canvas">
            <Trash2 size={16} />
            <span>Clear</span>
          </button>
          <button onClick={downloadCanvas} className="btn btn-secondary" style={{ padding: '0.45rem 0.75rem' }} title="Download PNG">
            <Download size={16} />
          </button>
          <button onClick={onClose} className="btn btn-danger" style={{ padding: '0.45rem 0.75rem' }} title="Close Whiteboard">
            <X size={16} />
          </button>
        </div>
      </div>

      <canvas
        ref={canvasRef}
        className="whiteboard-canvas"
        onMouseDown={startDrawing}
        onMouseMove={drawMove}
        onMouseUp={stopDrawing}
        onMouseLeave={stopDrawing}
      />
    </div>
  );
};
