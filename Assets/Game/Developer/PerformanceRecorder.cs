using System;
using System.IO;
using System.Globalization;
using UnityEngine;
namespace Podz {
    /// <summary>Read-only timing telemetry: never changes timeScale, fixedDeltaTime or test intervals.</summary>
    public class PerformanceRecorder : MonoBehaviour {
        double _real,_game,_dungeonGame;int _frames,_slow;StreamWriter _writer;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install() {
            if(Environment.GetEnvironmentVariable("PODZ_PERF")!="1")return;
            new GameObject("ReadOnlyPerformanceRecorder").AddComponent<PerformanceRecorder>();
        }
        void Start() {
            var dir=Path.Combine(Application.dataPath,"../Diagnostics/Performance");Directory.CreateDirectory(dir);
            _writer=new StreamWriter(Path.Combine(dir,"performance.csv"),false);
            _writer.WriteLine("real_seconds,frames,game_seconds,dungeon_clock_seconds,fps,game_seconds_per_real_second,dungeon_seconds_per_real_second,frames_above_max_delta,time_scale,max_delta,dungeon_paused,stage");
            _writer.Flush();ResetWindow();
        }
        void ResetWindow(){_real=Time.realtimeSinceStartupAsDouble;_game=Time.timeAsDouble;_dungeonGame=DTime.Now;_frames=Time.frameCount;_slow=0;}
        void Update() {
            if(Time.unscaledDeltaTime>Time.maximumDeltaTime+.005f)_slow++;
            if(Time.realtimeSinceStartupAsDouble-_real>=10)FlushWindow();
        }
        void FlushWindow(){
            if(_writer==null)return;
            double elapsed=Time.realtimeSinceStartupAsDouble-_real,game=Time.timeAsDouble-_game,dungeon=DTime.Now-_dungeonGame;int frames=Time.frameCount-_frames;
            if(elapsed<.01)return;
            string stage="manual";
            _writer.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0:F3},{1},{2:F3},{3:F3},{4:F3},{5:F5},{6:F5},{7},{8:F3},{9:F5},{10},{11}",elapsed,frames,game,dungeon,frames/elapsed,game/elapsed,dungeon/elapsed,_slow,Time.timeScale,Time.maximumDeltaTime,DTime.Paused?1:0,stage));
            _writer.Flush();ResetWindow();
        }
        void OnDestroy(){FlushWindow();_writer?.Dispose();_writer=null;}
    }
}
