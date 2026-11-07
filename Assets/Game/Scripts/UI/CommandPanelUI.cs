using UnityEngine;
using UnityEngine.Serialization;

namespace NecroDefense
{
    /// <summary>
    /// เมนูวงกลมรอบมินเนียนที่ถูกคลิก (สั่งทีละตัว)
    /// Hold Area = ปกป้องบริเวณนั้น (ค่าเริ่มต้น) / Move = ย้ายไปจุดที่เลือกแล้วปกป้องตรงนั้น / Follow Player = เดินตามผู้เล่น
    /// </summary>
    public class CommandPanelUI : MonoBehaviour
    {
        [SerializeField] private Vector2 screenOffset = Vector2.zero;
        [Tooltip("ปกป้องบริเวณนั้น")]
        [FormerlySerializedAs("selfDefenseOption")]
        [SerializeField] private RingOptionUI holdAreaOption;
        [SerializeField] private RingOptionUI moveOption;
        [SerializeField] private RingOptionUI followPlayerOption;

        private MinionUnit _target;
        private bool _wired;
        private RectTransform _rt;

        public bool IsOpen => gameObject.activeSelf;

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            _rt = (RectTransform)transform;
            if (holdAreaOption != null) holdAreaOption.Bind(() => Pick(MinionCommand.SelfDefense));
            if (moveOption != null) moveOption.Bind(() => Pick(MinionCommand.MoveTo));
            if (followPlayerOption != null) followPlayerOption.Bind(() => Pick(MinionCommand.FollowPlayer));
        }

        public void Show(MinionUnit minion)
        {
            Wire();
            _target = minion;
            gameObject.SetActive(minion != null);
            Refresh();
            Follow();
        }

        public void Hide()
        {
            _target = null;
            gameObject.SetActive(false);
        }

        private void Pick(MinionCommand c)
        {
            if (GameManager.Instance == null) return;
            if (c == MinionCommand.MoveTo)
            {
                GameManager.Instance.Minions.BeginMoveOrder(GameManager.Instance.Minions.SelectedMinion);
                return;
            }
            GameManager.Instance.Minions.CommandSelected(c);
            GameManager.Instance.Minions.Deselect();
        }

        private void LateUpdate()
        {
            if (_target == null || !_target.IsAlive)
            {
                if (GameManager.Instance != null) GameManager.Instance.Minions.Deselect();
                else Hide();
                return;
            }
            Follow();
            Refresh();
        }

        private void Follow()
        {
            if (_target == null || _rt == null || Camera.main == null) return;
            Vector3 sp = Camera.main.WorldToScreenPoint(_target.transform.position);
            _rt.position = new Vector3(sp.x, sp.y, 0f) + (Vector3)(screenOffset * _rt.lossyScale.x);
        }

        private void Refresh()
        {
            if (_target == null) return;
            MinionCommand c = _target.Command;
            if (moveOption != null) moveOption.SetHighlighted(c == MinionCommand.MoveTo);
            if (holdAreaOption != null) holdAreaOption.SetHighlighted(c == MinionCommand.SelfDefense);
            if (followPlayerOption != null) followPlayerOption.SetHighlighted(c == MinionCommand.FollowPlayer);
        }
    }
}
