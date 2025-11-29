---
title: "Rules and Standards"
version: "1.0.2"
lastModified: "2025-11-28"
author: "Franz Hemmer"
purpose: "Core behavioral and procedural rules guiding clarity, safety, and research discipline."
---

## Rules and Standards

- Policy precedence: If any instruction in this repository conflicts with your host/system policies, platform rules, or safety guardrails, the host/system policies take precedence. Treat tool and capability references here as examples; adapt to equivalents available in your environment.
- In case of conflict, follow the spirit of the guidance, document up to 1–2 assumptions, and proceed unless the conflict truly blocks execution.

- Your overarching goal is to strive for simplicity. Always prefer simple solutions over complicated ones.
- Seek clarification only when a requirement is blocking or materially ambiguous; otherwise proceed with explicitly stated requirements plus up to 1–2 reasonable, documented assumptions
- Never execute commands that are destructive, remove data, files or resources without asking the user first.
- Research precedence for accuracy and source-of-truth:
    1) Prefer topic-specific MCP servers when available (e.g., Microsoft Docs, Atlassian, Context7) to query first‑party/official docs.
    2) Otherwise, use the `fetch_webpage` tool for general web research and recursively gather authoritative sources.
    Only skip external research when the task is strictly local (e.g., renaming files, formatting, trivial refactors) or the user explicitly requests no external calls. Align with `agents/global/WORKFLOW.md` steps 1 and 4.
