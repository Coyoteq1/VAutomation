# ICB.core Configuration Guide

**Version:** 1.0.0-alpha  
**Last Updated:** 2025-10-17 01:37 UTC+03:00

---

## 📁 **CONFIGURATION FILE LOCATIONS:**

### **1. Source Configuration (Development):**
**Location:** `ICB.core/Config/arena_config.json`
- **Purpose:** Template/reference configuration with all available options
- **Contains:** Advanced features, loadout data, extracted GUIDs
- **Used For:** Development reference and documentation

### **2. Runtime Configuration (Production):**
**Location:** `BepInEx/config/ICB.core/arena_config.json`
- **Purpose:** Active configuration used by the server
- **Contains:** Simplified settings for current deployment
- **Used For:** Actual arena system behavior

### **3. V Blood Configuration:**
**Location:** `BepInEx/config/ICB.core/vblood_bosses.json`
- **Purpose:** V Blood boss GUIDs for progression tracking
- **Status:** ⏳ Not yet implemented (V Blood system pending)

---

## 🎮 **CURRENT RUNTIME CONFIGURATION:**

### **Active Settings (BepInEx/config/ICB.core/arena_config.json):**

(See `arena_config.json` in this folder for the full runtime example.)

---

## ⚙️ **CONFIGURATION OPTIONS:**

### **Global Settings:**

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `ArenaEnabled` | bool | true | Enable/disable arena system |
| `MaxPlayers` | int | 20 | Maximum players allowed in arena |
| `NoDurabilityLoss` | bool | false | Disable equipment durability loss |
| `NoBloodLoss` | bool | false | Disable blood pool drain |
| `BlockHealingPotions` | bool | false | Block healing potion usage |
| `BlockOPAbilities` | bool | false | Block overpowered abilities |

### **Portal Settings:**

| Setting | Type | Description |
|---------|------|-------------|
| `PortalX` | float | Portal X coordinate |
| `PortalY` | float | Portal Y coordinate |
| `PortalZ` | float | Portal Z coordinate |
| `PortalRadius` | float | Portal activation radius |

---

## 🗺️ **ARENA ZONES:**

### **Zone Configuration:**

```json
{
  "Name": "zone_name",
  "Description": "Zone description",
  "SpawnX": -1000.0,
  "SpawnY": 0.0,
  "SpawnZ": -500.0,
  "Radius": 200.0,
  "Enabled": true
}
```

### **How to Add a New Zone:**

1. Get coordinates in-game (use `.whereami` or similar command)
2. Add new zone object to `Zones` array
3. Save file and restart server

---

## ⚔️ **WEAPONS:**

### **Weapon Configuration:**

```json
{
  "Name": "weapon_id",
  "Description": "Weapon Name",
  "Guid": 1234567890,
  "Enabled": true,
  "Variants": []
}
```

(See `arena_config.json` for available weapons and GUIDs.)

---

## 🛡️ **ARMOR SETS:**

See `arena_config.json` for armor set GUIDs and examples.

---

## 🎒 **LOADOUTS:**

Loadouts are defined in `arena_config.json`. The system is configured but the automatic application of loadouts on arena entry is currently not implemented in the plugin.

---

## 🏗️ **BUILDS:**

Builds and weapon mod codes are defined in `arena_config.json`. See the file for examples.

---

## 🩸 **BLOOD TYPES:**

Blood types are listed in `arena_config.json` and mapped by GUID.

---

## 🔧 **ADVANCED FEATURES (Source Config Only):**

The source configuration (`ICB.core/Config/arena_config.json`) contains advanced features not yet implemented such as full LoadoutData and ExtractedGuids.

---

## 📝 **HOW TO MODIFY CONFIGURATION:**

### **1. Edit Runtime Config:**

```powershell
# Navigate to config directory
cd "C:\Program Files (x86)\Steam\steamapps\common\VRising\VRising_Server\BepInEx\config\ICB.core"

# Edit arena_config.json
notepad arena_config.json
```

### **2. Make Changes:**
- Modify settings as needed
- Add/remove zones
- Enable/disable weapons/armor
- Adjust portal location

### **3. Save and Restart:**
- Save the file
- Restart V Rising server
- Changes will take effect on next load

---

## 🚀 **DEPLOYMENT:**

### **Manual Deployment:**
```powershell
# Copy source config to runtime location
Copy-Item "ICB.core/Config/arena_config.json" -Destination "BepInEx/config/ICB.core/" -Force
```

---

## 🎯 **NEXT STEPS:**
1. Add this runtime `arena_config.json` to `BepInEx/config/ICB.core/` on your server
2. Restart server and confirm plugin loads without JSON errors

---

For details and full examples, open the `arena_config.json` file in this folder.
