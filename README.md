# V Automation

V Rising Automation mod for V Rising servers. Players get full VBlood access, curated loadouts, and automatic state restore when they leave the arena.

---

## Requirements

- V Rising Dedicated Server 1.0+
- BepInEx 6 (IL2CPP)
- VampireCommandFramework 0.10+

---

## Install

1. Build (or grab the release DLL)
   ```bash
   dotnet build -c Release
   ```
   Output: `bin/Release/net6.0/VAutomationEvents.dll`
2. Copy the DLL to `VRising_Server/BepInEx/plugins/`
3. Restart the server

---

## Configure (optional)

Edit `BepInEx/config/gg.Automation.arena.cfg` to tweak:

- Default loadout items (`InputItems`)
- Entry/exit behavior (`EnableGodMode`, `RestoreOnExit`, etc.)

Changes apply on the next server restart.

---

## Play

- Walk within 50 m of the arena to auto-enter.
- Leave the 75 m exit radius to restore your original gear and stats.

### Player Commands

- `.arena enter [player] arena name loadout]` – Enter arena (snapshots and equips optional loadout)
- `.arena exit [player] arena name` – Leave arena and restore your saved state
- `.arena status` / `.arena status` – Show location, arena status, spawn info
- `.arena tp [player] arena name <e|x>` – Teleport to arena entry (`e`) or exit (`x`) point
- `.arena build [player] arena name <1-4>` – Apply numbered build preset
- `.arena loadout [player] arena name <name>` – Apply named loadout or complete build
- `.arena listloadouts` – List all available loadouts/builds
- `.arena give <item> [amount]` – Spawn item(s) while in arena
- `.arena stats` – View snapshot counts and arena status
- `.arena swap new <name>` – Create fresh arena-only character shell

### Blood & Progression

- `.blood preset <type>` – Set arena blood (rogue, warrior, scholar, creature, mutant, dracula, corrupted)
- `.progression snapshot [player]` – Save progression snapshot (admin)
- `.progression restore [player]` – Restore a saved progression snapshot (admin)
- `.progression delete-snapshot [player]` – Delete specific snapshot (admin)
- `.progression clear-all` – Delete all progression snapshots (admin)
- `.progression has-snapshot [player]` – Check if snapshot exists

### Loadout Management

- `.loadouts` / `.loadouts custom` – List default or custom loadouts
- `.loadout <name>` – Apply loadout directly
- `.loadout save <name>` – Save current gear as custom loadout
- `.loadout delete <name>` – Remove custom loadout
- `.loadout info <name>` – Inspect loadout contents
- `.loadout reload` – Reload loadouts from disk (admin)
- `.loadout stats` – Show loadout database statistics

### Map & Portal Tools (Admin)

- `.portal start` / `.portal end` – Create arena portal pair
- `.mapicon create <prefab>` – Spawn a map icon at your position
- `.mapicon list` – List icon prefab names
- `.mapicon remove` – Remove nearby map icon

### Arena Setup & Config (Admin)

- `.arena reload` / `.arena save` – Reload or persist arena config
- `.arena clear` – Clear all arena snapshots
- `.arena add <category> <name> <guid>` – Add prefab to databases
- `.arena import <json>` / `.arena export` – Manage prefab data
- `.arena setzone <radius>` – Set arena zone radius
- `.arena setentry <radius>` – Set entry point
- `.arena setexit <radius>` – Set exit point
- `.arena setspawn` – Set arena spawn point

---

## Support

Encounter an issue? Check server logs in `BepInEx/LogOutput.log` and share reproducible steps when reporting.

- Join the community modding Discord: [discord.gg/99yzyAD8](https://discord.gg/99yzyAD8)

---

## Author

- Coyoteq1
