Section 3.3: Message Flow Examples — Component Identification & Interaction
Overview
This section identifies all components involved in both outbound and inbound message flows, describing each component's role, responsibilities, and interactions without diving into code implementation.

Message Flow Component Inventory
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                              COMPLETE COMPONENT INVENTORY                                                    │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  AZURE COMPONENTS (12 Total)                                                                                │
│  ════════════════════════                                                                                   │
│                                                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────────────────────────────────┐     │
│  │  ID   │  Component Name                │  Type              │  Primary Role                       │     │
│  ├───────┼────────────────────────────────┼────────────────────┼─────────────────────────────────────┤     │
│  │  C1   │  Outbound Poll Logic App       │  Logic App         │  Orchestration - Poll ERP           │     │
│  │  C2   │  Inbound Poll Logic App        │  Logic App         │  Orchestration - Poll 3rd Party     │     │
│  │  C3   │  Batch Processing Logic App    │  Logic App         │  Orchestration - Batch Send         │     │
│  │  C4   │  Alert Handler Logic App       │  Logic App         │  Orchestration - DLQ Alerts         │     │
│  │  C5   │  Correlation Manager Function  │  Azure Function    │  Processing - ID Management         │     │
│  │  C6   │  Schema Validator Function     │  Azure Function    │  Processing - XML Validation        │     │
│  │  C7   │  Business Validator Function   │  Azure Function    │  Processing - Business Rules        │     │
│  │  C8   │  Transformer Function          │  Azure Function    │  Processing - XSLT Transform        │     │
│  │  C9   │  Service Bus (Outbound Topic)  │  Messaging         │  Message Queue - Outbound           │     │
│  │  C10  │  Service Bus (Inbound Topic)   │  Messaging         │  Message Queue - Inbound            │     │
│  │  C11  │  API Management                │  Gateway           │  API Gateway - 3rd Party Access     │     │
│  │  C12  │  Application Insights          │  Observability     │  Telemetry Collection               │     │
│  └───────────────────────────────────────────────────────────────────────────────────────────────────┘     │
│                                                                                                             │
│  EXTERNAL SYSTEMS (2 Total)                                                                                 │
│  ═══════════════════════                                                                                    │
│                                                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────────────────────────────────┐     │
│  │  ID   │  System Name                   │  Type              │  Primary Role                       │     │
│  ├───────┼────────────────────────────────┼────────────────────┼─────────────────────────────────────┤     │
│  │  E1   │  ERP Database (SQL)            │  On-Premise DB     │  Source - Shipment Data             │     │
│  │  E2   │  3rd Party Logistics API       │  Cloud REST API    │  Destination - Tendering System     │     │
│  └───────────────────────────────────────────────────────────────────────────────────────────────────┘     │
│                                                                                                             │
│  STORAGE COMPONENTS (3 Total)                                                                               │
│  ═════════════════════════                                                                                  │
│                                                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────────────────────────────────┐     │
│  │  ID   │  Component Name                │  Type              │  Primary Role                       │     │
│  ├───────┼────────────────────────────────┼────────────────────┼─────────────────────────────────────┤     │
│  │  S1   │  Table Storage                 │  NoSQL Storage     │  Correlation Mapping & Audit        │     │
│  │  S2   │  Blob Storage                  │  Object Storage    │  Payload Archival                   │     │
│  │  S3   │  Integration Account           │  B2B Storage       │  XSLT Maps & XML Schemas            │     │
│  └───────────────────────────────────────────────────────────────────────────────────────────────────┘     │
│                                                                                                             │
│  SECURITY COMPONENTS (2 Total)                                                                              │
│  ══════════════════════════                                                                                 │
│                                                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────────────────────────────────┐     │
│  │  ID   │  Component Name                │  Type              │  Primary Role                       │     │
│  ├───────┼────────────────────────────────┼────────────────────┼─────────────────────────────────────┤     │
│  │  K1   │  Key Vault                     │  Secret Management │  Credentials & Connection Strings   │     │
│  │  K2   │  On-Premise Data Gateway       │  Hybrid Connector  │  Secure ERP DB Access               │     │
│  └───────────────────────────────────────────────────────────────────────────────────────────────────┘     │
│                                                                                                             │
│  TOTAL COMPONENTS: 19                                                                                       │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘


***** Outbound Message Flow — Component-by-Component Breakdown
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                              OUTBOUND FLOW: ERP → 3RD PARTY                                                  │
│                              (New Shipment Submission)                                                       │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  FLOW SEQUENCE:                                                                                              │
│  E1 → K2 → C1 → C5 → C6 → C7 → C8 → C9 → C3 → C11 → E2                                                      │
│                                                                                                             │
│  WITH TELEMETRY: All components → C12 (Application Insights)                                                │
│  WITH STORAGE:   C5 ↔ S1, All components → S2 (audit), C8 ↔ S3 (XSLT maps)                                 │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
STEP 1: ERP Database (E1) — Data Source
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: E1 — ERP Database (SQL Server)                                                                  │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Source system containing shipment data                                                     │
│  LOCATION:       On-premise, behind corporate firewall                                                      │
│  ACCESS METHOD:  Stored Procedure: dbo.GetNewShipments(@LastPollTimestamp)                                  │
│  PROTOCOL:       SQL (via On-Premise Data Gateway)                                                          │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Store shipment master data (origin, destination, weight, due date)                                       │
│  • Track LastModifiedTimestamp for change detection                                                         │
│  • Expose stored procedure that returns changed records since last poll                                     │
│  • Maintain data integrity and transactional consistency                                                    │
│                                                                                                             │
│  INPUT:                                                                                                     │
│  ──────                                                                                                     │
│  • @LastPollTimestamp (datetime) — passed by Outbound Poll Logic App                                        │
│                                                                                                             │
│  OUTPUT:                                                                                                    │
│  ───────                                                                                                    │
│  • Result set (table) containing:                                                                           │
│    - entityId                                                                                               │
│    - shpnum                                                                                                 │
│    - origin                                                                                                 │
│    - destination                                                                                            │
│    - loc                                                                                                    │
│    - weight                                                                                                 │
│    - duedate                                                                                                │
│    - operationType (NEW, UPDATE)                                                                            │
│    - LastModifiedTimestamp                                                                                  │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C1 (Outbound Poll Logic App) via K2 (On-Premise Data Gateway)                                │
│  • CALLS: None (passive data source)                                                                        │
│  • EMITS TELEMETRY TO: None directly (Logic App tracks the SQL dependency)                                  │
│                                                                                                             │
│  CHANGE DETECTION LOGIC:                                                                                    │
│  ───────────────────────                                                                                    │
│  • Query: SELECT * FROM Shipments WHERE LastModifiedTimestamp > @LastPollTimestamp                          │
│  • Returns only new or updated records since last successful poll                                           │
│  • Polling interval: 5 minutes (configured in C1)                                                           │
│                                                                                                             │
│  DATA OWNERSHIP:                                                                                            │
│  ───────────────                                                                                            │
│  • Master data source for shipments                                                                         │
│  • Does NOT store CorrelationId or CorrelationGuid (generated by integration layer)                         │
│  • Does NOT store tenderId (received from 3rd party, updated via inbound flow)                              │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
STEP 2: On-Premise Data Gateway (K2) — Hybrid Connector
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: K2 — On-Premise Data Gateway                                                                    │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Secure bridge between Azure (C1) and on-premise ERP database (E1)                          │
│  LOCATION:       Installed on on-premise server with access to ERP database                                 │
│  PROTOCOL:       Outbound HTTPS to Azure (no inbound firewall rules required)                               │
│  AUTHENTICATION: Basic Authentication to ERP DB (credentials stored in K1 - Key Vault)                      │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Establish secure tunnel from Azure Logic Apps to on-premise SQL Server                                   │
│  • Relay SQL queries from C1 to E1                                                                          │
│  • Return result sets from E1 to C1                                                                         │
│  • Handle connection pooling and retry logic                                                                │
│  • Encrypt data in transit (TLS 1.2+)                                                                       │
│                                                                                                             │
│  INPUT:                                                                                                     │
│  ──────                                                                                                     │
│  • SQL query/stored procedure call from C1                                                                  │
│  • Connection string (references credentials in K1)                                                         │
│  • Parameters: @LastPollTimestamp                                                                           │
│                                                                                                             │
│  OUTPUT:                                                                                                    │
│  ───────                                                                                                    │
│  • Result set from E1 (shipment records)                                                                    │
│  • Metadata: row count, execution time                                                                      │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C1 (Outbound Poll Logic App) — SQL Server connector action                                    │
│  • CALLS: E1 (ERP Database) — executes stored procedure                                                     │
│  • EMITS TELEMETRY TO: C12 (via Logic App's SQL dependency tracking)                                        │
│                                                                                                             │
│  SECURITY MODEL:                                                                                            │
│  ───────────────                                                                                            │
│  • Outbound-only connections (no inbound ports opened on firewall)                                          │
│  • Service Bus Relay protocol for secure communication                                                      │
│  • Credentials retrieved from Key Vault at runtime                                                          │
│  • No credentials stored locally on gateway server                                                          │
│                                                                                                             │
│  HIGH AVAILABILITY:                                                                                         │
│  ──────────────────                                                                                         │
│  • Can install multiple gateway instances for redundancy                                                    │
│  • Azure automatically load balances across healthy gateways                                                │
│  • Gateway health monitored via Azure portal                                                                │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
STEP 3: Outbound Poll Logic App (C1) — Orchestrator
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C1 — Outbound Poll Logic App                                                                    │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Orchestrator — Poll ERP for new/changed shipments and initiate processing pipeline         │
│  TYPE:           Logic App (Standard or Consumption)                                                        │
│  TRIGGER:        Recurrence (every 5 minutes)                                                               │
│  CONCURRENCY:    Sequential (one poll at a time to maintain state)                                          │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Schedule periodic polling (every 5 minutes)                                                              │
│  • Retrieve last successful poll timestamp from state (Table Storage or variable)                           │
│  • Call ERP stored procedure via On-Premise Data Gateway (K2)                                               │
│  • Iterate through returned shipment records                                                                │
│  • For each shipment:                                                                                       │
│    - Call Correlation Manager (C5) to get/create CorrelationId + CorrelationGuid                            │
│    - Call Schema Validator (C6) to validate XML structure                                                   │
│    - Call Business Validator (C7) to validate business rules                                                │
│    - If validation passes: Forward to Transformer (C8)                                                      │
│    - If validation fails: Send to Dead-Letter Queue (DLQ) and trigger Alert Handler (C4)                    │
│  • Update last poll timestamp on successful completion                                                      │
│  • Emit telemetry to Application Insights (C12)                                                             │
│                                                                                                             │
│  INPUT:                                                                                                     │
│  ──────                                                                                                     │
│  • Timer trigger (every 5 minutes)                                                                          │
│  • State: LastSuccessfulPollTimestamp (from Table Storage)                                                  │
│                                                                                                             │
│  OUTPUT:                                                                                                    │
│  ───────                                                                                                    │
│  • Individual messages sent to C5, C6, C7, C8 (via HTTP calls)                                              │
│  • State update: New LastSuccessfulPollTimestamp                                                            │
│  • Telemetry events to C12                                                                                  │
│                                                                                                             │
│  WORKFLOW STRUCTURE:                                                                                        │
│  ──────────────────                                                                                         │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. TRIGGER: Recurrence (5 minutes)                                                                │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. ACTION: Get Last Poll Timestamp                                                                │    │
│  │      • Read from Table Storage (PollingState table)                                                │    │
│  │      • Key: "OutboundPollLastTimestamp"                                                            │    │
│  │      • Default: Current time - 10 minutes (if first run)                                           │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. ACTION: Execute Stored Procedure (via K2)                                                      │    │
│  │      • Connection: On-Premise Data Gateway                                                         │    │
│  │      • Procedure: dbo.GetNewShipments                                                              │    │
│  │      • Parameter: @LastPollTimestamp = {previous step output}                                      │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. ACTION: Parse SQL Result                                                                       │    │
│  │      • Parse JSON schema of result set                                                             │    │
│  │      • Extract array of shipment records                                                           │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  5. ACTION: For Each Shipment                                                                      │    │
│  │      │                                                                                             │    │
│  │      ├──▶ 5.1: HTTP Call to Correlation Manager (C5)                                              │    │
│  │      │      • Endpoint: POST /api/correlation/resolve                                             │    │
│  │      │      • Body: { entityId, shpnum, loc, destination, duedate }                               │    │
│  │      │      • Returns: { correlationId, correlationGuid, isNew }                                  │    │
│  │      │                                                                                             │    │
│  │      ├──▶ 5.2: HTTP Call to Schema Validator (C6)                                                 │    │
│  │      │      • Endpoint: POST /api/validate/schema                                                 │    │
│  │      │      • Body: Shipment XML + CorrelationId                                                  │    │
│  │      │      • Returns: { valid: true/false, errors: [] }                                          │    │
│  │      │                                                                                             │    │
│  │      ├──▶ 5.3: CONDITION: If schema valid                                                         │    │
│  │      │      │                                                                                     │    │
│  │      │      ├──▶ YES: HTTP Call to Business Validator (C7)                                        │    │
│  │      │      │      • Endpoint: POST /api/validate/business                                        │    │
│  │      │      │      • Body: Shipment data + CorrelationId                                          │    │
│  │      │      │      • Returns: { valid: true/false, errors: [] }                                   │    │
│  │      │      │                                                                                     │    │
│  │      │      │      ├──▶ YES: HTTP Call to Transformer (C8)                                        │    │
│  │      │      │      │      • Endpoint: POST /api/transform/outbound                                │    │
│  │      │      │      │      • Body: ERP XML + CorrelationId + CorrelationGuid                       │    │
│  │      │      │      │      • Returns: Transformed 3rd Party XML                                    │    │
│  │      │      │      │                                                                              │    │
│  │      │      │      │      ├──▶ Send Message to Service Bus (C9)                                   │    │
│  │      │      │      │      │      • Topic: outbound-shipments                                      │    │
│  │      │      │      │      │      • Properties:                                                    │    │
│  │      │      │      │      │          - CorrelationId                                             │    │
│  │      │      │      │      │          - CorrelationGuid                                           │    │
│  │      │      │      │      │          - OperationType (NEW/UPDATE)                                │    │
│  │      │      │      │      │          - Direction: Outbound                                       │    │
│  │      │      │      │      │      • Body: Transformed XML                                          │    │
│  │      │      │      │                                                                              │    │
│  │      │      │      └──▶ NO (Business Validation Failed):                                          │    │
│  │      │      │           • Send to DLQ                                                             │    │
│  │      │      │           • Log error to C12                                                        │    │
│  │      │      │                                                                                     │    │
│  │      │      └──▶ NO (Schema Invalid):                                                             │    │
│  │      │           • Send to DLQ                                                                    │    │
│  │      │           • Log error to C12                                                               │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  6. ACTION: Update Last Poll Timestamp                                                             │    │
│  │      • Write to Table Storage                                                                      │    │
│  │      • Key: "OutboundPollLastTimestamp"                                                            │    │
│  │      • Value: Current timestamp                                                                    │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  7. END                                                                                            │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: Timer/Recurrence trigger (Azure scheduler)                                                    │
│  • CALLS:                                                                                                   │
│    - E1 (ERP Database) via K2 (On-Premise Data Gateway)                                                     │
│    - C5 (Correlation Manager Function)                                                                      │
│    - C6 (Schema Validator Function)                                                                         │
│    - C7 (Business Validator Function)                                                                       │
│    - C8 (Transformer Function)                                                                              │
│    - C9 (Service Bus) — publishes messages                                                                  │
│    - S1 (Table Storage) — state management                                                                  │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Workflow execution start/end                                                                             │
│  • SQL dependency call (via K2 to E1)                                                                       │
│  • HTTP dependencies to C5, C6, C7, C8                                                                      │
│  • Service Bus message publish to C9                                                                        │
│  • Tracked properties: CorrelationId, CorrelationGuid, BatchId, RecordCount                                 │
│  • Custom events: PollStarted, PollCompleted, ValidationFailed, ShipmentProcessed                           │
│                                                                                                             │
│  ERROR HANDLING:                                                                                            │
│  ───────────────                                                                                            │
│  • SQL connection failure: Retry 3 times with exponential backoff, then fail workflow                       │
│  • Function call failure: Log error, continue with next shipment (don't fail entire batch)                  │
│  • Validation failure: Route to DLQ, trigger alert                                                          │
│  • Service Bus failure: Retry automatically (Service Bus SDK handles)                                       │
│                                                                                                             │
│  STATE MANAGEMENT:                                                                                          │
│  ─────────────────                                                                                          │
│  • LastPollTimestamp stored in Table Storage (S1)                                                           │
│  • Ensures exactly-once processing semantics                                                                │
│  • On failure, next poll will reprocess records (idempotency handled by C5)                                 │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
STEP 4: Correlation Manager Function (C5) — ID Generator
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C5 — Correlation Manager Function                                                               │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Generate or retrieve Correlation ID + Correlation GUID for each shipment                   │
│  TYPE:           Azure Function (HTTP Trigger)                                                              │
│  RUNTIME:        .NET 8 Isolated Worker                                                                     │
│  ENDPOINT:       POST /api/correlation/resolve                                                              │
│  AUTHENTICATION: Function key (managed in Key Vault)                                                        │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Receive shipment key fields from C1 (entityId, shpnum, loc, destination, duedate)                        │
│  • Build composite key: {entityId}-{shpnum}-{loc}-{destination}-{duedate}                                   │
│  • Query Table Storage (S1 - CorrelationMapping) to check if shipment already exists                        │
│  • IF EXISTS:                                                                                               │
│    - Return existing CorrelationId and CorrelationGuid                                                      │
│    - Set isNew = false                                                                                      │
│  • IF NOT EXISTS:                                                                                           │
│    - Generate new CorrelationId (composite key format)                                                      │
│    - Generate new CorrelationGuid (UUID v4)                                                                 │
│    - Store in CorrelationMapping table (S1)                                                                 │
│    - Store reverse lookup in GuidLookup table (S1)                                                          │
│    - Set isNew = true                                                                                       │
│  • Return response to C1                                                                                    │
│  • Emit telemetry to Application Insights (C12)                                                             │
│                                                                                                             │
│  INPUT (HTTP Request Body):                                                                                 │
│  ───────────────────────────                                                                                │
│  {                                                                                                          │
│    "entityId": "THM",                                                                                       │
│    "shpnum": "ERP12345",                                                                                    │
│    "loc": "TOR",                                                                                            │
│    "destination": "YVR",                                                                                    │
│    "duedate": "20260129",                                                                                   │
│    "origin": "TOR",                                                                                         │
│    "operationType": "NEW"                                                                                   │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response):                                                                                    │
│  ────────────────────────                                                                                   │
│  {                                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129",                                                        │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                               │
│    "isNew": true,                                                                                           │
│    "tenderId": null,                                                                                        │
│    "status": "Active"                                                                                       │
│  }                                                                                                          │
│                                                                                                             │
│  PROCESSING LOGIC:                                                                                          │
│  ─────────────────                                                                                          │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. Extract input parameters                                                                       │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. Build composite key                                                                            │    │
│  │      • rowKey = "{shpnum}-{loc}-{destination}-{duedate}"                                           │    │
│  │      • correlationId = "{entityId}-{rowKey}"                                                       │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. Query CorrelationMapping table (S1)                                                            │    │
│  │      • PartitionKey = entityId ("THM")                                                             │    │
│  │      • RowKey = rowKey ("ERP12345-TOR-YVR-20260129")                                               │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. CONDITION: Record exists?                                                                      │    │
│  │      │                                                                                             │    │
│  │      ├──▶ YES: (Existing shipment)                                                                 │    │
│  │      │      • Retrieve existing CorrelationId                                                      │    │
│  │      │      • Retrieve existing CorrelationGuid                                                    │    │
│  │      │      • Retrieve existing TenderId (may be null)                                             │    │
│  │      │      • Set isNew = false                                                                    │    │
│  │      │      • Log: "Found existing correlation"                                                    │    │
│  │      │      • Return response                                                                      │    │
│  │      │                                                                                             │    │
│  │      └──▶ NO: (New shipment)                                                                       │    │
│  │           • Generate new CorrelationGuid = Guid.NewGuid()                                          │    │
│  │           • Create CorrelationMapping entity:                                                      │    │
│  │           •   - PartitionKey = entityId                                                            │    │
│  │           •   - RowKey = rowKey                                                                    │    │
│  │           •   - CorrelationId = correlationId                                                      │    │
│  │           •   - CorrelationGuid = generated GUID                                                   │    │
│  │           •   - ShpNum, EntityId, Loc, Destination, DueDate, Origin                               │    │
│  │           •   - TenderId = null                                                                    │    │
│  │           •   - Status = "Active"                                                                  │    │
│  │           •   - CreatedTimestamp = UtcNow                                                          │    │
│  │           • Insert into CorrelationMapping table (S1)                                              │    │
│  │           │                                                                                         │    │
│  │           • Create GuidLookup entity:                                                              │    │
│  │           •   - PartitionKey = GUID[0:8] (first 8 chars)                                          │    │
│  │           •   - RowKey = full GUID                                                                 │    │
│  │           •   - CorrelationId = correlationId                                                      │    │
│  │           •   - EntityId, ShpNum                                                                   │    │
│  │           • Insert into GuidLookup table (S1)                                                      │    │
│  │           │                                                                                         │    │
│  │           • Set isNew = true                                                                       │    │
│  │           • Log: "Created new correlation"                                                         │    │
│  │           • Return response                                                                        │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY:                                                                                               │
│    - C1 (Outbound Poll Logic App) — for outbound shipments                                                  │
│    - C2 (Inbound Poll Logic App) — for inbound message correlation lookup                                   │
│  • CALLS:                                                                                                   │
│    - S1 (Table Storage) — query and insert operations                                                       │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Request telemetry: HTTP POST /api/correlation/resolve                                                    │
│  • Dependency telemetry: Table Storage queries and inserts                                                  │
│  • Custom dimensions:                                                                                       │
│    - CorrelationId                                                                                          │
│    - CorrelationGuid                                                                                        │
│    - IsNew (true/false)                                                                                     │
│    - EntityId, ShpNum                                                                                       │
│  • Custom events:                                                                                           │
│    - "CorrelationCreated" (when isNew = true)                                                               │
│    - "CorrelationRetrieved" (when isNew = false)                                                            │
│  • Performance metrics: Duration of Table Storage operations                                                │
│                                                                                                             │
│  ERROR HANDLING:                                                                                            │
│  ───────────────                                                                                            │
│  • Table Storage timeout: Return 503 Service Unavailable                                                    │
│  • Duplicate key exception: Return existing record (treat as idempotent)                                    │
│  • Invalid input: Return 400 Bad Request with validation errors                                             │
│  • Unhandled exception: Return 500 Internal Server Error, log to C12                                        │
│                                                                                                             │
│  IDEMPOTENCY:                                                                                               │
│  ────────────                                                                                               │
│  • Multiple calls with same inputs return same CorrelationId + CorrelationGuid                              │
│  • Ensures exactly-once semantics even if C1 retries                                                        │
│  • No duplicate records created in Table Storage                                                            │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Table Storage query: ~10-50ms (point query with PartitionKey + RowKey)                                   │
│  • Table Storage insert: ~20-100ms                                                                          │
│  • Total function execution: ~50-200ms                                                                      │
│  • Concurrent execution: Supported (stateless function)                                                     │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
STEP 5: Schema Validator Function (C6) — XML Validator
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C6 — Schema Validator Function                                                                  │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Validate XML structure against XSD schema before processing                                │
│  TYPE:           Azure Function (HTTP Trigger)                                                              │
│  RUNTIME:        .NET 8 Isolated Worker                                                                     │
│  ENDPOINT:       POST /api/validate/schema                                                                  │
│  AUTHENTICATION: Function key (managed in Key Vault)                                                        │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Receive XML payload from C1 along with CorrelationId                                                     │
│  • Load XSD schema from Integration Account (S3) or embedded resource                                       │
│  • Validate XML structure:                                                                                  │
│    - Well-formedness check (valid XML syntax)                                                               │
│    - Schema validation (XSD compliance)                                                                     │
│    - Required fields present                                                                                │
│    - Data type validation (string, int, date formats)                                                       │
│  • Return validation result (valid/invalid) with error details                                              │
│  • Emit telemetry to Application Insights (C12)                                                             │
│                                                                                                             │
│  INPUT (HTTP Request Body):                                                                                 │
│  ───────────────────────────                                                                                │
│  {                                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129",                                                        │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                               │
│    "xmlPayload": "<Shipment>...</Shipment>",                                                                │
│    "schemaType": "ERP"                                                                                      │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response):                                                                                    │
│  ────────────────────────                                                                                   │
│  {                                                                                                          │
│    "valid": true,                                                                                           │
│    "errors": [],                                                                                            │
│    "warnings": [],                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129"                                                         │
│  }                                                                                                          │
│                                                                                                             │
│  OR (if validation fails):                                                                                  │
│                                                                                                             │
│  {                                                                                                          │
│    "valid": false,                                                                                          │
│    "errors": [                                                                                              │
│      {                                                                                                      │
│        "field": "dueDate",                                                                                  │
│        "message": "Element 'dueDate' is missing",                                                           │
│        "severity": "Error"                                                                                  │
│      },                                                                                                     │
│      {                                                                                                      │
│        "field": "weight",                                                                                   │
│        "message": "Value 'abc' is not a valid integer",                                                     │
│        "severity": "Error"                                                                                  │
│      }                                                                                                      │
│    ],                                                                                                       │
│    "warnings": [],                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129"                                                         │
│  }                                                                                                          │
│                                                                                                             │
│  VALIDATION CHECKS:                                                                                         │
│  ─────────────────                                                                                          │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  LAYER 1: XML Well-Formedness                                                                      │    │
│  │  ─────────────────────────                                                                         │    │
│  │  • Valid XML syntax (balanced tags, proper escaping)                                               │    │
│  │  • Encoding declaration (UTF-8)                                                                    │    │
│  │  • No malformed characters                                                                         │    │
│  │                                                                                                    │    │
│  │  LAYER 2: XSD Schema Compliance                                                                    │    │
│  │  ───────────────────────────                                                                       │    │
│  │  • Root element matches schema                                                                     │    │
│  │  • All required elements present                                                                   │    │
│  │  • Element order matches schema (if sequence enforced)                                             │    │
│  │  • No unexpected elements (strict mode)                                                            │    │
│  │                                                                                                    │    │
│  │  LAYER 3: Data Type Validation                                                                     │    │
│  │  ──────────────────────────                                                                        │    │
│  │  • Integers: weight, quantity                                                                      │    │
│  │  • Dates: dueDate (format: YYYYMMDD or ISO 8601)                                                   │    │
│  │  • Strings: shpnum, entityId (max length, allowed characters)                                      │    │
│  │  • Enumerations: operationType (NEW, UPDATE, CANCEL)                                               │    │
│  │                                                                                                    │    │
│  │  LAYER 4: Required Fields                                                                          │    │
│  │  ────────────────────                                                                              │    │
│  │  • entityId (mandatory)                                                                            │    │
│  │  • shpnum (mandatory)                                                                              │    │
│  │  • origin (mandatory)                                                                              │    │
│  │  • destination (mandatory)                                                                         │    │
│  │  • dueDate (mandatory)                                                                             │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C1 (Outbound Poll Logic App) — for each shipment record                                       │
│  • CALLS:                                                                                                   │
│    - S3 (Integration Account) — retrieves XSD schema (cached in memory)                                     │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Request telemetry: HTTP POST /api/validate/schema                                                        │
│  • Custom dimensions:                                                                                       │
│    - CorrelationId                                                                                          │
│    - CorrelationGuid                                                                                        │
│    - ValidationResult (Pass/Fail)                                                                           │
│    - ErrorCount                                                                                             │
│  • Traces (if validation fails):                                                                            │
│    - Severity: Warning                                                                                      │
│    - Message: "Schema validation failed for {CorrelationId}"                                                │
│    - Error details                                                                                          │
│  • Performance metrics: Validation duration                                                                 │
│                                                                                                             │
│  ERROR HANDLING:                                                                                            │
│  ───────────────                                                                                            │
│  • Invalid XML: Return validation failure with parse error                                                  │
│  • Schema load failure: Return 500 Internal Server Error                                                    │
│  • Timeout: Return 503 Service Unavailable (if validation takes > 5 seconds)                                │
│  • Unhandled exception: Return 500, log to C12                                                              │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Typical validation time: 50-200ms per message                                                            │
│  • Schema caching: XSD loaded once, reused for all validations                                              │
│  • Concurrent execution: Supported (stateless function)                                                     │
│  • Memory usage: ~10-50 MB per instance                                                                     │
│                                                                                                             │
│  FAIL-FAST PRINCIPLE:                                                                                       │
│  ────────────────────                                                                                       │
│  • Schema validation occurs BEFORE business validation                                                      │
│  • Prevents wasting resources on structurally invalid messages                                              │
│  • Failed messages immediately routed to DLQ (by C1)                                                        │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
Due to length constraints, I'll continue with the remaining components in the next response.

So far we've covered:

✅ E1 — ERP Database (Data Source)
✅ K2 — On-Premise Data Gateway (Hybrid Connector)
✅ C1 — Outbound Poll Logic App (Orchestrator)
✅ C5 — Correlation Manager Function (ID Generator)
✅ C6 — Schema Validator Function (XML Validator)
Remaining components for outbound flow:

C7 — Business Validator Function
C8 — Transformer Function
C9 — Service Bus (Outbound Topic)
C3 — Batch Processing Logic App
C11 — API Management
E2 — 3rd Party API



STEP 6: Business Validator Function (C7) — Business Rules Validator
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C7 — Business Validator Function                                                                │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Validate business logic and cross-field rules after schema validation passes               │
│  TYPE:           Azure Function (HTTP Trigger)                                                              │
│  RUNTIME:        .NET 8 Isolated Worker                                                                     │
│  ENDPOINT:       POST /api/validate/business                                                                │
│  AUTHENTICATION: Function key (managed in Key Vault)                                                        │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Receive validated shipment data from C1 (already passed schema validation)                               │
│  • Execute business rule validations:                                                                       │
│    - Date logic (dueDate must be in future, not past)                                                       │
│    - Cross-field validation (destination != origin)                                                         │
│    - Reference data validation (valid location codes from master data)                                      │
│    - Weight/dimension constraints (weight > 0, within carrier limits)                                       │
│    - Entity-specific rules (e.g., THM entity can only ship to certain destinations)                         │
│    - Duplicate detection (check IdempotencyCheck table in S1)                                               │
│  • Return validation result (valid/invalid) with business error details                                     │
│  • Emit telemetry to Application Insights (C12)                                                             │
│                                                                                                             │
│  INPUT (HTTP Request Body):                                                                                 │
│  ───────────────────────────                                                                                │
│  {                                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129",                                                        │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                               │
│    "shipmentData": {                                                                                        │
│      "entityId": "THM",                                                                                     │
│      "shpnum": "ERP12345",                                                                                  │
│      "origin": "TOR",                                                                                       │
│      "destination": "YVR",                                                                                  │
│      "loc": "TOR",                                                                                          │
│      "weight": 1500,                                                                                        │
│      "duedate": "20260129",                                                                                 │
│      "operationType": "NEW",                                                                                │
│      "lastModifiedTimestamp": "2026-01-29T10:00:00Z"                                                        │
│    }                                                                                                        │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response - Success):                                                                          │
│  ───────────────────────────────────                                                                        │
│  {                                                                                                          │
│    "valid": true,                                                                                           │
│    "errors": [],                                                                                            │
│    "warnings": [                                                                                            │
│      {                                                                                                      │
│        "rule": "DueDateProximity",                                                                          │
│        "message": "Due date is only 1 day away - expedited shipping may be required",                       │
│        "severity": "Warning"                                                                                │
│      }                                                                                                      │
│    ],                                                                                                       │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129"                                                         │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response - Failure):                                                                          │
│  ───────────────────────────────────                                                                        │
│  {                                                                                                          │
│    "valid": false,                                                                                          │
│    "errors": [                                                                                              │
│      {                                                                                                      │
│        "rule": "DueDateInFuture",                                                                           │
│        "field": "duedate",                                                                                  │
│        "message": "Due date 2026-01-28 is in the past (current date: 2026-01-29)",                          │
│        "severity": "Error",                                                                                 │
│        "currentValue": "20260128",                                                                          │
│        "expectedConstraint": "Must be >= today"                                                             │
│      },                                                                                                     │
│      {                                                                                                      │
│        "rule": "OriginDestinationDifferent",                                                                │
│        "field": "destination",                                                                              │
│        "message": "Origin and destination cannot be the same (TOR)",                                        │
│        "severity": "Error"                                                                                  │
│      }                                                                                                      │
│    ],                                                                                                       │
│    "warnings": [],                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129"                                                         │
│  }                                                                                                          │
│                                                                                                             │
│  BUSINESS RULES IMPLEMENTED:                                                                                │
│  ──────────────────────────                                                                                 │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  RULE 1: Date Validation                                                                           │    │
│  │  ───────────────────────                                                                           │    │
│  │  • dueDate must be today or in the future                                                          │    │
│  │  • dueDate must be within 90 days from today (max planning horizon)                                │    │
│  │  • Warning if dueDate is within 48 hours (expedited shipping alert)                                │    │
│  │                                                                                                    │    │
│  │  RULE 2: Location Validation                                                                       │    │
│  │  ───────────────────────                                                                           │    │
│  │  • origin and destination must be different                                                        │    │
│  │  • origin must exist in ValidLocations reference table (S1)                                        │    │
│  │  • destination must exist in ValidLocations reference table (S1)                                   │    │
│  │  • loc (delivery geo-location) must match destination                                              │    │
│  │                                                                                                    │    │
│  │  RULE 3: Weight Validation                                                                         │    │
│  │  ───────────────────────                                                                           │    │
│  │  • weight must be > 0                                                                              │    │
│  │  • weight must be <= 30,000 lbs (carrier maximum)                                                  │    │
│  │  • Warning if weight > 20,000 lbs (heavy shipment alert)                                           │    │
│  │                                                                                                    │    │
│  │  RULE 4: Entity-Specific Rules                                                                     │    │
│  │  ────────────────────────────                                                                      │    │
│  │  • Query EntityRules table (S1) for entity-specific constraints                                    │    │
│  │  • Example: Entity "THM" may only ship to Canadian destinations                                    │    │
│  │  • Example: Entity "ABC" requires approval for international shipments                             │    │
│  │                                                                                                    │    │
│  │  RULE 5: Duplicate Detection (Idempotency)                                                         │    │
│  │  ───────────────────────────────────────                                                           │    │
│  │  • Query IdempotencyCheck table (S1)                                                               │    │
│  │  • Key: CorrelationId + Direction ("Outbound") + lastModifiedTimestamp                             │    │
│  │  • If found: Return validation error "Duplicate message already processed"                         │    │
│  │  • If not found: Insert record to mark as processed                                                │    │
│  │                                                                                                    │    │
│  │  RULE 6: Reference Data Validation                                                                 │    │
│  │  ───────────────────────────────                                                                   │    │
│  │  • Validate location codes against master data (cached in memory)                                  │    │
│  │  • Validate entity codes against EntityMaster table                                                │    │
│  │  • Validate operationType against allowed values (NEW, UPDATE, CANCEL)                             │    │
│  │                                                                                                    │    │
│  │  RULE 7: Cross-Field Validation                                                                    │    │
│  │  ──────────────────────────                                                                        │    │
│  │  • If operationType = "UPDATE", shipment must exist in CorrelationMapping (S1)                     │    │
│  │  • If operationType = "NEW", shipment should NOT exist (checked by C5)                             │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  PROCESSING LOGIC:                                                                                          │
│  ─────────────────                                                                                          │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. Extract shipment data from request                                                             │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. Initialize validation context                                                                  │    │
│  │      • errors = []                                                                                 │    │
│  │      • warnings = []                                                                               │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. Execute validation rules (parallel where possible)                                             │    │
│  │      │                                                                                             │    │
│  │      ├──▶ Date Validation                                                                          │    │
│  │      │      • Parse dueDate                                                                        │    │
│  │      │      • Compare to current date                                                              │    │
│  │      │      • Add error/warning if constraint violated                                             │    │
│  │      │                                                                                             │    │
│  │      ├──▶ Location Validation                                                                      │    │
│  │      │      • Query ValidLocations table (S1) — cached                                             │    │
│  │      │      • Check origin != destination                                                          │    │
│  │      │      • Add error if invalid                                                                 │    │
│  │      │                                                                                             │    │
│  │      ├──▶ Weight Validation                                                                        │    │
│  │      │      • Check weight > 0 and <= max                                                          │    │
│  │      │      • Add error/warning if constraint violated                                             │    │
│  │      │                                                                                             │    │
│  │      ├──▶ Entity Rules Validation                                                                  │    │
│  │      │      • Query EntityRules table (S1) for entityId                                            │    │
│  │      │      • Execute dynamic rules                                                                │    │
│  │      │      • Add error if rule violated                                                           │    │
│  │      │                                                                                             │    │
│  │      ├──▶ Idempotency Check                                                                        │    │
│  │      │      • Query IdempotencyCheck table (S1)                                                    │    │
│  │      │      • Key: CorrelationId + "Outbound" + lastModifiedTimestamp                              │    │
│  │      │      • If exists: Add error "Duplicate"                                                     │    │
│  │      │      • If not exists: Insert record                                                         │    │
│  │      │                                                                                             │    │
│  │      └──▶ Reference Data Validation                                                                │    │
│  │           • Validate all codes against master data                                                 │    │
│  │           • Add error if invalid code found                                                        │    │
│  │           │                                                                                         │    │
│  │           ▼                                                                                         │    │
│  │  4. Aggregate validation results                                                                   │    │
│  │      • valid = (errors.length == 0)                                                                │    │
│  │      • Construct response object                                                                   │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  5. Log telemetry to Application Insights (C12)                                                    │    │
│  │      • Validation result (Pass/Fail)                                                               │    │
│  │      • Error count, warning count                                                                  │    │
│  │      • Failed rules                                                                                │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  6. Return HTTP response                                                                           │    │
│  │      • Status: 200 OK (validation completed, even if failed)                                       │    │
│  │      • Body: { valid, errors, warnings, correlationId }                                            │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C1 (Outbound Poll Logic App) — after C6 schema validation passes                              │
│  • CALLS:                                                                                                   │
│    - S1 (Table Storage) — query reference data, EntityRules, IdempotencyCheck                               │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Request telemetry: HTTP POST /api/validate/business                                                      │
│  • Custom dimensions:                                                                                       │
│    - CorrelationId                                                                                          │
│    - CorrelationGuid                                                                                        │
│    - ValidationResult (Pass/Fail)                                                                           │
│    - ErrorCount, WarningCount                                                                               │
│    - FailedRules (comma-separated list)                                                                     │
│  • Traces (if validation fails):                                                                            │
│    - Severity: Warning                                                                                      │
│    - Message: "Business validation failed for {CorrelationId}: {rule}"                                      │
│    - Each rule failure logged separately                                                                    │
│  • Custom metrics:                                                                                          │
│    - ValidationDuration (milliseconds)                                                                      │
│    - ValidationFailureRate (percentage)                                                                     │
│                                                                                                             │
│  ERROR HANDLING:                                                                                            │
│  ───────────────                                                                                            │
│  • Reference data not found: Log warning, continue with other rules                                         │
│  • Table Storage timeout: Return 503 Service Unavailable                                                    │
│  • Invalid input: Return 400 Bad Request                                                                    │
│  • Unhandled exception: Return 500 Internal Server Error, log to C12                                        │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Typical validation time: 100-300ms per message                                                           │
│  • Reference data caching: Master data cached in memory (refreshed every 15 minutes)                        │
│  • Parallel rule execution: Independent rules execute concurrently                                          │
│  • Concurrent execution: Supported (stateless function)                                                     │
│                                                                                                             │
│  EXTENSIBILITY:                                                                                             │
│  ──────────────                                                                                             │
│  • New rules can be added without changing calling components (C1)                                          │
│  • Entity-specific rules stored in database (configurable without deployment)                               │
│  • Rule execution order configurable                                                                        │
│  • Support for custom validation plugins (future enhancement)                                               │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
STEP 7: Transformer Function (C8) — XSLT Transformer
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C8 — Transformer Function                                                                       │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Transform ERP XML to 3rd Party XML using XSLT maps from Integration Account                │
│  TYPE:           Azure Function (HTTP Trigger)                                                              │
│  RUNTIME:        .NET 8 Isolated Worker                                                                     │
│  ENDPOINT:       POST /api/transform/outbound                                                               │
│  AUTHENTICATION: Function key (managed in Key Vault)                                                        │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Receive validated ERP XML payload from C1 (passed schema + business validation)                          │
│  • Receive CorrelationId and CorrelationGuid                                                                │
│  • Retrieve XSLT map from Integration Account (S3)                                                          │
│  • Apply XSLT transformation:                                                                               │
│    - Map ERP field names to 3rd Party field names                                                           │
│    - Convert date formats (YYYYMMDD → ISO 8601)                                                             │
│    - Convert units (lbs → kg if required by 3rd party)                                                      │
│    - Inject CorrelationGuid into <correlationGuid> element                                                  │
│  • Return transformed XML payload                                                                           │
│  • Store both source and transformed payloads to Blob Storage (S2) for audit                                │
│  • Emit telemetry to Application Insights (C12)                                                             │
│                                                                                                             │
│  INPUT (HTTP Request Body):                                                                                 │
│  ───────────────────────────                                                                                │
│  {                                                                                                          │
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129",                                                        │
│    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                               │
│    "direction": "Outbound",                                                                                 │
│    "operationType": "NEW",                                                                                  │
│    "sourceXml": "<Shipment>                                                                                │
│                    <shpnum>ERP12345</shpnum>                                                                │
│                    <entityId>THM</entityId>                                                                 │
│                    <origin>TOR</origin>                                                                     │
│                    <destination>YVR</destination>                                                           │
│                    <loc>TOR</loc>                                                                           │
│                    <weight>1500</weight>                                                                    │
│                    <duedate>20260129</duedate>                                                              │
│                    <operationType>NEW</operationType>                                                       │
│                  </Shipment>"                                                                               │
│  }                                                                                                          │
│                                                                                                             │
│  OUTPUT (HTTP Response):                                                                                    │
│  ────────────────────────                                                                                   │
│  {                                                                                                          │
│    "success": true,                                                                                         │
│    "transformedXml": "<ShipmentRequest>                                                                     │
│                         <correlationGuid>550e8400-e29b-41d4-a716-446655440000</correlationGuid>             │
│                         <shipmentNumber>ERP12345</shipmentNumber>                                           │
│                         <entityCode>THM</entityCode>                                                        │
│                         <pickupLocation>TOR</pickupLocation>                                                │
│                         <deliveryLocation>YVR</deliveryLocation>                                            │
│                         <grossWeight>1500</grossWeight>                                                     │
│                         <weightUnit>LBS</weightUnit>                                                        │
│                         <requiredDate>2026-01-29</requiredDate>                                             │
│                       </ShipmentRequest>",                                                                  │
│    "sourcePayloadPath": "/audit/2026/01/29/THM-ERP12345-TOR-YVR-20260129/outbound/100500Z-source.xml",     │
│    "transformedPayloadPath": "/audit/2026/01/29/THM-ERP12345-TOR-YVR-20260129/outbound/100500Z-transformed.xml",│
│    "correlationId": "THM-ERP12345-TOR-YVR-20260129"                                                         │
│  }                                                                                                          │
│                                                                                                             │
│  TRANSFORMATION MAPPINGS (XSLT):                                                                            │
│  ───────────────────────────────                                                                            │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  ERP XML (Source)                         3rd Party XML (Target)                                  │    │
│  │  ═════════════════                        ═══════════════════════                                 │    │
│  │                                                                                                    │    │
│  │  <Shipment>                        ───▶   <ShipmentRequest>                                       │    │
│  │                                                                                                    │    │
│  │  (injected by function)            ───▶     <correlationGuid>                                     │    │
│  │                                               {CorrelationGuid}                                    │    │
│  │                                             </correlationGuid>                                     │    │
│  │                                                                                                    │    │
│  │  <shpnum>ERP12345</shpnum>         ───▶     <shipmentNumber>                                      │    │
│  │                                               ERP12345                                             │    │
│  │                                             </shipmentNumber>                                      │    │
│  │                                                                                                    │    │
│  │  <entityId>THM</entityId>          ───▶     <entityCode>THM</entityCode>                          │    │
│  │                                                                                                    │    │
│  │  <origin>TOR</origin>              ───▶     <pickupLocation>                                      │    │
│  │                                               TOR                                                  │    │
│  │                                             </pickupLocation>                                      │    │
│  │                                                                                                    │    │
│  │  <destination>YVR</destination>    ───▶     <deliveryLocation>                                    │    │
│  │                                               YVR                                                  │    │
│  │                                             </deliveryLocation>                                    │    │
│  │                                                                                                    │    │
│  │  <weight>1500</weight>             ───▶     <grossWeight>1500</grossWeight>                       │    │
│  │                                             <weightUnit>LBS</weightUnit>                           │    │
│  │                                                                                                    │    │
│  │  <duedate>20260129</duedate>       ───▶     <requiredDate>                                        │    │
│  │                                               2026-01-29                                           │    │
│  │                                             </requiredDate>                                        │    │
│  │                                                                                                    │    │
│  │  <operationType>NEW</operationType> ───▶    (Omitted in target schema)                            │    │
│  │                                                                                                    │    │
│  │  </Shipment>                       ───▶   </ShipmentRequest>                                      │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  PROCESSING LOGIC:                                                                                          │
│  ─────────────────                                                                                          │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. Extract input parameters                                                                       │    │
│  │      • sourceXml, correlationId, correlationGuid, operationType                                    │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. Retrieve XSLT map from Integration Account (S3)                                                │    │
│  │      • Map name: "ERP-to-ThirdParty-Outbound.xslt"                                                 │    │
│  │      • Cached in memory after first retrieval                                                      │    │
│  │      • Cache TTL: 60 minutes (refreshed if map updated)                                            │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. Load sourceXml into XmlDocument                                                                │    │
│  │      • Parse XML string                                                                            │    │
│  │      • Validate well-formedness                                                                    │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. Create XSLT parameters                                                                         │    │
│  │      • Add correlationGuid as XSLT parameter                                                       │    │
│  │      • Add currentDate as XSLT parameter                                                           │    │
│  │      • Add operationType as XSLT parameter                                                         │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  5. Apply XSLT transformation                                                                      │    │
│  │      • XslCompiledTransform.Transform(sourceXml, parameters, outputStream)                         │    │
│  │      • Capture transformed XML output                                                              │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  6. Store payloads to Blob Storage (S2) for audit                                                  │    │
│  │      │                                                                                             │    │
│  │      ├──▶ Source Payload:                                                                          │    │
│  │      │      Path: /audit/{YYYY}/{MM}/{DD}/{CorrelationId}/outbound/{timestamp}-source.xml         │    │
│  │      │      Content: sourceXml                                                                     │    │
│  │      │      Metadata: { correlationId, correlationGuid, direction, operationType }                 │    │
│  │      │                                                                                             │    │
│  │      └──▶ Transformed Payload:                                                                     │    │
│  │           Path: /audit/{YYYY}/{MM}/{DD}/{CorrelationId}/outbound/{timestamp}-transformed.xml       │    │
│  │           Content: transformedXml                                                                  │    │
│  │           Metadata: { correlationId, correlationGuid, direction, operationType }                   │    │
│  │           │                                                                                         │    │
│  │           ▼                                                                                         │    │
│  │  7. Log telemetry to Application Insights (C12)                                                    │    │
│  │      • Transformation success                                                                      │    │
│  │      • Duration of XSLT execution                                                                  │    │
│  │      • Blob storage writes                                                                         │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  8. Return HTTP response                                                                           │    │
│  │      • Status: 200 OK                                                                              │    │
│  │      • Body: { success, transformedXml, sourcePayloadPath, transformedPayloadPath }                │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C1 (Outbound Poll Logic App) — after C7 business validation passes                            │
│  • CALLS:                                                                                                   │
│    - S3 (Integration Account) — retrieves XSLT map (cached)                                                 │
│    - S2 (Blob Storage) — stores source and transformed payloads                                             │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Request telemetry: HTTP POST /api/transform/outbound                                                     │
│  • Custom dimensions:                                                                                       │
│    - CorrelationId                                                                                          │
│    - CorrelationGuid                                                                                        │
│    - Direction (Outbound)                                                                                   │
│    - OperationType                                                                                          │
│    - SourcePayloadSize (bytes)                                                                              │
│    - TransformedPayloadSize (bytes)                                                                         │
│  • Dependency telemetry:                                                                                    │
│    - Integration Account (XSLT retrieval)                                                                   │
│    - Blob Storage (2 writes: source + transformed)                                                          │
│  • Custom metrics:                                                                                          │
│    - TransformationDuration (milliseconds)                                                                  │
│    - PayloadSizeRatio (transformed / source)                                                                │
│                                                                                                             │
│  ERROR HANDLING:                                                                                            │
│  ───────────────                                                                                            │
│  • XSLT map not found: Return 500 Internal Server Error                                                     │
│  • XSLT transformation error: Return 500 with detailed error message                                        │
│  • Blob Storage write failure: Log warning, continue (audit is secondary concern)                           │
│  • Invalid source XML: Return 400 Bad Request                                                               │
│  • Unhandled exception: Return 500, log to C12                                                              │
│                                                                                                             │
│  PERFORMANCE:                                                                                               │
│  ────────────                                                                                               │
│  • Typical transformation time: 50-150ms per message                                                        │
│  • XSLT map caching: Loaded once, compiled, reused for all transformations                                  │
│  • Blob Storage write: Asynchronous (fire-and-forget pattern)                                               │
│  • Concurrent execution: Supported (stateless function)                                                     │
│  • Memory usage: ~20-100 MB per instance (XSLT compiled in memory)                                          │
│                                                                                                             │
│  XSLT MAP MANAGEMENT:                                                                                       │
│  ────────────────────                                                                                       │
│  • Maps stored in Integration Account (S3)                                                                  │
│  • Version control: Integration Account supports map versioning                                             │
│  • Map selection: Based on direction (Outbound/Inbound) and operation type                                  │
│  • Hot reload: Function detects map updates and reloads cache                                               │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
STEP 8: Service Bus — Outbound Topic (C9) — Message Queue
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C9 — Service Bus (Outbound Topic)                                                               │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Message queue decoupling C1 (polling/validation) from C3 (batch processing/API calls)      │
│  TYPE:           Azure Service Bus Topic with Subscriptions                                                 │
│  NAMESPACE:      sb-shipment-integration                                                                    │
│  TOPIC NAME:     outbound-shipments                                                                         │
│  PROTOCOL:       AMQP 1.0 / HTTPS                                                                           │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Receive transformed XML messages from C1 (via C8)                                                        │
│  • Queue messages for batch processing by C3                                                                │
│  • Provide durable storage (messages persisted until consumed)                                              │
│  • Enable decoupling (C1 can continue polling even if C3 is temporarily unavailable)                        │
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
│  │  Message TTL (Time-to-Live):        7 days (default), 30 minutes (outbound-shipments)             │    │
│  │  Max Delivery Count:                10 (after 10 failed deliveries, move to DLQ)                  │    │
│  │  Duplicate Detection:               Enabled (5-minute window)                                      │    │
│  │  Duplicate Detection ID:            CorrelationId + OperationType + LastModifiedTimestamp         │    │
│  │  Partitioning:                      Disabled (not needed for this volume)                         │    │
│  │  Dead-Letter Queue:                 Enabled                                                        │    │
│  │  Lock Duration:                     5 minutes (time to process message before lock expires)       │    │
│  │  Enable Sessions:                   No (no ordering requirement)                                   │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  SUBSCRIPTIONS:                                                                                             │
│  ──────────────                                                                                             │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  SUBSCRIPTION 1: batch-processor                                                                   │    │
│  │  ═══════════════════════════════                                                                   │    │
│  │  • Filter: (No filter - processes all messages)                                                    │    │
│  │  • Max Delivery Count: 5 (after 5 attempts, move to DLQ)                                           │    │
│  │  • Lock Duration: 5 minutes                                                                        │    │
│  │  • Consumer: C3 (Batch Processing Logic App)                                                       │    │
│  │  • Batch Size: 25 messages per trigger                                                             │    │
│  │  • Description: Processes all outbound messages in batches                                         │    │
│  │                                                                                                    │    │
│  │  SUBSCRIPTION 2: new-shipments (Optional - for future extensibility)                               │    │
│  │  ════════════════════════════════════════════════════════════                                      │    │
│  │  • Filter: operationType = 'NEW'                                                                   │    │
│  │  • Use Case: Separate processing pipeline for new vs. update shipments                             │    │
│  │  • Currently: Not actively used, placeholder for future                                            │    │
│  │                                                                                                    │    │
│  │  SUBSCRIPTION 3: shipment-updates (Optional - for future extensibility)                            │    │
│  │  ══════════════════════════════════════════════════════════════                                    │    │
│  │  • Filter: operationType = 'UPDATE'                                                                │    │
│  │  • Use Case: Different SLA or processing logic for updates                                         │    │
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
│  │    "MessageId": "550e8400-e29b-41d4-a716-446655440000",      // CorrelationGuid                   │    │
│  │    "CorrelationId": "THM-ERP12345-TOR-YVR-20260129",         // Custom property                   │    │
│  │    "ApplicationProperties": {                                                                      │    │
│  │      "CorrelationId": "THM-ERP12345-TOR-YVR-20260129",                                           │    │
│  │      "CorrelationGuid": "550e8400-e29b-41d4-a716-446655440000",                                   │    │
│  │      "OperationType": "NEW",                                                                       │    │
│  │      "Direction": "Outbound",                                                                      │    │
│  │      "EntityId": "THM",                                                                            │    │
│  │      "ShpNum": "ERP12345",                                                                         │    │
│  │      "EnqueuedTimeUtc": "2026-01-29T10:05:10.000Z",                                                │    │
│  │      "SourceTimestamp": "2026-01-29T10:00:00.000Z"            // ERP LastModifiedTimestamp        │    │
│  │    },                                                                                              │    │
│  │    "ContentType": "application/xml",                                                               │    │
│  │    "TimeToLive": "00:30:00"                                   // 30 minutes                        │    │
│  │  }                                                                                                 │    │
│  │                                                                                                    │    │
│  │  MESSAGE BODY:                                                                                     │    │
│  │  ════════════                                                                                      │    │
│  │  (Base64-encoded transformed XML from C8)                                                          │    │
│  │                                                                                                    │    │
│  │  <ShipmentRequest>                                                                                 │    │
│  │    <correlationGuid>550e8400-e29b-41d4-a716-446655440000</correlationGuid>                         │    │
│  │    <shipmentNumber>ERP12345</shipmentNumber>                                                       │    │
│  │    <entityCode>THM</entityCode>                                                                    │    │
│  │    <pickupLocation>TOR</pickupLocation>                                                            │    │
│  │    <deliveryLocation>YVR</deliveryLocation>                                                        │    │
│  │    <grossWeight>1500</grossWeight>                                                                 │    │
│  │    <weightUnit>LBS</weightUnit>                                                                    │    │
│  │    <requiredDate>2026-01-29</requiredDate>                                                         │    │
│  │  </ShipmentRequest>                                                                                │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  MESSAGE PUBLISHING (from C1):                                                                              │
│  ────────────────────────────                                                                               │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. C1 calls C8 (Transformer) and receives transformedXml                                          │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. C1 creates Service Bus message:                                                                │    │
│  │      • MessageId = CorrelationGuid                                                                 │    │
│  │      • CorrelationId = CorrelationId (for tracing)                                                 │    │
│  │      • ApplicationProperties = { CorrelationId, CorrelationGuid, OperationType, ... }             │    │
│  │      • Body = Base64(transformedXml)                                                               │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. C1 publishes message to Topic "outbound-shipments"                                             │    │
│  │      • Service Bus SDK handles:                                                                    │    │
│  │        - Connection pooling                                                                        │    │
│  │        - Automatic retry (3 attempts)                                                              │    │
│  │        - Batching (if multiple messages)                                                           │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. Service Bus confirms message accepted                                                          │    │
│  │      • C1 completes workflow successfully                                                          │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  MESSAGE CONSUMPTION (by C3):                                                                               │
│  ───────────────────────────                                                                                │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. C3 (Batch Processing Logic App) triggers when messages available                               │    │
│  │      • Subscription: batch-processor                                                               │    │
│  │      • Max Message Count: 25 (batching)                                                            │    │
│  │      • Peek-Lock mode (message locked until C3 completes or abandons)                              │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. C3 receives batch of up to 25 messages                                                         │    │
│  │      • Each message includes properties + body                                                     │    │
│  │      • Lock acquired (5-minute timeout)                                                            │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. C3 processes each message:                                                                     │    │
│  │      • Extracts CorrelationId, CorrelationGuid from properties                                     │    │
│  │      • Decodes XML body from Base64                                                                │    │
│  │      • Calls C11 (API Management) to send to 3rd party                                             │    │
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
│  │  • Explicit dead-letter by C3 (validation failure after dequeue)                                   │    │
│  │                                                                                                    │    │
│  │  DLQ MESSAGE PROPERTIES:                                                                           │    │
│  │  • DeadLetterReason: "MaxDeliveryCountExceeded" or "TTLExpired"                                    │    │
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
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY (Publisher):                                                                                   │
│    - C1 (Outbound Poll Logic App) — publishes messages after transformation                                 │
│  • CALLS (Consumer):                                                                                        │
│    - C3 (Batch Processing Logic App) — triggers on message availability                                     │
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
│    - Throttled Requests (count)                                                                             │
│  • Operational logs (diagnostic settings):                                                                  │
│    - Message send/receive events                                                                            │
│    - Dead-letter events                                                                                     │
│    - Authentication/authorization events                                                                    │
│                                                                                                             │
│  PERFORMANCE & SCALE:                                                                                       │
│  ───────────────────                                                                                        │
│  • Message throughput: 2,000+ messages/second (Premium tier)                                                │
│  • Current load: ~500 messages/hour (~0.14 messages/second) — well within capacity                          │
│  • Latency: < 1ms (message enqueue to available for consumption)                                            │
│  • Storage: 5 GB capacity (thousands of messages)                                                           │
│  • Partitioning: Not enabled (not needed for this volume)                                                   │
│                                                                                                             │
│  RELIABILITY:                                                                                               │
│  ────────────                                                                                               │
│  • Message persistence: Stored in Azure Storage (durable)                                                   │
│  • Geo-redundancy: Available with Premium tier (geo-disaster recovery)                                      │
│  • Duplicate detection: 5-minute window using MessageId (CorrelationGuid)                                   │
│  • At-least-once delivery: Guaranteed                                                                       │
│  • Exactly-once: Achieved through idempotency checks in C5                                                  │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
STEP 9: Batch Processing Logic App (C3) — Batch Orchestrator
Code
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C3 — Batch Processing Logic App                                                                 │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Orchestrator — Consume messages from Service Bus in batches and send to 3rd party via APIM │
│  TYPE:           Logic App (Standard or Consumption)                                                        │
│  TRIGGER:        Service Bus Topic Subscription (batch-processor)                                           │
│  BATCH SIZE:     25 messages per trigger                                                                    │
│  CONCURRENCY:    Sequential per batch (to maintain order within batch, if needed)                           │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Trigger when messages available in Service Bus subscription "batch-processor"                            │
│  • Receive batch of up to 25 messages                                                                       │
│  • For each message in batch:                                                                               │
│    - Extract CorrelationId and CorrelationGuid from message properties                                      │
│    - Decode XML body from Base64                                                                            │
│    - Call API Management (C11) to forward to 3rd party API                                                  │
│    - Handle response from 3rd party:                                                                        │
│      • SUCCESS (200/201/202): Complete Service Bus message, store response to Blob (S2), update audit       │
│      • FAILURE (4xx/5xx): Log error, abandon message (will retry or DLQ)                                    │
│  • Emit telemetry to Application Insights (C12)                                                             │
│  • Update MessageAuditLog table (S1) with processing status                                                 │
│                                                                                                             │
│  WORKFLOW STRUCTURE:                                                                                        │
│  ──────────────────                                                                                         │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. TRIGGER: Service Bus Topic Subscription                                                        │    │
│  │      • Topic: outbound-shipments                                                                   │    │
│  │      • Subscription: batch-processor                                                               │    │
│  │      • Max Message Count: 25                                                                       │    │
│  │      • Is Sessions Enabled: No                                                                     │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. ACTION: Parse Service Bus Messages                                                             │    │
│  │      • Input: triggerBody() — array of messages                                                    │    │
│  │      • Extract message count                                                                       │    │
│  │      • Log: "Received {count} messages in batch"                                                   │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. ACTION: For Each Message                                                                       │    │
│  │      • Items: @triggerBody()                                                                       │    │
│  │      • Run in Parallel: No (sequential processing to avoid rate limit issues)                      │    │
│  │      │                                                                                             │    │
│  │      │                                                                                             │    │
│  │      ├──▶ 3.1: SCOPE: Process Single Message (for error handling)                                 │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 3.1.1: Compose — Extract Message Properties                                      │    │
│  │      │      │      • correlationId = @{items('For_Each')?['ApplicationProperties']?[              │    │
│  │      │      │                            'CorrelationId']}                                         │    │
│  │      │      │      • correlationGuid = @{items('For_Each')?['ApplicationProperties']?[            │    │
│  │      │      │                              'CorrelationGuid']}                                     │    │
│  │      │      │      • operationType = @{items('For_Each')?['ApplicationProperties']?[              │    │
│  │      │      │                           'OperationType']}                                          │    │
│  │      │      │      • entityId = @{items('For_Each')?['ApplicationProperties']?['EntityId']}       │    │
│  │      │      │      • shpNum = @{items('For_Each')?['ApplicationProperties']?['ShpNum']}           │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 3.1.2: Compose — Decode XML Body                                                 │    │
│  │      │      │      • xmlPayload = @{base64ToString(items('For_Each')?['ContentData'])}            │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 3.1.3: HTTP — Call API Management                                                │    │
│  │      │      │      • Method: POST                                                                  │    │
│  │      │      │      • URI: https://apim-shipment-integration.azure-api.net/logistics/shipments     │    │
│  │      │      │      • Headers:                                                                      │    │
│  │      │      │        {                                                                             │    │
│  │      │      │          "Content-Type": "application/xml",                                          │    │
│  │      │      │          "X-CorrelationId": "@{outputs('Extract_Properties')?['correlationId']}",   │    │
│  │      │      │          "X-CorrelationGuid": "@{outputs('Extract_Properties')?['correlationGuid']}", │  │
│  │      │      │          "Ocp-Apim-Subscription-Key": "@{parameters('ApimSubscriptionKey')}"        │    │
│  │      │      │        }                                                                             │    │
│  │      │      │      • Body: @{outputs('Decode_XML')?['xmlPayload']}                                 │    │
│  │      │      │      • Retry Policy: None (APIM handles retries)                                     │    │
│  │      │      │      • Timeout: 60 seconds                                                           │    │
│  │      │      │                                                                                      │    │
│  │      │      ├──▶ 3.1.4: CONDITION — Check HTTP Response Status                                    │    │
│  │      │      │      • Expression: @or(                                                              │    │
│  │      │      │                      equals(outputs('HTTP')?['statusCode'], 200),                    │    │
│  │      │      │                      equals(outputs('HTTP')?['statusCode'], 201),                    │    │
│  │      │      │                      equals(outputs('HTTP')?['statusCode'], 202)                     │    │
│  │      │      │                    )                                                                 │    │
│  │      │      │      │                                                                               │    │
│  │      │      │      ├──▶ TRUE (Success):                                                           │    │
│  │      │      │      │      │                                                                        │    │
│  │      │      │      │      ├──▶ Store Response to Blob Storage (S2)                                │    │
│  │      │      │      │      │      • Container: shipment-audit                                       │    │
│  │      │      │      │      │      • Path: /audit/{YYYY}/{MM}/{DD}/{CorrelationId}/outbound/        │    │
│  │      │      │      │      │               {timestamp}-response.xml                                 │    │
│  │      │      │      │      │      • Content: @{body('HTTP')}                                        │    │
│  │      │      │      │      │      • Metadata: { correlationId, correlationGuid, statusCode }        │    │
│  │      │      │      │      │                                                                        │    │
│  │      │      │      │      ├──▶ Update MessageAuditLog (S1)                                        │    │
│  │      │      │      │      │      • PartitionKey: CorrelationId                                     │    │  
│  │      │      │      │      │      • RowKey: {Timestamp}-Outbound-{OperationType}                   │    │
│  │      │      │      │      │      • Properties: { Status: "Success", StatusCode, Duration, ... }   │    │
│  │      │      │      │      │                                                                        │    │
│  │      │      │      │      ├──▶ Log Success to Application Insights (C12)                          │    │
│  │      │      │      │      │      • Custom Event: "OutboundMessageSuccess"                          │    │
│  │      │      │      │      │      • Properties: { CorrelationId, CorrelationGuid, StatusCode }     │    │
│  │      │      │      │      │                                                                        │    │
│  │      │      │      │      └──▶ Complete Service Bus Message                                       │    │
│  │      │      │      │           • Message removed from queue                                        │    │
│  │      │      │      │           • Delivery count NOT incremented                                    │    │
│  │      │      │      │                                                                               │    │
│  │      │      │      └──▶ FALSE (Failure):                                                          │    │
│  │      │      │           │                                                                          │    │
│  │      │      │           ├──▶ Log Error to Application Insights (C12)                              │    │
│  │      │      │           │      • Custom Event: "OutboundMessageFailed"                             │    │
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
│  │      • Custom Event: "BatchProcessingCompleted"                                                    │    │
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
│  • TRIGGERED BY: C9 (Service Bus — Outbound Topic, subscription: batch-processor)                           │
│  • CALLS:                                                                                                   │
│    - C11 (API Management) — forwards messages to 3rd party API                                              │
│    - S2 (Blob Storage) — stores response payloads                                                           │
│    - S1 (Table Storage) — updates MessageAuditLog                                                           │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Workflow execution telemetry (automatic):                                                                │
│    - WorkflowRunStarted                                                                                     │
│    - WorkflowRunCompleted                                                                                   │
│    - Duration                                                                                               │
│  • Tracked properties (configured in workflow):                                                             │
│    - BatchSize                                                                                              │
│    - SuccessCount                                                                                           │
│    - FailureCount                                                                                           │
│  • Custom events (via Application Insights connector):                                                      │
│    - "OutboundMessageSuccess" (per message)                                                                 │
│    - "OutboundMessageFailed" (per message)                                                                  │
│    - "BatchProcessingCompleted" (per batch)                                                                 │
│  • HTTP dependency tracking:                                                                                │
│    - Calls to C11 (API Management)                                                                          │
│    - Duration, status code, success/failure                                                                 │
│                                                                                                             │
│  ERROR HANDLING STRATEGY:                                                                                   │
│  ────────────────────────                                                                                   │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  PER-MESSAGE ERROR HANDLING (Not Per-Batch):                                                       │    │
│  │  ───────────────────────────────────────────                                                       │    │
│  │  • If message 5 fails, messages 1-4 are completed, messages 6-25 continue processing               │    │
│  │  • Failed message is abandoned (returned to queue for retry)                                       │    │
│  │  • Batch processing does NOT fail entirely due to single message failure                           │    │
│  │                                                                                                    │    │
│  │  RETRY FLOW FOR FAILED MESSAGE:                                                                    │    │
│  │  ──────────────────────────────                                                                    │    │
│  │  1. Message abandoned → returns to Service Bus queue                                               │    │
│  │  2. Delivery count incremented (was 1, now 2)                                                      │    │
│  │  3. Message becomes available again for next batch                                                 │    │
│  │  4. C3 triggers again, processes message again                                                     │    │
│  │  5. If fails again → repeat up to Max Delivery Count (5)                                           │    │
│  │  6. After 5th failure → Message moved to DLQ                                                       │    │
│  │  7. DLQ triggers C4 (Alert Handler) → Teams notification                                           │    │
│  │                                                                                                    │    │
│  │  TRANSIENT vs. PERMANENT FAILURES:                                                                 │    │
│  │  ──────────────────────────────────                                                                │    │
│  │  • Transient (retry will help):                                                                    │    │
│  │    - 503 Service Unavailable (3rd party API down)                                                  │    │
│  │    - 429 Too Many Requests (rate limit - APIM should handle)                                       │    │
│  │    - Network timeout                                                                               │    │
│  │    - APIM circuit breaker open                                                                     │    │
│  │  • Permanent (retry won't help):                                                                   │    │
│  │    - 400 Bad Request (invalid payload)                                                             │    │
│  │    - 401 Unauthorized (invalid API key)                                                            │    │
│  │    - 404 Not Found (wrong endpoint)                                                                │    │
│  │                                                                                                    │    │
│  │  OPTIMIZATION: Early DLQ for Permanent Failures                                                    │    │
│  │  ────────────────────────────────────────────────                                                  │    │
│  │  • If status code is 400 or 401: Explicitly dead-letter (don't retry 5 times)                      │    │
│  │  • Add custom dead-letter reason: "PermanentFailure"                                               │    │
│  │  • Saves processing time and queue resources                                                       │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  PERFORMANCE & SCALE:                                                                                       │
│  ───────────────────                                                                                        │
│  • Batch size: 25 messages (configurable)                                                                   │
│  • Processing time per message: ~1-3 seconds (HTTP call to APIM/3rd party)                                  │
│  • Total batch processing time: ~25-75 seconds (sequential processing)                                      │
│  • Throughput: ~1 batch per minute = 25 messages/minute = 1,500 messages/hour                               │
│  • Current load: 500 messages/hour (33% of capacity)                                                        │
│  • Scale-out: Logic Apps can scale horizontally (multiple concurrent workflows)                             │
│                                                                                                             │
│  CONCURRENCY SETTINGS:                                                                                      │
│  ────────────────────                                                                                       │
│  • Trigger Concurrency: 1 (one batch at a time to avoid overwhelming 3rd party API)                         │
│  • For Each Concurrency: 1 (sequential within batch, could be increased to 5-10 if needed)                  │
│  • Rationale: API Management handles rate limiting, but sequential processing simplifies error handling     │
│                                                                                                             │
│  IDEMPOTENCY:                                                                                               │
│  ────────────                                                                                               │
│  • Message may be reprocessed if C3 fails after calling APIM but before completing Service Bus message      │
│  • Protection: APIM/3rd party should be idempotent (same CorrelationGuid = same result)                     │
│  • Additional check: Query MessageAuditLog before calling APIM (if record exists with Success, skip)        │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

STEP 10: API Management (C11) — Gateway to 3rd Party


┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: C11 — API Management                                                                            │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           Gateway and abstraction layer between integration platform and 3rd party API                │
│  TYPE:           Azure API Management (Standard or Premium tier)                                            │
│  GATEWAY URL:    https://apim-shipment-integration.azure-api.net                                            │
│  API PATH:       /logistics/shipments                                                                       │
│  BACKEND URL:    https://api.thirdparty-logistics.com/api/v1/shipments                                      │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Receive HTTP POST requests from C3 (Batch Processing Logic App)                                          │
│  • Extract CorrelationId and CorrelationGuid from request headers                                           │
│  • Validate subscription key (authenticate C3)                                                              │
│  • Apply rate limiting (protect 3rd party from overload)                                                    │
│  • Retrieve 3rd party API key from Key Vault (K1)                                                           │
│  • Inject API key into outbound request header                                                              │
│  • Forward request to 3rd party backend (E2)                                                                │
│  • Implement retry policy (3 attempts with exponential backoff)                                             │
│  • Implement circuit breaker (stop calling failed backend)                                                  │
│  • Log request/response to Application Insights (C12)                                                       │
│  • Return response to C3                                                                                    │
│                                                                                                             │
│  REQUEST FLOW:                                                                                              │
│  ─────────────                                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  C3 (Logic App)                    APIM                              E2 (3rd Party API)            │    │
│  │       │                             │                                       │                      │    │
│  │       │  POST /logistics/shipments  │                                       │                      │    │
│  │       │  Headers:                   │                                       │                      │    │
│  │       │  - X-CorrelationId          │                                       │                      │    │
│  │       │  - X-CorrelationGuid        │                                       │                      │    │
│  │       │  - Ocp-Apim-Subscription-Key│                                       │                      │    │
│  │       │  Body: <ShipmentRequest>... │                                       │                      │    │
│  │       ├───────────────────────────▶│                                       │                      │    │
│  │       │                             │                                       │                      │    │
│  │       │                             │ INBOUND POLICY:                       │                      │    │
│  │       │                             │ ───────────────                       │                      │    │
│  │       │                             │ 1. Rate limit check (100/min)         │                      │    │
│  │       │                             │ 2. Validate subscription key          │                      │    │
│  │       │                             │ 3. Extract correlation context        │                      │    │
│  │       │                             │ 4. Retrieve API key from Key Vault    │                      │    │
│  │       │                             │ 5. Check circuit breaker state        │                      │    │
│  │       │                             │ 6. Log request to App Insights (C12)  │                      │    │
│  │       │                             │                                       │                      │    │
│  │       │                             │ BACKEND POLICY:                       │                      │    │
│  │       │                             │ ──────────────                        │                      │    │
│  │       │                             │ • Set backend URL                     │                      │    │
│  │       │                             │ • Apply retry (3 attempts)            │                      │    │
│  │       │                             │                                       │                      │    │
│  │       │                             │  POST /api/v1/shipments               │                      │    │
│  │       │                             │  Headers:                             │                      │    │
│  │       │                             │  - X-API-Key: {from Key Vault}        │                      │    │
│  │       │                             │  - Content-Type: application/xml      │                      │    │
│  │       │                             │  Body: <ShipmentRequest>...           │                      │    │
│  │       │                             ├───────────────────────────────────────▶│                      │    │
│  │       │                             │                                       │                      │    │
│  │       │                             │                                       │ Process request      │    │
│  │       │                             │                                       │ Validate payload     │    │
│  │       │                             │                                       │ Store in database    │    │
│  │       │                             │                                       │                      │    │
│  │       │                             │  HTTP 202 Accepted                    │                      │    │
│  │       │                             │  <ShipmentResponse>                   │                      │    │
│  │       │                             │    <status>ACCEPTED</status>          │                      │    │
│  │       │                             │    <correlationGuid>550e8400...</correlationGuid>             │    │
│  │       │                             │    <message>Queued for tendering</message>                    │    │
│  │       │                             │  </ShipmentResponse>                  │                      │    │
│  │       │                             │◀───────────────────────────────────────│                      │    │
│  │       │                             │                                       │                      │    │
│  │       │                             │ OUTBOUND POLICY:                      │                      │    │
│  │       │                             │ ───────────────                       │                      │    │
│  │       │                             │ 1. Add correlation headers back       │                      │    │
│  │       │                             │ 2. Log response to App Insights (C12) │                      │    │
│  │       │                             │ 3. Reset circuit breaker (success)    │                      │    │
│  │       │                             │                                       │                      │    │
│  │       │  HTTP 202 Accepted          │                                       │                      │    │
│  │       │  Headers:                   │                                       │                      │    │
│  │       │  - X-CorrelationId          │                                       │                      │    │
│  │       │  - X-CorrelationGuid        │                                       │                      │    │
│  │       │  Body: <ShipmentResponse>...│                                       │                      │    │
│  │       │◀───────────────────────────│                                       │                      │    │
│  │       │                             │                                       │                      │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  POLICY LAYERS (Recap from earlier deep dive):                                                              │
│  ─────────────────────────────────────────                                                                  │
│  • INBOUND:  Rate limiting, authentication, correlation extraction, API key retrieval, circuit breaker      │
│  • BACKEND:  Retry policy (3 attempts), timeout (30s)                                                       │
│  • OUTBOUND: Response logging, correlation header restoration, circuit breaker reset                        │
│  • ON-ERROR: Circuit breaker increment, error logging, standardized error response                          │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C3 (Batch Processing Logic App) — for each message in batch                                   │
│  • CALLS:                                                                                                   │
│    - K1 (Key Vault) — retrieves 3rd party API key via Managed Identity                                      │
│    - E2 (3rd Party API) — forwards request to backend                                                       │
│  • EMITS TELEMETRY TO: C12 (Application Insights)                                                           │
│                                                                                                             │
│  TELEMETRY EMISSION:                                                                                        │
│  ──────────────────                                                                                         │
│  • Request telemetry:                                                                                       │
│    - Incoming request from C3                                                                               │
│    - Method, URL, headers (correlation context)                                                             │
│    - Duration, status code                                                                                  │
│  • Dependency telemetry:                                                                                    │
│    - Outbound call to E2 (3rd party)                                                                        │
│    - Target, duration, status code, success/failure                                                         │
│  • Traces:                                                                                                  │
│    - "Forwarding shipment to 3rd party: CorrelationId={...}"                                                │
│    - "Received response from 3rd party: StatusCode={...}"                                                   │
│    - "Error calling 3rd party API: {error message}" (on failure)                                            │
│  • Custom dimensions:                                                                                       │
│    - CorrelationId, CorrelationGuid, Direction, OperationType, CircuitBreakerState                          │
│                                                                                                             │
│  RESILIENCE FEATURES:                                                                                       │
│  ───────────────────                                                                                        │
│  • Rate Limiting: 100 calls/min (protect 3rd party)                                                         │
│  • Circuit Breaker: Open after 5 failures in 60s, auto-recover after timeout                                │
│  • Retry Policy: 3 attempts with exponential backoff (2s, 4s, 8s)                                           │
│  • Timeout: 30 seconds per request                                                                          │
│  • Graceful Degradation: Return 503 when circuit open (with Retry-After header)                             │
│                                                                                                             │
│  OBSERVABILITY:                                                                                             │
│  ──────────────                                                                                             │
│  • Full request/response logging (8KB body limit)                                                           │
│  • Correlation context propagated through all logs                                                          │
│  • Performance metrics: latency, throughput, error rate                                                     │
│  • Health monitoring: Circuit breaker state, backend availability                                           │
│                                                                                                             │
│  BENEFITS IN THIS ARCHITECTURE:                                                                             │
│  ──────────────────────────────                                                                             │
│  • Decouples C3 from 3rd party API specifics (endpoint, auth, retry logic)                                  │
│  • Centralized security (API key never exposed to C3)                                                       │
│  • Prevents cascading failures (circuit breaker stops calling failed backend)                               │
│  • Simplifies C3 logic (no retry/timeout handling needed in workflow)                                       │
│  • Future-proof (switch to different provider without changing C3)                                          │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘


STEP 11: 3rd Party Logistics API (E2) — External System


┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT: E2 — 3rd Party Logistics API                                                                    │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  ROLE:           External destination system for outbound shipment tendering requests                       │
│  TYPE:           Cloud-hosted REST API (external vendor)                                                    │
│  LOCATION:       3rd party cloud infrastructure (outside Azure environment)                                 │
│  ENDPOINT:       https://api.thirdparty-logistics.com/api/v1/shipments                                      │
│  PROTOCOL:       HTTPS (REST)                                                                               │
│  AUTHENTICATION: API Key (X-API-Key header)                                                                 │
│                                                                                                             │
│  RESPONSIBILITIES:                                                                                          │
│  ────────────────                                                                                           │
│  • Receive shipment submission/update requests from APIM (C11)                                              │
│  • Authenticate request via API key                                                                         │
│  • Validate XML payload structure and business rules                                                        │
│  • Store shipment data in 3rd party database                                                                │
│  • Queue shipment for tendering workflow (carrier matching)                                                 │
│  • Return synchronous response with acceptance confirmation                                                 │
│  • Echo back correlationGuid for correlation                                                                │
│  • Later: Expose polling endpoint for tendering updates (inbound flow)                                      │
│                                                                                                             │
│  REQUEST HANDLING:                                                                                          │
│  ─────────────────                                                                                          │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  1. Receive HTTP POST request                                                                      │    │
│  │      • Endpoint: POST /api/v1/shipments                                                            │    │
│  │      • Headers: X-API-Key, Content-Type: application/xml                                           │    │
│  │      • Body: <ShipmentRequest>...</ShipmentRequest>                                                │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  2. Authenticate API Key                                                                           │    │
│  │      • Validate X-API-Key header against customer database                                         │    │
│  │      • If invalid: Return 401 Unauthorized                                                         │    │
│  │      • If valid: Continue                                                                          │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  3. Extract correlationGuid from payload                                                           │    │
│  │      • Parse XML: <correlationGuid>550e8400-e29b-41d4-a716-446655440000</correlationGuid>          │    │
│  │      • Store for use in response and future tendering updates                                      │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  4. Validate XML payload                                                                           │    │
│  │      • Schema validation (XSD)                                                                     │    │
│  │      • Required fields present (shipmentNumber, pickupLocation, deliveryLocation, etc.)            │    │
│  │      • Business rules (weight > 0, valid locations, etc.)                                          │    │
│  │      • If invalid: Return 400 Bad Request with error details                                       │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  5. Check for duplicate (idempotency)                                                              │    │
│  │      • Query database by correlationGuid                                                           │    │
│  │      • If exists: Return existing response (idempotent)                                            │    │
│  │      • If not exists: Continue                                                                     │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  6. Store shipment in database                                                                     │    │
│  │      • Insert into Shipments table                                                                 │    │
│  │      • Fields: correlationGuid, shipmentNumber, pickup, delivery, weight, requiredDate, status     │    │
│  │      • Status: "PENDING_TENDERING"                                                                 │    │
│  │      • Generate internal tenderId (not yet assigned, will be created later)                        │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  7. Queue for tendering workflow                                                                   │    │
│  │      • Publish message to internal message queue                                                   │    │
│  │      • Tendering engine will process asynchronously                                                │    │
│  │      • Matching carriers, calculating cost, assigning tender                                       │    │
│  │      │                                                                                             │    │
│  │      ▼                                                                                             │    │
│  │  8. Return synchronous response                                                                    │    │
│  │      • Status: 202 Accepted (asynchronous processing)                                              │    │
│  │      • Body:                                                                                       │    │
│  │        <ShipmentResponse>                                                                          │    │
│  │          <status>ACCEPTED</status>                                                                 │    │
│  │          <correlationGuid>550e8400-e29b-41d4-a716-446655440000</correlationGuid>                   │    │
│  │          <message>Shipment received and queued for tendering</message>                             │    │
│  │          <receivedTimestamp>2026-01-29T10:05:01.234Z</receivedTimestamp>                           │    │
│  │        </ShipmentResponse>                                                                         │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  RESPONSE CODES:                                                                                            │
│  ──────────────                                                                                             │
│  • 200 OK: Shipment updated successfully (for UPDATE operations)                                            │
│  • 201 Created: New shipment created (if synchronous)                                                       │
│  • 202 Accepted: Request queued for asynchronous processing (most common)                                   │
│  • 400 Bad Request: Invalid payload, missing fields, validation errors                                      │
│  • 401 Unauthorized: Invalid or missing API key                                                             │
│  • 429 Too Many Requests: Rate limit exceeded                                                               │
│  • 500 Internal Server Error: 3rd party system error                                                        │
│  • 503 Service Unavailable: 3rd party system down or overloaded                                             │
│                                                                                                             │
│  INTERACTIONS:                                                                                              │
│  ─────────────                                                                                              │
│  • CALLED BY: C11 (API Management) — receives shipment requests                                             │
│  • CALLS: None in this flow (internal tendering workflow is asynchronous)                                   │
│  • EMITS TELEMETRY TO: 3rd party's own monitoring system (outside integration scope)                        │
│                                                                                                             │
│  DATA OWNERSHIP:                                                                                            │
│  ───────────────                                                                                            │
│  • Master data for tendering process (carrier matching, pricing, appointments)                              │
│  • Generates tenderId asynchronously (after tendering workflow completes)                                   │
│  • Stores correlationGuid for correlation with inbound updates                                              │
│  • Later provides polling endpoint: GET /api/v1/tendering?entryId={lastEntryId}                             │
│                                                                                                             │
│  ASYNCHRONOUS WORKFLOW (Post-Acceptance):                                                                   │
│  ────────────────────────────────────────                                                                   │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                                                    │    │
│  │  After returning 202 Accepted, 3rd party performs asynchronously:                                  │    │
│  │                                                                                                    │    │
│  │  1. Tendering Engine picks up shipment from queue                                                  │    │
│  │  2. Matches shipment to available carriers based on:                                               │    │
│  │     • Origin/destination coverage                                                                  │    │
│  │     • Weight capacity                                                                              │    │
│  │     • Required date availability                                                                   │    │
│  │     • Cost                                                                                         │    │
│  │  3. Generates tenderId (e.g., "TND-2026-00456")                                                    │    │
│  │  4. Assigns carrier (e.g., "FastFreight Inc")                                                      │    │
│  │  5. Calculates cost (e.g., $2,500 CAD)                                                             │    │
│  │  6. Updates shipment status: "TENDERED"                                                            │    │
│  │  7. Creates tendering update record with new entryId (e.g., 98765)                                 │    │
│  │  8. Makes available via polling endpoint (C2 will retrieve via inbound polling)                    │    │
│  │                                                                                                    │    │
│  │  Timeline: Typically 30 minutes to 4 hours after initial submission                                │    │
│  │                                                                                                    │    │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                                             │
│  IDEMPOTENCY:                                                                                               │
│  ────────────                                                                                               │
│  • Uses correlationGuid as idempotency key                                                                  │
│  • Multiple requests with same correlationGuid return same response                                         │
│  • Prevents duplicate shipment creation                                                                     │
│  • Critical for retry scenarios (APIM retries, network issues)                                              │
│                                                                                                             │
│  RATE LIMITING:                                                                                             │
│  ──────────────                                                                                             │
│  • 3rd party may enforce rate limits (e.g., 100 requests/minute per API key)                                │
│  • Returns 429 Too Many Requests if exceeded                                                                │
│  • APIM's rate limiting (100/min) should prevent hitting 3rd party limit                                    │
│  • Retry-After header indicates when to retry                                                               │
│                                                                                                             │
│  AVAILABILITY & SLA:                                                                                        │
│  ──────────────────                                                                                         │
│  • 3rd party SLA: 99.5% uptime (typical for cloud SaaS)                                                     │
│  • Expected downtime: ~3.6 hours/month                                                                      │
│  • Maintenance windows: Typically announced in advance                                                      │
│  • Circuit breaker in APIM (C11) protects integration from prolonged outages                                │
│                                                                                                             │
│  INTEGRATION TOUCHPOINTS:                                                                                   │
│  ────────────────────────                                                                                   │
│  • OUTBOUND (this flow): Receive shipment submissions via POST /api/v1/shipments                            │
│  • INBOUND (separate flow): Expose tendering updates via GET /api/v1/tendering?entryId={lastId}             │
│  • Both use correlationGuid for correlation                                                                 │
│  • Both use same API key for authentication                                                                 │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘


Outbound Flow Summary — Complete End-to-End Journey

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                              OUTBOUND FLOW — COMPLETE SEQUENCE SUMMARY                                       │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  COMPONENT SEQUENCE:                                                                                         │
│  ═══════════════════                                                                                         │
│                                                                                                             │
│  E1 (ERP DB) → K2 (Gateway) → C1 (Poll Logic App) → C5 (Correlation Mgr) → C6 (Schema Validator) →         │
│  C7 (Business Validator) → C8 (Transformer) → C9 (Service Bus) → C3 (Batch Logic App) →                    │
│  C11 (API Management) → E2 (3rd Party API)                                                                  │
│                                                                                                             │
│  WITH TELEMETRY: All components → C12 (Application Insights)                                                │
│  WITH STORAGE: C5 ↔ S1 (Tables), C8 → S2 (Blobs), C8 ↔ S3 (XSLT), C3 → S1 & S2 (Audit)                    │
│                                                                                                             │
│                                                                                                             │
│  TIMING (Typical Message):                                                                                  │
│  ══════════════════════════                                                                                 │
│                                                                                                             │
│  ┌──────────────────────────────────────────┬──────────────────┬────────────────────────────────────┐       │
│  │  Step                                    │  Duration        │  Cumulative Time                   │       │
│  ├──────────────────────────────────────────┼──────────────────┼────────────────────────────────────┤       │
│  │  E1 → C1: SQL query via K2               │  100-500ms       │  0.5s                              │       │
│  │  C1 → C5: Correlation Manager            │  50-200ms        │  0.7s                              │       │
│  │  C1 → C6: Schema Validator               │  50-200ms        │  0.9s                              │       │
│  │  C1 → C7: Business Validator             │  100-300ms       │  1.2s                              │       │
│  │  C1 → C8: Transformer                    │  50-150ms        │  1.35s                             │       │
│  │  C1 → C9: Publish to Service Bus         │  10-50ms         │  1.4s                              │       │
│  │  C9 queuing (waiting for batch)          │  0-300s          │  Variable (batch trigger)          │       │
│  │  C3 → C11: API Management call           │  1000-3000ms     │  +2s (per message in batch)        │       │
│  │  C11 → E2: 3rd party API call            │  Included above  │                                    │       │
│  │  C3: Complete Service Bus message        │  10-50ms         │  +0.05s                            │       │
│  ├──────────────────────────────────────────┼──────────────────┼────────────────────────────────────┤       │
│  │  TOTAL (poll to API call):               │                  │  ~3.5 seconds + queue wait         │       │
│  └──────────────────────────────────────────┴──────────────────┴────────────────────────────────────┘       │
│                                                                                                             │
│                                                                                                             │
│  DATA TRANSFORMATIONS:                                                                                      │
│  ═════════════════════                                                                                      │
│                                                                                                             │
│  1. ERP SQL Result Set → C1 internal JSON representation                                                    │
│  2. JSON → C5 HTTP request (correlation resolution)                                                         │
│  3. JSON + CorrelationId/Guid → C6 HTTP request (schema validation)                                         │
│  4. JSON + CorrelationId/Guid → C7 HTTP request (business validation)                                       │
│  5. ERP XML + CorrelationId/Guid → C8 HTTP request (transformation)                                         │
│  6. 3rd Party XML → Service Bus message (Base64-encoded body + properties)                                  │
│  7. Service Bus message → C3 (decoded XML)                                                                  │
│  8. 3rd Party XML + headers → C11 HTTP request (API Management)                                             │
│  9. 3rd Party XML + API key → E2 HTTP request (external API)                                                │
│                                                                                                             │
│                                                                                                             │
│  CORRELATION CONTEXT PROPAGATION:                                                                           │
│  ════════════════════════════════                                                                           │
│                                                                                                             │
│  • Generated by C5 on first encounter: CorrelationId + CorrelationGuid                                      │
│  • Propagated through:                                                                                      │
│    - HTTP request headers (X-CorrelationId, X-CorrelationGuid) between C1 ↔ C5/C6/C7/C8                    │
│    - Service Bus message properties (ApplicationProperties)                                                 │
│    - XML payload (<correlationGuid> element injected by C8)                                                 │
│    - Application Insights customDimensions (all components)                                                 │
│    - Table Storage records (CorrelationMapping, MessageAuditLog)                                            │
│    - Blob Storage metadata and folder paths                                                                 │
│    - API Management logs and traces                                                                         │
│    - 3rd party API request/response (correlationGuid echoed back)                                           │
│                                                                                                             │
│                                                                                                             │
│  ERROR HANDLING POINTS:                                                                                     │
│  ═════════════════════                                                                                      │
│                                                                                                             │
│  • E1 connection failure: C1 retries SQL call 3x, then fails workflow                                       │
│  • C5/C6/C7/C8 HTTP failure: C1 logs error, continues with next shipment (don't fail entire batch)          │
│  • C6 schema validation failure: C1 sends to DLQ, triggers C4 (Alert Handler)                               │
│  • C7 business validation failure: C1 sends to DLQ, triggers C4                                             │
│  • C9 publish failure: Service Bus SDK auto-retries, C1 workflow fails if all retries exhausted             │
│  • C11 circuit breaker open: Returns 503, C3 abandons message (will retry)                                  │
│  • E2 API failure (5xx): APIM retries 3x, if still fails, C3 abandons message                               │
│  • E2 API failure (4xx): C3 dead-letters immediately (no retry), triggers C4                                │
│  • Service Bus max delivery count: Message moved to DLQ, triggers C4                                        │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘


