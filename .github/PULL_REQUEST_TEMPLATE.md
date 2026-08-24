## What this changes

<!-- One or two sentences. What behaviour is different after this PR? -->

## Why

<!-- Link the issue if there is one. If there isn't, explain the problem this solves. -->

Closes #

## Approach and alternatives

<!-- What you did, and what you considered and rejected. -->

## Security impact

<!--
What can an attacker do after this change that they could not before, or what does it stop
them doing? "None — this is a documentation change" is a perfectly good answer.
-->

## Breaking changes

<!--
Any change to a request or response shape, a status code, a default, or a configuration key.
Say what a consumer (including the owner's agent scripts) has to do. "None" if none.
-->

## Checklist

- [ ] `dotnet build AutoVeritas.slnx` and `dotnet test AutoVeritas.slnx` pass
- [ ] `dotnet format AutoVeritas.slnx --verify-no-changes` passes
- [ ] `pnpm lint && pnpm test && pnpm build` pass
- [ ] Behaviour changes are covered by a test
- [ ] Documentation updated (README / docs/) where the change is user-visible
