# NecroDefense – Gameplay (คู่มือโครงสร้าง)

## เริ่มใช้งาน (ทำครั้งเดียว)
1. ลบของเก่าที่ไม่ใช้แล้วในหน้าต่าง Project (มี git สำรองไว้แล้ว):
   - `Assets/Scripts/` (ทั้งโฟลเดอร์)
   - `Assets/Prefabs/` (ทั้งโฟลเดอร์ รวม Slowball.cs)
   - `Assets/Scenes/SampleScene.unity`, `Assets/Scenes/1.unity`
2. รอ Unity คอมไพล์เสร็จ → เมนูบน **Tools > NecroDefense > Build Gameplay Scene**
   - **Rebuild All** = สร้างซีน + Prefab ใหม่ทั้งหมด (ดึงรูปจาก `Assets/picture` ใหม่, ค่าใน Prefab กลับเป็นค่าเริ่มต้น)
   - **Scene Only** = สร้างแค่ซีน ใช้ Prefab เดิม
3. เปิด `Assets/Scenes/Gameplay.unity` แล้วกด Play

> หลังสร้างแล้ว **ทุกอย่างปรับใน Hierarchy / Inspector / Prefab ได้เลย ไม่ต้องแก้โค้ด**

### รูปตัวละคร
ตัวสร้างจะดึงรูปจาก `Assets/picture/<ชื่อ>.pt.png` ให้อัตโนมัติ (ถ้ารูปเป็น Sprite Mode = Multiple จะถูกเปลี่ยนเป็น Single)
`Player_Necromancer`, `Enemy_slime`, `Enemy_Goblin`, `Enemy_SkeletonArcher`, `Enemy_Vulture`, `Enemy_giant`,
`Minion_slime`, `Minion_Goblin`, `Minion_SkeletonArcher`, `Minion_Vulture`, `Minion_Giant`
เปลี่ยนรูปทีหลัง = ลากรูปใหม่ใส่ child `Body` ใน Prefab ได้เลย (ไอคอนใน UI ใช้รูปเดียวกันอัตโนมัติ)

---

## Hierarchy ของซีน Gameplay
```
GameManager                    ← ตัวหลัก (ลาก Reference ทั้งหมดอยู่ที่นี่)
 ├ EconomyManager             ← Souls เริ่มต้น / Mana สูงสุด / Mana Regen
 ├ WaveManager                ← 12 เวฟ, เวลาพัก, ทางเกิด (Path West/East/South)
 ├ MinionManager              ← Minion Bag, ปุ่มลัด 1-5, คำสั่งเริ่มต้น, จำนวนสูงสุด
 ├ TowerManager               ← รายการป้อม, % เงินคืน, RangeIndicator
 ├ LevelUpManager             ← XP curve, เลเวลสูงสุด 30, Card Pool
 └ UIManager                  ← อ้างอิงทุกแผง UI
Main Camera                    ← ตามผู้เล่น, ซูมเริ่มต้น = Orthographic Size 7, CameraFollow > Min/Max Size = ขอบเขตซูม
Map                            ← MapBounds (กรอบเหลือง)
 ├ Ground                     ← พื้นเทา + Free Zone (ลานขาวกลาง) + EnemyLane_West/East/South
 ├ MainBase                   ← ป้อมหลัก (ด้านบนกลาง) HP / ฮีลทุก 30 วิ / ระยะลากสังเวย
 ├ TowerSlots/TowerSlot_1..6  ← ป้อมรอง 6 จุดตามภาพ (ย้าย/ก๊อป/ลบได้)
 ├ EnemyPaths/Path_West|East|South/WP_1..  ← ทางมอน: ย้ายจุด WP ได้ (เส้นแดงใน Scene View)
 ├ RangeIndicator             ← วงระยะยิงตอนเลือกป้อม
 └ Decorations                ← ป่า/หิน (Sprite ล้วน)
Player_Necromancer             ← PlayerNecromancer, PlayerAutoAttack, PlayerSkills
Runtime (ของที่เกิดตอนเล่น)
Canvas_Gameplay
 ├ TowerPanel (Ring Menu)     ← เมนูวงกลมรอบแท่น: BuildGroup (3 ป้อม) / ManageGroup (Upgrade, Sell) / InfoBox
 ├ MinionCommandPanel (Ring Menu) ← เมนูวงกลมรอบมินเนียน: Guard / Self-Defense / Follow
 ├ HUD
 │  ├ TopLeft_PlayerStatus     (HP / Mana / XP / Level)
 │  ├ Help_Button              (ชี้เพื่อดูปุ่มควบคุม)
 │  ├ TopCenter_Wave
 │  ├ TopRight_Resources       (Souls, HP ฐาน, เวลาฮีล)
 │  ├ BottomLeft_MinionBag     (Bag_Button + Bag_Panel ที่เปิด/ปิดด้วย Tab)
 │  ├ BottomRight_Skills       (Raise Dead / Teleport / Enigma Wave / Shockwave)
 │  └ TowerInfoBar             (โผล่ตอนเลือกป้อม)
 ├ LevelUpPanel / PausePanel / GameOverPanel
 ├ Message_Text / DragGhost
 └ Tooltip                    ← กล่องคำอธิบายตอนชี้ไอคอน (ข้อความแก้ได้ใน TooltipTrigger)
```
UI เน้นไอคอน ข้อความรายละเอียดอยู่ใน Tooltip (ชี้เมาส์ค้าง) ปรับตำแหน่ง/ขนาด/รูปได้ใน Inspector ตรงๆ

---

## อยากปรับอะไร → ไปที่ไหน
| อยากปรับ | ที่ |
|---|---|
| HP / ความเร็วผู้เล่น | `Player_Necromancer` > PlayerNecromancer |
| ยิงอัตโนมัติ, ลูกบอลหมุนรอบตัว | `Player_Necromancer` > PlayerAutoAttack |
| Teleport / Raise Dead / สกิล Q / E / R (คูลดาวน์, รัศมี, ดาเมจ, เวลาร่าย Cast Time) | `Player_Necromancer` > PlayerSkills |
| วิญญาณบินเข้ากระเป๋า (ความเร็ว/ความโค้ง) | `Canvas_Gameplay/SoulFly` > SoulFlyUI |
| การ์ดอัปสกิล Q | `Card_RaiseRadius` (+รัศมี), `Card_RaiseCooldown` (-คูลดาวน์), `Card_NecromancyMastery` (+โอกาส) |
| ซูมกล้อง | `Main Camera` > Camera (Size) และ CameraFollow (Min/Max) |
| Souls เริ่มต้น, Mana | `GameManager/EconomyManager` |
| ศัตรูแต่ละเวฟ / ออกทางไหน | `GameManager/WaveManager` > Waves (Side = West/East/South/Random/AllSides) |
| ทางเดินศัตรู | ย้าย `Map/EnemyPaths/Path_*/WP_*` |
| ค่าศัตรู | `Assets/Game/Prefabs/Enemies/Enemy_*.prefab` (Follow Path = เดินตามทางมอน) |
| ศัตรูเล็งอะไร | Prefab ศัตรู > **Targeting**: ติ๊ก Attack Player / Attack Minions / Attack Main Base + Priority (เป้าหลัก) |
| พฤติกรรมบนทางมอน | Prefab ศัตรู > **Lane Behaviour**: Only Minions On Lane (สนแค่มินเนียนจนกว่าจะเข้าพื้นที่อิสระ), Shoot While Walking (เดินไปยิงไป) |
| ขอบเขตพื้นที่อิสระ | `Map/FreeZone (Area)` > Size (กรอบฟ้าใน Scene View) |
| ป้ายเตือนเวฟ | `Map/EnemyPaths/Path_*/WaveWarning` (ในฉาก) และ `Canvas_Gameplay/WaveWarnings` (ไอคอนบนจอ) |
| ค่ามินเนียน / รูปไอคอน | `Assets/Game/Prefabs/Minions/Minion_*.prefab` |
| ระยะมองหาศัตรูของมินเนียน | Prefab มินเนียน > AI Ranges > **Aggro Range** (วงฟ้าใน Scene View) และ Chase Leash |
| ราคา/ดาเมจป้อมแต่ละ Tier, เวลาสร้าง | `Assets/Game/Prefabs/Towers/Tower_*.prefab` |
| ตำแหน่งไอคอนในเมนูวงกลม | `Canvas_Gameplay/TowerPanel (Ring Menu)/BuildGroup` และ `ManageGroup` |
| HP ฐาน, ฮีลอัตโนมัติ | `Map/MainBase` |
| การ์ดเลเวลอัป | `Assets/Game/Data/UpgradeCards/Card_*.asset` |
| เปิดกระเป๋าตอนเริ่มเกม / ปุ่มเปิด | `Canvas_Gameplay/HUD/BottomLeft_MinionBag` > MinionBagUI |

---

## ระบบกองทัพ / ความยาก (v2)
| อยากปรับ | ที่ |
|---|---|
| จำนวนกองทัพสูงสุดต่อชนิด (Slime 10, Goblin 6, Skeleton 3, Vulture 3, Giant 1) | `GameManager/MinionManager` > Bag > **Max Owned** |
| การ์ดเพิ่มกองทัพ +1 ทุกชนิด | `Assets/Game/Data/UpgradeCards/Card_Legion` |
| มินเนียนตาย | วิญญาณบินกลับเข้ากระเป๋าอัตโนมัติ (เรียกใหม่เสีย Mana) |
| ศัตรูแข็งขึ้นทุกเวฟ | `GameManager/WaveManager` > Difficulty Scaling (Health/Damage Growth Per Wave = 0.12 คือ +12%/เวฟ) |
| ศพหายภายใน / หน้าตาศพ | `Assets/Game/Prefabs/Pickups/Corpse.prefab` > Lifetime (10), Corpse Tint, Fallen Angle |
| สกิล E Arcane Nova | `Player_Necromancer` > PlayerSkills > Skill E (Cooldown, Cast Time, Radius, Damage) / การ์ด `Card_NovaBoost` |

## ซีนเทส (Test Arena)
สร้างด้วย **Tools > NecroDefense > Build Test Arena Scene** -> `Assets/Scenes/TestArena.unity`
(ไม่แตะซีน Gameplay / Prefab / การ์ด - สร้างใหม่ได้เรื่อยๆ)
- เลนเดียว: ศัตรูเดินจากซ้าย -> พื้นที่อิสระ -> ป้อมหลัก, แท่นป้อม 4 จุด, ไม่มีเวฟอัตโนมัติ
- แผงเทส (กด **F1** หรือปุ่ม TEST): ตั้ง Souls / Max Mana / Mana ไม่จำกัด, God Mode (ผู้เล่น / ป้อมหลัก), Heal All,
  ความเร็วเกม 0.5x–4x, เสกศัตรูชนิดใดก็ได้ จำนวนเท่าไหร่ก็ได้ (ที่ต้นเลน / พื้นที่อิสระ / ใกล้ตัว) พร้อมเลือกความแข็งเท่าเวฟที่ N,
  ฆ่าศัตรูทั้งหมด, ล้างศพ, เพิ่มมินเนียนเข้ากระเป๋า (+1 / +5 / เต็มเพดาน), Level Up, +XP, ให้การ์ดใบไหนก็ได้ทันที, รีเซ็ตคูลดาวน์
- ค่าตอนเริ่มกด Play: เลือก `Canvas_Gameplay/TestPanel (F1)` > DebugTestPanel > Start Settings

## ปุ่มควบคุม
| ปุ่ม | ผล |
|---|---|
| WASD / ลูกศร | เดิน |
| Q | Mass Raise (จงตื่น): ชุบทุกศพรอบตัว แต่ละศพสุ่มโอกาสแยกกัน วิญญาณบินเข้ากระเป๋า (คูลดาวน์ 10 วิ) |
| คลิกซ้ายที่ซากศพ | Raise Dead ทีละศพ |
| คลิกซ้ายที่แท่น/ป้อม | เมนูวงกลม → กดไอคอน 1 ครั้งดูข้อมูล+ระยะ, กดซ้ำ (✓) เพื่อยืนยัน |
| คลิกซ้ายที่มินเนียน | เมนูวงกลม 3 คำสั่ง: Hold Area (ปกป้องบริเวณนั้น, ค่าเริ่มต้น) / Move (ย้ายไปจุดที่คลิกแล้วปกป้องตรงนั้น) / Follow Player |
| Move → คลิกซ้ายในสนาม | มินเนียนเดินไปจุดนั้น (ไม่สนศัตรูระหว่างทาง) แล้วยืนเฝ้าตรงนั้น (คลิกขวา/Esc ยกเลิก) |
| คลิกขวา | Teleport |
| Q / E | มีเวลาร่าย: ผู้เล่นยืนนิ่งจนหลอดร่าย (ม่วง ใต้ตัว) เต็มแล้วค่อยปล่อยสกิล |
| E | Arcane Nova: ยืนร่ายแล้ววงเวทระเบิดรอบตัว (AoE) คูลดาวน์ 8 วิ |
| คลิกไอคอนเตือนเวฟ (กะโหลกแดง) | เรียกเวฟถัดไปทันที |
| Tab หรือปุ่มกระเป๋า | เปิด/ปิด Minion Bag |
| 1-5 หรือคลิกช่อง → คลิกซ้ายในสนาม | อัญเชิญตรงจุดที่คลิก (คลิกขวา/Esc ยกเลิก) หรือลากช่องลงสนามก็ได้ |
| ลูกกลิ้งเมาส์ | ซูมเข้า/ออก |
| ลากช่องไปที่ป้อมหลัก | สังเวย (ฮีลฐาน) |
| Esc | Pause |

---

## โครงสร้างโค้ด (`Assets/Game/Scripts`)
- `Core/` Enums, Registry (รายชื่อยูนิตในฉาก), Unit (คลาสแม่ของทุกอย่างที่มีเลือด)
- `Units/` CombatUnit → EnemyUnit, MinionUnit
- `Player/` PlayerNecromancer, PlayerAutoAttack, PlayerSkills
- `World/` Crystal (ป้อมหลัก), EnemyPath, RangeIndicator, MapBounds, CameraFollow, WorldHealthBar
- `Towers/` Tower, TowerSlot
- `Effects/` Projectile, DamageZone (บ่อพิษ), RingEffect, BeamEffect
- `Pickups/` Corpse, XpOrb
- `Managers/` GameManager + Manager ย่อยทั้งหมด
- `Data/` UpgradeCard (ScriptableObject)
- `UI/` สคริปต์ของแต่ละแผง UI (TowerPanelUI / CommandPanelUI = เมนูวงกลม, RingOptionUI = ปุ่มกลม, TooltipUI)
- `Assets/Game/Editor/` ตัวสร้างซีน (ใช้ครั้งเดียว จะลบทิ้งหลังสร้างเสร็จก็ได้)
