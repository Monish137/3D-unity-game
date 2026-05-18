# Assets/scripts Script Usage Overview

This document summarizes the purpose, attachment method, key fields, main methods, and dependencies of all current C# scripts in the `./3Dunity game/Assets/scripts` directory, to facilitate future maintenance and scene configuration.

---

## 1. minimap.cs / `Minimap`

### Purpose
Used to make a minimap camera or other following object continuously follow a target while maintaining a fixed top-down angle.

### Attachment
- Attach to the minimap camera.
- In the Inspector, point `target` to the player or the object to follow.

### Key Fields
- `target`: The target to follow.
- `followSpeed`: Smoothing speed for following.
- `heightOffset`: Height offset.
- `worldOffset`: World space offset.
- `fixedRotation`: Fixed rotation angle.

### Usage
- Suitable for top-down minimaps.
- If the follow target is null, the minimap will not update its position.

### Dependencies
- No direct script dependencies.
- `ChapterOneAutoSetup` will attempt to automatically set its target.

---

## 2. MissionCollectible.cs / `MissionCollectible`

### Purpose
Mission collectible logic. When the player touches the object, it advances progress for a specified mission and may optionally destroy itself.

### Attachment
- Attach to collectible objects.
- The object needs a Collider, usually set as a Trigger.

### Key Fields
- `missionManager`: Reference to the mission manager.
- `missionId`: The mission ID to advance.
- `amount`: Amount of progress to add each time.
- `destroyOnCollect`: Whether to destroy the object upon collection.

### Usage
- Only takes effect when the current mission ID matches `missionId`.
- Suitable for simple collection-type mission objectives like wood, weapons, letters, quest items, etc.

### Dependencies
- `MissionManager`

---

## 3. MissionInteractable.cs / `MissionInteractable`

### Purpose
Mission interaction point logic. When the player enters the range, they can press a key to complete the interaction and advance the mission, or configure it to trigger automatically.

### Attachment
- Attach to interactive objects or interactive zones.
- Requires a Collider, usually set as a Trigger.

### Key Fields
- `missionManager`: Mission manager.
- `missionId`: Corresponding mission ID.
- `amount`: Advancement value.
- `interactionKey`: Interaction key, default usually E.
- `requireInteractionKey`: Whether the key press is required.
- `triggerOnce`: Whether to trigger only once.
- `hideAfterInteract`: Whether to hide the object after interaction.

### Usage
- Suitable for bulletin boards, quest turn-in points, mechanisms, interactive NPC markers, etc.
- If you want the player to trigger simply by walking in, turn off `requireInteractionKey`.

### Dependencies
- `MissionManager`

---

## 4. MissionTrigger.cs / `MissionTrigger`

### Purpose
Pure area-triggered mission logic. Automatically advances the mission when the player enters a specified area.

### Attachment
- Attach to an area trigger.
- Requires a Trigger Collider.

### Key Fields
- `missionManager`: Mission manager.
- `missionId`: Corresponding mission ID.
- `progressAmount`: Amount of progress each time.
- `triggerOnce`: Whether to trigger only once.

### Usage
- Suitable for missions like "reach a certain location", "enter a certain area", "approach a building", etc.

### Dependencies
- `MissionManager`

---

## 5. AnimalHealth.cs / `AnimalHealth`

### Purpose
Animal health management script, responsible for:
- Taking damage
- Death handling
- Dropping meat
- Displaying a simple world-space health bar
- Disabling animal behavior and delayed destruction

### Attachment
- Attach to the animal root object.
- It is recommended that the animal model, Animator, and movement scripts be attached to the same object or its child structure.

### Key Fields
- `animalName`: Animal name.
- `animalLevel`: Level.
- `maxHealth`: Maximum health.
- `meatDropCount`: Number of meat drops.
- `destroyDelay`: Delay before destruction after death.
- `meatSpawnOffset`: Offset for drop spawning.
- Health bar related fields: control health bar display.

### Main Methods
- `TakeDamage(int damage, GameObject attacker)`: Called when taking damage.

### Usage
- When hit by `PlayerSkills` melee attacks, `TakeDamage` is called.
- Upon death, it spawns a drop with `MeatPickup`.
- It attempts to disable animal behavior scripts such as `BearMovement`.

### Dependencies
- `MeatPickup`
- `BearMovement`
- Unity `Animator`

---

## 6. BearMovement.cs / `BearMovement`

### Purpose
Simple bear movement logic that makes the bear automatically move forward, and also supports external control of speed and direction.

### Attachment
- Attach to the bear object.

### Key Fields
- `moveSpeed`: Movement speed.
- `autoMove`: Whether to move automatically.

### Main Methods
- `SetMoveSpeed(float)`: Modify speed.
- `SetAutoMove(bool)`: Toggle auto-move.
- `MoveInDirection(Vector3)`: Move in a specified direction.

### Usage
- Currently a very lightweight movement script, not a full AI.
- Mainly used for basic movement of hunting targets.

### Dependencies
- Optional dependency on `Animator`
- Disabled by `AnimalHealth` on death

---

## 7. ChapterOneAutoSetup.cs / `ChapterOneAutoSetup`

### Purpose
Automatic setup script for Chapter One content. At runtime, it adds to the scene:
- Player-related components
- Mission points
- Collectibles
- Hunting targets
- Mission NPCs / interaction points
- Minimap follow target, etc.

### Attachment
- Attach to a manager object in the scene.
- Suitable for quickly initializing a demo / level prototype.

### Key Fields
- `missionManager`: Mission manager.
- `player`: Player object.
- `merlinDialogue`: Merlin dialogue object.
- `gameplayCamera`: Main camera.
- Multiple position fields: control spawn positions for NPCs, weapons, prey, villagers arming points, etc.

### Usage
- Suitable for one‑click configuration of the Chapter One flow.
- It automatically adds or configures many other scripts.
- It assigns values to some private fields via reflection, so be careful to keep field names in sync when renaming.

### Dependencies
- `MissionManager`
- `MissionTrigger`
- `MissionCollectible`
- `MissionInteractable`
- `SwordPickup`
- `NpcDialogue`
- `PlayerSkills`
- `PlayerInventory`
- `AnimalHealth`
- `BearMovement`
- `Minimap`

---

## 8. DialogueManager.cs / `DialogueManager`

### Purpose
Global dialogue manager, responsible for:
- Opening/closing the dialogue UI
- Displaying speaker name and content
- Advancing dialogue with a key
- Calling back to NPC after dialogue ends

### Attachment
- Only one instance should exist in the scene.
- If none exists in the scene, it can automatically create a runtime instance via `EnsureInstance()`.

### Key Fields
- `dialogueRoot`: Dialogue UI root object.
- `speakerNameText`: Speaker name text.
- `dialogueContentText`: Dialogue content text.
- `continueHintText`: Continue hint text.
- `advanceKey`: Key to advance dialogue.
- `continueHint`: Hint message.

### Main Methods
- `EnsureInstance()`: Ensure singleton exists.
- `StartDialogue(...)`: Start a dialogue.
- `AdvanceDialogue()`: Advance to the next line.
- `EndDialogue()`: End the dialogue.
- `IsDialogueOpen`: Whether dialogue is currently open.

### Usage
- Typically initiated by `NpcDialogue`.
- If UI is not manually configured, a fallback UI is automatically created.

### Dependencies
- `NpcDialogue`
- Unity UI

---

## 9. MeatPickup.cs / `MeatPickup`

### Purpose
Meat drop pickup logic:
- Picked up by the player
- Advances hunting‑type missions
- Adds meat items to the player’s inventory

### Attachment
- Attach to meat drop objects.
- Usually created dynamically by `AnimalHealth` on death, which also attaches this script.

### Key Fields
- `playerTag`: Player tag.
- `missionId`: Mission ID.
- `missionProgressAmount`: Amount to advance the mission.
- `destroyOnPickup`: Whether to destroy on pickup.
- `meatLabel`: Display name.
- `inventoryItemId`: Item ID in the inventory.

### Main Methods
- `SetMeatLabel(string)`: Dynamically set the drop name.

### Usage
- When the player touches it, it attempts to add an item to `PlayerInventory`.
- If the current mission matches, it also advances progress in `MissionManager`.

### Dependencies
- `MissionManager`
- `PlayerInventory`

---

## 10. ObjectiveTracker.cs / `ObjectiveTracker`

### Purpose
Mission tracking UI, responsible for:
- Displaying the current mission text
- Showing the direction of the current mission objective on screen
- Estimating the distance to the objective

### Attachment
- Attach to a persistent object in the scene.
- If none exists in the scene, `MissionManager` will automatically create one.

### Key Fields
- `objectiveTextAnchor`: Text anchor.
- `markerClampPadding`: Screen edge padding.
- `worldOffset`: Target display offset.
- `markerColor`: Indicator color.

### Usage
- Automatically finds the target object based on the current mission.
- Internally scans scripts like `NpcDialogue`, `MissionInteractable`, `MissionCollectible`, `MissionTrigger` and matches their mission IDs.

### Dependencies
- `MissionManager`
- `NpcDialogue`
- `MissionInteractable`
- `MissionCollectible`
- `MissionTrigger`

---

## 11. PlayerInventory.cs / `PlayerInventory`

### Purpose
Player inventory and item interaction system, responsible for:
- Categorised storage of weapons/food/materials
- Auto‑generation of inventory UI
- Opening/closing the inventory
- Dragging and swapping items
- Using food
- Equipping a sword from the inventory
- Displaying hint text

The same file also contains `InventorySlotUI` for individual slot UI and interaction.

### Attachment
- Attach to the player object.

### Key Fields
- `toggleInventoryKey`: Key to open/close inventory, default `B`.
- `equipSwordKey`: Shortcut to equip weapon, default `1`.
- `useFoodKey`: Shortcut to use food, default `2`.
- `weaponSlotCount` / `foodSlotCount` / `materialSlotCount`: Number of slots for each category.
- `defaultFoodItemId`: Default food ID.
- `foodHealAmount`: Amount healed by food.

### Main Methods
- `AddItem(...)`: Add an item.
- `HasItem(string)`: Check if an item exists.
- `RemoveItem(...)`: Remove an item.
- `EquipSwordFromInventory()`: Equip sword from inventory.
- `ConsumeFood()`: Consume food.
- Drag‑related: `BeginDrag`, `UpdateDrag`, `EndDrag`, `HandleDrop`, `HandleSlotClick`.

### Usage
- `SwordPickup`, `MeatPickup`, etc., call `AddItem`.
- Equipping a sword interacts with `PlayerSkills`.
- Eating food triggers health restoration.

### Dependencies
- `PlayerSkills`
- `SUPERCharacterAIO`
- Unity UI / EventSystem

---

## 12. PlayerVisualAttachment.cs / `PlayerVisualAttachment`

### Purpose
Attaches a model or visual object under the current object and uniformly applies local position, rotation, and scale.

### Attachment
- Attach to the player root object or a character display object.

### Key Fields
- `visualRoot`: The visual root object to attach.
- `localPosition`: Local position.
- `localRotation`: Local rotation.
- `localScale`: Local scale.
- `oldVisualToDisable`: Old model (optional).

### Usage
- Commonly used for character skinning, model replacement, attaching a display model under a controller object.
- If an old model exists, it can be hidden when attaching the new one.

### Dependencies
- No direct script dependencies.

---

## 13. SwordPickup.cs / `SwordPickup`

### Purpose
Sword pickup logic. When the player touches it:
- Adds the sword to the inventory
- Optionally equips it immediately based on configuration
- Destroys the pickup object in the scene

### Attachment
- Attach to a sword pickup object in the scene.
- The object needs a Collider, usually set as a Trigger.

### Key Fields
- `playerTag`: Player tag.
- `destroyOnPickup`: Whether to destroy on pickup.
- `equipImmediatelyOnPickup`: Whether to equip immediately on pickup.

### Usage
- If you want the player to pick up the sword first and then equip it manually with a shortcut, turn off `equipImmediatelyOnPickup`.
- If you want it to be equipped as soon as it’s picked up, turn it on.

### Dependencies
- `PlayerSkills`
- `PlayerInventory`

---

## 14. NpcDialogue.cs / `NpcDialogue`

### Purpose
NPC dialogue trigger script, responsible for:
- Showing a hint when the player approaches
- Starting a dialogue on key press
- Advancing the mission after dialogue ends
- Determining whether dialogue is allowed based on mission progress

### Attachment
- Attach to the NPC or its interaction trigger zone.
- Requires a Collider, usually set as a Trigger.

### Key Fields
#### Dialogue part
- `dialogueLines`: Array of dialogue lines.
- `interactionKey`: Interaction key, default E.
- `oneTimeOnly`: Whether dialogue can only be triggered once.

#### Hint part
- `promptRoot`: Custom hint UI root.
- `promptText`: Hint text.
- `promptMessage`: Default hint message.

#### Mission part
- `missionManager`: Mission manager.
- `missionId`: Mission to advance when dialogue ends.
- `completeMissionWhenDialogueEnds`: Whether to automatically add mission progress when dialogue ends.

#### Unlock condition part
- `gateMode`: Dialogue availability condition.
- `gateMissionId`: Mission ID used for checking.
- `lockedPromptMessage`: Hint message when locked.

### Main Methods
- `StartDialogue()`: Start dialogue.
- `IsUnlocked()`: Check if dialogue is currently allowed.
- `NotifyDialogueFinished()`: Called by `DialogueManager` when dialogue ends.

### Usage
- Suitable for main story NPCs, phased unlock NPCs, mission‑gated dialogues.
- If no custom hint UI is configured, a runtime hint box is automatically created.

### Dependencies
- `DialogueManager`
- `MissionManager`
- `SUPERCharacterAIO` (to identify the player)

---

## 15. MissionManager.cs / `MissionManager`

### Purpose
Core of the global mission system, responsible for:
- Holding the mission list
- Controlling the current mission flow
- Adding mission progress
- Completing missions and advancing to the next
- Displaying mission UI
- Ensuring the objective tracker exists

### Attachment
- Only one instance should exist in the scene.
- It is recommended to attach to a persistent manager object.

### Key Fields
#### Mission data
- `missions`: List of missions.
- `startingMissionIndex`: Index of the starting mission.

#### UI
- `missionTitleText`
- `missionDescriptionText`
- `missionProgressText`

#### Debug
- `skipMissionKey`: Debug key to skip current mission, default `P`.

### Main Methods
- `StartMission(int)`: Start a specific mission.
- `AdvanceToNextMission()`: Move to the next mission.
- `SkipCurrentMission()`: Debug skip.
- `CompleteCurrentMission()`: Complete the current mission.
- `IsMissionCompleted(string)`: Check if a mission is completed.
- `AddProgress(string, int)`: Add mission progress.
- `IsCurrentMission(string)`: Check if a given mission ID is the current mission.
- `GetMissionIndex(string)`: Get index by mission ID.
- `GetMissionByIndex(int)`: Get mission by index.
- `LoadChapterOneMissions()`: Quickly load the Chapter One mission chain.

### Usage
- The central hub for all mission‑related scripts.
- In the current project, after the `talk_to_merlin` mission is completed, it automatically unlocks the phantom skill in `PlayerSkills`.
- If no UI is manually assigned in the Inspector, a fallback mission panel is automatically created.

### Dependencies
- `ObjectiveTracker`
- `PlayerSkills`
- Unity UI

---

## 16. PhantomSkill.cs / `PhantomSkill`

### Purpose
Phantom skill entity logic, responsible for:
- Spawning a skill visual body
- Delayed area‑of‑effect damage
- Attaching a sword to the phantom
- Playing an attack animation
- Fading out and destroying over time

### Attachment
- Does not need to be manually attached to a scene object.
- Usually created dynamically by `PlayerSkills.TryUseDefaultSkill()` which adds a `PhantomSkill` component to a temporary GameObject.

### Key Fields
This script primarily uses `Initialize(...)` to inject parameters; it does not rely on many Inspector configurations.

### Main Methods
- `Initialize(...)`: Initialises damage, range, visuals, weapon, lifetime, etc.

### Usage
- The phantom skill is not a pickup nor a persistent object; it is a temporary attack entity generated at runtime.
- If `visualPrefab` is null, a simplified fallback visual is generated.
- If `phantomSwordPrefab` is not null, it is automatically attached to the phantom’s hand bone.

### Dependencies
- `PlayerSkills`
- `AnimalHealth`
- `Animator`

---

## 17. PlayerSkills.cs / `PlayerSkills`

### Purpose
Core player combat and skill script, responsible for:
- Whether the player has a sword equipped
- Attack/defense input
- Health/mana management
- Mana regeneration
- Phantom skill casting
- Visual attachment of the sword
- Dealing melee damage to animals
- Broadcasting state change events to UI

### Attachment
- Attach to the player object.

### Key Fields
#### Sword and stats
- `hasSword`
- `baseAttackPower`
- `swordAttackPower`
- `baseDefense`
- `swordDefenseBonus`

#### Input
- `attackKey`
- `defenseKey`
- `skillKey`

#### Survival and mana
- `maxHealth`
- `startingHealth`
- `maxMana`
- `startingMana`
- `manaRegenPerSecond`

#### Animations and sword display
- `animator`
- `attackTriggerName`
- `defenseTriggerName`
- `equippedSwordPrefab`
- `swordHandBoneName`
- `swordHandBoneFallbackNames`
- `swordLocalPosition / Rotation / Scale`
- `swordFallbackLocalPosition / Rotation / Scale`

#### Attack
- `attackCamera`
- `attackOrigin`
- `attackRange`
- `attackRadius`
- `attackMask`
- `attackCooldown`
- `attackDamageDelay`

#### Phantom skill
- `phantomSkillUnlocked`
- `phantomVisualPrefab`
- `phantomSwordPrefab`
- `phantomVisualLayerName`
- `phantomSkillManaCost`
- `phantomSkillCooldown`
- `phantomSkillDamageMultiplier`
- `phantomSkillRangeMultiplier`
- `phantomSkillRadiusMultiplier`
- `phantomSkillDamageDelay`
- `phantomLifetime`
- `phantomSpawnOffset`
- `lowManaHintText`
- `lowManaHintCooldown`

### Main public properties / events
- `AttackPower`
- `DefensePower`
- `HasSword`
- `CurrentHealth` / `MaxHealth`
- `CurrentMana` / `MaxMana`
- `PhantomSkillUnlocked`
- `PhantomSkillCooldownRemaining`
- `PhantomSkillCooldownNormalized`
- `SwordEquipChanged`
- `HealthChanged`
- `ManaChanged`
- `PhantomSkillUnlockedChanged`
- `HintRequested`

### Main Methods
- `UnlockSword()`: Unlock the sword.
- `SetSwordEquipped(bool)`: Set whether the sword is equipped.
- `Attack()`: Perform a normal attack.
- `Defend()`: Perform a defense action.
- `TryUseDefaultSkill()`: Attempt to cast the phantom skill.
- `UnlockPhantomSkill()`: Unlock the phantom skill.
- `TakeDamage(int)`: Player takes damage.
- `RestoreHealth(int)`: Restore health.
- `ConsumeMana(int)`: Consume mana.
- `RestoreMana(int)`: Restore mana.
- `DealSlashDamage(...)`: Perform a cone‑based slashing damage check.

### Usage
- Currently, normal attacks and skills both rely on the “sword equipped” state.
- `SwordPickup` and `PlayerInventory` control its equipped state.
- `MissionManager` unlocks the phantom skill after a specific mission is completed.
- The sword model now uses an existing Prefab rather than being constructed in code.

### Dependencies
- `PhantomSkill`
- `AnimalHealth`
- `MissionManager`

---

## 18. PlayerStatusUI.cs / `PlayerStatusUI`

### Purpose
Player status HUD, responsible for:
- Displaying health and mana bars
- Displaying health and mana numeric values
- Displaying skill cooldown ring
- Showing low‑mana hints
- Shifting the HUD upward during dialogue to avoid obstruction

### Attachment
- Can be attached to any object in the scene or to the player object.
- Only one instance should exist in the scene.

### Key Fields
- `playerSkills`: Player skills script.
- `dialogueHudYOffset`: Amount to shift HUD upward during dialogue.
- `hudMoveSpeed`: Smooth movement speed of the HUD.

### Usage
- Automatically creates the full status UI.
- Health and mana bars now use `RectTransform` width changes, no longer rely on `fillAmount`.
- The skill cooldown icon still uses radial `fillAmount` to show cooldown overlay.
- Listens to `PlayerSkills` events for health, mana, and hints.

### Dependencies
- `PlayerSkills`
- `DialogueManager`
- Unity UI

---

## Overall Dependency Overview

### Player‑side core
- `PlayerSkills`: Combat, mana, skills
- `PlayerInventory`: Inventory, items, equipment entry point
- `PlayerStatusUI`: Status display
- `PlayerVisualAttachment`: Model attachment

### Mission‑side core
- `MissionManager`: Mission master control
- `ObjectiveTracker`: Mission tracking display
- `MissionTrigger` / `MissionCollectible` / `MissionInteractable`: Three types of mission progression nodes

### Dialogue‑side core
- `DialogueManager`: Global dialogue control
- `NpcDialogue`: NPC dialogue entry point

### Combat / drop‑side core
- `AnimalHealth`: Animal health and death drops
- `BearMovement`: Animal movement
- `MeatPickup`: Meat pickup
- `SwordPickup`: Sword pickup
- `PhantomSkill`: Phantom skill entity

### Level auto‑configuration
- `ChapterOneAutoSetup`: Automatically creates and wires many systems

---

## Typical Flow in the Current Project

### New Player Main Quest Flow
1. `MissionManager` loads and advances the main quest.
2. `MissionTrigger` / `NpcDialogue` / `MissionCollectible` / `MissionInteractable` handle progression at each stage.
3. `ObjectiveTracker` provides directional hints for the current mission.
4. The player obtains a sword via `SwordPickup` and equips it through `PlayerInventory` / `PlayerSkills`.
5. The player uses `PlayerSkills` to attack animals; `AnimalHealth` drops `MeatPickup`.
6. Collecting the prey advances the mission, then continues the dialogue.
7. After completing `talk_to_merlin`, `MissionManager` unlocks the phantom skill in `PlayerSkills`.
8. `PlayerStatusUI` displays health, mana, and skill cooldown throughout.

---

If you add more scripts in the future, it is recommended to follow the format of this document for unified maintenance.