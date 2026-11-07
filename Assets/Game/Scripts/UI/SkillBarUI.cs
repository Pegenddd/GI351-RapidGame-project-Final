using UnityEngine;
using UnityEngine.Serialization;

namespace NecroDefense
{
    /// <summary>แถบสกิล (Q Mass Raise / RMB Teleport / E Arcane Nova)</summary>
    public class SkillBarUI : MonoBehaviour
    {
        [SerializeField] private SkillSlotUI raiseDeadSlot;
        [SerializeField] private SkillSlotUI teleportSlot;
        [FormerlySerializedAs("enigmaSlot")]
        [SerializeField] private SkillSlotUI novaSlot;

        private void Start()
        {
            PlayerSkills s = GameManager.Instance != null ? GameManager.Instance.PlayerSkills : null;
            if (s == null) return;
            if (raiseDeadSlot != null) raiseDeadSlot.Setup(UIUtil.KeyLabel(s.MassRaiseKey), "", "Mass Raise (Arise!)",
                "Raise every corpse around you. Each corpse rolls its own chance.\nCooldown " + s.MassRaiseCooldown + "s   (Left-click a corpse = raise just one)");
            if (teleportSlot != null) teleportSlot.Setup("RMB", "", "Teleport", "Right-click to teleport to the cursor. Free, " + s.TeleportCooldown + "s cooldown.");
            if (novaSlot != null)
            {
                novaSlot.Setup(UIUtil.KeyLabel(s.NovaKey), "", "Arcane Nova",
                    "Stand still and channel, then a magic circle explodes around you.\nDamage " + s.NovaDamage + "   Radius " + s.NovaRadius + "   Cooldown " + s.NovaCooldown + "s");
                novaSlot.SetLocked(false, "");
            }
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.PlayerSkills == null) return;
            PlayerSkills s = gm.PlayerSkills;

            if (raiseDeadSlot != null) raiseDeadSlot.SetState(s.MassRaiseRemaining, s.MassRaiseCooldown, true);
            if (teleportSlot != null) teleportSlot.SetState(s.TeleportRemaining, s.TeleportCooldown, true);
            if (novaSlot != null) novaSlot.SetState(s.NovaRemaining, s.NovaCooldown, true);
        }
    }
}
