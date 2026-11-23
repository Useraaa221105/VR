using UnityEngine;
namespace Podz {
    /// <summary>Отдельный небьющийся общим Sfx-пулом голос. Не зависит от подсказок/уничтоженного монстра.</summary>
    public class MandrakeVoice : MonoBehaviour {
        static MandrakeVoice _instance; AudioSource _source;
        public static bool CanStart { get { Ensure();return _instance!=null && _instance._source.clip!=null && !_instance._source.isPlaying; } }
        public static AudioSource Source => _instance!=null?_instance._source:null;
        public static int OutputSamples; public static double OutputEnergy; public static float OutputPeak;
        static void Ensure() {
            if(_instance!=null)return;if(Sfx.I==null)return;
            var go=new GameObject("MandrakeDedicatedVoice");go.transform.SetParent(Sfx.I.transform,false);_instance=go.AddComponent<MandrakeVoice>();
            var a=go.AddComponent<AudioSource>();_instance._source=a;
            a.playOnAwake=false;a.loop=false;a.clip=Resources.Load<AudioClip>("PodzMandrakeCry") ?? Sfx.FallbackMandrakeClip;
            if(a.clip!=null) a.clip.LoadAudioData();
            a.spatialBlend=1f;a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=3f;a.maxDistance=18f;
            a.volume=.95f;a.pitch=1f;a.dopplerLevel=0f;a.priority=16;a.mute=false;
            // outputAudioMixerGroup=null: не уходит в выключенный/пустой пользовательский Mixer.
            // Глобальное mute пользователя не обходим: состояние отмечается в диагностике.
            if(a.clip==null)Debug.LogError("[MandrakeVoice] Missing Resources/PodzMandrakeCry.wav");
        }
        public static bool TryPlay(Vector3 position,float volume=.95f) {
            if(!CanStart)return false;_instance.transform.position=position;_instance._source.volume=Mathf.Clamp01(volume);_instance._source.Play();
            int listeners=Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length;
            Debug.Log($"[MandrakeVoice] Play pos={position}; listeners={listeners}; listenerVolume={AudioListener.volume}; listenerPause={AudioListener.pause}; sourceMute={_instance._source.mute}; mixer=direct; DSP={AudioSettings.dspTime:F3}");
            return true;
        }
        // Реальные отсчёты audio DSP, а не увеличение счётчика вызова Play.
        void OnAudioFilterRead(float[] data,int channels) {foreach(float v in data){OutputSamples++;OutputEnergy+=(double)v*v;float av=v<0?-v:v;if(av>OutputPeak)OutputPeak=av;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){_instance=null;OutputSamples=0;OutputEnergy=0;OutputPeak=0;}
    }
}
