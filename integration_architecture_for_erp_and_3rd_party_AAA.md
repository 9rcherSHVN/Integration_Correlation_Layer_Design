# Copilot Chat Conversation Export: Integration architecture for ERP and 3rd-party

**User:** @Dong-Shen-Metrotown
**Thread URL:** https://github.com/copilot/c/8a0698b8-ebaa-48cc-99dc-9befd951eb51

## @Dong-Shen-Metrotown



You are a master in Azure Integration Services, specializing custom application integration implementation.

This is a general integration architecture and design question.

Platform: Azure Cloud and available Azure services.

1. Logic Apps
2. API Management
3. Azure Functions
4. Event Grid
5. Service Bus
6. Data Factory
7. Blob Storage
8. Relays
9. Key Vaults
10. On-Premise Data Gateway
11. Integration Accounts

Task:  to define a business application integration design architecture by utilizing above available Azure cloud services.

###use case###:

1. an ERP application server hosted on-premise - private network (behind the firewall) which a master of business application source system.
2. 3rd-party cloud based enterprise application, such as a truck shipment procurement business entity. It exposes REST APIs for

Inbound processes: new shipment submission, shipment information updates, such as shipment weights, shipment destination, and shipment due date and time, etc.
Outbound processes: shipment pick-up appointments, shipment cost estimation, shipment delivery status, and shipment invoice, etc.

3. The ERP application server will transact with this 3rd-party cloud based enterprise application via its REST APIs - exchanging business messages in XML over both inbound and outbound processes.


Question:

first repeat my intent here in your words. let me review it. If you have any questions, do ask.

## Copilot

## My Understanding of Your Integration Requirement

Let me restate your intent to ensure we're aligned:

---

### **Business Scenario**

You need to design an **enterprise integration architecture** on Azure that enables **bidirectional communication** between:

1. **Source System**: An **on-premise ERP application server** located in a private network behind a corporate firewall. This is your **system of record** (master data source).

2. **Target System**: A **3rd-party cloud-based shipment/logistics application** that exposes REST APIs for procurement and transportation management.

---

### **Integration Patterns Required**

**Inbound (ERP → 3rd Party):**
- Push new shipment requests
- Send shipment updates (weights, destinations, due dates, etc.)

**Outbound (3rd Party → ERP):**
- Receive pick-up appointment confirmations
- Receive cost estimations
- Receive delivery status updates
- Receive invoices

---

### **Technical Constraints**
- The ERP system communicates using **XML message format**
- The 3rd-party application uses **REST APIs** (likely expecting/returning JSON)
- The on-premise ERP is **not publicly accessible** (firewall-protected)
- You want to leverage the **11 Azure Integration Services** you listed

---

### **Clarifying Questions**

Before I propose an architecture, I'd like to understand:

1. **Initiation Pattern**: For outbound processes (3rd party → ERP), does the 3rd party **push** events via webhooks, or must you **poll** their APIs?

2. **ERP Capabilities**: Can the ERP trigger outbound HTTP calls, or does it only expose APIs/drop files for pickup?

3. **Volume & Latency**: What's the expected transaction volume, and do you need real-time or near-real-time processing?

4. **Transformation Complexity**: Are the XML schemas complex (requiring XSLT/maps), or relatively simple structures?

Please confirm or correct my understanding, and I'll proceed with the architecture design.

## @Dong-Shen-Metrotown

ERP system will not directly push or pull from 3rd-party application.

All new/update triggers are initiated by this integration layer (to be designed here hosted in Azure Cloud):

1. For outbound process: this integration layer will pull outgoing shipments from the ERP system (either by direct DB stored procedure calls or by its REST APIs) and push to the 3rd-party application via REST APIs.
2. For inbound process: this integration layer will pull shipment updates from the 3rd-party application via REST APIs and push to the ERP system (either by direct DB transact, such as calling stored procedures, or by calling REST APIs).
3. Yes, this integration lay acts as a middle man - moving shipment information back and forth between two systems.
4. This is a  near real-time process - estimated 500 messages each hour in peak.
5. Yes, XML message transformation is required. Business message schema is different.

Ask questions if you have any.

## Copilot

## Updated Understanding

Thank you for the clarification. Let me restate the refined requirements:

---

### **Integration Layer Role**

The Azure-hosted integration layer acts as an **orchestration middleware** that:

- **Initiates all data movement** — neither the ERP nor the 3rd-party system pushes data proactively
- **Pulls and pushes bidirectionally** between both systems
- **Transforms messages** between differing XML schemas

---

### **Data Flow Patterns**

**Outbound (ERP → 3rd Party Shipment App):**
1. Integration layer **polls/pulls** from ERP (via stored procedures or REST APIs)
2. **Transforms** ERP XML schema → 3rd-party expected format
3. **Pushes** to 3rd-party REST APIs

**Inbound (3rd Party → ERP):**
1. Integration layer **polls/pulls** from 3rd-party REST APIs (appointments, costs, status, invoices)
2. **Transforms** 3rd-party response → ERP XML schema
3. **Pushes** to ERP (via stored procedures or REST APIs)

---

### **Technical Characteristics**

| Aspect | Detail |
|--------|--------|
| **Processing Model** | Near real-time, polling-based |
| **Volume** | ~500 messages/hour at peak |
| **Message Format** | XML on both ends, but **different schemas** |
| **Transformation** | Required (likely XSLT or mapping) |
| **ERP Connectivity** | Stored procedures (direct DB) **or** REST APIs |
| **ERP Network** | On-premise, behind firewall |

---

### **Clarifying Questions**

1. **ERP Connectivity Preference**: Do you have a preference between **stored procedure calls** vs **REST APIs** for ERP communication, or should the architecture support both options?

2. **Polling Frequency**: For near real-time, are you targeting a specific interval (e.g., every 1 minute, 5 minutes)?

3. **Error Handling & Retry**: Do you need robust retry mechanisms, dead-letter queues, and alerting for failed transactions?

4. **Security Requirements**: 
   - How does the 3rd-party API authenticate (API Key, OAuth 2.0, certificates)?
   - Does the ERP require specific authentication for its APIs/database?

5. **Idempotency**: Should the integration handle duplicate detection to prevent reprocessing the same shipment?

6. **Monitoring & Auditing**: Do you need full message tracking, audit logs, and operational dashboards?

Once confirmed, I'll present the architecture design using the Azure services you listed.

## @Dong-Shen-Metrotown

1. support both options; 2. 5 minutes; 3. robust retry; 4. API Key; Based-Authentication; 5. yes - prevent reprocessing the same shipment record or updates; 6. full message tracking and audit logs.

## Copilot

## Final Confirmed Requirements

Excellent! Here's the complete picture before I proceed with the architecture design:

---

### **Integration Layer Specifications**

| Requirement | Decision |
|-------------|----------|
| **ERP Connectivity** | Support both stored procedures (DB) and REST APIs |
| **Polling Frequency** | Every 5 minutes |
| **Error Handling** | Robust retry with dead-letter queues |
| **3rd-Party Auth** | API Key |
| **ERP Auth** | Basic Authentication |
| **Idempotency** | Duplicate detection to prevent reprocessing |
| **Observability** | Full message tracking and audit logs |

---

### **Summary of Integration Design Scope**

| Aspect | Detail |
|--------|--------|
| **Source System** | On-premise ERP (private network, behind firewall) |
| **Target System** | 3rd-party cloud shipment app (REST APIs) |
| **Direction** | Bidirectional (Outbound & Inbound) |
| **Trigger Model** | Polling-based, initiated by Azure integration layer |
| **Frequency** | Every 5 minutes |
| **Volume** | ~500 messages/hour peak |
| **Message Format** | XML (different schemas on each side) |
| **Transformation** | XSLT/Schema mapping required |
| **ERP Access Methods** | Stored procedures OR REST APIs |
| **Retry & Resilience** | Dead-letter queues, retry policies |
| **Duplicate Prevention** | Idempotency checks |
| **Security** | API Key (3rd party), Basic Auth (ERP), secrets in Key Vault |
| **Monitoring** | Full audit trail, message tracking |

---

### **One Final Question**

For **duplicate detection/idempotency**, what unique identifier should be used?

- A **shipment ID** from the ERP system?
- A **combination of fields** (e.g., shipment ID + timestamp)?
- Or should the integration layer **generate a correlation ID** and track state?

Once you confirm, I'll present the **full architecture design** with component mapping to the 11 Azure services you listed.

## @Dong-Shen-Metrotown

3rd-party application will provide a unique identifier for each shipment message;  Question: what is the best way to track any shipment changes in ERP system? Any good suggestions that fit into this middle integration layer?

## Copilot

## Great Question — Tracking ERP Shipment Changes

This is a critical design consideration. Since the integration layer **initiates polling** (ERP doesn't push), you need a reliable mechanism to detect **new and changed shipment records**.

Here are the common approaches, ranked by suitability for your scenario:

---

### **Option 1: Timestamp-Based Change Tracking (Recommended)**

**How it works:**
- ERP maintains a `LastModifiedTimestamp` column on shipment records
- Integration layer stores the **last successful poll timestamp**
- Each poll queries: `WHERE LastModifiedTimestamp > @LastPollTimestamp`

**Pros:**
- Simple to implement
- Minimal ERP changes required
- Works with both stored procedures and REST APIs

**Cons:**
- Requires ERP to reliably update timestamps on every change
- Clock synchronization considerations

**Azure Implementation:**
- Store `LastPollTimestamp` in **Blob Storage** (simple) or **Table Storage**
- Logic App retrieves timestamp → polls ERP → updates timestamp on success

---

### **Option 2: Change Flag / Outbox Pattern**

**How it works:**
- ERP sets a flag (e.g., `PendingSync = true`) on new/updated shipments
- Integration layer queries: `WHERE PendingSync = true`
- After successful sync, integration layer resets the flag to `false`

**Pros:**
- Explicit change tracking
- No timestamp drift issues
- Clear acknowledgment of processed records

**Cons:**
- Requires ERP modification (add flag column)
- Requires write-back capability to ERP

**Azure Implementation:**
- Logic App polls → processes → calls ERP stored procedure/API to reset flag

---

### **Option 3: Dedicated Outbox Table in ERP**

**How it works:**
- ERP application writes to a dedicated `ShipmentOutbox` table on any shipment change
- Integration layer reads from outbox → processes → deletes/marks as processed

**Pros:**
- Clean separation of concerns
- Native support for idempotency (each outbox row = one message)
- ERP performance isolated from integration polling

**Cons:**
- Requires ERP development effort
- Additional table maintenance

**Azure Implementation:**
- Logic App polls outbox table via **On-Premise Data Gateway**
- After successful delivery, delete outbox record

---

### **Option 4: Change Data Capture (CDC)**

**How it works:**
- Enable database-level CDC (SQL Server, Oracle, etc.)
- Capture all inserts/updates automatically at DB level

**Pros:**
- No ERP application changes required
- Captures all changes, including direct DB modifications

**Cons:**
- Database-specific implementation
- More complex setup
- May capture unwanted changes (need filtering)

**Azure Implementation:**
- **Azure Data Factory** with CDC connectors, or
- Custom **Azure Function** reading CDC tables via Data Gateway

---

### **My Recommendation for Your Scenario**

| Approach | Fit for Your Case |
|----------|-------------------|
| **Timestamp-Based** | ✅ **Best fit** — minimal ERP change, works with SP & REST |
| **Change Flag** | ✅ Good alternative if ERP can support write-back |
| **Outbox Table** | ⚠️ Good but requires ERP development |
| **CDC** | ⚠️ Overkill for 500 msg/hour; adds complexity |

---

### **Recommended Implementation**

```
┌─────────────────────────────────────────────────────────────┐
│                  CHANGE TRACKING DESIGN                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. ERP maintains "LastModifiedTimestamp" on shipments      │
│                                                             │
│  2. Azure Blob Storage holds:                               │
│     • LastSuccessfulPollTimestamp (per process type)        │
│     • ProcessedShipmentIDs (for idempotency)                │
│                                                             │
│  3. Each 5-min poll cycle:                                  │
│     a. Read LastPollTimestamp from Blob                     │
│     b. Query ERP: WHERE ModifiedTime > LastPollTimestamp    │
│     c. Check each ShipmentID against ProcessedIDs           │
│     d. Process only new/changed records                     │
│     e. Update LastPollTimestamp on success                  │
│     f. Store 3rd-party unique ID for idempotency tracking   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

### **Clarifying Questions**

1. Does your ERP already have a `LastModifiedTimestamp` or similar audit column on shipment records?

2. Can the ERP expose a **delta query** (changed records since X) via stored procedure or REST API?

3. For idempotency tracking, should we maintain a **rolling window** (e.g., last 7 days of processed IDs) or **permanent history**?

Let me know, and I'll proceed with the **full architecture diagram and component mapping**!

## @Dong-Shen-Metrotown

OK - go with "Timestamp-Based" change tracking option. Finalize the design requirements.

## Copilot

## Finalized Design Requirements

Here is the complete and confirmed specification for the Azure Integration Architecture:

---

### **1. Business Context**

| Aspect | Description |
|--------|-------------|
| **Source System** | On-premise ERP application server (private network, behind firewall) |
| **Target System** | 3rd-party cloud-based shipment/logistics application |
| **Integration Purpose** | Bidirectional shipment data exchange for procurement operations |
| **Integration Layer** | Azure-hosted middleware (orchestration & transformation) |

---

### **2. Integration Patterns**

#### **Outbound Process (ERP → 3rd Party)**

| Step | Action |
|------|--------|
| 1 | Poll ERP for new/changed shipments (using `LastModifiedTimestamp`) |
| 2 | Transform ERP XML schema → 3rd-party XML schema |
| 3 | Push to 3rd-party REST APIs |
| 4 | Store 3rd-party unique shipment ID for tracking |

**Message Types:**
- New shipment submissions
- Shipment updates (weights, destinations, due dates, etc.)

#### **Inbound Process (3rd Party → ERP)**

| Step | Action |
|------|--------|
| 1 | Poll 3rd-party REST APIs for updates |
| 2 | Transform 3rd-party XML schema → ERP XML schema |
| 3 | Push to ERP (via stored procedures or REST APIs) |
| 4 | Mark records as processed for idempotency |

**Message Types:**
- Shipment pick-up appointments
- Shipment cost estimations
- Shipment delivery status
- Shipment invoices

---

### **3. Technical Specifications**

| Specification | Value |
|---------------|-------|
| **Polling Frequency** | Every 5 minutes |
| **Peak Volume** | ~500 messages/hour |
| **Processing Model** | Near real-time, polling-based |
| **Message Format** | XML (different schemas per system) |
| **Transformation** | XSLT / Schema mapping required |
| **ERP Connectivity** | Stored procedures AND/OR REST APIs |

---

### **4. Change Tracking (Timestamp-Based)**

| Component | Detail |
|-----------|--------|
| **ERP Requirement** | `LastModifiedTimestamp` column on shipment records |
| **State Storage** | Azure Blob Storage (or Table Storage) |
| **Tracked State** | `LastSuccessfulPollTimestamp` per process type |
| **Poll Query Logic** | `WHERE LastModifiedTimestamp > @LastPollTimestamp` |
| **Timestamp Update** | Only on successful processing |

---

### **5. Idempotency & Duplicate Prevention**

| Aspect | Implementation |
|--------|----------------|
| **Outbound** | Track processed ERP shipment IDs + timestamps |
| **Inbound** | Track 3rd-party unique shipment message IDs |
| **Storage** | Azure Table Storage or SQL (for queryable history) |
| **Retention** | Rolling window (configurable, e.g., 30 days) |

---

### **6. Security Requirements**

| System | Authentication Method | Secret Storage |
|--------|----------------------|----------------|
| **3rd-Party APIs** | API Key | Azure Key Vault |
| **ERP REST APIs** | Basic Authentication | Azure Key Vault |
| **ERP Database** | SQL Authentication / Windows Auth | Azure Key Vault |
| **On-Premise Connectivity** | On-Premise Data Gateway | Managed by Azure |

---

### **7. Error Handling & Resilience**

| Requirement | Implementation |
|-------------|----------------|
| **Retry Policy** | Exponential backoff with configurable max retries |
| **Dead-Letter Queue** | Azure Service Bus dead-letter queue for failed messages |
| **Alerting** | Notifications on failures (email, Teams, etc.) |
| **Circuit Breaker** | Pause processing if repeated failures detected |

---

### **8. Observability & Auditing**

| Requirement | Implementation |
|-------------|----------------|
| **Message Tracking** | Full payload logging (before/after transformation) |
| **Correlation ID** | Unique ID assigned to each message flow |
| **Audit Logs** | Timestamp, source, destination, status, error details |
| **Dashboards** | Operational monitoring (success/failure rates, latency) |
| **Log Storage** | Azure Log Analytics / Application Insights |

---

### **9. Azure Services Mapping (Preview)**

| Azure Service | Role in Architecture |
|---------------|----------------------|
| **Logic Apps** | Orchestration, polling, workflow execution |
| **API Management** | API facade, throttling, security policies |
| **Azure Functions** | Custom transformation, complex logic |
| **Service Bus** | Message queuing, dead-letter handling, decoupling |
| **Event Grid** | Event-driven triggers (optional for notifications) |
| **Data Factory** | Alternative for batch/bulk data movement |
| **Blob Storage** | State management, audit log storage |
| **Key Vault** | Secrets management (API keys, credentials) |
| **On-Premise Data Gateway** | Secure connectivity to ERP (DB & APIs) |
| **Integration Accounts** | XSLT maps, XML schemas, B2B artifacts |
| **Relays** | Hybrid connection alternative (if needed) |

---

### **10. Summary Checklist**

| Requirement | Confirmed |
|-------------|-----------|
| Bidirectional integration | ✅ |
| Polling-based (5 min interval) | ✅ |
| Timestamp-based change tracking | ✅ |
| XML schema transformation | ✅ |
| Support both SP and REST for ERP | ✅ |
| Idempotency / duplicate prevention | ✅ |
| Robust retry with dead-letter | ✅ |
| API Key auth (3rd party) | ✅ |
| Basic Auth (ERP) | ✅ |
| Secrets in Key Vault | ✅ |
| Full message tracking & audit | ✅ |
| Near real-time (~500 msg/hour) | ✅ |

---

## Next Step

**Shall I now proceed with the full architecture design**, including:

1. **Architecture diagram** (component relationships)
2. **Detailed component design** (each Azure service's role)
3. **Data flow diagrams** (outbound & inbound processes)
4. **Error handling flow**
5. **Security architecture**

Please confirm, or let me know if any adjustments are needed!

## @Dong-Shen-Metrotown

Yes, confirm to proceed with the full architecture design.

*System: User retried message*

## Copilot

## Full Architecture Design

### **Azure Integration Services Architecture for ERP ↔ 3rd-Party Shipment Application**

---

## **1. High-Level Architecture Diagram**

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                          AZURE CLOUD                                                        │
│                                                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                                    API MANAGEMENT (APIM)                                            │   │
│  │                         • API Facade & Gateway • Rate Limiting • Security Policies                  │   │
│  └─────────────────────────────────────────┬───────────────────────────────────────────────────────────┘   │
│                                            │                                                               │
│  ┌─────────────────────────────────────────┼───────────────────────────────────────────────────────────┐   │
│  │                              INTEGRATION LAYER                                                      │   │
│  │                                                                                                     │   │
│  │   ┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐   │   │
│  │   │   LOGIC APPS    │      │  SERVICE BUS    │      │ AZURE FUNCTIONS │      │  INTEGRATION    │   │   │
│  │   │  (Standard)     │      │                 │      │                 │      │  ACCOUNTS       │   │   │
│  │   │                 │      │  • Queues       │      │  • Complex      │      │                 │   │   │
│  │   │  • Outbound     │◄────►│  • Dead-Letter  │◄────►│    Transform    │◄────►│  • XSLT Maps    │   │   │
│  │   │    Workflow     │      │  • Sessions     │      │  • Validation   │      │  • XML Schemas  │   │   │
│  │   │  • Inbound      │      │                 │      │  • Custom Logic │      │  • Agreements   │   │   │
│  │   │    Workflow     │      │                 │      │                 │      │                 │   │   │
│  │   └────────┬────────┘      └─────────────────┘      └─────────────────┘      └─────────────────┘   │   │
│  │            │                                                                                        │   │
│  └────────────┼────────────────────────────────────────────────────────────────────────────────────────┘   │
│               │                                                                                             │
│  ┌────────────┼────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │            │                         SUPPORTING SERVICES                                            │   │
│  │            │                                                                                        │   │
│  │   ┌────────▼────────┐      ┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐   │   │
│  │   │  BLOB STORAGE   │      │   KEY VAULT     │      │   EVENT GRID    │      │  LOG ANALYTICS  │   │   │
│  │   │                 │      │                 │      │                 │      │  & APP INSIGHTS │   │   │
│  │   │  • Poll State   │      │  • API Keys     │      │  • Failure      │      │                 │   │   │
│  │   │  • Audit Logs   │      │  • Credentials  │      │    Alerts       │      │  • Tracking     │   │   │
│  │   │  • Idempotency  │      │  • Certificates │      │  • Notifications│      │  • Dashboards   │   │   │
│  │   │    Tracking     │      │                 │      │                 │      │  • Audit Logs   │   │   │
│  │   └─────────────────┘      └─────────────────┘      └─────────────────┘      └─────────────────┘   │   │
│  │                                                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                             │
└──────────────────────────────────┬─────────────────────────────────────────┬────────────────────────────────┘
                                   │                                         │
                                   │ ON-PREMISE DATA GATEWAY                 │ HTTPS (Outbound)
                                   │ (Hybrid Connection)                     │
                                   ▼                                         ▼
┌─────────────────────────────────────────────────┐     ┌─────────────────────────────────────────────────┐
│              ON-PREMISE NETWORK                 │     │            3RD-PARTY CLOUD                      │
│              (Behind Firewall)                  │     │                                                 │
│                                                 │     │                                                 │
│   ┌─────────────────────────────────────────┐   │     │   ┌─────────────────────────────────────────┐   │
│   │           ERP APPLICATION               │   │     │   │      SHIPMENT APPLICATION              │   │
│   │                                         │   │     │   │                                         │   │
│   │   • Shipment Master Data                │   │     │   │   • REST APIs                          │   │
│   │   • LastModifiedTimestamp               │   │     │   │   • Unique Shipment IDs                │   │
│   │   • Stored Procedures                   │   │     │   │   • API Key Authentication             │   │
│   │   • REST APIs (Optional)                │   │     │   │                                         │   │
│   └─────────────────────────────────────────┘   │     │   └─────────────────────────────────────────┘   │
│                                                 │     │                                                 │
└─────────────────────────────────────────────────┘     └─────────────────────────────────────────────────┘
```

---

## **2. Detailed Component Design**

### **2.1 Logic Apps (Standard) — Orchestration Engine**

| Aspect | Description |
|--------|-------------|
| **SKU** | Standard (for stateful workflows, better performance) |
| **Role** | Primary orchestration, polling, workflow coordination |

**Workflows to Create:**

| Workflow Name | Trigger | Purpose |
|---------------|---------|---------|
| `Outbound-Shipment-Poller` | Recurrence (5 min) | Poll ERP for new/changed shipments |
| `Outbound-Shipment-Processor` | Service Bus Queue | Process & send to 3rd-party |
| `Inbound-Appointment-Poller` | Recurrence (5 min) | Poll 3rd-party for appointments |
| `Inbound-CostEstimate-Poller` | Recurrence (5 min) | Poll 3rd-party for cost estimates |
| `Inbound-DeliveryStatus-Poller` | Recurrence (5 min) | Poll 3rd-party for delivery status |
| `Inbound-Invoice-Poller` | Recurrence (5 min) | Poll 3rd-party for invoices |
| `Inbound-Message-Processor` | Service Bus Queue | Process & send to ERP |
| `DeadLetter-Handler` | Service Bus Dead-Letter | Handle failed messages |

---

### **2.2 Service Bus — Message Queuing & Decoupling**

| Aspect | Description |
|--------|-------------|
| **SKU** | Standard or Premium (Premium for larger messages) |
| **Role** | Decouple polling from processing, enable retry, dead-letter |

**Queue Configuration:**

| Queue Name | Purpose | Settings |
|------------|---------|----------|
| `outbound-shipment-queue` | Outbound shipment messages | Sessions enabled, TTL 24h |
| `inbound-appointment-queue` | Inbound appointments | Sessions enabled, TTL 24h |
| `inbound-costestimate-queue` | Inbound cost estimates | Sessions enabled, TTL 24h |
| `inbound-deliverystatus-queue` | Inbound delivery status | Sessions enabled, TTL 24h |
| `inbound-invoice-queue` | Inbound invoices | Sessions enabled, TTL 24h |

**Dead-Letter Configuration:**

| Setting | Value |
|---------|-------|
| Max Delivery Count | 5 |
| Dead-Letter on Expiration | Enabled |
| Dead-Letter Reason Tracking | Enabled |

---

### **2.3 Azure Functions — Transformation & Custom Logic**

| Aspect | Description |
|--------|-------------|
| **SKU** | Consumption or Premium (Premium for VNET integration) |
| **Runtime** | .NET 6+ or Node.js |
| **Role** | Complex XML transformations, validation, custom business logic |

**Functions to Create:**

| Function Name | Trigger | Purpose |
|---------------|---------|---------|
| `TransformErpToThirdParty` | HTTP | Transform ERP XML → 3rd-party XML schema |
| `TransformThirdPartyToErp` | HTTP | Transform 3rd-party XML → ERP XML schema |
| `ValidateShipmentPayload` | HTTP | Validate XML structure and business rules |
| `CheckIdempotency` | HTTP | Check if message already processed |
| `UpdateIdempotencyStore` | HTTP | Mark message as processed |
| `GetPollState` | HTTP | Retrieve last poll timestamp |
| `UpdatePollState` | HTTP | Update last poll timestamp |

---

### **2.4 Integration Accounts — B2B Artifacts**

| Aspect | Description |
|--------|-------------|
| **SKU** | Standard |
| **Role** | Store and manage XML schemas, XSLT maps |

**Artifacts to Upload:**

| Artifact Type | Name | Purpose |
|---------------|------|---------|
| **Schema** | `ERP-Shipment-Schema.xsd` | ERP shipment XML structure |
| **Schema** | `ThirdParty-Shipment-Schema.xsd` | 3rd-party shipment XML structure |
| **Schema** | `ERP-Appointment-Schema.xsd` | ERP appointment XML structure |
| **Schema** | `ThirdParty-Appointment-Schema.xsd` | 3rd-party appointment XML structure |
| **Map** | `ERP-To-ThirdParty-Shipment.xslt` | Outbound transformation |
| **Map** | `ThirdParty-To-ERP-Appointment.xslt` | Inbound appointment transformation |
| **Map** | `ThirdParty-To-ERP-Invoice.xslt` | Inbound invoice transformation |

---

### **2.5 API Management (APIM) — API Gateway**

| Aspect | Description |
|--------|-------------|
| **SKU** | Developer / Standard / Premium |
| **Role** | API facade, security policies, rate limiting, logging |

**API Configuration:**

| API | Backend | Purpose |
|-----|---------|---------|
| `third-party-shipment-api` | 3rd-party REST endpoints | Proxy with API key injection |
| `erp-rest-api` | ERP REST endpoints (via Gateway) | Proxy with Basic Auth |
| `internal-transform-api` | Azure Functions | Expose transformation functions |

**Policies to Apply:**

| Policy | Purpose |
|--------|---------|
| `set-header` | Inject API Key for 3rd-party calls |
| `authentication-basic` | Inject Basic Auth for ERP calls |
| `rate-limit` | Prevent throttling by 3rd-party |
| `retry` | Automatic retry on transient failures |
| `log-to-eventhub` | Full request/response logging |

---

### **2.6 Blob Storage — State & Audit Management**

| Aspect | Description |
|--------|-------------|
| **SKU** | Standard LRS |
| **Role** | Poll state persistence, idempotency tracking, audit logs |

**Container Structure:**

```
shipment-integration-storage/
├── poll-state/
│   ├── outbound-shipment-lastpoll.json
│   ├── inbound-appointment-lastpoll.json
│   ├── inbound-costestimate-lastpoll.json
│   ├── inbound-deliverystatus-lastpoll.json
│   └── inbound-invoice-lastpoll.json
├── idempotency/
│   ├── processed-outbound-{date}.json
│   └── processed-inbound-{date}.json
├── audit-logs/
│   ├── {year}/{month}/{day}/
│   │   ├── outbound-{correlationId}.json
│   │   └── inbound-{correlationId}.json
└── failed-messages/
    ├── {year}/{month}/{day}/
    │   └── {correlationId}.json
```

**Poll State JSON Structure:**

```json
{
  "processType": "outbound-shipment",
  "lastSuccessfulPollTimestamp": "2026-01-29T10:30:00Z",
  "lastPollStatus": "success",
  "recordsProcessed": 15,
  "updatedAt": "2026-01-29T10:35:00Z"
}
```

---

### **2.7 Key Vault — Secrets Management**

| Aspect | Description |
|--------|-------------|
| **Role** | Centralized secrets storage, access control |

**Secrets to Store:**

| Secret Name | Purpose |
|-------------|---------|
| `ThirdParty-ApiKey` | API Key for 3rd-party application |
| `ERP-BasicAuth-Username` | ERP REST API username |
| `ERP-BasicAuth-Password` | ERP REST API password |
| `ERP-DB-ConnectionString` | ERP database connection string |
| `ServiceBus-ConnectionString` | Service Bus connection |
| `Storage-ConnectionString` | Blob Storage connection |

---

### **2.8 On-Premise Data Gateway — Hybrid Connectivity**

| Aspect | Description |
|--------|-------------|
| **Role** | Secure tunnel from Azure to on-premise ERP |
| **Installation** | On a Windows server within ERP network |

**Supported Connections:**

| Connection Type | Purpose |
|-----------------|---------|
| SQL Server Connector | Execute stored procedures |
| HTTP Connector | Call ERP REST APIs |
| File System Connector | Optional file-based integration |

---

### **2.9 Event Grid — Event-Driven Notifications**

| Aspect | Description |
|--------|-------------|
| **Role** | Trigger alerts on failures, notify stakeholders |

**Event Subscriptions:**

| Event Source | Event Type | Target |
|--------------|------------|--------|
| Service Bus | Dead-Letter Message | Logic App (Alert Handler) |
| Blob Storage | Blob Created (failed-messages) | Azure Function (Notification) |

---

### **2.10 Log Analytics & Application Insights — Observability**

| Aspect | Description |
|--------|-------------|
| **Role** | Centralized logging, monitoring, dashboards |

**Tracking Data:**

| Data Point | Source | Purpose |
|------------|--------|---------|
| Correlation ID | All components | End-to-end message tracing |
| Request/Response Payloads | APIM, Logic Apps | Audit trail |
| Transformation Logs | Azure Functions | Debug transformation issues |
| Error Details | All components | Failure analysis |
| Performance Metrics | All components | Latency, throughput monitoring |

---

## **3. Data Flow Diagrams**

### **3.1 Outbound Flow (ERP → 3rd-Party)**

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                    OUTBOUND FLOW: ERP → 3RD-PARTY                                          │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

    ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐
    │  STEP 1  │      │  STEP 2  │      │  STEP 3  │      │  STEP 4  │      │  STEP 5  │      │  STEP 6  │
    │  POLL    │─────►│  QUERY   │─────►│  QUEUE   │─────►│ TRANSFORM│─────►│   SEND   │─────►│  UPDATE  │
    │  TRIGGER │      │  ERP     │      │  MESSAGE │      │  PAYLOAD │      │  TO 3RD  │      │  STATE   │
    └──────────┘      └──────────┘      └──────────┘      └──────────┘      └──────────┘      └──────────┘
         │                 │                 │                 │                 │                 │
         ▼                 ▼                 ▼                 ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │
│  │             │  │             │  │             │  │             │  │             │  │             │      │
│  │  Logic App  │  │   On-Prem   │  │  Service    │  │   Azure     │  │    APIM     │  │    Blob     │      │
│  │  (Poller)   │  │   Gateway   │  │    Bus      │  │  Function   │  │             │  │  Storage    │      │
│  │             │  │     +       │  │   Queue     │  │     +       │  │             │  │             │      │
│  │  Recurrence │  │    ERP      │  │             │  │ Integration │  │             │  │             │      │
│  │  5 minutes  │  │             │  │             │  │  Account    │  │             │  │             │      │
│  │             │  │             │  │             │  │             │  │             │  │             │      │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘      │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

DETAILED STEPS:

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 1: POLL TRIGGER                                                                                        │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • Logic App "Outbound-Shipment-Poller" triggers every 5 minutes                                            │
│ • Retrieves LastPollTimestamp from Blob Storage                                                            │
│ • Generates unique CorrelationId for this poll cycle                                                       │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 2: QUERY ERP                                                                                           │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • Connect to ERP via On-Premise Data Gateway                                                               │
│ • Execute: Stored Procedure or REST API                                                                    │
│ • Query: WHERE LastModifiedTimestamp > @LastPollTimestamp                                                  │
│ • Retrieve new/changed shipment records (XML format)                                                       │
│ • Check each record against idempotency store                                                              │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 3: QUEUE MESSAGE                                                                                       │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • For each shipment record:                                                                                │
│   - Assign unique MessageId                                                                                │
│   - Set SessionId = ShipmentId (for ordered processing)                                                    │
│   - Add CorrelationId to message properties                                                                │
│   - Send to "outbound-shipment-queue"                                                                      │
│ • Log message details to audit store                                                                       │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 4: TRANSFORM PAYLOAD                                                                                   │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • Logic App "Outbound-Shipment-Processor" receives message from queue                                      │
│ • Validate XML against ERP schema (Integration Account)                                                    │
│ • Call Azure Function for transformation                                                                   │
│ • Apply XSLT map: ERP XML → 3rd-Party XML                                                                  │
│ • Validate output against 3rd-party schema                                                                 │
│ • Log before/after payloads to audit store                                                                 │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 5: SEND TO 3RD-PARTY                                                                                   │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • Call 3rd-party REST API via APIM                                                                         │
│ • APIM injects API Key from Key Vault                                                                      │
│ • POST transformed XML payload                                                                             │
│ • Receive response with 3rd-party unique ShipmentId                                                        │
│ • On failure: Retry with exponential backoff (max 5 attempts)                                              │
│ • On persistent failure: Move to dead-letter queue                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 6: UPDATE STATE                                                                                        │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • On success:                                                                                              │
│   - Update LastPollTimestamp in Blob Storage                                                               │
│   - Store 3rd-party ShipmentId in idempotency store                                                        │
│   - Log success status to audit                                                                            │
│   - Complete message (remove from queue)                                                                   │
│ • On failure:                                                                                              │
│   - Log error details                                                                                      │
│   - Trigger Event Grid alert                                                                               │
│   - Store failed message in failed-messages container                                                      │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

### **3.2 Inbound Flow (3rd-Party → ERP)**

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                    INBOUND FLOW: 3RD-PARTY → ERP                                           │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

    ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐
    │  STEP 1  │      │  STEP 2  │      │  STEP 3  │      │  STEP 4  │      │  STEP 5  │      │  STEP 6  │
    │  POLL    │─────►│  QUERY   │─────►│  QUEUE   │─────►│ TRANSFORM│─────►│  SEND    │─────►│  UPDATE  │
    │  TRIGGER │      │ 3RD-PARTY│      │  MESSAGE │      │  PAYLOAD │      │  TO ERP  │      │  STATE   │
    └──────────┘      └──────────┘      └──────────┘      └──────────┘      └──────────┘      └──────────┘
         │                 │                 │                 │                 │                 │
         ▼                 ▼                 ▼                 ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │
│  │             │  │             │  │             │  │             │  │             │  │             │      │
│  │  Logic App  │  │    APIM     │  │  Service    │  │   Azure     │  │   On-Prem   │  │    Blob     │      │
│  │  (Poller)   │  │      +      │  │    Bus      │  │  Function   │  │   Gateway   │  │  Storage    │      │
│  │             │  │  3rd-Party  │  │   Queue     │  │     +       │  │     +       │  │             │      │
│  │  Recurrence │  │    APIs     │  │             │  │ Integration │  │    ERP      │  │             │      │
│  │  5 minutes  │  │             │  │             │  │  Account    │  │             │  │             │      │
│  │             │  │             │  │             │  │             │  │             │  │             │      │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘      │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

DETAILED STEPS:

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 1: POLL TRIGGER                                                                                        │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • Logic App "Inbound-{Type}-Poller" triggers every 5 minutes                                               │
│ • Types: Appointment, CostEstimate, DeliveryStatus, Invoice                                                │
│ • Retrieves LastPollTimestamp from Blob Storage                                                            │
│ • Generates unique CorrelationId for this poll cycle                                                       │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 2: QUERY 3RD-PARTY                                                                                     │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • Call 3rd-party REST API via APIM                                                                         │
│ • APIM injects API Key from Key Vault                                                                      │
│ • GET updates since LastPollTimestamp                                                                      │
│ • Receive response with list of updates (XML format)                                                       │
│ • Check each record against idempotency store (using 3rd-party unique ID)                                  │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 3: QUEUE MESSAGE                                                                                       │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • For each update record:                                                                                  │
│   - Assign unique MessageId                                                                                │
│   - Set SessionId = 3rd-party ShipmentId (for ordered processing)                                          │
│   - Add CorrelationId to message properties                                                                │
│   - Send to appropriate inbound queue                                                                      │
│ • Log message details to audit store                                                                       │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 4: TRANSFORM PAYLOAD                                                                                   │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • Logic App "Inbound-Message-Processor" receives message from queue                                        │
│ • Validate XML against 3rd-party schema (Integration Account)                                              │
│ • Call Azure Function for transformation                                                                   │
│ • Apply XSLT map: 3rd-Party XML → ERP XML                                                                  │
│ • Validate output against ERP schema                                                                       │
│ • Log before/after payloads to audit store                                                                 │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 5: SEND TO ERP                                                                                         │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • Connect to ERP via On-Premise Data Gateway                                                               │
│ • Option A: Execute stored procedure with XML payload                                                      │
│ • Option B: Call ERP REST API (via APIM with Basic Auth)                                                   │
│ • Receive acknowledgment from ERP                                                                          │
│ • On failure: Retry with exponential backoff (max 5 attempts)                                              │
│ • On persistent failure: Move to dead-letter queue                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 6: UPDATE STATE                                                                                        │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ • On success:                                                                                              │
│   - Update LastPollTimestamp in Blob Storage                                                               │
│   - Store 3rd-party unique ID in idempotency store                                                         │
│   - Log success status to audit                                                                            │
│   - Complete message (remove from queue)                                                                   │
│ • On failure:                                                                                              │
│   - Log error details                                                                                      │
│   - Trigger Event Grid alert                                                                               │
│   - Store failed message in failed-messages container                                                      │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## **4. Error Handling Flow**

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                       ERROR HANDLING ARCHITECTURE                                           │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

                                    ┌─────────────────────────┐
                                    │    MESSAGE PROCESSING   │
                                    │      (Logic App)        │
                                    └────────────┬────────────┘
                                                 │
                                                 ▼
                                    ┌─────────────────────────┐
                                    │    PROCESSING ERROR?    │
                                    └────────────┬────────────┘
                                                 │
                          ┌──────────────────────┼──────────────────────┐
                          │ NO                   │                      │ YES
                          ▼                      │                      ▼
             ┌─────────────────────────┐         │         ┌─────────────────────────┐
             │   COMPLETE MESSAGE      │         │         │   RETRY ATTEMPT < 5?    │
             │   Update State          │         │         └────────────┬────────────┘
             │   Log Success           │         │                      │
             └─────────────────────────┘         │       ┌──────────────┼──────────────┐
                                                 │       │ YES          │              │ NO
                                                 │       ▼              │              ▼
                                    ┌─────────────────────────┐    ┌─────────────────────────┐
                                    │   EXPONENTIAL BACKOFF   │    │   MOVE TO DEAD-LETTER   │
                                    │   Wait & Retry          │    │   QUEUE                 │
                                    └─────────────────────────┘    └────────────┬────────────┘
                                                                                │
                                                                                ▼
                                                                   ┌─────────────────────────┐
                                                                   │   EVENT GRID TRIGGER    │
                                                                   │   Dead-Letter Event     │
                                                                   └────────────┬────────────┘
                                                                                │
                                         ┌──────────────────────────────────────┼──────────────────────────────────────┐
                                         │                                      │                                      │
                                         ▼                                      ▼                                      ▼
                            ┌─────────────────────────┐            ┌─────────────────────────┐            ┌─────────────────────────┐
                            │   STORE FAILED MESSAGE  │            │   SEND ALERT            │            │   LOG TO APPLICATION    │
                            │   Blob Storage          │            │   Email / Teams         │            │   INSIGHTS              │
                            │   failed-messages/      │            │                         │            │                         │
                            └─────────────────────────┘            └─────────────────────────┘            └─────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                         RETRY POLICY CONFIGURATION                                          │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│   Retry Attempt    │    Delay           │    Total Wait                                                    │
│   ─────────────────┼────────────────────┼─────────────────                                                 │
│   1st              │    30 seconds      │    30 seconds                                                    │
│   2nd              │    1 minute        │    1.5 minutes                                                   │
│   3rd              │    2 minutes       │    3.5 minutes                                                   │
│   4th              │    4 minutes       │    7.5 minutes                                                   │
│   5th              │    8 minutes       │    15.5 minutes                                                  │
│   Dead-Letter      │    —               │    After 5 failures                                              │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                    DEAD-LETTER HANDLER WORKFLOW                                             │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│   1. Logic App "DeadLetter-Handler" monitors dead-letter queues                                            │
│   2. On new dead-letter message:                                                                           │
│      a. Extract original message and error details                                                         │
│      b. Store complete payload in Blob Storage (failed-messages container)                                 │
│      c. Log to Application Insights with full context                                                      │
│      d. Trigger Event Grid notification                                                                    │
│      e. Send alert (email/Teams) to operations team                                                        │
│   3. Manual intervention options:                                                                          │
│      a. Review and fix data issues                                                                         │
│      b. Resubmit message to original queue                                                                 │
│      c. Mark as permanently failed                                                                         │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## **5. Security Architecture**

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                        SECURITY ARCHITECTURE                                                │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                      IDENTITY & ACCESS MANAGEMENT                                           │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│   │                               MANAGED IDENTITIES (System-Assigned)                                  │   │
│   ├─────────────────────────────────────────────────────────────────────────────────────────────────────┤   │
│   │                                                                                                     │   │
│   │   Logic Apps ──────────► Key Vault (Get Secrets)                                                   │   │
│   │              ──────────► Blob Storage (Read/Write)                                                 │   │
│   │              ──────────► Service Bus (Send/Receive)                                                │   │
│   │              ──────────► Integration Account (Read Maps/Schemas)                                   │   │
│   │                                                                                                     │   │
│   │   Azure Functions ─────► Key Vault (Get Secrets)                                                   │   │
│   │                   ─────► Blob Storage (Read/Write)                                                 │   │
│   │                   ─────► Integration Account (Read Maps/Schemas)                                   │   │
│   │                                                                                                     │   │
│   │   API Management ──────► Key Vault (Get Secrets for policies)                                      │   │
│   │                                                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                         SECRETS MANAGEMENT                                                  │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│   ┌───────────────────────────────────────────────────────────────────────────────────────────┐             │
│   │                                    AZURE KEY VAULT                                        │             │
│   ├───────────────────────────────────────────────────────────────────────────────────────────┤             │
│   │                                                                                           │             │
│   │   SECRET                          │   USAGE                          │   ACCESSED BY      │             │
│   │   ────────────────────────────────┼──────────────────────────────────┼────────────────────│             │
│   │   ThirdParty-ApiKey               │   3rd-party API authentication   │   APIM Policy      │             │
│   │   ERP-BasicAuth-Username          │   ERP REST API authentication    │   APIM Policy      │             │
│   │   ERP-BasicAuth-Password          │   ERP REST API authentication    │   APIM Policy      │             │
│   │   ERP-DB-ConnectionString         │   ERP database connection        │   Logic Apps       │             │
│   │   ServiceBus-ConnectionString     │   Service Bus connection         │   Logic Apps       │             │
│   │   Storage-ConnectionString        │   Blob Storage connection        │   Logic Apps       │             │
│   │                                                                                           │             │
│   │   Access Policy: Managed Identity only (no human access in production)                   │             │
│   │   Secret Rotation: Enabled with 90-day rotation policy                                   │             │
│   │   Soft Delete: Enabled with 90-day retention                                             │             │
│   │                                                                                           │             │
│   └───────────────────────────────────────────────────────────────────────────────────────────┘             │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                        NETWORK SECURITY                                                     │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│   │                                                                                                     │   │
│   │   ON-PREMISE CONNECTIVITY                                                                          │   │
│   │   ───────────────────────                                                                          │   │
│   │   • On-Premise Data Gateway: Outbound HTTPS only (no inbound ports required)                       │   │
│   │   • Encrypted tunnel between Azure and on-premise                                                  │   │
│   │   • Gateway installed on dedicated Windows server in DMZ                                           │   │
│   │                                                                                                     │   │
│   │   3RD-PARTY CONNECTIVITY                                                                           │   │
│   │   ──────────────────────                                                                           │   │
│   │   • HTTPS/TLS 1.2+ enforced                                                                        │   │
│   │   • API Key transmitted via HTTP header (not URL)                                                  │   │
│   │   • IP whitelisting (if supported by 3rd-party)                                                    │   │
│   │                                                                                                     │   │
│   │   INTERNAL AZURE TRAFFIC                                                                           │   │
│   │   ──────────────────────                                                                           │   │
│   │   • Private endpoints for Storage, Service Bus, Key Vault (optional but recommended)               │   │
│   │   • VNET integration for Logic Apps Standard and Azure Functions Premium                           │   │
│   │                                                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                         DATA PROTECTION                                                     │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│   ENCRYPTION AT REST                                                                                       │
│   ──────────────────                                                                                       │
│   • Blob Storage: Microsoft-managed keys (or Customer-managed keys)                                       │
│   • Service Bus: Microsoft-managed keys                                                                    │
│   • Key Vault: HSM-backed keys                                                                             │
│                                                                                                             │
│   ENCRYPTION IN TRANSIT                                                                                    │
│   ─────────────────────                                                                                    │
│   • TLS 1.2+ enforced on all connections                                                                   │
│   • HTTPS only for all API endpoints                                                                       │
│                                                                                                             │
│   DATA MASKING (in logs)                                                                                   │
│   ──────────────────────                                                                                   │
│   • PII fields masked in Application Insights                                                              │
│   • Sensitive payload data excluded from diagnostic logs                                                   │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## **6. Monitoring & Observability Dashboard**

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                    OBSERVABILITY ARCHITECTURE                                               │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                                             │
│   ┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐          │
│   │  Logic Apps │     │   Azure     │     │    APIM     │     │  Service    │     │   On-Prem   │          │
│   │             │     │  Functions  │     │             │     │    Bus      │     │   Gateway   │          │
│   └──────┬──────┘     └──────┬──────┘     └──────┬──────┘     └──────┬──────┘     └──────┬──────┘          │
│          │                   │                   │                   │                   │                  │
│          └───────────────────┴───────────────────┴───────────────────┴───────────────────┘                  │
│                                                  │                                                          │
│                                                  ▼                                                          │
│                              ┌───────────────────────────────────────────┐                                  │
│                              │         APPLICATION INSIGHTS              │                                  │
│                              │         (Centralized Telemetry)           │                                  │
│                              └───────────────────────────────────────────┘                                  │
│                                                  │                                                          │
│                                                  ▼                                                          │
│                              ┌───────────────────────────────────────────┐                                  │
│                              │         LOG ANALYTICS WORKSPACE           │                                  │
│                              │         (Query & Analysis)                │                                  │
│                              └───────────────────────────────────────────┘                                  │
│                                                  │                                                          │
│                    ┌─────────────────────────────┼─────────────────────────────┐                            │
│                    │                             │                             │                            │
│                    ▼                             ▼                             ▼                            │
│   ┌─────────────────────────────┐ ┌─────────────────────────────┐ ┌─────────────────────────────┐           │
│   │     AZURE DASHBOARDS        │ │      ALERT RULES            │ │      WORKBOOKS              │           │
│   │     (Real-time Metrics)     │ │      (Proactive Alerts)     │ │      (Detailed Reports)     │           │
│   └─────────────────────────────┘ └─────────────────────────────┘ └─────────────────────────────┘           │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                       KEY METRICS TO TRACK                                                  │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│   OPERATIONAL METRICS                                                                                      │
│   ───────────────────                                                                                      │
│   • Messages processed per hour (by direction: inbound/outbound)                                           │
│   • Success rate (%) per workflow                                                                          │
│   • Failure rate (%) per workflow                                                                          │
│   • Average processing latency (end-to-end)                                                                │
│   • Queue depth (pending messages)                                                                         │
│   • Dead-letter queue count                                                                                │
│                                                                                                             │
│   PERFORMANCE METRICS                                                                                      │
│   ───────────────────                                                                                      │
│   • ERP query response time                                                                                │
│   • 3rd-party API response time                                                                            │
│   • Transformation duration                                                                                │
│   • Gateway throughput                                                                                     │
│                                                                                                             │
│   BUSINESS METRICS                                                                                         │
│   ────────────────                                                                                         │
│   • Shipments processed today                                                                              │
│   • Appointments received                                                                                  │
│   • Invoices synchronized                                                                                  │
│   • Cost estimates received                                                                                │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                          ALERT CONFIGURATION                                                │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│   ALERT NAME                      │   CONDITION                           │   SEVERITY   │   ACTION        │
│   ────────────────────────────────┼───────────────────────────────────────┼──────────────┼─────────────────│
│   High Failure Rate               │   Failure rate > 10% in 15 min        │   Critical   │   Email + Teams │
│   Dead-Letter Threshold           │   Dead-letter count > 10              │   High       │   Email + Teams │
│   Processing Latency              │   Avg latency > 5 minutes             │   Warning    │   Email         │
│   Queue Backlog                   │   Queue depth > 100 messages          │   Warning    │   Email         │
│   Gateway Connectivity            │   Gateway offline > 5 minutes         │   Critical   │   Email + Teams │
│   3rd-Party API Errors            │   HTTP 5xx > 5 in 10 min              │   High       │   Email + Teams │
│   ERP Connectivity Failure        │   ERP unreachable > 3 retries         │   Critical   │   Email + Teams │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                          AUDIT LOG STRUCTURE                                                │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│   {                                                                                                        │
│     "correlationId": "guid",                                                                               │
│     "timestamp": "2026-01-29T10:35:00Z",                                                                   │
│     "direction": "outbound",                                                                               │
│     "messageType": "shipment-submission",                                                                  │
│     "sourceSystem": "ERP",                                                                                 │
│     "targetSystem": "3rd-Party",                                                                           │
│     "erpShipmentId": "ERP-12345",                                                                          │
│     "thirdPartyShipmentId": "TP-67890",                                                                    │
│     "status": "success",                                                                                   │
│     "steps": [                                                                                             │
│       { "step": "poll", "timestamp": "...", "status": "success" },                                         │
│       { "step": "transform", "timestamp": "...", "status": "success", "durationMs": 150 },                 │
│       { "step": "send", "timestamp": "...", "status": "success", "httpStatus": 200 },                      │
│       { "step": "updateState", "timestamp": "...", "status": "success" }                                   │
│     ],                                                                                                     │
│     "payloadBefore": "blob-storage-ref",                                                                   │
│     "payloadAfter": "blob-storage-ref",                                                                    │
│     "errorDetails": null                                                                                   │
│   }                                                                                                        │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## **7. Complete Azure Services Summary**

| Azure Service | Role | Justification |
|---------------|------|---------------|
| **Logic Apps (Standard)** | Workflow orchestration, polling, message routing | Low-code, built-in connectors, stateful workflows |
| **API Management** | API gateway, security policies, rate limiting | Centralized API facade, credential injection |
| **Azure Functions** | Complex transformations, custom logic | Flexible code execution, XSLT processing |
| **Service Bus** | Message queuing, decoupling, dead-letter | Reliable messaging, ordered delivery, retry support |
| **Event Grid** | Event-driven alerts, notifications | Real-time failure notifications |
| **Data Factory** | *Reserved for future batch scenarios* | Bulk data movement if needed |
| **Blob Storage** | State persistence, audit logs, idempotency | Cost-effective, durable storage |
| **Key Vault** | Secrets management | Secure credential storage, rotation |
| **On-Premise Data Gateway** | Hybrid connectivity to ERP | Secure tunnel, no inbound firewall ports |
| **Integration Accounts** | XSLT maps, XML schemas | B2B artifact management |
| **Relays** | *Alternative hybrid option* | Available if Gateway insufficient |

---

## **8. Next Steps & Recommendations**

| Phase | Activities |
|-------|------------|
| **Phase 1: Foundation** | Provision Azure resources, configure Key Vault, set up On-Premise Gateway |
| **Phase 2: Development** | Build XSLT maps, create Logic App workflows, develop Azure Functions |
| **Phase 3: Integration** | Connect to ERP (stored procedures/APIs), connect to 3rd-party APIs |
| **Phase 4: Testing** | End-to-end testing, error handling validation, performance testing |
| **Phase 5: Observability** | Configure Application Insights, build dashboards, set up alerts |
| **Phase 6: Go-Live** | Production deployment, monitoring, documentation |

---

**Do you have any questions about this architecture, or would you like me to dive deeper into any specific component?**

## @Dong-Shen-Metrotown

Yes, confirm to proceed with the full architecture design. NOTE: want to generate a correlation ID for each shipment. All shipment messages by this correlation ID can be easily traced by timestamps. Let me know if the change affects the overall design...

*System: User retried message*

## Copilot

## Correlation ID Enhancement — Impact Analysis

Excellent addition! This is a **best practice** for enterprise integration traceability.

---

### **Correlation ID Design**

| Aspect | Detail |
|--------|--------|
| **Generation** | Integration layer generates a unique Correlation ID on **first encounter** of a shipment |
| **Format** | GUID or structured ID (e.g., `SHIP-{ERP_ID}-{Timestamp}`) |
| **Persistence** | Stored and reused across **all related messages** for that shipment |
| **Scope** | Links all inbound & outbound transactions for a single shipment |

---

### **How It Works**

```
┌─────────────────────────────────────────────────────────────────────┐
│                     CORRELATION ID LIFECYCLE                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  OUTBOUND (ERP → 3rd Party):                                        │
│  ┌─────────────┐    ┌──────────────────┐    ┌───────────────────┐   │
│  │ ERP Shipment│───▶│ Generate/Lookup  │───▶│ Attach CorrelID   │   │
│  │ Record      │    │ Correlation ID   │    │ to all messages   │   │
│  └─────────────┘    └──────────────────┘    └───────────────────┘   │
│                                                                     │
│  INBOUND (3rd Party → ERP):                                         │
│  ┌─────────────┐    ┌──────────────────┐    ┌───────────────────┐   │
│  │ 3rd Party   │───▶│ Lookup existing  │───▶│ Attach same       │   │
│  │ Update      │    │ Correlation ID   │    │ CorrelID to msg   │   │
│  └─────────────┘    └──────────────────┘    └───────────────────┘   │
│                                                                     │
│  TRACEABILITY:                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ Query by Correlation ID → Returns ALL messages (with timestamps)│ │
│  │ • Initial submission                                          │   │
│  │ • Weight updates                                              │   │
│  │ • Pick-up appointment                                         │   │
│  │ • Cost estimation                                             │   │
│  │ • Delivery status                                             │   │
│  │ • Invoice                                                     │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

### **Correlation ID Mapping Table**

A new tracking table is required:

| Column | Description |
|--------|-------------|
| **CorrelationID** | Unique ID generated by integration layer (Primary Key) |
| **ERPShipmentID** | Shipment ID from ERP system |
| **ThirdPartyShipmentID** | Unique ID from 3rd-party application |
| **CreatedTimestamp** | When correlation was first established |
| **LastUpdatedTimestamp** | Most recent activity |
| **Status** | Active / Completed / Error |

---

### **Impact on Overall Design**

| Component | Impact | Change Required |
|-----------|--------|-----------------|
| **Logic Apps** | Minor | Add Correlation ID lookup/generation step |
| **Service Bus** | Minor | Include Correlation ID in message properties |
| **Blob/Table Storage** | Minor | Add Correlation ID mapping table |
| **Integration Account** | None | No change |
| **Key Vault** | None | No change |
| **API Management** | Minor | Log Correlation ID in request/response |
| **Azure Functions** | Minor | Pass Correlation ID through transformations |
| **Application Insights** | Minor | Use Correlation ID as custom dimension |
| **Overall Architecture** | **No structural change** | Adds traceability layer |

---

### **Enhanced Audit Log Structure**

Each log entry now includes:

| Field | Example |
|-------|---------|
| **CorrelationID** | `SHIP-ERP12345-20260129T103000Z` |
| **MessageID** | `MSG-001-ABC123` |
| **Timestamp** | `2026-01-29T10:30:00.000Z` |
| **Direction** | Outbound / Inbound |
| **MessageType** | NewShipment / WeightUpdate / Invoice / etc. |
| **Source** | ERP / 3rdParty |
| **Destination** | 3rdParty / ERP |
| **Status** | Success / Failed / Retry |
| **PayloadHash** | For integrity verification |
| **ErrorDetails** | (if applicable) |

---

### **Traceability Query Example**

```
Query: Get all messages for CorrelationID = "SHIP-ERP12345-20260129T103000Z"

Result:
┌────────────────────────┬─────────────┬──────────────────────┬─────────┐
│ Timestamp              │ Direction   │ MessageType          │ Status  │
├────────────────────────┼─────────────┼──────────────────────┼─────────┤
│ 2026-01-29T10:30:00Z   │ Outbound    │ NewShipment          │ Success │
│ 2026-01-29T11:15:00Z   │ Outbound    │ WeightUpdate         │ Success │
│ 2026-01-29T12:00:00Z   │ Inbound     │ PickupAppointment    │ Success │
│ 2026-01-29T12:05:00Z   │ Inbound     │ CostEstimation       │ Success │
│ 2026-01-29T18:30:00Z   │ Inbound     │ DeliveryStatus       │ Success │
│ 2026-01-30T09:00:00Z   │ Inbound     │ Invoice              │ Success │
└────────────────────────┴─────────────┴──────────────────────┴─────────┘
```

---

### **Conclusion**

| Question | Answer |
|----------|--------|
| Does this affect overall architecture? | **No structural changes** |
| Does this add complexity? | **Minimal** — adds lookup/generation step |
| Does this add value? | **Significant** — full shipment lifecycle traceability |
| Storage impact? | **Minor** — one additional mapping table |

---

### **Updated Requirements Checklist**

| Requirement | Confirmed |
|-------------|-----------|
| All previous requirements | ✅ |
| **Correlation ID generation** | ✅ NEW |
| **Correlation ID mapping (ERP ↔ 3rdParty)** | ✅ NEW |
| **Full lifecycle traceability by Correlation ID** | ✅ NEW |

---

## Proceeding with Full Architecture Design

I will now generate the complete architecture including:

1. **High-Level Architecture Diagram**
2. **Detailed Component Design** (all 11 Azure services)
3. **Outbound Process Flow** (with Correlation ID)
4. **Inbound Process Flow** (with Correlation ID)
5. **Error Handling & Dead-Letter Flow**
6. **Security Architecture**
7. **Observability & Audit Architecture**

---

## 1. High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              AZURE INTEGRATION ARCHITECTURE                              │
│                         ERP ←→ 3rd Party Shipment Application                           │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────┐                                           ┌─────────────────────┐
│                     │                                           │                     │
│   ON-PREMISE ERP    │                                           │   3RD PARTY CLOUD   │
│   (Behind Firewall) │                                           │   SHIPMENT APP      │
│                     │                                           │                     │
│  ┌───────────────┐  │                                           │  ┌───────────────┐  │
│  │  Shipment DB  │  │                                           │  │   REST APIs   │  │
│  │  (SQL Server) │  │                                           │  │               │  │
│  └───────────────┘  │                                           │  │ • Submissions │  │
│  ┌───────────────┐  │                                           │  │ • Updates     │  │
│  │  REST APIs    │  │                                           │  │ • Appts       │  │
│  │  (Optional)   │  │                                           │  │ • Status      │  │
│  └───────────────┘  │                                           │  │ • Invoices    │  │
│                     │                                           │  └───────────────┘  │
└──────────┬──────────┘                                           └──────────┬──────────┘
           │                                                                  │
           │ (Secured Connection)                                             │ (HTTPS + API Key)
           │                                                                  │
           ▼                                                                  ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│                              AZURE CLOUD INTEGRATION LAYER                              │
│                                                                                         │
│  ┌───────────────────────────────────────────────────────────────────────────────────┐  │
│  │                           ON-PREMISE DATA GATEWAY                                 │  │
│  │                     (Secure tunnel to on-premise ERP)                             │  │
│  └───────────────────────────────────────────────────────────────────────────────────┘  │
│                                          │                                              │
│                                          ▼                                              │
│  ┌───────────────────────────────────────────────────────────────────────────────────┐  │
│  │                              API MANAGEMENT (APIM)                                │  │
│  │           (Unified API facade, throttling, security, logging)                     │  │
│  └───────────────────────────────────────────────────────────────────────────────────┘  │
│                                          │                                              │
│            ┌─────────────────────────────┴─────────────────────────────┐                │
│            ▼                                                           ▼                │
│  ┌──────────────────────┐                                 ┌──────────────────────┐      │
│  │     OUTBOUND FLOW    │                                 │     INBOUND FLOW     │      │
│  │    (ERP → 3rd Party) │                                 │   (3rd Party → ERP)  │      │
│  └──────────┬───────────┘                                 └───────────┬──────────┘      │
│             │                                                         │                 │
│             ▼                                                         ▼                 │
│  ┌──────────────────────┐                                 ┌──────────────────────┐      │
│  │      LOGIC APPS      │                                 │      LOGIC APPS      │      │
│  │   (Orchestration)    │                                 │   (Orchestration)    │      │
│  │                      │                                 │                      │      │
│  │ • Poll ERP (5 min)   │                                 │ • Poll 3rd Party     │      │
│  │ • Change detection   │                                 │ • Process updates    │      │
│  │ • Correlation ID     │                                 │ • Correlation ID     │      │
│  │ • Idempotency check  │                                 │ • Idempotency check  │      │
│  └──────────┬───────────┘                                 └───────────┬──────────┘      │
│             │                                                         │                 │
│             ▼                                                         ▼                 │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐   │
│  │                               SERVICE BUS                                        │   │
│  │                     (Message queuing & decoupling)                               │   │
│  │                                                                                  │   │
│  │   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐     │   │
│  │   │  Outbound   │  │  Inbound    │  │ Dead-Letter │  │ Correlation ID      │     │   │
│  │   │  Queue      │  │  Queue      │  │ Queue       │  │ in Message Props    │     │   │
│  │   └─────────────┘  └─────────────┘  └─────────────┘  └─────────────────────┘     │   │
│  │                                                                                  │   │
│  └──────────────────────────────────────────────────────────────────────────────────┘   │
│             │                                                         │                 │
│             ▼                                                         ▼                 │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐   │
│  │                           AZURE FUNCTIONS                                        │   │
│  │                    (Transformation & Business Logic)                             │   │
│  │                                                                                  │   │
│  │   ┌────────────────────┐              ┌────────────────────┐                     │   │
│  │   │  XML Transform     │              │  XML Transform     │                     │   │
│  │   │  ERP → 3rd Party   │              │  3rd Party → ERP   │                     │   │
│  │   └────────────────────┘              └────────────────────┘                     │   │
│  │                                                                                  │   │
│  └──────────────────────────────────────────────────────────────────────────────────┘   │
│             │                                                         │                 │
│             ▼                                                         ▼                 │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐   │
│  │                         INTEGRATION ACCOUNT                                      │   │
│  │                    (B2B artifacts, schemas, maps)                                │   │
│  │                                                                                  │   │
│  │   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │   │
│  │   │ ERP XML     │  │ 3rd Party   │  │ XSLT Maps   │  │ Validation  │             │   │
│  │   │ Schemas     │  │ XML Schemas │  │             │  │ Rules       │             │   │
│  │   └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘             │   │
│  │                                                                                  │   │
│  └──────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                         │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐   │
│  │                              STORAGE LAYER                                       │   │
│  │                                                                                  │   │
│  │   ┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐  │   │
│  │   │    BLOB STORAGE     │    │    TABLE STORAGE    │    │     KEY VAULT       │  │   │
│  │   │                     │    │                     │    │                     │  │   │
│  │   │ • Audit logs        │    │ • Correlation ID    │    │ • API Keys          │  │   │
│  │   │ • Message archive   │    │   mapping table     │    │ • ERP credentials   │  │   │
│  │   │ • Poll timestamps   │    │ • Idempotency       │    │ • Connection strings│  │   │
│  │   │                     │    │   tracking          │    │ • Certificates      │  │   │
│  │   └─────────────────────┘    └─────────────────────┘    └─────────────────────┘  │   │
│  │                                                                                  │   │
│  └──────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                         │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐   │
│  │                           OBSERVABILITY LAYER                                    │   │
│  │                                                                                  │   │
│  │   ┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐  │   │
│  │   │ APPLICATION INSIGHTS│    │    LOG ANALYTICS    │    │    EVENT GRID       │  │   │
│  │   │                     │    │                     │    │                     │  │   │
│  │   │ • Correlation ID    │    │ • Centralized logs  │    │ • Failure alerts    │  │   │
│  │   │   tracking          │    │ • Query & analysis  │    │ • Event routing     │  │   │
│  │   │ • Performance       │    │ • Dashboards        │    │ • Notifications     │  │   │
│  │   │ • Dependencies      │    │ • Retention         │    │                     │  │   │
│  │   └─────────────────────┘    └─────────────────────┘    └─────────────────────┘  │   │
│  │                                                                                  │   │
│  └──────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Detailed Component Design

### **Component 1: On-Premise Data Gateway**

| Aspect | Detail |
|--------|--------|
| **Purpose** | Secure bridge between Azure and on-premise ERP |
| **Connectivity** | Outbound HTTPS (no inbound firewall rules needed) |
| **Supported Actions** | SQL queries, stored procedures, REST API calls |
| **High Availability** | Deploy gateway cluster (2+ nodes) |
| **Authentication** | Windows Auth or SQL Auth to ERP database |

---

### **Component 2: API Management (APIM)**

| Aspect | Detail |
|--------|--------|
| **Purpose** | Unified API facade for all external communications |
| **Inbound Policies** | Rate limiting, IP filtering, request validation |
| **Outbound Policies** | Response transformation, header injection |
| **Security** | API Key validation, OAuth (if needed) |
| **Logging** | Request/response logging with Correlation ID |
| **Products** | Separate products for ERP APIs vs 3rd-party APIs |

---

### **Component 3: Logic Apps (Standard)**

| Aspect | Detail |
|--------|--------|
| **Purpose** | Workflow orchestration for both inbound/outbound flows |
| **Trigger** | Recurrence (every 5 minutes) |
| **Key Actions** | Poll → Check idempotency → Transform → Send → Log |
| **State Management** | Stateful workflows for complex orchestrations |
| **Correlation ID** | Generated/retrieved at workflow start |
| **Error Handling** | Try-catch with retry policies |

---

### **Component 4: Service Bus**

| Aspect | Detail |
|--------|--------|
| **Purpose** | Reliable message queuing and decoupling |
| **Queues** | Outbound queue, Inbound queue, Dead-letter queue |
| **Message Properties** | CorrelationID, MessageType, Timestamp, ERPShipmentID |
| **Sessions** | Enable for ordered processing per shipment |
| **TTL** | Configurable message expiration |
| **Dead-Letter** | Automatic after max retry attempts |

---

### **Component 5: Azure Functions**

| Aspect | Detail |
|--------|--------|
| **Purpose** | Complex XML transformations and business logic |
| **Runtime** | .NET 6+ or Node.js |
| **Triggers** | Service Bus trigger, HTTP trigger |
| **Functions** | TransformERPToThirdParty, TransformThirdPartyToERP |
| **Integration** | Calls Integration Account for XSLT maps |
| **Correlation** | Accepts and propagates Correlation ID |

---

### **Component 6: Integration Account**

| Aspect | Detail |
|--------|--------|
| **Purpose** | Store and manage B2B integration artifacts |
| **Schemas** | ERP XML schemas, 3rd-party XML schemas |
| **Maps** | XSLT transformations (ERP ↔ 3rd Party) |
| **Tier** | Standard (for production workloads) |
| **Versioning** | Maintain schema/map versions for rollback |

---

### **Component 7: Blob Storage**

| Aspect | Detail |
|--------|--------|
| **Purpose** | Audit logs, message archive, state management |
| **Containers** | `audit-logs`, `message-archive`, `poll-state` |
| **Structure** | Partitioned by date and Correlation ID |
| **Retention** | Configurable (e.g., 90 days for archive) |
| **Access Tier** | Hot for recent, Cool for archive |

---

### **Component 8: Table Storage**

| Aspect | Detail |
|--------|--------|
| **Purpose** | Fast lookup for Correlation ID and idempotency |
| **Tables** | `CorrelationMapping`, `ProcessedMessages` |
| **Partition Key** | ERPShipmentID or ThirdPartyShipmentID |
| **Row Key** | CorrelationID or MessageID |
| **Queries** | Point queries for idempotency checks |

---

### **Component 9: Key Vault**

| Aspect | Detail |
|--------|--------|
| **Purpose** | Centralized secrets management |
| **Secrets** | 3rd-party API Key, ERP Basic Auth credentials |
| **Connection Strings** | Service Bus, Storage, SQL |
| **Access Policies** | Logic Apps, Functions, APIM (Managed Identity) |
| **Rotation** | Enable secret rotation policies |

---

### **Component 10: Event Grid**

| Aspect | Detail |
|--------|--------|
| **Purpose** | Event-driven notifications and alerting |
| **Event Sources** | Service Bus dead-letter, Logic App failures |
| **Subscribers** | Email, Teams, Azure Functions (remediation) |
| **Topics** | Integration alerts, audit events |

---

### **Component 11: Application Insights + Log Analytics**

| Aspect | Detail |
|--------|--------|
| **Purpose** | End-to-end observability and traceability |
| **Custom Dimensions** | CorrelationID, ERPShipmentID, MessageType |
| **Queries** | KQL queries by Correlation ID |
| **Dashboards** | Success/failure rates, latency, volume |
| **Alerts** | Threshold-based alerts on failures |

---

## 3. Outbound Process Flow (ERP → 3rd Party)

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                        OUTBOUND FLOW: ERP → 3RD PARTY                                   │
│                           (New Shipments & Updates)                                     │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────┐     ┌─────────────────────────────────────────────────────────────────────────┐
│  START  │     │                         LOGIC APP: OUTBOUND ORCHESTRATOR                │
│(5 min)  │────▶│                                                                         │
└─────────┘     │  ┌─────────────────────────────────────────────────────────────────┐    │
                │  │ STEP 1: RETRIEVE POLL STATE                                     │    │
                │  │                                                                 │    │
                │  │  ┌──────────────┐      ┌─────────────────────────────────────┐  │    │
                │  │  │ Blob Storage │─────▶│ Get LastSuccessfulPollTimestamp     │  │    │
                │  │  └──────────────┘      │ (e.g., 2026-01-29T10:25:00Z)        │  │    │
                │  │                        └─────────────────────────────────────┘  │    │
                │  └─────────────────────────────────────────────────────────────────┘    │
                │                                    │                                    │
                │                                    ▼                                    │
                │  ┌─────────────────────────────────────────────────────────────────┐    │
                │  │ STEP 2: POLL ERP FOR CHANGES                                    │    │
                │  │                                                                 │    │
                │  │  ┌──────────────────┐      ┌─────────────────────────────────┐  │    │
                │  │  │ On-Prem Gateway  │─────▶│ Execute SP or REST API:         │  │    │
                │  │  └──────────────────┘      │ WHERE ModifiedTime > @LastPoll  │  │    │
                │  │                            └─────────────────────────────────┘  │    │
                │  │                                                                 │    │
                │  │  Returns: List of new/changed shipment records (XML)            │    │
                │  └─────────────────────────────────────────────────────────────────┘    │
                │                                    │                                    │
                │                                    ▼                                    │
                │  ┌─────────────────────────────────────────────────────────────────┐    │
                │  │ STEP 3: FOR EACH SHIPMENT RECORD                                │    │
                │  └─────────────────────────────────────────────────────────────────┘    │
                │                                    │                                    │
                │       ┌────────────────────────────┴────────────────────────────┐       │
                │       ▼                                                          ▼       │
                │  ┌─────────────────────────────────┐  ┌─────────────────────────────┐   │
                │  │ STEP 3a: CORRELATION ID         │  │ STEP 3b: IDEMPOTENCY CHECK  │   │
                │  │                                 │  │                             │   │
                │  │ ┌─────────────────────────────┐ │  │ ┌─────────────────────────┐ │   │
                │  │ │ Query Table Storage:        │ │  │ │ Query ProcessedMessages │ │   │
                │  │ │ Exists for ERPShipmentID?   │ │  │ │ Already processed?      │ │   │
                │  │ │                             │ │  │ │                         │ │   │
                │  │ │ YES → Use existing CorrelID │ │  │ │ YES → Skip (duplicate)  │ │   │
                │  │ │ NO  → Generate new CorrelID │ │  │ │ NO  → Continue          │ │   │
                │  │ └─────────────────────────────┘ │  │ └─────────────────────────┘ │   │
                │  └─────────────────────────────────┘  └─────────────────────────────┘   │
                │                                    │                                    │
                │                                    ▼                                    │
                │  ┌─────────────────────────────────────────────────────────────────┐    │
                │  │ STEP 4: ENQUEUE MESSAGE TO SERVICE BUS                          │    │
                │  │                                                                 │    │
                │  │  Message Properties:                                            │    │
                │  │  ┌─────────────────────────────────────────────────────────┐    │    │
                │  │  │ CorrelationID: SHIP-ERP12345-20260129T103000Z           │    │    │
                │  │  │ ERPShipmentID: ERP12345                                 │    │    │
                │  │  │ MessageType: NewShipment / ShipmentUpdate               │    │    │
                │  │  │ Timestamp: 2026-01-29T10:30:00Z                         │    │    │
                │  │  │ Body: <ERP XML Payload>                                 │    │    │
                │  │  └─────────────────────────────────────────────────────────┘    │    │
                │  └─────────────────────────────────────────────────────────────────┘    │
                │                                    │                                    │
                └────────────────────────────────────┼────────────────────────────────────┘
                                                     │
                                                     ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              SERVICE BUS: OUTBOUND QUEUE                                │
└─────────────────────────────────────────────────────────────────────────────────────────┘
                                                     │
                                                     ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                         AZURE FUNCTION: TRANSFORM & SEND                                │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 5: TRANSFORM XML                                                           │    │
│  │                                                                                 │    │
│  │  ┌───────────────┐      ┌─────────────────────┐      ┌───────────────────────┐  │    │
│  │  │ ERP XML       │─────▶│ Integration Account │─────▶│ 3rd Party XML         │  │    │
│  │  │ (Source)      │      │ (XSLT Map)          │      │ (Target)              │  │    │
│  │  └───────────────┘      └─────────────────────┘      └───────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 6: CALL 3RD PARTY API                                                      │    │
│  │                                                                                 │    │
│  │  ┌───────────────┐      ┌─────────────────────┐      ┌───────────────────────┐  │    │
│  │  │ Get API Key   │─────▶│ POST to 3rd Party   │─────▶│ Receive Response      │  │    │
│  │  │ (Key Vault)   │      │ REST API (via APIM) │      │ (incl. ThirdPartyID)  │  │    │
│  │  └───────────────┘      └─────────────────────┘      └───────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 7: UPDATE TRACKING                                                         │    │
│  │                                                                                 │    │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐    │    │
│  │  │ Table Storage: Update CorrelationMapping                                │    │    │
│  │  │ • Add ThirdPartyShipmentID                                              │    │    │
│  │  │ • Update LastUpdatedTimestamp                                           │    │    │
│  │  │                                                                         │    │    │
│  │  │ Table Storage: Insert into ProcessedMessages                            │    │    │
│  │  │ • MessageID, CorrelationID, Timestamp, Status                           │    │    │
│  │  └─────────────────────────────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 8: AUDIT LOG                                                               │    │
│  │                                                                                 │    │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐    │    │
│  │  │ Blob Storage: Write to audit-logs container                             │    │    │
│  │  │ • CorrelationID                                                         │    │    │
│  │  │ • Original payload (ERP XML)                                            │    │    │
│  │  │ • Transformed payload (3rd Party XML)                                   │    │    │
│  │  │ • Response from 3rd Party                                               │    │    │
│  │  │ • Timestamp, Status                                                     │    │    │
│  │  │                                                                         │    │    │
│  │  │ Application Insights: Log with CorrelationID                            │    │    │
│  │  └─────────────────────────────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
                                                     │
                                                     ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                      UPDATE POLL STATE (on success)                                     │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ Blob Storage: Update LastSuccessfulPollTimestamp                                │    │
│  │ New value: 2026-01-29T10:30:00Z                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
                                                     │
                                                     ▼
                                               ┌───────────┐
                                               │    END    │
                                               └───────────┘
```

---

## 4. Inbound Process Flow (3rd Party → ERP)

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                        INBOUND FLOW: 3RD PARTY → ERP                                    │
│                  (Appointments, Costs, Status, Invoices)                                │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────┐     ┌─────────────────────────────────────────────────────────────────────────┐
│  START  │     │                         LOGIC APP: INBOUND ORCHESTRATOR                 │
│(5 min)  │────▶│                                                                         │
└─────────┘     │  ┌─────────────────────────────────────────────────────────────────┐    │
                │  │ STEP 1: RETRIEVE POLL STATE (per message type)                  │    │
                │  │                                                                 │    │
                │  │  ┌──────────────┐      ┌─────────────────────────────────────┐  │    │
                │  │  │ Blob Storage │─────▶│ Get LastPollTimestamp for:          │  │    │
                │  │  └──────────────┘      │ • Appointments                      │  │    │
                │  │                        │ • CostEstimations                   │  │    │
                │  │                        │ • DeliveryStatus                    │  │    │
                │  │                        │ • Invoices                          │  │    │
                │  │                        └─────────────────────────────────────┘  │    │
                │  └─────────────────────────────────────────────────────────────────┘    │
                │                                    │                                    │
                │                                    ▼                                    │
                │  ┌─────────────────────────────────────────────────────────────────┐    │
                │  │ STEP 2: POLL 3RD PARTY APIs (via APIM)                          │    │
                │  │                                                                 │    │
                │  │  ┌──────────────────┐      ┌─────────────────────────────────┐  │    │
                │  │  │ API Management   │─────▶│ GET /appointments?since=...     │  │    │
                │  │  │ (+ API Key from  │      │ GET /cost-estimates?since=...   │  │    │
                │  │  │  Key Vault)      │      │ GET /delivery-status?since=...  │  │    │
                │  │  └──────────────────┘      │ GET /invoices?since=...         │  │    │
                │  │                            └─────────────────────────────────┘  │    │
                │  │                                                                 │    │
                │  │  Returns: List of updates from 3rd party (XML)                  │    │
                │  │  Each record includes: ThirdPartyShipmentID                     │    │
                │  └─────────────────────────────────────────────────────────────────┘    │
                │                                    │                                    │
                │                                    ▼                                    │
                │  ┌─────────────────────────────────────────────────────────────────┐    │
                │  │ STEP 3: FOR EACH UPDATE RECORD                                  │    │
                │  └─────────────────────────────────────────────────────────────────┘    │
                │                                    │                                    │
                │       ┌────────────────────────────┴────────────────────────────┐       │
                │       ▼                                                          ▼       │
                │  ┌─────────────────────────────────┐  ┌─────────────────────────────┐   │
                │  │ STEP 3a: CORRELATION ID LOOKUP  │  │ STEP 3b: IDEMPOTENCY CHECK  │   │
                │  │                                 │  │                             │   │
                │  │ ┌─────────────────────────────┐ │  │ ┌─────────────────────────┐ │   │
                │  │ │ Query Table Storage:        │ │  │ │ Query ProcessedMessages │ │   │
                │  │ │ Find by ThirdPartyShipmentID│ │  │ │ (by 3rdParty MessageID) │ │   │
                │  │ │                             │ │  │ │                         │ │   │
                │  │ │ Returns: Existing CorrelID  │ │  │ │ YES → Skip (duplicate)  │ │   │
                │  │ │ + ERPShipmentID             │ │  │ │ NO  → Continue          │ │   │
                │  │ └─────────────────────────────┘ │  │ └─────────────────────────┘ │   │
                │  └─────────────────────────────────┘  └─────────────────────────────┘   │
                │                                    │                                    │
                │                                    ▼                                    │
                │  ┌─────────────────────────────────────────────────────────────────┐    │
                │  │ STEP 4: ENQUEUE MESSAGE TO SERVICE BUS                          │    │
                │  │                                                                 │    │
                │  │  Message Properties:                                            │    │
                │  │  ┌─────────────────────────────────────────────────────────┐    │    │
                │  │  │ CorrelationID: SHIP-ERP12345-20260129T103000Z           │    │    │
                │  │  │ ERPShipmentID: ERP12345                                 │    │    │
                │  │  │ ThirdPartyShipmentID: 3P-98765                          │    │    │
                │  │  │ MessageType: Appointment / Cost / Status / Invoice      │    │    │
                │  │  │ Timestamp: 2026-01-29T12:00:00Z                         │    │    │
                │  │  │ Body: <3rd Party XML Payload>                           │    │    │
                │  │  └─────────────────────────────────────────────────────────┘    │    │
                │  └─────────────────────────────────────────────────────────────────┘    │
                │                                    │                                    │
                └────────────────────────────────────┼────────────────────────────────────┘
                                                     │
                                                     ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              SERVICE BUS: INBOUND QUEUE                                 │
└─────────────────────────────────────────────────────────────────────────────────────────┘
                                                     │
                                                     ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                         AZURE FUNCTION: TRANSFORM & PUSH TO ERP                         │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 5: TRANSFORM XML                                                           │    │
│  │                                                                                 │    │
│  │  ┌───────────────┐      ┌─────────────────────┐      ┌───────────────────────┐  │    │
│  │  │ 3rd Party XML │─────▶│ Integration Account │─────▶│ ERP XML               │  │    │
│  │  │ (Source)      │      │ (XSLT Map)          │      │ (Target)              │  │    │
│  │  └───────────────┘      └─────────────────────┘      └───────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 6: PUSH TO ERP (via On-Prem Gateway)                                       │    │
│  │                                                                                 │    │
│  │  Option A: Stored Procedure                                                     │    │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐    │    │
│  │  │ EXEC usp_UpdateShipment @ERPShipmentID, @MessageType, @XMLPayload       │    │    │
│  │  └─────────────────────────────────────────────────────────────────────────┘    │    │
│  │                                                                                 │    │
│  │  Option B: REST API                                                             │    │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐    │    │
│  │  │ PUT /api/shipments/{ERPShipmentID}                                      │    │    │
│  │  │ Authorization: Basic {credentials from Key Vault}                       │    │    │
│  │  │ Body: <ERP XML Payload>                                                 │    │    │
│  │  └─────────────────────────────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 7: UPDATE TRACKING                                                         │    │
│  │                                                                                 │    │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐    │    │
│  │  │ Table Storage: Update CorrelationMapping                                │    │    │
│  │  │ • Update LastUpdatedTimestamp                                           │    │    │
│  │  │                                                                         │    │    │
│  │  │ Table Storage: Insert into ProcessedMessages                            │    │    │
│  │  │ • 3rdPartyMessageID, CorrelationID, Timestamp, Status                   │    │    │
│  │  └─────────────────────────────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 8: AUDIT LOG                                                               │    │
│  │                                                                                 │    │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐    │    │
│  │  │ Blob Storage: Write to audit-logs container                             │    │    │
│  │  │ Application Insights: Log with CorrelationID                            │    │    │
│  │  └─────────────────────────────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
                                                     │
                                                     ▼
                                               ┌───────────┐
                                               │    END    │
                                               └───────────┘
```

---

## 5. Error Handling & Dead-Letter Flow

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                           ERROR HANDLING & RETRY FLOW                                   │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │                          RETRY POLICY CONFIGURATION                             │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │ Type: Exponential Backoff                                               │   │    │
│  │   │ Initial Interval: 30 seconds                                            │   │    │
│  │   │ Maximum Interval: 15 minutes                                            │   │    │
│  │   │ Maximum Retries: 5                                                      │   │    │
│  │   │ Retry On: 5xx errors, timeout, transient failures                       │   │    │
│  │   │ No Retry On: 4xx errors (except 429), validation failures               │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│                                                                                         │
│      ┌──────────────────┐                                                               │
│      │ Message          │                                                               │
│      │ Processing       │                                                               │
│      └────────┬─────────┘                                                               │
│               │                                                                         │
│               ▼                                                                         │
│      ┌──────────────────┐        ┌─────────────────────────────────────────────────┐    │
│      │   SUCCESS?       │───YES─▶│ Complete message, update tracking, log success │    │
│      └────────┬─────────┘        └─────────────────────────────────────────────────┘    │
│               │                                                                         │
│               NO                                                                        │
│               │                                                                         │
│               ▼                                                                         │
│      ┌──────────────────┐                                                               │
│      │ TRANSIENT ERROR? │                                                               │
│      │ (5xx, timeout)   │                                                               │
│      └────────┬─────────┘                                                               │
│               │                                                                         │
│        ┌──────┴──────┐                                                                  │
│        │             │                                                                  │
│       YES            NO                                                                 │
│        │             │                                                                  │
│        ▼             ▼                                                                  │
│  ┌───────────┐  ┌────────────────────────────────────────────────────────────────┐      │
│  │  RETRY    │  │ PERMANENT FAILURE                                              │      │
│  │  (with    │  │ (4xx, validation error, schema mismatch)                       │      │
│  │  backoff) │  │                                                                │      │
│  └─────┬─────┘  └────────────────────────────────────────────────────────────────┘      │
│        │                                            │                                   │
│        ▼                                            │                                   │
│  ┌───────────┐                                      │                                   │
│  │ MAX       │                                      │                                   │
│  │ RETRIES?  │                                      │                                   │
│  └─────┬─────┘                                      │                                   │
│        │                                            │                                   │
│     ┌──┴──┐                                         │                                   │
│     │     │                                         │                                   │
│    NO    YES                                        │                                   │
│     │     │                                         │                                   │
│     │     └─────────────────────────────────────────┼───────────────────────┐           │
│     │                                               │                       │           │
│     ▼                                               ▼                       ▼           │
│  ┌──────────┐                              ┌─────────────────────────────────────────┐  │
│  │ WAIT &   │                              │           DEAD-LETTER QUEUE             │  │
│  │ RETRY    │                              │                                         │  │
│  └──────────┘                              │  Message Properties:                    │  │
│                                            │  • Original CorrelationID               │  │
│                                            │  • ErrorCode                            │  │
│                                            │  • ErrorMessage                         │  │
│                                            │  • FailureTimestamp                     │  │
│                                            │  • RetryCount                           │  │
│                                            │  • Original Payload                     │  │
│                                            └─────────────────────────────────────────┘  │
│                                                         │                               │
│                                                         ▼                               │
│                                            ┌─────────────────────────────────────────┐  │
│                                            │           EVENT GRID                    │  │
│                                            │                                         │  │
│                                            │  Publish: DeadLetterEvent               │  │
│                                            │  • CorrelationID                        │  │
│                                            │  • ErrorDetails                         │  │
│                                            │  • Timestamp                            │  │
│                                            └─────────────────────────────────────────┘  │
│                                                         │                               │
│                              ┌───────────────────────────┼───────────────────────────┐  │
│                              │                           │                           │  │
│                              ▼                           ▼                           ▼  │
│                   ┌─────────────────┐         ┌─────────────────┐         ┌──────────┐  │
│                   │  EMAIL ALERT    │         │  TEAMS ALERT    │         │ AZURE    │  │
│                   │                 │         │                 │         │ FUNCTION │  │
│                   │  To: Ops Team   │         │  To: Channel    │         │ (Auto    │  │
│                   │                 │         │                 │         │ Remediate│  │
│                   └─────────────────┘         └─────────────────┘         └──────────┘  │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          DEAD-LETTER REPROCESSING                                       │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │  MANUAL INTERVENTION:                                                           │    │
│  │                                                                                 │    │
│  │  1. Ops team reviews dead-letter messages                                       │    │
│  │  2. Fix root cause (data issue, schema, connectivity)                           │    │
│  │  3. Move message back to main queue for reprocessing                            │    │
│  │     OR                                                                          │    │
│  │  4. Mark as permanently failed and archive                                      │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Security Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              SECURITY ARCHITECTURE                                      │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │                           IDENTITY & ACCESS                                     │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │                      MANAGED IDENTITIES                                 │   │    │
│  │   │                                                                         │   │    │
│  │   │   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │   │    │
│  │   │   │ Logic Apps  │  │  Functions  │  │    APIM     │  │ Data Factory│    │   │    │
│  │   │   │ (System MI) │  │ (System MI) │  │ (System MI) │  │ (System MI) │    │   │    │
│  │   │   └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘    │   │    │
│  │   │          │                │                │                │           │   │    │
│  │   │          └────────────────┴────────────────┴────────────────┘           │   │    │
│  │   │                                   │                                     │   │    │
│  │   │                                   ▼                                     │   │    │
│  │   │                        ┌─────────────────────┐                          │   │    │
│  │   │                        │     KEY VAULT       │                          │   │    │
│  │   │                        │   (Access Policies) │                          │   │    │
│  │   │                        └─────────────────────┘                          │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │                           SECRETS MANAGEMENT                                    │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │                         KEY VAULT SECRETS                               │   │    │
│  │   │                                                                         │   │    │
│  │   │   Secret Name                    │ Purpose                              │   │    │
│  │   │   ───────────────────────────────┼───────────────────────────────────   │   │    │
│  │   │   ThirdPartyApiKey               │ API Key for 3rd party REST APIs      │   │    │
│  │   │   ERPBasicAuthUsername           │ ERP REST API username                │   │    │
│  │   │   ERPBasicAuthPassword           │ ERP REST API password                │   │    │
│  │   │   ERPSqlConnectionString         │ SQL connection to ERP database       │   │    │
│  │   │   ServiceBusConnectionString     │ Service Bus access                   │   │    │
│  │   │   StorageAccountConnectionString │ Blob/Table storage access            │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │                           NETWORK SECURITY                                      │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │                                                                         │   │    │
│  │   │   ON-PREMISE ERP                    AZURE CLOUD                         │   │    │
│  │   │   ┌─────────────────┐               ┌─────────────────────────────────┐ │   │    │
│  │   │   │                 │               │                                 │ │   │    │
│  │   │   │  ┌───────────┐  │   OUTBOUND    │

## @Dong-Shen-Metrotown

continuing to finish the remaining content...

## Copilot

## 6. Security Architecture (Continued)

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                           NETWORK SECURITY (Continued)                                  │
│                                                                                         │
│   ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│   │                                                                                 │   │
│   │   ON-PREMISE ERP                         AZURE CLOUD                            │   │
│   │   ┌─────────────────────┐                ┌─────────────────────────────────┐    │   │
│   │   │                     │                │                                 │    │   │
│   │   │  ┌───────────────┐  │   OUTBOUND     │  ┌───────────────────────────┐  │    │   │
│   │   │  │   ERP Server  │  │   HTTPS ONLY   │  │   On-Premise Data Gateway │  │    │   │
│   │   │  │               │  │ ──────────────▶│  │                           │  │    │   │
│   │   │  │  • Database   │  │   (Port 443)   │  │   • Relay to Azure        │  │    │   │
│   │   │  │  • REST APIs  │  │                │  │   • No inbound ports      │  │    │   │
│   │   │  │               │  │   NO INBOUND   │  │   • Encrypted tunnel      │  │    │   │
│   │   │  └───────────────┘  │   REQUIRED     │  └───────────────────────────┘  │    │   │
│   │   │                     │                │                                 │    │   │
│   │   │  ┌───────────────┐  │                │                                 │    │   │
│   │   │  │   FIREWALL    │  │                │                                 │    │   │
│   │   │  │               │  │                │                                 │    │   │
│   │   │  │  Outbound:    │  │                │                                 │    │   │
│   │   │  │  *.azure.com  │  │                │                                 │    │   │
│   │   │  │  Port 443     │  │                │                                 │    │   │
│   │   │  └───────────────┘  │                │                                 │    │   │
│   │   │                     │                │                                 │    │   │
│   │   └─────────────────────┘                └─────────────────────────────────┘    │   │
│   │                                                                                 │   │
│   └─────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                         │
│   ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│   │                     3RD PARTY API SECURITY                                      │   │
│   │                                                                                 │   │
│   │   ┌───────────────────┐         ┌───────────────────┐         ┌─────────────┐   │   │
│   │   │   AZURE APIM      │         │   HTTPS (TLS 1.2+)│         │  3RD PARTY  │   │   │
│   │   │                   │────────▶│                   │────────▶│  REST APIs  │   │   │
│   │   │  • API Key in     │         │   • Certificate   │         │             │   │   │
│   │   │    header         │         │     validation    │         │  • API Key  │   │   │
│   │   │  • Rate limiting  │         │   • Encrypted     │         │    auth     │   │   │
│   │   │  • IP filtering   │         │     payload       │         │             │   │   │
│   │   └───────────────────┘         └───────────────────┘         └─────────────┘   │   │
│   │                                                                                 │   │
│   └─────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                         │
│   ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│   │                     DATA PROTECTION                                             │   │
│   │                                                                                 │   │
│   │   ┌─────────────────────────────────────────────────────────────────────────┐   │   │
│   │   │                                                                         │   │   │
│   │   │   AT REST:                                                              │   │   │
│   │   │   • Blob Storage: Azure Storage Service Encryption (SSE)                │   │   │
│   │   │   • Table Storage: Azure Storage Service Encryption (SSE)               │   │   │
│   │   │   • Service Bus: Encryption at rest                                     │   │   │
│   │   │   • Key Vault: HSM-backed encryption                                    │   │   │
│   │   │                                                                         │   │   │
│   │   │   IN TRANSIT:                                                           │   │   │
│   │   │   • All connections: TLS 1.2 minimum                                    │   │   │
│   │   │   • On-Prem Gateway: Encrypted tunnel                                   │   │   │
│   │   │   • APIM to 3rd Party: HTTPS enforced                                   │   │   │
│   │   │                                                                         │   │   │
│   │   │   SENSITIVE DATA:                                                       │   │   │
│   │   │   • PII fields masked in logs (if applicable)                           │   │   │
│   │   │   • Audit logs: Access controlled via RBAC                              │   │   │
│   │   │                                                                         │   │   │
│   │   └─────────────────────────────────────────────────────────────────────────┘   │   │
│   │                                                                                 │   │
│   └─────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Observability & Audit Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                         OBSERVABILITY & AUDIT ARCHITECTURE                              │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │                         LOGGING SOURCES                                         │    │
│  │                                                                                 │    │
│  │   ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │    │
│  │   │ Logic Apps  │ │  Functions  │ │    APIM     │ │ Service Bus │ │  Gateway  │ │    │
│  │   └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └─────┬─────┘ │    │
│  │          │               │               │               │              │       │    │
│  │          └───────────────┴───────────────┴───────────────┴──────────────┘       │    │
│  │                                          │                                      │    │
│  │                                          ▼                                      │    │
│  │                          ┌───────────────────────────────┐                      │    │
│  │                          │      DIAGNOSTIC SETTINGS      │                      │    │
│  │                          │   (All resources configured)  │                      │    │
│  │                          └───────────────────────────────┘                      │    │
│  │                                          │                                      │    │
│  │                    ┌─────────────────────┴─────────────────────┐                │    │
│  │                    ▼                                           ▼                │    │
│  │     ┌────────────────────────────┐            ┌────────────────────────────┐    │    │
│  │     │    APPLICATION INSIGHTS    │            │      LOG ANALYTICS         │    │    │
│  │     │                            │            │       WORKSPACE            │    │    │
│  │     │  • Distributed tracing     │            │                            │    │    │
│  │     │  • Correlation ID linking  │            │  • Centralized log store   │    │    │
│  │     │  • Performance metrics     │            │  • KQL query interface     │    │    │
│  │     │  • Dependency tracking     │            │  • Long-term retention     │    │    │
│  │     │  • Custom dimensions       │            │  • Cross-resource queries  │    │    │
│  │     │                            │            │                            │    │    │
│  │     └────────────────────────────┘            └────────────────────────────┘    │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │                         AUDIT LOG STRUCTURE                                     │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │                    BLOB STORAGE: audit-logs container                   │   │    │
│  │   │                                                                         │   │    │
│  │   │   Path Structure:                                                       │   │    │
│  │   │   /audit-logs/{year}/{month}/{day}/{correlationId}/{messageId}.json    │   │    │
│  │   │                                                                         │   │    │
│  │   │   Example:                                                              │   │    │
│  │   │   /audit-logs/2026/01/29/SHIP-ERP12345-20260129T103000Z/MSG-001.json   │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │                    AUDIT LOG JSON SCHEMA                                │   │    │
│  │   │                                                                         │   │    │
│  │   │   {                                                                     │   │    │
│  │   │     "correlationId": "SHIP-ERP12345-20260129T103000Z",                  │   │    │
│  │   │     "messageId": "MSG-001-ABC123",                                      │   │    │
│  │   │     "timestamp": "2026-01-29T10:30:00.000Z",                            │   │    │
│  │   │     "direction": "Outbound",                                            │   │    │
│  │   │     "messageType": "NewShipment",                                       │   │    │
│  │   │     "source": "ERP",                                                    │   │    │
│  │   │     "destination": "3rdParty",                                          │   │    │
│  │   │     "erpShipmentId": "ERP12345",                                        │   │    │
│  │   │     "thirdPartyShipmentId": "3P-98765",                                 │   │    │
│  │   │     "status": "Success",                                                │   │    │
│  │   │     "durationMs": 1250,                                                 │   │    │
│  │   │     "retryCount": 0,                                                    │   │    │
│  │   │     "sourcePayload": "<base64 encoded XML>",                            │   │    │
│  │   │     "transformedPayload": "<base64 encoded XML>",                       │   │    │
│  │   │     "responsePayload": "<base64 encoded response>",                     │   │    │
│  │   │     "errorDetails": null                                                │   │    │
│  │   │   }                                                                     │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │                     CORRELATION ID TRACEABILITY                                 │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │              KQL QUERY: Trace Shipment by Correlation ID                │   │    │
│  │   │                                                                         │   │    │
│  │   │   // Query all events for a specific shipment                          │   │    │
│  │   │   union traces, requests, dependencies, customEvents                   │   │    │
│  │   │   | where customDimensions.CorrelationId ==                            │   │    │
│  │   │           "SHIP-ERP12345-20260129T103000Z"                              │   │    │
│  │   │   | project timestamp, operation_Name, message,                        │   │    │
│  │   │            customDimensions.MessageType,                               │   │    │
│  │   │            customDimensions.Direction,                                 │   │    │
│  │   │            customDimensions.Status                                     │   │    │
│  │   │   | order by timestamp asc                                             │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │              SAMPLE QUERY OUTPUT                                        │   │    │
│  │   │                                                                         │   │    │
│  │   │   timestamp             │ operation      │ type        │ status        │   │    │
│  │   │   ──────────────────────┼────────────────┼─────────────┼─────────────  │   │    │
│  │   │   2026-01-29T10:30:00Z  │ PollERP        │ Outbound    │ Success       │   │    │
│  │   │   2026-01-29T10:30:01Z  │ Transform      │ Outbound    │ Success       │   │    │
│  │   │   2026-01-29T10:30:02Z  │ Send3rdParty   │ Outbound    │ Success       │   │    │
│  │   │   2026-01-29T12:00:00Z  │ Poll3rdParty   │ Inbound     │ Success       │   │    │
│  │   │   2026-01-29T12:00:01Z  │ Transform      │ Inbound     │ Success       │   │    │
│  │   │   2026-01-29T12:00:02Z  │ PushERP        │ Inbound     │ Success       │   │    │
│  │   │   2026-01-29T18:30:00Z  │ Poll3rdParty   │ Inbound     │ Success       │   │    │
│  │   │   2026-01-29T18:30:01Z  │ Transform      │ Inbound     │ Success       │   │    │
│  │   │   2026-01-29T18:30:02Z  │ PushERP        │ Inbound     │ Success       │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │                         DASHBOARDS & ALERTING                                   │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │                    AZURE DASHBOARD WIDGETS                              │   │    │
│  │   │                                                                         │   │    │
│  │   │   ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐         │   │    │
│  │   │   │ Messages/Hour   │  │ Success Rate    │  │ Avg Latency     │         │   │    │
│  │   │   │                 │  │                 │  │                 │         │   │    │
│  │   │   │     487         │  │     99.2%       │  │     1.3s        │         │   │    │
│  │   │   │   ▲ +12%        │  │   ▲ +0.5%       │  │   ▼ -0.2s       │         │   │    │
│  │   │   └─────────────────┘  └─────────────────┘  └─────────────────┘         │   │    │
│  │   │                                                                         │   │    │
│  │   │   ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐         │   │    │
│  │   │   │ Dead Letters    │  │ Active Shipments│  │ Errors (24h)    │         │   │    │
│  │   │   │                 │  │                 │  │                 │         │   │    │
│  │   │   │       3         │  │      142        │  │       7         │         │   │    │
│  │   │   │   ⚠ Needs Review│  │   In Progress   │  │   ▼ -3          │         │   │    │
│  │   │   └─────────────────┘  └─────────────────┘  └─────────────────┘         │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │                         ALERT RULES                                     │   │    │
│  │   │                                                                         │   │    │
│  │   │   Alert Name              │ Condition              │ Action             │   │    │
│  │   │   ────────────────────────┼────────────────────────┼─────────────────   │   │    │
│  │   │   High Failure Rate       │ Error rate > 5%        │ Email + Teams      │   │    │
│  │   │   Dead Letter Threshold   │ DLQ count > 10         │ Email + PagerDuty  │   │    │
│  │   │   Latency Spike           │ P95 latency > 30s      │ Teams notification │   │    │
│  │   │   Gateway Offline         │ No heartbeat 10 min    │ Email + SMS        │   │    │
│  │   │   3rd Party API Down      │ 5xx errors > 20/min    │ Email + Teams      │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Data Storage Schema

### **Table Storage: CorrelationMapping**

| Column | Type | Description |
|--------|------|-------------|
| **PartitionKey** | String | ERPShipmentID (for fast lookup) |
| **RowKey** | String | CorrelationID |
| **ERPShipmentID** | String | Shipment ID from ERP system |
| **ThirdPartyShipmentID** | String | Unique ID from 3rd-party (nullable initially) |
| **CorrelationID** | String | Generated unique identifier |
| **CreatedTimestamp** | DateTime | When correlation was first established |
| **LastUpdatedTimestamp** | DateTime | Most recent activity |
| **Status** | String | Active / Completed / Error |
| **LastMessageType** | String | Most recent message type processed |

### **Table Storage: ProcessedMessages**

| Column | Type | Description |
|--------|------|-------------|
| **PartitionKey** | String | CorrelationID (for grouping) |
| **RowKey** | String | MessageID (unique per message) |
| **CorrelationID** | String | Links to CorrelationMapping |
| **Direction** | String | Inbound / Outbound |
| **MessageType** | String | NewShipment, WeightUpdate, Invoice, etc. |
| **SourceMessageID** | String | Original ID from source system |
| **ProcessedTimestamp** | DateTime | When message was processed |
| **Status** | String | Success / Failed / Retrying |
| **RetryCount** | Int | Number of retry attempts |

### **Blob Storage: Poll State**

| File | Content |
|------|---------|
| `poll-state/outbound-erp.json` | `{ "lastPollTimestamp": "2026-01-29T10:30:00Z" }` |
| `poll-state/inbound-appointments.json` | `{ "lastPollTimestamp": "2026-01-29T12:00:00Z" }` |
| `poll-state/inbound-costs.json` | `{ "lastPollTimestamp": "2026-01-29T12:05:00Z" }` |
| `poll-state/inbound-status.json` | `{ "lastPollTimestamp": "2026-01-29T18:30:00Z" }` |
| `poll-state/inbound-invoices.json` | `{ "lastPollTimestamp": "2026-01-30T09:00:00Z" }` |

---

## 9. Azure Resource Summary

| Resource | SKU/Tier | Quantity | Purpose |
|----------|----------|----------|---------|
| **Logic Apps** | Standard | 2 | Outbound + Inbound orchestration |
| **Azure Functions** | Consumption/Premium | 1 App (multiple functions) | Transformation & business logic |
| **API Management** | Developer/Standard | 1 | API facade & security |
| **Service Bus** | Standard | 1 Namespace | Message queuing |
| **Integration Account** | Standard | 1 | Schemas & XSLT maps |
| **Storage Account** | Standard LRS | 1 | Blob + Table storage |
| **Key Vault** | Standard | 1 | Secrets management |
| **Application Insights** | - | 1 | Monitoring & tracing |
| **Log Analytics Workspace** | - | 1 | Centralized logging |
| **On-Premise Data Gateway** | - | 2 (HA cluster) | ERP connectivity |
| **Event Grid** | - | 1 Topic | Alerting & events |

---

## 10. Architecture Summary

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              ARCHITECTURE SUMMARY                                       │
└─────────────────────────────────────────────────────────────────────────────────────────┘

  ┌─────────────────┐                                           ┌─────────────────┐
  │   ON-PREMISE    │                                           │   3RD PARTY     │
  │   ERP SYSTEM    │                                           │   SHIPMENT APP  │
  └────────┬────────┘                                           └────────┬────────┘
           │                                                              │
           │ (On-Prem Gateway)                                            │ (HTTPS/API Key)
           │                                                              │
           ▼                                                              ▼
  ┌───────────────────────────────────────────────────────────────────────────────────┐
  │                                                                                   │
  │                          AZURE INTEGRATION LAYER                                  │
  │                                                                                   │
  │   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐        │
  │   │ Logic Apps  │───▶│ Service Bus │───▶│  Functions  │───▶│    APIM     │        │
  │   │(Orchestrate)│    │  (Queuing)  │    │(Transform)  │    │  (Facade)   │        │
  │   └─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘        │
  │          │                  │                  │                  │               │
  │          └──────────────────┴──────────────────┴──────────────────┘               │
  │                                      │                                            │
  │                    ┌─────────────────┼─────────────────┐                          │
  │                    ▼                 ▼                 ▼                          │
  │          ┌─────────────┐    ┌─────────────┐    ┌─────────────┐                    │
  │          │  Key Vault  │    │   Storage   │    │ Integration │                    │
  │          │  (Secrets)  │    │(State/Audit)│    │  Account    │                    │
  │          └─────────────┘    └─────────────┘    └─────────────┘                    │
  │                                      │                                            │
  │                                      ▼                                            │
  │                    ┌─────────────────────────────────────┐                        │
  │                    │   App Insights + Log Analytics      │                        │
  │                    │   (Observability + Correlation ID)  │                        │
  │                    └─────────────────────────────────────┘                        │
  │                                                                                   │
  └───────────────────────────────────────────────────────────────────────────────────┘

  KEY FEATURES:
  ✅ Bidirectional integration (Outbound + Inbound)
  ✅ Polling-based (5-minute intervals)
  ✅ Timestamp-based change tracking
  ✅ Correlation ID for full shipment lifecycle tracing
  ✅ XML schema transformation (XSLT)
  ✅ Idempotency & duplicate prevention
  ✅ Robust retry with dead-letter handling
  ✅ Secure secrets management (Key Vault)
  ✅ Full audit logging & message tracking
  ✅ On-premise connectivity (no inbound firewall rules)
```

---

## Next Steps

The architecture design is now complete. Would you like me to:

1. **Deep-dive** into any specific component (e.g., Logic App workflow details, Function code patterns)?
2. **Provide ARM/Bicep templates** for infrastructure provisioning?
3. **Detail the XSLT transformation approach** for XML schema mapping?
4. **Outline a phased implementation plan**?
5. **Define DevOps CI/CD pipeline** for deployment?

Let me know how you'd like to proceed!

## @Dong-Shen-Metrotown

elaborate further on how Correlation ID  affects the overall component design. For example: how this Correlattion ID was tracked and applied in Logic Apps? What is the table structure in the "Blob/Table Storage" component to store this Correlation ID? Does it must include shipment, its created date time along with this Correlation ID? Reply with questions if you have any. 

## Copilot

## Correlation ID Deep Dive — Design Impact Analysis

Great question! Let me elaborate on how the Correlation ID flows through and affects each component in the architecture.

---

## 1. Correlation ID Design Principles

### **Core Concept**

| Principle | Description |
|-----------|-------------|
| **Uniqueness** | One Correlation ID per shipment lifecycle |
| **Persistence** | Generated once, reused across all related transactions |
| **Traceability** | Links all messages (outbound & inbound) for a single shipment |
| **Immutability** | Once created, never changes for that shipment |

### **Correlation ID Format Options**

| Format | Example | Pros | Cons |
|--------|---------|------|------|
| **GUID** | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` | Universally unique, no collisions | Not human-readable |
| **Structured** | `SHIP-ERP12345-20260129T103000Z` | Human-readable, includes context | Slightly longer |
| **Hybrid** | `SHIP-{ERPShipmentID}-{GUID}` | Balance of readability & uniqueness | Moderate length |

**Recommendation:** Use **Structured Format** for easier troubleshooting:
```
SHIP-{ERPShipmentID}-{CreatedTimestamp}

Example: SHIP-ERP12345-20260129T103000Z
```

---

## 2. Correlation ID Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                        CORRELATION ID LIFECYCLE                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

  PHASE 1: GENERATION (First Outbound Message)
  ─────────────────────────────────────────────
  
  ┌─────────────────┐         ┌─────────────────────────────────────────────────────────┐
  │ ERP Shipment    │         │                                                         │
  │ Record Detected │────────▶│  Is this ERPShipmentID already in CorrelationMapping?   │
  │ (New/Updated)   │         │                                                         │
  └─────────────────┘         └─────────────────────────┬───────────────────────────────┘
                                                        │
                                      ┌─────────────────┴─────────────────┐
                                      │                                   │
                                     NO                                  YES
                                      │                                   │
                                      ▼                                   ▼
                        ┌─────────────────────────┐         ┌─────────────────────────┐
                        │ GENERATE NEW            │         │ RETRIEVE EXISTING       │
                        │ Correlation ID          │         │ Correlation ID          │
                        │                         │         │                         │
                        │ Format:                 │         │ Use stored CorrelID     │
                        │ SHIP-{ERPShipmentID}-   │         │ for this shipment       │
                        │ {CurrentTimestamp}      │         │                         │
                        │                         │         │                         │
                        │ Store in Table Storage  │         │                         │
                        └─────────────────────────┘         └─────────────────────────┘
                                      │                                   │
                                      └─────────────────┬─────────────────┘
                                                        │
                                                        ▼
                                      ┌─────────────────────────────────────┐
                                      │ ATTACH Correlation ID to message   │
                                      │ and all subsequent operations      │
                                      └─────────────────────────────────────┘


  PHASE 2: PROPAGATION (All Subsequent Messages)
  ──────────────────────────────────────────────
  
  ┌─────────────────┐         ┌─────────────────────────────────────────────────────────┐
  │ Any message for │         │                                                         │
  │ this shipment   │────────▶│  Lookup by ERPShipmentID OR ThirdPartyShipmentID        │
  │ (In or Out)     │         │                                                         │
  └─────────────────┘         └─────────────────────────┬───────────────────────────────┘
                                                        │
                                                        ▼
                                      ┌─────────────────────────────────────┐
                                      │ RETRIEVE existing Correlation ID   │
                                      │ from CorrelationMapping table      │
                                      └─────────────────────────────────────┘
                                                        │
                                                        ▼
                                      ┌─────────────────────────────────────┐
                                      │ ATTACH same Correlation ID to:     │
                                      │ • Service Bus message properties   │
                                      │ • Application Insights telemetry   │
                                      │ • Audit log entries                │
                                      │ • All downstream API calls         │
                                      └─────────────────────────────────────┘


  PHASE 3: COMPLETION (Shipment Lifecycle Ends)
  ─────────────────────────────────────────────
  
  ┌─────────────────┐         ┌─────────────────────────────────────────────────────────┐
  │ Final Invoice   │         │                                                         │
  │ Received        │────────▶│  Update CorrelationMapping status to "Completed"        │
  │                 │         │  Correlation ID remains for historical traceability     │
  └─────────────────┘         └─────────────────────────────────────────────────────────┘
```

---

## 3. Table Storage Schema Design

### **Table 1: CorrelationMapping**

This is the **master table** linking all identifiers for a shipment.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                         TABLE: CorrelationMapping                                       │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                         │
│  PURPOSE: Master lookup table to link ERPShipmentID ↔ CorrelationID ↔ ThirdPartyID     │
│                                                                                         │
│  PARTITION STRATEGY: Partition by ERPShipmentID for fast outbound lookups              │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌──────────────────┬──────────────────┬───────────┬─────────────────────────────────────────┐
│ Column Name      │ Data Type        │ Required  │ Description                             │
├──────────────────┼──────────────────┼───────────┼─────────────────────────────────────────┤
│ PartitionKey     │ String           │ Yes       │ ERPShipmentID (e.g., "ERP12345")        │
│ RowKey           │ String           │ Yes       │ CorrelationID (unique identifier)       │
│ CorrelationID    │ String           │ Yes       │ Same as RowKey (for query convenience)  │
│ ERPShipmentID    │ String           │ Yes       │ Shipment ID from ERP system             │
│ ThirdPartyShipmentID │ String       │ No*       │ Unique ID from 3rd party (set after     │
│                  │                  │           │ first successful outbound call)         │
│ CreatedDateTime  │ DateTime (ISO)   │ Yes       │ When Correlation ID was generated       │
│ CreatedBy        │ String           │ Yes       │ "OutboundOrchestrator" or process name  │
│ LastUpdatedDateTime │ DateTime (ISO)│ Yes       │ Most recent activity timestamp          │
│ LastMessageType  │ String           │ Yes       │ Last processed message type             │
│ LastMessageDirection │ String       │ Yes       │ "Inbound" or "Outbound"                 │
│ Status           │ String           │ Yes       │ "Active" / "Completed" / "Error"        │
│ TotalOutboundCount │ Int            │ Yes       │ Count of outbound messages              │
│ TotalInboundCount │ Int             │ Yes       │ Count of inbound messages               │
│ Metadata         │ String (JSON)    │ No        │ Additional context (optional)           │
└──────────────────┴──────────────────┴───────────┴─────────────────────────────────────────┘

* ThirdPartyShipmentID is NULL until first successful response from 3rd party API
```

**Sample Data:**

| PartitionKey | RowKey | ERPShipmentID | ThirdPartyShipmentID | CreatedDateTime | Status |
|--------------|--------|---------------|----------------------|-----------------|--------|
| ERP12345 | SHIP-ERP12345-20260129T103000Z | ERP12345 | 3P-98765 | 2026-01-29T10:30:00Z | Active |
| ERP12346 | SHIP-ERP12346-20260129T110000Z | ERP12346 | 3P-98766 | 2026-01-29T11:00:00Z | Completed |
| ERP12347 | SHIP-ERP12347-20260129T113000Z | ERP12347 | NULL | 2026-01-29T11:30:00Z | Active |

---

### **Table 2: ThirdPartyLookup (Secondary Index)**

This table enables **reverse lookup** when inbound messages arrive with only the ThirdPartyShipmentID.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                         TABLE: ThirdPartyLookup                                         │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                         │
│  PURPOSE: Reverse lookup - find CorrelationID from ThirdPartyShipmentID                │
│                                                                                         │
│  PARTITION STRATEGY: Partition by ThirdPartyShipmentID for fast inbound lookups        │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌──────────────────┬──────────────────┬───────────┬─────────────────────────────────────────┐
│ Column Name      │ Data Type        │ Required  │ Description                             │
├──────────────────┼──────────────────┼───────────┼─────────────────────────────────────────┤
│ PartitionKey     │ String           │ Yes       │ ThirdPartyShipmentID (e.g., "3P-98765") │
│ RowKey           │ String           │ Yes       │ CorrelationID                           │
��� CorrelationID    │ String           │ Yes       │ Links to CorrelationMapping             │
│ ERPShipmentID    │ String           │ Yes       │ For cross-reference                     │
│ CreatedDateTime  │ DateTime (ISO)   │ Yes       │ When mapping was established            │
└──────────────────┴──────────────────┴───────────┴─────────────────────────────────────────┘
```

**Sample Data:**

| PartitionKey | RowKey | CorrelationID | ERPShipmentID | CreatedDateTime |
|--------------|--------|---------------|---------------|-----------------|
| 3P-98765 | SHIP-ERP12345-20260129T103000Z | SHIP-ERP12345-20260129T103000Z | ERP12345 | 2026-01-29T10:30:05Z |
| 3P-98766 | SHIP-ERP12346-20260129T110000Z | SHIP-ERP12346-20260129T110000Z | ERP12346 | 2026-01-29T11:00:03Z |

---

### **Table 3: MessageAuditLog**

This table stores **every message** processed, linked by Correlation ID.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                         TABLE: MessageAuditLog                                          │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                         │
│  PURPOSE: Complete audit trail of all messages per shipment                            │
│                                                                                         │
│  PARTITION STRATEGY: Partition by CorrelationID for grouped retrieval                  │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────┬──────────────────┬───────────┬─────────────────────────────────────┐
│ Column Name          │ Data Type        │ Required  │ Description                         │
├──────────────────────┼──────────────────┼───────────┼─────────────────────────────────────┤
│ PartitionKey         │ String           │ Yes       │ CorrelationID                       │
│ RowKey               │ String           │ Yes       │ MessageID (unique per message)      │
│ CorrelationID        │ String           │ Yes       │ Links all messages for shipment     │
│ MessageID            │ String           │ Yes       │ Unique message identifier           │
│ ERPShipmentID        │ String           │ Yes       │ ERP reference                       │
│ ThirdPartyShipmentID │ String           │ No        │ 3rd party reference (if available)  │
│ Direction            │ String           │ Yes       │ "Inbound" / "Outbound"              │
│ MessageType          │ String           │ Yes       │ NewShipment, WeightUpdate, etc.     │
│ SourceSystem         │ String           │ Yes       │ "ERP" / "3rdParty"                  │
│ DestinationSystem    │ String           │ Yes       │ "ERP" / "3rdParty"                  │
│ ProcessedDateTime    │ DateTime (ISO)   │ Yes       │ When message was processed          │
│ Status               │ String           │ Yes       │ Success / Failed / Retrying         │
│ RetryCount           │ Int              │ Yes       │ Number of retry attempts            │
│ DurationMs           │ Int              │ Yes       │ Processing duration in milliseconds │
│ ErrorCode            │ String           │ No        │ Error code (if failed)              │
│ ErrorMessage         │ String           │ No        │ Error details (if failed)           │
│ PayloadBlobPath      │ String           │ No        │ Path to full payload in Blob Storage│
└──────────────────────┴──────────────────┴───────────┴─────────────────────────────────────┘
```

**Sample Data:**

| PartitionKey | RowKey | Direction | MessageType | ProcessedDateTime | Status |
|--------------|--------|-----------|-------------|-------------------|--------|
| SHIP-ERP12345-20260129T103000Z | MSG-001-A1B2 | Outbound | NewShipment | 2026-01-29T10:30:02Z | Success |
| SHIP-ERP12345-20260129T103000Z | MSG-002-C3D4 | Outbound | WeightUpdate | 2026-01-29T11:15:01Z | Success |
| SHIP-ERP12345-20260129T103000Z | MSG-003-E5F6 | Inbound | PickupAppointment | 2026-01-29T12:00:03Z | Success |
| SHIP-ERP12345-20260129T103000Z | MSG-004-G7H8 | Inbound | CostEstimation | 2026-01-29T12:05:02Z | Success |
| SHIP-ERP12345-20260129T103000Z | MSG-005-I9J0 | Inbound | DeliveryStatus | 2026-01-29T18:30:01Z | Success |
| SHIP-ERP12345-20260129T103000Z | MSG-006-K1L2 | Inbound | Invoice | 2026-01-30T09:00:02Z | Success |

---

### **Table 4: IdempotencyCheck**

This table prevents **duplicate processing** of the same message.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                         TABLE: IdempotencyCheck                                         │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                         │
│  PURPOSE: Track processed messages to prevent duplicates                               │
│                                                                                         │
│  PARTITION STRATEGY: Partition by Direction + Date for efficient cleanup               │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────┬──────────────────┬───────────┬─────────────────────────────────────┐
│ Column Name          │ Data Type        │ Required  │ Description                         │
├──────────────────────┼──────────────────┼───────────┼─────────────────────────────────────┤
│ PartitionKey         │ String           │ Yes       │ "{Direction}-{YYYYMMDD}"            │
│ RowKey               │ String           │ Yes       │ SourceMessageID (from source system)│
│ CorrelationID        │ String           │ Yes       │ Links to shipment                   │
│ SourceMessageID      │ String           │ Yes       │ Original ID from source             │
│ MessageType          │ String           │ Yes       │ Type of message                     │
│ ProcessedDateTime    │ DateTime (ISO)   │ Yes       │ When first processed                │
│ ExpiryDateTime       │ DateTime (ISO)   │ Yes       │ When record can be purged           │
└──────────────────────┴──────────────────┴───────────┴─────────────────────────────────────┘
```

**Sample Data:**

| PartitionKey | RowKey | CorrelationID | ProcessedDateTime | ExpiryDateTime |
|--------------|--------|---------------|-------------------|----------------|
| Outbound-20260129 | ERP12345-UPD-001 | SHIP-ERP12345-20260129T103000Z | 2026-01-29T11:15:01Z | 2026-02-28T11:15:01Z |
| Inbound-20260129 | 3P-INV-98765-001 | SHIP-ERP12345-20260129T103000Z | 2026-01-29T12:00:03Z | 2026-02-28T12:00:03Z |

---

## 4. Blob Storage Structure

### **Container: audit-payloads**

Stores full message payloads referenced by MessageAuditLog.

```
audit-payloads/
├── 2026/
│   ├── 01/
│   │   ├── 29/
│   │   │   ├── SHIP-ERP12345-20260129T103000Z/
│   │   │   │   ├── MSG-001-A1B2/
│   │   │   │   │   ├── source-payload.xml
│   │   │   │   │   ├── transformed-payload.xml
│   │   │   │   │   └── response.json
│   │   │   │   ├── MSG-002-C3D4/
│   │   │   │   │   ├── source-payload.xml
│   │   │   │   │   ├── transformed-payload.xml
│   │   │   │   │   └── response.json
│   │   │   │   └── MSG-003-E5F6/
│   │   │   │       ├── source-payload.xml
│   │   │   │       ├── transformed-payload.xml
│   │   │   │       └── response.json
│   │   │   └── SHIP-ERP12346-20260129T110000Z/
│   │   │       └── ...
│   │   └── 30/
│   │       └── ...
│   └── 02/
│       └── ...
└── ...
```

---

## 5. Logic Apps — Correlation ID Implementation

### **Outbound Orchestrator Logic App**

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                    LOGIC APP: Outbound Orchestrator                                     │
│                    (Correlation ID Generation & Tracking)                               │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│  TRIGGER: Recurrence (Every 5 minutes)                                                 │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 1: Initialize Variables                                                    │    │
│  │                                                                                 │    │
│  │   Variable: currentTimestamp = utcNow()                                         │    │
│  │   Variable: correlationId = null                                                │    │
│  │   Variable: messageId = null                                                    │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                          │                                              │
│                                          ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 2: Get Last Poll Timestamp                                                 │    │
│  │                                                                                 │    │
│  │   Action: Azure Blob Storage - Get blob content                                 │    │
│  │   Container: poll-state                                                         │    │
│  │   Blob: outbound-erp.json                                                       │    │
│  │                                                                                 │    │
│  │   Output: lastPollTimestamp                                                     │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                          │                                              │
│                                          ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 3: Poll ERP for Changed Shipments                                          │    │
│  │                                                                                 │    │
│  │   Action: SQL Server - Execute stored procedure (via On-Prem Gateway)           │    │
│  │   Procedure: usp_GetChangedShipments                                            │    │
│  │   Parameter: @LastModifiedSince = lastPollTimestamp                             │    │
│  │                                                                                 │    │
│  │   Output: Array of shipment records (XML format)                                │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                          │                                              │
│                                          ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 4: For Each Shipment Record                                                │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │ STEP 4a: Lookup Existing Correlation ID                                 │   │    │
│  │   │                                                                         │   │    │
│  │   │   Action: Azure Table Storage - Get entity                              │   │    │
│  │   │   Table: CorrelationMapping                                             │   │    │
│  │   │   PartitionKey: @{items('For_each')?['ERPShipmentID']}                  │   │    │
│  │   │                                                                         │   │    │
│  │   │   Condition: Entity exists?                                             │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                         │                                                       │    │
│  │           ┌─────────────┴─────────────┐                                         │    │
│  │           │                           │                                         │    │
│  │          YES                          NO                                        │    │
│  │           │                           │                                         │    │
│  │           ▼                           ▼                                         │    │
│  │   ┌───────────────────┐       ┌───────────────────────────────────────────┐     │    │
│  │   │ Use existing      │       │ STEP 4b: Generate New Correlation ID      │     │    │
│  │   │ Correlation ID    │       │                                           │     │    │
│  │   │                   │       │ Expression:                               │     │    │
│  │   │ correlationId =   │       │ concat('SHIP-',                           │     │    │
│  │   │ body('Get_entity')│       │   items('For_each')?['ERPShipmentID'],    │     │    │
│  │   │ ?['CorrelationID']│       │   '-',                                    │     │    │
│  │   │                   │       │   formatDateTime(utcNow(),                │     │    │
│  │   │                   │       │     'yyyyMMddTHHmmssZ'))                   │     │    │
│  │   │                   │       │                                           │     │    │
│  │   │                   │       │ Result: SHIP-ERP12345-20260129T103000Z    │     │    │
│  │   └───────────────────┘       └───────────────────────────────────────────┘     │    │
│  │           │                           │                                         │    │
│  │           │                           ▼                                         │    │
│  │           │                   ┌───────────────────────────────────────────┐     │    │
│  │           │                   │ STEP 4c: Insert New CorrelationMapping    │     │    │
│  │           │                   │                                           │     │    │
│  │           │                   │ Action: Azure Table Storage - Insert entity│    │    │
│  │           │                   │ Table: CorrelationMapping                 │     │    │
│  │           │                   │                                           │     │    │
│  │           │                   │ Entity:                                   │     │    │
│  │           │                   │ {                                         │     │    │
│  │           │                   │   "PartitionKey": "{ERPShipmentID}",      │     │    │
│  │           │                   │   "RowKey": "{correlationId}",            │     │    │
│  │           │                   │   "CorrelationID": "{correlationId}",     │     │    │
│  │           │                   │   "ERPShipmentID": "{ERPShipmentID}",     │     │    │
│  │           │                   │   "ThirdPartyShipmentID": null,           │     │    │
│  │           │                   │   "CreatedDateTime": "{utcNow()}",        │     │    │
│  │           │                   │   "CreatedBy": "OutboundOrchestrator",    │     │    │
│  │           │                   │   "LastUpdatedDateTime": "{utcNow()}",    │     │    │
│  │           │                   │   "LastMessageType": "{messageType}",     │     │    │
│  │           │                   │   "LastMessageDirection": "Outbound",     │     │    │
│  │           │                   │   "Status": "Active",                     │     │    │
│  │           │                   │   "TotalOutboundCount": 0,                │     │    │
│  │           │                   │   "TotalInboundCount": 0                  │     │    │
│  │           │                   │ }                                         │     │    │
│  │           │                   │                                           │     │    │
│  │           │                   └───────────────────────────────────────────┘     │    │
│  │           │                           │                                         │    │
│  │           └───────────────┬───────────┘                                         │    │
│  │                           │                                                     │    │
│  │                           ▼                                                     │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │ STEP 4d: Idempotency Check                                              │   │    │
│  │   │                                                                         │   │    │
│  │   │   Action: Azure Table Storage - Get entity                              │   │    │
│  │   │   Table: IdempotencyCheck                                               │   │    │
│  │   │   PartitionKey: concat('Outbound-', formatDateTime(utcNow(), 'yyyyMMdd'))│  │    │
│  │   │   RowKey: concat(ERPShipmentID, '-', ERPMessageID)                      │   │    │
│  │   │                                                                         │   │    │
│  │   │   Condition: Already processed?                                         │   │    │
│  │   │   YES → Skip this record                                                │   │    │
│  │   │   NO  → Continue processing                                             │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────��───────────────────────────────┘   │    │
│  │                           │                                                     │    │
│  │                           ▼                                                     │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │ STEP 4e: Generate Message ID                                            │   │    │
│  │   │                                                                         │   │    │
│  │   │   Expression: concat('MSG-', guid())                                    │   │    │
│  │   │   Result: MSG-a1b2c3d4-e5f6-7890-abcd-ef1234567890                      │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                           │                                                     │    │
│  │                           ▼                                                     │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │ STEP 4f: Send to Service Bus                                            │   │    │
│  │   │                                                                         │   │    │
│  │   │   Action: Service Bus - Send message                                    │   │    │
│  │   │   Queue: outbound-queue                                                 │   │    │
│  │   │                                                                         │   │    │
│  │   │   Message Properties (Custom Properties):                               │   │    │
│  │   │   ┌─────────────────────────────────────────────────────────────────┐   │   │    │
│  │   │   │ CorrelationId: {correlationId}                                  │   │   │    │
│  │   │   │ MessageId: {messageId}                                          │   │   │    │
│  │   │   │ ERPShipmentID: {erpShipmentId}                                  │   │   │    │
│  │   │   │ MessageType: {NewShipment | WeightUpdate | ...}                 │   │   │    │
│  │   │   │ Direction: Outbound                                             │   │   │    │
│  │   │   │ SourceSystem: ERP                                               │   │   │    │
│  │   │   │ Timestamp: {utcNow()}                                           │   │   │    │
│  │   │   └─────────────────────────────────────────────────────────────────┘   │   │    │
│  │   │                                                                         │   │    │
│  │   │   Message Body: {ERP XML Payload}                                       │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                           │                                                     │    │
│  │                           ▼                                                     │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │ STEP 4g: Record in IdempotencyCheck                                     │   │    │
│  │   │                                                                         │   │    │
│  │   │   Action: Azure Table Storage - Insert entity                           │   │    │
│  │   │   Table: IdempotencyCheck                                               │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                          │                                              │
│                                          ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ STEP 5: Update Poll State                                                       │    │
│  │                                                                                 │    │
│  │   Action: Azure Blob Storage - Create/Update blob                               │    │
│  │   Container: poll-state                                                         │    │
│  │   Blob: outbound-erp.json                                                       │    │
│  │   Content: { "lastPollTimestamp": "{currentTimestamp}" }                        │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

### **Inbound Orchestrator Logic App**

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                    LOGIC APP: Inbound Orchestrator                                      │
│                    (Correlation ID Lookup & Tracking)                                   │
└───��─────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│  TRIGGER: Recurrence (Every 5 minutes)                                                 │
│                                                                                         │
│  ... (Similar initialization and polling steps) ...                                    │
│                                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │ KEY DIFFERENCE: Lookup by ThirdPartyShipmentID                                  │    │
│  │                                                                                 │    │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐   │    │
│  │   │ STEP 4a: Lookup Correlation ID via ThirdPartyLookup                     │   │    │
│  │   │                                                                         │   │    │
│  │   │   Action: Azure Table Storage - Get entity                              │   │    │
│  │   │   Table: ThirdPartyLookup                                               │   │    │
│  │   │   PartitionKey: @{items('For_each')?['ThirdPartyShipmentID']}           │   │    │
│  │   │                                                                         │   │    │
│  │   │   Output: CorrelationID, ERPShipmentID                                  │   │    │
│  │   │                                                                         │   │    │
│  │   │   Condition: Entity exists?                                             │   │    │
│  │   │   YES → Use existing CorrelationID                                      │   │    │
│  │   │   NO  → Log warning (orphan inbound message)                            │   │    │
│  │   │                                                                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                                 │    │
│  │   ... (Similar Service Bus send with CorrelationID attached) ...               │    │
│  │                                                                                 │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Azure Function — Correlation ID Propagation

### **Function: ProcessOutboundMessage**

```csharp
[Function("ProcessOutboundMessage")]
public async Task Run(
    [ServiceBusTrigger("outbound-queue", Connection = "ServiceBusConnection")] 
    ServiceBusReceivedMessage message,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessOutboundMessage");
    
    // ─────────────────────────────────────────────────────────────────────
    // STEP 1: Extract Correlation ID from message properties
    // ─────────────────────────────────────────────────────────────────────
    var correlationId = message.ApplicationProperties["CorrelationId"]?.ToString();
    var messageId = message.ApplicationProperties["MessageId"]?.ToString();
    var erpShipmentId = message.ApplicationProperties["ERPShipmentID"]?.ToString();
    var messageType = message.ApplicationProperties["MessageType"]?.ToString();
    
    // ─────────────────────────────────────────────────────────────────────
    // STEP 2: Set Correlation ID in telemetry context
    // ──────────────────────────────────────��──────────────────────────────
    using var operation = _telemetryClient.StartOperation<RequestTelemetry>(
        $"Process-{messageType}");
    operation.Telemetry.Properties["CorrelationId"] = correlationId;
    operation.Telemetry.Properties["MessageId"] = messageId;
    operation.Telemetry.Properties["ERPShipmentID"] = erpShipmentId;
    operation.Telemetry.Properties["Direction"] = "Outbound";
    
    logger.LogInformation(
        "Processing outbound message. CorrelationId: {CorrelationId}, " +
        "MessageId: {MessageId}, Type: {MessageType}",
        correlationId, messageId, messageType);
    
    var startTime = DateTime.UtcNow;
    string status = "Success";
    string errorMessage = null;
    
    try
    {
        // ─────────────────────────────────────────────────────────────────
        // STEP 3: Transform XML (ERP → 3rd Party)
        // ─────────────────────────────────────────────────────────────────
        var erpXml = Encoding.UTF8.GetString(message.Body);
        var transformedXml = await _transformationService.TransformErpTo3rdParty(
            erpXml, 
            messageType);
        
        // ─────────────────────────────────────────────────────────────────
        // STEP 4: Call 3rd Party API (with Correlation ID in header)
        // ─────────────────────────────────────────────────────────────────
        var response = await _thirdPartyClient.SendShipmentAsync(
            transformedXml,
            correlationId,  // Pass Correlation ID for end-to-end tracing
            messageType);
        
        var thirdPartyShipmentId = response.ShipmentId;
        
        // ─────────────────────────────────────────────────────────────────
        // STEP 5: Update CorrelationMapping with ThirdPartyShipmentID
        // ─────────────────────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(thirdPartyShipmentId))
        {
            await _correlationService.UpdateThirdPartyIdAsync(
                correlationId,
                erpShipmentId,
                thirdPartyShipmentId);
            
            // Also insert into ThirdPartyLookup for reverse lookups
            await _correlationService.InsertThirdPartyLookupAsync(
                thirdPartyShipmentId,
                correlationId,
                erpShipmentId);
        }
        
        // ─────────────────────────────────────────────────────────────────
        // STEP 6: Update CorrelationMapping counters
        // ─────────────────────────────────────────────────────────────────
        await _correlationService.IncrementOutboundCountAsync(correlationId);
        
        // ─────────────────────────────────────────────────────────────────
        // STEP 7: Log to MessageAuditLog
        // ─────────────────────────────────────────────────────────────────
        await _auditService.LogMessageAsync(new MessageAuditEntry
        {
            CorrelationId = correlationId,
            MessageId = messageId,
            ERPShipmentID = erpShipmentId,
            ThirdPartyShipmentID = thirdPartyShipmentId,
            Direction = "Outbound",
            MessageType = messageType,
            SourceSystem = "ERP",
            DestinationSystem = "3rdParty",
            ProcessedDateTime = DateTime.UtcNow,
            Status = "Success",
            DurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
        });
        
        // ─────────────────────────────���───────────────────────────────────
        // STEP 8: Store payloads in Blob Storage
        // ─────────────────────────────────────────────────────────────────
        await _blobService.StorePayloadsAsync(
            correlationId,
            messageId,
            erpXml,           // source payload
            transformedXml,   // transformed payload
            response.RawResponse);  // response from 3rd party
    }
    catch (Exception ex)
    {
        status = "Failed";
        errorMessage = ex.Message;
        
        logger.LogError(ex, 
            "Failed to process message. CorrelationId: {CorrelationId}", 
            correlationId);
        
        // Log failure to audit
        await _auditService.LogMessageAsync(new MessageAuditEntry
        {
            CorrelationId = correlationId,
            MessageId = messageId,
            Status = "Failed",
            ErrorMessage = ex.Message,
            // ... other fields
        });
        
        throw; // Let Service Bus handle retry/dead-letter
    }
}
```

---

## 7. Complete Data Flow with Correlation ID

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                 COMPLETE DATA FLOW WITH CORRELATION ID                                  │
└─────────────────────────────────────────────────────────────────────────────────────────┘

SHIPMENT LIFECYCLE EXAMPLE: ERP12345

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│  TIME: 2026-01-29T10:30:00Z                                                            │
│  EVENT: New Shipment Created in ERP                                                    │
│  ─────────────────────────────────────────────────────────────────────────────────────  │
│                                                                                         │
│  1. Logic App polls ERP → finds new shipment ERP12345                                  │
│  2. Lookup CorrelationMapping → NOT FOUND                                              │
│  3. GENERATE: CorrelationID = "SHIP-ERP12345-20260129T103000Z"                         │
│  4. INSERT into CorrelationMapping:                                                    │
│     ┌────────────────────────────────────────────────────────────────────────────┐     │
│     │ PartitionKey: ERP12345                                                     │     │
│     │ RowKey: SHIP-ERP12345-20260129T103000Z                                     │     │
│     │ ThirdPartyShipmentID: NULL                                                 │     │
│     │ CreatedDateTime: 2026-01-29T10:30:00Z                                      │     │
│     │ Status: Active                                                             │     │
│     └────────────────────────────────────────────────────────────────────────────┘     │
│  5. Send to Service Bus with CorrelationID in properties                               │
│  6. Function transforms & sends to 3rd Party                                           │
│  7. 3rd Party returns: ThirdPartyShipmentID = "3P-98765"                               │
│  8. UPDATE CorrelationMapping: ThirdPartyShipmentID = "3P-98765"                       │
│  9. INSERT into ThirdPartyLookup:                                                      │
│     ┌────────────────────────────────────────────────────────────────────────────┐     │
│     │ PartitionKey: 3P-98765                                                     │     │
│     │ RowKey: SHIP-ERP12345-20260129T103000Z                                     │     │
│     └────────────────────────────────────────────────────────────────────────────┘     │
│  10. INSERT into MessageAuditLog:                                                      │
│     ┌────────────────────────────────────────────────────────────────────────────┐     │
│     │ PartitionKey: SHIP-ERP12345-20260129T103000Z                               │     │
│     │ RowKey: MSG-001-A1B2                                                       │     │
│     │ MessageType: NewShipment                                                   │     │
│     │ Direction: Outbound                                                        │     │
│     └────────────────────────────────────────────────────────────────────────────┘     │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│  TIME: 2026-01-29T11:15:00Z                                                            │
│  EVENT: Weight Updated in ERP                                                          │
│  ─────────────────────────────────────────────────────────────────────────────────────  │
│                                                                                         │
│  1. Logic App polls ERP → finds changed shipment ERP12345                              │
│  2. Lookup CorrelationMapping → FOUND: "SHIP-ERP12345-20260129T103000Z"                │
│  3. USE EXISTING CorrelationID (no new generation)                                     │
│  4. Send to Service Bus with SAME CorrelationID                                        │
│  5. INSERT into MessageAuditLog:                                                       │
│     ┌────────────────────────────────────────────────────────────────────────────┐     │
│     │ PartitionKey: SHIP-ERP12345-20260129T103000Z                               │     │
│     │ RowKey: MSG-002-C3D4                                                       │     │
│     │ MessageType: WeightUpdate                                                  │     │
│     │ Direction: Outbound                                                        │     │
│     └────────────────────────────────────────────────────────────────────────────┘     │
│                                                                                         │
└──────────────────────────���──────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│  TIME: 2026-01-29T12:00:00Z                                                            │
│  EVENT: Pickup Appointment from 3rd Party                                              │
│  ─────────────────────────────────────────────────────────────────────────────────────  │
│                                                                                         │
│  1. Logic App polls 3rd Party → finds appointment for "3P-98765"                       │
│  2. Lookup ThirdPartyLookup by "3P-98765"                                              │
│     → FOUND: CorrelationID = "SHIP-ERP12345-20260129T103000Z"                          │
│            ERPShipmentID = "ERP12345"                                                  │
│  3. USE EXISTING CorrelationID (maintains linkage)                                     │
│  4. Send to Service Bus with SAME CorrelationID                                        │
│  5. Function transforms & pushes to ERP                                                │
│  6. INSERT into MessageAuditLog:                                                       │
│     ┌────────────────────────────────────────────────────────────────────────────┐     │
│     │ PartitionKey: SHIP-ERP12345-20260129T103000Z                               │     │
│     │ RowKey: MSG-003-E5F6                                                       │     │
│     │ MessageType: PickupAppointment                                             │     │
│     │ Direction: Inbound                                                         │     │
│     └────────────────────────────────────────────────────────────────────────────┘     │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

... (Similar flow for Cost, Status, Invoice) ...

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│  FINAL STATE: Query by CorrelationID                                                   │
│  ─────────────────────────────────────────────────────────────────────────────────────  │
│                                                                                         │
│  Query: PartitionKey = "SHIP-ERP12345-20260129T103000Z"                                │
│                                                                                         │
│  Result (MessageAuditLog):                                                             │
│  ┌────────────────────────────────────────────────────────────────────────────────┐    │
│  │ RowKey      │ Direction │ MessageType      │ ProcessedDateTime      │ Status   │    │
│  ├─────────────┼───────────┼──────────────────┼────────────────────────┼──────────│    │
│  │ MSG-001-A1B2│ Outbound  │ NewShipment      │ 2026-01-29T10:30:02Z   │ Success  │    │
│  │ MSG-002-C3D4│ Outbound  │ WeightUpdate     │ 2026-01-29T11:15:01Z   │ Success  │    │
│  │ MSG-003-E5F6│ Inbound   │ PickupAppointment│ 2026-01-29T12:00:03Z   │ Success  │    │
│  │ MSG-004-G7H8│ Inbound   │ CostEstimation   │ 2026-01-29T12:05:02Z   │ Success  │    │
│  │ MSG-005-I9J0│ Inbound   │ DeliveryStatus   │ 2026-01-29T18:30:01Z   │ Success  │    │
│  │ MSG-006-K1L2│ Inbound   │ Invoice          │ 2026-01-30T09:00:02Z   │ Success  │    │
│  └────────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                         │
│  COMPLETE SHIPMENT LIFECYCLE TRACED BY SINGLE CORRELATION ID ✓                        │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Summary: Correlation ID Impact on Components

| Component | Correlation ID Impact | Implementation |
|-----------|----------------------|----------------|
| **Logic Apps** | Generate/Lookup before processing | Table Storage queries at start of each flow |
| **Service Bus** | Carried in message properties | `ApplicationProperties["CorrelationId"]` |
| **Azure Functions** | Extract, propagate, log | Read from message, set in telemetry context |
| **Table Storage** | Primary storage for mappings | 4 tables: CorrelationMapping, ThirdPartyLookup, MessageAuditLog, IdempotencyCheck |
| **Blob Storage** | Organize payloads by CorrelationID | Path includes CorrelationID for grouping |
| **Application Insights** | Custom dimension for tracing | `Properties["CorrelationId"]` on all telemetry |
| **APIM** | Log in request/response | Policy to extract and log CorrelationID header |

---

## Questions for You

1. **Retention Policy**: How long should CorrelationMapping records be retained after a shipment is completed? (e.g., 90 days, 1 year, indefinite)

2. **Orphan Handling**: If an inbound message arrives with a ThirdPartyShipmentID that has no CorrelationID mapping (orphan), should we:
   - Reject the message?
   - Create a new CorrelationID on-the-fly?
   - Queue for manual review?

3. **Correlation ID in 3rd Party Calls**: Should we pass the CorrelationID as an HTTP header to the 3rd party API (if they support it) for end-to-end tracing?

Let me know if you need further elaboration on any specific aspect!
