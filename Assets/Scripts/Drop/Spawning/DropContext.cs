using UnityEngine;

namespace ExpiryGame.Drop
{
    // 월드 드롭이 필요로 하는 외부 의존성 묶음. DropSpawner가 만들어 DroppedFood에 넘긴다.
    public sealed class DropContext
    {
        public DropFeelSettings Settings;
        public IFoodInventory Inventory;
        public Transform Collector;
        public IGameClock Clock;
    }
}
