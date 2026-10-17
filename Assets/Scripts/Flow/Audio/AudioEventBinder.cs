using System;
using System.Collections.Generic;
using ExpiryGame.Combat;
using ExpiryGame.Drop;
using ExpiryGame.Landmark;

namespace ExpiryGame.Flow
{
    // 기존 이벤트(먹기/타격/썩음/줍기/파괴)를 효과음으로 연결만 한다. 클립이 없으면 AudioManager가 무시한다.
    public class AudioEventBinder : IDisposable
    {
        private readonly AudioManager audio;
        private readonly AudioCatalog catalog;
        private readonly FoodConsumer consumer;
        private readonly SwingHitDetector hitDetector;
        private readonly IFoodInventory inventory;
        private readonly IDropService drops;
        private readonly ILandmarkEvents landmarks;
        private readonly HashSet<FoodItem> watched = new HashSet<FoodItem>();
        private readonly List<FoodItem> buffer = new List<FoodItem>();

        public AudioEventBinder(AudioManager audio, AudioCatalog catalog, FoodConsumer consumer, SwingHitDetector hitDetector,
            IFoodInventory inventory, IDropService drops, ILandmarkEvents landmarks = null)
        {
            this.audio = audio;
            this.catalog = catalog;
            this.consumer = consumer;
            this.hitDetector = hitDetector;
            this.inventory = inventory;
            this.drops = drops;
            this.landmarks = landmarks;

            if (consumer != null) consumer.FoodEaten += OnEaten;
            if (hitDetector != null) hitDetector.HitLanded += OnHit;
            if (inventory != null)
            {
                inventory.Changed += OnInventoryChanged;
                OnInventoryChanged();
            }
            if (drops != null) drops.FoodCollected += OnCollected;
            if (landmarks != null)
            {
                landmarks.OnFoodEnhanced += OnEnhanced;
                landmarks.OnFridgeOpened += OnFridgeOpened;
            }
        }

        public void Dispose()
        {
            if (consumer != null) consumer.FoodEaten -= OnEaten;
            if (hitDetector != null) hitDetector.HitLanded -= OnHit;
            if (inventory != null) inventory.Changed -= OnInventoryChanged;
            if (drops != null) drops.FoodCollected -= OnCollected;
            if (landmarks != null)
            {
                landmarks.OnFoodEnhanced -= OnEnhanced;
                landmarks.OnFridgeOpened -= OnFridgeOpened;
            }
            foreach (var item in watched)
            {
                item.Rotted -= OnRotted;
                item.Broken -= OnBroken;
            }
            watched.Clear();
        }

        private void OnEaten(FoodItem item) => audio.PlaySfx(catalog.sfxEat);
        private void OnHit(HitInfo info) => audio.PlaySfx(catalog.sfxHit);
        private void OnCollected(FoodItem item) => audio.PlaySfx(catalog.sfxPickup);
        private void OnRotted(FoodItem item) => audio.PlaySfx(catalog.sfxRot);
        private void OnBroken(FoodItem item) => audio.PlaySfx(catalog.sfxBreak);
        private void OnEnhanced(FoodEnhancedInfo info) => audio.PlaySfx(catalog.sfxEnhance);
        private void OnFridgeOpened(FridgeOpenedInfo info) => audio.PlaySfx(catalog.sfxFridgeOpen);

        // 가방에 들어온 음식의 Rotted/Broken만 구독하고, 나간 음식은 해제한다
        private void OnInventoryChanged()
        {
            buffer.Clear();
            buffer.AddRange(inventory.Items);
            foreach (var item in buffer)
            {
                if (!watched.Add(item)) continue;
                item.Rotted += OnRotted;
                item.Broken += OnBroken;
            }

            watched.RemoveWhere(item =>
            {
                if (buffer.Contains(item)) return false;
                item.Rotted -= OnRotted;
                item.Broken -= OnBroken;
                return true;
            });
        }
    }
}
