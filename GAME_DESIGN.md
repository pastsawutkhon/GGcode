# ATM - Arrest That Man

ชื่อโปรเจกต์ Unity คือ `GGcode`

Project Context สำหรับเกม Unity C# แนว 2D Top-down Endless Roguelite ดูโครงสร้างคลาสใน [CLASS_DIAGRAM.md](CLASS_DIAGRAM.md)

อ้างอิง Project Context Summary v1.0 (16 พฤศจิกายน 2026) และผลรีวิว UML ล่าสุด งานออกแบบให้ความสำคัญกับ Level Scaling, Character Stats และ Wave System ตามหัวข้อของรายวิชา

## Confirmed Requirements

### Core Gameplay

- ผู้เล่นเดินด้วย WASD เล็งด้วยเมาส์ และกดเมาส์ค้างเพื่อยิงต่อเนื่องตาม AttackSpeed
- ใช้ปืนหลักชนิดเดียวตลอด Run
- เกมเป็น Endless Wave ใช้ Wave Number เป็นความคืบหน้าหลัก ไม่มี Player Level และ Run จบเมื่อผู้เล่นตาย
- ปืนยิงแบบ Spread และอัปเกรดจำนวนกระสุนต่อการยิง ระยะยิง และจำนวนศัตรูที่กระสุนยิงทะลุได้
- กระสุนหายทันทีเมื่อเดินทางถึงระยะสูงสุด

### Wave & Level Scaling

- ศัตรูทยอยเกิดตามเวลาจนครบจำนวนที่กำหนดในแต่ละ Wave โดยไม่ต้องรอให้ศัตรูตัวก่อนตายและไม่มีเพดานจำนวนศัตรูที่มีชีวิตพร้อมกัน
- ต้องกำจัดศัตรูทั้งหมดจึงจบ Wave
- จำนวนศัตรูเพิ่มเป็นช่วง ๆ และมีเพดาน
- ศัตรูมี Base Stats ตามประเภท HP และ ATK เพิ่มแบบ Exponential ตาม Wave ส่วน DEF เพิ่มตาม Wave เช่นกัน
- มี Melee ที่โจมตีประชิด, Ranged ที่ยิงกระสุนจากระยะไกล, Fast ที่เคลื่อนที่เร็ว และ Boss ที่แข็งแกร่งกว่าศัตรูปกติ
- มี Boss หลายแบบ Boss เกิดทุก 10 Wave หลังจากศัตรูปกติเกิดครบและถูกกำจัดทั้งหมด ต้องกำจัด Boss เพื่อจบ Wave ดังกล่าว

### Room System

- ใช้ Room Layout ขอบเขตจำกัดที่ออกแบบไว้ล่วงหน้าหลายแบบ ห้องมีสิ่งกีดขวางทั้งการเดินและกระสุน
- ใช้ Spawn Points ที่กำหนดไว้ในห้องและตรวจสอบระยะห่างจากผู้เล่นก่อนให้ศัตรูเกิด
- หลังจบทุก Wave ผู้เล่นเลือกการ์ดก่อน จากนั้น Wave ถัดไปเริ่มอัตโนมัติ
- สุ่มห้องสำหรับ Wave ถัดไปหลังจบ Wave 10, 20, 30 และทุก Wave ที่หารด้วย 10 ลงตัว โดยเปลี่ยนห้องหลังเลือกการ์ด

### Character Stats

| กลุ่ม | Stats |
| --- | --- |
| พลังชีวิตและการเคลื่อนที่ | MaxHP, CurrentHP, MoveSpeed, HPRegen |
| การต่อสู้ | AttackDamage, Defense, AttackSpeed, CritChance, CritDamage, LifeSteal |
| กระสุน | ProjectileRange, ProjectileCount, PierceCount |

เวอร์ชันปัจจุบันไม่มี Poison, Burn, Slow, Stun หรือระบบ Buff/Debuff

### Card Upgrade

- หลังจบ Wave สุ่มการ์ด 3 ใบให้เลือก 1 ใบ
- มีระดับ Legendary, Epic, Rare, Uncommon และ Common ใช้ Wave ที่เพิ่งเคลียร์และ Luck คำนวณน้ำหนักด้วย `UpgradeRarityCalculator` แล้วสุ่ม Rarity แยกต่อใบ ทั้งตอนเปิดชุดแรกและ Reroll อนุญาตให้ Tier ซ้ำกันในชุดเดียว
- การ์ดเพิ่ม Basic Stats ใช้โบนัส Percentage สะสมจากค่า Base ส่วน Luck เพิ่มเป็นแต้ม
- Prototype สุ่มจากรายการประเภทการ์ดในโค้ดและใช้เอฟเฟกต์กับผู้เล่นเมื่อเลือก กติกาจำนวนคงเหลือและ Max Stack ของการ์ด Stat ยังไม่กำหนด
- มีการ์ด Heal, เพิ่ม MaxHP, เพิ่ม ATK, Multishot, Pierce, Explosion และการ์ดอื่น ๆ
- Heal ฟื้น CurrentHP ส่วนการเพิ่ม MaxHP จะฟื้น CurrentHP ตามจำนวน MaxHP ที่เพิ่มจริง โดย CurrentHP อยู่ภายในเพดาน MaxHP
- Special Effect Cards บางใบไม่มี Stat Bonus การ์ดอัปเกรด Stats หรือความสามารถของปืนเดิม

### Economy

- ศัตรูดรอปเงิน โดยเงินที่ได้รับเพิ่มตามความคืบหน้าของ Wave
- ใช้เงินใน Run เพื่อสุ่มการ์ดใหม่ 3 ใบ ราคาเพิ่มตามจำนวน Reroll สำเร็จสะสมข้าม Wave และห้อง และเริ่มนับใหม่เมื่อเริ่ม Run ใหม่ เวอร์ชันนี้ยังไม่ต้องมีร้านค้าภายใน Run
- เมื่อผู้เล่นตาย แสดง Wave ที่ไปถึง, Total Earned, Total Spent และ Remaining Coins โดยยอดคงเหลือเท่ากับยอดที่ได้รับลบยอดที่ใช้
- โอนเงินที่เหลือหลังจบ Run ไปเป็นเงินสำหรับ Permanent Upgrade
- Permanent Upgrade รองรับ Base HP, ATK, DEF และโบนัสเงิน

### OOP และขอบเขตการพัฒนา

- ใช้ Unity C# Architecture และ UML Class Diagram พร้อม Attributes, Methods, Data Types, Inheritance และ Multiplicity
- ใช้ Inheritance เมื่อเป็นความสัมพันธ์แบบ IS-A และใช้ Composition หรือ Association ตามการเป็นเจ้าของและอายุของข้อมูล
- ใช้ลำดับชั้น Character, Enemy, UpgradeEffect, ProjectileHitEffect และ PermanentUpgrade ตาม [CLASS_DIAGRAM.md](CLASS_DIAGRAM.md)
- สำหรับงานออกแบบหรือพัฒนาระบบใหม่ทุกครั้ง ให้อ่านเอกสารทั้งสองไฟล์ก่อนเริ่มงาน และถามผู้ใช้ก่อนเปลี่ยน Confirmed Requirements เมื่อได้รับคำยืนยันแล้วให้อัปเดตเอกสารที่เกี่ยวข้อง
- รอคำสั่งก่อนสร้าง Gameplay Scripts

## Proposed Defaults

ข้อเสนอสำหรับเริ่มออกแบบและทดสอบ ค่าจริงยังรอการยืนยัน

| ระบบ | ค่าเริ่มต้นหรือพฤติกรรมที่เสนอ |
| --- | --- |
| Spread และ Pierce | สุ่มมุมกระสุนภายในพัดที่มีทิศเล็งเป็นแกนกลาง ถ้ายิงหลายเม็ดจะสุ่มหนึ่งเม็ดต่อช่วงมุมที่แบ่งเท่า ๆ กัน และ PierceCount = 0 หมายถึงหยุดที่ศัตรูตัวแรก ค่า 1 ยิงทะลุตัวแรกและหยุดที่ตัวที่สอง |
| Spawn Point | หากไม่มีจุดที่ผ่านระยะขั้นต่ำ ให้เลื่อนการเกิดและลองใหม่โดยยังไม่นับศัตรูตัวนั้นว่าเกิดแล้ว |
| จำนวนศัตรู | ทดลองใช้ `min(8 + 2 × floor((wave - 1) / 3), 40)` สำหรับศัตรูปกติ และนับ Boss เพิ่มต่างหาก |
| Spawn Ratio | Prototype ใช้ Weighted Random ต่อหนึ่งตัว เปลี่ยนโอกาสตามช่วง Wave ตามตารางด้านล่าง |
| Enemy Scaling | ทดลองใช้ `BaseHP × 1.07^(wave - 1)`, `BaseATK × 1.045^(wave - 1)` และ `BaseDEF + 0.5 × (wave - 1)` |
| Damage และ DEF | ทดลองใช้ `RawDamage × 100 / (100 + Defense)` แล้วทดสอบ Balance ร่วมกับอัตราโตของ DEF |
| Stat Modifiers | ใช้ `Base × (1 + ผลรวม Percentage)` สำหรับ Basic Stats เก็บเปอร์เซ็นต์เป็นทศนิยม เช่น 0.15 คือ 15% |
| Card Offer และ Reroll | ใช้ Wave ที่เพิ่งเคลียร์ สุ่ม 3 ครั้งแยกกันจากสูตรเดียวกัน โดยหักเงินเฉพาะ Reroll ที่สร้างชุดใหม่สำเร็จ |
| ราคา Reroll | ครั้งแรกของ Run ฟรี หลังจากนั้นใช้ `floor(10 × 1.2^(n - 1))` โดย `n` คือจำนวน Reroll ที่สำเร็จแล้วใน Run |
| Run Wallet | ใช้จำนวนเต็มสำหรับเงิน โดย Remaining Coins = Total Earned − Total Spent เมื่อเริ่ม Run ใหม่ด้วย 0 Coins บันทึกยอดใน RunResult ก่อนโอน และใช้ Run ID ป้องกันโอนซ้ำ |
| Permanent Upgrade | ใช้ค่าอัปเกรดจาก PlayerProfile ตอนเริ่ม Run สำหรับ Base HP, ATK, DEF และค่าโบนัสเงินที่ใช้คำนวณรายรับใน Run |
| Run State | หยุด Gameplay และการ Spawn ระหว่างเลือกการ์ด ใช้ `RunState` คุมการเปลี่ยนช่วง และใช้ `WavePhase` แยกการเกิดศัตรู การกำจัดศัตรูที่เหลือ และช่วง Boss |
| Bullet Effects | ให้ Bullet รับผิดชอบความเสียหายจากการชนโดยตรง ให้ Explosion เป็น Hit Effect เพิ่มเติม และให้ Multishot กับ Pierce ใช้ Stat Modifier |

ตัวอย่างจากสูตร Scaling เมื่อ BaseHP = 100 และ BaseATK = 10

| Wave | HP โดยประมาณ | ATK โดยประมาณ | ศัตรูปกติ |
| ---: | ---: | ---: | ---: |
| 1 | 100 | 10.0 | 8 |
| 10 | 184 | 14.9 | 14 |
| 20 | 362 | 23.1 | 20 |
| 30 | 711 | 35.8 | 26 |
| 50 | 2,753 | 86.4 | 40 |

น้ำหนักศัตรูปกติสำหรับ Prototype ใช้ใน `SpawnRatioCalculator`

| Wave | Melee | Ranged | Fast |
| ---: | ---: | ---: | ---: |
| 1–4 | 100% | 0% | 0% |
| 5–9 | 70% | 30% | 0% |
| 10–19 | 60% | 25% | 15% |
| 20–39 | 50% | 30% | 20% |
| 40 ขึ้นไป | 40% | 35% | 25% |

ที่ Wave 10 โปรแกรมแปลงน้ำหนักเป็น Array ผลรวมสะสม `[60, 85, 100]` แล้วใช้ Linear Search หาช่องแรกที่ค่ามากกว่าเลขสุ่ม 0–99 จำนวนจริงของแต่ละประเภทอาจต่างกันในแต่ละ Wave ถ้าจุดเกิดใช้ไม่ได้จะลองใหม่ด้วยประเภทที่เลือกไว้ และเพิ่มตัวนับเมื่อสร้างสำเร็จ

Prototype ใช้ Fast โจมตีสัมผัสด้วย MoveSpeed สูง และใช้ Boss โจมตีสัมผัสจาก Prefab ที่มีอยู่หนึ่งแบบ ส่วนรูปแบบโจมตีและจำนวน Boss ในเกมเต็มยังอยู่ในรายการที่จะออกแบบต่อ

โอกาส Rarity ใช้ไฟล์ [UpgradeRarityCalculator.cs](Assets/Scripts/Upgrades/UpgradeRarityCalculator.cs) ตามสูตรที่อนุมัติ เรียง Legendary, Epic, Rare, Uncommon, Common ที่ Wave 1 ได้ 0.2%, 1%, 10%, 25%, 63.8% ตามลำดับ ส่วน Wave 100 ได้ประมาณ 15%, 73.38325%, 10.39429%, 1.10420%, 0.11826%

เริ่ม Run ที่ 0 Gold และรับ `floor(10 × 1.2^(wave - 1))` Gold ต่อศัตรูที่กำจัด ชุดแรกหลังจบ Wave และ Reroll ครั้งแรกของแต่ละ Run ฟรี การคิดเงินและการนับครั้งเกิดขึ้นเมื่อ Reroll สำเร็จเท่านั้น

## Upgrade Cards ใน Prototype

Tier I–V เรียงจาก Common ถึง Legendary Card Offer และ Reroll ใช้ Wave ที่เพิ่งเคลียร์ คำนวณ Rarity ตาม Wave และ Luck แล้วสุ่มแต่ละใบแยกกัน ชุดหนึ่งมีการ์ด 3 ประเภทต่างกันและ Tier ซ้ำได้

| การ์ด | I | II | III | IV | V |
| --- | ---: | ---: | ---: | ---: | ---: |
| Heal (% ของ Max HP) | 20% | 40% | 70% | 90% | 100% + Barrier 25% ของ Max HP |
| Max HP (%) | +5% | +10% | +15% | +20% | +25% |
| ATK | +10% | +15% | +20% | +25% | +30% |
| Fire Rate | +10% | +20% | +30% | +40% | +50% |
| Range | +5% | +10% | +15% | +20% | +25% |
| Luck | +5 | +10 | +15 | +20 | +25 |

- Max HP, ATK, Fire Rate และ Range ใช้ `Base × (1 + ผลรวมโบนัส %)` เมื่อ Base HP 100 ได้ +20% สองครั้ง จะได้ 120 แล้ว 140 การ์ด Max HP ฟื้น HP ตามเพดานที่เพิ่มจริง
- Heal ฟื้นทันทีได้ถึง 100% เลือกซ้ำได้และยังสุ่มได้เมื่อ HP เต็ม Barrier จาก Heal V บวกเพิ่มจากค่าเดิม รับดาเมจหลังคำนวณ DEF ก่อนถึง HP และอยู่ข้าม Wave จนหมด
- Luck เริ่ม 100 ไม่มีเพดานใน Prototype คูณน้ำหนักหลัง Soft Cap ตาม Priority ก่อนตรวจ Minimum รอบสุดท้าย โดยรักษาเงื่อนไข Minimum เดิมและ Legendary Hard Cap 15%
- การ์ดแสดงชื่อ Tier เลขโรมัน โบนัส และผลก่อน–หลังจาก Stats ของผู้เล่น ยังไม่กำหนด Max Stack สำหรับการ์ด Stat ใน Prototype

## Unresolved Decisions

- ปุ่มเมาส์สำหรับยิง ค่าเริ่มต้นและขีดจำกัดของ Stats มุม Spread และพฤติกรรมกระสุนเมื่อชนสิ่งกีดขวาง
- สูตร Damage และ DEF, Crit, LifeSteal, HPRegen และวิธีปัดค่าที่ต้องเป็นจำนวนเต็ม
- ค่าเริ่มต้นของจำนวนศัตรู ช่วงการเพิ่ม เพดาน อัตราเกิด ระยะ Spawn ขั้นต่ำ และวิธีแก้เมื่อไม่มีจุดที่ใช้ได้เป็นเวลานาน
- อัตรา Scaling ของแต่ละ Stat วิธีจัดสัดส่วนศัตรูปกติ รูปแบบการโจมตีของ Fast และ Boss ตัวคูณ Stats ของ Boss และการเลือก Boss แบบสุ่มหรือวนลำดับ
- วิธีสุ่มห้องและการอนุญาตให้ซ้ำติดกัน การใช้ Room Prefab หรือ Scene แยก จุดเกิดผู้เล่น และการจัดการกระสุนหรือเงินที่ยังอยู่บนพื้นเมื่อจบ Wave หรือเปลี่ยนห้อง
- รายละเอียดเอฟเฟกต์การ์ดและกติกา Explosion เช่น รัศมี ความเสียหาย และการเกิดซ้ำเมื่อกระสุนยิงทะลุ
- วิธีเก็บเงิน เพดาน Reroll ต่อหน้าการ์ด การแสดงผลเมื่อเงินไม่พอ โบนัสเงินแบบ Flat หรือ Percentage และราคา/เพดาน Permanent Upgrade
- การออกกลางคันและรูปแบบการบันทึก PlayerProfile
