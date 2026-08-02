# AGENTS.md

## Project

Ashwood County is a Godot 4.7.1 .NET third-person 3D zombie survival RPG.

The intended experience is a believable, atmospheric and highly interactive survival game inspired by:

* State of Decay
* Project Zomboid
* Mist Survival
* The Walking Dead

The project is no longer restricted to a small vertical slice.

The goal is to progressively develop the complete game, including its world, interiors, gameplay systems, survival mechanics, presentation, atmosphere and supporting technology.

The repository may contain unfinished prototypes, temporary implementations, outdated design assumptions and systems that need substantial improvement.

Do not treat the current implementation as the maximum intended scope.

---

## Authority of the Current User Request

The user’s current request is the highest-priority project instruction.

When the user explicitly requests broad work such as:

* improving the complete game
* redesigning multiple systems
* building several locations
* performing a visual overhaul
* implementing a major roadmap phase
* auditing the entire project
* improving overall quality
* continuing autonomously across multiple related tasks

you are authorised to work across the relevant parts of the repository.

Do not silently reduce a broad request to a tiny vertical slice.

Do not substitute a smaller task merely because it is easier, safer or more convenient.

If the requested scope is extremely large, create an internal execution plan and complete as much meaningful work as possible in priority order.

Clearly report what was completed and what remains.

Do not claim that the full request was completed when only a small portion was addressed.

---

## Instruction Conflicts

If any project document conflicts with the user’s current explicit request, the current request takes precedence unless the user specifically says otherwise.

Documents such as:

* `docs/roadmap.md`
* `docs/devlog.md`
* `docs/vision.md`
* location design documents
* implementation notes
* previous plans
* archived task descriptions

are context and guidance, not permanent restrictions.

Do not allow an outdated roadmap item, vertical-slice instruction or previous development phase to override a newer explicit request.

When a significant conflict exists, state it before implementation rather than silently choosing the narrower interpretation.

---

## Development Objective

Develop Ashwood County as a complete, cohesive and enjoyable game.

Aim for the highest quality realistically achievable with:

* the existing project
* available assets
* Godot 4.7.1
* C#
* the development hardware
* the current task
* the available execution context

Quality includes:

* strong gameplay
* believable environments
* readable level design
* responsive controls
* effective combat
* coherent art direction
* convincing lighting
* appropriate sound and visual feedback
* meaningful exploration
* reliable technical implementation
* good performance
* maintainable systems

High visual quality must not come at the expense of broken gameplay, unusable navigation or severe performance problems.

Do not describe work as AAA merely because it builds successfully.

Evaluate the result honestly against the relevant visual and gameplay references.

---

## Technology

Current technology:

* Godot 4.7.1 .NET
* C#
* Third-person 3D
* Compatibility renderer unless the project has intentionally changed
* Existing Godot scenes, resources and imported assets

Use C# for gameplay and editor tooling unless another format is clearly more appropriate.

You may recommend or implement major technical changes when they materially improve the project and are compatible with the user’s request.

Do not change the engine or programming language without explicit permission.

Renderer settings, project settings, shaders, import settings, materials and performance systems may be changed when necessary, provided the project remains functional.

---

## Project-Wide Development

You are authorised to:

* inspect the complete repository
* inspect nested asset directories
* improve multiple related scenes in one task
* modify existing gameplay systems
* replace weak temporary implementations
* refactor systems when justified
* create reusable tools
* improve world layout
* build interiors and exteriors
* improve lighting and materials
* add gameplay interactions
* improve UI and feedback
* improve navigation and collision
* create project-owned resources
* create project-owned textures, meshes, materials and generated assets
* improve performance
* update documentation
* restructure code where the existing architecture blocks progress

Do not artificially limit work to one building, one scene, one system or one roadmap item unless the user requests that limitation.

Do not stop merely because one small portion of a larger request is functional.

Continue through the requested scope while meaningful progress can still be made.

---

## Planning Large Tasks

For large tasks:

1. Inspect the repository and relevant documentation.
2. Inspect Git status.
3. Identify existing working systems and unfinished work.
4. Determine dependencies between requested changes.
5. Establish a priority order.
6. Implement in coherent stages.
7. Validate each major stage.
8. Reassess visual, gameplay and technical quality.
9. Continue improving weak areas within the requested scope.
10. Report completed work and remaining gaps honestly.

Do not spend most of the task creating plans, benchmarks or test infrastructure while leaving the actual game mostly unchanged.

Validation tools should support development, not replace it.

Prioritise visible and playable improvements.

---

## Autonomous Work

When the user requests autonomous, comprehensive or project-wide work, you may continue across multiple related tasks without waiting for separate permission after every small step.

You may:

* fan out sub-agents
* assign repository audits
* assign asset discovery
* assign scene analysis
* assign gameplay validation
* assign visual critique
* assign performance investigation
* compare alternative implementations
* revise weak work
* continue into the next related component

Sub-agents must not modify overlapping files simultaneously unless their work is coordinated.

Use sub-agents for meaningful parallel work, not merely to produce duplicate summaries.

Integrate and review their output before accepting it.

Do not treat a sub-agent’s claim of success as proof.

Inspect the actual changes.

---

## Quality Review

For visual or world-building tasks, review the result based on:

* silhouette
* scale
* composition
* material quality
* texture consistency
* lighting
* prop placement
* environmental storytelling
* visual hierarchy
* repetition
* gameplay readability
* third-person camera behaviour
* traversal
* combat space
* navigation
* collision
* performance
* consistency with concept art
* consistency with the rest of Ashwood County

For gameplay tasks, review:

* responsiveness
* clarity
* player feedback
* edge cases
* persistence
* interactions
* animation transitions
* enemy behaviour
* camera behaviour
* failure states
* save/load implications
* performance

A build passing is only one validation criterion.

Do not declare artistic or gameplay success solely from automated tests.

Use rendered screenshots, scene inspection, runtime traversal or available visual review methods where appropriate.

Be a harsh but practical critic.

When something is visibly weak, improve it rather than merely documenting that it is weak.

---

## Reference Quality

Games such as State of Decay, Project Zomboid and Mist Survival are quality references, not guarantees that a small team or autonomous coding session can reproduce their entire production scope.

Use references to identify concrete qualities such as:

* environmental density
* composition
* material fidelity
* atmosphere
* animation quality
* combat readability
* world believability
* interface clarity
* survival tension
* interaction depth

Do not make unverifiable claims that Ashwood County is objectively better than a commercial reference.

When comparing quality, identify specific gaps and improvements.

Do not waste large amounts of execution time repeatedly producing abstract scores without improving the project.

---

## Asset Usage

Before creating new assets, recursively inspect relevant directories, including:

* `res://assets/characters/`
* `res://assets/environment/`
* `res://assets/items/`
* `res://assets/materials/`
* `res://assets/third_party/`
* all nested subdirectories

Reuse suitable existing assets whenever possible.

Do not assume an asset is unavailable without checking.

Project-owned wrappers, inherited scenes, material overrides and composed scenes are preferred when they preserve source assets cleanly.

Third-party assets may be used, instanced, configured and visually integrated.

Avoid destructive modification of original third-party source files when a project-owned override or derived asset is practical.

However, this rule must not prevent necessary project progress.

You may create optimised or modified project-owned copies when required.

Do not download new assets unless the user explicitly permits it.

---

## Visual Development

You may improve:

* environment geometry
* terrain
* foliage
* roads
* sidewalks
* buildings
* interiors
* props
* materials
* shaders
* lighting
* fog
* sky
* post-processing
* VFX
* decals
* weather presentation
* environmental damage
* clutter
* signage
* landmarks
* distant scenery
* scene composition

Visual improvements must remain compatible with gameplay.

Avoid:

* blocking navigation with decorative props
* camera traps
* floating objects
* obvious intersections
* repeated props without variation
* unrealistic scale
* empty rooms with no functional identity
* excessive clutter
* extreme lighting that harms visibility
* expensive effects with little visible benefit
* scenery that looks good only from one screenshot angle

---

## World and Interior Design

Gameplay spaces should communicate their function immediately.

Buildings should contain believable zones for:

* customers
* staff
* storage
* loot
* traversal
* combat
* entry and exit
* environmental storytelling

Interiors should support:

* third-person camera movement
* melee combat
* zombie navigation
* clear circulation
* readable entrances
* believable dimensions
* useful loot placement
* multiple tactical choices where appropriate

Do not blindly preserve cramped footprints if they damage gameplay.

Where appropriate:

* preserve storefront frontage
* extend buildings behind the façade
* widen structures when street composition allows
* revise room arrangements
* relocate non-structural props
* rebuild temporary interiors
* improve ceiling height
* improve aisle width
* add staff and storage areas
* add loading and service access

Do not simply scale complete scenes unless uniform scaling is genuinely correct.

---

## Gameplay Systems

Existing systems are not untouchable.

They should be preserved when they work well, but may be modified, replaced or refactored when required by the task.

Relevant systems may include:

* player movement
* camera
* combat
* weapons
* zombies
* animation
* inventory
* containers
* interaction
* looting
* quests
* dialogue
* saving
* world persistence
* vehicles
* survival systems
* base management
* NPC systems
* audio
* UI
* world streaming
* spawning
* navigation
* authoring tools

Before replacing a working system, understand its current dependencies.

Avoid breaking unrelated behaviour unnecessarily.

Do not preserve a poor implementation solely because it already exists.

---

## Inventory and Container Rules

World containers and the player should maintain separate inventory state.

Searching a container may generate or reveal its contents but should not automatically transfer everything to the player unless a specific gameplay design intentionally requires that behaviour.

Items should move through explicit interactions.

Containers should retain:

* uncollected items
* items added by the player
* searched state
* generated state
* persistent contents where required

Prefer a shared configurable container-inventory foundation rather than unrelated implementations for every container type.

These are gameplay design defaults and may be changed when the user explicitly requests a different system.

---

## Coding Standards

* Use C# for gameplay systems.
* Keep classes focused where practical.
* Prefer composition over unnecessary inheritance.
* Expose genuinely tunable values with `[Export]`.
* Use descriptive names.
* Avoid unexplained magic numbers.
* Avoid duplicate implementations.
* Reuse systems where appropriate.
* Refactor when duplication or architecture materially blocks progress.
* Add comments for non-obvious behaviour.
* Preserve scene and resource references during refactors.
* Handle missing nodes and resources safely where appropriate.
* Avoid unnecessary per-frame allocations.
* Avoid expensive scene-tree searches inside frequent update loops.
* Consider save compatibility when changing persistent data.
* Keep editor tools safe to run repeatedly.

Maintainability matters, but it must not be used as an excuse to avoid requested features.

---

## Plugins and Dependencies

Prefer existing project systems and built-in Godot functionality.

External dependencies may be proposed when they provide substantial value.

Do not add paid, network-dependent or licence-restricted dependencies without explicit approval.

Free local dependencies may be added only when:

* their licence is compatible
* they are genuinely useful
* they do not create unnecessary project fragility
* the user’s request permits broader technical changes

Document any added dependency clearly.

---

## Repository Safety

Before making changes:

1. Inspect Git status.
2. Identify uncommitted user work.
3. Avoid overwriting unrelated changes.
4. Inspect relevant scenes, scripts and resources.
5. Understand current dependencies.

Preserve unrelated work.

Do not reset, clean, discard, revert or overwrite user changes unless explicitly instructed.

Do not use destructive Git operations without explicit permission.

Do not push unless explicitly requested.

Do not create a commit unless the user requests one or the active task explicitly requires one.

When no commit is requested, leave the changes uncommitted and report them.

---

## Documentation

Relevant documents may include:

* `docs/vision.md`
* `docs/roadmap.md`
* `docs/devlog.md`
* `docs/assets_reference.md`
* location design documents
* concept art
* implementation notes
* gameplay design documents

Read documents relevant to the task.

Do not read only `roadmap.md` and assume it overrides newer instructions.

Use concept art and design documents as creative and structural references.

Update documentation when changes make existing documentation inaccurate or when the user requests documentation updates.

Do not spend excessive time rewriting documents instead of improving the project.

---

## Development Workflow

Before implementation:

1. Read this file.
2. Read the user’s complete request.
3. Inspect Git status.
4. Inspect relevant project documents.
5. Inspect relevant scenes, scripts and assets.
6. Identify conflicts between the request and existing implementation.
7. Determine the real scope before editing.

During implementation:

* Work across all relevant files needed to satisfy the request.
* Preserve unrelated user work.
* Reuse existing assets and systems where suitable.
* Replace inadequate temporary work when justified.
* Validate major changes incrementally.
* Review visual and gameplay quality during development.
* Correct weak implementations instead of stopping at the first functional result.
* Keep working through the requested priority order.
* Do not reduce broad work to one arbitrary example without clearly stating that limitation.

Before finishing:

* Build the C# project.
* Fix compilation errors introduced by the changes.
* Verify edited scenes load.
* Run available relevant validation.
* Test affected gameplay manually or through suitable runtime scenes.
* Inspect the Git diff.
* Check for accidental unrelated changes.
* Check for missing resources and broken paths.
* Evaluate visual and gameplay quality honestly.
* Report incomplete areas explicitly.

---

## Validation

Validation should be appropriate to the work performed.

Possible checks include:

* C# build
* Godot project launch
* scene loading
* resource loading
* player spawning
* entrances and exits
* collision
* navigation
* zombie traversal
* combat
* camera movement
* interaction prompts
* inventory behaviour
* persistence
* save/load
* animation transitions
* UI behaviour
* lighting
* material appearance
* performance
* visual comparison
* traversal through edited environments

Do not create fake validation that only checks for node names or expected metadata while ignoring whether the feature actually works.

Automated validation should complement runtime inspection.

Report failures and uncertainties honestly.

---

## Performance

Performance remains important but is not an absolute barrier to visual progress.

Optimise intelligently using techniques such as:

* visibility ranges
* LOD
* occlusion
* batching
* reduced overdraw
* reasonable shadow settings
* collision simplification
* navigation optimisation
* texture optimisation
* mesh optimisation
* object pooling
* controlled update frequency
* world streaming
* disabling distant processing

Do not remove major visual or gameplay features solely to maximise benchmark numbers unless performance is genuinely unacceptable.

Consider average frame rate, frame pacing, one-percent lows, loading behaviour and movement through the world.

Do not optimise only a stationary benchmark scene.

---

## Scope Completion

A task is complete when the requested outcome has been meaningfully addressed, not merely when:

* the project builds
* one example exists
* a validation scene passes
* documentation has been updated
* a benchmark has been recorded
* a commit has been created

For broad requests, completion requires broad work.

When full completion is not realistic within the available execution context:

1. Complete the highest-value portions.
2. Leave the project in a stable state.
3. Clearly list completed work.
4. Clearly list unfinished work.
5. Explain the practical limiting factors.
6. Do not disguise partial completion as full completion.

---

## Reporting

Final reports should include:

* what was inspected
* what was changed
* files changed
* files created
* systems affected
* build result
* validation performed
* visual and gameplay assessment
* known limitations
* unfinished requested work
* exact testing steps
* editor setup if required

For dimension-sensitive world work, also include relevant original and new measurements.

For performance-sensitive work, include the test conditions and meaningful results.

Keep the report readable, but include enough detail for the user to understand exactly what happened.

Do not report a commit unless one was actually created.

Do not create or push a commit unless requested.

---

## Prohibited Behaviour

Do not:

* silently reduce a project-wide request to one tiny slice
* stop after one arbitrary example when multiple items were requested
* allow outdated vertical-slice instructions to override the current request
* claim AAA quality based only on compilation or automated validation
* claim visual success without inspecting the rendered result
* spend most of the task on tests while barely changing the game
* destroy unrelated user work
* reset the repository
* push without permission
* commit without permission
* download assets without permission
* fabricate validation results
* hide incomplete work
* repeatedly analyse the same issue without implementing improvements
* preserve clearly inadequate temporary implementations without justification
* use performance concerns as a blanket excuse to avoid requested development
* interpret “autonomous” as permission to ignore the requested goal

---

## Default Decision Principle

When choosing between:

* narrowly satisfying an outdated task boundary, or
* meaningfully advancing the user’s current explicit goal

advance the current goal.

When choosing between:

* stopping after the first functional implementation, or
* improving a visibly weak implementation within the requested scope

continue improving it.

When choosing between:

* claiming completion, or
* honestly reporting partial progress

report partial progress honestly.

The purpose of this file is to help develop Ashwood County into a complete, high-quality game, not to prevent ambitious development.
