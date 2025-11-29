# Microsoft Agent Framework - A2A Protocol [AI in C#]

## Video Metadata

**Title:** Microsoft Agent Framework - A2A Protocol [AI in C#]

**Channel:** Rasmus Wulff Jensen

**Description:** We have reached video number 50 in my YouTube series on the Microsoft Agent Framework 🎉... In this video, we explore the Google A2A (Agent to Agent) protocol, which is similar to the MCP Protocol, but offers its set of unique features

**Categories:** Education

**Video ID:** g72ks3rY9qQ

**URL:** https://www.youtube.com/watch?v=g72ks3rY9qQ

**Duration:** 13:14 (794 seconds)

**Date Published:** 2025-11-05

**Date Downloaded:** 2025-11-05

**Engagement:**
- Likes: 6
- Comments: 1

**ClickBait Score:** 3/10 (Straightforward educational title accurately describing the content with no sensationalism)

**Total Segments:** 104

---

## Executive Summary

This video marks the 50th installment in Rasmus Wulff Jensen's series on the Microsoft Agent Framework, focusing on Google's A2A (Agent to Agent) protocol. The A2A protocol enables multiple AI agents to communicate and collaborate across different processes or even different geographical locations, similar to the Model Context Protocol (MCP) but with enhanced intelligence capabilities. The protocol uses "agent cards" - business card-like metadata that remote agents expose to describe their capabilities, skills, and available tools, allowing local applications to consume remote agent services over HTTP URLs.

The video demonstrates a practical implementation where a client application communicates with a server agent that has file management capabilities. The server exposes tools for operations like listing, creating, and deleting files through an ASP.NET Core endpoint, while the client treats this remote agent as a tool in its own agent system. This architecture showcases how agents can be distributed globally while maintaining seamless integration, with one agent in the US potentially communicating with another in Europe through standard HTTP protocols.

Throughout the demonstration, Rasmus walks through both server and client code implementations, showing how the Microsoft Agent Framework makes it surprisingly simple to set up A2A communication. The server uses the AI.Hosting.A2A.AspNetCore NuGet package to expose agent capabilities through a web API, complete with an agent card configuration that describes its skills, input/output modes, and available endpoints. The client consumes this remote agent using just a few lines of code, converting a URL-based remote agent into a local tool that can be used by the framework's standard agent architecture.

---

## Content Type

**Category:** ✓ Educational, ✓ Tutorial, ✓ Technical Demo

**Audience:** C# developers working with AI and agent frameworks, software architects interested in distributed agent systems, developers familiar with or learning the Microsoft Agent Framework

**Tone:** Conversational, Technical, Educational

---

## Main Topics Covered

### Topic 1: Introduction to A2A Protocol and Comparison with MCP
**Timestamp:** 00:00 - 01:02
**Key Points:**
- This is the 50th video in the Microsoft Agent Framework series
- A2A (Agent to Agent) is a Google-created protocol for multi-agent communication
- A2A is very similar to MCP (Model Context Protocol) but has more intelligence built in
- Understanding agents calling other agents as tools is beneficial context
- The key difference is that in A2A, the other agent is remote rather than local

**Notable Quotes:**
> "If you are familiar with MCPs, A2A is very similar to MCPs, but there is a bit more of intelligence in here."

**Analysis:** This introduction sets up the fundamental concept that A2A isn't entirely new if you understand MCP, but it extends the capability with additional intelligence features. The speaker positions this as building on previous knowledge in the series, particularly the concept of agents using other agents as tools, making it more accessible to his existing audience.

---

### Topic 2: Understanding Agent Cards and Architecture
**Timestamp:** 01:02 - 02:26
**Key Points:**
- Local apps connect to remote agents via URLs
- Remote agents expose their capabilities through "agent cards"
- Agent cards describe what the agent does, its skills, and how to interact with it
- Agent cards are like business cards for agents, similar to MCP's list tools command
- The architecture allows agents in different parts of the world to communicate
- Local apps can use remote tools as if they were connected directly

**Notable Quotes:**
> "It's sort of like a business card if you met a person where you could find information about what they can do and how they can help and what to talk with them."

**Analysis:** The business card analogy effectively simplifies the concept of agent cards, making it relatable and easy to understand. This architecture represents a significant shift toward distributed AI systems where capabilities can be exposed and consumed across network boundaries, enabling more modular and scalable agent ecosystems.

---

### Topic 3: Live Demo Setup and File Operations
**Timestamp:** 02:26 - 04:04
**Key Points:**
- The demo includes both a server and client running simultaneously
- The server has knowledge about files on the local machine
- The demo folder contains 10 files that the agent can manipulate
- The system can perform both information retrieval (counting files) and actions (deleting files)
- The server executes operations on behalf of the client's requests
- All operations happen over a URL even though both are local in the demo

**Notable Quotes:**
> "And over here, things are happening on behalf of this."

**Analysis:** The live demonstration effectively shows the practical application of A2A, starting with simple operations like counting files and progressing to more complex actions like file deletion. This demonstrates both read and write capabilities, proving that A2A isn't limited to information retrieval but can also execute actions on remote systems.

---

### Topic 4: Server Implementation Deep Dive
**Timestamp:** 04:41 - 06:09
**Key Points:**
- Server uses the AI.Hosting.A2A.AspNetCore NuGet package
- Console app is configured to work as ASP.NET Core to expose a URL
- The server exposes a well-known agent card JSON endpoint
- Standard agent setup with Azure OpenAI client and tool registration
- Function calling middleware allows visibility into tool execution
- Tools aren't required - agents could excel at understanding, use vector stores, or expose specialized models

**Notable Quotes:**
> "You don't technically need tools here. It could be that it was very good at understanding something. It was hooked up to a vector store."

**Analysis:** This section reveals the flexibility of the A2A protocol - it's not just about exposing tools but about exposing any kind of specialized capability an agent might have. The implementation shows how Microsoft has made it straightforward to convert a console application into an agent server using familiar ASP.NET Core patterns.

---

### Topic 5: Agent Card Configuration
**Timestamp:** 06:09 - 07:55
**Key Points:**
- Agent card serves as the contract between client and server
- Configuration includes agent name ("files agent"), description, and version (1.0)
- Input and output modes are specified (text in this case)
- Protocol supports authentication, streaming, and notifications (not shown in basic demo)
- Skills are defined with descriptions, tags for categorization, and usage examples
- URL endpoint is specified for agent communication

**Notable Quotes:**
> "So we tell, hey, I'm an agent. I'm called files agent. I can handle requests related to files."

**Analysis:** The agent card configuration is the heart of the A2A protocol, providing a standardized way for agents to advertise their capabilities. This metadata-driven approach enables discovery and integration patterns similar to REST API documentation but specifically designed for AI agent interactions.

---

### Topic 6: Server Endpoint Configuration and Tools
**Timestamp:** 07:55 - 08:40
**Key Points:**
- Tools use standard C# code for operations like create folder and delete folder
- The MapA2A method is similar to MapGet/MapPost in minimal APIs
- Server specifies which agent to expose and at what path (root in this case)
- Both the agent and agent card are registered with the endpoint
- Well-known agent card endpoint is automatically configured
- The server runs a mini web server within the console application

**Analysis:** The similarity to minimal API patterns in ASP.NET Core makes this technology immediately familiar to .NET developers. The framework abstracts away much of the complexity, reducing the entire server setup to declarative configuration that mirrors existing web development patterns.

---

### Topic 7: Client Implementation
**Timestamp:** 08:40 - 10:14
**Key Points:**
- Client uses AI.A2A package instead of AI.Hosting.A2A (consumer vs. host)
- One-second delay ensures server is ready before client attempts connection
- A2ACardResolver takes the remote URL and resolves the agent card
- The framework converts the remote agent into an AI agent object
- Remote agent is then given as a tool to the local agent
- Beyond the remote agent setup, everything else is standard agent framework code
- Local agent receives instructions and operates normally with the remote agent as a tool

**Notable Quotes:**
> "And the agent framework just gives us and give me that as an AI agent and we're done."

**Analysis:** The client implementation is remarkably simple - just a few lines of code to consume a remote agent. This simplicity demonstrates excellent framework design where complex distributed systems concepts are abstracted into intuitive APIs. The ability to treat remote agents as local tools seamlessly bridges the gap between distributed and monolithic architectures.

---

### Topic 8: Advanced Demo - File Organization
**Timestamp:** 10:14 - 12:04
**Key Points:**
- Demo shows complex operation: grouping files into folders by color
- Agent finds root folder and reads content of each file
- System requests user confirmation before proceeding with file operations
- The confirmation is standard AI behavior, not specific to A2A
- Agent executes work by creating color-based folders (yellow, etc.)
- Operations like moving banana, lemon, and pear files to appropriate folders happen through remote calls
- The AI behaves identically whether tools are local or remote

**Analysis:** This advanced demonstration proves that A2A can handle complex, multi-step operations that require reasoning and planning. The fact that the AI's behavior is identical regardless of tool location validates the abstraction provided by the framework and shows that developers don't need to change their agent logic when moving to distributed architectures.

---

### Topic 9: A2A vs. MCP Comparison and Use Cases
**Timestamp:** 12:04 - 12:53
**Key Points:**
- Both systems enable cross-process and cross-geography communication
- In the demo, two processes on same machine represent what could be globally distributed servers
- Example given: one server in US, another in Europe communicating seamlessly
- A2A sounds similar to MCP but offers more advanced features
- MCP could technically wrap AI in a tool, but it's not designed for that purpose
- A2A is meant for "bigger things" and "more advanced things"
- Rasmus expects to use MCP more than A2A in practice
- A2A makes sense for specific use cases requiring more intelligence

**Notable Quotes:**
> "You could technically just make a tool called files tool in MCP and have an AI work behind the scenes. But it's not really meant for that. This is more meant for that. It's bigger things. It's more advanced things that are happening."

**Analysis:** This comparison helps developers understand when to choose A2A over MCP. While both enable distributed capabilities, A2A's design specifically accommodates AI agents with reasoning and decision-making capabilities, making it suitable for scenarios where the remote service needs to perform complex analysis or multi-step operations rather than just executing deterministic functions.

---

### Topic 10: Conclusion and Simplicity Assessment
**Timestamp:** 12:53 - 13:14
**Key Points:**
- The implementation is surprisingly simple despite being a complex topic
- Only two key lines of code enable the entire remote agent capability
- Those two lines convert a URL into a usable agent tool
- The complexity is well-abstracted by the framework
- Marks the completion of video 50 in the series

**Notable Quotes:**
> "A little higher complex topic, but behind the scenes, it's actually very simple that it's just these two lines of code that give us an agent that we can use as a tool."

**Analysis:** The conclusion emphasizes the framework's excellent design - taking a conceptually complex distributed AI system pattern and reducing it to minimal code. This low barrier to entry will likely accelerate adoption of distributed agent architectures in enterprise applications.

---

## Key Takeaways

- **A2A Protocol Enables Distributed Agent Communication**: Google's A2A (Agent to Agent) protocol allows AI agents to communicate and collaborate across different processes and geographic locations using HTTP URLs, with "agent cards" serving as metadata that describes each agent's capabilities, skills, and available tools.

- **Similar to MCP but More Intelligent**: While A2A shares similarities with the Model Context Protocol (MCP), it's specifically designed for more complex scenarios where remote services need to perform reasoning and multi-step operations, not just execute simple deterministic functions.

- **Remarkably Simple Implementation**: Despite the conceptual complexity of distributed AI systems, the Microsoft Agent Framework reduces A2A implementation to just a few lines of code - the server uses MapA2A to expose agents, while the client uses A2ACardResolver to consume them as local tools.

- **Agent Cards as Service Contracts**: Agent cards function like business cards for AI agents, providing structured metadata about capabilities, input/output modes, versions, skills, and endpoints. This enables standardized discovery and integration patterns specifically designed for AI agent interactions.

- **Transparent Local and Remote Operations**: The framework successfully abstracts the location of tools - AI agents behave identically whether executing local or remote operations. Developers don't need to modify agent logic when transitioning from monolithic to distributed architectures.

- **Console Apps as Agent Servers**: Using the AI.Hosting.A2A.AspNetCore NuGet package, console applications can easily expose agent capabilities through ASP.NET Core endpoints, making it simple to deploy agent services without building full web applications.

- **Beyond Simple Tool Calling**: A2A isn't limited to exposing tools - agents can advertise any specialized capability including expertise in specific domains, connections to vector stores, or use of specialized models. This flexibility enables diverse agent ecosystem architectures.

- **Real-World Distribution Scenarios**: While demos often run locally, the architecture supports true distributed deployments where agents in different continents can collaborate seamlessly, enabling global AI service networks and specialized agent marketplaces.

---

## Technical Details

### Technologies Mentioned
- **A2A (Agent to Agent) Protocol** - Google's protocol for enabling communication between AI agents across different processes and locations
- **MCP (Model Context Protocol)** - Alternative protocol for agent communication, mentioned for comparison
- **Microsoft Agent Framework** - The framework being demonstrated throughout the series
- **ASP.NET Core** - Used to expose agent endpoints via HTTP in console applications
- **Azure OpenAI** - The AI service used for the agent's language model capabilities
- **C#** - The programming language used for all implementations

### Tools & Products
- **AI.Hosting.A2A.AspNetCore** - NuGet package for hosting A2A agents in ASP.NET Core applications
- **AI.A2A** - NuGet package for consuming remote A2A agents in client applications
- **A2ACardResolver** - Class used to resolve agent cards from remote URLs
- **Visual Studio** - Development environment used for the multi-project solution setup
- **Agent Card JSON** - Well-known endpoint (/.well-known/agent-card.json) that exposes agent metadata

### People Mentioned
- **Rasmus Wulff Jensen** - Video creator and presenter of the Microsoft Agent Framework series

---

## Statistics & Data

- **Stat:** Video number 50 in the series
  **Context:** This milestone indicates significant depth of coverage on the Microsoft Agent Framework topic

- **Stat:** 10 files in the demo folder
  **Context:** Used to demonstrate file counting and manipulation operations through the remote agent

- **Stat:** 2 lines of code for core A2A implementation
  **Context:** Demonstrates the simplicity and excellent abstraction provided by the framework despite the complex distributed systems concepts

- **Stat:** 1 second delay for client startup
  **Context:** Technical implementation detail ensuring server is ready before client connection attempts

---

## Resources & Links

- **Title:** Microsoft Agent Framework Sample Repository
  **Context:** The video demonstrates code from the official sample repository, specifically the "agent to agent" folder with server and client projects

- **Title:** Previous video on agent calling another agent as a tool
  **Context:** Referenced as beneficial prerequisite knowledge for understanding A2A concepts

- **Title:** Tool calling advanced video
  **Context:** Referenced for detailed explanation of the file management tools used in the server implementation

---

## Tags

Microsoft Agent Framework, A2A Protocol, Agent to Agent, C#, Azure OpenAI, Distributed Agents, Agent Cards, MCP Protocol, ASP.NET Core, AI Agents, Remote Agent Communication, Google A2A, Rasmus Wulff Jensen, AI in C#, Agent Communication Protocols, Distributed AI Systems, Agent Tools, File Management Agent

---

## Transcription

Full transcript available in VTT format: 2025-11-05 - microsoft-agent-framework-a2a-protocol-ai-in-c.en.vtt

Duration: 13:14 (794 seconds)

---
