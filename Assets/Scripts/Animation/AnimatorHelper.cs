using Cysharp.Threading.Tasks;
using Data;
using UnityEngine;

public class AnimatorHelper : Singleton<AnimatorHelper>
{
    public static async UniTask WaitForStateComplete(Animator animator, string stateName, int layer = -1)
    {
        await UniTask.Yield();
        int hash = Animator.StringToHash(stateName);
        await UniTask.WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == hash || animator.GetNextAnimatorStateInfo(layer).shortNameHash == hash);
        await UniTask.WaitUntil(() =>
        {
            var s = animator.GetCurrentAnimatorStateInfo(layer);
            return !animator.IsInTransition(layer) && s.shortNameHash == hash && s.normalizedTime >= 1f;
        });
    }
}