# 🧙‍♂️ Necromancer Defense: Tower Defense + Survivor-like
**GI351 Rapid Game Development — OOP Architecture Specification**

---

## 🎮 Concept & Core Gameplay
- **Genre:** Tower Defense + Survivor-like (Bullet-Heaven / Action Roguelite).
- **Art Style:** **2-Color Palette (1-Bit Monochrome ขาว-ดำ)** ได้แรงบันดาลใจจากสไตล์เรโทร 1-bit คอนทราสต์สูง (เช่น Minit, Downwell, World of Horror)
  - พื้นหลังสีดำสนิท (`#000000`)
  - ตัวละคร มอนสเตอร์ ฐาน กระสุน และ UI เส้นกรอบสีขาวบริสุทธิ์ (`#FFFFFF`)
  - แยกมอนสเตอร์ศัตรู (Evil White Silhouette + ตา/ปากดำ) ออกจากสมุนฝ่ายเรา (Allied Minions มี **Runic Halo / วงแหวนมงกุฎแสงสีขาวลอยเหนือหัว** และตราสัญลักษณ์ที่อก)
  - เอฟเฟกต์โดนดาเมจเป็น **Inversion Flash** (กะพริบสลับเป็นสีดำ)
- **Player Role:** **จอมเวท Necromancer** ผู้ครอบครองพลังวิญญาณ ควบคุมและเคลื่อนที่หลบหลีกคลื่นมอนสเตอร์ พร้อมร่ายเวท `Dark Bolt` โจมตีอัตโนมัติ.
- **Core Defense Objective:** ปกป้อง **Citadel Core (Soul Crystal)** ที่อยู่ใจกลางสมรภูมิ ไม่ให้ศัตรูทำลาย.
- **Unique Necromancy Mechanic:** 
  > *"ฆ่าตัวอะไรตายไป เราก็ชุบมันมาเป็นพวกช่วยเราป้องกันฐานได้ตามหลัก OOP"*
  - มอนสเตอร์ทุกตัวที่ถูกสังหารจะทิ้ง **ซากศพ (Corpse)** และมอบดวงวิญญาณ (`Soul Essence`).
  - ผู้เล่นสามารถกด `[Space]` หรือคลิกขวา เพื่อร่ายเวทชุบชีวิตซากศพรอบตัวให้กลายเป็น **มินเนียนพันธมิตร (Allied Minions)**.
  - มินเนียนจะแปลงสภาพตามประเภทของศัตรูที่ตาย และได้รับหน้าที่ในการป้องกันฐานหรือคุ้มกันผู้เล่น.

---

## 🏛️ OOP Principles Implementation (หลักการเชิงวัตถุอย่างละเอียด)

### 1. Abstraction (การกำหนดโครงสร้างนามธรรม & การแยกส่วนส่วนต่อประสาน)
- **Interfaces (`Assets/Scripts/Core/`):**
  - `IDamageable`: สัญญาสากลสำหรับสิ่งที่รับดาเมจได้ (`CurrentHealth`, `MaxHealth`, `TakeDamage()`, `Heal()`, `OnDied`). รองรับทั้ง Player, Base Citadel, ศัตรู และมินเนียน.
  - `ITargetable`: สัญญาสำหรับวัตถุที่เป็นเป้าหมายการโจมตีได้ (`Position`, `Faction Team`, `IsValidTarget`).
  - `IAttacker`: สัญญาสำหรับวัตถุที่มีความสามารถในการโจมตี (`AttackPower`, `AttackRange`, `Attack()`).
  - `IResurrectable`: สัญญาสำหรับซากศพหรือดวงวิญญาณที่สามารถชุบชีวิตได้ (`SourceType`, `SoulCost`, `CanBeResurrected`, `OnResurrected()`).
- **Abstract Classes:**
  - `Entity`: คลาสนามธรรมรากฐาน ครอบคลุมการจัดการ Transform, Life-cycle และ Identification.
  - `Unit`: คลาสนามธรรมของนักสู้ จัดการ Health, Physics Rigidbody2D, และลูปการต่อสู้.

### 2. Encapsulation (การห่อหุ้มและการซ่อนข้อมูล)
- ค่าสถานะทั้งหมด (`_currentHealth`, `_maxHealth`, `_moveSpeed`, `_attackPower`, `_currentSouls`) เป็น `private` หรือ `protected`.
- เปิดเผยข้อมูลสู่ภายนอกผ่าน Read-only Properties เช่น `public float CurrentHealth => _currentHealth;`.
- การแก้ไขสถานะทำได้ผ่าน Domain Methods ที่มี Validation เช่น `TakeDamage(DamageInfo)`, `Heal(float)`, `SpendSouls(int)`.
- ห่อหุ้มข้อมูลการโจมตีไว้ใน `readonly struct DamageInfo` ป้องกันข้อมูลถูกแทรกแซงระหว่างการส่งผ่าน.

### 3. Inheritance (การสืบทอดคุณสมบัติ)
```
MonoBehaviour
  └── Entity
        ├── BaseCrystal (ฐานปราการที่ต้องป้องกัน)
        ├── Corpse (ซากศพที่ตก implement IResurrectable)
        ├── Projectile (กระสุนโจมตี)
        │     └── HomingProjectile (กระสุนเวทมนตร์วิถีโค้งตามเป้า)
        └── Unit (Implement IDamageable, ITargetable, IAttacker)
              ├── PlayerNecromancer (จอมเวทผู้เล่น)
              ├── EnemyUnit (Abstract คลาสแม่ของศัตรู)
              │     ├── ZombieEnemy (สายประชิด รุมโจมตี)
              │     ├── SkeletonArcherEnemy (สายยิงไกล ลูกธนูกระดูก)
              │     ├── GolemEnemy (สายแทงค์ เลือดเยอะ ทุบพื้นกระจาย)
              │     └── GhostEnemy (สายความเร็ว ลอยทะลุ ดูดเลือด)
              └── MinionUnit (Abstract คลาสแม่ของสมุนที่ถูกชุบชีวิต)
                    ├── ZombieMinion (Undead Ghoul: วิ่งโจมตีเคลือบพิษวิญญาณ)
                    ├── SkeletonArcherMinion (Spectral Sniper: ยิงคุ้มกันระยะไกล)
                    ├── GolemMinion (Undead Titan: ปราการมีชีวิต บล็อกทางหน้าฐาน)
                    └── GhostMinion (Spectral Phantom: ออร่าความเย็นสโลว์ศัตรู)
```

### 4. Polymorphism (ภาวะหลายรูปแบบ)
- **Dynamic Polymorphism (Virtual & Abstract Method Overriding):**
  - `ExecuteAttack(ITargetable target)`:
    - `ZombieEnemy` / `ZombieMinion` -> โจมตีกรงเล็บระยะประชิดพร้อมพิษ Necrotic.
    - `SkeletonArcherEnemy` / `SkeletonArcherMinion` -> ยิง Projectile ลูกธนูระยะไกล.
    - `GolemEnemy` / `GolemMinion` -> Ground Slam สลายความเสียหายแบบ Area of Effect (AOE).
    - `GhostEnemy` / `GhostMinion` -> โจมตีดูดพลังชีวิต / ปล่อยออร่า Chilling Aura สโลว์รอบตัว.
  - `TakeDamage(DamageInfo info)`:
    - `GolemEnemy` / `GolemMinion` Override เพื่อหักลดดาเมจด้วยเกราะหนา (Flat Armor Reduction).
    - `GhostEnemy` Override เพื่อมีโอกาสก้าวผ่านมิติหลบการโจมตี (Phasing Evasion).
  - `Die()`:
    - ศัตรู Override เพื่อดรอปวัตถุ `Corpse` และให้ Souls แก่ผู้เล่น.
    - มินเนียน Override สลายร่างคืนสู่ผุยผง.
    - ผู้เล่น Override ส่งสัญญาณ Game Over ไปยัง GameManager.
- **Parametric / Factory Polymorphism:**
  - เมธอด `MinionFactory.CreateMinionFromCorpse(IResurrectable corpse)` รับ Interface นามธรรม และสร้าง Instance ของ Minion ที่สอดคล้องกับชนิดของศัตรูโดยอัตโนมัติ.

### 5. Design Patterns Applied
1. **Factory Pattern:** `MinionFactory` และ `EnemyFactory` ทำหน้าที่สร้าง Instance โดยแยกตรรกะการสร้าง (Object Creation) ออกจากตรรกะการเล่น (Gameplay Logic).
2. **Strategy Pattern:** `ITargetingStrategy` แยกขั้นตอนการหาเป้าหมายของ AI:
   - `TargetBasePriorityStrategy`: ศัตรูเน้นบุกตีฐานเป็นหลัก หรือเปลี่ยนเป้าหากผู้เล่นเข้าใกล้.
   - `TargetNearestHostileStrategy`: มินเนียนค้นหาศัตรูที่อยู่ใกล้ที่สุดเพื่อกำจัด.
   - `GuardBasePerimeterStrategy`: มินเนียนคุ้มกันแนวรัศมีรอบฐาน Citadel.
3. **Observer Pattern:** ใช้ C# `Action` Events (`OnHealthChanged`, `OnDied`, `OnSoulsChanged`, `OnWaveStarted`) เชื่อมต่อไปยัง UI HUD และ Game Manager โดยไม่ต้อง Coupling กันโดยตรง.
4. **Singleton Pattern:** `GameManager`, `MinionFactory`, `EnemyFactory` มี Instance ส่วนกลางสำหรับบริหารจัดการภาพรวม.

---

## 🕹️ Game Controls & Hotkeys
- **Movement:** `[W][A][S][D]` หรือ Arrow Keys
- **Auto-Attack:** โจมตีอัตโนมัติด้วยเวทมนตร์ `Dark Bolt` ล็อกเป้าหมายศัตรูที่ใกล้ที่สุด
- **Reanimate (ชุบชีวิต):** `[Spacebar]`, `[E]` หรือ `[คลิกขวา]` เพื่อชุบศพรอบตัวขึ้นมาเป็นมินเนียน
- **Minion Command Orders:**
  - `[1]`: Defend Citadel (สั่งมินเนียนทั้งหมดตั้งรับรอบฐาน)
  - `[2]`: Follow Mage (สั่งมินเนียนตามคุ้มกัน Necromancer)
  - `[3]`: Free Hunt (ปล่อยมินเนียนออกไล่ล่าอย่างอิสระ)

