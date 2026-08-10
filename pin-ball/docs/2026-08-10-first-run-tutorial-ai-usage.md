# First-run tutorial AI usage record

- AI tool/model: OpenAI Codex, GPT-5
- User request: Add a Slotbound-style first-run tutorial that stays under two minutes, pauses for forced UI actions, explains goal-specific unit summons, forces one merge, purchases a personal potion, opens the item tab, and mentions automatic class advancement at level 5.
- AI proposal: Use a small event-driven tutorial state machine over the existing Game scene flow, with a skip button, a 120-second timeout, and a one-time PlayerPrefs completion flag.
- AI changes: Added tutorial progress/UI control, minimal gameplay/UI events, tutorial launch gold, deterministic second summon, personal-potion purchase restriction, skip/completion persistence, and static verification hooks.
- User decisions: First-run only; skip is allowed; short guidance; first merge announces level-5 automatic advancement; shop must purchase one personal potion before opening the item tab.
- Important instruction: Implement immediately without a separate design approval and perform static checks only.
- Verification: Unity Play Mode, EditMode tests, and build were intentionally not run. Static reference review and `git diff --check` were performed.
- Remaining direct check: Confirm the temporary runtime overlay layout and click focus in the Game scene, then replace it with scene-preplaced UI after the visual design is finalized.
