┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                              BLOB STORAGE STRUCTURE — AUDIT PAYLOADS                                         │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  CONTAINER: shipment-audit                                                                                  │
│  ════════════════════════                                                                                   │
│                                                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                                                     │   │
│  │  FOLDER HIERARCHY:                                                                                  │   │
│  │  ─────────────────                                                                                  │   │
│  │                                                                                                     │   │
│  │  /shipment-audit                                                                                    │   │
│  │  │                                                                                                  │   │
│  │  └── /{YYYY}                                          ← Year partition                              │   │
│  │      │                                                                                              │   │
│  │      └── /{MM}                                        ← Month partition                             │   │
│  │          │                                                                                          │   │
│  │          └── /{DD}                                    ← Day partition                               │   │
│  │              │                                                                                      │   │
│  │              └── /{CorrelationId}                     ← Shipment folder                             │   │
│  │                  │                                                                                  │   │
│  │                  ├── /outbound                        ← Outbound messages                           │   │
│  │                  │   ├── {timestamp}-source.xml       ← Original ERP payload                        │   │
│  │                  │   ├── {timestamp}-transformed.xml  ← Transformed payload                         │   │
│  │                  │   └── {timestamp}-response.xml     ← 3rd party response                          │   │
│  │                  │                                                                                  │   │
│  │                  └── /inbound                         ← Inbound messages                            │   │
│  │                      ├── {timestamp}-source.xml       ← Original 3rd party payload                  │   │
│  │                      ├── {timestamp}-transformed.xml  ← Transformed payload                         │   │
│  │                      └── {timestamp}-response.xml     ← ERP response                                │   │
│  │                                                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                             │
│  EXAMPLE FILE PATHS:                                                                                        │
│  ═══════════════════                                                                                        │
│                                                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                                                     │   │
│  │  Shipment: THM-ERP12345-TOR-YVR-20260129                                                            │   │
│  │  ─────────────────────────────────────────                                                          │   │
│  │                                                                                                     │   │
│  │  OUTBOUND (ERP → 3rd Party):                                                                        │   │
│  │                                                                                                     │   │
│  │  /shipment-audit/2026/01/29/THM-ERP12345-TOR-YVR-20260129/outbound/                                 │   │
│  │      20260129T100500Z-source.xml           ← ERP XML (original)                                     │   │
│  │      20260129T100500Z-transformed.xml      ← 3rd Party XML (after XSLT)                             │   │
│  │      20260129T100501Z-response.xml         ← 3rd Party API response                                 │   │
│  │                                                                                                     │   │
│  │  /shipment-audit/2026/01/29/THM-ERP12345-TOR-YVR-20260129/outbound/                                 │   │
│  │      20260129T143000Z-source.xml           ← Weight update (ERP XML)                                │   │
│  │      20260129T143000Z-transformed.xml      ← Weight update (3rd Party XML)                          │   │
│  │      20260129T143001Z-response.xml         ← 3rd Party API response                                 │   │
│  │                                                                                                     │   │
│  │  INBOUND (3rd Party → ERP):                                                                         │   │
│  │                                                                                                     │   │
│  │  /shipment-audit/2026/01/29/THM-ERP12345-TOR-YVR-20260129/inbound/                                  │   │
│  │      20260129T143500Z-source.xml           ← Tender assigned (3rd Party XML)                        │   │
│  │      20260129T143500Z-transformed.xml      ← Tender assigned (ERP XML)                              │   │
│  │      20260129T143502Z-response.xml         ← ERP REST API response                                  │   │
│  │                                                                                                     │   │
│  │  /shipment-audit/2026/01/30/THM-ERP12345-TOR-YVR-20260129/inbound/                                  │   │
│  │      20260130T094500Z-source.xml           ← Delivery status (3rd Party XML)                        │   │
│  │      20260130T094500Z-transformed.xml      ← Delivery status (ERP XML)                              │   │
│  │      20260130T094502Z-response.xml         ← ERP REST API response                                  │   │
│  │                                                                                                     │   │
│  │  /shipment-audit/2026/02/01/THM-ERP12345-TOR-YVR-20260129/inbound/                                  │   │
│  │      20260201T091500Z-source.xml           ← Invoice (3rd Party XML)                                │   │
│  │      20260201T091500Z-transformed.xml      ← Invoice (ERP XML)                                      │   │
│  │      20260201T091502Z-response.xml         ← ERP REST API response                                  │   │
│  │                                                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                             │
│  BLOB METADATA:                                                                                             │
│  ══════════════                                                                                             │
│                                                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Each blob includes metadata for quick filtering without downloading content:                       │   │
│  │                                                                                                     │   │
│  │  {                                                                                                  │   │
│  │    "correlationId": "THM-ERP12345-TOR-YVR-20260129",                                                │   │
│  │    "correlationGuid": "550e8400-e29b-41d4-a716-446655440000",                                       │   │
│  │    "direction": "Outbound",                                                                         │   │
│  │    "operationType": "NEW",                                                                          │   │
│  │    "payloadType": "source",                                                                         │   │
│  │    "entityId": "THM",                                                                               │   │
│  │    "shpNum": "ERP12345",                                                                            │   │
│  │    "tenderId": "TND-2026-00456",                                                                    │   │
│  │    "batchId": "BATCH-2026012910-001",                                                               │   │
│  │    "contentType": "application/xml",                                                                │   │
│  │    "processedTimestamp": "2026-01-29T10:05:00.000Z"                                                 │   │
│  │  }                                                                                                  │   │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                             │
│  LIFECYCLE MANAGEMENT:                                                                                      │
│  ═════════════════════                                                                                      │
│                                                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │  Rule: shipment-audit-retention                                                                     │   │
│  │                                                                                                     │   │
│  │  {                                                                                                  │   │
│  │    "rules": [                                                                                       │   │
│  │      {                                                                                              │   │
│  │        "name": "move-to-cool-after-30-days",                                                        │   │
│  │        "enabled": true,                                                                             │   │
│  │        "type": "Lifecycle",                                                                         │   │
│  │        "definition": {                                                                              │   │
│  │          "filters": {                                                                               │   │
│  │            "blobTypes": ["blockBlob"],                                                              │   │
│  │            "prefixMatch": ["shipment-audit/"]                                                       │   │
│  │          },                                                                                         │   │
│  │          "actions": {                                                                               │   │
│  │            "baseBlob": {                                                                            │   │
│  │              "tierToCool": { "daysAfterModificationGreaterThan": 30 },                              │   │
│  │              "tierToArchive": { "daysAfterModificationGreaterThan": 90 },                           │   │
│  │              "delete": { "daysAfterModificationGreaterThan": 365 }                                  │   │
│  │            }                                                                                        │   │
│  │          }                                                                                          │   │
│  │        }                                                                                            │   │
│  │      }                                                                                              │   │
│  │    ]                                                                                                │   │
│  │  }                                                                                                  │   │
│  │                                                                                                     │   │
│  │  Retention Policy:                                                                                  │   │
│  │  • 0-30 days:   Hot storage (frequent access for troubleshooting)                                   │   │
│  │  • 30-90 days:  Cool storage (occasional access)                                                    │   │
│  │  • 90-365 days: Archive storage (compliance/audit)                                                  │   │
│  │  • 365+ days:   Deleted (configurable)                                                              │   │
│  │                                                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘




┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                              COMPLETE SHIPMENT LIFECYCLE — END-TO-END EXAMPLE                                │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                             │
│  SHIPMENT: THM-ERP12345-TOR-YVR-20260129                                                                    │
│  GUID:     550e8400-e29b-41d4-a716-446655440000                                                             │
│  TENDER:   TND-2026-00456                                                                                   │
│                                                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                                                     │   │
│  │  ══════════════════════════════════════════════════════════════════════════════════════════════    │   │
│  │  TIMELINE                                                                                           │   │
│  │  ══════════════════════════════════════════════════════════════════════════════════════════════    │   │
│  │                                                                                                     │   │
│  │  ┌────────────────────────────────────────────────────────────────────────────────────────────┐    │   │
│  │  │  DAY 1: January 29, 2026                                                                   │    │   │
│  │  └────────────────────────────────────────────────────────────────────────────────────────────┘    │   │
│  │                                                                                                     │   │
│  │  10:00:00 ─── ERP creates new shipment record                                                       │   │
│  │      │        LastModifiedTimestamp = 2026-01-29T10:00:00Z                                          │   │
│  │      │                                                                                              │   │
│  │  10:05:00 ─── Outbound Poll Logic App triggers                                                      │   │
│  │      │        ├─ Stored Procedure returns shipment (lastPoll was 10:00)                             │   │
│  │      │        ├─ Correlation Manager: New shipment detected                                         │   │
│  │      │        │   ├─ Generated CorrelationId: THM-ERP12345-TOR-YVR-20260129                         │   │
│  │      │        │   ├─ Generated CorrelationGuid: 550e8400-e29b-41d4-a716-446655440000                │   │
│  │      │        │   └─ Stored in CorrelationMapping + GuidLookup tables                               │   │
│  │      │        ├─ Schema Validation: PASSED                                                          │   │
│  │      │        ├─ Business Validation: PASSED                                                        │   │
│  │      │        └─ Message sent to Service Bus (outbound-shipments topic)                             │   │
│  │      │                                                                                              │   │
│  │  10:05:01 ─── Batch Processing Logic App processes message                                          │   │
│  │      │        ├─ XSLT Transform: ERP XML → 3rd Party XML                                            │   │
│  │      │        ├─ Inject <correlationGuid>550e8400...</correlationGuid>                              │   │
│  │      │        ├─ API Management: POST to 3rd Party API                                              │   │
│  │      │        └─ Response: 202 Accepted (correlationGuid echoed back)                               │   │
│  │      │                                                                                              │   │
│  │  10:05:02 ─── Audit records created                                                                 │   │
│  │      │        ├─ Blob: /2026/01/29/THM-ERP12345.../outbound/100500Z-source.xml                      │   │
│  │      │        ├─ Blob: /2026/01/29/THM-ERP12345.../outbound/100500Z-transformed.xml                 │   │
│  │      │        ├─ Blob: /2026/01/29/THM-ERP12345.../outbound/100501Z-response.xml                    │   │
│  │      │        ├─ Table: MessageAuditLog entry (Status: Success)                                     │   │
│  │      │        └─ Table: IdempotencyCheck entry                                                      │   │
│  │      │                                                                                              │   │
│  │  14:30:00 ─── 3rd Party assigns tender                                                              │   │
│  │      │        EntryId: 98765, TenderId: TND-2026-00456                                              │   │
│  │      │                                                                                              │   │
│  │  14:35:00 ─── Inbound Poll Logic App triggers                                                       │   │
│  │      │        ├─ GET /tendering?entryId=98764 returns new entry (98765)                             │   │
│  │      │        ├─ Correlation Manager: Lookup by correlationGuid                                     │   │
│  │      │        │   ├─ Found: THM-ERP12345-TOR-YVR-20260129                                           │   │
│  │      │        │   └─ Link TenderId: TND-2026-00456                                                  │   │
│  │      │        ├─ Updated CorrelationMapping.TenderId                                                │   │
│  │      │        ├─ Created ThirdPartyLookup entry                                                     │   │
│  │      │        └─ Message sent to Service Bus (inbound-tendering topic)                              │   │
│  │      │                                                                                              │   │
│  │  14:35:02 ─── Inbound Processing                                                                    │   │
│  │      │        ├─ XSLT Transform: 3rd Party XML → ERP XML                                            │   │
│  │      │        ├─ Azure Relay: POST to ERP REST API                                                  │   │
│  │      │        ├─ Response: 200 OK                                                                   │   │
│  │      │        └─ Audit records created (source, transformed, response)                              │   │
│  │      │                                                                                              │   │
│  │  ┌────────────────────────────────────────────────────────────────────────────────────────────┐    │   │
│  │  │  DAY 2: January 30, 2026                                                                   │    │   │
│  │  └────────────────────────────────────────────────────────────────────────────────────────────┘    │   │
│  │                                                                                                     │   │
│  │  08:00:00 ─── 3rd Party schedules pickup appointment                                                │   │
│  │      │        EntryId: 98801, OperationType: APPOINTMENT_SCHEDULED                                  │   │
│  │      │                                                                                              │   │
│  │  08:05:00 ─── Inbound Poll processes appointment                                                    │   │
│  │      │        ├─ Correlation lookup via correlationGuid → Found                                     │   │
│  │      │        ├─ Same CorrelationId: THM-ERP12345-TOR-YVR-20260129                                  │   │
│  │      │        ├─ Transform & send to ERP                                                            │   │
│  │      │        └─ Audit: /2026/01/30/THM-ERP12345.../inbound/080500Z-*.xml                           │   │
│  │      │                                                                                              │   │
│  │  09:45:00 ─── Shipment delivered                                                                    │   │
│  │      │        EntryId: 99102, OperationType: DELIVERED                                              │   │
│  │      │                                                                                              │   │
│  │  09:50:00 ─── Inbound Poll processes delivery status                                                │   │
│  │      │        ├─ Correlation lookup via correlationGuid → Found                                     │   │
│  │      │        ├─ Same CorrelationId: THM-ERP12345-TOR-YVR-20260129                                  │   │
│  │      │        ├─ Transform & send to ERP                                                            │   │
│  │      │        └─ Audit: /2026/01/30/THM-ERP12345.../inbound/094500Z-*.xml                           │   │
│  │      │                                                                                              │   │
│  │  ┌────────────────────────────────────────────────────────────────────────────────────────────┐    │   │
│  │  │  DAY 3: February 1, 2026                                                                   │    │   │
│  │  └────────────────────────────────────────────────────────────────────────────────────────────┘    │   │
│  │                                                                                                     │   │
│  │  09:15:00 ─── 3rd Party sends invoice                                                               │   │
│  │      │        EntryId: 99501, OperationType: INVOICE                                                │   │
│  │      │                                                                                              │   │
│  │  09:20:00 ─── Inbound Poll processes invoice                                                        │   │
│  │      │        ├─ Correlation lookup via correlationGuid → Found                                     │   │
│  │      │        ├─ Same CorrelationId: THM-ERP12345-TOR-YVR-20260129                                  │   │
│  │      │        ├─ Transform & send to ERP                                                            │   │
│  │      │        ├─ Audit: /2026/02/01/THM-ERP12345.../inbound/091500Z-*.xml                           │   │
│  │      │        └─ Update CorrelationMapping.Status = "Completed"                                     │   │
│  │                                                                                                     │   │
│  │  ══════════════════════════════════════════════════════════════════════════════════════════════    │   │
│  │  END OF LIFECYCLE                                                                                   │   │
│  │  ══════════════════════════════════════════════════════════════════════════════════════════════    │   │
│  │                                                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                             │
│  FINAL STATE — TABLE STORAGE RECORDS:                                                                       │
│  ════════════════════════════════════                                                                       │
│                                                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                                                     │   │
│  │  CorrelationMapping:                                                                                │   │
│  │  {                                                                                                  │   │
│  │    "PartitionKey": "THM",                                                                           │   │
│  │    "RowKey": "ERP12345-TOR-YVR-20260129",                                                           │   │
│  │    "CorrelationId": "THM-ERP12345-TOR-YVR-20260129",                                                │   │
│  │    "CorrelationGuid": "550e8400-e29b-41d4-a716-446655440000",                                       │   │
│  │    "TenderId": "TND-2026-00456",                                                                    │   │
│  │    "Status": "Completed",                                                                           │   │
│  │    "CreatedTimestamp": "2026-01-29T10:05:00Z",                                                      │   │
│  │    "LastUpdatedTimestamp": "2026-02-01T09:20:00Z"                                                   │   │
│  │  }                                                                                                  │   │
│  │                                                                                                     │   │
│  │  MessageAuditLog (6 entries):                                                                       │   │
│  │  ├─ 20260129T100500-Outbound-NEW                                                                    │   │
│  │  ├─ 20260129T143500-Inbound-TENDER_ASSIGNED                                                         │   │
│  │  ├─ 20260130T080500-Inbound-APPOINTMENT_SCHEDULED                                                   │   │
│  │  ├─ 20260130T094500-Inbound-DELIVERED                                                               │   │
│  │  └─ 20260201T091500-Inbound-INVOICE                                                                 │   │
│  │                                                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                             │
│  TRACEABILITY QUERY (Application Insights):                                                                 │
│  ══════════════════════════════════════════                                                                 │
│                                                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                                                     │   │
│  │  // Query all events for this shipment                                                              │   │
│  │  traces                                                                                             │   │
│  │  | union requests                                                                                   │   │
│  │  | union dependencies                                                                               │   │
│  │  | where customDimensions.CorrelationId == "THM-ERP12345-TOR-YVR-20260129"                          │   │
│  │  | order by timestamp asc                                                                           │   │
│  │  | project timestamp, name, customDimensions.OperationType, customDimensions.Direction,             │   │
│  │            customDimensions.Status, duration                                                        │   │
│  │                                                                                                     │   │
│  │  // Or query by GUID                                                                                │   │
│  │  traces                                                                                             │   │
│  │  | where customDimensions.CorrelationGuid == "550e8400-e29b-41d4-a716-446655440000"                 │   │
│  │                                                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘


9. Summary — Correlation ID Technical Design
Aspect												Design Decision
Internal Identifier									Composite CorrelationId: {entityId}-{shpnum}-{loc}-{destination}-{duedate}
External Identifier									UUID CorrelationGuid: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Relationship										1:1 mapping, both generated on first encounter
Immutability										Both IDs are immutable; field changes create new shipment
XML Field											<correlationGuid> (existing field in 3rd party schema)
Storage												5 Table Storage tables (CorrelationMapping, GuidLookup, ThirdPartyLookup, MessageAuditLog, IdempotencyCheck)
Lookup Priority										1) CorrelationGuid → 2) TenderId → 3) ShpNumRef+EntityId
Audit Storage										Blob Storage organized by date and CorrelationId
Observability										Both IDs propagated to Application Insights customDimensions
