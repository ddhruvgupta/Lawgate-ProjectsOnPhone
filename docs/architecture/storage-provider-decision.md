# Storage Provider Decision: Azure Blob Storage vs AWS S3

**Status:** Open for decision  
**Date:** April 2026  
**Context:** Multi-tenant legal document storage for a SaaS platform (Lawgate) serving India-based law firms

---

## Problem Statement

Lawgate stores legal documents (contracts, case files, evidence) on behalf of multiple tenant companies. The storage layer must provide:

- Strong tenant isolation (one tenant must never access another's files)
- Secure, time-limited direct access URLs (so the API doesn't proxy large files)
- Scalable, durable object storage with low operational overhead
- India data residency
- Compliance-friendly immutability (documents must be tamper-proof once lodged)
- Efficient document processing integration (virus scanning, OCR, thumbnail generation)

The system currently uses **Azure Blob Storage** with a container-per-tenant pattern. This document evaluates whether to stay on Azure or migrate to **AWS S3**, specifically in light of **S3 Files** (launched April 7, 2026).

---

## Current Architecture

```
[Client] → [App Service / .NET API] → [Azure Blob Storage]
                    ↕
             [Azure PostgreSQL]
             [Azure Key Vault]
             [ACS Email Service]
```

**Tenant isolation model:** one container per company (`company-{id}-documents`).  
**Access pattern:** SAS tokens (time-limited signed URLs) returned from the API; client uploads/downloads directly to Blob Storage.  
**Interface:** `IBlobStorageService` — abstracted, so the underlying provider can be swapped.

---

## S3 Files — What It Actually Is

S3 Files (April 2026) makes any S3 general-purpose bucket accessible as a **POSIX-compliant NFS v4.1 file system** on AWS compute (EC2, ECS, EKS, Lambda). It uses Amazon EFS under the hood and delivers **~1ms latency** for active data with intelligent prefetching.

Key characteristics:
- Mount an S3 bucket as a file system: `sudo mount -t s3files fs-xxx:/ /mnt/docs`
- Files written to the mount are automatically exported back to S3 as objects
- Changes to S3 objects appear in the file system within seconds
- POSIX permissions via UID/GID stored as S3 object metadata
- TLS 1.3 in transit, SSE-S3 or KMS at rest
- Supports concurrent access from multiple compute resources (NFS close-to-open consistency)

**What S3 Files is NOT:**
- It is not a replacement for pre-signed URLs for user upload/download
- It is not an isolation model — tenants share the same bucket namespace
- It does not change the fundamental S3 security or pricing model

**Relevant use case for Lawgate:**  
S3 Files is compelling for **background document processing pipelines** — a Lambda or ECS task that needs to scan, OCR, or watermark documents can mount the bucket and use standard file operations rather than streaming S3 objects manually. This is an internal compute benefit, not a direct user-facing storage benefit.

---

## Multi-Tenancy Patterns Compared

### Pattern A: Container / Bucket per Tenant (current)

```
Azure: container = company-42-documents/
AWS:   bucket  = lawgate-company-42-documents
```

| Aspect | Detail |
|--------|--------|
| Isolation | Strongest — each tenant is a completely separate namespace |
| Management | More overhead as tenant count grows (AWS: 10k bucket limit per account) |
| Cost | Same per-GB pricing regardless of pattern |
| IAM/RBAC | Simple — grant access to the whole container/bucket |

### Pattern B: Prefix per Tenant (single container / bucket)

```
Azure: lawgate-documents/company-42/doc.pdf
AWS:   lawgate-documents/company-42/doc.pdf
```

| Aspect | Detail |
|--------|--------|
| Isolation | Enforced via IAM policy conditions on prefix — slightly weaker but industry-standard |
| Management | Simpler — one container to manage |
| IAM/RBAC | Requires careful IAM policies with prefix conditions |
| S3 Access Grants | AWS 2023 feature — grants tenant-specific access to prefixes without IAM complexity |

**Recommendation:** Keep container-per-tenant for now (simpler, stronger isolation). Revisit when tenants exceed 500 (AWS bucket limit concern).

---

## Azure Blob Storage — Analysis

### Strengths

| Factor | Detail |
|--------|--------|
| **Already deployed** | Integrated with App Service via Managed Identity — no credentials to rotate |
| **India regions** | Central India, South India, West India |
| **Ecosystem cohesion** | Azure PostgreSQL + Key Vault + ACS Email + App Service are all Azure-native — no cross-cloud network latency or egress costs |
| **Managed Identity** | API authenticates to Blob Storage with no secrets; automatic credential rotation |
| **Lifecycle policies** | Hot → Cool → Archive tiering by age — ideal for older legal documents |
| **Immutable Blob Storage** | WORM (Write Once Read Many) policies for legal compliance — time-based or legal hold |
| **SAS tokens** | Time-limited signed URLs for direct client upload/download (equivalent to S3 pre-signed URLs) |
| **Azure RBAC** | Fine-grained role assignments at container or blob scope |

### Weaknesses

| Factor | Detail |
|--------|--------|
| No file system interface | No equivalent to S3 Files yet — processing pipelines must stream objects via SDK |
| Vendor lock-in | ACS email, Key Vault, App Service, PostgreSQL, Blob Storage — deep Azure dependency |

---

## AWS S3 — Analysis

### Strengths

| Factor | Detail |
|--------|--------|
| **S3 Files** | Filesystem access (~1ms) for document processing pipelines — unique capability (April 2026) |
| **S3 Object Lock** | WORM compliance — stronger guarantees than Azure Immutable Blob in some scenarios |
| **S3 Access Grants** | Tenant-prefix access delegation without IAM complexity (launched 2023) |
| **S3 Intelligent-Tiering** | Automatic tiering with no retrieval fees — slightly simpler than Azure lifecycle rules |
| **Pre-signed URLs** | Direct equivalent to SAS tokens |
| **Mumbai region** | ap-south-1 — India data residency |
| **Ecosystem breadth** | Better ML/AI processing services (Rekognition, Textract for OCR) |

### Weaknesses

| Factor | Detail |
|--------|--------|
| **Multi-cloud overhead** | Would create Azure + AWS dependency — two sets of credentials, IAM, billing, and monitoring |
| **Egress costs** | Cross-cloud traffic (Azure API → AWS S3) incurs egress charges (~$0.08/GB) |
| **Migration effort** | `IBlobStorageService` must be re-implemented with AWSSDK.S3; SAS token logic differs slightly |
| **Credential management** | IAM role or access key needed — managed identity equivalent (IAM roles) requires EC2/ECS; not available for Azure App Service |
| **No current integration** | Azure PostgreSQL, Key Vault, ACS, App Service are all Azure — S3 would be isolated |

---

## S3 Files — Specific Relevance for Lawgate

S3 Files is primarily useful for these Lawgate workloads:

| Workload | S3 Files Benefit | Realistic? |
|----------|-----------------|------------|
| Virus scanning before document acceptance | Mount bucket, scan as regular file | Yes — if on AWS compute |
| OCR / text extraction for search indexing | Mount bucket, run Tesseract/pdftools on files | Yes — if on AWS compute |
| Document watermarking | Read/write files without streaming SDK | Yes — if on AWS compute |
| Client upload/download | N/A — still uses pre-signed URLs | Not applicable |
| Legal archiving | N/A — still stored as S3 objects | Not applicable |

**Bottom line:** S3 Files is compelling **only if your processing infrastructure is on AWS**. On Azure App Service + ECS, there is no way to mount an S3 Files filesystem — you would need to migrate compute to AWS too.

---

## Comparison Summary

| Dimension | Azure Blob Storage | AWS S3 |
|-----------|-------------------|--------|
| Current integration | ✅ Already deployed, no changes | ❌ Full migration required |
| India data residency | ✅ Central India region | ✅ Mumbai (ap-south-1) |
| Tenant isolation (container/bucket per tenant) | ✅ Supported | ✅ Supported |
| Filesystem access (S3 Files equivalent) | ❌ Not available | ✅ S3 Files (~1ms, NFS v4.1) |
| WORM / immutability for compliance | ✅ Immutable Blob Storage | ✅ S3 Object Lock |
| SAS / pre-signed URL access | ✅ SAS tokens | ✅ Pre-signed URLs |
| Managed identity (no credentials) | ✅ Azure Managed Identity | ⚠️ IAM Role (AWS compute only) |
| Ecosystem fit (rest of stack is Azure) | ✅ Native | ❌ Cross-cloud overhead |
| Cross-cloud egress cost | N/A | ❌ ~$0.08/GB outbound |
| Migration effort | None | IBlobStorageService reimplementation |
| Processing pipeline benefit | None | High (if compute moves to AWS) |

---

## Recommendation

**Stay on Azure Blob Storage.** Migrate to S3 only if the team decides to move all compute to AWS.

**Rationale:**

1. **The entire stack is Azure.** Azure PostgreSQL, Key Vault, ACS email, App Service, and Blob Storage are already deployed and integrated with Managed Identity. Introducing S3 would make Lawgate a multi-cloud system with cross-cloud egress fees and two separate IAM/credential systems.

2. **S3 Files doesn't apply here.** S3 Files provides filesystem mounting on AWS compute (EC2/ECS/Lambda). Lawgate's API runs on Azure App Service — there is no way to mount an S3 Files filesystem from it. To benefit from S3 Files, you'd also need to migrate the compute layer to AWS.

3. **Azure has equivalent compliance features.** Azure Immutable Blob Storage provides the same WORM guarantees as S3 Object Lock, which matters for legal document tamper-proofing.

4. **Processing pipelines can use Azure equivalents.** For document OCR and scanning, Azure Document Intelligence (Form Recognizer) and Azure Defender for Storage (malware scanning) are native options — no cross-cloud calls needed.

---

## Future Consideration: Adding S3 as a Secondary Provider

If Lawgate ever needs multi-cloud resilience or moves document processing to AWS Lambda, the `IBlobStorageService` interface is already abstraction-ready. A future `S3BlobStorageService` implementation could be registered behind a feature flag without touching the rest of the application.

```csharp
// Future pattern - no changes to controllers or domain
builder.Services.AddScoped<IBlobStorageService>(sp =>
    configuration["Storage:Provider"] == "S3"
        ? new S3BlobStorageService(...)
        : new AzureBlobStorageService(...));
```

---

## References

- [AWS Blog: Launching S3 Files (April 7, 2026)](https://aws.amazon.com/blogs/aws/launching-s3-files-making-s3-buckets-accessible-as-file-systems/)
- [Amazon S3 Files feature page](https://aws.amazon.com/s3/features/)
- [Azure Immutable Blob Storage docs](https://learn.microsoft.com/en-us/azure/storage/blobs/immutable-storage-overview)
- [S3 Object Lock docs](https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-lock.html)
- [S3 Access Grants docs](https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-grants.html)
- Current implementation: `backend/LegalDocSystem.Infrastructure/Services/` (AzureBlobStorageService)
- Interface: `backend/LegalDocSystem.Application/Interfaces/IBlobStorageService.cs`
