# Contributing

Start with [README.md](README.md) and [AGENTS.md](AGENTS.md). Keep changes as the smallest coherent vertical slice, preserve dependency direction, update the dogfood model when delivered truth changes, and run the repository verification command before review.

```shell
./eng/verify.sh
```

On Windows PowerShell:

```powershell
./eng/verify.ps1
```

Do not commit secrets, local paths, build outputs, generated churn, or evidence that cannot be reproduced. Product decisions, compatibility breaks, and new boundaries require an explicit finding or ADR.
