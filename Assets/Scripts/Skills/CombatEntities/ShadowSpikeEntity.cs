using System.Collections;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;

namespace NecroDefense.Skills.CombatEntities
{
    /// <summary>
    /// Erupting Shadow Spike Pillar.
    /// Telegraphs ground shadow, erupts piercing shadow spikes, and deals AoE Necrotic damage.
    /// </summary>
    public class ShadowSpikeEntity : MonoBehaviour
    {
        private float _damage;
        private float _radius;
        private Unit _attacker;
        private SpriteRenderer _sr;

        public void Initialize(float damage, float radius, Unit attacker)
        {
            _damage = damage;
            _radius = radius;
            _attacker = attacker;

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 9;

            StartCoroutine(EruptRoutine());
        }

        private IEnumerator EruptRoutine()
        {
            // Phase 1: Ground telegraph (Shadow ring / rift)
            _sr.sprite = Utils.ProceduralVisuals.GetProjectileSprite("ShadowRift");
            _sr.color = Color.white;
            transform.localScale = Vector3.one * (_radius * 0.8f);

            yield return new WaitForSeconds(0.25f);

            // Phase 2: Spike eruption!
            _sr.sprite = Utils.ProceduralVisuals.GetProjectileSprite("ShadowSpike");
            transform.localScale = Vector3.one * (_radius * 1.6f);

            // Deal AoE damage to hostile units in radius
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable) &&
                    hit.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team != Faction.Player && targetable.IsValidTarget)
                    {
                        Vector2 hitDir = ((Vector2)targetable.Position - (Vector2)transform.position).normalized;
                        DamageInfo info = new DamageInfo(_damage, DamageType.Necrotic, _attacker, hitDir, 0.8f);
                        damageable.TakeDamage(info);
                    }
                }
            }

            // Hold spike peak
            yield return new WaitForSeconds(0.35f);

            // Phase 3: Fade and crumble
            float fadeTime = 0.2f;
            float elapsed = 0f;
            Color initialColor = _sr.color;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                _sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

