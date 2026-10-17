using UnityEngine;
using ExpiryGame.Drop;

namespace ExpiryGame.Landmark
{
    // 스폰된 랜드마크 프리팹이 씬 서비스에 접근하는 유일한 통로 (프리팹은 씬 오브젝트를 직접 참조할 수 없다).
    public class LandmarkContext
    {
        public LandmarkData Data;
        // 랜드마크 ID(종류+위치). 청크가 언로드돼도 상태를 이어 붙이는 키
        public string Id;
        public CampfireStateStore Store;
        public Transform Player;
        public IHealable PlayerHealth;
        public IDropService Drops;
        public IDangerZoneSink Danger;
        public IFoodInventory Inventory;
        public ILandmarkEventSink Events;
    }

    // 랜드마크 프리팹 안의 컴포넌트가 스폰 직후 컨텍스트를 받는 계약
    public interface ILandmarkPart
    {
        void Bind(LandmarkContext context);
    }
}
