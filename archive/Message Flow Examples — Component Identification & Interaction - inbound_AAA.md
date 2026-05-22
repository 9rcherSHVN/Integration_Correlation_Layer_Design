




Inbound Message Flow — Component-by-Component Breakdown

Inbound Flow Component Inventory

Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                              INBOUND FLOW: 3RD PARTY → ERP                                                   │
│                              (Tendering Updates, Appointments, Invoices, Delivery Status)                    │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  FLOW SEQUENCE:                                                                                              │
│  E2 → C2 → C5 → C6 → C7 → C8 → C10 → C3B → E3 → E1                                                          │
│                                                                                                             │
│  WHERE:                                                                                                      │
│  • E2  = 3rd Party Logistics API (polling endpoint)                                                         │
│  • C2  = Inbound Poll Logic App                                                                             │
│  • C5  = Correlation Manager Function (lookup mode)                                                         │
│  • C6  = Schema Validator Function                                                                          │
│  • C7  = Business Validator Function                                                                        │
│  • C8  = Transformer Function (inbound direction)                                                           │
│  • C10 = Service Bus Inbound Topic                                                                          │
│  • C3B = Batch Processing Logic App (Inbound)                                                               │
│  • E3  = Azure Relay / Hybrid Connection                                                                    │
│  • E1  = ERP REST API (on-premise)                                                                          │
│                                                                                                             │
│  WITH TELEMETRY: All components → C12 (Application Insights)                                                │
│  WITH STORAGE:   C5 ↔ S1 (Correlation lookup & linking), C8 → S2 (Audit blobs), C3B → S1 (Audit log)       │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

STEP 1: 3rd Party Logistics API — Polling Endpoint (E2)
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: E2 — 3rd Party Logistics API (Inbound Polling Endpoint)                                         │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Source system for inbound tendering updates, appointments, invoices, and delivery status   │
│  TYPE:           Cloud-hosted REST API (external vendor)                                                    │
│  LOCATION:       3rd party cloud infrastructure (outside Azure environment)                                 │
│  ENDPOINT:       GET https://api.thirdparty-logistics.com/api/v1/tendering?entryId={lastEntryId}&limit={n} │
│  PROTOCOL:       HTTPS (REST)                                                                               │
│  AUTHENTICATION: API Key (X-API-Key header)                                                                 │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Expose polling endpoint for integration platform to retrieve new updates                                 │
│  • Maintain sequential entryId for each update event (1, 2, 3, ...)                                         │
│  • Return updates since last retrieved entryId                                                              │
│  • Include correlationGuid in each update (for correlation with original shipment)                          │
│  • Include tenderId once assigned (for linking)                                                             │
│  • Support pagination (limit parameter)                                                                     │
│  • Authenticate requests via API key                                                                        │
│  • Provide idempotent responses (same entryId always returns same data)                                     │
│                                                                                                             │
│  POLLING PATTERN:                                                                                           │
│  ────────────────                                                                                           │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  SEQUENTIAL ENTRY ID MODEL:                                                                        │    │
│  │  ═══════════════════════════                                                                       │    │
│  │                                                                                                    │    │
│  │  • Each update event gets unique, sequential entryId (auto-incrementing)                           │    │
│  │  • entryId never reused                                                                            │    │
│  │  • Integration polls: GET /tendering?entryId=98764&limit=100                                       │    │
│  │    - Returns entries 98765, 98766, 98767, ... up to 100 records                                    │    │
│  │  • Next poll: GET /tendering?entryId=98864&limit=100                                               │    │
│  │    - Returns entries 98865, 98866, ...                                                             │    │
│  │  • If no new updates: Returns empty array                                                          │    │
│  │  • Integration stores lastEntryId for next poll                                                    │    │
│  │                                                                                                    │    │
│  │  BENEFITS:                                                                                         │    │
│  │  • Simple, reliable change detection                                                               │    │
│  │  • No missed updates (sequential processing)                                                       │    │
│  │  • Idempotent (can re-poll same entryId range)                                                     │    │
│  │  • No timestamp issues (no clock skew, timezone problems)                                          │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  REQUEST/RESPONSE EXAMPLE:                                                                                  │
│  ─────────────────────────                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  REQUEST:                                                                                          │    │
│  │  ════════                                                                                          │    │
│  │                                                                                                    │    │
│  │  GET /api/v1/tendering?entryId=98764&limit=100                                                     │    │
│  │  Headers:                                                                                          │    │
│  │    X-API-Key: {api-key-value}                                                                      │    │
│  │    Accept: application/xml                                                                         │    │
│  │                                                                                                    │    │
│  │                                                                                                    │    │
│  │  RESPONSE (Multiple Updates):                                                                      │    │
│  │  ═════════════════════════                                                                         │    │
│  │                                                                                                    │    │
│  │  HTTP 200 OK                                                                                       │    │
│  │  Content-Type: application/xml                                                                     │    │
│  │                                                                                                    │    │
│  │  <TenderingUpdates>                                                                                │    │
│  │    <update>                                                                                        │    │
│  │      <entryId>98765</entryId>                                                                      │    │
│  │      <correlationGuid>550e8400-e29b-41d4-a716-446655440000</correlationGuid>                       │    │
│  │      <tenderId>TND-2026-00456</tenderId>                                                           │    │
│  │      <shpnumRef>ERP12345</shpnumRef>                                                               │    │
│  │      <entityId>THM</entityId>                                                                      │    │
│  │      <operationType>TENDER_ASSIGNED</operationType>                                                │    │
│  │      <shipmentCost>2500.00</shipmentCost>                                                          │    │
│  │      <currency>CAD</currency>                                                                      │    │
│  │      <vendor>FastFreight Inc</vendor>                                                              │    │
│  │      <vendorCode>FFI</vendorCode>                                                                  │    │
│  │      <estimatedPickup>2026-01-30T08:00:00Z</estimatedPickup>                                       │    │
│  │      <createdDateTime>2026-01-29T14:30:00Z</createdDateTime>                                       │    │
│  │    </update>                                                                                       │    │
│  │                                                                                                    │    │
│  │    <update>                                                                                        │    │
│  │      <entryId>98766</entryId>                                                                      │    │
│  │      <correlationGuid>7f3d2a10-8b4c-4e5f-9a1d-123456789abc</correlationGuid>                       │    │
│  │      <tenderId>TND-2026-00457</tenderId>                                                           │    │
│  │      <shpnumRef>ERP12346</shpnumRef>                                                               │    │
│  │      <entityId>THM</entityId>                                                                      │    │
│  │      <operationType>TENDER_ASSIGNED</operationType>                                                │    │
│  │      <shipmentCost>1850.00</shipmentCost>                                                          │    │
│  │      <currency>CAD</currency>                                                                      │    │
│  │      <vendor>QuickShip Logistics</vendor>                                                          │    │
│  │      <vendorCode>QSL</vendorCode>                                                                  │    │
│  │      <estimatedPickup>2026-01-30T10:00:00Z</estimatedPickup>                                       │    │
│  │      <createdDateTime>2026-01-29T14:35:00Z</createdDateTime>                                       │    │
│  │    </update>                                                                                       │    │
│  │                                                                                                    │    │
│  │    <update>                                                                                        │    │
│  │      <entryId>98767</entryId>                                                                      │    │
│  │      <correlationGuid>550e8400-e29b-41d4-a716-446655440000</correlationGuid>                       │    │
│  │      <tenderId>TND-2026-00456</tenderId>                                                           │    │
│  │      <shpnumRef>ERP12345</shpnumRef>                                                               │    │
│  │      <entityId>THM</entityId>                                                                      │    │
│  │      <operationType>APPOINTMENT_SCHEDULED</operationType>                                          │    │
│  │      <appointmentDate>2026-01-30</appointmentDate>                                                 │    │
│  │      <appointmentWindow>08:00-10:00</appointmentWindow>                                            │    │
│  │      <createdDateTime>2026-01-29T15:00:00Z</createdDateTime>                                       │    │
│  │    </update>                                                                                       │    │
│  │                                                                                                    │    │
│  │    <!-- ... more updates up to limit=100 ... -->                                                   │    │
│  │                                                                                                    │    │
│  │  </TenderingUpdates>                                                                               │    │
│  │                                                                                                    │    │
│  │                                                                                                    │    │
│  │  RESPONSE (No New Updates):                                                                        │    │
│  │  ═══════════════════════════                                                                       │    │
│  │                                                                                                    │    │
│  │  HTTP 200 OK                                                                                       │    │
│  │  Content-Type: application/xml                                                                     │    │
│  │                                                                                                    │    │
│  │  <TenderingUpdates>                                                                                │    │
│  │    <!-- Empty - no new updates since entryId 98764 -->                                             │    │
│  │  </TenderingUpdates>                                                                               │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  UPDATE TYPES (operationType):                                                                              │
│  ─────────────────────────────                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. TENDER_ASSIGNED                                                                                │    │
│  │     • Returned when carrier assigned to shipment                                                   │    │
│  │     • Includes: tenderId, vendor, vendorCode, shipmentCost, estimatedPickup                        │    │
│  │     • Typically first update for a shipment (hours after submission)                               │    │
│  │                                                                                                    │    │
│  │  2. APPOINTMENT_SCHEDULED                                                                          │    │
│  │     • Returned when pickup/delivery appointment scheduled                                          │    │
│  │     • Includes: appointmentDate, appointmentWindow                                                 │    │
│  │     • May occur multiple times (pickup appointment, delivery appointment)                          │    │
│  │                                                                                                    │    │
│  │  3. COST_ESTIMATION_UPDATED                                                                        │    │
│  │     • Returned when cost estimation changes (e.g., due to weight adjustment)                       │    │
│  │     • Includes: updatedCost, reason                                                                │    │
│  │                                                                                                    │    │
│  │  4. DELIVERED                                                                                      │    │
│  │     • Returned when shipment delivered                                                             │    │
│  │     • Includes: deliveryTimestamp, signedBy, proofOfDelivery (optional)                           │    │
│  │                                                                                                    │    │
│  │  5. INVOICE                                                                                        │    │
│  │     • Returned when invoice generated                                                              │    │
│  │     • Includes: invoiceNumber, invoiceAmount, invoiceDate, dueDate                                │    │
│  │     • Typically final update for a shipment                                                        │    │
│  │                                                                                                    │    │
│  │  6. EXCEPTION                                                                                      │    │
│  │     • Returned when exception occurs (delay, damage, lost)                                         │    │
│  │     • Includes: exceptionType, description, resolutionStatus                                       │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  CORRELATION IDENTIFIERS:                                                                                   │
│  ────────────────────────                                                                                   │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  Each update includes THREE identifiers for correlation:                                           │    │
│  │                                                                                                    │    │
│  │  1. correlationGuid (PRIMARY)                                                                      │    │
│  │     • The GUID originally sent in outbound shipment request                                        │    │
│  │     • Used by C5 (Correlation Manager) for fastest lookup                                          │    │
│  │     • Example: "550e8400-e29b-41d4-a716-446655440000"                                              │    │
│  │                                                                                                    │    │
│  │  2. tenderId (SECONDARY)                                                                           │    │
│  │     • Generated by 3rd party during tendering process                                              │    │
│  │     • Used for subsequent updates (appointments, invoices)                                         │    │
│  │     • Example: "TND-2026-00456"                                                                    │    │
│  │     • Linked to CorrelationId by C5 on first TENDER_ASSIGNED update                                │    │
│  │                                                                                                    │    │
│  │  3. shpnumRef + entityId (FALLBACK)                                                                │    │
│  │     • Reference to original ERP shipment number                                                    │    │
│  │     • Used if correlationGuid or tenderId lookup fails                                             │    │
│  │     • Example: shpnumRef="ERP12345", entityId="THM"                                                │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C2 (Inbound Poll Logic App) — every 5 minutes                                                 │
│  • CALLS: None (passive endpoint)                                                                           │
│  • EMITS TELEMETRY TO: 3rd party's own monitoring (outside integration scope)                               │
│                                                                                                             │
│  ERROR RESPONSES:                                                                                           │
│  ───────────────                                                                                            │
│  • 400 Bad Request: Invalid entryId format or limit value                                                   │
│  • 401 Unauthorized: Invalid or missing API key                                                             │
│  • 429 Too Many Requests: Polling too frequently (rate limit)                                               │
│  • 500 Internal Server Error: 3rd party system error                                                        │
│  • 503 Service Unavailable: 3rd party system down                                                           │
│                                                                                                             │
│  RATE LIMITING:                                                                                             │
│  ──────────────                                                                                             │
│  • 3rd party may limit polling frequency (e.g., max 1 request per minute)                                   │
│  • C2 polls every 5 minutes (well within typical limits)                                                    │
│  • If 429 received, C2 should back off (increase polling interval temporarily)                              │
│                                                                                                             │
│  IDEMPOTENCY:                                                                                               │
│  ────────────                                                                                               │
│  • Same entryId query always returns same results                                                           │
│  • Safe to re-poll same range (e.g., if C2 crashes before processing)                                       │
│  • No updates are "consumed" or removed by polling (read-only operation)                                    │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

STEP 2: Inbound Poll Logic App (C2) — Polling Orchestrator
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C2 — Inbound Poll Logic App                                                                     │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Orchestrator — Poll 3rd party for tendering updates and initiate inbound processing        │
│  TYPE:           Logic App (Standard or Consumption)                                                        │
│  TRIGGER:        Recurrence (every 5 minutes)                                                               │
│  CONCURRENCY:    Sequential (one poll at a time to maintain state)                                          │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Schedule periodic polling (every 5 minutes)                                                              │
│  • Retrieve last successful entryId from state (Table Storage)                                              │
│  • Call 3rd party polling endpoint (E2) with lastEntryId                                                    │
│  • Parse response XML (array of update records)                                                             │
│  • For each update:                                                                                         │
│    - Call Correlation Manager (C5) to lookup CorrelationId by correlationGuid/tenderId/shpnumRef            │
│    - If first TENDER_ASSIGNED for shipment: Link tenderId to CorrelationId                                  │
│    - Call Schema Validator (C6) to validate XML structure                                                   │
│    - Call Business Validator (C7) to validate business rules                                                │
│    - If validation passes: Forward to Transformer (C8)                                                      │
│    - If validation fails: Send to Dead-Letter Queue (DLQ) and trigger Alert Handler (C4)                    │
│  • Update last processed entryId on successful completion                                                   │
│  • Emit telemetry to Application Insights (C12)                                                             │
│                                                                                                             │
│  WORKFLOW STRUCTURE:                                                                                        │
│  ──────────────────                                                                                         │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. TRIGGER: Recurrence (5 minutes)                                                                │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. ACTION: Get Last Processed EntryId                                                             │    │
│  │      • Read from Table Storage (PollingState table)                                                │    │
│  │      • Key: "InboundPollLastEntryId"                                                               │    │
│  │      • Default: 0 (if first run)                                                                   │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. ACTION: HTTP GET to 3rd Party API (E2)                                                         │    │
│  │      • URI: https://api.thirdparty-logistics.com/api/v1/tendering                                  │    │
│  │      • Query Parameters:                                                                           │    │
│  │        - entryId = {lastEntryId from previous step}                                                │    │
│  │        - limit = 100                                                                               │    │
│  │      • Headers:                                                                                    │    │
│  │        - X-API-Key: @{parameters('ThirdPartyApiKey')}                                              │    │
│  │        - Accept: application/xml                                                                   │    │
│  │      • Timeout: 30 seconds                                                                         │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. ACTION: Parse XML Response                                                                     │    │
│  │      • Parse <TenderingUpdates> XML                                                                │    │
│  │      • Extract array of <update> elements                                                          │    │
│  │      • Check if empty (no new updates)                                                             │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  5. CONDITION: Are there new updates?                                                              │    │
│  │      │                                                                                             │    │
│  │      ├──▶ NO (empty array):                                                                        │    │
│  │      │      • Log: "No new updates available"                                                      │    │
│  │      │      • Skip processing                                                                      │    │
│  │      │      • END (wait for next trigger)                                                          │    │
│  │      │                                                                                             │    │
│  │      └──▶ YES (updates found):                                                                     │    │
│  │           │                                                                                         │    │
│  │           ▼                                                                                         │    │
│  │  6. ACTION: For Each Update                                                                        │    │
│  │      │                                                                                             │    │
│  │      ├──▶ 6.1: SCOPE: Process Single Update (for error handling)                                  │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 6.1.1: Compose — Extract Update Fields                                           │    │
│  │      │      │      • entryId                                                                       │    │
│  │      │      │      • correlationGuid                                                               │    │
│  │      │      │      • tenderId                                                                      │    │
│  │      │      │      • shpnumRef                                                                     │    │
│  │      │      │      • entityId                                                                      │    │
│  │      │      │      • operationType                                                                 │    │
│  │      │      │      • updateXml (full XML of this update)                                           │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 6.1.2: HTTP Call to Correlation Manager (C5) — Lookup                            │    │
│  │      │      │      • Endpoint: POST /api/correlation/lookup                                        │    │
│  │      │      │      • Body:                                                                         │    │
│  │      │      │        {                                                                             │    │
│  │      │      │          "correlationGuid": "{from 6.1.1}",                                          │    │
│  │      │      │          "tenderId": "{from 6.1.1}",                                                 │    │
│  │      │      │          "shpnumRef": "{from 6.1.1}",                                                │    │
│  │      │      │          "entityId": "{from 6.1.1}"                                                  │    │
│  │      │      │        }                                                                             │    │
│  │      │      │      • Returns: { found, correlationId, correlationGuid, ... }                       │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 6.1.3: CONDITION — Correlation Found?                                            │    │
│  │      │      │      │                                                                               │    │
│  │      │      │      ├──▶ NO (orphan message):                                                      │    │
│  │      │      │      │      • Log error: "Cannot correlate update, no matching shipment"            │    │
│  │      │      │      │      • Send to DLQ (with error details)                                      │    │
│  │      │      │      │      • Skip to next update                                                   │    │
│  │      │      │      │                                                                               │    │
│  │      │      │      └──▶ YES (correlation found):                                                  │    │
│  │      │      │           │                                                                          │    │
│  │      │      │           ├──▶ 6.1.4: CONDITION — First TENDER_ASSIGNED for shipment?              │    │
│  │      │      │           │      • Check: operationType == "TENDER_ASSIGNED"                         │    │
│  │      │      │           │      • AND: Lookup response has tenderId == null                         │    │
│  │      │      │           │      │                                                                   │    │
│  │      │      │           │      ├──▶ YES: HTTP Call to Link Tender                                 │    │
│  │      │      │           │      │      • Endpoint: POST /api/correlation/link-tender                │    │
│  │      │      │           │      │      • Body:                                                      │    │
│  │      │      │           │      │        {                                                          │    │
│  │      │      │           │      │          "correlationGuid": "{...}",                              │    │
│  │      │      │           │      │          "tenderId": "{from update}",                             │    │
│  │      │      │           │      │          "entityId": "{...}"                                      │    │
│  │      │      │           │      │        }                                                          │    │
│  │      │      │           │      │      • Updates CorrelationMapping + ThirdPartyLookup tables       │    │
│  │      │      │           │      │                                                                   │    │
│  │      │      │           │      └──▶ NO: Skip (tender already linked)                               │    │
│  │      │      │           │                                                                          │    │
│  │      │      │           ├──▶ 6.1.5: HTTP Call to Schema Validator (C6)                            │    │
│  │      │      │           │      • Endpoint: POST /api/validate/schema                               │    │
│  │      │      │           │      • Body: { correlationId, correlationGuid, xmlPayload, schemaType }  │    │
│  │      │      │           │      • Returns: { valid, errors }                                        │    │
│  │      │      │           │                                                                          │    │
│  │      │      │           ├──▶ 6.1.6: CONDITION — Schema Valid?                                     │    │
│  │      │      │           │      │                                                                   │    │
│  │      │      │           │      ├──▶ NO: Send to DLQ, skip to next update                          │    │
│  │      │      │           │      │                                                                   │    │
│  │      │      │           │      └──▶ YES: Continue                                                 │    │
│  │      │      │           │           │                                                              │    │
│  │      │      │           │           ├──▶ 6.1.7: HTTP Call to Business Validator (C7)              │    │
│  │      │      │           │           │      • Similar to schema validation                          │    │
│  │      │      │           │           │      • Returns: { valid, errors }                            │    │
│  │      │      │           │           │                                                              │    │
│  │      │      │           │           ├──▶ 6.1.8: CONDITION — Business Rules Valid?                 │    │
│  │      │      │           │           │      │                                                       │    │
│  │      │      │           │           │      ├──▶ NO: Send to DLQ                                   │    │
│  │      │      │           │           │      │                                                       │    │
│  │      │      │           │           │      └──▶ YES: Continue                                     │    │
│  │      │      │           │           │           │                                                  │    │
│  │      │      │           │           │           ├──▶ 6.1.9: HTTP Call to Transformer (C8)         │    │
│  │      │      │           │           │           │      • Endpoint: POST /api/transform/inbound     │    │
│  │      │      │           │           │           │      • Body: 3rd Party XML + correlation info    │    │
│  │      │      │           │           │           │      • Returns: ERP XML (transformed)            │    │
│  │      │      │           │           │           │                                                  │    │
│  │      │      │           │           │           └──▶ 6.1.10: Send to Service Bus (C10)            │    │
│  │      │      │           │           │                • Topic: inbound-tendering                    │    │
│  │      │      │           │           │                • Properties:                                 │    │
│  │      │      │           │           │                  - CorrelationId                             │    │
│  │      │      │           │           │                  - CorrelationGuid                           │    │
│  │      │      │           │           │                  - TenderId                                  │    │
│  │      │      │           │           │                  - OperationType                             │    │
│  │      │      │           │           │                  - Direction: Inbound                        │    │
│  │      │      │           │           │                  - EntryId                                   │    │
│  │      │      │           │           │                • Body: Transformed ERP XML                   │    │
│  │      │      │                                                                                      │    │
│  │      │      └──▶ 6.1.11: CATCH (Scope Error Handling)                                             │    │
│  │      │           • If any action fails: Log error, continue with next update                       │    │
│  │      │           • Don't fail entire batch due to single update failure                            │    │
│  │      │                                                                                             │    │
│  │      └──▶ (End For Each)                                                                          │    │
│  │           │                                                                                         │    │
│  │           ▼                                                                                         │    │
│  │  7. ACTION: Update Last Processed EntryId                                                          │    │
│  │      • Extract max(entryId) from processed updates                                                 │    │
│  │      • Write to Table Storage                                                                      │    │
│  │      • Key: "InboundPollLastEntryId"                                                                │    │
│  │      • Value: {maxEntryId}                                                                         │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  8. ACTION: Log Batch Completion                                                                   │    │
│  │      • Custom Event: "InboundPollingCompleted"                                                     │    │
│  │      • Properties: UpdateCount, SuccessCount, FailureCount, LastEntryId                            │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  9. END                                                                                            │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: Timer/Recurrence trigger (Azure scheduler)                                                    │
│  • CALLS:                                                                                                   │
│    - E2 (3rd Party API) — polling endpoint                                                                  │
│    - C5 (Correlation Manager) — lookup and link operations                                                  │
│    - C6 (Schema Validator)                                                                                  │
│    - C7 (Business Validator)                                                                                │
│    - C8 (Transformer) — inbound direction                                                                   │
│    - C10 (Service Bus Inbound Topic) — publishes messages                                                   │
│    - S1 (Table Storage) — state management (lastEntryId)                                                    │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Workflow execution start/end                                                                             │
│  • HTTP dependency to E2 (3rd party polling)                                                                │
│  • HTTP dependencies to C5, C6, C7, C8                                                                      │
│  • Service Bus message publish to C10                                                                       │
│  • Tracked properties: LastEntryId, UpdateCount, SuccessCount, FailureCount                                 │
│  • Custom events: PollStarted, PollCompleted, UpdateProcessed, CorrelationNotFound                          │
│                                                                                                             │
│  ERROR HANDLING:                                                                                            │
│  ───────────────                                                                                            │
│  • 3rd party API failure: Retry 3 times, then fail workflow (lastEntryId not updated, will retry next poll) │
│  • Correlation not found: Log error, send to DLQ, continue with next update                                 │
│  • Validation failure: Send to DLQ, trigger alert, continue                                                 │
│  • Service Bus failure: Retry automatically (SDK handles)                                                   │
│                                                                                                             │
│  STATE MANAGEMENT:                                                                                          │
│  ─────────────────                                                                                          │
│  • LastEntryId stored in Table Storage (S1)                                                                 │
│  • Only updated after ALL updates in batch processed successfully                                           │
│  • If workflow fails mid-batch: Next poll will reprocess from last successful entryId                       │
│  • Idempotency handled by C5 (duplicate detection) and Service Bus (duplicate detection)                    │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Polling interval: 5 minutes                                                                              │
│  • Batch size: Up to 100 updates per poll (configurable via limit parameter)                                │
│  • Processing time per update: ~1-2 seconds (C5 lookup + validations + transform + Service Bus publish)     │
│  • Total batch processing: ~100-200 seconds for full batch (may span into next poll interval)               │
│  • Expected load: ~10-50 updates per poll (based on 500 shipments/hour with 3-5 updates per shipment)       │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘


STEP 3: Correlation Manager Function (C5) — Lookup & Link Mode
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C5 — Correlation Manager Function (Inbound/Lookup Mode)                                         │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Lookup CorrelationId from inbound message identifiers + Link tenderId on first encounter   │
│  TYPE:           Azure Function (HTTP Trigger)                                                              │
│  RUNTIME:        .NET 8 Isolated Worker                                                                     │
│  ENDPOINTS:      POST /api/correlation/lookup                                                               │
│                  POST /api/correlation/link-tender                                                          │
│  AUTHENTICATION: Function key (managed in Key Vault)                                                        │
│                                                                                                             │
│  RESPONSIBILITIES (INBOUND OPERATIONS):                                                                     │
│  ──────────────────────────────────────                                                                     │
│  • Receive inbound message identifiers from C2 (correlationGuid, tenderId, shpnumRef, entityId)             │
│  • Perform multi-strategy lookup to find matching CorrelationId:                                            │
│    - STRATEGY 1 (Primary):   Lookup by correlationGuid in GuidLookup table (fastest)                        │
│    - STRATEGY 2 (Fallback):  Lookup by tenderId in ThirdPartyLookup table                                   │
│    - STRATEGY 3 (Fallback):  Lookup by shpnumRef + entityId in CorrelationMapping table (scan required)     │
│  • If found: Return CorrelationId and full correlation context                                              │
│  • If not found: Return error (orphan message)                                                              │
│  • On first TENDER_ASSIGNED: Link tenderId to CorrelationId                                                 │
│  • Emit telemetry to Application Insights (C12)                                                             │
│                                                                                                             │
│  ENDPOINT 1: LOOKUP CORRELATION                                                                             │
│  ══════════════════════════════                                                                             │
│                                                                                                             │
│  INPUT (HTTP Request Body):                                                                                 │
│  ───────────────────────────                                                                                │
│  {                                                                                                          │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",   // Primary lookup key                       │
│    "tenderId": "TND-2026-00456",                                 // Fallback 1                              │
│    "shpnumRef": "ERP12345",                                      // Fallback 2                              │
│    "entityId": "THM"                                             // Fallback 2                              │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response — Found):                                                                            │
│  ────────────────────────────────                                                                           │
│  {                                                                                                          │
│    "found": true,                                                                                           │
│    "lookupMethod": "CorrelationGuid",                            // Which strategy succeeded                │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129",                                                        │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                               │
│    "tenderId": "TND-2026-00456",                                 // May be null if not yet linked           │
│    "shpNum": "ERP12345",                                                                                    │
│    "entityId": "THM",                                                                                       │
│    "status": "Active"                                                                                       │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response — Not Found):                                                                        │
│  ────────────────────────────────────                                                                       │
│  {                                                                                                          │
│    "found": false,                                                                                          │
│    "lookupMethod": null,                                                                                    │
│    "correlationId": null,                                                                                   │
│    "error": "No correlation found for provided identifiers",                                                │
│    "attemptedStrategies": [                                                                                 │
│      { "strategy": "CorrelationGuid", "result": "NotFound" },                                               │
│      { "strategy": "TenderId", "result": "NotFound" },                                                      │
│      { "strategy": "ShpNumRef", "result": "NotFound" }                                                      │
│    ]                                                                                                        │
│  }                                                                                                          │
│                                                                                                             │
│  LOOKUP LOGIC (MULTI-STRATEGY):                                                                             │
│  ──────────────────────────────                                                                             │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. Extract input parameters (correlationGuid, tenderId, shpnumRef, entityId)                      │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. STRATEGY 1: Lookup by correlationGuid (PRIMARY - FASTEST)                                      │    │
│  │      ═════════════════════════════════════════════════════                                         │    │
│  │      │                                                                                             │    │
│  │      ├──▶ IF correlationGuid provided:                                                            │    │
│  │      │      • Extract prefix: guid[0:8]                                                            │    │
│  │      │      • Query GuidLookup table:                                                              │    │
│  │      │        - PartitionKey = prefix ("550e8400")                                                 │    │
│  │      │        - RowKey = full GUID                                                                 │    │
│  │      │      • Performance: O(1) point query (~10-50ms)                                             │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ FOUND: Return CorrelationId, set lookupMethod = "CorrelationGuid"                │    │
│  │      │      │                                                                                      │    │
│  │      │      └──▶ NOT FOUND: Continue to Strategy 2                                                │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. STRATEGY 2: Lookup by tenderId (FALLBACK 1 - FAST)                                             │    │
│  │      ════════════════════════════════════════════════                                              │    │
│  │      │                                                                                             │    │
│  │      ├──▶ IF tenderId provided AND entityId provided:                                             │    │
│  │      │      • Query ThirdPartyLookup table:                                                        │    │
│  │      │        - PartitionKey = entityId ("THM")                                                    │    │
│  │      │        - RowKey = tenderId ("TND-2026-00456")                                               │    │
│  │      │      • Performance: O(1) point query (~10-50ms)                                             │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ FOUND: Return CorrelationId, set lookupMethod = "TenderId"                       │    │
│  │      │      │                                                                                      │    │
│  │      │      └──▶ NOT FOUND: Continue to Strategy 3                                                │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. STRATEGY 3: Lookup by shpnumRef + entityId (FALLBACK 2 - SLOWER)                               │    │
│  │      ═══════════════════════════════════════════════════════════════                               │    │
│  │      │                                                                                             │    │
│  │      ├──▶ IF shpnumRef provided AND entityId provided:                                            │    │
│  │      │      • Query CorrelationMapping table:                                                      │    │
│  │      │        - PartitionKey = entityId ("THM")                                                    │    │
│  │      │        - Filter: ShpNum == shpnumRef ("ERP12345")                                           │    │
│  │      │      • Performance: O(n) partition scan (~50-500ms depending on partition size)             │    │
│  │      │      • May return multiple results (if destination/duedate changed)                         │    │
│  │      │      • Select most recent (by CreatedTimestamp) with Status = "Active"                      │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ FOUND: Return CorrelationId, set lookupMethod = "ShpNumRef"                      │    │
│  │      │      │                                                                                      │    │
│  │      │      └──▶ NOT FOUND: Continue to final step                                                │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  5. ALL STRATEGIES FAILED                                                                          │    │
│  │      ═══════════════════════                                                                       │    │
│  │      • Log warning: "Correlation not found for inbound message"                                    │    │
│  │      • Emit telemetry with all attempted identifiers                                               │    │
│  │      • Return response: { found: false, error: "..." }                                             │    │
│  │      • Caller (C2) will send message to DLQ                                                        │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  ENDPOINT 2: LINK TENDER                                                                                    │
│  ═══════════════════════                                                                                    │
│                                                                                                             │
│  PURPOSE: Link tenderId to CorrelationId on first TENDER_ASSIGNED update                                    │
│                                                                                                             │
│  INPUT (HTTP Request Body):                                                                                 │
│  ───────────────────────────                                                                                │
│  {                                                                                                          │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                               │
│    "tenderId": "TND-2026-00456",                                                                            │
│    "entityId": "THM"                                                                                        │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response):                                                                                    │
│  ────────────────────────                                                                                   │
│  {                                                                                                          │
│    "success": true,                                                                                         │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129",                                                        │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                               │
│    "tenderId": "TND-2026-00456",                                                                            │
│    "linkedTimestamp": "2026-01-29T14:35:00.000Z"                                                            │
│  }                                                                                                          │
│                                                                                                             │
│  LINKING LOGIC:                                                                                             │
│  ──────────────                                                                                             │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. Lookup CorrelationId by correlationGuid (using GuidLookup table)                               │    │
│  │      • If not found: Return 404 Not Found error                                                    │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. Parse CorrelationId to extract entityId and composite key                                      │    │
│  │      • CorrelationId format: "THM-ERP12345-TOR-YVR-20260129"                                       │    │
│  │      • entityId = "THM" (first segment)                                                            │    │
│  │      • rowKey = "ERP12345-TOR-YVR-20260129" (remaining segments)                                   │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. Update CorrelationMapping table                                                                │    │
│  │      • Query existing record:                                                                      │    │
│  │        - PartitionKey = entityId                                                                   │    │
│  │        - RowKey = rowKey                                                                           │    │
│  │      • Set TenderId = tenderId (from request)                                                      │    │
│  │      • Set LastUpdatedTimestamp = UtcNow                                                           │    │
│  │      • Update entity in table                                                                      │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. Create ThirdPartyLookup entry (reverse lookup: tenderId → CorrelationId)                       │    │
│  │      • Insert new entity:                                                                          │    │
│  │        - PartitionKey = entityId                                                                   │    │
│  │        - RowKey = tenderId                                                                         │    │
│  │        - CorrelationId = correlationId                                                             │    │
│  │        - CorrelationGuid = correlationGuid                                                         │    │
│  │        - ShpNum = (from GuidLookup result)                                                         │    │
│  │        - LinkedTimestamp = UtcNow                                                                  │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  5. Log success and return response                                                                │    │
│  │      • Emit telemetry: "TenderLinked" event                                                        │    │
│  │      • Return { success: true, ... }                                                               │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY:                                                                                               │
│    - C2 (Inbound Poll Logic App) — lookup for each inbound update                                           │
│    - C2 (Inbound Poll Logic App) — link-tender on first TENDER_ASSIGNED                                     │
│  • CALLS:                                                                                                   │
│    - S1 (Table Storage) — GuidLookup, ThirdPartyLookup, CorrelationMapping tables                           │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Request telemetry: POST /api/correlation/lookup or /link-tender                                          │
│  • Custom dimensions:                                                                                       │
│    - CorrelationId (if found)                                                                               │
│    - CorrelationGuid                                                                                        │
│    - TenderId                                                                                               │
│    - LookupMethod (CorrelationGuid/TenderId/ShpNumRef)                                                      │
│    - Found (true/false)                                                                                     │
│  • Custom events:                                                                                           │
│    - "CorrelationLookupSuccess" / "CorrelationLookupFailed"                                                 │
│    - "TenderLinked" (when linking occurs)                                                                   │
│  • Performance metrics:                                                                                     │
│    - Lookup duration by strategy                                                                            │
│    - Percentage of lookups by each strategy                                                                 │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Strategy 1 (CorrelationGuid): ~10-50ms (point query)                                                     │
│  • Strategy 2 (TenderId): ~10-50ms (point query)                                                            │
│  • Strategy 3 (ShpNumRef): ~50-500ms (partition scan)                                                       │
│  • Link operation: ~50-150ms (2 table operations)                                                           │
│  • Concurrent execution: Supported (stateless function)                                                     │
│                                                                                                             │
│  ERROR HANDLING:                                                                                            │
│  ───────────────                                                                                            │
│  • Table Storage timeout: Return 503 Service Unavailable                                                    │
│  • Correlation not found: Return 404 Not Found (caller handles as orphan)                                   │
│  • Duplicate link attempt: Idempotent (update existing record, return success)                              │
│  • Invalid input: Return 400 Bad Request with validation errors                                             │
│                                                                                                             │
│  ORPHAN MESSAGE HANDLING:                                                                                   │
│  ────────────────────────                                                                                   │
│  When lookup fails (all strategies return not found):                                                       │
│  • C5 returns { found: false, error: "..." }                                                                │
│  • C2 receives 404 response                                                                                 │
│  • C2 logs error with full inbound message details                                                          │
│  • C2 sends message to DLQ (with DeadLetterReason: "CorrelationNotFound")                                   │
│  • C4 (Alert Handler) triggers on DLQ message                                                               │
│  • Teams notification sent to ERP Business Support                                                          │
│  • Manual investigation required (possible causes):                                                         │
│    - 3rd party sent update for shipment not submitted by this integration                                   │
│    - Data corruption in correlation tables                                                                  │
│    - Clock skew causing timing issues                                                                       │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

STEP 4: Schema Validator Function (C6) — Inbound Validation
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C6 — Schema Validator Function (Inbound Mode)                                                   │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Validate inbound XML from 3rd party against expected schema                                │
│  TYPE:           Azure Function (HTTP Trigger)                                                              │
│  RUNTIME:        .NET 8 Isolated Worker                                                                     │
│  ENDPOINT:       POST /api/validate/schema                                                                  │
│  AUTHENTICATION: Function key (managed in Key Vault)                                                        │
│                                                                                                             │
│  RESPONSIBILITIES (INBOUND):                                                                                │
│  ───────────────────────────                                                                                │
│  • Receive 3rd party XML from C2 (tendering update, appointment, invoice, delivery status)                  │
│  • Load appropriate XSD schema based on operationType:                                                      │
│    - TENDER_ASSIGNED → TenderingUpdate.xsd                                                                  │
│    - APPOINTMENT_SCHEDULED → AppointmentUpdate.xsd                                                          │
│    - INVOICE → InvoiceUpdate.xsd                                                                            │
│    - DELIVERED → DeliveryUpdate.xsd                                                                         │
│    - etc.                                                                                                   │
│  • Validate XML structure against schema                                                                    │
│  • Return validation result (valid/invalid) with error details                                              │
│  • Emit telemetry to Application Insights (C12)                                                             │
│                                                                                                             │
│  INPUT (HTTP Request Body):                                                                                 │
│  ───────────────────────────                                                                                │
│  {                                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129",                                                        │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                               │
│    "xmlPayload": "<update>...</update>",                                                                    │
│    "schemaType": "ThirdParty",                                                                              │
│    "operationType": "TENDER_ASSIGNED"                                                                       │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response — Success):                                                                          │
│  ───────────────────────────────────                                                                        │
│  {                                                                                                          │
│    "valid": true,                                                                                           │
│    "errors": [],                                                                                            │
│    "warnings": [],                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129"                                                         │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response — Failure):                                                                          │
│  ───────────────────────────────────                                                                        │
│  {                                                                                                          │
│    "valid": false,                                                                                          │
│    "errors": [                                                                                              │
│      {                                                                                                      │
│        "field": "tenderId",                                                                                 │
│        "message": "Element 'tenderId' is missing",                                                          │
│        "severity": "Error"                                                                                  │
│      },                                                                                                     │
│      {                                                                                                      │
│        "field": "shipmentCost",                                                                             │
│        "message": "Value 'abc' is not a valid decimal",                                                     │
│        "severity": "Error"                                                                                  │
│      }                                                                                                      │
│    ],                                                                                                       │
│    "warnings": [],                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129"                                                         │
│  }                                                                                                          │
│                                                                                                             │
│  VALIDATION CHECKS (INBOUND SPECIFIC):                                                                      │
│  ─────────────────────────────────────                                                                      │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  LAYER 1: XML Well-Formedness                                                                      │    │
│  │  • Valid XML syntax                                                                                │    │
│  │  • Proper encoding (UTF-8)                                                                         │    │
│  │                                                                                                    │    │
│  │  LAYER 2: XSD Schema Compliance (by operationType)                                                 │    │
│  │  ────────────────────────────────────────────────                                                  │    │
│  │                                                                                                    │    │
│  │  TENDER_ASSIGNED Schema:                                                                           │    │
│  │  • Required: entryId, correlationGuid, tenderId, operationType, shipmentCost, vendor               │    │
│  │  • Optional: estimatedPickup, vendorCode, currency                                                 │    │
│  │                                                                                                    │    │
│  │  APPOINTMENT_SCHEDULED Schema:                                                                     │    │
│  │  • Required: entryId, correlationGuid, tenderId, operationType, appointmentDate                    │    │
│  │  • Optional: appointmentWindow, appointmentType                                                    │    │
│  │                                                                                                    │    │
│  │  INVOICE Schema:                                                                                   │    │
│  │  • Required: entryId, correlationGuid, tenderId, operationType, invoiceNumber, invoiceAmount       │    │
│  │  • Optional: invoiceDate, dueDate, currency, paymentTerms                                          │    │
│  │                                                                                                    │    │
│  │  DELIVERED Schema:                                                                                 │    │
│  │  • Required: entryId, correlationGuid, tenderId, operationType, deliveryTimestamp                  │    │
│  │  • Optional: signedBy, proofOfDeliveryUrl                                                          │    │
│  │                                                                                                    │    │
│  │  LAYER 3: Data Type Validation                                                                     │    │
│  │  • Integers: entryId                                                                               │    │
│  │  • Decimals: shipmentCost, invoiceAmount                                                           │    │
│  │  • Dates: appointmentDate, invoiceDate, deliveryTimestamp (ISO 8601 format)                        │    │
│  │  • Strings: tenderId, vendor, invoiceNumber (max lengths)                                          │    │
│  │  • GUIDs: correlationGuid (valid UUID format)                                                      │    │
│  │                                                                                                    │    │
│  │  LAYER 4: Required Fields Enforcement                                                              │    │
│  │  • All required fields must be present and non-empty                                               │    │
│  │  • Schema-driven validation (XSD enforces requirements)                                            │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  SCHEMA SELECTION LOGIC:                                                                                    │
│  ───────────────────────                                                                                    │
│  Based on operationType from request:                                                                       │
│  • TENDER_ASSIGNED → Load "ThirdParty-TenderingUpdate.xsd"                                                  │
│  • APPOINTMENT_SCHEDULED → Load "ThirdParty-AppointmentUpdate.xsd"                                          │
│  • INVOICE → Load "ThirdParty-InvoiceUpdate.xsd"                                                            │
│  • DELIVERED → Load "ThirdParty-DeliveryUpdate.xsd"                                                         │
│  • EXCEPTION → Load "ThirdParty-ExceptionUpdate.xsd"                                                        │
│  • All schemas stored in Integration Account (S3)                                                           │
│  • Schemas cached in memory after first load                                                                │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C2 (Inbound Poll Logic App) — for each inbound update                                         │
│  • CALLS:                                                                                                   │
│    - S3 (Integration Account) — retrieves XSD schemas (cached)                                              │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Request telemetry: POST /api/validate/schema                                                             │
│  • Custom dimensions:                                                                                       │
│    - CorrelationId                                                                                          │
│    - CorrelationGuid                                                                                        │
│    - Direction: "Inbound"                                                                                   │
│    - OperationType                                                                                          │
│    - ValidationResult (Pass/Fail)                                                                           │
│    - ErrorCount                                                                                             │
│  • Performance metrics: Validation duration                                                                 │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Typical validation time: 50-200ms per message                                                            │
│  • Schema caching: XSD loaded once per operationType, reused                                                │
│  • Concurrent execution: Supported (stateless function)                                                     │
│                                                                                                             │
│  NOTE: Same function as outbound, different schemas and validation rules based on direction                 │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

STEP 5: Business Validator Function (C7) — Inbound Validation
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C7 — Business Validator Function (Inbound Mode)                                                 │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Validate business rules for inbound tendering updates                                      │
│  TYPE:           Azure Function (HTTP Trigger)                                                              │
│  RUNTIME:        .NET 8 Isolated Worker                                                                     │
│  ENDPOINT:       POST /api/validate/business                                                                │
│  AUTHENTICATION: Function key (managed in Key Vault)                                                        │
│                                                                                                             │
│  RESPONSIBILITIES (INBOUND):                                                                                │
│  ───────────────────────────                                                                                │
│  • Receive validated inbound update data from C2 (after schema validation passes)                           │
│  • Execute inbound-specific business rule validations:                                                      │
│    - Duplicate detection (entryId already processed?)                                                       │
│    - Cost validation (shipmentCost within expected range)                                                   │
│    - Date validation (invoiceDate, deliveryTimestamp not in future)                                         │
│    - Sequence validation (DELIVERED must come after TENDER_ASSIGNED)                                        │
│    - Vendor validation (vendor exists in approved vendor list)                                              │
│    - Currency validation (currency matches expected)                                                        │
│  • Return validation result (valid/invalid) with business error details                                     │
│  • Emit telemetry to Application Insights (C12)                                                             │
│                                                                                                             │
│  INPUT (HTTP Request Body):                                                                                 │
│  ───────────────────────────                                                                                │
│  {                                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129",                                                        │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                               │
│    "tenderId": "TND-2026-00456",                                                                            │
│    "updateData": {                                                                                          │
│      "entryId": 98765,                                                                                      │
│      "operationType": "TENDER_ASSIGNED",                                                                    │
│      "shipmentCost": 2500.00,                                                                               │
│      "currency": "CAD",                                                                                     │
│      "vendor": "FastFreight Inc",                                                                           │
│      "vendorCode": "FFI",                                                                                   │
│      "estimatedPickup": "2026-01-30T08:00:00Z",                                                             │
│      "createdDateTime": "2026-01-29T14:30:00Z"                                                              │
│    }                                                                                                        │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response — Success):                                                                          │
│  ───────────────────────────────────                                                                        │
│  {                                                                                                          │
│    "valid": true,                                                                                           │
│    "errors": [],                                                                                            │
│    "warnings": [                                                                                            │
│      {                                                                                                      │
│        "rule": "CostVariance",                                                                              │
│        "message": "Actual cost $2,500 is 10% higher than estimated $2,273",                                 │
│        "severity": "Warning"                                                                                │
│      }                                                                                                      │
│    ],                                                                                                       │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129"                                                         │
│  }                                                                                                          │
│                                                                                                             │
│  BUSINESS RULES IMPLEMENTED (INBOUND):                                                                      │
│  ─────────────────────────────────────                                                                      │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  RULE 1: Duplicate Detection (Idempotency)                                                         │    │
│  │  ───────────────────────────────────────                                                           │    │
│  │  • Query IdempotencyCheck table (S1)                                                               │    │
│  │  • Key: CorrelationId + Direction ("Inbound") + entryId                                            │    │
│  │  • If found: Return validation error "Duplicate message already processed"                         │    │
│  │  • If not found: Insert record to mark as processed                                                │    │
│  │                                                                                                    │    │
│  │  RULE 2: Cost Validation                                                                           │    │
│  │  ────────────────────                                                                              │    │
│  │  • shipmentCost must be > 0                                                                        │    │
│  │  • shipmentCost must be < $50,000 (maximum reasonable cost)                                        │    │
│  │  • Warning if cost variance > 20% from original estimated cost (if available)                      │    │
│  │                                                                                                    │    │
│  │  RULE 3: Date/Time Validation                                                                      │    │
│  │  ──────────────────────────                                                                        │    │
│  │  • estimatedPickup must be in future (for TENDER_ASSIGNED)                                         │    │
│  │  • deliveryTimestamp must be in past (for DELIVERED)                                               │    │
│  │  • invoiceDate must be <= today (for INVOICE)                                                      │    │
│  │  • createdDateTime must be within last 30 days (freshness check)                                   │    │
│  │                                                                                                    │    │
│  │  RULE 4: Sequence Validation                                                                       │    │
│  │  ────────────────────────                                                                          │    │
│  │  • Query MessageAuditLog (S1) for this CorrelationId                                               │    │
│  │  • Check lifecycle sequence is valid:                                                              │    │
│  │    - DELIVERED cannot occur before TENDER_ASSIGNED                                                 │    │
│  │    - INVOICE cannot occur before DELIVERED                                                         │    │
│  │    - APPOINTMENT_SCHEDULED should occur after TENDER_ASSIGNED                                      │    │
│  │  • Warning (not error) if out of sequence (3rd party may have timing issues)                       │    │
│  │                                                                                                    │    │
│  │  RULE 5: Vendor Validation                                                                         │    │
│  │  ────────────────────                                                                              │    │
│  │  • Query ApprovedVendors table (S1) for vendorCode                                                 │    │
│  │  • If not found: Warning "Vendor not in approved list"                                             │    │
│  │  • Not an error (3rd party may use new vendors)                                                    │    │
│  │                                                                                                    │    │
│  │  RULE 6: Currency Validation                                                                       │    │
│  │  ─────────────────────                                                                             │    │
│  │  • currency must be in allowed list (CAD, USD)                                                     │    │
│  │  • If CAD expected but USD received: Warning "Currency mismatch"                                   │    │
│  │                                                                                                    │    │
│  │  RULE 7: TenderId Consistency                                                                      │    │
│  │  ──────────────────────────                                                                        │    │
│  │  • If update has tenderId, it must match tenderId in CorrelationMapping (if already linked)        │    │
│  │  • Error if tenderId mismatch (data integrity issue)                                               │    │
│  │                                                                                                    │    │
│  │  RULE 8: EntryId Monotonic Increase                                                                │    │
│  │  ─────────────────────────────────                                                                 │    │
│  │  • entryId must be > last processed entryId for this CorrelationId                                 │    │
│  │  • Warning if entryId is out of sequence (possible replay or clock skew)                           │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C2 (Inbound Poll Logic App) — after C6 schema validation passes                               │
│  • CALLS:                                                                                                   │
│    - S1 (Table Storage) — IdempotencyCheck, MessageAuditLog, ApprovedVendors, CorrelationMapping           │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Request telemetry: POST /api/validate/business                                                           │
│  • Custom dimensions:                                                                                       │
│    - CorrelationId, CorrelationGuid, TenderId                                                               │
│    - Direction: "Inbound"                                                                                   │
│    - OperationType, EntryId                                                                                 │
│    - ValidationResult (Pass/Fail)                                                                           │
│    - FailedRules (comma-separated)                                                                          │
│  • Custom events: "InboundValidationFailed" (with rule details)                                             │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Typical validation time: 100-300ms per message                                                           │
│  • Reference data caching: ApprovedVendors cached in memory                                                 │
│  • Parallel rule execution: Independent rules execute concurrently                                          │
│                                                                                                             │
│  ERROR HANDLING:                                                                                            │
│  ───────────────                                                                                            │
│  • Duplicate detection failure (Table Storage timeout): Return 503, caller will retry                       │
│  • Reference data unavailable: Log warning, skip that rule, continue with others                            │
│  • Validation failure: Return 200 OK with { valid: false, errors: [...] }                                   │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

STEP 6: Transformer Function (C8) — Inbound Transformation
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C8 — Transformer Function (Inbound Mode)                                                        │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Transform 3rd Party XML to ERP XML using XSLT maps (reverse direction)                     │
│  TYPE:           Azure Function (HTTP Trigger)                                                              │
│  RUNTIME:        .NET 8 Isolated Worker                                                                     │
│  ENDPOINT:       POST /api/transform/inbound                                                                │
│  AUTHENTICATION: Function key (managed in Key Vault)                                                        │
│                                                                                                             │
│  RESPONSIBILITIES (INBOUND):                                                                                │
│  ───────────────────────────                                                                                │
│  • Receive validated 3rd party XML from C2 (passed schema + business validation)                            │
│  • Retrieve XSLT map from Integration Account (S3) based on operationType                                   │
│  • Apply XSLT transformation:                                                                               │
│    - Map 3rd party field names to ERP field names                                                           │
│    - Convert date formats (ISO 8601 → YYYYMMDD if needed)                                                   │
│    - Convert currency (if needed)                                                                           │
│    - Inject CorrelationId for ERP tracking                                                                  │
│    - Map operationType to ERP-specific update type codes                                                    │
│  • Return transformed ERP XML payload                                                                       │
│  • Store both source and transformed payloads to Blob Storage (S2) for audit                                │
│  • Emit telemetry to Application Insights (C12)                                                             │
│                                                                                                             │
│  INPUT (HTTP Request Body):                                                                                 │
│  ───────────────────────────                                                                                │
│  {                                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129",                                                        │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                               │
│    "tenderId": "TND-2026-00456",                                                                            │
│    "direction": "Inbound",                                                                                  │
│    "operationType": "TENDER_ASSIGNED",                                                                      │
│    "sourceXml": "<update>                                                                                   │
│                    <entryId>98765</entryId>                                                                 │
│                    <correlationGuid>550e8400...</correlationGuid>                                           │
│                    <tenderId>TND-2026-00456</tenderId>                                                      │
│                    <shpnumRef>ERP12345</shpnumRef>                                                          │
│                    <entityId>THM</entityId>                                                                 │
│                    <operationType>TENDER_ASSIGNED</operationType>                                           │
│                    <shipmentCost>2500.00</shipmentCost>                                                     │
│                    <currency>CAD</currency>                                                                 │
│                    <vendor>FastFreight Inc</vendor>                                                         │
│                    <vendorCode>FFI</vendorCode>                                                             │
│                    <estimatedPickup>2026-01-30T08:00:00Z</estimatedPickup>                                  │
│                  </update>"                                                                                 │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response):                                                                                    │
│  ────────────────────────                                                                                   │
│  {                                                                                                          │
│    "success": true,                                                                                         │
│    "transformedXml": "<ShipmentUpdate>                                                                      │
│                         <CorrelationId>THM-ERP12345-TOR-YVR-20260129</CorrelationId>                        │
│                         <ShpNum>ERP12345</ShpNum>                                                           │
│                         <EntityId>THM</EntityId>                                                            │
│                         <UpdateType>TENDER_ASSIGNED</UpdateType>                                            │
│                         <TenderId>TND-2026-00456</TenderId>                                                 │
│                         <Cost>2500.00</Cost>                                                                │
│                         <Currency>CAD</Currency>                                                            │
│                         <Carrier>FastFreight Inc</Carrier>                                                  │
│                         <CarrierCode>FFI</CarrierCode>                                                      │
│                         <PickupDate>20260130</PickupDate>                                                   │
│                         <PickupTime>08:00</PickupTime>                                                      │
│                       </ShipmentUpdate>",                                                                   │
│    "sourcePayloadPath": "/audit/2026/01/29/THM-ERP12345-TOR-YVR-20260129/inbound/143500Z-source.xml",      │
│    "transformedPayloadPath": "/audit/2026/01/29/THM-ERP12345-TOR-YVR-20260129/inbound/143500Z-transformed.xml",│
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129"                                                         │
│  }                                                                                                          │
│                                                                                                             │
│  TRANSFORMATION MAPPINGS (INBOUND):                                                                         │
│  ──────────────────────────────────                                                                         │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  3rd Party XML (Source)               ERP XML (Target)                                             │    │
│  │  ═══════════════════════               ══════════════                                              │    │
│  │                                                                                                    │    │
│  │  <update>                      ───▶   <ShipmentUpdate>                                            │    │
│  │                                                                                                    │    │
│  │  (injected by function)        ───▶     <CorrelationId>                                           │    │
│  │                                           THM-ERP12345-TOR-YVR-20260129                            │    │
│  │                                         </CorrelationId>                                           │    │
│  │                                                                                                    │    │
│  │  <shpnumRef>ERP12345</shpnumRef> ───▶   <ShpNum>ERP12345</ShpNum>                                 │    │
│  │                                                                                                    │    │
│  │  <entityId>THM</entityId>      ───▶     <EntityId>THM</EntityId>                                  │    │
│  │                                                                                                    │    │
│  │  <operationType>               ───▶     <UpdateType>                                              │    │
│  │    TENDER_ASSIGNED                        TENDER_ASSIGNED                                          │    │
│  │  </operationType>                       </UpdateType>                                              │    │
│  │                                                                                                    │    │
│  │  <tenderId>TND-2026-00456      ───▶     <TenderId>                                                │    │
│  │  </tenderId>                              TND-2026-00456                                           │    │
│  │                                         </TenderId>                                                │    │
│  │                                                                                                    │    │
│  │  <shipmentCost>2500.00         ───▶     <Cost>2500.00</Cost>                                      │    │
│  │  </shipmentCost>                        <Currency>CAD</Currency>                                   │    │
│  │  <currency>CAD</currency>                                                                          │    │
│  │                                                                                                    │    │
│  │  <vendor>FastFreight Inc       ───▶     <Carrier>FastFreight Inc</Carrier>                        │    │
│  │  </vendor>                              <CarrierCode>FFI</CarrierCode>                             │    │
│  │  <vendorCode>FFI</vendorCode>                                                                      │    │
│  │                                                                                                    │    │
│  │  <estimatedPickup>             ───▶     <PickupDate>20260130</PickupDate>                         │    │
│  │    2026-01-30T08:00:00Z                 <PickupTime>08:00</PickupTime>                            │    │
│  │  </estimatedPickup>                                                                                │    │
│  │                                                                                                    │    │
│  │  <correlationGuid>550e8400...  ───▶     (Omitted in ERP XML - not needed)                         │    │
│  │  </correlationGuid>                                                                                │    │
│  │                                                                                                    │    │
│  │  <entryId>98765</entryId>      ───▶     (Stored in audit, not in ERP XML)                         │    │
│  │                                                                                                    │    │
│  │  </update>                     ───▶   </ShipmentUpdate>                                           │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  XSLT MAP SELECTION (BY OPERATION TYPE):                                                                    │
│  ───────────────────────────────────────                                                                    │
│  • TENDER_ASSIGNED → "ThirdParty-to-ERP-TenderAssigned.xslt"                                                │
│  • APPOINTMENT_SCHEDULED → "ThirdParty-to-ERP-Appointment.xslt"                                             │
│  • INVOICE → "ThirdParty-to-ERP-Invoice.xslt"                                                               │
│  • DELIVERED → "ThirdParty-to-ERP-Delivery.xslt"                                                            │
│  • EXCEPTION → "ThirdParty-to-ERP-Exception.xslt"                                                           │
│  • All maps stored in Integration Account (S3)                                                              │
│  • Cached in memory after first load                                                                        │
│                                                                                                             │
│  PROCESSING LOGIC (INBOUND):                                                                                │
│  ───────────────────────────                                                                                │
│  1. Extract input parameters (sourceXml, correlationId, operationType)                                      │
│  2. Retrieve XSLT map from Integration Account based on operationType                                       │
│  3. Load sourceXml into XmlDocument                                                                         │
│  4. Create XSLT parameters (correlationId, tenderId, current date)                                          │
│  5. Apply XSLT transformation                                                                               │
│  6. Store source payload to Blob Storage (S2):                                                              │
│     Path: /audit/{YYYY}/{MM}/{DD}/{CorrelationId}/inbound/{timestamp}-source.xml                            │
│  7. Store transformed payload to Blob Storage (S2):                                                         │
│     Path: /audit/{YYYY}/{MM}/{DD}/{CorrelationId}/inbound/{timestamp}-transformed.xml                       │
│  8. Log telemetry to Application Insights                                                                   │
│  9. Return response with transformedXml                                                                     │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C2 (Inbound Poll Logic App) — after C7 business validation passes                             │
│  • CALLS:                                                                                                   │
│    - S3 (Integration Account) — retrieves XSLT map (cached)                                                 │
│    - S2 (Blob Storage) — stores source and transformed payloads                                             │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Request telemetry: POST /api/transform/inbound                                                           │
│  • Custom dimensions:                                                                                       │
│    - CorrelationId, CorrelationGuid, TenderId                                                               │
│    - Direction: "Inbound"                                                                                   │
│    - OperationType                                                                                          │
│    - SourcePayloadSize, TransformedPayloadSize                                                              │
│  • Dependency telemetry: Integration Account, Blob Storage                                                  │
│  • Custom metrics: TransformationDuration                                                                   │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Typical transformation time: 50-150ms per message                                                        │
│  • XSLT map caching: Loaded once per operationType, compiled, reused                                        │
│  • Blob Storage write: Asynchronous                                                                         │
│  • Concurrent execution: Supported                                                                          │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘


STEP 7: Service Bus — Inbound Topic (C10) — Message Queue
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C10 — Service Bus (Inbound Topic)                                                               │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Message queue decoupling C2 (polling/validation) from C3B (batch processing/ERP delivery)  │
│  TYPE:           Azure Service Bus Topic with Subscriptions                                                 │
│  NAMESPACE:      sb-shipment-integration                                                                    │
│  TOPIC NAME:     inbound-tendering                                                                          │
│  PROTOCOL:       AMQP 1.0 / HTTPS                                                                           │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Receive transformed ERP XML messages from C2 (via C8)                                                    │
│  • Queue messages for batch processing by C3B                                                               │
│  • Provide durable storage (messages persisted until consumed)                                              │
│  • Enable decoupling (C2 can continue polling even if C3B is temporarily unavailable)                       │
│  • Support message filtering via subscriptions (by operationType)                                           │
│  • Handle dead-letter scenarios (messages that fail processing)                                             │
│  • Emit metrics to Application Insights (C12)                                                               │
│                                                                                                             │
│  TOPIC CONFIGURATION:                                                                                       │
│  ───────────────────                                                                                        │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  PROPERTY                           VALUE                                                          │    │
│  │  ════════                           ═════                                                          │    │
│  │                                                                                                    │    │
│  │  Max Size:                          5 GB                                                           │    │
│  │  Message TTL (Time-to-Live):        7 days (default), 30 minutes (inbound-tendering)              │    │
│  │  Max Delivery Count:                10 (after 10 failed deliveries, move to DLQ)                  │    │
│  │  Duplicate Detection:               Enabled (5-minute window)                                      │    │
│  │  Duplicate Detection ID:            CorrelationId + Direction + EntryId                            │    │
│  │  Partitioning:                      Disabled (not needed for this volume)                         │    │
│  │  Dead-Letter Queue:                 Enabled                                                        │    │
│  │  Lock Duration:                     5 minutes (time to process message before lock expires)       │    │
│  │  Enable Sessions:                   No (no ordering requirement within shipment)                   │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  SUBSCRIPTIONS:                                                                                             │
│  ──────────────                                                                                             │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  SUBSCRIPTION 1: batch-processor-inbound                                                           │    │
│  │  ═══════════════════════════════════════                                                           │    │
│  │  • Filter: (No filter - processes all inbound messages)                                            │    │
│  │  • Max Delivery Count: 5 (after 5 attempts, move to DLQ)                                           │    │
│  │  • Lock Duration: 5 minutes                                                                        │    │
│  │  • Consumer: C3B (Batch Processing Logic App - Inbound)                                            │    │
│  │  • Batch Size: 25 messages per trigger                                                             │    │
│  │  • Description: Processes all inbound tendering updates in batches                                 │    │
│  │                                                                                                    │    │
│  │  SUBSCRIPTION 2: critical-updates (Optional - for future prioritization)                           │    │
│  │  ═══════════════════════════════════════════════════════════                                       │    │
│  │  • Filter: operationType IN ('EXCEPTION', 'DELIVERED')                                             │    │
│  │  • Use Case: Higher priority processing for critical updates                                       │    │
│  │  • Currently: Not actively used, placeholder for future                                            │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  MESSAGE STRUCTURE:                                                                                         │
│  ─────────────────                                                                                          │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  MESSAGE PROPERTIES (Metadata):                                                                    │    │
│  │  ═════════════════════════════                                                                     │    │
│  │  {                                                                                                 │    │
│  │    "MessageId": "98765",                                                  // EntryId from 3rd party│    │
│  │    "CorrelationId": "THM-ERP12345-TOR-YVR-20260129",                      // For distributed trace │    │
│  │    "ApplicationProperties": {                                                                      │    │
│  │      "CorrelationId": "THM-ERP12345-TOR-YVR-20260129",                                            │    │
│  │      "CorrelationGuid": "550e8400-e29b-41d4-a716-446655440000",                                   │    │
│  │      "TenderId": "TND-2026-00456",                                                                │    │
│  │      "OperationType": "TENDER_ASSIGNED",                                                          │    │
│  │      "Direction": "Inbound",                                                                      │    │
│  │      "EntityId": "THM",                                                                           │    │
│  │      "ShpNum": "ERP12345",                                                                        │    │
│  │      "EntryId": "98765",                                                                          │    │
│  │      "EnqueuedTimeUtc": "2026-01-29T14:35:10.000Z",                                               │    │
│  │      "SourceTimestamp": "2026-01-29T14:30:00.000Z"             // 3rd party createdDateTime       │    │
│  │    },                                                                                             │    │
│  │    "ContentType": "application/xml",                                                              │    │
│  │    "TimeToLive": "00:30:00"                                    // 30 minutes                       │    │
│  │  }                                                                                                │    │
│  │                                                                                                    │    │
│  │  MESSAGE BODY:                                                                                     │    │
│  │  ════════════                                                                                      │    │
│  │  (Base64-encoded transformed ERP XML from C8)                                                      │    │
│  │                                                                                                    │    │
│  │  <ShipmentUpdate>                                                                                  │    │
│  │    <CorrelationId>THM-ERP12345-TOR-YVR-20260129</CorrelationId>                                    │    │
│  │    <ShpNum>ERP12345</ShpNum>                                                                       │    │
│  │    <EntityId>THM</EntityId>                                                                        │    │
│  │    <UpdateType>TENDER_ASSIGNED</UpdateType>                                                        │    │
│  │    <TenderId>TND-2026-00456</TenderId>                                                             │    │
│  │    <Cost>2500.00</Cost>                                                                            │    │
│  │    <Currency>CAD</Currency>                                                                        │    │
│  │    <Carrier>FastFreight Inc</Carrier>                                                              │    │
│  │    <CarrierCode>FFI</CarrierCode>                                                                  │    │
│  │    <PickupDate>20260130</PickupDate>                                                               │    │
│  │    <PickupTime>08:00</PickupTime>                                                                  │    │
│  │  </ShipmentUpdate>                                                                                 │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  MESSAGE PUBLISHING (from C2):                                                                              │
│  ────────────────────────────                                                                               │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. C2 calls C8 (Transformer) and receives transformedXml                                          │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. C2 creates Service Bus message:                                                                │    │
│  │      • MessageId = EntryId (from 3rd party update)                                                 │    │
│  │      • CorrelationId = CorrelationId (for distributed tracing)                                     │    │
│  │      • ApplicationProperties = { CorrelationId, CorrelationGuid, TenderId, OperationType,         │    │
│  │                                  Direction, EntryId, EntityId, ShpNum }                            │    │
│  │      • Body = Base64(transformedXml)                                                               │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. C2 publishes message to Topic "inbound-tendering"                                              │    │
│  │      • Service Bus SDK handles:                                                                    │    │
│  │        - Connection pooling                                                                        │    │
│  │        - Automatic retry (3 attempts)                                                              │    │
│  │        - Duplicate detection (based on MessageId = EntryId)                                        │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. Service Bus confirms message accepted                                                          │    │
│  │      • C2 continues to next update                                                                 │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  MESSAGE CONSUMPTION (by C3B):                                                                              │
│  ────────────────────────────                                                                               │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. C3B (Batch Processing Logic App - Inbound) triggers when messages available                    │    │
│  │      • Subscription: batch-processor-inbound                                                       │    │
│  │      • Max Message Count: 25 (batching)                                                            │    │
│  │      • Peek-Lock mode (message locked until C3B completes or abandons)                             │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. C3B receives batch of up to 25 messages                                                        │    │
│  │      • Each message includes properties + body                                                     │    │
│  │      • Lock acquired (5-minute timeout)                                                            │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. C3B processes each message:                                                                    │    │
│  │      • Extracts CorrelationId, TenderId, EntryId from properties                                   │    │
│  │      • Decodes ERP XML body from Base64                                                            │    │
│  │      • Calls ERP REST API (E1) via Azure Relay (E3)                                                │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. For each message:                                                                              │    │
│  │      │                                                                                             │    │
│  │      ├──▶ SUCCESS: Complete message (removed from queue)                                          │    │
│  │      │                                                                                             │    │
│  │      └──▶ FAILURE: Abandon message                                                                │    │
│  │           • Message returns to queue (delivery count++)                                            │    │
│  │           • If delivery count > Max (5), moved to DLQ                                              │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  DEAD-LETTER QUEUE (DLQ):                                                                                   │
│  ────────────────────────                                                                                   │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  TRIGGERS FOR DLQ:                                                                                 │    │
│  │  • Max delivery count exceeded (5 failed attempts)                                                 │    │
│  │  • Message TTL expired (30 minutes without consumption)                                            │    │
│  │  • Explicit dead-letter by C3B (validation failure after dequeue)                                  │    │
│  │                                                                                                    │    │
│  │  DLQ MESSAGE PROPERTIES:                                                                           │    │
│  │  • DeadLetterReason: "MaxDeliveryCountExceeded" or "TTLExpired" or "ERPApiFailure"                │    │
│  │  • DeadLetterErrorDescription: Detailed error message                                              │    │
│  │  • EnqueuedTimeUtc: Original enqueue time                                                          │    │
│  │  • DeadLetterTimeUtc: When moved to DLQ                                                            │    │
│  │                                                                                                    │    │
│  │  DLQ CONSUMER:                                                                                     │    │
│  │  • C4 (Alert Handler Logic App) monitors DLQ                                                       │    │
│  │  • Triggers on new DLQ message                                                                     │    │
│  │  • Sends Teams notification to ERP Business Support                                                │    │
│  │  • Logs detailed error to Application Insights (C12)                                               │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  DUPLICATE DETECTION:                                                                                       │
│  ────────────────────                                                                                       │
│  • Uses MessageId (EntryId) as duplicate detection key                                                      │
│  • 5-minute detection window                                                                                │
│  • Prevents processing same entryId multiple times if C2 retries poll                                       │
│  • Example: If entryId 98765 already in queue, duplicate is dropped silently                                │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY (Publisher):                                                                                   │
│    - C2 (Inbound Poll Logic App) — publishes messages after transformation                                  │
│  • CALLS (Consumer):                                                                                        │
│    - C3B (Batch Processing Logic App - Inbound) — triggers on message availability                          │
│    - C4 (Alert Handler Logic App) — triggers on DLQ messages                                                │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Metrics emitted automatically:                                                                           │
│    - Incoming Messages (count)                                                                              │
│    - Outgoing Messages (count)                                                                              │
│    - Messages in Queue (active count)                                                                       │
│    - Dead-lettered Messages (count)                                                                         │
│    - Message Size (bytes)                                                                                   │
│  • Operational logs (diagnostic settings):                                                                  │
│    - Message send/receive events                                                                            │
│    - Dead-letter events                                                                                     │
│                                                                                                             │
│  PERFORMANCE & SCALE:                                                                                       │
│  ───────────────────                                                                                        │
│  • Message throughput: 2,000+ messages/second (Premium tier)                                                │
│  • Current load: ~500 updates/hour (~0.14 messages/second) — well within capacity                           │
│  • Latency: < 1ms (message enqueue to available for consumption)                                            │
│  • Storage: 5 GB capacity                                                                                   │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

STEP 8: Batch Processing Logic App — Inbound (C3B)
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C3B — Batch Processing Logic App (Inbound)                                                      │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Orchestrator — Consume inbound messages from Service Bus and deliver to on-premise ERP     │
│  TYPE:           Logic App (Standard or Consumption)                                                        │
│  TRIGGER:        Service Bus Topic Subscription (batch-processor-inbound)                                   │
│  BATCH SIZE:     25 messages per trigger                                                                    │
│  CONCURRENCY:    Sequential per batch (to maintain order within batch, if needed)                           │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Trigger when messages available in Service Bus subscription "batch-processor-inbound"                    │
│  • Receive batch of up to 25 messages                                                                       │
│  • For each message in batch:                                                                               │
│    - Extract CorrelationId, TenderId, EntryId from message properties                                       │
│    - Decode ERP XML body from Base64                                                                        │
│    - Call on-premise ERP REST API via Azure Relay (E3)                                                      │
│    - Handle response from ERP:                                                                              │
│      • SUCCESS (200/201): Complete Service Bus message, store response to Blob (S2), update audit           │
│      • FAILURE (4xx/5xx): Log error, abandon message (will retry or DLQ)                                    │
│  • Emit telemetry to Application Insights (C12)                                                             │
│  • Update MessageAuditLog table (S1) with processing status                                                 │
│                                                                                                             │
│  WORKFLOW STRUCTURE:                                                                                        │
│  ──────────────────                                                                                         │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. TRIGGER: Service Bus Topic Subscription                                                        │    │
│  │      • Topic: inbound-tendering                                                                    │    │
│  │      • Subscription: batch-processor-inbound                                                       │    │
│  │      • Max Message Count: 25                                                                       │    │
│  │      • Is Sessions Enabled: No                                                                     │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. ACTION: Parse Service Bus Messages                                                             │    │
│  │      • Input: triggerBody() — array of messages                                                    │    │
│  │      • Extract message count                                                                       │    │
│  │      • Log: "Received {count} inbound messages in batch"                                           │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. ACTION: For Each Message                                                                       │    │
│  │      • Items: @triggerBody()                                                                       │    │
│  │      • Run in Parallel: No (sequential processing to avoid overwhelming on-premise ERP)            │    │
│  │      │                                                                                             │    │
│  │      │                                                                                             │    │
│  │      ├──▶ 3.1: SCOPE: Process Single Message (for error handling)                                 │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 3.1.1: Compose — Extract Message Properties                                      │    │
│  │      │      │      • correlationId = @{items('For_Each')?['ApplicationProperties']?[              │    │
│  │      │      │                            'CorrelationId']}                                         │    │
│  │      │      │      • correlationGuid = @{items('For_Each')?['ApplicationProperties']?[            │    │
│  │      │      │                              'CorrelationGuid']}                                     │    │
│  │      │      │      • tenderId = @{items('For_Each')?['ApplicationProperties']?['TenderId']}       │    │
│  │      │      │      • operationType = @{items('For_Each')?['ApplicationProperties']?[              │    │
│  │      │      │                           'OperationType']}                                          │    │
│  │      │      │      • entryId = @{items('For_Each')?['ApplicationProperties']?['EntryId']}         │    │
│  │      │      │      • entityId = @{items('For_Each')?['ApplicationProperties']?['EntityId']}       │    │
│  │      │      │      • shpNum = @{items('For_Each')?['ApplicationProperties']?['ShpNum']}           │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 3.1.2: Compose — Decode XML Body                                                 │    │
│  │      │      │      • erpXml = @{base64ToString(items('For_Each')?['ContentData'])}                │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 3.1.3: HTTP — Call ERP REST API via Azure Relay                                  │    │
│  │      │      │      • Method: POST                                                                  │    │
│  │      │      │      • URI: https://{relay-name}.servicebus.windows.net/erp/api/shipments/update    │    │
│  │      │      │      • Authentication: Managed Identity (to Relay) + API Key (to ERP)               │    │
│  │      │      │      • Headers:                                                                      │    │
│  │      │      │        {                                                                             │    │
│  │      │      │          "Content-Type": "application/xml",                                          │    │
│  │      │      │          "X-CorrelationId": "@{outputs('Extract_Properties')?['correlationId']}",   │    │
│  │      │      │          "X-TenderId": "@{outputs('Extract_Properties')?['tenderId']}",             │    │
│  │      │      │          "X-EntryId": "@{outputs('Extract_Properties')?['entryId']}",               │    │
│  │      │      │          "X-API-Key": "@{parameters('ErpApiKey')}"                                  │    │
│  │      │      │        }                                                                             │    │
│  │      │      │      • Body: @{outputs('Decode_XML')?['erpXml']}                                     │    │
│  │      │      │      • Retry Policy: Fixed interval, 3 attempts, 5 seconds interval                  │    │
│  │      │      │      • Timeout: 30 seconds                                                           │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 3.1.4: CONDITION — Check HTTP Response Status                                    │    │
│  │      │      │      • Expression: @or(                                                              │    │
│  │      │      │                      equals(outputs('HTTP')?['statusCode'], 200),                    │    │
│  │      │      │                      equals(outputs('HTTP')?['statusCode'], 201)                     │    │
│  │      │      │                    )                                                                 │    │
│  │      │      │      │                                                                               │    │
│  │      │      │      ├──▶ TRUE (Success):                                                           │    │
│  │      │      │      │      │                                                                        │    │
│  │      │      │      │      ├──▶ Store Response to Blob Storage (S2)                                │    │
│  │      │      │      │      │      • Container: shipment-audit                                       │    │
│  │      │      │      │      │      • Path: /audit/{YYYY}/{MM}/{DD}/{CorrelationId}/inbound/         │    │
│  │      │      │      │      │               {timestamp}-response.xml                                 │    │
│  │      │      │      │      │      • Content: @{body('HTTP')}                                        │    │
│  │      │      │      │      │      • Metadata: { correlationId, tenderId, entryId, statusCode }     │    │
│  │      │      │      │      │                                                                        │    │
│  │      │      │      │      ├──▶ Update MessageAuditLog (S1)                                        │    │
│  │      │      │      │      │      • PartitionKey: CorrelationId                                     │    │
│  │      │      │      │      │      • RowKey: {Timestamp}-Inbound-{OperationType}                    │    │
│  │      │      │      │      │      • Properties: { Status: "Success", StatusCode, Duration,         │    │
│  │      │      │      │      │                      TenderId, EntryId, ... }                         │    │
│  │      │      │      │      │                                                                        │    │
│  │      │      │      │      ├──▶ Log Success to Application Insights (C12)                          │    │
│  │      │      │      │      │      • Custom Event: "InboundMessageSuccess"                           │    │
│  │      │      │      │      │      • Properties: { CorrelationId, TenderId, EntryId, OperationType }│    │
│  │      │      │      │      │                                                                        │    │
│  │      │      │      │      └──▶ Complete Service Bus Message                                       │    │
│  │      │      │      │           • Message removed from queue                                        │    │
│  │      │      │      │           • Delivery count NOT incremented                                    │    │
│  │      │      │      │                                                                               │    │
│  │      │      │      └──▶ FALSE (Failure):                                                          │    │
│  │      │      │           │                                                                          │    │
│  │      │      │           ├──▶ Log Error to Application Insights (C12)                              │    │
│  │      │      │           │      • Custom Event: "InboundMessageFailed"                              │    │
│  │      │      │           │      • Properties: { CorrelationId, StatusCode, ErrorMessage }          │    │
│  │      │      │           │      • Severity: Error                                                   │    │
│  │      │      │           │                                                                          │    │
│  │      │      │           ├──▶ Update MessageAuditLog (S1)                                          │    │
│  │      │      │           │      • Status: "Failed"                                                  │    │
│  │      │      │           │      • ErrorMessage: Response body                                       │    │
│  │      │      │           │                                                                          │    │
│  │      │      │           └──▶ Abandon Service Bus Message                                          │    │
│  │      │      │                • Message returns to queue                                            │    │
│  │      │      │                • Delivery count incremented                                          │    │
│  │      │      │                • Will retry (up to 5 times total)                                    │    │
│  │      │      │                • After 5 failures → DLQ (triggers C4)                                │    │
│  │      │      │                                                                                      │    │
│  │      │      └──▶ 3.1.5: CATCH (Scope Error Handling)                                             │    │
│  │      │           • If any action in scope fails (HTTP timeout, network error, etc.):              │    │
│  │      │           • Log Exception to Application Insights                                          │    │
│  │      │           • Abandon Service Bus Message                                                    │    │
│  │      │           • Continue with next message in batch                                            │    │
│  │      │                                                                                             │    │
│  │      └──▶ (End For Each)                                                                          │    │
│  │           │                                                                                         │    │
│  │           ▼                                                                                         │    │
│  │  4. ACTION: Log Batch Completion                                                                   │    │
│  │      • Custom Event: "InboundBatchProcessingCompleted"                                             │    │
│  │      • Properties:                                                                                 │    │
│  │        - BatchSize: {count}                                                                        │    │
│  │        - SuccessCount: {calculated}                                                                │    │
│  │        - FailureCount: {calculated}                                                                │    │
│  │        - Duration: {workflow duration}                                                             │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  5. END                                                                                            │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • TRIGGERED BY: C10 (Service Bus — Inbound Topic, subscription: batch-processor-inbound)                   │
│  • CALLS:                                                                                                   │
│    - E3 (Azure Relay / Hybrid Connection) → E1 (ERP REST API)                                               │
│    - S2 (Blob Storage) — stores response payloads                                                           │
│    - S1 (Table Storage) — updates MessageAuditLog                                                           │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Workflow execution telemetry (automatic):                                                                │
│    - WorkflowRunStarted, WorkflowRunCompleted, Duration                                                     │
│  • Tracked properties:                                                                                      │
│    - BatchSize, SuccessCount, FailureCount                                                                  │
│  • Custom events:                                                                                           │
│    - "InboundMessageSuccess" (per message)                                                                  │
│    - "InboundMessageFailed" (per message)                                                                   │
│    - "InboundBatchProcessingCompleted" (per batch)                                                          │
│  • HTTP dependency tracking:                                                                                │
│    - Calls to E1 (ERP REST API via E3 Relay)                                                                │
│    - Duration, status code, success/failure                                                                 │
│                                                                                                             │
│  ERROR HANDLING STRATEGY:                                                                                   │
│  ────────────────────────                                                                                   │
│  • Per-message error handling (not per-batch)                                                               │
│  • If message 5 fails, messages 1-4 are completed, messages 6-25 continue                                   │
│  • Failed message is abandoned (returned to queue for retry)                                                │
│  • Retry flow:                                                                                              │
│    1. Message abandoned → returns to Service Bus queue                                                      │
│    2. Delivery count incremented (was 1, now 2)                                                             │
│    3. Message becomes available again for next batch                                                        │
│    4. C3B triggers again, processes message again                                                           │
│    5. If fails again → repeat up to Max Delivery Count (5)                                                  │
│    6. After 5th failure → Message moved to DLQ                                                              │
│    7. DLQ triggers C4 (Alert Handler) → Teams notification                                                  │
│                                                                                                             │
│  PERFORMANCE & SCALE:                                                                                       │
│  ───────────────────                                                                                        │
│  • Batch size: 25 messages (configurable)                                                                   │
│  • Processing time per message: ~1-3 seconds (HTTP call to ERP via Relay)                                   │
│  • Total batch processing time: ~25-75 seconds (sequential)                                                 │
│  • Throughput: ~1 batch per minute = 25 messages/minute = 1,500 messages/hour                               │
│  • Current load: 500 updates/hour (33% of capacity)                                                         │
│                                                                                                             │
│  DIFFERENCES FROM OUTBOUND (C3):                                                                            │
│  ───────────────────────────────                                                                            │
│  • Destination: On-premise ERP (via Relay) instead of 3rd Party API (via APIM)                              │
│  • No API Management layer (direct to Relay)                                                                │
│  • Authentication: Managed Identity to Relay, API Key to ERP                                                │
│  • Retry: Logic App handles retry (not offloaded to gateway like APIM)                                      │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

STEP 9: Azure Relay / Hybrid Connection (E3)
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: E3 — Azure Relay / Hybrid Connection                                                            │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Secure bridge between Azure (C3B) and on-premise ERP REST API (E1)                         │
│  TYPE:           Azure Relay Hybrid Connection                                                              │
│  NAMESPACE:      relay-shipment-integration.servicebus.windows.net                                          │
│  CONNECTION:     erp-api-connection                                                                         │
│  PROTOCOL:       HTTPS over WebSockets (outbound from on-premise)                                           │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Establish secure tunnel from Azure Logic Apps to on-premise ERP REST API                                 │
│  • Relay HTTP requests from C3B to E1                                                                       │
│  • Return HTTP responses from E1 to C3B                                                                     │
│  • Handle connection management and keep-alive                                                              │
│  • Encrypt data in transit (TLS 1.2+)                                                                       │
│  • Authenticate both sides (Azure → Relay via Managed Identity, On-premise → Relay via SAS token)           │
│                                                                                                             │
│  ARCHITECTURE:                                                                                              │
│  ─────────────                                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │   AZURE CLOUD                         AZURE RELAY                     ON-PREMISE NETWORK          │    │
│  │   ═══════════                         ═══════════                     ══════════════════          │    │
│  │                                                                                                    │    │
│  │   ┌──────────────┐                 ┌────────────────┐                 ┌──────────────┐           │    │
│  │   │  C3B         │                 │  Azure Relay   │                 │  Hybrid      │           │    │
│  │   │  (Logic App) │  ───────────▶   │  Namespace     │   ◀──────────   │  Connection  │           │    │
│  │   │              │  HTTPS Request  │                │   WebSocket     │  Manager     │           │    │
│  │   │              │  (Managed ID)   │  Hybrid Conn:  │   (SAS Token)   │  (Listener)  │           │    │
│  │   │              │                 │  erp-api-conn  │                 │              │           │    │
│  │   │              │  ◀───────────   │                │   ───────────▶   │              │           │    │
│  │   │              │  HTTPS Response │                │   HTTP Response │              │           │    │
│  │   └──────────────┘                 └────────────────┘                 └──────┬───────┘           │    │
│  │                                                                              │                    │    │
│  │                                                                              │ Localhost          │    │
│  │                                                                              │ HTTP Call          │    │
│  │                                                                              ▼                    │    │
│  │                                                                       ┌──────────────┐            │    │
│  │                                                                       │  ERP REST    │            │    │
│  │                                                                       │  API (E1)    │            │    │
│  │                                                                       │  Port: 8080  │            │    │
│  │                                                                       └──────────────┘            │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  CONFIGURATION:                                                                                             │
│  ──────────────                                                                                             │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  AZURE SIDE (Relay Namespace):                                                                     │    │
│  │  ────────────────────────────                                                                      │    │
│  │  • Name: relay-shipment-integration                                                                │    │
│  │  • Tier: Standard (required for Hybrid Connections)                                                │    │
│  │  • Location: Same region as Logic Apps                                                            │    │
│  │  • Hybrid Connection Name: erp-api-connection                                                      │    │
│  │  • Endpoint: https://relay-shipment-integration.servicebus.windows.net/erp-api-connection         │    │
│  │  • Requires Client Authorization: Yes (Managed Identity from C3B)                                  │    │
│  │                                                                                                    │    │
│  │  ON-PREMISE SIDE (Hybrid Connection Manager):                                                      │    │
│  │  ───────────────────────────────────────────                                                       │    │
│  │  • Installed on server in same network as ERP                                                      │    │
│  │  • Target endpoint: http://localhost:8080/api/shipments/update                                     │    │
│  │  • Connection string: Endpoint=sb://relay-shipment-integration.servicebus.windows.net/;           │    │
│  │                       SharedAccessKeyName=RootManageSharedAccessKey;                               │    │
│  │                       SharedAccessKey={key};                                                       │    │
│  │                       EntityPath=erp-api-connection                                                │    │
│  │  • Auto-start: Yes (runs as Windows Service)                                                       │    │
│  │  • Reconnect on failure: Yes (automatic)                                                           │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  REQUEST FLOW:                                                                                              │
│  ─────────────                                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. C3B (Logic App) sends HTTP request:                                                            │    │
│  │      • URI: https://relay-shipment-integration.servicebus.windows.net/erp-api-connection          │    │
│  │      • Method: POST                                                                                │    │
│  │      • Headers:                                                                                    │    │
│  │        - ServiceBusAuthorization: {Managed Identity token}                                         │    │
│  │        - X-CorrelationId: THM-ERP12345-TOR-YVR-20260129                                            │    │
│  │        - X-TenderId: TND-2026-00456                                                                │    │
│  │        - X-EntryId: 98765                                                                          │    │
│  │        - X-API-Key: {ERP API Key}                                                                  │    │
│  │        - Content-Type: application/xml                                                             │    │
│  │      • Body: <ShipmentUpdate>...</ShipmentUpdate>                                                  │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. Azure Relay Namespace receives request:                                                        │    │
│  │      • Authenticates C3B via Managed Identity                                                      │    │
│  │      • Checks if Hybrid Connection "erp-api-connection" has active listener                        │    │
│  │      • If no listener: Return 502 Bad Gateway (on-premise connection down)                         │    │
│  │      • If listener active: Forward request via WebSocket tunnel                                    │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. On-premise Hybrid Connection Manager receives request:                                         │    │
│  │      • Receives request from Relay via persistent WebSocket connection                             │    │
│  │      • Extracts HTTP method, headers, body                                                         │    │
│  │      • Makes local HTTP call to ERP REST API:                                                      │    │
│  │        POST http://localhost:8080/api/shipments/update                                             │    │
│  │        (with all headers and body from original request)                                           │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. ERP REST API (E1) processes request:                                                           │    │
│  │      • Authenticates via X-API-Key header                                                          │    │
│  │      • Parses XML body                                                                             │    │
│  │      • Updates shipment record in ERP database                                                     │    │
│  │      • Returns HTTP 200 OK with response XML                                                       │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  5. Hybrid Connection Manager receives ERP response:                                               │    │
│  │      • Forwards response back through Relay via WebSocket                                          │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  6. Azure Relay forwards response to C3B:                                                          │    │
│  │      • HTTP 200 OK                                                                                 │    │
│  │      • Headers from ERP                                                                            │    │
│  │      • Body: <UpdateResponse>...</UpdateResponse>                                                  │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  7. C3B receives response and processes accordingly                                                │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C3B (Batch Processing Logic App - Inbound)                                                    │
│  • CALLS: E1 (ERP REST API on-premise)                                                                      │
│  • EMITS TELEMETRY TO: Azure Relay metrics (via Azure Monitor)                                              │
│                                                                                                             │
│  SECURITY MODEL:                                                                                            │
│  ───────────────                                                                                            │
│  • Outbound-only from on-premise (no inbound firewall rules required)                                       │
│  • Azure side: Managed Identity authentication (C3B → Relay)                                                │
│  • On-premise side: SAS token authentication (Hybrid Connection Manager → Relay)                            │
│  • ERP API Key: Passed in X-API-Key header (ERP validates)                                                  │
│  • Data encryption: TLS 1.2+ for all communication                                                          │
│  • No credentials stored on-premise (connection string in secure config)                                    │
│                                                                                                             │
│  RELIABILITY:                                                                                               │
│  ────────────                                                                                               │
│  • Persistent WebSocket connection (keep-alive)                                                             │
│  • Automatic reconnection on connection loss                                                                │
│  • Health monitoring: Hybrid Connection Manager reports status to Relay                                     │
│  • If connection down: C3B receives 502 Bad Gateway immediately (fast fail)                                 │
│  • Multiple listeners supported (high availability - deploy on 2+ servers)                                  │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Latency: ~50-200ms overhead (vs. direct connection)                                                      │
│  • Throughput: Up to 1,000 concurrent connections                                                           │
│  • Message size: Up to 64 MB per request                                                                    │
│  • Current load: ~25 requests/minute (0.4 requests/second)                                                  │
│                                                                                                             │
│  MONITORING:                                                                                                │
│  ───────────                                                                                                │
│  • Azure Relay metrics (via Azure Monitor):                                                                 │
│    - Active Connections                                                                                     │
│    - Active Listeners                                                                                       │
│    - Bytes Transferred                                                                                      │
│    - Listener Disconnects                                                                                   │
│    - Sender Disconnects                                                                                     │
│  • Hybrid Connection Manager logs (on-premise):                                                             │
│    - Connection status                                                                                      │
│    - Request/response logs                                                                                  │
│    - Error logs                                                                                             │
│                                                                                                             │
│  ERROR SCENARIOS:                                                                                           │
│  ────────────────                                                                                           │
│  • Listener not connected: 502 Bad Gateway (C3B abandons message, will retry)                               │
│  • Listener timeout: 504 Gateway Timeout (C3B abandons message, will retry)                                 │
│  • ERP API down: Depends on ERP error (C3B handles based on status code)                                    │
│  • Network interruption: WebSocket reconnects automatically                                                 │
│                                                                                                             │
│  BENEFITS VS. ON-PREMISE DATA GATEWAY (K2):                                                                 │
│  ──────────────────────────────────────────                                                                 │
│  • Hybrid Connection: Bidirectional HTTP tunnel (works for REST APIs)                                       │
│  • On-Premise Gateway: SQL/ODBC connection tunnel (works for databases)                                     │
│  • Both: Outbound-only (no firewall changes required)                                                       │
│  • Relay used for E1 (REST API), Gateway used for ERP DB (SQL)                                              │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

STEP 10: ERP REST API — On-Premise (E1)
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: E1 — ERP REST API (On-Premise)                                                                  │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Destination API for inbound tendering updates from integration platform                    │
│  TYPE:           On-premise REST API (ASP.NET, Node.js, or custom ERP API)                                  │
│  LOCATION:       On-premise, same network as ERP database                                                   │
│  ENDPOINT:       http://localhost:8080/api/shipments/update                                                 │
│  PROTOCOL:       HTTP (localhost only, not exposed to internet)                                             │
│  AUTHENTICATION: API Key (X-API-Key header)                                                                 │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Receive shipment update requests from Azure via Relay (E3)                                               │
│  • Authenticate request via API key                                                                         │
│  • Parse ERP XML payload                                                                                    │
│  • Validate update type (TENDER_ASSIGNED, APPOINTMENT_SCHEDULED, DELIVERED, INVOICE)                        │
│  • Update shipment record in ERP database:                                                                  │
│    - TENDER_ASSIGNED: Update TenderId, Carrier, Cost fields                                                 │
│    - APPOINTMENT_SCHEDULED: Update PickupDate, PickupTime fields                                            │
│    - DELIVERED: Update DeliveryStatus, DeliveryTimestamp fields                                             │
│    - INVOICE: Update InvoiceNumber, InvoiceAmount, InvoiceDate fields                                       │
│  • Return synchronous response confirming update                                                            │
│  • Log request/response for audit (ERP's own logging system)                                                │
│                                                                                                             │
│  REQUEST HANDLING:                                                                                          │
│  ─────────────────                                                                                          │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. Receive HTTP POST request (from E3 Relay)                                                      │    │
│  │      • Endpoint: POST /api/shipments/update                                                        │    │
│  │      • Headers:                                                                                    │    │
│  │        - X-API-Key: {api-key-value}                                                                │    │
│  │        - X-CorrelationId: THM-ERP12345-TOR-YVR-20260129                                            │    │
│  │        - X-TenderId: TND-2026-00456                                                                │    │
│  │        - X-EntryId: 98765                                                                          │    │
│  │        - Content-Type: application/xml                                                             │    │
│  │      • Body: <ShipmentUpdate>...</ShipmentUpdate>                                                  │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. Authenticate API Key                                                                           │    │
│  │      • Validate X-API-Key header against configured key                                            │    │
│  │      • If invalid: Return 401 Unauthorized                                                         │    │
│  │      • If valid: Continue                                                                          │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. Parse XML payload                                                                              │    │
│  │      • Extract:                                                                                    │    │
│  │        - CorrelationId (or derive from X-CorrelationId header)                                     │    │
│  │        - ShpNum (ERP shipment number)                                                              │    │
│  │        - EntityId                                                                                  │    │
│  │        - UpdateType (TENDER_ASSIGNED, APPOINTMENT_SCHEDULED, etc.)                                 │    │
│  │        - Update-specific fields (TenderId, Cost, Carrier, etc.)                                    │    │
│  │      • If parse error: Return 400 Bad Request                                                      │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. Validate shipment exists in ERP database                                                       │    │
│  │      • Query: SELECT * FROM Shipments WHERE ShpNum = '{ShpNum}' AND EntityId = '{EntityId}'       │    │
│  │      • If not found: Return 404 Not Found                                                          │    │
│  │      • If found: Continue                                                                          │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  5. Check for duplicate processing (idempotency)                                                   │    │
│  │      • Query: SELECT * FROM ShipmentUpdateLog WHERE EntryId = '{EntryId}'                          │    │
│  │      • If exists: Return existing response (idempotent)                                            │    │
│  │      • If not exists: Continue                                                                     │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  6. Update shipment record based on UpdateType                                                     │    │
│  │      │                                                                                             │    │
│  │      ├──▶ TENDER_ASSIGNED:                                                                         │    │
│  │      │      • UPDATE Shipments SET                                                                 │    │
│  │      │          TenderId = '{TenderId}',                                                           │    │
│  │      │          Carrier = '{Carrier}',                                                             │    │
│  │      │          CarrierCode = '{CarrierCode}',                                                     │    │
│  │      │          ShipmentCost = {Cost},                                                             │    │
│  │      │          Currency = '{Currency}',                                                           │    │
│  │      │          EstimatedPickupDate = '{PickupDate}',                                              │    │
│  │      │          TenderStatus = 'ASSIGNED',                                                         │    │
│  │      │          LastUpdated = GETDATE()                                                            │    │
│  │      │        WHERE ShpNum = '{ShpNum}' AND EntityId = '{EntityId}'                               │    │
│  │      │                                                                                             │    │
│  │      ├──▶ APPOINTMENT_SCHEDULED:                                                                   │    │
│  │      │      • UPDATE Shipments SET                                                                 │    │
│  │      │          AppointmentDate = '{AppointmentDate}',                                             │    │
│  │      │          AppointmentWindow = '{AppointmentWindow}',                                         │    │
│  │      │          AppointmentStatus = 'SCHEDULED',                                                   │    │
│  │      │          LastUpdated = GETDATE()                                                            │    │
│  │      │        WHERE ShpNum = '{ShpNum}' AND EntityId = '{EntityId}'                               │    │
│  │      │                                                                                             │    │
│  │      ├──▶ DELIVERED:                                                                               │    │
│  │      │      • UPDATE Shipments SET                                                                 │    │
│  │      │          DeliveryStatus = 'DELIVERED',                                                      │    │
│  │      │          DeliveryTimestamp = '{DeliveryTimestamp}',                                         │    │
│  │      │          SignedBy = '{SignedBy}',                                                           │    │
│  │      │          LastUpdated = GETDATE()                                                            │    │
│  │      │        WHERE ShpNum = '{ShpNum}' AND EntityId = '{EntityId}'                               │    │
│  │      │                                                                                             │    │
│  │      └──▶ INVOICE:                                                                                 │    │
│  │           • UPDATE Shipments SET                                                                   │    │
│  │               InvoiceNumber = '{InvoiceNumber}',                                                   │    │
│  │               InvoiceAmount = {InvoiceAmount},                                                     │    │
│  │               InvoiceDate = '{InvoiceDate}',                                                       │    │
│  │               InvoiceStatus = 'RECEIVED',                                                          │    │
│  │               LastUpdated = GETDATE()                                                              │    │
│  │             WHERE ShpNum = '{ShpNum}' AND EntityId = '{EntityId}'                                  │    │
│  │           │                                                                                         │    │
│  │           ▼                                                                                         │    │
│  │  7. Log update in ShipmentUpdateLog table (for idempotency and audit)                              │    │
│  │      • INSERT INTO ShipmentUpdateLog (                                                             │    │
│  │          EntryId, CorrelationId, ShpNum, EntityId, UpdateType,                                     │    │
│  │          TenderId, ReceivedTimestamp, ProcessedTimestamp                                           │    │
│  │        ) VALUES (...)                                                                              │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  8. Return synchronous response                
│  │      • Status: 200 OK                                                                              │    │
│  │      • Body:                                                                                       │    │
│  │        <UpdateResponse>                                                                            │    │
│  │          <Success>true</Success>                                                                   │    │
│  │          <CorrelationId>THM-ERP12345-TOR-YVR-20260129</CorrelationId>                              │    │
│  │          <ShpNum>ERP12345</ShpNum>                                                                 │    │
│  │          <UpdateType>TENDER_ASSIGNED</UpdateType>                                                  │    │
│  │          <Message>Shipment updated successfully</Message>                                          │    │
│  │          <ProcessedTimestamp>2026-01-29T14:35:05.000Z</ProcessedTimestamp>                         │    │
│  │        </UpdateResponse>                                                                           │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INPUT (HTTP Request Body):                                                                                 │
│  ───────────────────────────                                                                                │
│  <ShipmentUpdate>                                                                                           │
│    <CorrelationId>THM-ERP12345-TOR-YVR-20260129</CorrelationId>                                             │
│    <ShpNum>ERP12345</ShpNum>                                                                                │
│    <EntityId>THM</EntityId>                                                                                 │
│    <UpdateType>TENDER_ASSIGNED</UpdateType>                                                                 │
│    <TenderId>TND-2026-00456</TenderId>                                                                      │
│    <Cost>2500.00</Cost>                                                                                     │
│    <Currency>CAD</Currency>                                                                                 │
│    <Carrier>FastFreight Inc</Carrier>                                                                       │
│    <CarrierCode>FFI</CarrierCode>                                                                           │
│    <PickupDate>20260130</PickupDate>                                                                        │
│    <PickupTime>08:00</PickupTime>                                                                           │
│  </ShipmentUpdate>                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response):                                                                                    │
│  ────────────────────────                                                                                   │
│  <UpdateResponse>                                                                                           │
│    <Success>true</Success>                                                                                  │
│    <CorrelationId>THM-ERP12345-TOR-YVR-20260129</CorrelationId>                                             │
│    <ShpNum>ERP12345</ShpNum>                                                                                │
│    <UpdateType>TENDER_ASSIGNED</UpdateType>                                                                 │
│    <Message>Shipment updated successfully</Message>                                                         │
│    <ProcessedTimestamp>2026-01-29T14:35:05.000Z</ProcessedTimestamp>                                        │
│  </UpdateResponse>                                                                                          │
│                                                                                                             │
│  DATABASE SCHEMA (ERP Database):                                                                            │
│  ───────────────────────────────                                                                            │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  TABLE: Shipments                                                                                  │    │
│  │  ═════════════════                                                                                 │    │
│  │  • ShpNum (PK)                                                                                     │    │
│  │  • EntityId (PK)                                                                                   │    │
│  │  • Origin                                                                                          │    │
│  │  • Destination                                                                                     │    │
│  │  • Weight                                                                                          │    │
│  │  • DueDate                                                                                         │    │
│  │  • LastModifiedTimestamp                                                                           │    │
│  │  • TenderId (nullable - populated by inbound update)                                               │    │
│  │  • Carrier (nullable - populated by inbound update)                                                │    │
│  │  • CarrierCode (nullable - populated by inbound update)                                            │    │
│  │  • ShipmentCost (nullable - populated by inbound update)                                           │    │
│  │  • Currency (nullable - populated by inbound update)                                               │    │
│  │  • EstimatedPickupDate (nullable - populated by inbound update)                                    │    │
│  │  • TenderStatus (nullable - populated by inbound update)                                           │    │
│  │  • AppointmentDate (nullable - populated by inbound update)                                        │    │
│  │  • AppointmentWindow (nullable - populated by inbound update)                                      │    │
│  │  • AppointmentStatus (nullable - populated by inbound update)                                      │    │
│  │  • DeliveryStatus (nullable - populated by inbound update)                                         │    │
│  │  • DeliveryTimestamp (nullable - populated by inbound update)                                      │    │
│  │  • SignedBy (nullable - populated by inbound update)                                               │    │
│  │  • InvoiceNumber (nullable - populated by inbound update)                                          │    │
│  │  • InvoiceAmount (nullable - populated by inbound update)                                          │    │
│  │  • InvoiceDate (nullable - populated by inbound update)                                            │    │
│  │  • InvoiceStatus (nullable - populated by inbound update)                                          │    │
│  │  • LastUpdated (auto-updated on any change)                                                        │    │
│  │                                                                                                    │    │
│  │  TABLE: ShipmentUpdateLog (for idempotency and audit)                                              │    │
│  │  ══════════════════════════════════════════════                                                    │    │
│  │  • EntryId (PK) - from 3rd party                                                                   │    │
│  │  • CorrelationId                                                                                   │    │
│  │  • ShpNum                                                                                          │    │
│  │  • EntityId                                                                                        │    │
│  │  • UpdateType (TENDER_ASSIGNED, APPOINTMENT_SCHEDULED, etc.)                                       │    │
│  │  • TenderId                                                                                        │    │
│  │  • ReceivedTimestamp                                                                               │    │
│  │  • ProcessedTimestamp                                                                              │    │
│  │  • SourcePayload (XML text - full request for audit)                                               │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  RESPONSE CODES:                                                                                            │
│  ──────────────                                                                                             │
│  • 200 OK: Update successful                                                                                │
│  • 400 Bad Request: Invalid XML, missing required fields                                                    │
│  • 401 Unauthorized: Invalid or missing API key                                                             │
│  • 404 Not Found: Shipment not found in ERP database                                                        │
│  • 409 Conflict: Concurrent update conflict (optimistic locking failure)                                    │
│  • 500 Internal Server Error: Database error, unexpected exception                                          │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C3B (Batch Processing Logic App - Inbound) via E3 (Azure Relay)                               │
│  • CALLS: ERP Database (direct SQL connection, same network)                                                │
│  • EMITS TELEMETRY TO: ERP's own logging system (outside integration scope)                                 │
│                                                                                                             │
│  SECURITY:                                                                                                  │
│  ─────────                                                                                                  │
│  • API Key authentication (X-API-Key header)                                                                │
│  • Only accessible via localhost (not exposed to internet)                                                  │
│  • Accessed from Azure via Relay tunnel only                                                                │
│  • No direct internet exposure                                                                              │
│  • Database connection uses integrated authentication or secured connection string                           │
│                                                                                                             │
│  IDEMPOTENCY:                                                                                               │
│  ────────────                                                                                               │
│  • Uses EntryId (from 3rd party) as idempotency key                                                         │
│  • Multiple requests with same EntryId return same response                                                 │
│  • Prevents duplicate updates to shipment record                                                            │
│  • Critical for retry scenarios (C3B retries, Relay reconnects)                                             │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Typical request handling time: 100-500ms (includes database update)                                      │
│  • Database update: 50-200ms (indexed lookup + single UPDATE statement)                                     │
│  • Concurrent requests: Supported (stateless API)                                                           │
│  • Connection pooling: Database connection pool configured                                                  │
│  • Expected load: ~25 requests/minute (0.4 requests/second)                                                 │
│                                                                                                             │
│  ERROR HANDLING:                                                                                            │
│  ───────────────                                                                                            │
│  • Database timeout: Return 500 Internal Server Error                                                       │
│  • Shipment not found: Return 404 Not Found (caller will DLQ)                                               │
│  • Concurrent update conflict: Return 409 Conflict, caller should retry                                     │
│  • Invalid XML: Return 400 Bad Request with validation errors                                               │
│  • Unhandled exception: Return 500, log to ERP's logging system                                             │
│                                                                                                             │
│  LOGGING & AUDIT:                                                                                           │
│  ────────────────                                                                                           │
│  • All requests logged to ShipmentUpdateLog table                                                           │
│  • Full XML payload stored for audit purposes                                                               │
│  • Timestamps recorded (received, processed)                                                                │
│  • ERP's application logs record API calls                                                                  │
│  • Database triggers may log changes to shipment records (ERP-specific)                                     │
│                                                                                                             │
│  DEPLOYMENT:                                                                                                │
│  ───────────                                                                                                │
│  • Hosted on-premise server (Windows or Linux)                                                              │
│  • Runs as Windows Service or systemd service                                                               │
│  • Port 8080 (localhost only, not firewall-exposed)                                                         │
│  • Same server as Hybrid Connection Manager (E3) or separate server in same network                         │
│  • Configuration: API keys, database connection strings in config file                                      │
│                                                                                                             │
│  DATA FLOW COMPLETION:                                                                                      │
│  ─────────────────────                                                                                      │
│  This is the FINAL DESTINATION for inbound messages:                                                        │
│  • 3rd Party generates update → C2 polls → validates → transforms → C10 queues →                            │
│    C3B batches → E3 relays → E1 updates ERP database                                                        │
│  • Full lifecycle: ~5-10 seconds from 3rd party update to ERP database update                               │
│  • Including polling interval: ~5-10 minutes total (poll frequency dominates)                               │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

Inbound Flow Summary — Complete End-to-End Journey
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                              INBOUND FLOW — COMPLETE SEQUENCE SUMMARY                                        │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  COMPONENT SEQUENCE:                                                                                         │
│  ═══════════════════                                                                                         │
│                                                                                                             │
│  E2 (3rd Party API) → C2 (Inbound Poll Logic App) → C5 (Correlation Mgr - Lookup/Link) →                   │
│  C6 (Schema Validator) → C7 (Business Validator) → C8 (Transformer) → C10 (Service Bus) →                  │
│  C3B (Batch Logic App) → E3 (Azure Relay) → E1 (ERP REST API)                                               │
│                                                                                                             │
│  WITH TELEMETRY: All components → C12 (Application Insights)                                                │
│  WITH STORAGE: C5 ↔ S1 (Correlation), C8 → S2 (Blobs), C3B → S1 & S2 (Audit)                               │
│                                                                                                             │
│                                                                                                             │
│  TIMING (Typical Message):                                                                                  │
│  ══════════════════════════                                                                                 │
│                                                                                                             │
│  ┌──────────────────────────────────────────┬──────────────────┬────────────────────────────────────┐       │
│  │  Step                                    │  Duration        │  Cumulative Time                   │       │
│  ├──────────────────────────────────────────┼──────────────────┼────────────────────────────────────┤       │
│  │  E2 update created                       │  N/A             │  T+0 (3rd party async process)     │       │
│  │  Wait for next poll                      │  0-300s          │  Variable (5 min poll interval)    │       │
│  │  C2 → E2: Poll API                       │  100-500ms       │  0.5s                              │       │
│  │  C2 → C5: Correlation lookup             │  10-50ms         │  0.55s                             │       │
│  │  C2 → C5: Link tender (if first)         │  50-150ms        │  0.70s (if applicable)             │       │
│  │  C2 → C6: Schema validation              │  50-200ms        │  0.90s                             │       │
│  │  C2 → C7: Business validation            │  100-300ms       │  1.20s                             │       │
│  │  C2 → C8: Transformation                 │  50-150ms        │  1.35s                             │       │
│  │  C2 → C10: Publish to Service Bus        │  10-50ms         │  1.40s                             │       │
│  │  C10 queuing (waiting for batch)         │  0-300s          │  Variable (batch trigger)          │       │
│  │  C3B → E3 → E1: ERP API call             │  200-1000ms      │  +1s (per message in batch)        │       │
│  │  C3B: Complete Service Bus message       │  10-50ms         │  +0.05s                            │       │
│  ├──────────────────────────────────────────┼──────────────────┼────────────────────────────────────┤       │
│  │  TOTAL (poll to ERP update):             │                  │  ~2.5 seconds + queue wait         │       │
│  │  TOTAL (with polling latency):           │                  │  ~5-10 minutes (poll dominates)    │       │
│  └──────────────────────────────────────────┴──────────────────┴────────────────────────────────────┘       │
│                                                                                                             │
│                                                                                                             │
│  DATA TRANSFORMATIONS:                                                                                      │
│  ═════════════════════                                                                                      │
│                                                                                                             │
│  1. 3rd Party XML (TenderingUpdate) → C2 internal representation                                            │
│  2. Update data → C5 HTTP request (correlation lookup)                                                      │
│  3. 3rd Party XML → C6 HTTP request (schema validation)                                                     │
│  4. Update data → C7 HTTP request (business validation)                                                     │
│  5. 3rd Party XML → C8 HTTP request (transformation to ERP XML)                                             │
│  6. ERP XML → Service Bus message (Base64-encoded body + properties)                                        │
│  7. Service Bus message → C3B (decoded ERP XML)                                                             │
│  8. ERP XML → E3 Relay HTTP request                                                                         │
│  9. E3 Relay → E1 HTTP request (localhost)                                                                  │
│  10. E1 parses ERP XML → SQL UPDATE statement → ERP database                                                │
│                                                                                                             │
│                                                                                                             │
│  CORRELATION CONTEXT PROPAGATION:                                                                           │
│  ════════════════════════════════                                                                           │
│                                                                                                             │
│  • Retrieved by C5 from stored mappings (GuidLookup/ThirdPartyLookup/CorrelationMapping)                    │
│  • Propagated through:                                                                                      │
│    - HTTP request headers (X-CorrelationId, X-TenderId, X-EntryId) between C2 ↔ C5/C6/C7/C8                │
│    - Service Bus message properties (ApplicationProperties)                                                 │
│    - ERP XML payload (<CorrelationId> element)                                                              │
│    - Application Insights customDimensions (all components)                                                 │
│    - Table Storage records (MessageAuditLog)                                                                │
│    - Blob Storage metadata and folder paths                                                                 │
│    - ERP API request headers (X-CorrelationId, X-TenderId)                                                  │
│    - ERP database ShipmentUpdateLog table                                                                   │
│                                                                                                             │
│                                                                                                             │
│  ERROR HANDLING POINTS:                                                                                     │
│  ═════════════════════                                                                                      │
│                                                                                                             │
│  • E2 API failure: C2 retries HTTP call 3x, then fails workflow (will retry next poll)                      │
│  • C5 correlation not found: C2 logs error, sends to DLQ (orphan message)                                   │
│  • C6 schema validation failure: C2 sends to DLQ, triggers C4 (Alert Handler)                               │
│  • C7 business validation failure: C2 sends to DLQ, triggers C4                                             │
│  • C8 transformation failure: C2 logs error, sends to DLQ                                                   │
│  • C10 publish failure: Service Bus SDK auto-retries, C2 workflow fails if all retries exhausted            │
│  • E3 connection down: C3B receives 502 Bad Gateway, abandons message (will retry)                          │
│  • E1 API failure (5xx): C3B abandons message (will retry up to 5 times)                                    │
│  • E1 API failure (4xx): C3B dead-letters immediately (no retry), triggers C4                               │
│  • Service Bus max delivery count: Message moved to DLQ, triggers C4                                        │
│                                                                                                             │
│                                                                                                             │
│  MESSAGE LIFECYCLE EXAMPLE:                                                                                 │
│  ═══════════════════════════                                                                                │
│                                                                                                             │
│  Shipment: THM-ERP12345-TOR-YVR-20260129                                                                    │
│  Timeline of inbound updates:                                                                               │
│                                                                                                             │
│  2026-01-29 14:30:00 - 3rd party assigns tender (entryId 98765)                                             │
│  2026-01-29 14:35:00 - C2 polls, retrieves entryId 98765                                                    │
│  2026-01-29 14:35:01 - C5 links tenderId TND-2026-00456 to CorrelationId                                    │
│  2026-01-29 14:35:02 - Validations pass, transformed, sent to C10                                           │
│  2026-01-29 14:35:10 - C3B processes, calls E1 via E3                                                       │
│  2026-01-29 14:35:11 - E1 updates ERP database (TenderId, Carrier, Cost populated)                          │
│                                                                                                             │
│  2026-01-30 08:00:00 - 3rd party schedules appointment (entryId 98801)                                      │
│  2026-01-30 08:05:00 - C2 polls, retrieves entryId 98801                                                    │
│  2026-01-30 08:05:01 - C5 looks up CorrelationId via correlationGuid (found)                                │
│  2026-01-30 08:05:02 - Validations pass, transformed, sent to C10                                           │
│  2026-01-30 08:05:10 - C3B processes, calls E1 via E3                                                       │
│  2026-01-30 08:05:11 - E1 updates ERP database (AppointmentDate populated)                                  │
│                                                                                                             │
│  2026-01-30 09:45:00 - 3rd party marks delivered (entryId 99102)                                            │
│  2026-01-30 09:50:00 - C2 polls, retrieves entryId 99102                                                    │
│  2026-01-30 09:50:01 - C5 looks up CorrelationId via tenderId (found)                                       │
│  2026-01-30 09:50:02 - Validations pass, transformed, sent to C10                                           │
│  2026-01-30 09:50:10 - C3B processes, calls E1 via E3                                                       │
│  2026-01-30 09:50:11 - E1 updates ERP database (DeliveryStatus, DeliveryTimestamp populated)                │
│                                                                                                             │
│  2026-02-01 09:15:00 - 3rd party sends invoice (entryId 99501)                                              │
│  2026-02-01 09:20:00 - C2 polls, retrieves entryId 99501                                                    │
│  2026-02-01 09:20:01 - C5 looks up CorrelationId via correlationGuid (found)                                │
│  2026-02-01 09:20:02 - Validations pass, transformed, sent to C10                                           │
│  2026-02-01 09:20:10 - C3B processes, calls E1 via E3                                                       │
│  2026-02-01 09:20:11 - E1 updates ERP database (InvoiceNumber, InvoiceAmount populated)                     │
│                                                                                                             │
│  RESULT: ERP shipment record contains complete lifecycle data from 3rd party system                         │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
Complete Inbound Flow — Visual Summary
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                              INBOUND FLOW — VISUAL COMPONENT MAP                                             │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│                                                                                                             │
│   ┌──────────────────────────────────────────────────────────────────────────────────────────────────┐     │
│   │                        3RD PARTY CLOUD                                                           │     │
│   │                                                                                                  │     │
│   │   ┌─────────────────────┐                                                                        │     │
│   │   │  E2                 │  Polling Endpoint:                                                     │     │
│   │   │  3rd Party          │  GET /tendering?entryId={last}&limit=100                               │     │
│   │   │  Logistics API      │  Returns: XML array of updates                                         │     │
│   │   └──────────┬──────────┘                                                                        │     │
│   │              │                                                                                    │     │
│   └──────────────┼────────────────────────────────────────────────────────────────────────────────────┘     │
│                  │                                                                                          │
│                  │ HTTP GET (every 5 minutes)                                                               │
│                  │                                                                                          │
│   ┌──────────────▼────────────────────────────────────────────────────────────────────────────────────┐     │
│   │                        AZURE INTEGRATION LAYER                                                   │     │
│   │                                                                                                  │     │
│   │   ┌─────────────────────┐                                                                        │     │
│   │   │  C2                 │  Orchestrator - polls E2, validates, transforms                        │     │
│   │   │  Inbound Poll       │  Calls: C5 → C6 → C7 → C8                                              │     │
│   │   │  Logic App          │  Publishes to C10                                                      │     │
│   │   └──────────┬──────────┘                                                                        │     │
│   │              │                                                                                    │     │
│   │              ├───────▶ C5 (Correlation Manager) ─────▶ S1 (Table Storage)                        │     │
│   │              │         Lookup CorrelationId                                                       │     │
│   │              │         Link tenderId (if first)                                                   │     │
│   │              │                                                                                    │     │
│   │              ├───────▶ C6 (Schema Validator) ──────▶ S3 (Integration Account - XSDs)             │     │
│   │              │         Validate 3rd Party XML                                                     │     │
│   │              │                                                                                    │     │
│   │              ├───────▶ C7 (Business Validator) ─────▶ S1 (Reference data)                        │     │
│   │              │         Validate business rules                                                    │     │
│   │              │                                                                                    │     │
│   │              └───────▶ C8 (Transformer) ────────────▶ S3 (Integration Account - XSLT)            │     │
│   │                        3rd Party XML → ERP XML       S2 (Blob - Audit)                            │     │
│   │                             │                                                                     │     │
│   │                             ▼                                                                     │     │
│   │              ┌─────────────────────┐                                                              │     │
│   │              │  C10                │  Queue - decouples polling from ERP delivery                 │     │
│   │              │  Service Bus        │  Subscription: batch-processor-inbound                       │     │
│   │              │  Inbound Topic      │  Batch size: 25 messages                                     │     │
│   │              └──────────┬──────────┘                                                              │     │
│   │                         │                                                                         │     │
│   │                         ▼                                                                         │     │
│   │              ┌─────────────────────┐                                                              │     │
│   │              │  C3B                │  Orchestrator - batches messages to ERP                      │     │
│   │              │  Batch Processing   │  Calls: E3 → E1                                              │     │
│   │              │  Logic App          │  Updates: S1 (audit), S2 (blobs)                             │     │
│   │              └──────────┬──────────┘                                                              │     │
│   │                         │                                                                         │     │
│   └─────────────────────────┼─────────────────────────────────────────────────────────────────────────┘     │
│                             │                                                                               │
│                             │ HTTPS (via Managed Identity)                                                  │
│                             │                                                                               │
│   ┌─────────────────────────▼─────────────────────────────────────────────────────────────────────────┐     │
│   │                        HYBRID CONNECTIVITY                                                        │     │
│   │                                                                                                  │     │
│   │              ┌─────────────────────┐                                                              │     │
│   │              │  E3                 │  Secure tunnel - Azure ↔ On-premise                          │     │
│   │              │  Azure Relay        │  Protocol: HTTPS over WebSocket                              │     │
│   │              │  Hybrid Connection  │  Auth: Managed ID (Azure), SAS (On-prem)                     │     │
│   │              └──────────┬──────────┘                                                              │     │
│   │                         │                                                                         │     │
│   └─────────────────────────┼─────────────────────────────────────────────────────────────────────────┘     │
│                             │                                                                               │
│                             │ WebSocket Tunnel (outbound from on-prem)                                      │
│                             │                                                                               │
│   ┌─────────────────────────▼─────────────────────────────────────────────────────────────────────────┐     │
│   │                        ON-PREMISE NETWORK                                                         │     │
│   │                                                                                                  │     │
│   │   ┌─────────────────────┐                 ┌─────────────────────┐                                │     │
│   │   │  Hybrid Connection  │  localhost      │  E1                 │                                │     │
│   │   │  Manager            │  ──────────▶    │  ERP REST API       │                                │     │
│   │   │  (Listener)         │  HTTP           │  Port: 8080         │                                │     │
│   │   └─────────────────────┘                 └──────────┬──────────┘                                │     │
│   │                                                      │                                            │     │
│   │                                                      │ SQL UPDATE                                 │     │
│   │                                                      ▼                                            │     │
│   │                                            ┌─────────────────────┐                                │     │
│   │                                            │  ERP Database       │                                │     │
│   │                                            │  (SQL Server)       │                                │     │
│   │                                            │  Tables: Shipments, │                                │     │
│   │                                            │  ShipmentUpdateLog  │                                │     │
│   │                                            └─────────────────────┘                                │     │
│   │                                                                                                  │     │
│   └──────────────────────────────────────────────────────────────────────────────────────────────────┘     │
│                                                                                                             │
│                                                                                                             │
│   ALL COMPONENTS ────────────────────────────────▶ C12 (Application Insights) ◀─── Telemetry & Monitoring  │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
Final Summary
Complete Inbound Flow Coverage:

✅ E2 — 3rd Party API Polling Endpoint
✅ C2 — Inbound Poll Logic App
✅ C5 — Correlation Manager (Lookup & Link)
✅ C6 — Schema Validator (Inbound)
✅ C7 — Business Validator (Inbound)
✅ C8 — Transformer (Inbound)
✅ C10 — Service Bus Inbound Topic
✅ C3B — Batch Processing Logic App (Inbound)
✅ E3 — Azure Relay / Hybrid Connection
✅ E1 — ERP REST API (On-Premise)
Key Characteristics of Inbound Flow:

Polling-based: C2 polls E2 every 5 minutes (not push/webhook)
Sequential EntryId: Uses 3rd party's entryId for change detection
Multi-strategy Correlation: Looks up by correlationGuid (primary), tenderId, or shpnumRef
Tender Linking: First TENDER_ASSIGNED update links tenderId to CorrelationId
Hybrid Connectivity: Uses Azure Relay (not On-Premise Data Gateway like outbound SQL)
ERP Update: Final destination is ERP REST API updating database records
Idempotency: Handled at multiple layers (C7, Service Bus, E1)
Full Audit Trail: Every update stored in MessageAuditLog and Blob Storage
End-to-End Latency:

Pure processing time: ~2.5 seconds
With polling interval: 5-10 minutes (dominated by 5-minute poll frequency)
Total lifecycle (submission to invoice): ~3 days typical