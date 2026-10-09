# Military Video Conference App - AI Code Generation Prompts

**Context Instructions for the AI (Send this first):**
"We are building a highly secure, air-gapped video conferencing application for a military intranet. The stack is .NET 8, React, MSSQL Server, Redis, and LiveKit. The application will be deployed offline using Docker Swarm. Acknowledge this context, but do not write any code until I send Prompt 0."

---

## Phase 0: Local Development Infrastructure

**Prompt 0: Local Dev Docker Compose**
Role: Senior DevOps Engineer
Task: Create a local development `docker-compose.dev.yml` file so I can run my backing services before writing the .NET and React code.

Requirements:
1. Create a `docker-compose.dev.yml` file that includes only the infrastructure services: MSSQL Server 2022, Redis (Alpine), and LiveKit Server. (Do not include the .NET or React apps).
2. MSSQL Server: Use image `mcr.microsoft.com/mssql/server:2022-latest`. Expose port 1433. Set the SA password to a strong developer password and accept the EULA. Map a local volume for data persistence.
3. Redis: Use `redis:7-alpine`. Expose port 6379.
4. LiveKit: Use `livekit/livekit-server:latest`. Start it in development mode (`--dev`). Bind it to the Redis container. Expose ports 7880, 7881, and the required WebRTC UDP ports (50000-50050/udp).
5. Ensure they all share a custom Docker bridge network.

---

## Phase 1: Foundation & Identity

**Prompt 1: Project Initialization & Database Foundation**
Role: Senior .NET 8 Developer
Task: Scaffold the Web API and configure EF Core with MSSQL and ASP.NET Core Identity.

Requirements:
1. Initialize a .NET 8 Web API.
2. Install `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.
3. Create `AppDbContext` inheriting `IdentityDbContext<ApplicationUser>`.
4. Define `ApplicationUser` inheriting `IdentityUser`. Add properties: `FullName` (string), `MilitaryRank` (string).
5. Configure the MSSQL connection string in `appsettings.json` (include `TrustServerCertificate=True`).
6. Register the DbContext in `Program.cs`. Do not create controllers yet.

**Prompt 2: JWT Authentication Implementation**
Role: Senior .NET 8 Developer
Task: Implement secure JWT Authentication for military intranet deployment.

Requirements:
1. Configure ASP.NET Core Identity in `Program.cs` with strict password policies.
2. Add JWT Bearer authentication. Read Issuer, Audience, and Secret Key from `appsettings.json`.
3. Create `AuthController` with `POST /api/auth/register` and `POST /api/auth/login`.
4. The login endpoint must validate the user and generate a JWT containing standard claims plus `UserId`, `Role`, and `MilitaryRank`.
5. Implement standard DTOs for requests. Ensure endpoints are documented.

---

## Phase 2: Frontend Foundation

**Prompt 3: Frontend Foundation & Air-Gapped Constraints**
Role: Senior React Developer
Task: Scaffold the React frontend with strict military intranet (air-gapped) constraints.

Requirements:
1. Initialize a React app using Tailwind CSS.
2. CRITICAL CONSTRAINT: Zero internet dependencies. Do not use external CDNs for fonts, icons, or scripts in `index.html`. All assets must be bundled via npm.
3. Use environment variables (e.g., `REACT_APP_API_URL`) for all backend and WebRTC connections. Never hardcode `localhost`.
4. Implement an `AuthContext` to manage the JWT.
5. Configure an Axios interceptor to automatically attach the JWT Bearer token to all requests.
6. Create a `ProtectedRoute` wrapper and a Tailwind-styled login page.

---

## Phase 3: Meeting Data & SignalR Persistence

**Prompt 4: Meeting & Persistent SignalR Data Models**
Role: Senior .NET 8 Developer
Task: Create EF Core models for the meeting lifecycle and persistent SignalR state.

Requirements:
1. Create entities: `Meeting`, `MeetingParticipant`, `SignalRConnection`, `WaitingRoomEntry`, and `ChatMessage`.
2. `Meeting`: properties for `Id`, `Title`, `HostId`, `MeetingCode`, `Passcode`, `Status` (Scheduled, Active, Ended), `LiveKitRoomName`.
3. `SignalRConnection`: properties for `ConnectionId` (PK), `UserId` (FK), `MeetingId` (nullable FK), `ConnectedAt`.
4. Configure all Foreign Key relationships in `AppDbContext` via Fluent API.
5. Generate the Entity classes and the updated `AppDbContext` code.

**Prompt 5: Persistent SignalR Hub & Multi-Server Scaling**
Role: Senior .NET 8 Developer
Task: Implement the SignalR Hub with database persistence and Redis backplane.

Requirements:
1. Install `@microsoft/signalr` and `Microsoft.AspNetCore.SignalR.StackExchangeRedis`.
2. Register SignalR in `Program.cs`, configured to use the Redis backplane for multi-server Swarm deployment.
3. Create `MeetingHub` inheriting `Hub`. Apply `[Authorize]`.
4. Override `OnConnectedAsync`: Extract `UserId` from JWT, insert a `SignalRConnection` record into MSSQL with the `Context.ConnectionId`.
5. Override `OnDisconnectedAsync`: Delete or deactivate the user's `SignalRConnection` record in MSSQL.

---

## Phase 4: Video Integration

**Prompt 6: LiveKit Token Generation API**
Role: Senior .NET 8 Developer
Task: Implement the LiveKit SDK for secure room token generation.

Requirements:
1. Install the `LiveKit.Server` NuGet package.
2. Create `MeetingController` with `POST /api/meeting/{meetingCode}/join`.
3. Endpoint logic: Verify the meeting exists in MSSQL and the requesting user is authorized (host, or admitted attendee).
4. If authorized, use the LiveKit SDK to mint an Access Token using the `LiveKitRoomName`.
5. Return the Token and the LiveKit WebSocket URL (from `appsettings.json`) to the client.

**Prompt 7: LiveKit Video UI & Intranet WebRTC**
Role: Senior React Developer
Task: Implement the Video Room component using LiveKit components.

Requirements:
1. Install `@livekit/components-react` and `livekit-client`.
2. Create a `VideoRoom` component accepting `token` and `serverUrl` as props.
3. Wrap the UI in `<LiveKitRoom>`. 
4. CRITICAL CONSTRAINT: Override default WebRTC config to disable public Google STUN servers. Ensure it only routes locally or to the provided intranet TURN server.
5. Render the pre-built `<VideoConference />` component for the grid and media controls.
6. Style the container to occupy `100vh` using Tailwind.

---

## Phase 5: Collaboration & Controls

**Prompt 8: Persistent Meeting Chat**
Role: Senior Full-Stack Developer (.NET & React)
Task: Implement real-time, persistent meeting chat.

Requirements:
1. Backend (`MeetingHub`): Add `SendMessage(Guid meetingId, string text)`. Save the message to the `ChatMessage` MSSQL table, then broadcast via SignalR to clients in the room.
2. Backend (`MeetingController`): Add `GET /api/meeting/{meetingId}/chat` to fetch chat history for late joiners.
3. Frontend: Create a Tailwind-styled `ChatBox`. Fetch history on mount, then append incoming SignalR `ReceiveMessage` events to state.

**Prompt 9: Waiting Room & Host Controls**
Role: Senior Full-Stack Developer (.NET & React)
Task: Implement the Waiting Room and Host Admit logic via SignalR.

Requirements:
1. Backend (`MeetingHub`): Add `RequestJoin(Guid meetingId)`. Insert a `WaitingRoomEntry` (Status: Waiting) into MSSQL and emit `UserWaiting` to the Host's connection ID.
2. Backend (`MeetingHub`): Add `AdmitUser(Guid entryId)`. Update DB status to Admitted, generate the LiveKit token, and send it explicitly to the waiting user's active `ConnectionId` via SignalR.
3. Frontend: Build a `WaitingRoomSidebar` for the Host to view pending users and trigger the Admit/Deny actions.

**Prompt 10: Intranet-Safe Collaborative Whiteboard**
Role: Senior React Developer
Task: Implement a collaborative whiteboard synced via LiveKit Data Channels.

Requirements:
1. Install `tldraw`. Verify it requires no external web fonts or CDN scripts.
2. Create a `CollaborativeWhiteboard` component rendering `<Tldraw />`.
3. Use LiveKit's `useDataChannel` hook.
4. Capture local drawing events, serialize to byte arrays, and broadcast over the data channel.
5. On receiving remote byte arrays from the channel, deserialize and apply to the local `tldraw` instance.

---

## Phase 6: DevOps & Final Polish

**Prompt 11: Egress Recording API**
Role: Senior .NET 8 Developer
Task: Implement LiveKit Egress to record meetings to S3/MinIO.

Requirements:
1. Add `POST /api/meeting/{id}/record/start` to `MeetingController`.
2. Validate the requester is the Host.
3. Use `LiveKit.Server` SDK to trigger a `RoomCompositeEgressRequest`.
4. Configure the request to output an `.mp4` file to the S3-compatible MinIO endpoint configured in `appsettings.json`.
5. Update the `Meeting` record in MSSQL to reflect the recording state.

**Prompt 12: Docker Swarm Deployment Stack**
Role: Senior DevOps Engineer
Task: Create the final air-gapped deployment configuration.

Requirements:
1. Write a multi-stage `Dockerfile` for the .NET 8 backend.
2. Write a multi-stage `Dockerfile` for the React frontend, using Nginx to serve static files and proxy `/api` traffic to the backend container.
3. Create a `docker-compose.yml` formatted for Docker Swarm (`docker stack deploy`).
4. Include services: MSSQL 2022, Redis, LiveKit Server, MinIO, Backend API, Frontend UI.
5. CRITICAL CONSTRAINT: All networking must be handled internally via Docker networks. No ports should be exposed publicly except 80 (Frontend) and the required LiveKit UDP WebRTC ports. Use environment variables for all cross-container communication.
