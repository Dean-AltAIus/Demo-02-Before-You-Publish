# Demo-02-Before-You-Publish

University marking and assessment design, a compact third-person campus simulation.

The intended learner practice is to examine what an assessment rewards, mark contrasting fictional work, explain a counterexample, revise and retest the task/rubric, then seek review of the exact current versions or arrange a responsible hold.

## Current status

Project preparation only, 25 October 2026. Unity 6000.0.71f1, Universal Render Pipeline 17.0.4, Windows desktop, keyboard and mouse. The saved template SampleScene is a setup baseline. Demo gameplay and production assets have not been implemented.

Development uses `dev`. The initial `main` branch is preserved. Do not trigger GitHub-hosted builds or manually dispatch build workflows. Use necessary local Editor validation; create player builds only when they serve an agreed acceptance need.

Read [project context](Docs/AI/UnityProjectContext.md), [production requirements](Docs/AI/ProductionRequirements.md), [work order](Docs/AI/BuildoutPlan.md), and [integration preparation](Docs/AI/IntegrationPreparation.md). Machine-specific source locations and developer reference receipts are in ignored `LocalOnly/`.

## Current handoff

The requested MCP URL is now present in Packages/manifest.json following the Editor handoff. Dean finishes package setup and starts its server/Editor bridge. Resume implementation only after he reports it ready and the correct project is verified. The requested package URL is:

`https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`

The full reference pack stays outside Assets and outside this public repository. Private reviewer oracles, unrestricted source snapshots, credentials and participant data must not be imported into learner-facing content.
