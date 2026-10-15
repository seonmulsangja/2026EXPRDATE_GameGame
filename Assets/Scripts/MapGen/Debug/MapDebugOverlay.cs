using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 임시 OnGUI 디버그: 현재 청크 좌표 / 로드된 청크 수. 화면 오른쪽 위.
    public class MapDebugOverlay : MonoBehaviour
    {
        [SerializeField] private ChunkStreamer streamer;

        private void OnGUI()
        {
            var events = (IChunkEvents)streamer;
            var text = $"Chunk {events.CurrentChunk.x}, {events.CurrentChunk.y}   Loaded {events.LoadedChunks.Count}";
            GUI.Label(new Rect(Screen.width - 280f, 8f, 270f, 24f), text);
        }
    }
}
