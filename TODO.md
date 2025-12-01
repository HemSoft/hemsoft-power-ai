# Spam Filter Refactoring TODO

## Overview

Refactor the spam filtering system into three distinct commands:
- `/spam-scan` - Autonomous scanning that identifies new spam domains → HumanReview.json
- `/spam-review` - Human review of flagged domains in batches → SpamDomains.json
- `/spam-cleanup` - Automatic deletion of emails from blocked domains

---

## Task 1: Create HumanReview.json Data Structure ✅

- [x] Create `Models/HumanReviewFile.cs` with structure for pending domain reviews
- [x] Structure: `{ domains: [{ domain, emailCount, samples: [{subject, sender, reason}], firstSeen, lastSeen }] }`
- [x] Create `Models/HumanReviewDomain.cs` for domain entry
- [x] Create `Models/HumanReviewSample.cs` for email samples
- [x] Create `Services/HumanReviewService.cs` to manage read/write of review file
- [x] Store in `Data/HumanReview.json`

---

## Task 2: Refactor `/spam-scan` Command (Autonomous Mode) ✅

- [x] Create `SpamScanAgent.cs` (separate from original SpamFilterAgent)
- [x] Create `SpamScanTools.cs` with read-only Graph API tools
- [x] Change workflow:
  1. Scan inbox in batches
  2. Skip emails from domains already in SpamDomains.json (already blocked)
  3. Identify suspicious domains → add to HumanReview.json
- [x] No deletions or moves - identification only
- [x] Output summary with results table
- [x] Register as `/spam-scan` command in Program.cs

---

## Task 3: Create `/spam-review` Command ✅

- [x] Create `Agents/SpamReviewAgent.cs`
- [x] Load pending domains from HumanReview.json
- [x] Process in batches (configurable via settings.ReviewBatchSize)
- [x] For each batch:
  1. Display table with domain, email count, sample subjects
  2. Ask: "Which domains are NOT spam? Enter numbers (e.g., 3,7,12) or press Enter if all are spam"
  3. User picks legitimate domains to exclude
  4. Remaining domains → add to blocklist (SpamDomains.json)
  5. Remove processed domains from HumanReview.json
- [x] After batch: prompt to continue or stop
- [x] Register as `/spam-review` command in Program.cs

---

## Task 4: Create `/spam-cleanup` Command ✅

- [x] Create `Agents/SpamCleanupAgent.cs`
- [x] Load blocked domains from SpamDomains.json
- [x] For each blocked domain:
  1. Search inbox for emails from that domain
  2. Move all found emails to junk
  3. Report progress
- [x] Summary at end with totals
- [x] Register as `/spam-cleanup` command in Program.cs

---

## Task 5: Update Command Registration ✅

- [x] Update `Program.cs` to register three new commands:
  - `/spam-scan` → SpamScanAgent (autonomous scan, identify only)
  - `/spam-review` → SpamReviewAgent (batch human review)
  - `/spam-cleanup` → SpamCleanupAgent (move to junk)
- [x] Original `/spam` → SpamFilterAgent retained
- [x] Update command table display in DisplayHeader

---

## Task 6: Update Tests

- [ ] Add tests for HumanReviewService
- [ ] Add tests for SpamScanAgent/SpamScanTools
- [ ] Add tests for SpamReviewAgent
- [ ] Add tests for SpamCleanupAgent

---

## Decisions

1. **Review batch size**: Configurable via settings (default: 20)
2. **Sample emails per domain**: 2 sample subjects per domain
3. **Cleanup scope**: Inbox only
4. **Cleanup action**: Move to junk folder

