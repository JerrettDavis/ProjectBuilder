# ADR-0003: Use a Blazor Web App with an Interactive WebAssembly Studio Area

## Status

Accepted for initial implementation, with an early performance and interoperability spike.

## Context

The product is C#/.NET 10 and requires a highly interactive authoring studio. It also needs server-rendered entry, authentication, administration, and resilient ordinary web behavior. A fully client-side application would increase initial load and duplicate some server concerns. A fully server-interactive canvas could add latency and connection sensitivity to high-frequency editing.

## Decision

Use an ASP.NET Core Blazor Web App.

- server-rendered pages for entry, administration, and content that does not require rich client state,
- Interactive WebAssembly for the Studio area and high-frequency editing,
- thin JavaScript modules only for browser capabilities where the web platform API is not practical from C#,
- Minimal APIs for stable application boundaries,
- renderer and command contracts independent of Blazor components.

Do not assume offline editing in the first release. Preserve identifiers and draft contracts that can support it later.

## Consequences

### Benefits

- primary implementation language remains C#,
- shared contracts and validation where client-safe,
- server-rendered shell and interactive studio can coexist,
- high-frequency canvas work can remain local to browser,
- ordinary ASP.NET Core identity, API, telemetry, and deployment.

### Costs

- careful client/server contract boundary,
- WebAssembly load and memory must be measured,
- JavaScript interop still exists for pointer capture, clipboard, file, observers, and text measurement,
- shared assemblies must not leak server-only behavior or secrets.

## Spike requirements

- pointer/keyboard command throughput,
- SVG rendering at reference sizes,
- clipboard and file APIs,
- resize/text measurement,
- reconnect and draft recovery,
- authentication/cookie behavior,
- accessibility tree,
- bundle/startup performance.

## Rejected alternatives

### Desktop-only UI

Rejected because collaboration, deployment, and broad accessibility favor the web.

### Pure JavaScript/TypeScript SPA

Not selected because C# is the required platform and shared typed behavior is valuable. It remains a fallback if the spike exposes a blocking limitation.

### Server-interactive Studio only

Not selected as the default for high-frequency editing due to latency and connection dependence. It can still be used for selected components.

## Review triggers

- WebAssembly startup or memory fails the product envelope,
- browser API interop becomes the majority of the editor,
- accessibility cannot be achieved,
- offline or desktop packaging becomes a primary requirement.
