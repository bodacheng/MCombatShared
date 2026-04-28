using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class SKillAnalyzer
{
    class EventNameAndAtFrame 
    {
        public string name;
        public float startFrame;
    }
    
    public static readonly List<string> AttackFrameStartMethodNames = new List<string>() {
        "SetRightHandMarkerManager","SetLeftHandMarkerManager",
        "SetRightFootMarkerManager","SetLeftFootMarkerManager",
        "SetRightHandWeaponMarkerManager","SetLeftHandWeaponMarkerManager",
        "SetHeadMarkerManager","SetTailMarkerManager"
    };
    
    public static readonly List<string> AttackClearMethodNames = new List<string>() {
        "SetRightHandMarkerManager","SetLeftHandMarkerManager",
        "SetRightFootMarkerManager","SetLeftFootMarkerManager",
        "SetRightHandWeaponMarkerManager","SetLeftHandWeaponMarkerManager",
        "SetHeadMarkerManager","SetTailMarkerManager"
    };
    public static readonly List<string> EffectsAttackFrameStartMethodNames = new List<string>()
    {
        "MagicForward","MagicForwardOnBody","Bullet_shoot_from_body_part","Bullet_shoot_from_body_part_TD","BlastAttack","ReleasePreparedMagic","ReleasePreparedMagicToAir","MagicToEnemy"
    };
    
    public static async UniTask<IDictionary<string, AnimationClip>> AllSkillAnims(string type)
    {
        IDictionary<string, AnimationClip> AnimationClips = new Dictionary<string, AnimationClip>();
        
        var loadPath = Addressables.LoadResourceLocationsAsync("skill_anim");
        await loadPath.Task;
        if (loadPath.Status == AsyncOperationStatus.Succeeded)
        {
            foreach (var path in loadPath.Result)
            {
                //Debug.Log(":"+ path);
                var handle = Addressables.LoadAssetAsync<AnimationClip>(path);
                await handle.Task;
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }

                    continue;
                }

                var animationClip = handle.Result;
                if (!AnimationClips.ContainsKey(animationClip.name))
                {
                    AnimationClips.Add(animationClip.name, animationClip);
                }
            }
        }
        Addressables.Release(loadPath);
        return AnimationClips;
    }
}
