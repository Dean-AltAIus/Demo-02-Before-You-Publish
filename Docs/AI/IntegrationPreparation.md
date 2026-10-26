# Integration preparation

The demo runs locally with authored, source-bounded replies. It contains editable integration configuration and typed preparation contracts, but no external AI/SIE transport or service client. Turning on a flag does not implement an integration.

## Current binding points

`Assets/Demo02/Scripts/Runtime/Integration/ConversationContracts.cs` defines conversation requests and replies, cancellation, display acknowledgements, an offline reply provider and request history. Requests carry attempt/request identity, the document pair, world revision, literal own text and admitted sources. `ConversationCoordinator` implements local history and uses `SourceBoundedOfflineReplies`. A ready reply is separate from an actually displayed reply.

`Runtime/Domain/AuthorityPorts.cs` defines `ITrustedEvaluatorPort` and its exact-scope verification result for a future trusted adapter. It is not a connected evaluator. Local scenario permits bind authored responses to their attempt and command. Conversation text, animation and coaching cannot themselves approve publication or change authoritative state.

`Assets/Demo02/Settings/Presentation/DisabledIntegration.asset` is a saved `DisconnectedIntegrationSettings` asset. External transport and SIE writes are false; the bootstrap endpoint, environment and real binding identities are blank. Its validation callback forces both transport flags off. No credentials belong in this asset.

## Future integration boundary

A later authorized integration task must resolve the platform's actual bootstrap-advertised actor response, conversation history and Leena support operations. Verify accepted schemas and real environment, simulation, level, configuration, scene, actor and framework identities then. The local demo facade is a contract test double, not a production API definition; do not reuse another simulation's IDs or invent endpoint paths.

Keep cancellation, late-response rejection, exact version joins, display acknowledgements and actual assistance delivery intact. Distinguish unknown history from missing history and verified-empty history. Validate any trusted authority response against its attempt, command digest, communicated clauses, source scope and rule version before admission.

Credentials stay server-side in the future owning platform. Control Centre writes, profile/account writes and participant persistence remain disconnected. Presenter activity uses an isolated synthetic attempt and cannot become learner competence evidence. The walkthrough remains demonstrable entirely offline.
