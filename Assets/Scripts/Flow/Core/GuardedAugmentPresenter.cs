using System;
using UnityEngine;

namespace ExpiryGame.Flow
{
    // 팀원 선택 화면(IAugmentChooser)을 감싸서 게임이 멈추지 않게 지킨다.
    //  - 후보가 없으면(전부 최대 중첩 등) 화면을 열지 않고 대체 보상(fallback = NullAugmentPresenter: 체력 소량 회복)으로 끝낸다
    //  - 화면이 에러를 내거나 선택 없이 닫히면, 일정 시간을 기다려 자동 선택하지 않고 로그 에러를 남긴 뒤
    //    첫 번째 후보를 적용하고 재개한다 (첫 후보도 없으면 대체 보상)
    //  - 완료 콜백은 요청마다 정확히 한 번. Cancel/새 요청 뒤에 늦게 온 옛 결과는 무시한다
    public sealed class GuardedAugmentPresenter : IAugmentSelectionPresenter
    {
        private readonly IAugmentChooser chooser;
        private readonly IAugmentEffects effects;
        private readonly IAugmentSelectionPresenter fallback;
        private int ticket;

        public GuardedAugmentPresenter(IAugmentChooser chooser, IAugmentEffects effects, IAugmentSelectionPresenter fallback)
        {
            this.chooser = chooser;
            this.effects = effects;
            this.fallback = fallback;
        }

        public void RequestSelection(int level, Action onComplete)
        {
            var mine = ++ticket;
            var finished = false;
            void Finish()
            {
                if (finished || mine != ticket) return;
                finished = true;
                onComplete?.Invoke();
            }

            var hasCandidates = false;
            try { hasCandidates = chooser.Prepare(); }
            catch (Exception e) { Debug.LogException(e); }

            if (!hasCandidates)
            {
                fallback.RequestSelection(level, Finish);
                return;
            }

            try
            {
                chooser.Open(chosen =>
                {
                    if (finished || mine != ticket) return;
                    if (chosen == null)
                    {
                        Debug.LogError("[Augment] 증강 선택 화면이 선택 없이 닫혔습니다. 첫 번째 후보를 적용하고 재개합니다.");
                        Recover(level, Finish);
                        return;
                    }
                    if (!effects.Apply(chosen)) Debug.LogError($"[Augment] '{chosen.Id}' 는 더 이상 쌓을 수 없어 적용하지 못했습니다.");
                    Finish();
                });
            }
            catch (Exception e)
            {
                Debug.LogError("[Augment] 증강 선택 화면을 여는 중 에러가 났습니다. 첫 번째 후보를 적용하고 재개합니다.");
                Debug.LogException(e);
                if (!finished && mine == ticket) Recover(level, Finish);
            }
        }

        public void Cancel()
        {
            ticket++;
            try { chooser.Close(); }
            catch (Exception e) { Debug.LogException(e); }
            fallback.Cancel();
        }

        private void Recover(int level, Action finish)
        {
            try { chooser.Close(); }
            catch (Exception e) { Debug.LogException(e); }

            var first = chooser.FirstCandidate;
            if (first != null && effects.Apply(first))
            {
                finish();
                return;
            }
            fallback.RequestSelection(level, finish);
        }
    }
}
