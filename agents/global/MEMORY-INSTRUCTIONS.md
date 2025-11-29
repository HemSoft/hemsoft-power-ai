---
title: "Memory Instructions"
version: "1.1.1"
lastModified: "2025-11-28"
author: "Franz Hemmer"
purpose: "Persistent memory implementation for AI agents"
---

## MEMORY-INSTRUCTIONS.md -- Persistent Memory for AI Agents

## Purpose

Version-controlled knowledge storage in `/agents/memories` enabling knowledge continuity, team collaboration, and queryable structured memory across conversations

## Core Components

### Entities

Primary knowledge graph nodes. Types: `project`, `person`, `decision`, `concept`, `issue`, `resource`, `meeting`, `task`

### Observations

Atomic facts in format: `- [category] content #tags (context)`
Categories: `fact`, `decision`, `preference`, `constraint`, `tip`, `issue`, `resource`, `question`, `experiment`

### Relations

Directed connections: `- relation_type [[Target]] (context)`
Types: `depends_on`, `relates_to`, `implements`, `supersedes`, `documents`, `used_by`, `owned_by`, `blocked_by`

### Storage Format

```markdown
---
title: Entity Title
type: entity-type
permalink: unique-slug
created: 2025-10-02T10:00:00Z
updated: 2025-10-02T10:00:00Z
tags: [tag1, tag2]
---

# Entity Title

## Observations
- [category] Content #tag (context)

## Relations
- relation_type [[Target]] (context)

## Notes
Additional context
```

### Directory Structure

```text
/agents/memories/
├── entities/{type}/    # Organized by entity type
├── index/              # Generated indices (gitignored)
├── schemas/            # JSON validation schemas
└── .memoryconfig       # Configuration
```

## Retrieval Methods

| Method | Use Case | Implementation |
|--------|----------|----------------|
| **Semantic** | Related concepts, exploratory queries | Vector similarity (cosine), threshold > 0.7, expand via relations |
| **Text** | Known terms, names, phrases | SQLite FTS5, boolean operators, fuzzy matching |
| **Graph** | Dependencies, relationships, impact | Traverse from seed entities, filter by type, handle cycles |
| **Temporal** | Recent work, evolution tracking | Sort by timestamps, date filtering |

### Retrieval Strategy

1. Start with specific search → 2. Get top 3-5 results → 3. Follow 1-2 relation levels → 4. Filter by relevance/recency → 5. Summarize concisely

## Writing Memory (Storage)

### The Golden Rule: Quality Over Quantity

**Record selectively.** Recording everything creates noise.

### Future Value Test

Record if **3+ YES**:

- **Reusability**: Helps future similar situations?
- **Non-Obviousness**: Would need researching again?
- **Continuity**: Provides context across conversations?
- **Uniqueness**: NOT documented elsewhere?
- **Longevity**: Relevant in 3+ months?

### Record HIGH-VALUE Only

✅ **Record**: Architectural decisions, non-obvious solutions, reusable patterns, project constraints, important gotchas, team standards
❌ **Skip**: Routine ops, temporary info, documented elsewhere, over-specific details, universal knowledge

### Writing Guidelines

- **Entities**: Choose type, unique permalink (kebab-case), initial observations, establish relations, add tags
- **Observations**: Categorize, atomic, specific, tagged, contextualized, timestamped
- **Relations**: Active voice, bidirectional when appropriate, avoid redundancy
- **Quality**: Specific, actionable, verifiable, consistent, relevant, selective

### Health Indicators

- ✅ Healthy: Find info in 1-2 searches, 3-10 observations per entity
- ❌ Saturated: Mostly irrelevant results, duplicate info

## Configuration

### `.memoryconfig`

```yaml
version: "1.0"
storage: {root: "./agents/memories", entities_path: "entities", index_path: "index"}
indexing: {embedding_model: "text-embedding-3-small", chunk_size: 512, overlap: 50, min_similarity: 0.7}
search: {max_results: 10, default_depth: 2, include_metadata: true, highlight_matches: true}
git: {auto_commit: false, commit_message_template: "Memory update: {operation} {entity_type}"}
pruning: {max_age_days: 365, min_references: 2, archive_path: "archive"}
```

### Schemas

Located in `schemas/`: `entity.json`, `observation.json`, `relation.json`
Required fields: title, type, permalink, created, updated

## Best Practices

### AI Agents

- Start conversations with memory retrieval, show what you remember
- Progressive disclosure: start relevant, expand as needed
- Update memories during conversations, establish relations
- Quality over quantity: accurate > many, consolidate, remove obsolete
- Leverage graph structure and relations

### Users

- Review regularly: correct inaccuracies, add context, establish relations
- Consistent terminology: established names, standard tags/categories
- Git: clear commits, review diffs, branches for experiments, tag milestones
- Security: no credentials/PII, use `.gitignore`, review before push
- Maintenance: archive old, consolidate, remove orphans, update stale

## Workflow Integration

**Problem Solving**: Search context → Document solutions → Track progress
**Development**: Record arch decisions → Document patterns → Track bugs
**Meetings**: Capture key points → Track action items

## Versioning

**Semantic Versioning** (MAJOR.MINOR.PATCH):

- **MAJOR**: Breaking changes (remove/rename fields, change types)
- **MINOR**: Backward-compatible additions (new optional fields, entity types)
- **PATCH**: Bug fixes, documentation improvements

**Current**: Memory System 1.1.0 (2025-10-02), Schemas 1.0.0

**Migration**: Minor/patch → update version; Major → new version + migration script + 3-month transition

**Git Versioning**: Entities tracked via Git (history, diffs, blame, revert, branches, tags)

## Implementation Checklist

**Setup**: Create directory structure → Add schemas → Configure `.memoryconfig` → Gitignore indices
**Agent**: Implement semantic/text/graph search → Add read/write operations → Test accuracy
**Team**: Share conventions → Example files → Review process → Maintenance schedule

## Advanced Topics

- **Pruning**: Archive old, remove orphaned, consolidate duplicates
- **Migrations**: Version format, provide scripts, test, document
- **Merging**: Git strategies, conflict detection, resolution
- **Privacy**: Visibility controls, encryption, audit logs
- **Analytics**: Referenced entities, coverage gaps, growth, patterns

---

*Start simple, iterate often, let the knowledge graph grow organically.*
