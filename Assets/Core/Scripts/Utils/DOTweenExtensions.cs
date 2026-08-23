using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Core
{
    public static class DOTweenExtensions
    {
        public static async UniTask WaitForCompletionUniTask(this Tween t, CancellationToken ct)
        {
            if (!t.active)
            {
                return;
            }
            while (t.active && !t.IsComplete())
            {
                await UniTask.Yield(ct);
            }
        }
    }
}
