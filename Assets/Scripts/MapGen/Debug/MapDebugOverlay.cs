using UnityEngine;

namespace ExpiryGame.MapGen
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 임시 OnGUI 디버그: 현재 청크 좌표 / 로드된 청크 수. 에디터/Development Build에서만 컴파일된다.
    public class MapDebugOverlay : MonoBehaviour
    {
        [SerializeField] private ChunkStreamer streamer;

        private void OnGUI()
        {
            var events = (IChunkEvents)streamer;
            var text = $"Chunk {events.CurrentChunk.x}, {events.CurrentChunk.y}   Loaded {events.LoadedChunks.Count}";
            // 화면 왼쪽 아래 (플레이 화면 중앙/HUD 영역을 피한다)
            GUI.Label(new Rect(10f, Screen.height - 48f, 270f, 24f), text);
        }
    }
#else
    // 릴리스 빌드에서는 컴파일되지 않는다. 씬에 남은 컴포넌트가 Missing Script가 되지 않도록 빈 껍데기만 둔다.
    public class MapDebugOverlay : MonoBehaviour
    {
        [SerializeField] private ChunkStreamer streamer;
    }
#endif
}
