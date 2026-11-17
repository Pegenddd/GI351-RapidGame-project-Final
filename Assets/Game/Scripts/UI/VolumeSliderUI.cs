using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>แถบปรับเสียง 1 ช่อง (Master / Music / SFX / UI) ในหน้า Pause</summary>
    public class VolumeSliderUI : MonoBehaviour
    {
        public AudioChannel channel = AudioChannel.Master;
        public Slider slider;
        public TMP_Text valueText;

        private bool _ready;

        private void OnEnable()
        {
            AudioManager m = AudioManager.Get();
            if (slider == null) slider = GetComponentInChildren<Slider>();
            if (slider == null || m == null) return;
            _ready = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = m.GetVolume(channel);
            Refresh();
            _ready = true;
        }

        private void Awake()
        {
            if (slider == null) slider = GetComponentInChildren<Slider>();
            if (slider != null) slider.onValueChanged.AddListener(OnChanged);
        }

        private void OnChanged(float v)
        {
            Refresh();
            if (!_ready) return;
            AudioManager m = AudioManager.Get();
            if (m == null) return;
            m.SetVolume(channel, v);
            // ให้ได้ยินระดับเสียงใหม่ทันที
            if (channel == AudioChannel.SFX) m.PlayOneShot("SFX_Enemy_Hit_Generic", null, AudioChannel.SFX, 0.8f);
            else if (channel != AudioChannel.Music) Sfx.UI("SFX_UI_Click");
        }

        private void Refresh()
        {
            if (valueText != null && slider != null) valueText.text = Mathf.RoundToInt(slider.value * 100f) + "%";
        }
    }
}
